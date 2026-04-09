using System.Windows;
using System.Windows.Input;

namespace RemoteMate
{
    public partial class RegisterWindow : Window
    {
        public RegisterWindow()
        {
            InitializeComponent();
        }

        // ĐÂY LÀ HÀM QUAN TRỌNG NHẤT - Tên phải khớp với XAML
        private void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            // 1. Sau này Phước sẽ viết code gửi Mail OTP ở đây

            // 2. Mở bảng nhập OTP
            OTPWindow otpScreen = new OTPWindow();
            otpScreen.Show();

            // 3. Đóng bảng Đăng ký lại
            this.Close();
        }

        // Quay lại trang Login
        private void BackToLogin_Click(object sender, MouseButtonEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}