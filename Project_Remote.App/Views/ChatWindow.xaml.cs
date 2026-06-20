using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using RemoteMate.Models;
using RemoteMate.Services;

namespace RemoteMate.Views
{
    public partial class ChatWindow : Window
    {
        private readonly ChatClientService _chatClient = new ChatClientService();
        private string _remoteIp = string.Empty;

        public ChatWindow()
        {
            InitializeComponent();
        }

        public ChatWindow(string remoteIp) : this()
        {
            _remoteIp = remoteIp ?? string.Empty;
            txtRemoteInfo.Text = $"Đang chat với: {_remoteIp}";
            _chatClient.OnStatusChanged += (s) =>
            {
                Dispatcher.Invoke(() =>
                {
                    AppendSystemMessage(s);
                });
            };
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendMessageFromUiAsync();
        }

        private async void txtMessage_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await SendMessageFromUiAsync();
            }
        }

        private async Task SendMessageFromUiAsync()
        {
            string message = txtMessage.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(_remoteIp))
            {
                System.Windows.MessageBox.Show("Chưa chọn máy nhận tin nhắn.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(message))
            {
                System.Windows.MessageBox.Show("Tin nhắn rỗng.", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                AppendOwnMessage(message);
                txtMessage.Clear();
                await _chatClient.SendMessageAsync(_remoteIp, message);
            }
            catch (Exception ex)
            {
                AppendSystemMessage($"Lỗi gửi: {ex.Message}");
            }
        }

        public void AppendIncomingMessage(ChatMessage msg)
        {
            Dispatcher.Invoke(() =>
            {
                var tb = new System.Windows.Controls.TextBlock
                {
                    Text = $"{msg.FromUserName}: {msg.Message}",
                    TextWrapping = System.Windows.TextWrapping.Wrap,
                    Margin = new Thickness(6),
                    Background = System.Windows.Media.Brushes.LightBlue,
                    Padding = new Thickness(8),
                    HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                    MaxWidth = 300
                };
                pnlMessages.Children.Add(tb);
                ScrollToBottom();
            });
        }

        private void AppendOwnMessage(string text)
        {
            var tb = new System.Windows.Controls.TextBlock
            {
                Text = $"Bạn: {text}",
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new Thickness(6),
                Background = System.Windows.Media.Brushes.LightGreen,
                Padding = new Thickness(8),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Right,
                MaxWidth = 300
            };
            pnlMessages.Children.Add(tb);
            ScrollToBottom();
        }

        private void AppendSystemMessage(string text)
        {
            var tb = new System.Windows.Controls.TextBlock
            {
                Text = text,
                TextWrapping = System.Windows.TextWrapping.Wrap,
                Margin = new Thickness(6),
                Foreground = System.Windows.Media.Brushes.Gray,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center
            };
            pnlMessages.Children.Add(tb);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            try
            {
                scrollMessages.ScrollToEnd();
            }
            catch { }
        }

        public void SetRemote(string remoteIp)
        {
            _remoteIp = remoteIp ?? string.Empty;
            txtRemoteInfo.Text = $"Đang chat với: {_remoteIp}";
        }
    }
}