using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RemoteMate
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Xử lý ẩn/hiện chữ mờ ô Username
        private void txtUsername_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (placeholderUser == null) return;
            placeholderUser.Visibility = string.IsNullOrEmpty(txtUsername.Text) ? Visibility.Visible : Visibility.Collapsed;
        }

        // Xử lý ẩn/hiện chữ mờ ô Password
        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (placeholderPass == null) return;
            placeholderPass.Visibility = string.IsNullOrEmpty(txtPassword.Password) ? Visibility.Visible : Visibility.Collapsed;
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            // Vào thẳng Dashboard
            MainWindow main = new MainWindow();
            main.Show();
            this.Close();
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