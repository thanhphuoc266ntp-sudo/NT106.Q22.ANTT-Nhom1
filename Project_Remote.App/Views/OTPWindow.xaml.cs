using System.Windows;

namespace RemoteMate
{
    public partial class OTPWindow : Window
    {
        public OTPWindow()
        {
            InitializeComponent();
        }

        // Hàm xử lý nút XÁC NHẬN - Tên phải khớp 100% với XAML
        private void btnXacNhan_Click(object sender, RoutedEventArgs e)
        {
            if (txtOTP.Text.Length == 6)
            {
                MessageBox.Show("Xác thực thành công!");

                // Mở lại trang Login hoặc Dashboard tùy bạn
                LoginWindow login = new LoginWindow();
                login.Show();
                this.Close();
            }
            else
            {
                MessageBox.Show("Vui lòng nhập đủ 6 chữ số mã xác thực.");
            }
        }

        // Hàm xử lý nút Gửi lại mã - Tên phải khớp 100% với XAML
        private void btnResend_Click(object sender, RoutedEventArgs e)
        {
            // Sau này Phước sẽ viết code gửi mail ở đây
            MessageBox.Show("Mã xác thực mới đã được gửi vào Email của bạn!");
        }
    }
}