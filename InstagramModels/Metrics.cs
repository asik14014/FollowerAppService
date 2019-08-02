using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramModels
{
    public class Metrics
    {
        public object Followers { get; set; }
        public object Following { get; set; }
        public Profile ProfileInfo { get; set; }
        public DateTime PScreen { get; set; }

        public Metrics(int id, Profile info, int followers, int following, DateTime date)
        {
            ProfileInfo = info;
            Followers = followers;
            Following = following;
            PScreen = date;
        }
    }
}
