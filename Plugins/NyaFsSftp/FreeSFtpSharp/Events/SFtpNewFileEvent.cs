using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpNewFileEventArgs : SFtpEventArgs
    {
        public string Path;

        public SFtpNewFileEventArgs(string Path)
        {
            this.Path = Path;
        }
    }

    /// <summary>
    /// Обработка события создания директории
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpNewFileEventHandler(object sender, SFtpNewFileEventArgs e);
}
