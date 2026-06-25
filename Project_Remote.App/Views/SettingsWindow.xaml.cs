using Microsoft.Win32;
using RemoteMate.Services;
using System.Windows;
using System.Windows.Controls;
using MessageBox = System.Windows.MessageBox;

namespace RemoteMate.Views
{
    public partial class SettingsWindow : Window
    {
        private bool _isLoading = true;

        public SettingsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AppSettingsService.Load();

            if (Owner is MainWindow main)
            {
                tglAudio.IsChecked = main.AudioEnabled;
                tglClipboard.IsChecked = main.ClipboardSyncEnabled;
            }

            SetQualityCombo(AppSettingsService.Current.ScreenQuality);

            _isLoading = false;
        }

        private void SetQualityCombo(int quality)
        {
            for (int i = 0; i < cmbQuality.Items.Count; i++)
            {
                if (cmbQuality.Items[i] is ComboBoxItem item &&
                    item.Content?.ToString() == quality.ToString())
                {
                    cmbQuality.SelectedIndex = i;
                    return;
                }
            }

            cmbQuality.SelectedIndex = 5;
        }

        private void BtnProfile_Click(object sender, RoutedEventArgs e)
        {
            ProfileWindow profile = new ProfileWindow();
            profile.Owner = Owner;
            profile.ShowDialog();

            if (Owner is MainWindow main)
                main.RefreshUserInfoFromSettings();
        }

        private void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            ChangePasswordWindow changePwd = new ChangePasswordWindow();
            changePwd.Owner = Owner;
            changePwd.ShowDialog();
        }

        private void BtnReceivedFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = SelectFolder("Chọn thư mục nhận tệp");

            if (!string.IsNullOrWhiteSpace(folder))
            {
                AppSettingsService.Current.ReceivedFileFolder = folder;
                AppSettingsService.Save();

                MessageBox.Show(
                    $"Thư mục nhận tệp:\n{folder}",
                    "Cài đặt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private void BtnScreenshotFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = SelectFolder("Chọn thư mục lưu ảnh chụp");

            if (!string.IsNullOrWhiteSpace(folder))
            {
                AppSettingsService.Current.ScreenshotFolder = folder;
                AppSettingsService.Save();

                MessageBox.Show(
                    $"Thư mục lưu ảnh chụp:\n{folder}",
                    "Cài đặt",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
        }

        private string SelectFolder(string title)
        {
            OpenFolderDialog dialog = new OpenFolderDialog();
            dialog.Title = title;

            if (dialog.ShowDialog() == true)
                return dialog.FolderName;

            return string.Empty;
        }

        private void cmbQuality_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading)
                return;

            if (cmbQuality.SelectedItem is ComboBoxItem item &&
                int.TryParse(item.Content?.ToString(), out int quality))
            {
                AppSettingsService.Current.ScreenQuality = quality;
                AppSettingsService.Save();

                if (Owner is MainWindow main)
                    _ = main.SendRemoteScreenQualityAsync(quality);
            }
        }

        private void ToggleAudio_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            if (Owner is MainWindow main)
                main.AudioEnabled = tglAudio.IsChecked == true;
        }

        private void ToggleClipboard_Changed(object sender, RoutedEventArgs e)
        {
            if (_isLoading)
                return;

            if (Owner is MainWindow main)
                main.ClipboardSyncEnabled = tglClipboard.IsChecked == true;
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            if (Owner is MainWindow main)
            {
                Close();
                main.LogoutFromSettings();
            }
        }
    }
}