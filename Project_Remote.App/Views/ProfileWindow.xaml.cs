using System;
using System.Windows;
using MySqlConnector;
using RemoteMate.Services;
using MessageBox = System.Windows.MessageBox;

namespace RemoteMate
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();
            LoadProfileData();
        }

        private void LoadProfileData()
        {
            txtFullName.Text = UserSession.FullName ?? string.Empty;
            txtUsername.Text = UserSession.Username ?? string.Empty;
            txtEmail.Text = UserSession.Email ?? string.Empty;
        }

        private void btnUpdate_Click(object sender, RoutedEventArgs e)
        {
            string newFullName = txtFullName.Text.Trim();
            string newUsername = txtUsername.Text.Trim();

            if (string.IsNullOrWhiteSpace(newFullName))
            {
                MessageBox.Show("Họ và tên không được để trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(newUsername))
            {
                MessageBox.Show("Tên đăng nhập không được để trống!", "Thông báo", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                DatabaseService dbService = new DatabaseService();
                using (var connection = dbService.GetConnection())
                {
                    connection.Open();

                    if (newUsername != UserSession.Username)
                    {
                        string checkUserQuery = "SELECT COUNT(*) FROM Users WHERE Username = @newUsername";
                        using (var cmdCheck = new MySqlCommand(checkUserQuery, connection))
                        {
                            cmdCheck.Parameters.AddWithValue("@newUsername", newUsername);
                            if (Convert.ToInt32(cmdCheck.ExecuteScalar()) > 0)
                            {
                                MessageBox.Show("Tên đăng nhập này đã tồn tại trên hệ thống!", "Lỗi", MessageBoxButton.OK, MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }

                    string query = "UPDATE Users SET FullName = @fullName, Username = @newUsername WHERE Email = @email";
                    using (var cmd = new MySqlCommand(query, connection))
                    {
                        cmd.Parameters.AddWithValue("@fullName", newFullName);
                        cmd.Parameters.AddWithValue("@newUsername", newUsername);
                        cmd.Parameters.AddWithValue("@email", UserSession.Email);
                        cmd.ExecuteNonQuery();
                    }
                }

                UserSession.FullName = newFullName;
                UserSession.Username = newUsername;

                MessageBox.Show("Cập nhật hồ sơ thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Lỗi cập nhật: " + ex.Message, "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}