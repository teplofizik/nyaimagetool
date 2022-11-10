using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpReadLinkEventArgs : SFtpEventArgs
    {
        public string Path;

        // Прочитанная ссылка
        public string Target;

        public SFtpReadLinkEventArgs(string Path)
        {
            this.Path = Path;
        }
    }

    /// <summary>
    /// Обработка события текста ссылки
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpReadLinkEventHandler(object sender, SFtpReadLinkEventArgs e);
}
