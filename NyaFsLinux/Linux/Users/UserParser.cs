using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsLinux.Linux.Users
{
    static class UserParser
    {
        public static User[] ParseUsers(string[] passwd, string[] shadow)
        {
            var Res = new List<User>();
            foreach(var l in passwd)
            {
                if (l.Length < 5) continue;

                var U = new User(l);
                
                // Apply shadow line
                if(shadow != null)
                {
                    var sh = GetShadowString(U.Name, shadow);
                    if(sh != null)
                        U.ProcessShadow(sh);
                }

                Res.Add(U);
            }

            return Res.ToArray();
        }

        private static string GetShadowString(string Name, string[] shadow)
        {
            foreach(var l in shadow)
            {
                if (l.StartsWith($"{Name}:"))
                    return l;
            }

            return null;
        }
    }
}
