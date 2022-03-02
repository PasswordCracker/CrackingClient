using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClientCracking
{
    public class UserInfo
    {
        public string Username
        {
            get;set;
        }
        public string HashedPassword
        {
            get;set;
        }
        public string ClearTextPassword
        {
            get;set;
        }
    }
}
