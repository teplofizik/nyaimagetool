using System;
using System.Collections.Generic;
using System.Text;

namespace FreeSFtpSharp.Events
{
    public class SFtpWriteEventArgs : SFtpEventArgs
    {
        public string Path;
        public long Offset;

        // Прочитанные данные
        public byte[] Data;

        public SFtpWriteEventArgs(string Path, long Offset, byte[] Data)
        {
            this.Path = Path;
            this.Offset = Offset;
            this.Data = Data;
        }
    }

    /// <summary>
    /// Обработка события записи данных объекта
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void SFtpWriteEventHandler(object sender, SFtpWriteEventArgs e);
}
