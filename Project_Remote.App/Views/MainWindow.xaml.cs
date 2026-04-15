using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using RemoteMate.Services;

namespace RemoteMate
{
    public partial class MainWindow : Window
    {
        private bool _isMaximizedMode = false;
        private TcpServerService _serverService;
        private TcpClientService _clientService;
        private string _remoteIp;

        public MainWindow()
        {
            InitializeComponent();
            LoadUserInfo();
            StartServer();
        }

        private void LoadUserInfo()
        {
            if (!string.IsNullOrEmpty(UserSession.FullName))
            {
                txtProfileName.Text = UserSession.FullName;
                txtProfileEmail.Text = UserSession.Email;
            }
        }

        private void StartServer()
        {
            _serverService = new TcpServerService();
            _serverService.OnStatusChanged += (msg) =>
            {
                Dispatcher.Invoke(() => txtConnectionStatus.Text = $"Trạng thái: {msg}");
            };
            _serverService.Start();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            _serverService?.Stop();
            _clientService?.Disconnect();
            UserSession.Clear();
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }

        private void profileDot_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed)
            {
                ContextMenu contextMenu = new ContextMenu();

                MenuItem profileItem = new MenuItem { Header = "Hồ sơ" };
                profileItem.Click += MenuProfile_Click;

                MenuItem changePwdItem = new MenuItem { Header = "Đổi mật khẩu" };
                changePwdItem.Click += MenuChangePassword_Click;

                contextMenu.Items.Add(profileItem);
                contextMenu.Items.Add(changePwdItem);

                contextMenu.PlacementTarget = sender as UIElement;
                contextMenu.IsOpen = true;
            }
        }

        private void MenuProfile_Click(object sender, RoutedEventArgs e)
        {
            ProfileWindow profile = new ProfileWindow();
            profile.ShowDialog();
            LoadUserInfo();
        }

        private void MenuChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordWindow changePwd = new ChangePasswordWindow();
            changePwd.ShowDialog();
        }

        private void borderScreenArea_MouseEnter(object sender, MouseEventArgs e)
        {
            btnMaximize.Visibility = Visibility.Visible;
        }

        private void borderScreenArea_MouseLeave(object sender, MouseEventArgs e)
        {
            btnMaximize.Visibility = Visibility.Collapsed;
        }

        private void btnMaximize_Click(object sender, RoutedEventArgs e)
        {
            if (!_isMaximizedMode)
            {
                colSidebar.Width = new GridLength(0);
                rowHeader.Height = new GridLength(0);
                rowToolbar.Height = new GridLength(0);
                rowFooter.Height = new GridLength(0);

                borderScreenArea.Margin = new Thickness(0);
                borderScreenArea.CornerRadius = new CornerRadius(0);
                borderScreenArea.BorderThickness = new Thickness(0);

                _isMaximizedMode = true;
            }
            else
            {
                colSidebar.Width = new GridLength(320);
                rowHeader.Height = GridLength.Auto;
                rowToolbar.Height = GridLength.Auto;
                rowFooter.Height = GridLength.Auto;

                borderScreenArea.Margin = new Thickness(20, 10, 20, 10);
                borderScreenArea.CornerRadius = new CornerRadius(15);
                borderScreenArea.BorderThickness = new Thickness(1);

                _isMaximizedMode = false;
            }
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            lstClients.Items.Clear();
            lstClients.Items.Add("192.168.1.107");
        }

        private void lstClients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstClients.SelectedItem != null)
            {
                string selected = lstClients.SelectedItem.ToString();
                _remoteIp = selected.Split(' ')[0];
                txtRemoteMachine.Text = _remoteIp;
            }
        }

        private void BtnControlMode_Checked(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_remoteIp))
            {
                MessageBox.Show("Vui lòng chọn một thiết bị từ danh sách trước!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                btnControlMode.IsChecked = false;
                return;
            }

            ConnectToRemote();
        }

        private async void ConnectToRemote()
        {
            btnControlMode.IsEnabled = false;
            txtConnectionStatus.Text = $"Đang kết nối đến {_remoteIp}...";
            ledConnection.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(241, 196, 15));

            _clientService = new TcpClientService();

            _clientService.OnScreenReceived += (imageData) =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        using (MemoryStream ms = new MemoryStream(imageData))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();

                            imgRemoteScreen.Source = bitmap;
                            imgRemoteScreen.Stretch = System.Windows.Media.Stretch.Uniform;
                            imgRemoteScreen.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
                            imgRemoteScreen.VerticalAlignment = System.Windows.VerticalAlignment.Stretch;
                        }
                        imgRemoteScreen.Visibility = Visibility.Visible;
                        pnlPlaceholder.Visibility = Visibility.Collapsed;
                    }
                    catch { }
                });
            };

            _clientService.OnStatusChanged += (msg) =>
            {
                Dispatcher.Invoke(() =>
                {
                    txtConnectionStatus.Text = msg;
                    if (msg.Contains("Connected"))
                    {
                        ledConnection.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(46, 204, 113));
                        btnDisconnect.Visibility = Visibility.Visible;
                    }
                });
            };

            _clientService.OnDisconnected += () =>
            {
                Dispatcher.Invoke(() =>
                {
                    ledConnection.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
                    txtConnectionStatus.Text = "Đã ngắt kết nối";
                    btnDisconnect.Visibility = Visibility.Collapsed;
                    btnControlMode.IsEnabled = true;
                    btnControlMode.IsChecked = false;
                    imgRemoteScreen.Visibility = Visibility.Collapsed;
                    pnlPlaceholder.Visibility = Visibility.Visible;
                });
            };

            bool connected = await _clientService.ConnectAsync(_remoteIp);

            if (!connected)
            {
                btnControlMode.IsEnabled = true;
                btnControlMode.IsChecked = false;
                ledConnection.Fill = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(231, 76, 60));
            }
        }

        private void BtnControlMode_Unchecked(object sender, RoutedEventArgs e)
        {
            _clientService?.Disconnect();
        }

        private void BtnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _clientService?.Disconnect();
            btnControlMode.IsChecked = false;
        }

        private void BtnWakeOnLan_Click(object sender, RoutedEventArgs e) { }
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