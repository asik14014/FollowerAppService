using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using InstagramModels;
using System.Configuration;

namespace FlwDatabase
{
    public static class InstagramLogins
    {
        private static Profile InstantiateProfile(SqlDataReader reader)
        {
            if (reader.HasRows && reader.Read())
            {
                var profile = new Profile(Convert.ToInt32(reader["intId"].ToString()),
                                                   reader["vchLogin"].ToString(),
                                                   reader["vchPassword"].ToString(),
                                                   reader["vchProfileName"].ToString(),
                                                   reader["vchProfiles"].ToString());

                return profile;
            }
            else
                return null;
        }

        private static Profile InstantiateProfile2(SqlDataReader reader)
        {
            var profile = new Profile(Convert.ToInt32(reader["intId"].ToString()),
                                               reader["vchLogin"].ToString(),
                                               reader["vchPassword"].ToString(),
                                               reader["vchProfileName"].ToString(),
                                               reader["vchProfiles"].ToString());

            return profile;
        }

        private static Metrics InstantiateMetrica(SqlDataReader reader, Profile pf)
        {
            var metrica = new Metrics(Convert.ToInt32(reader["id"].ToString()),
                                      pf,
                                      Convert.ToInt32(reader["intFollowers"].ToString()),
                                      Convert.ToInt32(reader["intFollowing"].ToString()),
                                      Convert.ToDateTime(reader["dtSaved"].ToString()));

            return metrica;
        }

        private static User InstantiateRecord(SqlDataReader reader)
        {
            if (reader.HasRows && reader.Read())
            {
                return new User(reader["login"].ToString(),
                                Convert.ToBoolean(reader["isBusiness"].ToString()),
                                Convert.ToBoolean(reader["isDeleted"].ToString()),
                                Convert.ToBoolean(reader["isFollowRequested"].ToString()),
                                Convert.ToDateTime(reader["dtRequest"].ToString()),
                                Convert.ToBoolean(reader["isUnfollowed"].ToString()),
                                Convert.ToDateTime(reader["dtUfollowed"].ToString()),
                                Convert.ToDateTime(reader["dtSaved"].ToString()));
            }
            else
                return null;
        }
        
        public static string connectionString
        {
            get
            {
                return ConfigurationManager.AppSettings["connectionString"];
            }
        }

        public static bool IsFollowedUser(string login, int profileId)
        {
            int count = 0;

            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.IsFollowedUser", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("Login", SqlDbType.VarChar).Value = login;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                object a = command.ExecuteScalar();
                if (a != null)
                    count = (int)a;
            }

            return count > 0;
        }
        
        public static void SaveLogin(string login, int profileId)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.SaveLogin", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("Login", SqlDbType.VarChar).Value = login;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public static void UnfollowLogin(string login, int profileId, bool updateDateTime = false)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.SetUnfollowed", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("Login", SqlDbType.VarChar).Value = login;
                command.Parameters.Add("unfollowed", SqlDbType.Bit).Value = true;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;
                command.Parameters.Add("updatedt", SqlDbType.Bit).Value = updateDateTime;

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public static void FollowLogin(string login, int profileId)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.UserFollowed", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("Login", SqlDbType.VarChar).Value = login;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public static void BusinessLogin(string login, int profileId)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.UserIsBusinessProfile", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("Login", SqlDbType.VarChar).Value = login;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public static void DeletedLogin(string login, int profileId)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.UserDeleted", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("Login", SqlDbType.VarChar).Value = login;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public static void UpdateLoginStillRequested(string login, int profileId)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.UpdateRequested", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("Login", SqlDbType.VarChar).Value = login;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public static User GetUser(string url, int profileId)
        {
            User result = null;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetUser", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("Login", SqlDbType.VarChar).Value = url;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                result = InstantiateRecord(reader);
            }

