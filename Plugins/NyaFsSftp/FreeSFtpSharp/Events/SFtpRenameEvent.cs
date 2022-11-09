using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpRenameEventArgs : SFtpEventArgs
    {
        public string OldPath;
        public string NewPath;

        public SFtpRenameEventArgs(string OldPath, string NewPath)
        {
            this.OldPath = OldPath;
            this.NewPath = NewPath;
        }
    }

    /// <summary>
    /// Обработка события переименования
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpRenameEventHandler(object sender, SFtpRenameEventArgs e);
}
