using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Input;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;
using RemoteMate.Services;
using RemoteMate.Views;
using RemoteMate.Models;

namespace RemoteMate
{
    public partial class MainWindow : Window
    {
        private bool _isMaximizedMode = false;
        private TcpServerService _serverService;
        private TcpClientService _clientService;
        private NetworkService _networkService;
        private string _remoteIp = string.Empty;
        private ChatServerService _chatServerService;
        private ChatWindow _chatWindow;
        private DateTime _lastMouseMoveSent = DateTime.MinValue;
        private SessionHistoryService _sessionHistoryService = new SessionHistoryService();
        private DateTime? _currentControllerSessionStart;
        private string _currentControllerRemoteUsername = string.Empty;
        private string _currentControllerRemoteHostName = string.Empty;
        private DateTime? _currentReceiverSessionStart;
        private ControlRequest _currentReceiverRequest;

        public MainWindow()
        {
            InitializeComponent();
            LoadUserInfo();
            UserSession.InitNetworkInfo();

            StartServer();
            StartChatServer();
            StartNetworkDiscovery();

            _ = LoadRecentSessionHistoryAsync();
        }

        private void StartChatServer()
        {
            _chatServerService = new ChatServerService();

            _chatServerService.OnMessageReceived += (chatMessage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (_chatWindow == null || !_chatWindow.IsVisible)
                    {
                        _chatWindow = new ChatWindow(chatMessage.FromIp, chatMessage.FromUserName);
                        _chatWindow.Owner = this;
                        _chatWindow.Closed += (s, e) => _chatWindow = null;
                        _chatWindow.Show();
                    }

                    _chatWindow.AddIncomingMessage(chatMessage);
                    _chatWindow.Activate();
                });
            };

