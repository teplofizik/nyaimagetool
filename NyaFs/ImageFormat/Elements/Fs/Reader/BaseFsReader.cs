using System;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class BaseFsReader : Reader
    {
        Filesystem.Universal.IFilesystemReader FsReader;
        Types.FsType FsType;

        public BaseFsReader(Types.FsType FsType, Filesystem.Universal.IFilesystemReader FsReader)
        {
            this.FsType = FsType;
            this.FsReader = FsReader;
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(LinuxFilesystem Dst)
        {
            ImportDir(Dst.Fs.Root, ".");

            if (Dst.Info.Type == Types.ImageType.IH_TYPE_INVALID)
                Dst.Info.Type = Types.ImageType.IH_TYPE_RAMDISK;

            Dst.FilesystemType = FsType;
        }

        private void ImportDir(Filesystem.Universal.Items.Dir Dir, string Path)
        {
            var Elements = FsReader.ReadDir(Path);

            foreach (var E in Elements)
            {
                switch (E.NodeType)
                {
                    case Filesystem.Universal.Types.FilesystemItemType.Directory:
                        {
                            var SubDir = new Filesystem.Universal.Items.Dir(E.Path, E.User, E.Group, E.HexMode);
                            Dir.Items.Add(SubDir);
                            ImportDir(SubDir, E.Path);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.File:
                        {
                            var Data = FsReader.Read(E.Path);
                            var File = new Filesystem.Universal.Items.File(E.Path, E.User, E.Group, E.HexMode, Data);
                            Dir.Items.Add(File);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.SymLink:
                        {
                            var Target = UTF8Encoding.UTF8.GetString(FsReader.Read(E.Path));
                            var Link = new Filesystem.Universal.Items.SymLink(E.Path, E.User, E.Group, E.HexMode, Target);
                            Dir.Items.Add(Link);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Character:
                        {
                            var Node = new Filesystem.Universal.Items.Node(E.Path, E.User, E.Group, E.HexMode);
                            Dir.Items.Add(Node);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Fifo:
                        {
                            var Fifo = new Filesystem.Universal.Items.Fifo(E.Path, E.User, E.Group, E.HexMode);
                            Dir.Items.Add(Fifo);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.Block:
                        {
                            var Block = new Filesystem.Universal.Items.Block(E.Path, E.User, E.Group, E.HexMode);
                            Dir.Items.Add(Block);
                        }
                        break;
                }
            }
        }
    }
}