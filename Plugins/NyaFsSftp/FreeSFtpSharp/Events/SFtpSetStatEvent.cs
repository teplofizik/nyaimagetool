using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpSetStatEventArgs : SFtpEventArgs
    {
        public string Path;

        public uint? Mode = null;
        public uint? AccessTime = null;

        public uint? UID = null;
        public uint? GID = null;

        public SFtpSetStatEventArgs(string Path)
        {
            this.Path = Path;
        }
    }

    /// <summary>
    /// Обработка события задания свойств объекта
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpSetStatEventHandler(object sender, SFtpSetStatEventArgs e);
}
