using System;
using System.Windows;
using System.Windows.Input;

namespace RemoteMate
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            LoadUserInfo();
        }

        private void LoadUserInfo()
        {
            if (!string.IsNullOrEmpty(UserSession.FullName))
            {
                txtProfileName.Text = UserSession.FullName;
                txtProfileEmail.Text = UserSession.Email;
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            UserSession.Clear();

            LoginWindow login = new LoginWindow();
            login.Show();

            this.Close();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đang làm mới danh sách thiết bị...", "Thông báo");
        }

        private void BtnWakeOnLan_Click(object sender, RoutedEventArgs e) { }
        private void BtnDisconnect_Click(object sender, RoutedEventArgs e) { }
        private void BtnControlMode_Checked(object sender, RoutedEventArgs e) { }
        private void BtnControlMode_Unchecked(object sender, RoutedEventArgs e) { }
        private void BtnFileTransfer_Click(object sender, RoutedEventArgs e) { }
        private void BtnScreenshot_Click(object sender, RoutedEventArgs e) { }
        private void BtnLockScreen_Click(object sender, RoutedEventArgs e) { }
        private void BtnShutdown_Click(object sender, RoutedEventArgs e) { }

        private void ImgRemoteScreen_MouseMove(object sender, MouseEventArgs e) { }
        private void ImgRemoteScreen_MouseDown(object sender, MouseButtonEventArgs e) { }
        private void ImgRemoteScreen_MouseUp(object sender, MouseButtonEventArgs e) { }
        private void ImgRemoteScreen_MouseWheel(object sender, MouseWheelEventArgs e) { }
    }
}