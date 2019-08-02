using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.ServiceProcess;
using InstagramSelenium;
using NLog;
using FlwDatabase;
using InstagramModels;
using Configuration = FlwDatabase.Configuration;

namespace FlwService
{
    public partial class Service1 : ServiceBase
    {
        #region Private
        private static Logger _logger;
        private System.Timers.Timer timer;
        private Random probability = new Random();
        private static Configuration configuration;
        private static bool FLAG;

        private static int ProfileId
        {
            get
            {
                var id = 0;
                int.TryParse(ConfigurationManager.AppSettings["ProfileId"], out id);
                return id;
            }
        }

        private static Profile profile
        {
            get
            {
                var temp = InstagramLogins.GetProfile(ProfileId);
                var words = configuration.GetConfig("KeyWords", ProfileId);
                var locations = configuration.GetConfig("KeyLocations", ProfileId);
                
                temp.SearchWords = (words != null && !string.IsNullOrEmpty(words.Value)) ? words.Value.Split(';').ToList() : new List<string>();
                temp.Locations = (locations != null && !string.IsNullOrEmpty(locations.Value)) ? locations.Value.Split(';').ToList() : new List<string>();
                return temp;
            }
        }

        private int IntervalMIN
        {
            get
            {
                var config = configuration.GetConfig("TimerIntervalMin", ProfileId);
                return config != null ? config.IntValue : 10;
            }
        }

        private int IntervalMAX
        {
            get
            {
                var config = configuration.GetConfig("TimerIntervalMax", ProfileId);
                return config != null ? config.IntValue : 50;
            }
        }

        private int TimerInterval
        {
            get
            {
                var config = configuration.GetConfig("TimerInterval", ProfileId);
                return config != null ? config.IntValue : 1;
            }
        }

        private int MaxFollows
        {
            get
            {
                var config = configuration.GetConfig("MaxFollows", ProfileId);
                return config != null ? config.IntValue : 300;
            }
        }

