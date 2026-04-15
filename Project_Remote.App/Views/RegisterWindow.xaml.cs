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
            string username = txtUser.Text;
            string email = txtEmail.Text;
            string password = txtPass.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) ||
                string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Vui lòng điền đầy đủ thông tin!", "Thông báo");
                return;
            }

            if (password != confirmPassword)
            {
                MessageBox.Show("Mật khẩu nhập lại không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DatabaseService dbService = new DatabaseService();
                using (var connection = dbService.GetConnection())
                {
                    connection.Open();
                    string checkQuery = "SELECT COUNT(*) FROM Users WHERE Email = @email OR Username = @user";

                    using (var cmd = new MySqlCommand(checkQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@email", email);
                        cmd.Parameters.AddWithValue("@user", username);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count > 0)
                        {
                            MessageBox.Show("Email hoặc Tên đăng nhập này đã được đăng ký! Vui lòng sử dụng thông tin khác.", "Tài khoản đã tồn tại", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối kiểm tra dữ liệu: " + ex.Message, "Lỗi Server");
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
                MessageBox.Show("Gửi mail thất bại, vui lòng thử lại!", "Lỗi");
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