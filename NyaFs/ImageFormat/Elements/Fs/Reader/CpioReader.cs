using CpioLib.IO;
using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class CpioReader : Reader
    {
        CpioLib.Types.CpioArchive Archive;

        public CpioReader(string Filename)
        {
            Archive = CpioParser.Load(Filename);
        }

        public CpioReader(byte[] Data)
        {
            Archive = CpioParser.Load(Data);
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(LinuxFilesystem Dst)
        {
            foreach (var I in Archive.Files)
            {
                if(I.Path == ".")
                    ApplyCpioParams(Dst.Fs.Root, I);
                else
                {
                    //var Parent = Dst.GetParentDir();
                    var Dir = Dst.GetParentDirectory(I.Path);

                    if (Dir == null)
                        throw new InvalidOperationException($"Parent dir for {I.Path} not found");

                    switch (I.FileType)
                    {
                        case CpioLib.Types.CpioModeFileType.C_ISDIR: // Папка
                            {
                                var SubDir = new Filesystem.Universal.Items.Dir(I.Path, I.UserId, I.GroupId, I.HexMode);
                                ApplyCpioParams(SubDir, I);
                                Dir.Items.Add(SubDir);
                                //Console.WriteLine($"Added dir {I.Path}");
                                break;
                            }
                        case CpioLib.Types.CpioModeFileType.C_ISREG: // Файл
                            {
                                var File = new Filesystem.Universal.Items.File(I.Path, I.UserId, I.GroupId, I.HexMode, I.Content);
                                ApplyCpioParams(File, I);
                                Dir.Items.Add(File);
                                break;
                            }
                        case CpioLib.Types.CpioModeFileType.C_ISLNK: // Ссылка
                            {
                                var Target = UTF8Encoding.UTF8.GetString(I.Content);
                                var Link = new Filesystem.Universal.Items.SymLink(I.Path, I.UserId, I.GroupId, I.HexMode, Target);
                                ApplyCpioParams(Link, I);
                                Dir.Items.Add(Link);
                                //Console.WriteLine($"Added symlink {I.Path} to {Target}");
                                break;
                            }
                        case CpioLib.Types.CpioModeFileType.C_ISCHR: // NOD
                            {
                                var Node = new Filesystem.Universal.Items.Char(I.Path, I.UserId, I.GroupId, I.HexMode);
                                Node.Major = I.Major;
                                Node.Minor = I.Minor;
                                Node.RMajor = I.RMajor;
                                Node.RMinor = I.RMinor;
                                ApplyCpioParams(Node, I);
                                Dir.Items.Add(Node);
                                //Console.WriteLine($"Added node {I.Path}");
                                break;
                            }
                        case CpioLib.Types.CpioModeFileType.C_ISBLK: // Block
                            {
                                var Block = new Filesystem.Universal.Items.Block(I.Path, I.UserId, I.GroupId, I.HexMode);
                                Block.Major = I.Major;
                                Block.Minor = I.Minor;
                                Block.RMajor = I.RMajor;
                                Block.RMinor = I.RMinor;
                                ApplyCpioParams(Block, I);
                                Dir.Items.Add(Block);
                                break;
                            }
                        case CpioLib.Types.CpioModeFileType.C_ISFIFO: // FIFO
                            {
                                var Fifo = new Filesystem.Universal.Items.Fifo(I.Path, I.UserId, I.GroupId, I.HexMode);
                                ApplyCpioParams(Fifo, I);
                                Dir.Items.Add(Fifo);
                                break;
                            }
                        default:
                            //Console.WriteLine($"{I} : {I.FileType}");
                            throw new NotImplementedException($"CPIO node type {I.FileType} is not implemented in embedded NyaFs");
                    }
                }
            }
            Dst.FilesystemType = Types.FsType.Cpio;
        }

        private void ApplyCpioParams(Filesystem.Universal.FilesystemItem Item, CpioLib.Types.CpioNode Node)
        {
            Item.Mode = Node.HexMode;
            Item.User = Node.UserId;
            Item.Group = Node.GroupId;

            Item.Modified = ConvertFromUnixTimestamp(Node.ModificationTime);
        }
    }
}
