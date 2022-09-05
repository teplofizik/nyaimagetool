using Extension.Array;
using Extension.Packet;
using NyaFs.Filesystem.Universal;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Cpio
{
    class CpioFsReader : RawPacket, Universal.IFilesystemReader
    {
        private List<Types.CpioNode> Nodes = new List<Types.CpioNode>();

        public CpioFsReader(byte[] Data) : base(Data) { Init(); }

        public CpioFsReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }

        private void Init()
        {
            long Offset = 0;
            while (Offset < Data.Length)
            {
                var FI = new Types.CpioFileInfo(Data, Offset);

                if (FI.IsCorrectMagic)
                {
                    var Raw = Data.ReadArray(Offset, FI.FullFileBlockSize);
                    var F = new Types.CpioNode(Raw);

                    if (!FI.IsTrailer)
                    {
                        Nodes.Add(F);

                        Offset += FI.FullFileBlockSize;
                    }
                }
                else
                    break;
            }
        }

        private string UnifyPath(string Path)
        {
            if (Path.Length > 0)
                return (Path[0] == '/') ? Path.Substring(1) : Path;
            else
                return Path;
        }

        byte[] IFilesystemReader.Read(string Path)
        {
            foreach (var N in Nodes)
            {
                if (UnifyPath(N.Path) == UnifyPath(Path))
                    return N.Content;
            }

            return null;
        }

        FilesystemEntry[] IFilesystemReader.ReadDir(string Path)
        {
            var Res = new List<FilesystemEntry>();
            Path = UnifyPath(Path);
            foreach (var N in Nodes)
            {
                var UPath = UnifyPath(N.Path);
                int Pos = UPath.IndexOf(Path);
                if(Pos == 0)
                {
                    Pos = Pos + Path.Length + 1;
                    if(UPath.IndexOf('/', Pos) < 0)
                    {
                        Res.Add(new FilesystemEntry(N.FsType, UPath, N.UserId, N.GroupId, N.HexMode, Convert.ToUInt32(N.Content.Length)));
                    }
                }
            }
            return Res.ToArray();
        }
    }
}
