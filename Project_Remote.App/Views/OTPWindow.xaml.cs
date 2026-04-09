using System;
using System.Windows;
using MySqlConnector; 
using RemoteMate.Services; 

namespace RemoteMate
{
    public partial class OTPWindow : Window
    {
        private string _fullName, _username, _email, _passwordHash, _otpSent;

        public OTPWindow(string fullName, string username, string email, string passwordHash, string otpCode)
        {
            InitializeComponent();
            _fullName = fullName;
            _username = username;
            _email = email;
            _passwordHash = passwordHash;
            _otpSent = otpCode;
        }

       
        private void btnXacNhan_Click(object sender, RoutedEventArgs e)
        {
            
            if (txtOTP.Text == _otpSent)
            {
                try
                {
                    
                    DatabaseService dbService = new DatabaseService();
                    using (var connection = dbService.GetConnection())
                    {
                        connection.Open();

                       
                        string query = "INSERT INTO Users (FullName, Username, Email, Password) VALUES (@full, @user, @email, @hash)";

                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@full", _fullName);
                            cmd.Parameters.AddWithValue("@user", _username);
                            cmd.Parameters.AddWithValue("@email", _email);
                            cmd.Parameters.AddWithValue("@hash", _passwordHash); 

                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Đăng ký tài khoản thành công! Chào mừng bạn đến với RemoteMate.", "Thành công");

                    
                    LoginWindow login = new LoginWindow();
                    login.Show();
                    this.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Lỗi lưu Database: " + ex.Message, "Lỗi kết nối");
                }
            }
            else
            {
                MessageBox.Show("Mã OTP không chính xác. Bạn vui lòng kiểm tra kỹ lại trong mail nhé!", "Sai mã");
            }
        }

        
        private void btnResend_Click(object sender, RoutedEventArgs e)
        {
          
            Random rnd = new Random();
            _otpSent = rnd.Next(100000, 999999).ToString();

            
            EmailService emailService = new EmailService();
            if (emailService.SendOTP(_email, _otpSent))
            {
                MessageBox.Show("Mã xác thực mới đã được gửi lại vào Email của bạn!");
            }
            else
            {
                MessageBox.Show("Gửi lại mã thất bại. Kiểm tra kết nối mạng!");
            }
        }
    }
}