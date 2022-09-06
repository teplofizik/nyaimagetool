using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class CpioWriter : Writer
    {
        string Filename = null;
        byte[] CpioData = null;

        CpioLib.Types.CpioArchive Archive;

        public CpioWriter()
        {

        }

        public CpioWriter(string Filename)
        {
            this.Filename = Filename;
        }

        public override void WriteFs(LinuxFilesystem Fs)
        {
            Archive = new CpioLib.Types.CpioArchive();
            Archive.Trailer = new CpioLib.Types.Nodes.CpioTrailer();

            ProcessDirectory(Fs.Fs.Root);

            var Data = CpioLib.IO.CpioPacker.GetRawData(Archive);
            if (Filename != null)
            {
                CpioData = null;
                System.IO.File.WriteAllBytes(Filename, Data);
            }
            else
                CpioData = Data;
        }

        private void ProcessDirectory(Filesystem.Universal.Items.Dir Dir)
        {
            foreach (var I in Dir.Items)
            {
                switch (I.ItemType)
                {
                    case Filesystem.Universal.Types.FilesystemItemType.Directory:
                        {
                            var N = Archive.AddDir(I.Filename, Convert.ToUInt32((I as Filesystem.Universal.Items.Dir).Items.Count));
                            SetParamsToCpioNode(I, N);
                            ProcessDirectory(I as Filesystem.Universal.Items.Dir);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.File:
                        {
                            var N = Archive.AddFile(I.Filename, DateTime.Now, (I as Filesystem.Universal.Items.File).Content);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.SymLink:
                        {
                            var N = Archive.AddSLink(I.Filename, (I as Filesystem.Universal.Items.SymLink).Target);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Character:
                        {
                            var C = I as Filesystem.Universal.Items.Char;
                            var N = Archive.AddNod(I.Filename, C.RMajor, C.RMinor);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Block:
                        {
                            var B = I as Filesystem.Universal.Items.Char;
                            var N = Archive.AddBlock(I.Filename, B.RMajor, B.RMinor);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Fifo:
                        {
                            var N = Archive.AddFifo(I.Filename);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                }
            }
        }

        private void SetParamsToCpioNode(Filesystem.Universal.FilesystemItem Item, CpioLib.Types.CpioNode Node)
        {
            Node.HexMode = Item.Mode;
            Node.UserId = Item.User;
            Node.GroupId = Item.Group;
            Node.ModificationTime = ConvertToUnixTimestamp(Item.Modified);
        }

        public override bool HasRawStreamData => (CpioData != null);

        public override byte[] RawStream => CpioData;
    }
}
