using System;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace RemoteMate.Services
{
    internal static class AuthService
    {
        // Production: đặt trong biến môi trường / config, không hardcode
        private static readonly string SecretKey =
            Environment.GetEnvironmentVariable("REMOTE_MATE_JWT_SECRET") ?? "dev-change-this-secret";

        private static readonly byte[] SecretBytes = Encoding.UTF8.GetBytes(SecretKey);

        // Revocation store: key = jti, value = expiryUtc
        private static readonly ConcurrentDictionary<string, DateTime> _revokedTokens = new();

        public static string GenerateAccessToken(string userId, string userName, TimeSpan validFor)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, userId),
                new Claim("name", userName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var creds = new SigningCredentials(new SymmetricSecurityKey(SecretBytes), SecurityAlgorithms.HmacSha256);
            var now = DateTime.UtcNow;

            var token = new JwtSecurityToken(
                issuer: "RemoteMate",
                audience: "RemoteMateClients",
                claims: claims,
                notBefore: now,
                expires: now.Add(validFor),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static ClaimsPrincipal? ValidateToken(string token, out string? validationError)
        {
            validationError = null;

            if (string.IsNullOrWhiteSpace(token))
            {
                validationError = "Token empty";
                return null;
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "RemoteMate",
                ValidateAudience = true,
                ValidAudience = "RemoteMateClients",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(SecretBytes),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };

            try
            {
                var principal = tokenHandler.ValidateToken(token, parameters, out var validatedToken);

                // Kiểm tra revoked list — lấy jti từ token
                var jwt = validatedToken as JwtSecurityToken ?? tokenHandler.ReadJwtToken(token);
                var jti = jwt?.Id ?? jwt?.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;

                if (!string.IsNullOrEmpty(jti) && IsJtiRevoked(jti))
                {
                    validationError = "Token revoked";
                    return null;
                }

                // Clean up expired revoked entries opportunistically
                CleanupExpiredRevocations();

                return principal;
            }
            catch (Exception ex)
            {
                validationError = ex.Message;
                return null;
            }
        }

        public static void RevokeToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token)) return;

            var handler = new JwtSecurityTokenHandler();
            try
            {
                var jwt = handler.ReadJwtToken(token);
                var jti = jwt.Id ?? jwt.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Jti)?.Value;
                var exp = jwt.ValidTo; // UTC

                if (!string.IsNullOrEmpty(jti))
                {
                    _revokedTokens[jti] = exp;
                }
            }
            catch
            {
                // Không parse được token => không revoke
            }
        }

        private static bool IsJtiRevoked(string jti)
        {
            if (_revokedTokens.TryGetValue(jti, out var expiry))
            {
                if (expiry > DateTime.UtcNow) return true;
                // đã hết hạn => remove
                _revokedTokens.TryRemove(jti, out _);
                return false;
            }
            return false;
        }

        private static void CleanupExpiredRevocations()
        {
            var now = DateTime.UtcNow;
            foreach (var kv in _revokedTokens)
            {
                if (kv.Value <= now)
                {
                    _revokedTokens.TryRemove(kv.Key, out _);
                }
            }
        }
    }
}