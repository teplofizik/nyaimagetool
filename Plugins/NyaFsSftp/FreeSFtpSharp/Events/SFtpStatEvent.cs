using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpStatEventArgs : SFtpEventArgs
    {
        public string Path;
        public bool FollowSymLink;

        // Информация о файле (возвращаемая в сервис)
        public Types.SFtpFsEntry Entry;

        public SFtpStatEventArgs(string Path, bool FollowSymLink)
        {
            this.Path = Path;
            this.FollowSymLink = FollowSymLink;
        }
    }

    /// <summary>
    /// Обработка события получения свойств объекта
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpStatEventHandler(object sender, SFtpStatEventArgs e);
}
