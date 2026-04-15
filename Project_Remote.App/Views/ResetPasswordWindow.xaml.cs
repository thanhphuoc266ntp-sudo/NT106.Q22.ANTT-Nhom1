using System;
using System.Windows;
using MySqlConnector;
using BCrypt.Net;
using RemoteMate.Services;
using MessageBox = System.Windows.MessageBox;

namespace RemoteMate
{
    public partial class ResetPasswordWindow : Window
    {
        private string _email;
        private string _sentOTP;

        public ResetPasswordWindow(string email, string otp)
        {
            InitializeComponent();
            _email = email;
            _sentOTP = otp;
        }

        private void btnVerifyOTP_Click(object sender, RoutedEventArgs e)
        {
            if (txtOTP.Text.Trim() == _sentOTP)
            {
                stepVerifyOTP.Visibility = Visibility.Collapsed;
                stepNewPassword.Visibility = Visibility.Visible;
            }
            else
            {
                MessageBox.Show("Mã xác thực không chính xác!", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnResend_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Random rnd = new Random();
                _sentOTP = rnd.Next(100000, 999999).ToString();

                EmailService emailService = new EmailService();
                if (emailService.SendOTP(_email, _sentOTP))
                {
                    MessageBox.Show("Mã xác thực mới đã được gửi vào Email của bạn!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("Không thể gửi lại mã. Vui lòng thử lại sau!", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi hệ thống");
            }
        }

        private void btnUpdatePassword_Click(object sender, RoutedEventArgs e)
        {
            string newPass = txtNewPassword.Password;
            string confirmPass = txtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(newPass) || newPass.Length < 6)
            {
                MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPass != confirmPass)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi dữ liệu", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(newPass);
                DatabaseService dbService = new DatabaseService();

                using (var connection = dbService.GetConnection())
                {
                    connection.Open();
                    string query = "UPDATE Users SET Password = @hash WHERE LOWER(Email) = @email";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@hash", passwordHash);
                        cmd.Parameters.AddWithValue("@email", _email.ToLower());
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Mật khẩu đã được cập nhật thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật mật khẩu: " + ex.Message, "Lỗi Server", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}