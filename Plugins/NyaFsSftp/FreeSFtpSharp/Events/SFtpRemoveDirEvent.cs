using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpRemoveDirEventArgs : SFtpEventArgs
    {
        public string Path;

        public SFtpRemoveDirEventArgs(string Path)
        {
            this.Path = Path;
        }
    }

    /// <summary>
    /// Обработка события удаления директории
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpRemoveDirEventHandler(object sender, SFtpRemoveDirEventArgs e);
}
