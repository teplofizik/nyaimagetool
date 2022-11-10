using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsLinux.Linux.Users
{
    class User
    {
        /// <summary>
        /// User id
        /// </summary>
        public int UID;

        /// <summary>
        /// User name
        /// </summary>
        public string Name = null;

        /// <summary>
        /// User name
        /// </summary>
        public string Home = null;

        /// <summary>
        /// Default shell
        /// </summary>
        public string Shell = null;

        /// <summary>
        /// Has encrypted password
        /// </summary>
        private bool EncryptedPassword = false;

        /// <summary>
        /// Comment field
        /// </summary>
        private string Comment = null;

        /// <summary>
        /// Primary group id
        /// </summary>
        public int GID;

        /// <summary>
        /// Passhowd Hash
        /// </summary>
        public string Hash = null;

        /// <summary>
        /// Is user allowed to shell
        /// </summary>
        public bool IsAllowConsole
        {
            get
            {
                if (Shell == "/bin/false")
                    return false;

                return (Shell == "/bin/sh") || (Shell == "/bin/bash");
            }
        }

        /// <summary>
        /// Create user information by passwd line
        /// </summary>
        /// <param name="PasswdLine"></param>
        public User(string PasswdLine)
        {
            // root:x:0:0:root:/root:/bin/sh
            var Parts = PasswdLine.Split(new char[] { ':' });

            if (Parts.Length == 7)
            {
                // Known format
                Name = Parts[0];
                EncryptedPassword = Parts[1] == "x";
                UID = Convert.ToInt32(Parts[2]);
                GID = Convert.ToInt32(Parts[3]);
                Comment = Parts[4];
                Home = Parts[5];
                Shell = Parts[6];
            }
            else
                throw new ArgumentException("Unknown passwd format");
        }

        /// <summary>
        /// Create user information by id/name
        /// </summary>
        /// <param name="UID">User id</param>
        /// <param name="Name">User name</param>
        public User(int UID, string Name)
        {
            this.UID = UID;
            this.Name = Name;
        }

        public void ProcessShadow(string Shadow)
        {
            var Parts = Shadow.Split(new char[] { ':' });

            if (Parts.Length == 9)
            {
                var UserName = Parts[0];
                if (Name == UserName)
                {
                    Hash = Parts[1];
                    /*
                    var LastChangedDay = Convert.ToInt32(Parts[2]);
                    var MinimumDaysRequiredToChange = Convert.ToInt32(Parts[3]);
                    var MaximumDaysNeedToChange = Convert.ToInt32(Parts[4]);
                    var PasswordWarningDaysCount = Convert.ToInt32(Parts[5]);
                    var PasswordInactiveDays = Parts[6]; // empty
                    var PasswordExpireDays = Parts[7]; // empty
                    */
                }
            }
        }

        /// <summary>
        /// Salt applied to hash
        /// </summary>
        public string Salt => ((Hash != null) && (Hash.Length > 10)) ? Hash.Split(new char[] { '$' })[2] : null;

        /// <summary>
        /// Hash value
        /// </summary>
        public string HashValue => (Hash != null) ? Hash.Split(new char[] { '$' })[3] : null;

        //User has no any password
        public bool NoPassword => (Hash == "*") || (Hash == "!");

        /// <summary>
        /// Type of hash
        /// </summary>
        public string HashType
        {
            get
            {
                if ((Hash != null) && (Hash.Length > 3))
                {
                    var Type = Hash.Substring(0, 3);
                    switch(Type)
                    {
                        case "$1$": return "MD5";
                        case "$2a$": return "Blowfish";
                        case "$2y$": return "Blowfish";
                        case "$5$": return "SHA-256";
                        case "$6$": return "SHA-512";
                        default: return "Unknown";
                    }
                }
                else
                    return "Unknown";
            }
        }

        public bool CheckPassword(string Password)
        {
            if(Hash.Length > 4)
            {
                var TSalt = Hash.Substring(0, 3) + Salt;
                return ManagedUnixCrypt.Crypt(Password, TSalt) == Hash;
            }

            return false;
        }
    }
}
