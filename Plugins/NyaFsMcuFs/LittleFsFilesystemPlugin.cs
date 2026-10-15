using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Elements.Fs.Reader;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.ImageFormat.Types;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFsMcuFs
{
    /// <summary>
    /// littlefs v2 filesystem plugin (read). Detects the "littlefs" magic at offset 8.
    /// </summary>
    public class LittleFsFilesystemPlugin : FilesystemPlugin
    {
        public LittleFsFilesystemPlugin() : base("fs.littlefs", "littlefs", true, true, false) { }

        public override bool DetectFilesystem(ImageData Image)
        {
            if (Image.Length < 16) return false;

            return (Image.ReadByte(8) == (byte)'l') && (Image.ReadByte(9) == (byte)'i') &&
                   (Image.ReadByte(10) == (byte)'t') && (Image.ReadByte(11) == (byte)'t') &&
                   (Image.ReadByte(12) == (byte)'l') && (Image.ReadByte(13) == (byte)'e') &&
                   (Image.ReadByte(14) == (byte)'f') && (Image.ReadByte(15) == (byte)'s');
        }

        public override Reader GetFilesystemReader(byte[] Data)
        {
            var R = new LittleFsReader(Data);
            return new BaseFsReader(FsType.LittleFs, R, R.TotalSize);
        }

        public override NyaFs.ImageFormat.Elements.Fs.Writer.Writer GetFilesystemWriter(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs)
        {
            const int BlockSize = 4096;
            long BlockCount = (Fs.FsSize > 0) ? (Fs.FsSize / BlockSize) : 0;
            return new NyaFs.ImageFormat.Elements.Fs.Writer.BaseFsWriter(new LittleFsBuilder(BlockSize, BlockCount));
        }
    }
}
