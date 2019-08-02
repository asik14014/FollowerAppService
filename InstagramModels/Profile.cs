using System;
using System.Linq;
using System.Collections.Generic;

namespace InstagramModels
{
    public class Profile
    {
        public int Id { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string ProfileName { get; set; }
        public List<string> ProfileList { get; set; }
        public List<string> SearchWords { get; set; }
        public List<string> Locations { get; set; }

        public Profile()
        {
        }

        public Profile(int id, string login, string password, string profileName, string profiles)
        {
            Id = id;
            Login = login;
            Password = password;
            ProfileName = profileName;
            ProfileList = profiles.Split(';').ToList();
        }
    }
}
