using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpMakeDirEventArgs : SFtpEventArgs
    {
        public string Path;

        public SFtpMakeDirEventArgs(string Path)
        {
            this.Path = Path;
        }
    }

    /// <summary>
    /// Обработка события создания директории
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpMakeDirEventHandler(object sender, SFtpMakeDirEventArgs e);
}
