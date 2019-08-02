using FlwDatabase;
using InstagramSelenium;
using System;
using System.ServiceProcess;
using System.Configuration;
using System.Linq;
using NLog;
using System.Collections.Generic;

namespace FlwSaver
{
    public partial class Service1 : ServiceBase
    {
        private System.Timers.Timer timer;
        private Random probability = new Random();
        private static Logger _logger;

        private static List<int> AllowedProfiles
        {
            get
            {
                return ConfigurationManager.AppSettings["allowedProfiles"].Split(',').Select(item => Convert.ToInt32(item)).ToList();
            }
        }

        private static int Number
        {
            get
            {
                var temp = 0;
                int.TryParse(ConfigurationManager.AppSettings["NumberPageDown"], out temp);
                return temp;
            }
        }

        private bool InitBrowser()
        {
            try
            {
                Instagram.InitFirefoxDriver();

                Instagram.Open("https://www.instagram.com/");
                Instagram.InitCookiesAndRefreshPage("aliaskhat");
                Instagram.Login("aliaskhat", "3161148asker", "askhat");
            }
            catch (Exception ex)
            {
                return false;
            }

            return true;
        }

        public Service1()
        {
            InitializeComponent();
            _logger = LogManager.GetCurrentClassLogger();
        }

        protected override void OnStart(string[] args)
        {
            _logger.Log(LogLevel.Info, "Service started");

            try
            {
                this.timer = new System.Timers.Timer(1 * 30 * 1000); 
                this.timer.AutoReset = false;
                this.timer.Elapsed += this.timer_Elapsed;
                this.timer.Start();
            }
            catch (Exception ex)
            { }
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            _logger.Log(LogLevel.Info, "Timer elapsed");


            try
            {
                var profiles = InstagramLogins.GetProfiles();

                foreach (var prof in profiles)
                {
                    if (AllowedProfiles.Any(item => item == prof.Id))
                    {
                        InitBrowser();


                        _logger.Log(LogLevel.Info, string.Format("Save followers profile : {0}", prof.Login));
                        Instagram.Open(prof.ProfileList.ElementAt(probability.Next(0, prof.ProfileList.Count)));
                        Instagram.SaveFollowers_PageDown(Number, 1000, prof.Id);


                        Instagram.CloseDriver();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Timer elapsed error: {0}", ex));
            }


            this.timer.Interval = 5 * 60 * 1000;
            this.timer.Start();
        }

        protected override void OnStop()
        {
            _logger.Log(LogLevel.Info, "Service stoped");

            try
            {
                this.timer.Stop();
                this.timer.Dispose();
            }
            catch (Exception ex)
            {
            }
        }
    }
}
