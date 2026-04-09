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
           
            string fullName = txtFullName.Text;
            string username = txtUser.Text;
            string email = txtEmail.Text;
            string password = txtPass.Password;
            string confirmPassword = txtConfirmPassword.Password;

            
            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Bạn vui lòng điền đầy đủ thông tin nhé!", "Thông báo");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Mật khẩu nhập lại không khớp kìa!", "Lỗi");
                return;
            }

           
            
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            
            Random rnd = new Random();
            string otpCode = rnd.Next(100000, 999999).ToString();

            
            EmailService emailService = new EmailService();
            bool isMailSent = emailService.SendOTP(email, otpCode);

            if (isMailSent)
            {
                MessageBox.Show($"Mã OTP đã được gửi tới {email}. Kiểm tra ngay nhé!", "Thành công");

                
                OTPWindow otpScreen = new OTPWindow(fullName, username, email, passwordHash, otpCode);
                otpScreen.Show();

                this.Close(); 
            }
            else
            {
                MessageBox.Show("Gửi mail thất bại rồi. Bạn kiểm tra lại App Password hoặc kết nối mạng nha!", "Lỗi");
            }

        }

        private void BackToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            this.Close();
        }
    }
}