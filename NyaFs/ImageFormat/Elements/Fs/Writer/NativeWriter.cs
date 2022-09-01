using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.ImageFormat.Elements.Fs.Writer
{
    public class NativeWriter : Writer
    {
        string Dir;

        public NativeWriter(string Dir)
        {
            this.Dir = Dir;
        }

        public override void WriteFs(LinuxFilesystem Fs)
        {
            ProcessDirectory(Fs.Fs.Root);
        }

        private void ProcessDirectory(Filesystem.Universal.Items.Dir Dir)
        {
            foreach (var I in Dir.Items)
            {
                var AbsPath = System.IO.Path.Combine(this.Dir, I.Filename);

                switch (I.ItemType)
                {
                    case Filesystem.Universal.Types.FilesystemItemType.Directory:
                        if (System.IO.File.Exists(AbsPath))
                            Log.Warning(0, $"{AbsPath} is file, cannot make directory");
                        else
                        {
                            if (System.IO.Directory.Exists(AbsPath))
                                Log.Warning(0, $"{AbsPath} already exists");
                            else
                                System.IO.Directory.CreateDirectory(AbsPath);

                            ProcessDirectory(I as Filesystem.Universal.Items.Dir);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.File:
                        if (System.IO.Directory.Exists(AbsPath))
                            Log.Warning(0, $"{AbsPath} is directory, cannot save file");
                        else
                        {
                            if (System.IO.File.Exists(AbsPath))
                                Log.Warning(0, $"{AbsPath} already exists");

                            System.IO.File.WriteAllBytes(AbsPath, (I as Filesystem.Universal.Items.File).Content);
                        }
                        break;
                    case Filesystem.Universal.Types.FilesystemItemType.SymLink:
                        if (System.IO.File.Exists(AbsPath))
                            Log.Warning(0, $"{AbsPath} already exists");

                        // TODO

                        break;
                }
            }
        }
    }
}
