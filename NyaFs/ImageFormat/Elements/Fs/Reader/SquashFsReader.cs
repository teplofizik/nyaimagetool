using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class SquashFsReader : BaseFsReader
    {
        public SquashFsReader(string Filename) : this(File.ReadAllBytes(Filename))
        {

        }

        public SquashFsReader(byte[] Data) : base(Types.FsType.SquashFs, new Filesystem.SquashFs.SquashFsReader(Data)) { }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(LinuxFilesystem Dst)
        {
            base.ReadToFs(Dst);

            var Reader = FsReader as Filesystem.SquashFs.SquashFsReader;
            var Compression = Reader.Compression;
            if ((Compression == Filesystem.SquashFs.Types.SqCompressionType.Xz) ||
               (Compression == Filesystem.SquashFs.Types.SqCompressionType.Lzo))
            {
                Dst.SquashFsCompression = Filesystem.SquashFs.Types.SqCompressionType.Gzip;
                Log.Warning(0, "Compression of loaded squashfs filesystem can not be used on writing. Compression is changed to default gzip compression.");
            }
            else
                Dst.SquashFsCompression = Reader.Compression;
        }
    }
}
