using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySqlConnector;
using BCrypt.Net;
using RemoteMate.Services;
using MessageBox = System.Windows.MessageBox;

namespace RemoteMate
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (placeholderUser == null) return;
            placeholderUser.Visibility = string.IsNullOrEmpty(txtUsername.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (placeholderPass == null) return;
            placeholderPass.Visibility = string.IsNullOrEmpty(txtPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string userInput = txtUsername.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrEmpty(userInput) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ thông tin!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DatabaseService dbService = new DatabaseService();
                using (var connection = dbService.GetConnection())
                {
                    connection.Open();
                    string query = "SELECT FullName, Username, Email, Password FROM Users WHERE Username = @input OR Email = @input LIMIT 1";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@input", userInput);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                string storedHash = reader["Password"].ToString();
                                if (BCrypt.Net.BCrypt.Verify(password, storedHash))
                                {
                                    UserSession.FullName = reader["FullName"].ToString();
                                    UserSession.Username = reader["Username"].ToString(); 
                                    UserSession.Email = reader["Email"].ToString();

                                    MainWindow main = new MainWindow();
                                    main.Show();
                                    this.Close();
                                }
                                else { MessageBox.Show("Mật khẩu không chính xác!"); }
                            }
                            else { MessageBox.Show("Tài khoản không tồn tại!"); }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối: " + ex.Message);
            }
        }

        private void MoTrangDangKy_Click(object sender, MouseButtonEventArgs e)
        {
            RegisterWindow reg = new RegisterWindow();
            reg.Show();
            this.Close();
        }

        private void QuenMatKhau_Click(object sender, MouseButtonEventArgs e)
        {
            ForgotPasswordWindow forgot = new ForgotPasswordWindow();
            forgot.Show();
            this.Close();
        }
    }
}