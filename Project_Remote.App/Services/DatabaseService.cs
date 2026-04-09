using System;
using MySqlConnector;

namespace RemoteMate.Services
{
    public class DatabaseService
    {
        // Chuỗi kết nối lấy từ Clever Cloud
        private readonly string _connectionString = "Server=bp87lbgecbxpwsd1oa3k-mysql.services.clever-cloud.com;Port=3306;Database=bp87lbgecbxpwsd1oa3k;Uid=u88yk4ojnlqxxpwg;Pwd=7Fn7H4y8YqxjmsBLkZ3h;";

        // Phương thức tạo và trả về một đối tượng kết nối
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}