            _chatServerService.Start();
        }

        private void LoadUserInfo()
        {
            if (!string.IsNullOrEmpty(UserSession.FullName))
            {
                txtProfileName.Text = UserSession.FullName ?? string.Empty;
                txtProfileEmail.Text = UserSession.Email ?? string.Empty;
            }
        }

        private async Task LoadRecentSessionHistoryAsync()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UserSession.Username))
                    return;

                var historyItems = await _sessionHistoryService.GetRecentSessionsAsync(UserSession.Username, 10);

                Dispatcher.Invoke(() =>
                {
                    lstHistory.Items.Clear();

                    if (historyItems.Count == 0)
                    {
                        lstHistory.Items.Add("Chưa có phiên điều khiển nào");
                        return;
                    }

                    foreach (var item in historyItems)
                    {
                        lstHistory.Items.Add(item.DisplayText);
                    }
                });
            }
            catch
            {
                Dispatcher.Invoke(() =>
                {
                    lstHistory.Items.Clear();
                    lstHistory.Items.Add("Không tải được lịch sử phiên");
                });
            }
        }

        private async Task SaveControllerSessionAsync(string status, string note)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UserSession.Username))
                    return;

                DateTime startTime = _currentControllerSessionStart ?? DateTime.Now;
                DateTime endTime = DateTime.Now;

                int durationSeconds = Math.Max(0, (int)(endTime - startTime).TotalSeconds);

                SessionHistoryItem item = new SessionHistoryItem
                {
                    OwnerUsername = UserSession.Username ?? string.Empty,
                    Role = "Controller",
                    RemoteUsername = _currentControllerRemoteUsername,
                    RemoteHostName = _currentControllerRemoteHostName,
                    RemoteIp = _remoteIp,
                    StartTime = startTime,
                    EndTime = endTime,
                    DurationSeconds = durationSeconds,
                    Status = status,
                    Note = note
                };

                await _sessionHistoryService.AddSessionAsync(item);

                _currentControllerSessionStart = null;

                await LoadRecentSessionHistoryAsync();
            }
            catch
            {
          
            }
        }

        private async Task SaveReceiverSessionAsync(ControlRequest req, string status, string note)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(UserSession.Username))
                    return;

                DateTime startTime = _currentReceiverSessionStart ?? DateTime.Now;
                DateTime endTime = DateTime.Now;

                int durationSeconds = Math.Max(0, (int)(endTime - startTime).TotalSeconds);

                SessionHistoryItem item = new SessionHistoryItem
                {
                    OwnerUsername = UserSession.Username ?? string.Empty,
                    Role = "Receiver",
                    RemoteUsername = req.UserName,
                    RemoteHostName = req.HostName,
                    RemoteIp = req.FromIp,
                    StartTime = startTime,
                    EndTime = endTime,
                    DurationSeconds = durationSeconds,
                    Status = status,
                    Note = note
                };

                await _sessionHistoryService.AddSessionAsync(item);

                _currentReceiverSessionStart = null;
                _currentReceiverRequest = null;

                await LoadRecentSessionHistoryAsync();
            }
            catch
            {
               
            }
        }

        private string ExtractClientInfoFromText(string text)
        {
            int start = text.IndexOf('(');
            int end = text.IndexOf(')');

            if (start >= 0 && end > start)
            {
                return text.Substring(start + 1, end - start - 1).Trim();
            }

            return string.Empty;
        }

        private string ExtractUserNameFromClientText(string text)
        {
            string info = ExtractClientInfoFromText(text);

            int slashIndex = info.IndexOf('/');

            if (slashIndex >= 0)
            {
                return info.Substring(0, slashIndex).Trim();
            }

            return string.Empty;
        }

        private string ExtractHostNameFromClientText(string text)
        {
            string info = ExtractClientInfoFromText(text);

            int slashIndex = info.IndexOf('/');

            if (slashIndex >= 0)
            {
                return info.Substring(slashIndex + 1).Trim();
            }

            return info;
        }

        private void StartServer()
        {
            _serverService = new TcpServerService();

            _serverService.OnStatusChanged += (msg) =>
            {
                Dispatcher.Invoke(() => txtConnectionStatus.Text = $"Trạng thái: {msg}");
            };

            _serverService.OnControlRequest += async (req) =>
            {
                bool result = false;

                await Dispatcher.InvokeAsync(() =>
                {
                    string info = $"{req.UserName} ({req.FromIp}) muốn điều khiển máy của bạn";
                    var win = new ConfirmWindow(info);
                    win.Title = "Yêu cầu điều khiển";
                    result = win.ShowDialog() == true;
                });

                return result;
            };

            _serverService.OnSessionAccepted += (req) =>
            {
                _currentReceiverSessionStart = DateTime.Now;
                _currentReceiverRequest = req;
            };

            _serverService.OnSessionRejected += (req) =>
            {
                _ = SaveReceiverSessionAsync(
                    req,
                    "Rejected",
                    "Người dùng từ chối yêu cầu điều khiển"
                );
            };

            _serverService.OnSessionEnded += (req) =>
            {
                _ = SaveReceiverSessionAsync(
                    req,
                    "Completed",
                    "Phiên điều khiển kết thúc"
                );
            };

            _serverService.Start();
        }

        private void StartNetworkDiscovery()
        {
            _networkService = new NetworkService();

            _networkService.OnClientFound += (client) =>
            {
                if (client.Ip == UserSession.IpAddress)
                    return;

                if (client.HostName == UserSession.HostName)
                    return;

                if (client.Ip == "127.0.0.1")
                    return;

                Dispatcher.Invoke(() =>
                {
                    string remoteName = !string.IsNullOrWhiteSpace(client.UserName)
                        ? $"{client.UserName} / {client.HostName}"
                        : client.HostName;

                    string item = $"{client.Ip} ({remoteName})";

                    for (int i = 0; i < lstClients.Items.Count; i++)
                    {
                        string existing = lstClients.Items[i]?.ToString() ?? string.Empty;

                        if (existing.StartsWith(client.Ip + " "))
                        {
                            lstClients.Items[i] = item;
                            return;
                        }
                    }

                    lstClients.Items.Add(item);
                });
            };

            _networkService.StartDiscovery();
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

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Bạn có chắc muốn đăng xuất?",
                "Đăng xuất",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _clientService?.Disconnect();
                _serverService?.Stop();
                _chatServerService?.Stop();
                _networkService?.Stop();

                UserSession.Clear();

                var loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
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
            lstClients.Items.Clear(); // 🔥 đúng chuẩn
        }

        private void lstClients_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstClients.SelectedItem != null)
            {
                string selected = lstClients.SelectedItem.ToString() ?? string.Empty;
                _remoteIp = selected.Split(' ')[0];
                _currentControllerRemoteUsername = ExtractUserNameFromClientText(selected);
                _currentControllerRemoteHostName = ExtractHostNameFromClientText(selected);
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
                _ = SaveControllerSessionAsync("Completed", "Người dùng ngắt kết nối");

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

            if (connected)
            {
                _currentControllerSessionStart = DateTime.Now;
            }
            else
            {
                await SaveControllerSessionAsync("Failed", "Kết nối thất bại hoặc bị từ chối");

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

        private void BtnFileTransfer_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_remoteIp))
            {
                MessageBox.Show(
                    "Vui lòng chọn một thiết bị trước khi mở chat!",
                    "Thông báo",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
                return;
            }

            string remoteName = _remoteIp;

            if (lstClients.SelectedItem != null)
            {
                remoteName = lstClients.SelectedItem.ToString() ?? _remoteIp;
            }

            if (_chatWindow == null || !_chatWindow.IsVisible)
            {
                _chatWindow = new ChatWindow(_remoteIp, remoteName);
                _chatWindow.Owner = this;
                _chatWindow.Closed += (s, args) => _chatWindow = null;
                _chatWindow.Show();
            }
            else
            {
                _chatWindow.Activate();
            }
        }
        private void BtnScreenshot_Click(object sender, RoutedEventArgs e) { }
        private void BtnLockScreen_Click(object sender, RoutedEventArgs e) { }
        private void BtnShutdown_Click(object sender, RoutedEventArgs e) { }
        private bool IsRemoteInputReady()
        {
            return _clientService != null &&
                   btnControlMode.IsChecked == true &&
                   imgRemoteScreen.Source is BitmapSource;
        }

        private bool TryGetRemotePoint(MouseEventArgs e, out int remoteX, out int remoteY)
        {
            remoteX = 0;
            remoteY = 0;

            if (imgRemoteScreen.Source is not BitmapSource bitmap)
                return false;

            double imageWidth = bitmap.PixelWidth;
            double imageHeight = bitmap.PixelHeight;

            double controlWidth = imgRemoteScreen.ActualWidth;
            double controlHeight = imgRemoteScreen.ActualHeight;

            if (imageWidth <= 0 || imageHeight <= 0 || controlWidth <= 0 || controlHeight <= 0)
                return false;

            System.Windows.Point pos = e.GetPosition(imgRemoteScreen);

            double scale = Math.Min(controlWidth / imageWidth, controlHeight / imageHeight);

            double renderedWidth = imageWidth * scale;
            double renderedHeight = imageHeight * scale;

            double offsetX = (controlWidth - renderedWidth) / 2;
            double offsetY = (controlHeight - renderedHeight) / 2;

            double x = (pos.X - offsetX) / scale;
            double y = (pos.Y - offsetY) / scale;

            if (x < 0 || y < 0 || x >= imageWidth || y >= imageHeight)
                return false;

            remoteX = Math.Clamp((int)Math.Round(x), 0, bitmap.PixelWidth - 1);
            remoteY = Math.Clamp((int)Math.Round(y), 0, bitmap.PixelHeight - 1);

            return true;
        }

        private string GetMouseButtonName(MouseButton button)
        {
            return button switch
            {
                MouseButton.Left => "Left",
                MouseButton.Right => "Right",
                MouseButton.Middle => "Middle",
                _ => "Left"
            };
        }

        private async void ImgRemoteScreen_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsRemoteInputReady())
                return;

            if ((DateTime.Now - _lastMouseMoveSent).TotalMilliseconds < 15)
                return;

            if (TryGetRemotePoint(e, out int x, out int y))
            {
                _lastMouseMoveSent = DateTime.Now;
                await _clientService.SendMouseMoveAsync(x, y);
            }
        }

        private async void ImgRemoteScreen_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!IsRemoteInputReady())
                return;

            imgRemoteScreen.Focus();
            imgRemoteScreen.CaptureMouse();

            if (TryGetRemotePoint(e, out int x, out int y))
            {
                string button = GetMouseButtonName(e.ChangedButton);
                await _clientService.SendMouseDownAsync(button, x, y);
                e.Handled = true;
            }
        }

        private async void ImgRemoteScreen_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!IsRemoteInputReady())
                return;

            imgRemoteScreen.ReleaseMouseCapture();

            if (TryGetRemotePoint(e, out int x, out int y))
            {
                string button = GetMouseButtonName(e.ChangedButton);
                await _clientService.SendMouseUpAsync(button, x, y);
                e.Handled = true;
            }
        }

        private async void ImgRemoteScreen_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (!IsRemoteInputReady())
                return;

            if (TryGetRemotePoint(e, out int x, out int y))
            {
                await _clientService.SendMouseWheelAsync(e.Delta, x, y);
                e.Handled = true;
            }
        }

        private async void Window_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!IsRemoteInputReady())
                return;

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);

            if (virtualKey > 0)
            {
                await _clientService.SendKeyDownAsync(virtualKey);
                e.Handled = true;
            }
        }

        private async void Window_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!IsRemoteInputReady())
                return;

            Key key = e.Key == Key.System ? e.SystemKey : e.Key;
            int virtualKey = KeyInterop.VirtualKeyFromKey(key);

            if (virtualKey > 0)
            {
                await _clientService.SendKeyUpAsync(virtualKey);
                e.Handled = true;
            }
        }
    }
}