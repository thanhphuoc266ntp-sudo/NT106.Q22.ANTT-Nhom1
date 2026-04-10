using System.Windows;
using System.Windows.Input;

namespace RemoteMate
{
    public partial class ForgotPasswordWindow : Window
    {
        public ForgotPasswordWindow()
        {
            InitializeComponent();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtResetEmail.Text))
            {
              
                MessageBox.Show("Yêu cầu đã được gửi! Vui lòng kiểm tra Email để nhận mật khẩu mới.", "Thông báo");

                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Vui lòng nhập Email của bạn!");
            }
        }

        private void BackToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}