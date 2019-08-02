using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using OpenQA.Selenium;
using NLog;
using LogLevel = NLog.LogLevel;
using OpenQA.Selenium.Firefox;
//using OpenQA.Selenium.PhantomJS;
using OpenQA.Selenium.Chrome;
using FlwDatabase;
using System.Diagnostics;
using System.Globalization;
using InstagramModels;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;

namespace InstagramSelenium
{
    public static class Instagram
    {
        #region PRIVATE

        #region Properties
        #region private
        private static CookiesManager cookiesManager = new CookiesManager();
        private static IWebDriver _webDriver;
        private static readonly Random Rnd;
        private static Logger _logger;
        private static Stopwatch _stopWatch;
        private static string[] followWords = { "Подписки", "Запрос отправлен", "Подписаться", "Follow", "Following", "Requested" };
        private static string[] businessWords =
        {
            "адрес", "работа", "офис", "%",
            "скидка", "www", "telegram", "viber", "беспл", "достав", ".ru", ".com",
            ".kz", ".net", ".org", ".edu", "заказ", "сайте", "сайт", "ссылке", "ссылка", "школа",
            "гарантия", "whats", "магазин", "app", "отправляй", "direct", "регистрация", "зароботок",
            "онлайн", "раскрутка", ".gl", "поставка", "цены", "ресницы", "ногти", "дизайн", "обуч",
            "питание", "татуаж", "подпи", "фото", "директ", "вопросы",
            "наличии", "кухня", "блюда", "меню", "планировка", "кафе", "игруш", "конструкт",
            "мастер"
            };
        private static string[] mobilePrefix =
        {
            "700", "701", "702", "705", "706", "707", "708", "709",
            "747", "750", "751", "760", "761", "762", "763", "764",
            "771", "775", "776", "777", "778", "727"
        };
        private static string[] followText = { "Подписаться", "Follow" };
        private static string[] followedText = { "Подписки", "Following" };
        private static string[] followRequestedText = { "Запрос отправлен", "Requested" };
        private static string[] followersText = { "подписчиков", "followers" };
        #endregion

        #region public
        public static int ProfileId;
        #endregion
        #endregion

        /// <summary>
        /// Уснуть
        /// </summary>
        /// <param name="min">минимальное значение в секундах</param>
        /// <param name="max">максимальное значение в секундах</param>
        private static void Sleep(decimal min, decimal max)
        {
            var mSeconds = Rnd.Next((int)(min * 1000), (int)(max * 1000));
            _logger.Log(LogLevel.Trace, 
                string.Format("Sleep(min : {0}, max : {1}) : уснуть на {2} миллисекунд", min, max, mSeconds));
            Thread.Sleep(mSeconds);
        }

        /// <summary>
        /// Бизнес профайл
        /// </summary>
        /// <param name="description">Описание профайла</param>
        /// <returns></returns>
        private static bool IsBusinessPageProfile(string description)
        {
            try
            {
                if (string.IsNullOrEmpty(description)) return false;
                description = description.ToLower().Replace(" ", "").Replace("(", "").Replace(")", "").Replace("-", "");
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
                        /*
                        if (i + 3 < description.Length)
                        {
                            var bufferPrefix = description.Substring(i + 1, 3);

                            foreach (var prefix in mobilePrefix)
                            {
                                if (bufferPrefix.Equals(prefix)) return true;
                            }
                        }
                        */
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("IsBusinessPageProfile({0}) : {1}", description, ex));
            }

            return false;
        }

        /// <summary>
        /// Login есть в базе
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        private static bool IsInDatabase(string login)
        {
            return InstagramLogins.IsFollowedUser(login, ProfileId);
        }
        
        private static User GetUser(string login)
        {

            return InstagramLogins.GetUser(login, ProfileId);
        }

        /// <summary>
        /// Достать список логинов из базы для подписки
        /// </summary>
        /// <param name="count">Количество логинов</param>
        /// <returns></returns>
        private static List<string> GetNotFollowedUsers(int count)
        {
            return InstagramLogins.GetUsers(ProfileId, count, false);
        }

        /// <summary>
        /// Достать список логинов из базы для отписки
        /// </summary>
        /// <param name="count">Количество логинов</param>
        /// <returns></returns>
        private static List<string> GetUsersToUnfollow(int count)
        {
            return InstagramLogins.GetUnfollowedLogins(ProfileId, count);
        }

        #region Методы для работы со страницей

        /// <summary>
        /// Необходимость авторизации
        /// </summary>
        /// <returns></returns>
        private static bool IsNeedToLogin()
        {
            try
            {
                return IsElementPresent(By.XPath("//input[@name='username']"))
                       && IsElementPresent(By.XPath("//input[@name='password']"))
                       && IsElementPresent(By.XPath("//button"));
            }
            catch (Exception ex)
            {
            }
            return false;
        }

        /// <summary>
        /// Уже лайкал пост
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static bool AlreadyLikedArticle(IWebElement element)
        {
            var likeElement = element.FindElement(By.XPath(".//a[@role='button']"));
            string[] text = { "Не нравится", "Unlike" };
            return text.Any(likeElement.Text.Contains);
        }

        /// <summary>
        /// Подписывался на профайл
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static bool AlreadyFollowingUser(IWebElement element)
        {
            var followButton = element.FindElement(By.TagName("button"));
            return !followText.Any(followButton.Text.Contains);
        }

