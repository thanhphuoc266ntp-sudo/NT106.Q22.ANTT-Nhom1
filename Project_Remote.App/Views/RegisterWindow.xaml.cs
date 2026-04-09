using System;
using System.Windows;
using System.Windows.Input;
using BCrypt.Net;
using RemoteMate.Services;

namespace RemoteMate
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            // 1. LẤY DỮ LIỆU TỪ GIAO DIỆN
            string fullName = txtFullName.Text;
            string username = txtUser.Text;
            string email = txtEmail.Text;
            string password = txtPass.Password;
            string confirmPassword = txtConfirmPassword.Password;

            // 2. KIỂM TRA ĐẦU VÀO (Validation)
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Phước ơi, bạn cần điền đầy đủ thông tin nhé!", "Thông báo");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Mật khẩu nhập lại không khớp kìa!", "Lỗi");
                return;
            }

            // 3. XỬ LÝ BẢO MẬT (Dân InfoSec)
            // Băm mật khẩu bằng BCrypt
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            // 4. TẠO MÃ OTP 6 SỐ
            Random rnd = new Random();
            string otpCode = rnd.Next(100000, 999999).ToString();

            // 5. GỬI MAIL OTP
            EmailService emailService = new EmailService();
            bool isMailSent = emailService.SendOTP(email, otpCode);

            if (isMailSent)
            {
                MessageBox.Show($"Mã OTP đã được gửi tới {email}. Kiểm tra ngay nhé!", "Thành công");

                // 6. CHUYỂN SANG BẢNG NHẬP OTP 
                // Truyền dữ liệu sang cửa sổ OTP để lát nữa lưu vào DB
                // Lưu ý: Đảm bảo OTPWindow của Phước đã sửa Constructor để nhận 4 tham số này
                // Sửa dòng này:
                OTPWindow otpScreen = new OTPWindow(fullName, username, email, passwordHash, otpCode);
                otpScreen.Show();

                this.Close(); // Đóng bảng Đăng ký
            }
            else
            {
                MessageBox.Show("Gửi mail thất bại rồi. Bạn kiểm tra lại App Password hoặc kết nối mạng nha!", "Lỗi");
            }

            // ĐÃ XÓA PHẦN CODE THỪA Ở ĐÂY ĐỂ TRÁNH MỞ BẢNG OTP 2 LẦN
        }

        // Quay lại trang Login
        private void BackToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            // Giả sử bạn có LoginWindow, nếu chưa có hãy tạm comment dòng dưới lại
            // LoginWindow login = new LoginWindow();
            // login.Show();
            this.Close();
        }
    }
}