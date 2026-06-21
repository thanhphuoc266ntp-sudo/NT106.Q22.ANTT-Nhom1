using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using RemoteMate.Models;
using RemoteMate.Services;
using System.IO;
using WpfOpenFileDialog = Microsoft.Win32.OpenFileDialog;

using WpfKey = System.Windows.Input.Key;
using WpfKeyEventArgs = System.Windows.Input.KeyEventArgs;
using WpfBrushes = System.Windows.Media.Brushes;
using WpfColor = System.Windows.Media.Color;
using WpfSolidColorBrush = System.Windows.Media.SolidColorBrush;
using WpfHorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;

namespace RemoteMate.Views
{
    public partial class ChatWindow : Window
    {
        private readonly string _remoteIp;
        private readonly string _remoteName;
        private readonly ChatClientService _chatClientService;

        public ChatWindow(string remoteIp, string remoteName)
        {
            InitializeComponent();

            _remoteIp = remoteIp;
            _remoteName = remoteName;
            _chatClientService = new ChatClientService();

            txtRemoteInfo.Text = $"Đang chat với: {_remoteName} ({_remoteIp})";
        }

        private async void btnSend_Click(object sender, RoutedEventArgs e)
        {
            await SendCurrentMessageAsync();
        }

        private async void btnAttachFile_Click(object sender, RoutedEventArgs e)
        {
            WpfOpenFileDialog dialog = new WpfOpenFileDialog
            {
                Title = "Chọn file để gửi",
                Multiselect = false
            };

            if (dialog.ShowDialog() != true)
                return;

            string filePath = dialog.FileName;
            string fileName = Path.GetFileName(filePath);

            bool sent = await _chatClientService.SendFileAsync(_remoteIp, filePath);

            if (sent)
            {
                ChatMessage fileMessage = new ChatMessage
                {
                    FromIp = UserSession.IpAddress ?? string.Empty,
                    FromUserName = UserSession.Username ?? "Bạn",
                    Message = $"Đã gửi file: {fileName}",
                    SentAt = DateTime.Now,
                    IsMine = true
                };

                AddMessageToUi(fileMessage);
            }
            else
            {
                MessageBox.Show(
                    "Không gửi được file. Kiểm tra máy nhận hoặc firewall port 9002.",
                    "Lỗi gửi file",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private async void txtMessage_KeyDown(object sender, WpfKeyEventArgs e)
        {
            if (e.Key == WpfKey.Enter)
            {
                e.Handled = true;
                await SendCurrentMessageAsync();
            }
        }

        private async Task SendCurrentMessageAsync()
        {
            string message = txtMessage.Text.Trim();

            if (string.IsNullOrWhiteSpace(message))
                return;

            if (string.IsNullOrWhiteSpace(_remoteIp))
            {
                MessageBox.Show("Chưa chọn thiết bị để chat!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool sent = await _chatClientService.SendMessageAsync(_remoteIp, message);

            if (sent)
            {
                ChatMessage myMessage = new ChatMessage
                {
                    FromIp = UserSession.IpAddress ?? string.Empty,
                    FromUserName = UserSession.Username ?? "Bạn",
                    Message = message,
                    SentAt = DateTime.Now,
                    IsMine = true
                };

                AddMessageToUi(myMessage);
                txtMessage.Clear();
                txtMessage.Focus();
            }
            else
            {
                MessageBox.Show(
                    "Không gửi được tin nhắn. Kiểm tra máy nhận hoặc firewall port 9002.",
                    "Lỗi gửi tin",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        public void AddIncomingMessage(ChatMessage chatMessage)
        {
            chatMessage.IsMine = false;

            Dispatcher.Invoke(() =>
            {
                AddMessageToUi(chatMessage);
            });
        }

        private void AddMessageToUi(ChatMessage chatMessage)
        {
            Border bubble = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 4, 0, 4),
                MaxWidth = 320,
                HorizontalAlignment = chatMessage.IsMine
                    ? WpfHorizontalAlignment.Right
                    : WpfHorizontalAlignment.Left,
                Background = chatMessage.IsMine
                    ? new WpfSolidColorBrush(WpfColor.FromRgb(0, 184, 148))
                    : new WpfSolidColorBrush(WpfColor.FromRgb(236, 240, 241))
            };

            StackPanel stack = new StackPanel();

            TextBlock header = new TextBlock
            {
                Text = chatMessage.IsMine
                    ? $"Bạn • {chatMessage.SentAt:HH:mm}"
                    : $"{chatMessage.FromUserName} • {chatMessage.SentAt:HH:mm}",
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Foreground = chatMessage.IsMine ? WpfBrushes.White : WpfBrushes.DimGray,
                Margin = new Thickness(0, 0, 0, 4)
            };

            TextBlock body = new TextBlock
            {
                Text = chatMessage.Message,
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                Foreground = chatMessage.IsMine ? WpfBrushes.White : WpfBrushes.Black
            };

            stack.Children.Add(header);
            stack.Children.Add(body);

            bubble.Child = stack;
            pnlMessages.Children.Add(bubble);

            scrollMessages.ScrollToEnd();
        }
    }
}