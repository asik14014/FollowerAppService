using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramModels
{
    public class User
    {
        /// <summary>
        /// Логин пользователя
        /// </summary>
        public string Login { get; set; }

        /// <summary>
        /// Бизнес страница
        /// </summary>
        public bool IsBusiness { get; set; }

        /// <summary>
        /// Удаленная страница
        /// </summary>
        public bool IsDeleted { get; set; }
        
        /// <summary>
        /// Отписаны от пользователя
        /// </summary>
        public bool IsUnfollowed { get; set; }

        /// <summary>
        /// Запрос на подписку отправлен/подписан
        /// </summary>
        public bool IsFollowRequested { get; set; }

        /// <summary>
        /// Дата запроса на подписку
        /// </summary>
        public DateTime RequestDate { get; set; }

        /// <summary>
        /// Дата сохранения логина в базу
        /// </summary>
        public DateTime SavedDate { get; set; }

        /// <summary>
        /// Дата отписки от пользователя
        /// </summary>
        public DateTime UnfollowDate { get; set; }

        public User()
        {
        }

        public User(string login, 
                    bool isBusiness, 
                    bool isDeleted,
                    bool isFollowRequested, 
                    DateTime requestDate,
                    bool isUnfollowed,
                    DateTime unfollowDate,
                    DateTime savedDate)
        {
            Login = login;
            IsBusiness = isBusiness;
            IsDeleted = isDeleted;
            IsFollowRequested = isFollowRequested;
            RequestDate = requestDate;
            IsUnfollowed = isUnfollowed;
            UnfollowDate = unfollowDate;
            SavedDate = savedDate;
        }
    }
}
