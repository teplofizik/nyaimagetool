using CpioLib.Types.Nodes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace CpioLib.Types
{
    public class CpioArchive
    {
        public CpioNode Trailer = null;
        public List<CpioNode> Files = new List<CpioNode>();

        public CpioArchive()
        {

        }

        public bool Exists(string Filename)
        {
            foreach (var F in Files)
            {
                if (F.Path == Filename)
                    return true;
            }
            return false;
        }

        public CpioNode GetFile(string Filename)
        {
            foreach (var F in Files)
            {
                if (F.Path == Filename)
                    return F;
            }
            return null;
        }

        public CpioNode[] GetFiles(string Path)
        {
            var Res = new List<CpioNode>();
            foreach (var F in Files)
            {
                if (F.Path.StartsWith(Path))
                    Res.Add(F);
            }
            return Res.ToArray();
        }

        protected string[] ProcessFiles(string Path, Func<CpioNode,bool,bool> Action)
        {
            var Res = new List<string>();
            if (Path.Last() == '/')
            {
                var Files = GetFiles(Path);
                foreach (var F in Files)
                {
                    var ARes = Action?.Invoke(F, true);
                    if (ARes.HasValue && ARes.Value)
                        Res.Add(F.Path);
                }

            }
            else
            {
                var F = GetFile(Path);
                if (F != null)
                {
                    var ARes = Action?.Invoke(F, false);
                    if (ARes.HasValue && ARes.Value)
                        Res.Add(F.Path);
                }
            }
            return Res.ToArray();
        }

        public string[] DeleteFolder(string Filename)
        {
            var List = Array.ConvertAll(Files.Where(F => (F.Path.StartsWith(Filename))).ToArray(), F => F.Path);

            Files.RemoveAll(F => (F.Path.StartsWith(Filename)));
            return List;
        }

        public void Delete(string Filename)
        {
            Files.RemoveAll(F => (F.Path == Filename));
        }

        public string[] ChMod(string Filename, UInt32 Mode)
        {
            return ProcessFiles(Filename, (F,List) =>
                {
                    if ((F.FileType == CpioModeFileType.C_ISREG) || !List)
                    {
                        // rwxr-x---
                        // var OldMode = F.Mode & ~0x1FFU;

                        // OldMode |= (Mode & 0x7);
                        // OldMode |= ((Mode >> 4) & 0x7) << 3;
                        // OldMode |= ((Mode >> 8) & 0x7) << 6;

                        // F.Mode = OldMode;
                        F.HexMode = Mode;
                        return true;
                    }
                    else
                        return false;
                });
        }

        public string[] ChOwn(string Filename, UInt32 Uid)
        {
            return ProcessFiles(Filename, (F, List) => { F.UserId = Uid; return true; });
        }

        public string[] ChGroup(string Filename, UInt32 Gid)
        {
            return ProcessFiles(Filename, (F, List) => { F.GroupId = Gid; return true; });
        }

        public CpioDir AddDir(string Filename, uint Links = 1u)
        {
            var D = new CpioDir(Filename, Links);
            Files.Add(D);
            return D;
        }

        public CpioDir AddDir(string Filename, string LocalPath)
        {
            var D = new CpioDir(Filename, LocalPath);
            Files.Add(D);
            return D;
        }

        public CpioFile AddFile(string Filename, string LocalPath)
        {
            var F = new CpioFile(Filename, LocalPath);
            Files.Add(F);
            return F;
        }

        public CpioFile AddFile(string Path, DateTime Modified, byte[] Data)
        {
            var F = new CpioFile(Path, Modified, Data);
            Files.Add(F);
            return F;
        }

        public CpioSLink AddSLink(string Filename, string ToPath)
        {
            var L = new CpioSLink(Filename, ToPath);
            Files.Add(L);
            return L;
        }

        public CpioNod AddNod(string Filename, uint RMajor, uint RMinor)
        {
            var N = new CpioNod(Filename, RMajor, RMinor);
            Files.Add(N);
            return N;
        }

        public CpioBlock AddBlock(string Filename, uint RMajor, uint RMinor)
        {
            var B = new CpioBlock(Filename, RMajor, RMinor);
            Files.Add(B);
            return B;
        }

        public CpioFifo AddFifo(string Filename, uint RMajor, uint RMinor)
        {
            var F = new CpioFifo(Filename, RMajor, RMinor);
            Files.Add(F);
            return F;
        }

        public void Clear()
        {
            var Filenames = Array.ConvertAll(Files.ToArray(), F => F.Path);
            foreach(var F in Filenames)
            {
                if (!String.Equals(F, "."))
                {
                    Debug.WriteLine(F);
                    Delete(F);
                }
            }
        }

        public void UpdateSLink(string Filename, string ToPath)
        {
            var F = GetFile(Filename);

            if (F != null)
            {
                var Raw = UTF8Encoding.UTF8.GetBytes(ToPath);
                var NF = F.UpdateContent(Raw);

                for (int i = 0; i < Files.Count; i++)
                {
                    if (Files[i].Path == Filename)
                        Files[i] = NF;
                }
            }
        }

        public void UpdateFile(string Filename, string LocalPath)
        {
            var F = GetFile(Filename);

            if(F != null)
            {
                var Raw = File.ReadAllBytes(LocalPath);
                var NF = F.UpdateContent(Raw);

                for(int i = 0; i < Files.Count; i++)
                {
                    if (Files[i].Path == Filename)
                        Files[i] = NF;
                }
            }
        }
    }
}
