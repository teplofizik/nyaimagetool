using System;
using System.IO;

namespace NyaFs.ImageFormat.Elements.Fs.Reader
{
    public class ExtReader : Reader
    {
        //string Filename;
        //ExtDisk disk;

        public ExtReader(byte[] data)
        {
            //Filename = "tempextimage.ext4"; ///System.IO.Path.GetTempFileName();

            //System.IO.File.WriteAllBytes(Filename, data);
            //disk = ExtDisk.Open(Filename);
        }

        /// <summary>
        /// Читаем в файловую систему из cpio-файла
        /// </summary>
        /// <param name="Dst"></param>
        public override void ReadToFs(Filesystem Dst)
        {
            //var fs = ExtFileSystem.Open(disk, disk.Partitions[0]);

            //ProcessDirectory(fs, Dst.Root, ".");
        }
        /*
        private byte[] GetFile(ExtFileSystem fs, string Path)
        {
            var file = fs.OpenFile(Path, FileMode.Open, FileAccess.Read);

            var Res = new byte[file.Length];
            var Readed = file.Read(Res, 0, Convert.ToInt32(file.Length));

            file.Close();

            return Res;
        }

        private void ProcessDirectory(ExtFileSystem fs, Items.Dir ParentItem, string Path)
        {
            uint Mode = fs.GetMode(Path);
            var Owner = fs.GetOwner(Path);

            Log.Write(4, $"Added dir: {Path}");
            var CurrentDir = new Items.Dir(Path, Owner.Item1, Owner.Item2, Mode);
            ParentItem.Items.Add(CurrentDir);

            // Process dirs
            var Dirs = fs.GetDirectories(Path, "*", System.IO.SearchOption.TopDirectoryOnly);
            foreach (var D in Dirs)
            {
                ProcessDirectory(fs, CurrentDir, D);
            }

            // Process files
            var Files = fs.GetFiles(Path, "*", System.IO.SearchOption.TopDirectoryOnly);
            foreach (var F in Files)
            {
                uint FMode = fs.GetMode(Path);
                var FOwner = fs.GetOwner(Path);
                var Data = GetFile(fs, Path);

                Log.Write(4, $"Added file: {F} size {Data.Length}");
                var File = new Items.File(F, Owner.Item1, Owner.Item2, Mode, Data);

                CurrentDir.Items.Add(File);
            }
        }
        ~ExtReader()
        {
            disk.Dispose();
            System.IO.File.Delete(Filename);
        }*/
    }
}