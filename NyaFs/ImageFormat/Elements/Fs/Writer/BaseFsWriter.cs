using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class BaseFsWriter : Writer
    {
        string Filename = null;
        byte[] FsData = null;
        Filesystem.Universal.IFilesystemBuilder Builder;

        public BaseFsWriter(Filesystem.Universal.IFilesystemBuilder Builder)
        {
            this.Builder = Builder;
        }

        public BaseFsWriter(Filesystem.Universal.IFilesystemBuilder Builder, string Filename)
        {
            this.Builder = Builder;
            this.Filename = Filename;
        }

        public override void WriteFs(LinuxFilesystem Fs)
        {
            ProcessDirectory(Fs.Fs.Root);

            var Data = Builder.GetFilesystemImage();
            if (Filename != null)
            {
                FsData = null;
                System.IO.File.WriteAllBytes(Filename, Data);
            }
            else
                FsData = Data;
        }

        private void ProcessDirectory(Filesystem.Universal.Items.Dir Dir)
        {
            foreach (var I in Dir.Items)
            {
                switch (I.ItemType)
                {
                    case Filesystem.Universal.Types.FilesystemItemType.Directory:
                        Builder.Directory(I.Filename, I.User, I.Group, I.Mode);
                        ProcessDirectory(I as Filesystem.Universal.Items.Dir);
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.File:
                        Builder.File(I.Filename, (I as Filesystem.Universal.Items.File).Content, I.User, I.Group, I.Mode);
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.SymLink:
                        Builder.SymLink(I.Filename, (I as Filesystem.Universal.Items.SymLink).Target, I.User, I.Group, I.Mode);
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Character:
                        {
                            var C = I as Filesystem.Universal.Items.Char;
                            Builder.Char(I.Filename, C.Major, C.Minor, I.User, I.Group, I.Mode);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Block:
                        {
                            var B = I as Filesystem.Universal.Items.Block;
                            Builder.Block(I.Filename, B.Major, B.Minor, I.User, I.Group, I.Mode);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Fifo:
                        Builder.Fifo(I.Filename, I.User, I.Group, I.Mode);
                        break;
                }
            }
        }

        public override bool HasRawStreamData => (FsData != null);

        public override byte[] RawStream => FsData;
    }
}
