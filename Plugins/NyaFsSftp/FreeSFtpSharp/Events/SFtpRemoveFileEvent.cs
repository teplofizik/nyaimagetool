using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpRemoveFileEventArgs : SFtpEventArgs
    {
        public string Filename;

        public SFtpRemoveFileEventArgs(string Filename)
        {
            this.Filename = Filename;
        }
    }

    /// <summary>
    /// Обработка события удаления файла
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpRemoveFileEventHandler(object sender, SFtpRemoveFileEventArgs e);
}
