using System;
using System.Collections.Generic;
using Quartz;
using InstagramSelenium;
using System.Configuration;
using System.Linq;

namespace FlwService.Jobs
{
    [DisallowConcurrentExecution]
    public class Spinup : IJob
    {
        private static List<string> profileList
        {
            get
            {
                return ConfigurationManager.AppSettings["profiles"].Split(';').ToList();
            }
        }

        public void Execute(IJobExecutionContext context)
        {
            //like or follow
            var probability = new Random();
            var min = 20;
            var max = 150;

            if (probability.Next(0, 100) <  90)
            {
                int.TryParse(ConfigurationManager.AppSettings["MinFollows"], out min);
                int.TryParse(ConfigurationManager.AppSettings["MaxFollows"], out max);

                if (profileList != null && profileList.Any())
                {
                    Instagram.Follow_ScrollDown(profileList.ElementAt(probability.Next(0, profileList.Count)), 
                                               probability.Next(min, max));
                }
            }
            else
            {
                int.TryParse(ConfigurationManager.AppSettings["MinPostToLike"], out min);
                int.TryParse(ConfigurationManager.AppSettings["MaxPostToLike"], out max);

                Instagram.Like_ScrollDown(probability.Next(min, max));
            }

        }
    }
}
