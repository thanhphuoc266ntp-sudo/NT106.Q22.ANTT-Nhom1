using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RemoteMate
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        // Các hàm xử lý sự kiện để không bị lỗi giao diện
        private void BtnLogout_Click(object sender, RoutedEventArgs e) { this.Close(); }
        private void BtnRefresh_Click(object sender, RoutedEventArgs e) { }
        private void BtnWakeOnLan_Click(object sender, RoutedEventArgs e) { }
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e) { }
        private void BtnControlMode_Checked(object sender, RoutedEventArgs e) { }
        private void BtnControlMode_Unchecked(object sender, RoutedEventArgs e) { }
        private void BtnCtrlAltDel_Click(object sender, RoutedEventArgs e) { }
        private void BtnLockScreen_Click(object sender, RoutedEventArgs e) { }
        private void BtnFileTransfer_Click(object sender, RoutedEventArgs e) { }
        private void BtnScreenshot_Click(object sender, RoutedEventArgs e) { }
        private void BtnShutdown_Click(object sender, RoutedEventArgs e) { }

        // Các hàm xử lý chuột trên màn hình Remote
        private void ImgRemoteScreen_MouseMove(object sender, MouseEventArgs e) { }
        private void ImgRemoteScreen_MouseDown(object sender, MouseButtonEventArgs e) { }
        private void ImgRemoteScreen_MouseUp(object sender, MouseButtonEventArgs e) { }
        private void ImgRemoteScreen_MouseWheel(object sender, MouseWheelEventArgs e) { }
    }
}