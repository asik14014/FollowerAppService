using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Net;
using System.IO;

namespace Instagram.api
{
    public static class Users
    {
        #region Private GET URLs
        private static string OwnerInformationURL = "https://api.instagram.com/v1/users/self/?access_token={0}";
        private static string UserInformationURL = "https://api.instagram.com/v1/users/{0}/?access_token={1}";//0 - userid
        private static string OwnerRecentMediaURL = "https://api.instagram.com/v1/users/self/media/recent/?access_token={0}";
        private static string UserRecentMediaURL = "https://api.instagram.com/v1/users/{0}/media/recent/?access_token={1}";

        private static string MediaLikedByOwnerURL =
            "https://api.instagram.com/v1/users/self/media/liked?access_token={0}";

        private static string SearchUsersURL = "https://api.instagram.com/v1/users/search?q={0}&access_token={1}";
        #endregion

        private static string GetRequest(string url)
        {
            using (var webClient = new WebClient())
            {
                // Выполняем запрос по адресу и получаем ответ в виде строки
                var response = webClient.DownloadString(url);
                return response;
            }
            return "";
        }

        public static string GetOwnerInformation(string access_token)
        {
            return GetRequest(string.Format(OwnerInformationURL, access_token));
        }

        public static string GetUserInformation(string userId, string access_token)
        {
            return GetRequest(string.Format(UserInformationURL, userId, access_token));
        }

        public static string GetOwnerRecentMedia(string access_token)
        {
            return GetRequest(string.Format(OwnerRecentMediaURL, access_token));
        }

        public static string GetUserRecentMedia(string userId, string access_token)
        {
            return GetRequest(string.Format(UserRecentMediaURL, userId, access_token));
        }

        public static string GetMediaLikedByOwner(string access_token)
        {
            return GetRequest(string.Format(MediaLikedByOwnerURL, access_token));
        }

        public static string GetSearchUsers(string parameter, string access_token)
        {
            return GetRequest(string.Format(SearchUsersURL, parameter, access_token));
        }
    }
}
