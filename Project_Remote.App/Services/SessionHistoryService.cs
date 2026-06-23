using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MySqlConnector;
using RemoteMate.Models;

namespace RemoteMate.Services
{
    public class SessionHistoryService
    {
        private readonly DatabaseService _databaseService = new DatabaseService();

        public async Task AddSessionAsync(SessionHistoryItem item)
        {
            using (var connection = _databaseService.GetConnection())
            {
                await connection.OpenAsync();

                string query = @"
                    INSERT INTO SessionHistory
                    (
                        OwnerUsername,
                        Role,
                        RemoteUsername,
                        RemoteHostName,
                        RemoteIp,
                        StartTime,
                        EndTime,
                        DurationSeconds,
                        Status,
                        Note
                    )
                    VALUES
                    (
                        @OwnerUsername,
                        @Role,
                        @RemoteUsername,
                        @RemoteHostName,
                        @RemoteIp,
                        @StartTime,
                        @EndTime,
                        @DurationSeconds,
                        @Status,
                        @Note
                    );
                ";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@OwnerUsername", item.OwnerUsername);
                    cmd.Parameters.AddWithValue("@Role", item.Role);
                    cmd.Parameters.AddWithValue("@RemoteUsername", item.RemoteUsername);
                    cmd.Parameters.AddWithValue("@RemoteHostName", item.RemoteHostName);
                    cmd.Parameters.AddWithValue("@RemoteIp", item.RemoteIp);
                    cmd.Parameters.AddWithValue("@StartTime", item.StartTime);
                    cmd.Parameters.AddWithValue("@EndTime", item.EndTime.HasValue ? item.EndTime.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@DurationSeconds", item.DurationSeconds);
                    cmd.Parameters.AddWithValue("@Status", item.Status);
                    cmd.Parameters.AddWithValue("@Note", item.Note);

                    await cmd.ExecuteNonQueryAsync();
                }
            }
        }

        public async Task<List<SessionHistoryItem>> GetRecentSessionsAsync(string ownerUsername, int limit = 5)
        {
            List<SessionHistoryItem> result = new List<SessionHistoryItem>();

            using (var connection = _databaseService.GetConnection())
            {
                await connection.OpenAsync();

                string query = @"
                    SELECT
                        Id,
                        OwnerUsername,
                        Role,
                        RemoteUsername,
                        RemoteHostName,
                        RemoteIp,
                        StartTime,
                        EndTime,
                        DurationSeconds,
                        Status,
                        Note,
                        CreatedAt
                    FROM SessionHistory
                    WHERE OwnerUsername = @OwnerUsername
                    ORDER BY StartTime DESC
                    LIMIT @Limit;
                ";

                using (var cmd = new MySqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@OwnerUsername", ownerUsername);
                    cmd.Parameters.AddWithValue("@Limit", limit);

                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            SessionHistoryItem item = new SessionHistoryItem
                            {
                                Id = reader.GetInt32("Id"),
                                OwnerUsername = reader.GetString("OwnerUsername"),
                                Role = reader.GetString("Role"),
                                RemoteUsername = reader["RemoteUsername"]?.ToString() ?? string.Empty,
                                RemoteHostName = reader["RemoteHostName"]?.ToString() ?? string.Empty,
                                RemoteIp = reader["RemoteIp"]?.ToString() ?? string.Empty,
                                StartTime = reader.GetDateTime("StartTime"),
                                EndTime = reader["EndTime"] == DBNull.Value ? null : reader.GetDateTime("EndTime"),
                                DurationSeconds = reader.GetInt32("DurationSeconds"),
                                Status = reader.GetString("Status"),
                                Note = reader["Note"]?.ToString() ?? string.Empty,
                                CreatedAt = reader.GetDateTime("CreatedAt")
                            };

                            result.Add(item);
                        }
                    }
                }
            }

            return result;
        }
    }
}