            return result;
        }

        public static List<string> GetUsers(int profileId, int count, bool isFollowed, bool isBuisness = false)
        {
            var result = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetUsers", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("NumberOfResultsToReturn", SqlDbType.Int).Value = count;
                command.Parameters.Add("isFollowed", SqlDbType.Bit).Value = isFollowed;
                command.Parameters.Add("isBusiness", SqlDbType.Bit).Value = isBuisness;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(reader["login"].ToString());
                }
            }

            return result;
        }

        public static List<string> GetUnfollowedLogins(int profileId, int count)
        {
            var result = new List<string>();
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetLoginsToUnfollow", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("NumberOfResultsToReturn", SqlDbType.Int).Value = count;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    result.Add(reader["login"].ToString());
                }
            }

            return result;
        }

        public static Profile GetProfile(int id)
        {
            Profile result = null;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetProfileRecord", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("id", SqlDbType.VarChar).Value = id;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                result = InstantiateProfile(reader);
            }

            return result;
        }

        public static List<Profile> GetProfiles()
        {
            var result = new List<Profile>();
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetProfiles", conn))
            {
                command.CommandType = CommandType.StoredProcedure;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    Profile prof = InstantiateProfile2(reader);
                    result.Add(prof);
                }
            }

            return result;
        }

        public static void SaveMetrics(int profileId, int followers, int following)
        {
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.SaveMetrica", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;
                command.Parameters.Add("followers", SqlDbType.Int).Value = followers;
                command.Parameters.Add("following", SqlDbType.Int).Value = following;

                conn.Open();
                command.ExecuteNonQuery();
            }
        }

        public static IList<Metrics> GetMetrics(int profileId)
        {
            var profile = GetProfile(profileId);

            var result = new List<Metrics>();

            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetMetrica", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();

                while (reader.Read())
                {
                    var m = InstantiateMetrica(reader, profile);
                    result.Add(m);
                }
            }

            return result;
        }

        public static int GetNumberPossibleRequests(int profileId)
        {
            int result = 0;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetNumberOfFollowLogins", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("profileId", SqlDbType.VarChar).Value = profileId;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows && reader.Read())
                {
                    result = Convert.ToInt32(reader["answer"].ToString());
                }
            }

            return result;
        }

        public static int GetNumberPossibleUnfollows(int profileId)
        {
            int result = 0;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetNumberOfUnfollowLogins", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("profileId", SqlDbType.VarChar).Value = profileId;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows && reader.Read())
                {
                    result = Convert.ToInt32(reader["answer"].ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Достать количество запросов на подписку за последние n часов
        /// </summary>
        /// <param name="profileId">id профайла</param>
        /// <param name="hours">количество часов</param>
        /// <returns></returns>
        public static int GetNOFR(int profileId, int hours)
        {
            int result = 0;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetNOFR", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;
                command.Parameters.Add("hours", SqlDbType.Int).Value = hours;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows && reader.Read())
                {
                    result = Convert.ToInt32(reader["answer"].ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Достать количество запросов на отписку за последние n часов
        /// </summary>
        /// <param name="profileId">id профайла</param>
        /// <param name="hours">количество часов</param>
        /// <returns></returns>
        public static int GetNOUR(int profileId, int hours)
        {
            int result = 0;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetNOUR", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;
                command.Parameters.Add("hours", SqlDbType.Int).Value = hours;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows && reader.Read())
                {
                    result = Convert.ToInt32(reader["answer"].ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Достать количество логинов доступных на подписку
        /// </summary>
        /// <param name="profileId">id профайла</param>
        /// <param name="hours">количество часов</param>
        /// <returns></returns>
        public static int GetFreeLogins(int profileId)
        {
            int result = 0;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetFreeLogins", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows && reader.Read())
                {
                    result = Convert.ToInt32(reader["answer"].ToString());
                }
            }

            return result;
        }

        /// <summary>
        /// Достать количество сохраненных логинов за последние n часов
        /// </summary>
        /// <param name="profileId">id профайла</param>
        /// <param name="hours">количество часов</param>
        /// <returns></returns>
        public static int GetSavedLogins(int profileId, int hours)
        {
            int result = 0;
            using (var conn = new SqlConnection(connectionString))
            using (var command = new SqlCommand("dbo.GetSavedLogins", conn))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.Add("profileId", SqlDbType.Int).Value = profileId;
                command.Parameters.Add("hours", SqlDbType.Int).Value = hours;

                conn.Open();
                SqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows && reader.Read())
                {
                    result = Convert.ToInt32(reader["answer"].ToString());
                }
            }

            return result;
        }
    }
}
