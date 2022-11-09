using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpReadEventArgs : SFtpEventArgs
    {
        public string Path;
        public long Offset;
        public int Length;

        // Прочитанные данные
        public byte[] Data;

        public SFtpReadEventArgs(string Path, long Offset, int Length)
        {
            this.Path = Path;
            this.Offset = Offset;
            this.Length = Length;
        }
    }

    /// <summary>
    /// Обработка события получения данных объекта
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpReadEventHandler(object sender, SFtpReadEventArgs e);
}
