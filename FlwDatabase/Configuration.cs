using System.Data;
using System.Data.SqlClient;
using InstagramModels;
using System.Configuration;

namespace FlwDatabase
{
    public class Configuration
    {
        private Config InstantiateConfig(SqlDataReader reader)
        {
            if (reader.HasRows && reader.Read())
            {
                return new Config(reader["vchConfig"].ToString(),
                                  reader["vchValue"].ToString());
            }
            else
                return null;
        }

        public static string connectionString = @"Server=localhost\sqlexpress;Data Source=ASKHAT;Initial Catalog=rmtdtbs;Integrated Security=True;Connect Timeout=15;Encrypt=False;TrustServerCertificate=True;ApplicationIntent=ReadWrite;MultiSubnetFailover=False";

        public Configuration(string conf)
        {
            connectionString = conf;
        }

        public Config GetConfig(string name, int profileId)
        {
            Config result = null;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetConfig", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("Conf", SqlDbType.VarChar).Value = name;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                result = InstantiateConfig(reader);
            }

            return result;
        }
    }
}
