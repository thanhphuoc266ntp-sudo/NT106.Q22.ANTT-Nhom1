using System;
using System.Windows;
using MySqlConnector; // Thư viện kết nối MySQL
using RemoteMate.Services; // Để dùng DatabaseService và EmailService

namespace RemoteMate
{
    public partial class OTPWindow : Window
    {
        // Các biến tạm để giữ dữ liệu chờ xác thực xong mới lưu vào DB
        private string _fullName, _username, _email, _passwordHash, _otpSent;

        // SỬA LẠI HÀM NÀY: Để nhận dữ liệu từ RegisterWindow gửi sang
        public OTPWindow(string fullName, string username, string email, string passwordHash, string otpCode)
        {
            InitializeComponent();
            _fullName = fullName;
            _username = username;
            _email = email;
            _passwordHash = passwordHash;
            _otpSent = otpCode;
        }

        // Hàm xử lý nút XÁC NHẬN
        private void btnXacNhan_Click(object sender, RoutedEventArgs e)
        {
            // 1. Kiểm tra mã OTP người dùng nhập có khớp với mã đã gửi không
            if (txtOTP.Text == _otpSent)
            {
                try
                {
                    // 2. KẾT NỐI DATABASE VÀ LƯU USER (Dân InfoSec lưu PasswordHash nhé)
                    DatabaseService dbService = new DatabaseService();
                    using (var connection = dbService.GetConnection())
                    {
                        connection.Open();

                        // Câu lệnh SQL để chèn User mới vào bảng (Giả sử bảng của Phước tên là Users)
                        string query = "INSERT INTO Users (FullName, Username, Email, Password) VALUES (@full, @user, @email, @hash)";

                        using (var cmd = new MySqlCommand(query, connection))
                        {
                            cmd.Parameters.AddWithValue("@full", _fullName);
                            cmd.Parameters.AddWithValue("@user", _username);
                            cmd.Parameters.AddWithValue("@email", _email);
                            cmd.Parameters.AddWithValue("@hash", _passwordHash); // Vẫn lưu mã băm nhưng tên cột trong DB là Password

                            cmd.ExecuteNonQuery();
                        }
                    }

                    MessageBox.Show("Đăng ký tài khoản thành công! Chào mừng Phước đến với RemoteMate.", "Thành công");

                    // 3. Về trang Login
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
                MessageBox.Show("Mã OTP không chính xác. Phước kiểm tra kỹ lại trong mail nhé!", "Sai mã");
            }
        }

        // Hàm xử lý nút Gửi lại mã
        private void btnResend_Click(object sender, RoutedEventArgs e)
        {
            // Tạo lại mã mới
            Random rnd = new Random();
            _otpSent = rnd.Next(100000, 999999).ToString();

            // Gửi lại qua EmailService
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