        private static bool IsDeletedProfile()
        {
            var bttns = GetElements(By.TagName("button"));
            if (bttns == null || !bttns.Any())
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Элемент присутствует на странице
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        private static bool IsElementPresent(By by)
        {
            try
            {
                _webDriver.FindElement(by);
                return true;
            }
            catch (NoSuchElementException)
            {
                return false;
            }
        }

        /// <summary>
        /// Достать элемент со страницы
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        private static IWebElement GetElement(By by)
        {
            IWebElement result = null;
            int attempts = 0;
            while (attempts < 5)
            {
                try
                {
                    result = _webDriver.FindElement(by, 60);
                    break;
                }
                catch (StaleElementReferenceException e)
                {
                }
                attempts++;
            }
            return result;
        }

        /// <summary>
        /// Достать элементы со страницы
        /// </summary>
        /// <param name="by"></param>
        /// <returns></returns>
        private static ReadOnlyCollection<IWebElement> GetElements(By by)
        {
            ReadOnlyCollection<IWebElement> result = null;
            int attempts = 0;
            while (attempts < 5)
            {
                try
                {
                    result = _webDriver.FindElements(by, 60);
                    break;
                }
                catch (StaleElementReferenceException e)
                {
                }
                attempts++;
            }

            return result;
        }

        /// <summary>
        /// Нажать на элемент
        /// </summary>
        /// <param name="element"></param>
        private static void ClickOn(IWebElement element)
        {
            ((IJavaScriptExecutor)_webDriver).ExecuteScript("arguments[0].click();", element);
        }

        /// <summary>
        /// Проскролить до элемента
        /// </summary>
        /// <param name="element"></param>
        private static bool ScrollInto(IWebElement element)
        {
            try
            {
                ((IJavaScriptExecutor)_webDriver).ExecuteScript("arguments[0].scrollIntoView(true);", element);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
            //element.SendKeys(Keys.PageDown);
        }

        /// <summary>
        /// Нажать на PageDown
        /// </summary>
        private static void PageDown()
        {
            GetElement(By.TagName("ul")).SendKeys(Keys.PageDown);
        }

        /// <summary>
        /// Достать логин профайла
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        private static string GetProfileLogin(IWebElement element)
        {
            try
            {
                var client = element.FindElement(By.XPath(".//a[@title][@href]"));
                return client.GetAttribute("title");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Error while getting profile login, exception: {0}", ex));
            }
            return null;
        }

        /// <summary>
        /// Вернуть список подписчиков (элементов)
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        private static ReadOnlyCollection<IWebElement> UserFollowList(string[] words)
        {
            List<IWebElement> result = new List<IWebElement>();
            int attempts = 0;

            while (attempts < 10)
            {
                try
                {
                    result = new List<IWebElement>();
                    var collection = _webDriver.FindElements(By.XPath(".//ul/li"), 10000);
                    foreach (var item in collection)
                    {
                        var lastWord = string.IsNullOrEmpty(item.Text) ? "" : item.Text.Split("\r\n".ToCharArray()).Last();
                        if (words.Contains(lastWord))
                        {
                            result.Add(item);
                        }
                    }
                    break;
                }
                catch (StaleElementReferenceException e)
                {
                }
                attempts++;
            }
            
            return new ReadOnlyCollection<IWebElement>(result);
        }

        /// <summary>
        /// Поставить лайк
        /// </summary>
        /// <returns></returns>
        private static bool LikePost()
        {
            try
            {
                var buttons = GetElements(By.XPath(".//a[@role='button']"));
                string[] text = { "Нравится", "Like" };
                if (buttons != null && buttons.Any(btn => text.Any(btn.Text.Contains)))
                {
                    var likeButton = buttons.First(btn => text.Any(btn.Text.Contains));
                    ClickOn(likeButton);
                    Sleep(1, 1.5M);

                    return true;
                }
            }
            catch (Exception likeEx)
            {
                _logger.Log(LogLevel.Error,
                    string.Format("Follow(), Поставить Like фотографии: {0}", likeEx));
            }

            return false;
        }

        private static bool OpenPost(IWebElement post)
        {
            try
            {
                ScrollInto(post);
                Sleep(0, 0.5M);
                ClickOn(post);
                Sleep(1, 1.5M);

                return true;
            }
            catch (Exception openEx)
            {
                _logger.Log(LogLevel.Error,
                    string.Format("Follow(), Открыть фотографию: {0}", openEx));
                throw;
            }

            return false;
        }

        private static bool ClosePost()
        {
            try
            {
                var div = GetElement(By.XPath(".//div[article/@class]"));
                ClickOn(div);
                Sleep(1, 1.5M);

                return true;
            }
            catch (Exception closeEx)
            {
                _logger.Log(LogLevel.Error,
                    string.Format("Follow(), Закрыть фотографию: {0}", closeEx));
                throw;
            }

            return false;
        }

        /// <summary>
        /// Открыть в новой вкладке
        /// </summary>
        /// <param name="url">Ссылка</param>
        /// <returns></returns>
        private static bool OpenNewTab(string url)
        {
            try
            {
                var windowHandles = _webDriver.WindowHandles;
                ((IJavaScriptExecutor)_webDriver).ExecuteScript(string.Format("window.open('{0}', '_blank');", url));
                var newWindowHandles = _webDriver.WindowHandles;
                var openedWindowHandle = newWindowHandles.Except(windowHandles).Single();
                _webDriver.SwitchTo().Window(openedWindowHandle);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("OpenNewTab({0}) : {1}", url, ex));
                return false;
            }
        }
        
        /// <summary>
        /// Открыть профайл в новой вкладке, подписаться, пролайкать посты, закрыть вкладку
        /// </summary>
        /// <param name="url">Ссылка профайла</param>
        /// /// <param name="clientLogin">Логин</param>
        private static FollowStatus Follow(string url, string clientLogin)
        {
            var result = FollowStatus.Unknown;
            try
            {
                //Открыть страницу
                Open(url);

                //Удаленный профайл
                if (IsDeletedProfile())
                {
                    _logger.Log(LogLevel.Info, string.Format("Profile {0} is deleted", clientLogin));
                    InstagramLogins.DeletedLogin(clientLogin, ProfileId);
                    return result;
                }

                _logger.Log(LogLevel.Trace, string.Format("Open profile : {0}", clientLogin));
                Sleep(1, 2);

                //Достаем описание страницы
                var description = GetElement(By.TagName("header")).Text;
                try
                {
                    var businessProfile = IsBusinessPageProfile(description);
                    //Бизнес профайл
                    if (businessProfile)
                    {
                        _logger.Log(LogLevel.Info, string.Format("Follow() : {0} profile is business profile", clientLogin));
                        InstagramLogins.BusinessLogin(clientLogin, ProfileId);
                        return result;
                    }

                    var isFollowButtonClicked = false;

                    #region Подписаться

                    try
                    {
                        var buttons = GetElements(By.XPath(".//button"));
                        if (buttons != null && buttons.Any(btn => followText.Any(btn.Text.Contains)))
                        {
                            var followButton = buttons.First(btn => followText.Any(btn.Text.Contains));
                            ClickOn(followButton);
                        }
                        Sleep(1, 2);
                    }
                    catch (Exception followEx)
                    {
                        _logger.Log(LogLevel.Error,
                            string.Format(
                                "Follow(IWebElement element), Случилось что-то не хорошее во время подписки на профиль: {0}",
                                followEx));
                        isFollowButtonClicked = false;
                        return result;
                    }

                    try
                    {
                        var buttons = GetElements(By.XPath(".//button"));
                        if (buttons != null && buttons.Any(btn => followedText.Any(btn.Text.Contains)))
                        {
                            _logger.Log(LogLevel.Info, string.Format("Подписался на {0}", clientLogin));
                            isFollowButtonClicked = true;
                            result = FollowStatus.Followed;
                        }
                        else if (buttons != null && buttons.Any(btn => followRequestedText.Any(btn.Text.Contains)))
                        {
                            _logger.Log(LogLevel.Info, string.Format("Запрос отправлен на {0}", clientLogin));
                            isFollowButtonClicked = true;
                            result = FollowStatus.Requested;
                        }
                        else
                        {
                            _logger.Log(LogLevel.Info, string.Format("Пробовал подписаться на {0}", clientLogin));
                            isFollowButtonClicked = false;
                            return FollowStatus.Tried;
                        }
                    }
                    catch (Exception logEx)
                    {
                        isFollowButtonClicked = false;
                        return result;
                    }

                    #endregion

                    //Если смог подписаться
                    if (isFollowButtonClicked)
                    {
                        //Обновить статус в базе
                        InstagramLogins.FollowLogin(clientLogin, ProfileId);

                        #region Пролайкать посты юзера

                        try
                        {
                            _logger.Log(LogLevel.Trace,
                                string.Format("Calling like function (login : {0})", clientLogin));
                            IWebElement photo = null;
                            //a href taken-by
                            //var postList = GetElements(By.XPath(".//img[@src]"));
                            var postList = GetElements(By.XPath("//a[contains(@href, '" + string.Format("?taken-by={0}", clientLogin) + "')]"));
                            var postCount = postList.Count - 1;
                            if (postCount < 1) return result;
                            var mustLikeCount = 1;
                            var likes = 0;
                            bool[] photos = new bool[postCount];
                            for (int i = 0; i < postCount; i++)
                                photos[i] = false;

                            while (mustLikeCount > likes && postCount > likes)
                            {
                                #region Choose photo

                                try
                                {
                                    var index = Rnd.Next(postCount);
                                    while (photos[index])
                                    {
                                        index = Rnd.Next(postCount);
                                    }

                                    photos[index] = true;
                                    likes++;
                                    photo = postList[index];
                                }
                                catch (Exception chooseEx)
                                {
                                    _logger.Log(LogLevel.Error,
                                        string.Format("Follow(), Выбрать фотографию: {0}", chooseEx));
                                    throw;
                                }

                                #endregion

                                //Open photo
                                OpenPost(photo);
                                //Like photo
                                LikePost();
                                //Close photo
                                ClosePost();

                                _logger.Log(LogLevel.Trace, string.Format("Photo liked : {0}", clientLogin));
                                postList = GetElements(By.XPath(".//img[@id][@src]"));
                            }
                        }
                        catch (Exception followEx)
                        {
                            _logger.Log(LogLevel.Error,
                                string.Format(
                                    "Follow(), Случилось что-то не хорошее во время открытия и лайкания профиля: {0}",
                                    followEx));
                        }

                        #endregion

                        return result;
                    }
                }
                catch (Exception ex2)
                {
                    _logger.Log(LogLevel.Error, string.Format("Follow(IWebElement element) : {0}", ex2));
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Follow(IWebElement element) : {0}", ex));
            }

            return result;
        }

        /// <summary>
        /// Отписаться Вручную без базы
        /// </summary>
        /// <param name="element"></param>
        private static void Unfollow(IWebElement element)
        {
            try
            {
                var login = GetProfileLogin(element);
                var dbUser = GetUser(login);

                //Отписаться
                var unfollowButton = element.FindElement(By.TagName("button"));
                ClickOn(unfollowButton);

                if (dbUser != null) //Если логин сохранен
                {
                    //Обновить статус в базе
                    InstagramLogins.UnfollowLogin(login, ProfileId);
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Unfollow() : {0}", ex));
            }
        }

        /// <summary>
        /// Отписаться, логин из базы
        /// </summary>
        /// <param name="url"></param>
        /// <param name="login"></param>
        private static bool Unfollow(string url, string clientLogin)
        {
            try
            {
                Open(url);

                #region Отписаться
                try
                {
                    var buttons = GetElements(By.XPath(".//button"));
                    if (buttons != null && buttons.Any(btn => followedText.Any(btn.Text.Contains)))
                    {
                        var followButton = buttons.First(btn => followedText.Any(btn.Text.Contains));
                        ClickOn(followButton);

                        //Проверить действительно ли мы отписались
                        buttons = GetElements(By.XPath(".//button"));
                        if (buttons != null && buttons.Any(btn => followText.Any(btn.Text.Contains)))
                        {
                            InstagramLogins.UnfollowLogin(clientLogin, ProfileId, true);
                            _logger.Log(LogLevel.Info, string.Format("Unfollow() : {0} user unfollowed", clientLogin));
                            return true;
                        }
                        else
                        {
                            _logger.Log(LogLevel.Info, string.Format("Unfollow() : {0} user unfollow failed unknown causes", clientLogin));
                            return false;
                        }
                    }
                    else if (buttons != null && buttons.Any(btn => followRequestedText.Any(btn.Text.Contains)))
                    {
                        _logger.Log(LogLevel.Info, string.Format("Unfollow() : {0} user requested (update request date)", clientLogin));
                        InstagramLogins.UpdateLoginStillRequested(clientLogin, ProfileId);
                        
                        return false;
                    }
                    else
                    {
                        _logger.Log(LogLevel.Info, string.Format("Unfollow() : {0} user already unfollowed or page may be deleted", clientLogin));
                        InstagramLogins.UnfollowLogin(clientLogin, ProfileId, true);
                    }
                    Sleep(1, 1.5M);
                }
                catch (Exception followEx)
                {
                    _logger.Log(LogLevel.Error, string.Format("Unfollow() error: {0}", followEx));
                    return false;
                }
                #endregion
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Unfollow({0},{1}) : {2}", url, clientLogin, ex));
                return false;
            }
            return false;
        }

        #endregion

        #region Cookie
        private static Cookie NewCookie(string name, string value, string domain, string path, DateTime? expiry)
        {
            _logger.Log(LogLevel.Trace, "Initialize new cookie. Cookie name is: '" + name + "'.", "Document");
            var cookie = new Cookie(name, value, domain, path, expiry);
            return cookie;
        }

        private static void InitCookies(string name, string domain)
        {
            var cookies = cookiesManager.GetCookiesForUser(name);

            foreach (var cookie in cookies)
            {
                if (cookie.domain.Contains((domain)))
                {
                    DateTime? dt = string.IsNullOrEmpty(cookie.expiries)
                        ? (DateTime?) null
                        : DateTime.ParseExact(cookie.expiries, "MM/dd/yyyy HH:mm:ss.fff",
                        CultureInfo.InvariantCulture);
                    var newCookie = NewCookie(cookie.name, cookie.value, cookie.domain, cookie.path, dt);
                    _webDriver.Manage().Cookies.AddCookie(newCookie);
                }
            }
        }

        /// <summary>
        /// Сохранить cookie
        /// </summary>
        /// <param name="name"></param>
        private static void SaveCookies(string name)
        {
            var cookies = _webDriver.Manage().Cookies.AllCookies;

            foreach (var cookie in cookies)
            {
                string expiry = string.Empty;
                if (cookie.Expiry.HasValue)
                    expiry = cookie.Expiry.Value.ToString("MM/dd/yyyy HH:mm:ss.fff",
                        CultureInfo.InvariantCulture);

                cookiesManager.AddCookiesForUser(name, cookie.Name, cookie.Value, cookie.Domain, cookie.Path,
                                                 expiry, cookie.Secure.ToString());
            }

            cookiesManager.Save();
        }

        private static bool Search(string text)
        {
            //1. вбить текст
            //2. перейти на страницу результатов

            try
            {
                _logger.Log(LogLevel.Info, string.Format("search {0}", text));
                var searchInput = GetElement(By.XPath("//input[@type='text']"));
                //ClickOn(searchInput);
                Sleep(1, 3);

                //enter login
                searchInput.Clear();
                searchInput.SendKeys(text);
                Sleep(0, 4);

                _logger.Log(LogLevel.Info, "Found results");

                searchInput.SendKeys(Keys.Enter);
                Sleep(1, 3);

                searchInput.SendKeys(Keys.Enter);
                Sleep(8, 15);
                return true;
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Login() : {0}", ex));
            }

            return false;
        }
        
        #endregion

        #endregion
        
        #region Initialization

        static Instagram()
        {
            _logger = LogManager.GetCurrentClassLogger();
            Rnd = new Random();
            _stopWatch = new Stopwatch();
            AppDomain.CurrentDomain.ProcessExit += StaticClass_Dtor;
        }

        public static void InitFirefoxDriver(FirefoxProfile profile)
        {
            //_webDriver = new FirefoxDriver(profile);
            _webDriver = new FirefoxDriver();
        }

        public static void InitFirefoxDriver(DesiredCapabilities capa)
        {
            //_webDriver = new FirefoxDriver(capa);
            _webDriver = new FirefoxDriver();
        }

        public static void InitFirefoxDriver(bool mobile = false)
        {
            //_webDriver = new FirefoxDriver();
            _webDriver = new FirefoxDriver(FirefoxDriverService.CreateDefaultService(), new FirefoxOptions(), TimeSpan.FromMinutes(5));
        }

        public static void InitChromeDriver(bool mobile = false)
        {
            if (mobile)
            {
                ChromeOptions chromeCapabilities = new ChromeOptions();
                chromeCapabilities.EnableMobileEmulation("Nexus 6P");
                _webDriver = new ChromeDriver(chromeCapabilities);
            }
            else
            {
                //ChromeOptions m_Options = new ChromeOptions();
                //m_Options.AddArgument("--user-data-dir=C:/Users/dell/AppData/Local/Google/Chrome/User Data/Profile 2");
                //m_Options.AddArgument("--disable-extensions");
                //m_Options.AddArgument("--silent");
                //m_Options.AddArgument("--incognito");

                //_webDriver = new ChromeDriver(@"C:\Users\askhat.a\Desktop\Follower\packages\Selenium.WebDriver.ChromeDriver.75.0.3770.140\driver\win32", m_Options);
                //_webDriver = new ChromeDriver(m_Options);
                _webDriver = new ChromeDriver();
            }
            Sleep(10, 20);
        }

        public static void InitPhantomDriver(bool mobile = false)
        {
            //_webDriver = new PhantomJSDriver();
        }

        #endregion


        #region Общие методы
        
        /// <summary>
        /// Открыть Instagram
        /// </summary>
        public static void Open(string url)
        {
            try
            {
                _logger.Log(LogLevel.Trace, string.Format("Open Url {0}", url));
                _webDriver.Url = url;
                var waitForDocumentReady = new WebDriverWait(_webDriver, TimeSpan.FromMinutes(5));
                waitForDocumentReady.Until(
                    (wdriver) =>
                        (_webDriver as IJavaScriptExecutor).ExecuteScript("return document.readyState").Equals("complete"));
                Sleep(1, 2);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Open(url: {0}) : {1}", url, ex));
            }
        }

        /// <summary>
        /// Закрыть browser
        /// </summary>
        public static void CloseDriver()
        {
            _logger.Log(LogLevel.Info, "CloseDriver");
            if (_webDriver != null)
            {
                _webDriver.Close();
                _webDriver.Quit();
                _webDriver = null;
            }
        }

        static void StaticClass_Dtor(object sender, EventArgs e)
        {
            CloseDriver();
        }

        #endregion


        #region Login/Enter
        /// <summary>
        /// Установить cookie и обновить страницу
        /// </summary>
        /// <param name="userName"></param>
        public static void InitCookiesAndRefreshPage(string userName)
        {
            _logger.Log(LogLevel.Info, "InitCookiesAndRefreshPage profile: {0}", userName);

            try
            {
                InitCookies(userName, "instagram");
                _webDriver.Navigate().Refresh();

                var waitForDocumentReady = new WebDriverWait(_webDriver, TimeSpan.FromMinutes(5));
                waitForDocumentReady.Until(
                    (wdriver) =>
                        (_webDriver as IJavaScriptExecutor).ExecuteScript("return document.readyState").Equals("complete"));

                Sleep(1, 2);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "InitCookiesAndRefreshPage error: {0}", ex);
            }
        }

        /// <summary>
        /// Авторизация
        /// </summary>
        /// <param name="login">Логин страницы</param>
        /// <param name="password">Пароль</param>
        public static void Login(string login, string password, string profileName)
        {
            if (!IsNeedToLogin())
            {
                _logger.Log(LogLevel.Info, "Service already logged!");
                return;
            }

            try
            {
                _logger.Log(LogLevel.Info, "Calling Login function");

                var buttons = GetElements(By.XPath(".//a"));
                string[] text = { "Вход", "Log in" };
                if (buttons != null && buttons.Any(btn => text.Any(btn.Text.Contains)))
                {
                    var loginButton = buttons.First(btn => text.Any(btn.Text.Contains));
                    ClickOn(loginButton);
                    Sleep(1, 3);
                }
                else
                {
                    _logger.Log(LogLevel.Info, "Web page has no elements with text 'Login'");
                    _logger.Log(LogLevel.Info, "Service cannot login");
                    return;
                }

                var name = GetElement(By.XPath("//input[@name='username']"));
                var passwordElement = GetElement(By.XPath("//input[@name='password']"));
                var enter = GetElement(By.XPath("//button"));

                //enter login
                name.SendKeys(login);
                Sleep(0, 1);

                //enter password
                passwordElement.SendKeys(password);
                Sleep(0, 1);

                //click enter
                ClickOn(enter);

                _logger.Log(LogLevel.Info, "User successfully logged on");

                Sleep(1, 3);

                SaveCookies(profileName);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Login() : {0}", ex));
            }
        }
        #endregion


        #region Follow
        /// <summary>
        /// Пописаться на пользователей из базы данных
        /// </summary>
        /// <param name="count">количество пользователей</param>
        public static void Follow_FromDB(int count)
        {
            _logger.Log(LogLevel.Info, string.Format("Starting Follow_FromDB count: {0}", count));

            int Warning = 0;
            int index = 0;
            var list = new List<string>();

            do
            {
                #region Достаем список из базы
                try
                {
                    list = GetNotFollowedUsers(count);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, string.Format("Follow_FromDB --> GetNotFollowedUsers error: {0}", ex));
                    return;
                }
                #endregion

                foreach (var login in list)
                {
                    try
                    {
                        var status = Follow(string.Format("https://www.instagram.com/{0}/", login), login);
                        if (status == FollowStatus.Followed || status == FollowStatus.Requested) index++;
                        else if (status == FollowStatus.Tried)
                        {
                            Warning++;

                            if (Warning > 4)
                            {
                                _logger.Log(LogLevel.Info, "Warning maybe instagram blocked follow function temporarily!!!");
                                _logger.Log(LogLevel.Info, "Follow_FromDB ended");
                                return;
                            }
                        }
                        if (index >= count) break;
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, string.Format("Follow_FromDB --> Follow({0}), exception: {1}", login, ex));
                    }
                    Sleep(1, 2.5M);
                }
            } while (index < count || (list == null || !list.Any()));

            _logger.Log(LogLevel.Info, string.Format("Follow_FromDB ended, count: {0}", count));
        }

