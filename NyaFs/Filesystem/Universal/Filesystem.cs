using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal
{
    // cpio
    // gzipped cpio
    // ext4
    // gzipped ext4
    // legacy gzipped cpio
    // legacy gzipped ext4
    // fit => gzipped cpio
    // fit => gzipped ext4

    public class Filesystem
    {
        /// <summary>
        /// Root filesystem directory
        /// </summary>
        public Items.Dir Root = new Items.Dir(".", 0, 0, 0x755);

        /// <summary>
        /// Is image loaded?
        /// </summary>
        public bool Loaded => Root.Items.Count > 0;

        public void Dump()
        {
            DumpDir(Root);
        }

        private void DumpDir(Items.Dir Dir)
        {
            Console.WriteLine(Dir.ToString());
            foreach(var I in Dir.Items)
            {
                if (I.ItemType == Types.FilesystemItemType.Directory)
                    DumpDir(I as Items.Dir);
                else
                    Console.WriteLine(I.ToString());
            }
        }

        private string GetParentDirPath(string Path)
        {
            int Pos = Path.LastIndexOf('/');
            if (Pos >= 0)
            {
                var Res = Path.Substring(0, Pos);
                return (Res.Length > 0) ? Res : "/";
            }
            else
                return "/";
        }

        public Items.Dir GetDirectory(string Path)
        {
            var Element = GetElement(Path);

            return Element as Items.Dir;
        }

        public Items.Dir GetParentDirectory(string Path)
        {
            if (Path == ".") return null;
            if (Path.Length == 0)
                throw new ArgumentException($"{Path} is empty");

            var Parent = GetParentDirPath(Path);
            return GetElement(Parent) as Items.Dir;
        }

        public bool Exists(string Path)
        {
            if (Path == ".") return true;
            if (Path.Length == 0)
                throw new ArgumentException($"{Path} is empty");

            if (Path[0] == '/') Path = Path.Substring(1);

            var Parts = Path.Split("/");

            Items.Dir Base = Root;
            string Rel = null;
            foreach (var P in Parts)
            {
                Rel = (Rel == null) ? P : Rel + "/" + P;

                bool Found = false;
                foreach (var I in Base.Items)
                {
                    if (I.Filename == Rel)
                    {
                        if (Rel == Path)
                            return true;

                        if (I.ItemType == Types.FilesystemItemType.Directory)
                        {
                            Base = I as Items.Dir;
                            Found = true;
                            break;
                        }
                        else
                            return false;
                    }
                }
                if (!Found)
                    return false;
            }

            return false;
        }

        public FilesystemItem GetElement(string Path)
        {
            if ((Path == ".") || (Path == "/")) return Root;

            if (Path.Length == 0)
                throw new ArgumentException($"{Path} is empty");

            if (Path[0] == '/') Path = Path.Substring(1);

            var Parts = Path.Split("/");

            Items.Dir Base = Root; 
            string Rel = null;
            foreach(var P in Parts)
            {
                Rel = (Rel == null) ? P : Rel + "/" + P;

                bool Found = false;
                foreach(var I in Base.Items)
                {
                    if(I.Filename == Rel)
                    {
                        if (Rel == Path)
                            return I;

                        if (I.ItemType == Types.FilesystemItemType.Directory)
                        {
                            Base = I as Items.Dir;
                            Found = true;
                            break;
                        }
                        else
                            throw new ArgumentException($"{Rel} is not dir, cannot process to {Path} node");
                    }
                }
                if(!Found)
                    throw new ArgumentException($"{Rel} is not found in filesystem");
            }

            throw new ArgumentException($"{Path} is not found in filesystem");
        }

        internal void Delete(string Path)
        {
            var Element = GetElement(Path);
            if(Element == null)
                throw new ArgumentException($"{Path} is not found in filesystem");

            var Parent = GetParentDirectory(Path);
            if (Parent == null)
                throw new ArgumentException($"Parent dir for {Path} is not found in filesystem");

            Parent.Items.RemoveAll(FI => FI.Filename == Path);
        }
    }
}
