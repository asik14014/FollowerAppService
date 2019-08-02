using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramModels
{
    public enum FollowStatus
    {
        Followed,
        Requested,
        Tried, //Возможно Instagram заблокировал доступ на подписку (временно)
        Unknown
    }
}
