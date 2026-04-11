using System;
using System.Windows;
using MySqlConnector;
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
            string username = txtUser.Text.Trim();
            string email = txtEmail.Text.Trim().ToLower();
            string password = txtPass.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Vui lòng điền đầy đủ các thông tin đăng ký!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp. Vui lòng kiểm tra lại!", "Lỗi đăng ký", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (password.Length < 6)
            {
                MessageBox.Show("Mật khẩu phải có ít nhất 6 ký tự!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DatabaseService dbService = new DatabaseService();
                using (var connection = dbService.GetConnection())
                {
                    connection.Open();

                    string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = @user";
                    using (var cmdUser = new MySqlCommand(checkUserQuery, connection))
                    {
                        cmdUser.Parameters.AddWithValue("@user", username);
                        if (Convert.ToInt32(cmdUser.ExecuteScalar()) > 0)
                        {
                            MessageBox.Show("Tên đăng nhập này đã tồn tại trên hệ thống!", "Lỗi đăng ký", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }

                    string checkEmailQuery = "SELECT COUNT(*) FROM Users WHERE Email = @email";
                    using (var cmdEmail = new MySqlCommand(checkEmailQuery, connection))
                    {
                        cmdEmail.Parameters.AddWithValue("@email", email);
                        if (Convert.ToInt32(cmdEmail.ExecuteScalar()) > 0)
                        {
                            MessageBox.Show("Email này đã được sử dụng để đăng ký tài khoản khác!", "Lỗi đăng ký", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống khi kiểm tra dữ liệu: " + ex.Message, "Lỗi kết nối", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
            Random rnd = new Random();
            string otpCode = rnd.Next(100000, 999999).ToString();

            EmailService emailService = new EmailService();
            bool isMailSent = emailService.SendOTP(email, otpCode);

            if (isMailSent)
            {
                MessageBox.Show($"Mã xác thực đã được gửi đến {email}. Vui lòng kiểm tra hộp thư để tiếp tục!", "Gửi mã thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                OTPWindow otpScreen = new OTPWindow(fullName, username, email, passwordHash, otpCode);
                otpScreen.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Hệ thống không thể gửi mã xác nhận. Vui lòng kiểm tra lại kết nối mạng!", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}