using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MySqlConnector;
using BCrypt.Net; // Bắt buộc cho InfoSec
using RemoteMate.Services;

namespace RemoteMate
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Xử lý ẩn/hiện chữ mờ ô Username (Giữ nguyên của Phước)
        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (placeholderUser == null) return;
            placeholderUser.Visibility = string.IsNullOrEmpty(txtUsername.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Xử lý ẩn/hiện chữ mờ ô Password (Giữ nguyên của Phước)
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (placeholderPass == null) return;
            placeholderPass.Visibility = string.IsNullOrEmpty(txtPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        // HÀM QUAN TRỌNG: XỬ LÝ ĐĂNG NHẬP
        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            // 1. Lấy thông tin nhập vào
            string username = txtUsername.Text;
            string password = txtPassword.Password;

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 2. Mở kết nối Database lên Clever Cloud
                DatabaseService dbService = new DatabaseService();
                using (var connection = dbService.GetConnection())
                {
                    connection.Open();

                    // 3. Truy vấn tìm Hash Password dựa vào Username
                    // Giả sử bảng của Phước tên là 'Users', nếu khác thì đổi lại nhé
                    string query = "SELECT Password FROM Users WHERE Username = @user";

                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@user", username);
                        var result = cmd.ExecuteScalar();

                        if (result != null) // Nếu tìm thấy user
                        {
                            string storedHash = result.ToString();

                            // 4. BẢO MẬT INFOSEC: Kiểm tra chữ thô với mã băm
                            bool isPasswordCorrect = BCrypt.Net.BCrypt.Verify(password, storedHash);

                            if (isPasswordCorrect)
                            {
                                MessageBox.Show("Đăng nhập thành công!", "Thành công");

                                // 5. Vào thẳng Dashboard (MainWindow)
                                MainWindow main = new MainWindow();
                                main.Show();
                                this.Close(); // Đóng trang đăng nhập
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

        // Mở trang Đăng ký (Giữ nguyên của Phước)
        private void MoTrangDangKy_Click(object sender, MouseButtonEventArgs e)
        {
            RegisterWindow reg = new RegisterWindow();
            reg.Show();
            this.Close();
        }

        // Mở trang Quên mật khẩu (Giữ nguyên của Phước)
        private void QuenMatKhau_Click(object sender, MouseButtonEventArgs e)
        {
            ForgotPasswordWindow forgot = new ForgotPasswordWindow();
            forgot.Show();
            this.Close();
        }
    }
}