using System;
using System.Windows;
using MySqlConnector;
using RemoteMate.Services;
using MessageBox = System.Windows.MessageBox;

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
            string email = txtResetEmail.Text.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(email))
            {
                MessageBox.Show("Vui lòng nhập địa chỉ Email đã đăng ký!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                DatabaseService dbService = new DatabaseService();
                using (var connection = dbService.GetConnection())
                {
                    connection.Open();

                    string checkEmailQuery = "SELECT COUNT(*) FROM Users WHERE LOWER(Email) = @email";
                    using (var cmdCheck = new MySqlCommand(checkEmailQuery, connection))
                    {
                        cmdCheck.Parameters.AddWithValue("@email", email);
                        if (Convert.ToInt32(cmdCheck.ExecuteScalar()) == 0)
                        {
                            MessageBox.Show("Địa chỉ Email này không tồn tại trên hệ thống!", "Lỗi xác thực", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    Random rnd = new Random();
                    string otpCode = rnd.Next(100000, 999999).ToString();

                    EmailService emailService = new EmailService();
                    if (emailService.SendOTP(email, otpCode))
                    {
                        MessageBox.Show("Mã xác thực đã được gửi vào Email của bạn!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);

                        ResetPasswordWindow resetWindow = new ResetPasswordWindow(email, otpCode);
                        resetWindow.Show();
                        this.Close();
                    }
                    else
                    {
                        MessageBox.Show("Không thể gửi mã xác thực. Vui lòng thử lại sau!", "Lỗi hệ thống", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi hệ thống: " + ex.Message, "Lỗi Server", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BackToLogin_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            LoginWindow login = new LoginWindow();
            login.Show();
            this.Close();
        }
    }
}