        #endregion


        #region Unfollow

        /// <summary>
        /// Отписаться от юзеров (список юзеров из базы)
        /// </summary>
        /// <param name="count"></param>
        public static void Unfollow_FromDB(int count)
        {
            _logger.Log(LogLevel.Info, string.Format("Starting unfollow_FromDB count: {0}", count));

            int i = 0;
            var list = new List<string>();

            while (i < count)
            {
                try
                {
                    list = GetUsersToUnfollow(count);
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, string.Format("Unfollow_FromDB, cannot get user list from db: {0}", ex));
                }

                if (!list.Any() || list.Count() == 0) return;//пустой список

                foreach (var login in list)
                {
                    try
                    {
                        if (Unfollow(string.Format("https://www.instagram.com/{0}/", login), login))
                            i++;
                        Sleep(1, 2.5M);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, string.Format("Unfollow_FromDB --> Unfollow error: {0}", ex));
                    }
                }
            }

            _logger.Log(LogLevel.Info, string.Format("Unfollow_FromDB ended, actual count: {0}", i));
        }

        #endregion


        #region Save Users
        /// <summary>
        /// Сохранить список подписчиков
        /// </summary>
        /// <param name="count">Количество логинов</param>
        public static void SaveFollowers_PageDown(int pageDown, int count, int profId)
        {
            _logger.Log(LogLevel.Info, string.Format("Starting SaveFollowers_PageDown count: {0}", count));

            Sleep(1, 3);

            try
            {
                #region Open following link

                _logger.Log(LogLevel.Trace, "Кликаем на ссылку Подписки");
                var links = GetElements(By.XPath(".//ul/li/a[@href]"));
                if (links != null && links.Any(link => followersText.Any(link.Text.Contains)))
                {
                    var followersLink = links.First(link => followersText.Any(link.Text.Contains));
                    ClickOn(followersLink);
                }
                else
                {
                    _logger.Log(LogLevel.Info, "Не нашел ссылку подписчики");
                    return;
                }

                Sleep(1, 3);

                #endregion

                var users = UserFollowList(followWords);
                var user = users[0];
                ScrollInto(user);
                user.Click();

                int savedNumber = 0;
                int reset = 0;

                while (savedNumber < count)
                {
                    if (reset > 4) break;
                    reset++;
                    for (int index = 0; index < pageDown; index++)
                    {
                        try
                        {
                            PageDown();
                            _logger.Log(LogLevel.Trace, "PageDown...");
                            Sleep(0.3M, 1M);
                        }
                        catch (Exception exPageD)
                        {
                            _logger.Log(LogLevel.Error, string.Format("PageDown error: {0}", exPageD));
                        }
                    }

                    var elements = UserFollowList(followWords);
                    _logger.Log(LogLevel.Info, string.Format("PageDown, number of logins {0}", elements.Count));
                    string[] stringSeparators = new string[] { "\r\n" };
                    foreach (var element in elements)
                    {
                        try
                        {
                            var buffer = element.Text.Split(stringSeparators, StringSplitOptions.None);
                            var login = buffer.Length > 0 ? buffer[0] : "";
                            if (!string.IsNullOrEmpty(login))
                            {
                                reset = 0;
                                InstagramLogins.SaveLogin(login, profId);
                                savedNumber++;
                                _logger.Log(LogLevel.Info, string.Format("PageDown, login {0}, added to database", login));
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(LogLevel.Error, string.Format("PageDown error - {0}", ex));
                        }
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Log(LogLevel.Error, string.Format("PageDown error - {0}", e));
            }

            _logger.Log(LogLevel.Error, string.Format("SaveFollowers_PageDown ended, count: {0}", count));
        }
        #endregion


        #region Using search input (hashtag, keywords, location)
        public static void FindPostsByHashTag(string text, int numberOfPosts)
        {
            _logger.Log(LogLevel.Info, string.Format("Starting FindUsersByHashTag hashtag: {0}", text));

            var flag = false;
            while (!flag)
            {
                flag = Search(text);
            }

            //Если не смог найти посты по тэгу выходим
            if (!flag) return;

            var savedPostLinks = 0;
            var currentPostIndex = 10;
            try
            {
                while (savedPostLinks < numberOfPosts)
                {
                    var posts = GetElements(By.XPath($"//a[contains(@href, '?tagged={text.ToLower()}')]"));


                    #region если дошли до конца и на странице есть кнопка "Загрузить еще" жмем на нее
                    if (posts.Count() <= currentPostIndex)
                    {
                        try
                        {
                            var buttons = GetElements(By.XPath(".//a[@href]"));
                            string[] texttemp = { "Загрузить еще", "Load more" };
                            //link => text.Contains(link.Text)
                            if (buttons != null && buttons.Any(btn => texttemp.Any(btn.Text.Contains)))
                            {
                                //жмем на кнопку
                                var followButton = buttons.First(btn => texttemp.Any(btn.Text.Contains));
                                ClickOn(followButton);
                            }
                            else
                            {
                                //не нашли кнопку
                                return;
                            }
                            Sleep(1, 2);
                        }
                        catch (Exception loadpostEx)
                        {
                            return;
                        }
                    }
                    #endregion


                    //бегаем по постам и сохраняем линки
                    while (currentPostIndex < posts.Count())
                    {
                        //фокусируемся на посте
                        var post = posts[currentPostIndex];
                        string login = string.Empty;

                        try
                        {
                            #region Scroll into photo
                            flag = false;
                            while (!flag)
                            {
                                flag = ScrollInto(post);
                            }
                            #endregion

                            #region Open photo
                            flag = false;
                            while (!flag)
                            {
                                flag = OpenPost(post);
                            }
                            #endregion

                            #region Like photo
                            LikePost();
                            #endregion

                            #region Get Login
                            int index = 0;
                            flag = false;
                            while (!flag && index < 20)
                            {
                                try
                                {
                                    var log = GetElement(By.XPath(".//a[@title][@href]"));
                                    login = log.Text;

                                    flag = true;
                                }
                                catch (Exception ex)
                                {
                                    flag = false;
                                }
                                index++;
                            }

                            #endregion

                            #region Follow

                            try
                            {
                                var buttons = GetElements(By.XPath(".//button"));
                                if (buttons != null && buttons.Any(btn => followText.Any(btn.Text.Contains)))
                                {
                                    var followButton = buttons.First(btn => followText.Any(btn.Text.Contains));
                                    ClickOn(followButton);
                                }
                                Sleep(1, 2);
                            }
                            catch (Exception followEx)
                            {
                                _logger.Log(LogLevel.Error,
                                    string.Format(
                                        "Follow(IWebElement element), Случилось что-то не хорошее во время подписки на профиль: {0}",
                                        followEx));
                            }

                            try
                            {
                                var buttons = GetElements(By.XPath(".//button"));
                                if (buttons != null && buttons.Any(btn => followedText.Any(btn.Text.Contains)))
                                    _logger.Log(LogLevel.Info, string.Format("Подписался на {0}", login));
                                else if (buttons != null && buttons.Any(btn => followRequestedText.Any(btn.Text.Contains)))
                                    _logger.Log(LogLevel.Info, string.Format("Запрос отправлен на {0}", login));
                                else
                                    _logger.Log(LogLevel.Info, string.Format("Пробовал подписаться на {0}", login));
                            }
                            catch (Exception logEx)
                            {
                            }

                            #endregion

                            #region Save Login
                            InstagramLogins.SaveLogin(login, ProfileId);
                            #endregion

                            #region Close photo
                            ClosePost();
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(LogLevel.Error, string.Format("FindUsersByHashTag --> бегаем по постам и сохраняем линки exception: {0}", ex));
                        }
                        currentPostIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("FindUsersByHashTag exception: {0}", ex));
            }

            _logger.Log(LogLevel.Info, string.Format("FindUsersByHashTag ended, hashtag: {0}", text));
        }

        public static void FindPostsByLocation(string location, int numberOfPosts)
        {
            _logger.Log(LogLevel.Info, string.Format("Starting FindPostsByLocation: {0}", location));

            Open(location);
            bool flag;

            var savedPostLinks = 0;
            var currentPostIndex = 10;

            try
            {
                while (savedPostLinks < numberOfPosts)
                {
                    var posts = GetElements(By.XPath($"//a[contains(@href, '?taken-at')]"));

                    #region если дошли до конца и на странице есть кнопка "Загрузить еще" жмем на нее
                    if (posts.Count() <= currentPostIndex)
                    {
                        try
                        {
                            var buttons = GetElements(By.XPath(".//a[@href]"));
                            string[] texttemp = { "Загрузить еще", "Load more" };
                            //link => text.Contains(link.Text)
                            if (buttons != null && buttons.Any(btn => texttemp.Any(btn.Text.Contains)))
                            {
                                //жмем на кнопку
                                var followButton = buttons.First(btn => texttemp.Any(btn.Text.Contains));
                                ClickOn(followButton);
                            }
                            else
                            {
                                //не нашли кнопку
                                return;
                            }
                            Sleep(1, 2);
                        }
                        catch (Exception loadpostEx)
                        {
                            return;
                        }
                    }
                    #endregion

                    //бегаем по постам и сохраняем линки
                    while (currentPostIndex < posts.Count())
                    {
                        try
                        {
                            //фокусируемся на посте
                            var post = posts[currentPostIndex];
                            string login = string.Empty;
                            var followed = false;

                            #region Scroll into photo
                            flag = false;
                            while (!flag)
                            {
                                flag = ScrollInto(post);
                            }
                            #endregion

                            #region Open photo
                            flag = false;
                            while (!flag)
                            {
                                flag = OpenPost(post);
                            }
                            #endregion

                            #region Like photo
                            LikePost();
                            #endregion

                            #region Get Login
                            int index = 0;
                            flag = false;
                            while (!flag && index < 20)
                            {
                                try
                                {
                                    var log = GetElement(By.XPath(".//a[@title][@href]"));
                                    login = log.Text;

                                    flag = true;
                                }
                                catch (Exception ex)
                                {
                                    flag = false;
                                }
                                index++;
                            }

                            #endregion

                            #region Save Login
                            InstagramLogins.SaveLogin(login, ProfileId);
                            #endregion

                            #region Follow

                            try
                            {
                                var buttons = GetElements(By.XPath(".//button"));
                                if (buttons != null && buttons.Any(btn => followText.Any(btn.Text.Contains)))
                                {
                                    var followButton = buttons.First(btn => followText.Any(btn.Text.Contains));
                                    ClickOn(followButton);
                                }
                                Sleep(1, 2);
                            }
                            catch (Exception followEx)
                            {
                                _logger.Log(LogLevel.Error,
                                    string.Format(
                                        "Follow(IWebElement element), Случилось что-то не хорошее во время подписки на профиль: {0}",
                                        followEx));
                            }

                            try
                            {
                                var buttons = GetElements(By.XPath(".//button"));
                                if (buttons != null && buttons.Any(btn => followedText.Any(btn.Text.Contains)))
                                {
                                    followed = true;
                                    _logger.Log(LogLevel.Info, string.Format("Подписался на {0}", login));
                                }
                                else if (buttons != null && buttons.Any(btn => followRequestedText.Any(btn.Text.Contains)))
                                {
                                    followed = true;
                                    _logger.Log(LogLevel.Info, string.Format("Запрос отправлен на {0}", login));
                                }
                                else
                                    _logger.Log(LogLevel.Info, string.Format("Пробовал подписаться на {0}", login));
                            }
                            catch (Exception logEx)
                            {
                            }

                            #endregion

                            #region Update status
                            if (followed) InstagramLogins.FollowLogin(login, ProfileId);
                            #endregion

                            #region Close photo
                            ClosePost();
                            #endregion
                        }
                        catch (Exception ex)
                        {
                            _logger.Log(LogLevel.Error, string.Format("FindPostsByLocation --> бегаем по постам и сохраняем линки, exception: {0}", ex));
                        }

                        currentPostIndex++;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("FindPostsByLocation exception: {0}", ex));
            }

            _logger.Log(LogLevel.Info, string.Format("FindPostsByLocation ended: {0}", location));
        }
        #endregion


        public static void Post(int id)
        {
            //get post data from db

            //interact with web page

            try
            {
                //click on icon
                var uploadIcon = GetElement(By.XPath($".//div[contains(@class, 'coreSpriteCameraInactive')]"));
                ClickOn(uploadIcon);

                var parent = uploadIcon.FindElement(By.XPath(".."));
                ClickOn(parent);

                var upload = GetElement(By.XPath($".//input[contains(@type, 'file')]"));
                upload.SendKeys(@"C:\\Users\\Askha\\Desktop\\Em9EmEMPL3k.jpg;");
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, string.Format("Post exception: {0}", ex));
            }

            //update post status in db
        }

        public static void Metrica(string profileLogin, int profileId)
        {
            _logger.Log(LogLevel.Info, string.Format("Metrica date {0}", DateTime.Now.ToString()));

            Open(string.Format("https://www.instagram.com/{0}/", profileLogin));
            Sleep(3, 3);
            int followers = 0, following = 0;

            try
            {
                #region Get number of following
                try
                {
                    string textRU = "Подписки";
                    var links = GetElements(By.XPath(".//ul/li/span"));
                    if (links != null && links.Any(link => link.Text.Contains(textRU)))
                    {
                        var followersLink = links.First(link => link.Text.Contains(textRU));
                        following = Convert.ToInt32(followersLink.Text.Replace(",", "").Replace(textRU, "").Replace(" ", ""));
                    }

                    string textENG = "following";
                    if (links != null && links.Any(link => link.Text.Contains(textENG)))
                    {
                        var followersLink = links.First(link => link.Text.Contains(textENG));
                        following = Convert.ToInt32(followersLink.Text.Replace(",", "").Replace(textENG, "").Replace(" ", ""));
                    }
                }
                catch (Exception exflw)
                {

                }
                #endregion

                #region Get number of followers
                try
                {
                    string textRU = "подписчиков";
                    var links = GetElements(By.XPath(".//ul/li/span"));
                    if (links != null && links.Any(link => link.Text.Contains(textRU)))
                    {
                        //k
                        //m
                        var followersLink = links.First(link => link.Text.Contains(textRU));
                        followers = Convert.ToInt32(followersLink.Text.Replace(",", "")
                                                                      .Replace(textRU, "")
                                                                      .Replace(" ", "")
                                                                      .Replace("k", "000")
                                                                      .Replace("m", "000000"));
                    }

                    string textENG = "followers";
                    if (links != null && links.Any(link => link.Text.Contains(textENG)))
                    {
                        var followersLink = links.First(link => link.Text.Contains(textENG));
                        followers = Convert.ToInt32(followersLink.Text.Replace(",", "")
                                                                      .Replace(textENG, "")
                                                                      .Replace(" ", "")
                                                                      .Replace("k", "000")
                                                                      .Replace("m", "000000"));
                    }
                }
                catch (Exception exflws)
                {

                }
                #endregion

                #region Save to db
                InstagramLogins.SaveMetrics(profileId, followers, following);
                #endregion
            }
            catch (Exception ex)
            {

            }
        }

        #region OLD METHODS

        public static void Unfollow_ScrollDown(int number)
        {
            //try catch
            Sleep(1, 3);
            _logger.Log(LogLevel.Info, string.Format("Starting UnfollowScrollDown number: {0}", number));

            for (int i = 0; i < number; i++)
            {
                Open("https://www.instagram.com/");
                ReadOnlyCollection<IWebElement> links = null;

                #region Open profile
                try
                {
                    _logger.Log(LogLevel.Trace, "Unfollow_ScrollDown(): Open profile");
                    links = GetElements(By.XPath(".//a[@href]"));
                    string[] profile = { "Профиль", "Profile" };
                    if (links != null && links.Any(link => profile.Any(link.Text.Contains)))
                    {
                        var profileLink = links.First(link => profile.Any(link.Text.Contains));
                        ClickOn(profileLink);
                    }
                    else
                    {
                        _logger.Log(LogLevel.Info, "Не нашел ссылку Профиль");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, "Open profile");
                    return;
                }
                Sleep(1, 2);
                #endregion

                #region Open following link
                try
                {
                    _logger.Log(LogLevel.Trace, "Кликаем на ссылку Подписки");
                    links = GetElements(By.XPath(".//ul/li/a[@href]"));
                    if (links != null && links.Any(link => followedText.Any(link.Text.Contains)))
                    {
                        var followersLink = links.First(link => followedText.Any(link.Text.Contains));
                        ClickOn(followersLink);
                    }
                    else
                    {
                        _logger.Log(LogLevel.Info, "Не нашел ссылку подписчики");
                        return;
                    }
                }
                catch (Exception ex)
                {
                    return;
                }
                Sleep(0.3M, 1);
                #endregion

                var users = UserFollowList(followedText);

                foreach (var user in users)
                {
                    ScrollInto(user);
                    Unfollow(user);
                    Sleep(0.5M, 1.3M);
                }
            }
        }

        /// <summary>
        /// Прокрутка подписчиков профайла, подписка (Scroll into)
        /// </summary>
        /// <param name="profileUrl">ссылка профайла</param>
        /// <param name="numberOfFollowers">количество подписчиков</param>
        public static void Follow_ScrollDown(string profileUrl, int numberOfFollowers)
        {
            _logger.Log(LogLevel.Info, string.Format("Starting FollowScrollDown numberOfFollowers: {0}", numberOfFollowers));

            Open(profileUrl);

            Sleep(1, 3);

            _stopWatch.Start();

            #region Open followers link
            _logger.Log(LogLevel.Trace, "Кликаем на ссылку подписчики");
            var links = GetElements(By.XPath(".//ul/li/a[@href]"));
            string[] text = { "подписчиков", "followers" };
            if (links != null && links.Any(link => text.Any(link.Text.Contains)))
            {
                var followersLink = links.First(link => text.Any(link.Text.Contains));
                ClickOn(followersLink);
            }
            else
            {
                _logger.Log(LogLevel.Info, "Не нашел ссылку подписчики");
                return;
            }

            Sleep(1, 3);
            #endregion

            _stopWatch.Stop();
            _logger.Log(LogLevel.Trace, string.Format("Open followers link took: {0}.{1}", _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds));

            var numberOfFollowedUsers = 0;
            var userIndex = 0;

            while (numberOfFollowers > numberOfFollowedUsers)
            {
                try
                {
                    var users = UserFollowList(followWords);

                    if (users.Count > userIndex)
                    {
                        _stopWatch.Restart();
                        #region Check 7 next profiles
                        var profiles = (userIndex + 7 < users.Count) ? 7 : ((users.Count != userIndex) ? users.Count - userIndex - 1 : 0);
                        var accelerateflag = true;
                        if (userIndex + profiles < users.Count)
                        {
                            for (int j = 0; j < profiles; j++)
                            {
                                if (!AlreadyFollowingUser(users[userIndex + j]) && !IsInDatabase(GetProfileLogin(users[userIndex + j])))
                                {
                                    accelerateflag = false;
                                    userIndex = userIndex + j;
                                    break;
                                }
                            }
                        }
                        #endregion
                        _stopWatch.Stop();

                        _logger.Log(LogLevel.Trace, string.Format("Check 7 next profiles (for scrolling): {0}.{1}.{2}",
                                    _stopWatch.Elapsed.Minutes, _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds));

                        //accelerate focus on element
                        if (accelerateflag && userIndex + profiles < users.Count)
                        {
                            _stopWatch.Restart();

                            userIndex = userIndex + profiles;
                            ScrollInto(users[userIndex]);

                            _stopWatch.Stop();
                            _logger.Log(LogLevel.Trace, "Scrolling... : {0}.{1}.{2}",
                                        _stopWatch.Elapsed.Minutes, _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds);
                        }
                        else
                        {
                            var element = users[userIndex];
                            ScrollInto(element);
                            userIndex++;

                            if (!AlreadyFollowingUser(element) && !IsInDatabase(GetProfileLogin(element)))
                            {
                                _stopWatch.Restart();

                                var client = element.FindElement(By.XPath(".//a[@title][@href]"));
                                var clientLogin = GetProfileLogin(element);
                                var url = client.GetAttribute("href");

                                Follow(url, clientLogin);
                                numberOfFollowedUsers++;

                                _stopWatch.Stop();
                                _logger.Log(LogLevel.Trace, "Follow : {0}.{1}.{2}",
                                    _stopWatch.Elapsed.Minutes, _stopWatch.Elapsed.Seconds, _stopWatch.Elapsed.Milliseconds);
                            }
                            else Sleep(0, 0.01M);
                        }
                    }
                    else
                    {
                        var footer = GetElement(By.TagName("footer"));
                        ClickOn(footer);
                        userIndex++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, string.Format("FollowScrollDown(numberOfFollowers : {0}) : {1}", numberOfFollowers, ex));
                }

            }

            Open("https://www.instagram.com/");

            _logger.Log(LogLevel.Info, string.Format("FollowScrollDown function ended number of followed users: {0}", numberOfFollowedUsers));
        }

        /// <summary>
        /// Прокрутка основной ленты (Liking)
        /// </summary>
        /// <param name="numberOfPost">Максимальное количество постов</param>
        public static void Like_ScrollDown(int numberOfPost)
        {
            var numberOfLikedPosts = 0;
            _logger.Log(LogLevel.Info, string.Format("Starting ScrollDown numberOfPost: {0}", numberOfPost));
            var articleIndex = 0;

            while (numberOfPost > numberOfLikedPosts)
            {
                try
                {
                    var articles = GetElements(By.TagName("article"));
                    if (articles.Count > articleIndex)
                    {
                        var element = articles[articleIndex];
                        ScrollInto(element);
                        articleIndex++;

                        //Завершить процесс лайкания если пост уже был лайкнут
                        if (!AlreadyLikedArticle(element))
                        {
                            if (LikePost())
                            {
                                numberOfLikedPosts++;
                                Sleep(1, 4);
                            }
                            else
                                Sleep(1, 4);
                        }
                    }
                    else
                    {
                        var footer = GetElement(By.TagName("footer"));
                        ClickOn(footer);
                        articleIndex++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.Error, string.Format("ScrollDown(numberOfPost : {0}) : {1}", numberOfPost, ex));
                }

            }
            _logger.Log(LogLevel.Info, string.Format("ScrollDown function ended number of Liked Posts: {0}", numberOfLikedPosts));
        }
        #endregion

    }
}
