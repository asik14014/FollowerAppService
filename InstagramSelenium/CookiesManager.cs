using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using NLog;
using LogLevel = NLog.LogLevel;
using System.Reflection;

namespace InstagramSelenium
{
    public class CookiesManager
    {
        XDocument xmlDoc;
        string xml_path;
        private static Logger _logger;

        public CookiesManager()
        {
            _logger = LogManager.GetCurrentClassLogger();

            try
            {
                xml_path = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"/InstaCookies.xml";

                xmlDoc = new XDocument();

                if (File.Exists(xml_path))
                {
                    xmlDoc = XDocument.Load(xml_path);
                }
                else
                {
                    var xmlBodyNode = new XElement("body", "");
                    var xmlCList = new XElement("cookies_list", "");

                    xmlBodyNode.Add(xmlCList);

                    xmlBodyNode.Save(xml_path);
                    xmlDoc = XDocument.Load(xml_path);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "CookiesManager error: {0}", ex);
            }
        }

        public List<MyCookie> GetCookiesForUser(string userName)
        {
            List<MyCookie> cookiesList = new List<MyCookie>();
            try
            {
                cookiesList = (from cookie in xmlDoc.Root.Descendants("cookie")
                               where ((string)cookie.Attribute("user_name")).Equals(userName)
                               select new MyCookie
                               {
                                   name = (string) cookie.Attribute("c_name"),
                                   value = (string) cookie.Attribute("c_value"),
                                   domain = (string) cookie.Attribute("c_domain"),
                                   path = (string) cookie.Attribute("c_path"),
                                   expiries = (string) cookie.Attribute("c_expiries"),
                                   secure = (string) cookie.Attribute("c_secure")
                               })
                               .ToList();
            }
            catch (Exception ex)
            {
                var message = ex.Message;
            }

            return cookiesList;
        }

        public void AddCookiesForUser(string username, string cookieName, string cookieValue, string domainName, string path, string expiries, string secure)
        {
            var xmlNode = new XElement("cookie", new XAttribute("user_name", username),
                                new XAttribute("c_name", cookieName),
                                new XAttribute("c_value", cookieValue),
                                new XAttribute("c_domain", domainName),
                                new XAttribute("c_path", path),
                                new XAttribute("c_expiries", expiries),
                                new XAttribute("c_secure", secure)
            );

            xmlDoc.Element("body").Element("cookies_list").Add(xmlNode);
        }

        public void Save()
        {
            xmlDoc.Save(xml_path);
        }

        public void removeCookieForUser(string username)
        {
            try
            {
                xmlDoc.Element("body").Element("cookies_list").Descendants("cookie")
                                   .Where(x => (string)x.Attribute("user_name") == username)
                                   .Remove();
            }
            catch
            {
            }
        }


        public class MyCookie
        {
            public string name { get; set; }
            public string value { get; set; }
            public string domain { get; set; }
            public string path { get; set; }
            public string expiries { get; set; }
            public string secure { get; set; }
        }

    }
}
