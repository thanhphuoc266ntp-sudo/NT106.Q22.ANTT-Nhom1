using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySqlConnector;
using BCrypt.Net; 
using RemoteMate.Services;

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
            
            string username = txtUsername.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                
                DatabaseService dbService = new DatabaseService();
                using (var connection = dbService.GetConnection())
                {
                    connection.Open();

                    
                    string query = "SELECT Password FROM Users WHERE Username = @user";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@user", username);
                        var result = cmd.ExecuteScalar();

                        if (result != null) 
                        {
                            string storedHash = result.ToString();

                           
                            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, storedHash);

                            if (isPasswordCorrect)
                            {
                                MessageBox.Show("Đăng nhập thành công!", "Thành công");

                                
                                MainWindow main = new MainWindow();
                                main.Show();
                                this.Close(); 
                            }
                            else
                            {
                                MessageBox.Show("Mật khẩu không chính xác!", "Lỗi bảo mật", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        }
                        else
                        {
                            MessageBox.Show("Tài khoản không tồn tại!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi kết nối cơ sở dữ liệu: " + ex.Message, "Lỗi Server", MessageBoxButton.OK, MessageBoxImage.Error);
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