using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Permissions;
using System.Text;
using System.Threading.Tasks;

namespace VlibraryServer
{
    internal class TimeObj
    {
        private int loginTries;
        private DateTime lastLoginTime;
        private bool isBanned;
        public TimeObj()
        {
            loginTries = 0;
            lastLoginTime = DateTime.Now;
            isBanned = false;
        }
        public bool getBanned()
        {
            return isBanned;
        }
        public void SetLoginTries(int n)
        {
            loginTries = n;
        }
        public void ChangeBanned()
        {
            if(isBanned)
            {
                isBanned = false;
            }
            else if(!isBanned)
            {
                isBanned=true;
            }
        }
        public void AddTry()
        {
            loginTries++;
        }
        public int GetLoginTries()
        {
            return loginTries;
        }
    }
}
