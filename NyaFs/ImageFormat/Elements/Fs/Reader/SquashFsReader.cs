using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class SquashFsReader : Reader
    {
        Filesystem.SquashFs.SquashFs Fs;

        public SquashFsReader(string Filename) : this(File.ReadAllBytes(Filename))
        {

        }

        public SquashFsReader(byte[] Data)
        {
            Fs = new Filesystem.SquashFs.SquashFs(Data);
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(LinuxFilesystem Dst)
        {

        }
    }
}
