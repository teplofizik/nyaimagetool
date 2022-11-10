using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpMakeLinkEventArgs : SFtpEventArgs
    {
        public string Path;

        // Прочитанная ссылка
        public string Target;

        public SFtpMakeLinkEventArgs(string Path, string Target)
        {
            this.Path = Path;
            this.Target = Target;
        }
    }

    /// <summary>
    /// Обработка события создания ссылки
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpMakeLinkEventHandler(object sender, SFtpMakeLinkEventArgs e);
}
