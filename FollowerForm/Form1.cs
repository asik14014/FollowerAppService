using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Windows.Forms;
using InstagramSelenium;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using FlwDatabase;
using Configuration = FlwDatabase.Configuration;
using FlwDatabase;
using InstagramModels;
using InstagramSelenium;
using NLog;
using System.Configuration;
using System.Globalization;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace FollowerForm
{
    public partial class Form1 : Form
    {

        #region Private
        private static Profile profile = InstagramLogins.GetProfile(ProfileId);
        private static Logger _logger;
        private System.Timers.Timer timer;
        private Random probability = new Random();
        private Configuration configuration;

        private static int ProfileId
        {
            get
            {
                var id = 0;
                int.TryParse(ConfigurationManager.AppSettings["ProfileId"], out id);
                return id;
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

        private static string ConnectionString
        {
            get
            {
                return ConfigurationManager.AppSettings["connectionString"];
            }
        }
        #endregion
        
        public Form1()
        {
            InitializeComponent();
            _logger = LogManager.GetCurrentClassLogger();
            configuration = new Configuration(ConnectionString);
            //InstagramLogins.connectionString = ConnectionString;
        }

        private void listener()
        {
        }

        private static string[] businessWords =
        {
            "адрес", "работаем", "офис", "%",
            "скидка", "www", "telegram", "viber",
            "бесплатная", "доставка", ".ru", ".com",
            ".kz", ".net", ".org", ".edu", "заказать",
            "сайте", "сайт", "ссылке", "ссылка", "школа",
            "гарантия", "whats", "магазин", "app", "отправляй",
            "direct", "регистрация"
        };

        private static string[] mobilePrefix =
        {
            
            "700", "701", "702", "705", "706", "707", "708", "709",
            "747", "750", "751", "760", "761", "762", "763", "764",
            "771", "775", "776", "777", "778"
        };

        private void ReadLogins()
        {
            string[] lines = System.IO.File.ReadAllLines(@"C:\Instagram\Log\2017-03-15\2017-03-15.info.log", Encoding.GetEncoding("windows-1251"));
            var result = new List<string>();

            foreach (string line in lines)
            {
                if (line.Contains("InstagramSelenium.Instagram Follow(), Подписываемся на "))
                {
                    var login = line.Split(' ').Last();
                    if (!result.Contains(login))
                    {
                        result.Add(login);
                    }
                }
            }


            string[] lines2 = System.IO.File.ReadAllLines(@"C:\Instagram\Log\2017-03-16\2017-03-16.info.log", Encoding.GetEncoding("windows-1251"));

            foreach (string line in lines2)
            {
                if (line.Contains("InstagramSelenium.Instagram Follow(), закрываем вкладку "))
                {
                    var login = line.Split(' ').Last();
                    if (!result.Contains(login))
                    {
                        result.Add(login);
                        richTextBox1.AppendText("('" + login + "', 0),");
                        richTextBox1.AppendText("\n");
                    }
                }
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

        private static bool IsBusinessPageProfile(string description)
        {
            try
            {
                if (string.IsNullOrEmpty(description)) return false;
                description = description.ToLower().Replace(" ", "").Replace("(", "").Replace(")", "");
                for (int i = 0; i < description.Length; i++)
                {
                    //ключевые слова
                    foreach (var word in businessWords)
                    {
                        if (i + word.Length < description.Length)
                        {
                            var bufferWord = description.Substring(i, word.Length);
                            if (bufferWord.Equals(word)) return true;
                        }
                    }

                    //номер мобильного
                    if (description[i] == '8' || (description[i] == '7' && i - 1 >= 0 && description[i - 1] == '+'))
                    {
                        if (i + 3 < description.Length)
                        {
                            var bufferPrefix = description.Substring(i + 1, 3);

                            foreach (var prefix in mobilePrefix)
                            {
                                if (bufferPrefix.Equals(prefix)) return true;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                var message = ex.Message;
            }

            return false;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            var profile = InstagramLogins.GetProfile(3);
            if (profile == null)
            {
                return;
            }

            Instagram.ProfileId = profile.Id;
            //Instagram.InitPhantomDriver();
            //Instagram.InitFirefoxDriver();
            Instagram.InitChromeDriver();
            Instagram.Open("https://www.instagram.com/");
            Instagram.InitCookiesAndRefreshPage(profile.ProfileName);
            Instagram.Login(profile.Login, profile.Password, profile.ProfileName);
        }

        private void button4_Click(object sender, EventArgs e)
        {

            if (profile.ProfileList != null && profile.ProfileList.Any())
            {
                Instagram.Open(profile.ProfileList.ElementAt(probability.Next(0, profile.ProfileList.Count)));
                Instagram.SaveFollowers_PageDown(PageDownCount, 100, profile.Id);
            }

        }

        private void timer1_Tick(object sender, EventArgs e)
        {
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            timer1.Stop();
            Instagram.CloseDriver();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            timer1.Stop();
            Instagram.CloseDriver();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var data = InstagramLogins.GetMetrics(1);

            richTextBox1.AppendText(data.Count().ToString());
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Instagram.Unfollow_ScrollDown(50);
            //Instagram.SaveFollowers_PageDown(Convert.ToInt32(textBox1.Text));
            //Instagram.Unfollow_PageDown(2);

        }

        private void button5_Click(object sender, EventArgs e)
        {
            _logger.Log(LogLevel.Info, "Service started");
            try
            {
                if (profile == null)
                {
                    _logger.Log(LogLevel.Info, string.Format("No Profile id : {0}", ProfileId));
                    return;
                }
                
                var minutes = TimerInterval;

                this.timer = new System.Timers.Timer(minutes * 60 * 1000);  // 30000 milliseconds = 30 seconds
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
            InitBrowser();

            var prob = probability.Next(0, RandomFollow
                                           + RandomUnfollow
                                           + RandomLike
                                           + RandomSave
                                           + RandomSearchByHashTag
                                           + RandomSearchByLocation);

            #region Follow users
            if (prob < RandomFollow)
            {
                Instagram.Follow_FromDB(probability.Next(MinFollows, MaxFollows));
            }
            #endregion
            #region Unfollow users
            else if (prob < RandomFollow + RandomUnfollow)
            {
                //Instagram.Unfollow_PageDown(probability.Next(UnfollowUsersMin, UnfollowUsersMax));
                //Instagram.Unfollow_ScrollDown(probability.Next(UnfollowUsersMin, UnfollowUsersMax));
                Instagram.Unfollow_FromDB(probability.Next(UnfollowUsersMin, UnfollowUsersMax));
            }
            #endregion
            #region Save users
            else if (prob < RandomFollow + RandomUnfollow + RandomSave)
            {
                //Сохранить подписчиков
                if (profile.ProfileList != null && profile.ProfileList.Any())
                {
                    Instagram.Open(profile.ProfileList.ElementAt(probability.Next(0, profile.ProfileList.Count)));
                    Instagram.SaveFollowers_PageDown(PageDownCount, 100, profile.Id);
                }
            }
            #endregion
            #region Find users by hash tag and save to database
            else if (prob < RandomFollow + RandomUnfollow + RandomSave + RandomSearchByHashTag)
            {
                Instagram.FindPostsByHashTag(profile.SearchWords.ElementAt(probability.Next(0, profile.SearchWords.Count)), 600);
            }
            #endregion
            #region Find users by location and save to database
            else if (prob < RandomFollow + RandomUnfollow + RandomSave + RandomSearchByHashTag + RandomSearchByLocation)
            {
                Instagram.FindPostsByLocation(profile.Locations.ElementAt(probability.Next(0, profile.Locations.Count)), 600);
            }
            #endregion
            #region Like posts
            else
            {
                Instagram.Like_ScrollDown(probability.Next(MinPostLikes, MaxPostLikes));
            }
            #endregion

            this.timer.Interval = probability.Next(IntervalMIN * 60 * 1000, IntervalMAX * 60 * 1000);
            this.timer.Start();
            _logger.Log(LogLevel.Info, string.Format("Starting new timer with interval {0:##.000} minutes", (this.timer.Interval / (1000 * 60))));

            Instagram.CloseDriver();
        }

            private void button6_Click(object sender, EventArgs e)
        {
            Instagram.CloseDriver();
        }

        private void button7_Click(object sender, EventArgs e)
        {
            Instagram.FindPostsByHashTag(@"Алматы", 20);
        }

        public string GetDate(FileInfo f)
        {
            using (FileStream fs = new FileStream(f.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                BitmapSource img = BitmapFrame.Create(fs);
                BitmapMetadata md = (BitmapMetadata)img.Metadata;
                string date = md.DateTaken;
                Console.WriteLine(date);
                return date;
            }
        }

        private static string DecodeRational64u(System.Drawing.Imaging.PropertyItem propertyItem)
        {
            uint dN = BitConverter.ToUInt32(propertyItem.Value, 0);
            uint dD = BitConverter.ToUInt32(propertyItem.Value, 4);
            uint mN = BitConverter.ToUInt32(propertyItem.Value, 8);
            uint mD = BitConverter.ToUInt32(propertyItem.Value, 12);
            uint sN = BitConverter.ToUInt32(propertyItem.Value, 16);
            uint sD = BitConverter.ToUInt32(propertyItem.Value, 20);

            decimal deg;
            decimal min;
            decimal sec;
            // Found some examples where you could get a zero denominator and no one likes to devide by zero
            if (dD > 0) { deg = (decimal)dN / dD; } else { deg = dN; }
            if (mD > 0) { min = (decimal)mN / mD; } else { min = mN; }
            if (sD > 0) { sec = (decimal)sN / sD; } else { sec = sN; }

            if (sec == 0) return string.Format("{0}° {1:0.###}'", deg, min);
            else return string.Format("{0}° {1:0}' {2:0.#}\"", deg, min, sec);
        }

        private void ExtractLocation(string file)
        {
            if (file.ToLower().EndsWith("jpg") || file.ToLower().EndsWith("jpeg"))
            {
                Image image = null;
                try
                {
                    Console.Title = file;

                    try
                    {
                        FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read);
                        image = Image.FromStream(fs);
                    }
                    catch (Exception ex)
                    {
                        richTextBox1.AppendText(string.Format("Error opening {0} image may be corupted, {1}", file, ex.Message));
                        return;
                    }

                    // GPS Tag Names
                    // http://www.sno.phy.queensu.ca/~phil/exiftool/TagNames/GPS.html

                    // Check to see if we have gps data
                    if (Array.IndexOf<int>(image.PropertyIdList, 1) != -1 &&
                        Array.IndexOf<int>(image.PropertyIdList, 2) != -1 &&
                        Array.IndexOf<int>(image.PropertyIdList, 3) != -1 &&
                        Array.IndexOf<int>(image.PropertyIdList, 4) != -1)
                    {

                        string gpsLatitudeRef = BitConverter.ToChar(image.GetPropertyItem(1).Value, 0).ToString();
                        string latitude = DecodeRational64u(image.GetPropertyItem(2));
                        string gpsLongitudeRef = BitConverter.ToChar(image.GetPropertyItem(3).Value, 0).ToString();
                        string longitude = DecodeRational64u(image.GetPropertyItem(4));
                        richTextBox1.AppendText(string.Format("{0}\t{1} {2}, {3} {4}", file, gpsLatitudeRef, latitude, gpsLongitudeRef, longitude));
                    }
                }
                catch (Exception ex) { richTextBox1.AppendText(string.Format("Error processign {0} {1}", file, ex.Message)); }
                finally
                {
                    if (image != null) image.Dispose();
                }
            }
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Instagram.InitChromeDriver();

            var profiles = InstagramLogins.GetProfiles();

            foreach (var prof in profiles)
            {
                Instagram.Metrica(prof.Login, prof.Id);
            }
        }
    }
}
