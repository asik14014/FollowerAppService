using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure;
using InstagramSelenium;
using NLog;
using FlwDatabase;
using InstagramModels;
using Configuration = FlwDatabase.Configuration;

namespace InstaWebJob
{
    public class Functions
    {
        #region Private
        private static Profile profile = InstagramLogins.GetProfile(ProfileId);
        private static Logger _logger;
        private static Random probability = new Random();

        private static int ProfileId
        {
            get
            {
                var id = 0;
                int.TryParse(CloudConfigurationManager.GetSetting("ProfileId"), out id);
                return id;
            }
        }

        private static int IntervalMIN
        {
            get
            {
                var config = Configuration.GetConfig("TimerIntervalMin", ProfileId);
                return config != null ? config.IntValue : 10;
            }
        }

        private static int IntervalMAX
        {
            get
            {
                var config = Configuration.GetConfig("TimerIntervalMax", ProfileId);
                return config != null ? config.IntValue : 50;
            }
        }

        private static int TimerInterval
        {
            get
            {
                var config = Configuration.GetConfig("TimerInterval", ProfileId);
                return config != null ? config.IntValue : 1;
            }
        }

        private static int MaxFollows
        {
            get
            {
                var config = Configuration.GetConfig("MaxFollows", ProfileId);
                return config != null ? config.IntValue : 300;
            }
        }

        private static int MinFollows
        {
            get
            {
                var config = Configuration.GetConfig("MinFollows", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private static int PageDownCount
        {
            get
            {
                var config = Configuration.GetConfig("PageDown", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private static int MaxPostLikes
        {
            get
            {
                var config = Configuration.GetConfig("MaxPostToLike", ProfileId);
                return config != null ? config.IntValue : 300;
            }
        }

        private static int MinPostLikes
        {
            get
            {
                var config = Configuration.GetConfig("MinPostToLike", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private static int UnfollowUsersMin
        {
            get
            {
                var config = Configuration.GetConfig("MinUnfollows", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private static int UnfollowUsersMax
        {
            get
            {
                var config = Configuration.GetConfig("MaxUnfollows", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private static int RandomFollow
        {
            get
            {
                var config = Configuration.GetConfig("RandomFollowValue", ProfileId);
                return config != null ? config.IntValue : 50;
            }
        }

        private static int RandomUnfollow
        {
            get
            {
                var config = Configuration.GetConfig("RandomUnfollowValue", ProfileId);
                return config != null ? config.IntValue : 30;
            }
        }

        private static int RandomSave
        {
            get
            {
                var config = Configuration.GetConfig("RandomSaveUsersValue", ProfileId);
                return config != null ? config.IntValue : 30;
            }
        }

        private static int RandomLike
        {
            get
            {
                var config = Configuration.GetConfig("RandomLikeScrollValue", ProfileId);
                return config != null ? config.IntValue : 30;
            }
        }

        private static string ConnectionString
        {
            get
            {
                return CloudConfigurationManager.GetSetting("connectionString");
            }
        }
        #endregion

        public static void TimerJob([TimerTrigger("00:10:00")] TimerInfo timer)
        {
            var prob = probability.Next(0, RandomFollow + RandomUnfollow + RandomLike + RandomSave);

            if (prob < RandomFollow)
            {
                Instagram.Follow_FromDB(probability.Next(MinFollows, MaxFollows));
            }
            else if (prob < RandomFollow + RandomUnfollow)
            {
                Instagram.Unfollow_PageDown(probability.Next(UnfollowUsersMin, UnfollowUsersMax));
            }
            else if (prob < RandomFollow + RandomUnfollow + RandomSave)
            {
                //Сохранить подписчиков
                if (profile.ProfileList != null && profile.ProfileList.Any())
                {
                    Instagram.Open(profile.ProfileList.ElementAt(probability.Next(0, profile.ProfileList.Count)));
                    Instagram.SaveFollowers_PageDown(PageDownCount);
                }
            }
            else
            {
                Instagram.Like_ScrollDown(probability.Next(MinPostLikes, MaxPostLikes));
            }
        }
    }
}
