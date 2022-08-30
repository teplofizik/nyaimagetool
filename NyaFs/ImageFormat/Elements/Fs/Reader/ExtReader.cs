using System;
using System.IO;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class ExtReader : Reader
    {
        NyaExt2.Implementations.Ext2Fs Fs;

        public ExtReader(string Filename) : this(System.IO.File.ReadAllBytes(Filename)) { }

        public ExtReader(byte[] data)
        {
            Fs = new NyaExt2.Implementations.Ext2Fs(data);
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(Filesystem Dst)
        {
            DumpDir(Dst.Root, ".");
        }

        private void DumpDir(Items.Dir Dir, string Path)
        {
            var Elements = Fs.ReadDir(Path);

            foreach (var E in Elements)
            {
                //Console.WriteLine(E);

                switch (E.NodeType)
                {
                    case NyaExt2.Types.FilesystemEntryType.Directory:
                        {
                            var SubDir = new Items.Dir(E.Path, E.User, E.Group, E.HexMode);
                            Dir.Items.Add(SubDir);
                            DumpDir(SubDir, E.Path);
                        }
                        break;
                    case NyaExt2.Types.FilesystemEntryType.Regular:
                        {
                            var File = new Items.File(E.Path, E.User, E.Group, E.HexMode, Fs.Read(E.Path));
                            Dir.Items.Add(File);
                        }
                        break;
                    case NyaExt2.Types.FilesystemEntryType.Link:
                        {
                            var Target = UTF8Encoding.UTF8.GetString(Fs.Read(E.Path));
                            var Link = new Items.SymLink(E.Path, E.User, E.Group, E.HexMode, Target);
                            Dir.Items.Add(Link);
                        }
                        break;
                    case NyaExt2.Types.FilesystemEntryType.Character:
                        {
                            var Node = new Items.Node(E.Path, E.User, E.Group, E.HexMode);
                            Dir.Items.Add(Node);
                        }
                        break;
                    case NyaExt2.Types.FilesystemEntryType.Fifo:
                        {
                            var Fifo = new Items.Fifo(E.Path, E.User, E.Group, E.HexMode);
                            Dir.Items.Add(Fifo);
                        }
                        break;
                    case NyaExt2.Types.FilesystemEntryType.Block:
                        {
                            var Block = new Items.Block(E.Path, E.User, E.Group, E.HexMode);
                            Dir.Items.Add(Block);
                        }
                        break;
                }
            }
        }
    }
}