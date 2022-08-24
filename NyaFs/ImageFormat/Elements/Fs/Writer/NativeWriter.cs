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

        public override void WriteFs(Filesystem Fs)
        {
            ProcessDirectory(Fs.Root);
        }

        private void ProcessDirectory(Items.Dir Dir)
        {
            foreach (var I in Dir.Items)
            {
                var AbsPath = System.IO.Path.Combine(this.Dir, I.Filename);

                switch (I.ItemType)
                {
                    case Types.FilesystemItemType.Dir:
                        if (System.IO.File.Exists(AbsPath))
                            Log.Warning(0, $"{AbsPath} is file, cannot make directory");
                        else
                        {
                            if (System.IO.Directory.Exists(AbsPath))
                                Log.Warning(0, $"{AbsPath} already exists");
                            else
                                System.IO.Directory.CreateDirectory(AbsPath);

                            ProcessDirectory(I as Items.Dir);
                        }
                        break;
                    case Types.FilesystemItemType.File:
                        if (System.IO.Directory.Exists(AbsPath))
                            Log.Warning(0, $"{AbsPath} is directory, cannot save file");
                        else
                        {
                            if (System.IO.File.Exists(AbsPath))
                                Log.Warning(0, $"{AbsPath} already exists");

                            System.IO.File.WriteAllBytes(AbsPath, (I as Items.File).Content);
                        }
                        break;
                    case Types.FilesystemItemType.SymLink:
                        if (System.IO.File.Exists(AbsPath))
                            Log.Warning(0, $"{AbsPath} already exists");

                        // TODO

                        break;
                }
            }
        }
    }
}
