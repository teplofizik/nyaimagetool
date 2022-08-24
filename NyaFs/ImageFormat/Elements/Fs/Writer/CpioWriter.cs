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

        public override void WriteFs(Filesystem Fs)
        {
            Archive = new CpioLib.Types.CpioArchive();
            Archive.Trailer = new CpioLib.Types.Nodes.CpioTrailer();

            ProcessDirectory(Fs.Root);

            var Data = CpioLib.IO.CpioPacker.GetRawData(Archive);
            if (Filename != null)
            {
                CpioData = null;
                System.IO.File.WriteAllBytes(Filename, Data);
            }
            else
                CpioData = Data;
        }

        private void ProcessDirectory(Items.Dir Dir)
        {
            foreach (var I in Dir.Items)
            {
                switch (I.ItemType)
                {
                    case Types.FilesystemItemType.Dir:
                        {
                            var N = Archive.AddDir(I.Filename, Convert.ToUInt32((I as Items.Dir).Items.Count));
                            SetParamsToCpioNode(I, N);
                            ProcessDirectory(I as Items.Dir);
                        }
                        break;
                    case Types.FilesystemItemType.File:
                        {
                            var N = Archive.AddFile(I.Filename, DateTime.Now, (I as Items.File).Content);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                    case Types.FilesystemItemType.SymLink:
                        {
                            var N = Archive.AddSLink(I.Filename, (I as Items.SymLink).Target);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                    case Types.FilesystemItemType.Node:
                        {
                            var N = Archive.AddNod(I.Filename, I.RMajor, I.RMinor);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                    case Types.FilesystemItemType.Block:
                        {
                            var N = Archive.AddBlock(I.Filename, I.RMajor, I.RMinor);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                    case Types.FilesystemItemType.Fifo:
                        {
                            var N = Archive.AddFifo(I.Filename, I.RMajor, I.RMinor);
                            SetParamsToCpioNode(I, N);
                        }
                        break;
                }
            }
        }

        private void SetParamsToCpioNode(FilesystemItem Item, CpioLib.Types.CpioNode Node)
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
