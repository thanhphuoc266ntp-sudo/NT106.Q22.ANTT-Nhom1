using System;
using System.Windows;
using MySqlConnector;
using BCrypt.Net;
using RemoteMate.Services;
using MessageBox = System.Windows.MessageBox;

namespace RemoteMate
{
    public partial class ChangePasswordWindow : Window
    {
        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private void btnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            string oldPassword = txtOldPassword.Password;
            string newPassword = txtNewPassword.Password;
            string confirmPassword = txtConfirmPassword.Password;

            if (string.IsNullOrWhiteSpace(oldPassword) || string.IsNullOrWhiteSpace(newPassword) || string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword.Length < 6)
            {
                MessageBox.Show("Mật khẩu mới phải có ít nhất 6 ký tự!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (newPassword != confirmPassword)
            {
                MessageBox.Show("Mật khẩu xác nhận không khớp!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DatabaseService dbService = new DatabaseService();
                using (var connection = dbService.GetConnection())
                {
                    connection.Open();

                    string getPasswordQuery = "SELECT Password FROM Users WHERE Username = @username OR Email = @username";
                    string storedHash = "";

                    using (var cmd = new MySqlCommand(getPasswordQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@username", UserSession.Username);
                        var result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            storedHash = result.ToString();
                        }
                    }

                    if (!BCrypt.Net.BCrypt.Verify(oldPassword, storedHash))
                    {
                        MessageBox.Show("Mật khẩu cũ không chính xác!", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                    string updateQuery = "UPDATE Users SET Password = @newHash WHERE Username = @username OR Email = @username";

                    using (var cmd = new MySqlCommand(updateQuery, connection))
                    {
                        cmd.Parameters.AddWithValue("@newHash", newPasswordHash);
                        cmd.Parameters.AddWithValue("@username", UserSession.Username);
                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show("Đổi mật khẩu thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi: " + ex.Message, "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}