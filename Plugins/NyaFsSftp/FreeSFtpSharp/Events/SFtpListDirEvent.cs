using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpListDirEventArgs : SFtpEventArgs
    {
        public string Path;

        public List<Types.SFtpFsEntry> Entries = new List<Types.SFtpFsEntry>();

        public SFtpListDirEventArgs(string Path)
        {
            this.Path = Path;
        }
    }

    /// <summary>
    /// Обработка события получения списка файлов папки
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpListDirEventHandler(object sender, SFtpListDirEventArgs e);
}

