using System;
using MySqlConnector;

namespace RemoteMate.Services
{
    public class DatabaseService
    {
        private readonly string _connectionString = "Server=bp87lbgecbxpwsd1oa3k-mysql.services.clever-cloud.com;Port=3306;Database=bp87lbgecbxpwsd1oa3k;Uid=u88yk4ojnlqxxpwg;Pwd=7Fn7H4y8YqxjmsBLkZ3h;";

        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
    }
}