        private int MinFollows
        {
            get
            {
                var config = configuration.GetConfig("MinFollows", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private int PageDownCount
        {
            get
            {
                var config = configuration.GetConfig("PageDown", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private int MaxPostLikes
        {
            get
            {
                var config = configuration.GetConfig("MaxPostToLike", ProfileId);
                return config != null ? config.IntValue : 300;
            }
        }

        private int MinPostLikes
        {
            get
            {
                var config = configuration.GetConfig("MinPostToLike", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private int UnfollowUsersMin
        {
            get
            {
                var config = configuration.GetConfig("MinUnfollows", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private int UnfollowUsersMax
        {
            get
            {
                var config = configuration.GetConfig("MaxUnfollows", ProfileId);
                return config != null ? config.IntValue : 100;
            }
        }

        private int RandomFollow
        {
            get
            {
                var config = configuration.GetConfig("RandomFollowValue", ProfileId);
                return config != null ? config.IntValue : 50;
            }
        }

        private int RandomUnfollow
        {
            get
            {
                var config = configuration.GetConfig("RandomUnfollowValue", ProfileId);
                return config != null ? config.IntValue : 30;
            }
        }

        private int RandomSave
        {
            get
            {
                var config = configuration.GetConfig("RandomSaveUsersValue", ProfileId);
                return config != null ? config.IntValue : 30;
            }
        }

        private int RandomLike
        {
            get
            {
                var config = configuration.GetConfig("RandomLikeScrollValue", ProfileId);
                return config != null ? config.IntValue : 30;
            }
        }

        private int RandomSearchByHashTag
        {
            get
            {
                var config = configuration.GetConfig("RandomHashtag", ProfileId);
                return config != null ? config.IntValue : 0;
            }
        }

        private int RandomSearchByLocation
        {
            get
            {
                var config = configuration.GetConfig("RandomLocation", ProfileId);
                return config != null ? config.IntValue : 0;
            }
        }

        private static string ConnectionString
        {
            get
            {
                return ConfigurationManager.AppSettings["connectionString"];
            }
        }

        private static string WebDriverString
        {
            get
            {
                return ConfigurationManager.AppSettings["WebDriver"].ToLower();
            }
        }

        private bool InitBrowser()
        {
            try
            {
                Instagram.ProfileId = profile.Id;

                _logger.Log(LogLevel.Info, "Initiating {0} driver, user: {1}", WebDriverString, profile.Login);
                switch (WebDriverString)
                {
                    case "chrome":
                        Instagram.InitChromeDriver();
                        break;
                    case "firefox":
                        Instagram.InitFirefoxDriver();
                        break;
                    case "phantom":
                        Instagram.InitPhantomDriver();
                        break;
                    default:
                        Instagram.InitFirefoxDriver();
                        break;
                }

                Instagram.Open("https://www.instagram.com/");
                Instagram.InitCookiesAndRefreshPage(profile.ProfileName);
                Instagram.Login(profile.Login, profile.Password, profile.ProfileName);    
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Error InitBrowser({0}) : {1}", WebDriverString, ex));

                return false;
            }

            return true;
        }

        private bool CloseBrowser()
        {
            return false;
        }

        private bool Login()
        {
            Instagram.InitCookiesAndRefreshPage(profile.ProfileName);

            return false;
        }
        #endregion

        public Service1()
        {
            InitializeComponent();
            _logger = LogManager.GetCurrentClassLogger();
            configuration = new Configuration(ConnectionString);
            //InstagramLogins.connectionString = ConnectionString;
        }

        protected override void OnStart(string[] args)
        {
            _logger.Log(LogLevel.Info, "Service started");
            try
            {
                if (profile == null)
                {
                    _logger.Log(LogLevel.Info, string.Format("No Profile id : {0}", ProfileId));
                    return;
                }

                FLAG = false;
                var minutes = TimerInterval;

                this.timer = new System.Timers.Timer(/*minutes * 60*/ 10 * 1000);  // 30000 milliseconds = 30 seconds
                this.timer.AutoReset = false;
                this.timer.Elapsed += this.timer_Elapsed;
                this.timer.Start();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Service exception: {0}", ex));
            }
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                InitBrowser();
                /*
                var prob = probability.Next(0, RandomFollow
                                               + RandomUnfollow
                                               + RandomLike
                                               + RandomSave
                                               + RandomSearchByHashTag
                                               + RandomSearchByLocation);

                #region Follow users
                if (prob < RandomFollow)
                {
                    var nfromdb = InstagramLogins.GetNumberPossibleRequests(ProfileId);
                    var rndm = probability.Next(MinFollows, MaxFollows);

                    Instagram.Follow_FromDB(MaxFollows > nfromdb ? nfromdb : rndm);
                }
                #endregion
                #region Unfollow users
                else if (prob < RandomFollow + RandomUnfollow)
                {
                    var nfromdb = InstagramLogins.GetNumberPossibleUnfollows(ProfileId);
                    var rndm = probability.Next(UnfollowUsersMin, UnfollowUsersMax);

                    Instagram.Unfollow_FromDB(probability.Next(UnfollowUsersMin, UnfollowUsersMax));


                    //Instagram.Unfollow_ScrollDown(UnfollowUsersMax > nfromdb ? nfromdb : rndm);
                    //Instagram.Unfollow_PageDown(probability.Next(UnfollowUsersMin, UnfollowUsersMax));
                }
                #endregion
                #region Save users
                else if (prob < RandomFollow + RandomUnfollow + RandomSave)
                {
                    //Сохранить подписчиков
                    if (profile.ProfileList != null && profile.ProfileList.Any())
                    {
                        Instagram.Open(profile.ProfileList.ElementAt(probability.Next(0, profile.ProfileList.Count)));
                        Instagram.SaveFollowers_PageDown(PageDownCount, 1000, profile.Id);
                    }
                }
                #endregion
                #region Find users by hash tag and save to database
                else if (prob < RandomFollow + RandomUnfollow + RandomSave + RandomSearchByHashTag)
                {
                    var nfromdb = InstagramLogins.GetNumberPossibleRequests(ProfileId);
                    var rndm = probability.Next(MinFollows, MaxFollows);

                    Instagram.FindPostsByHashTag(profile.SearchWords.ElementAt(probability.Next(0, profile.SearchWords.Count)),
                        MaxFollows > nfromdb ? nfromdb : rndm);
                }
                #endregion
                #region Find users by location and save to database
                else if (prob < RandomFollow + RandomUnfollow + RandomSave + RandomSearchByHashTag + RandomSearchByLocation)
                {
                    var nfromdb = InstagramLogins.GetNumberPossibleRequests(ProfileId);
                    var rndm = probability.Next(MinFollows, MaxFollows);

                    Instagram.FindPostsByLocation(profile.Locations.ElementAt(probability.Next(0, profile.Locations.Count)),
                        MaxFollows > nfromdb ? nfromdb : rndm);
                }
                #endregion
                #region Like posts
                else
                {
                    Instagram.Like_ScrollDown(probability.Next(MinPostLikes, MaxPostLikes));
                }
                #endregion
                */

                if (!FLAG)
                {
                    #region Follow users

                    var nfromdb = InstagramLogins.GetNumberPossibleRequests(ProfileId);
                    var rndm = probability.Next(MinFollows, MaxFollows);
                    var count = MaxFollows > nfromdb ? nfromdb : rndm;
                    _logger.Log(LogLevel.Info, string.Format("Follow users MaxFollows: {0}, NumberOfFollowLogins: {1}, Random(min:{2}-max:{3}): {4}",
                        MaxFollows, nfromdb, MinFollows, MaxFollows, rndm ));

                    Instagram.Follow_FromDB(count);
                    FLAG = true;
                    #endregion

                }
                else if (FLAG)
                {
                    #region Unfollow users

                    var nfromdb = InstagramLogins.GetNumberPossibleUnfollows(ProfileId);
                    var rndm = probability.Next(UnfollowUsersMin, UnfollowUsersMax);
                    var count = UnfollowUsersMax > nfromdb ? nfromdb : rndm;
                    _logger.Log(LogLevel.Info, string.Format("Unfollow users MaxUnfollows: {0}, NumberOfUnfollowLogins: {1}, Random(min:{2}-max:{3}): {4}",
                        UnfollowUsersMax, nfromdb, UnfollowUsersMin, UnfollowUsersMax, rndm));

                    Instagram.Unfollow_FromDB(count);
                    FLAG = false;
                    #endregion
                }

                this.timer.Interval = probability.Next(IntervalMIN * 60 * 1000, IntervalMAX * 60 * 1000);
                this.timer.Start();
                _logger.Log(LogLevel.Info, string.Format("Starting new timer with interval {0:##.000} minutes", (this.timer.Interval / (1000 * 60))));

                Instagram.CloseDriver();
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Service timer_Elapsed error : {0}", ex));

                this.timer.Interval = 60 * 1000; //Запустить через минуту
                this.timer.Start();

                _logger.Log(LogLevel.Info, string.Format("Starting new timer with interval {0:##.000} minutes", (this.timer.Interval / (1000 * 60))));

                Instagram.CloseDriver();
            }
        }

        protected override void OnStop()
        {
            //SpinupScheduler.Stop();
            try
            {
                this.timer.Stop();
                this.timer.Dispose();

                Instagram.CloseDriver();
                _logger.Log(LogLevel.Info, "Service stoped");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Error during stoping service: " + ex);
            }
        }
    }
}
