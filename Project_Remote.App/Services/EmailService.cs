using System;
using MailKit.Net.Smtp;
using MimeKit;

namespace RemoteMate.Services
{
    public class EmailService
    {
        // Thay bằng email và App Password của bạn (hoặc xin bạn của bạn)
        private readonly string _senderEmail = "remotemate.system@gmail.com";
        private readonly string _appPassword = "lmzexdthsizuqxua";

        public bool SendOTP(string targetEmail, string otpCode)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("RemoteMate System", _senderEmail));
                message.To.Add(new MailboxAddress("User", targetEmail));
                message.Subject = "Mã xác thực đăng ký RemoteMate";

                message.Body = new TextPart("plain")
                {
                    Text = $"Mã OTP đăng ký của bạn là: {otpCode}. Vui lòng không chia sẻ mã này cho bất kỳ ai!"
                };

                using (var client = new SmtpClient())
                {
                    // Kết nối tới server Gmail
                    client.Connect("smtp.gmail.com", 587, MailKit.Security.SecureSocketOptions.StartTls);

                    // Xác thực bằng Mật khẩu ứng dụng (App Password)
                    client.Authenticate(_senderEmail, _appPassword);

                    client.Send(message);
                    client.Disconnect(true);
                }
                return true; // Gửi thành công
            }
            catch (Exception ex)
            {
                Console.WriteLine("Lỗi gửi mail: " + ex.Message);
                return false; // Gửi thất bại
            }
        }
    }
}