using FlwDatabase;
using InstagramSelenium;
using System;
using System.ServiceProcess;
using System.Configuration;

namespace FlwMetricService
{
    public partial class Service1 : ServiceBase
    {
        private static int Interval
        {
            get
            {
                var temp = 0;
                int.TryParse(ConfigurationManager.AppSettings["Timer"], out temp);
                return temp;
            }
        }

        private System.Timers.Timer timer;

        private bool InitBrowser()
        {
            try
            {
                Instagram.InitChromeDriver();
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
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                this.timer = new System.Timers.Timer(Interval * 60 * 1000); 
                this.timer.AutoReset = false;
                this.timer.Elapsed += this.timer_Elapsed;
                this.timer.Start();
            }
            catch (Exception ex)
            { }
        }

        private void timer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            InitBrowser();

            try
            {
                var profiles = InstagramLogins.GetProfiles();

                foreach (var prof in profiles)
                {
                    Instagram.Metrica(prof.Login, prof.Id);
                }
            }
            catch (Exception ex)
            {

            }

            Instagram.CloseDriver();

            this.timer.Interval = Interval * 60 * 1000;
            this.timer.Start();
        }

        protected override void OnStop()
        {
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
