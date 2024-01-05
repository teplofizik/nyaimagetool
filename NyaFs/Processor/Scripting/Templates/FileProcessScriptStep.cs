using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Templates
{
    /// <summary>
    /// Process files in directory with one algorithm
    /// </summary>
    public class FileProcessScriptStep : ScriptStep
    {
        /// <summary>
        /// Path to file or directory
        /// </summary>
        private string[] Paths;

        public FileProcessScriptStep(string Name, string[] Paths) : base(Name)
        {
            this.Paths = Paths;
        }

        public FileProcessScriptStep(string Name, string Path) : base(Name)
        {
            this.Paths = new string[] { Path };
        }

        public override ScriptStepResult Exec(ImageProcessor Processor)
        {
            var Fs = Processor.GetFs();

            if (Fs == null)
                return new ScriptStepResult(ScriptStepStatus.Error, "Filesystem is not loaded");

            if ((Paths != null) && (Paths.Length > 0))
            {
                foreach (var Path in Paths)
                {
                    if (!Fs.Exists(Path))
                        return new ScriptStepResult(ScriptStepStatus.Error, $"Filesystem does not contains {Path}.");

                    var Element = Fs.GetElement(Path);
                    if (Element.ItemType == Filesystem.Universal.Types.FilesystemItemType.Directory)
                    {
                        // This is directory.
                        var R = ProcessDir(Fs, Path);
                        if (R.Status != ScriptStepStatus.Ok)
                            return R;
                    }
                    else
                    {
                        var R = ProcessFile(Fs, Path);
                        if (R.Status != ScriptStepStatus.Ok)
                            return R;
                    }
                }

                return new ScriptStepResult(ScriptStepStatus.Ok, $"Processing completed.");
            }
            else
                return new ScriptStepResult(ScriptStepStatus.Error, $"No paths specified.");
        }

        /// <summary>
        /// Process all files in directory
        /// </summary>
        /// <param name="Fs"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        protected ScriptStepResult ProcessDir(ImageFormat.Elements.Fs.LinuxFilesystem Fs, string Path)
        {
            var Dir = Fs.GetDirectory(Path);
            foreach (var E in Dir.Items)
            {
                if (E.ItemType == Filesystem.Universal.Types.FilesystemItemType.File)
                {
                    if (IsNeedProcessFile(E.Filename))
                    {
                        var Res = ProcessFile(Fs, E.Filename);
                        if (Res.Status != ScriptStepStatus.Ok)
                            return Res;
                    }
                }
                if (E.ItemType == Filesystem.Universal.Types.FilesystemItemType.Directory)
                {
                    if (IsNeedProcessDir(E.Filename))
                    {
                        var Res = ProcessDir(Fs, E.Filename);
                        if (Res.Status != ScriptStepStatus.Ok)
                            return Res;
                    }
                }
            }

            return new ScriptStepResult(ScriptStepStatus.Ok, $"Processed {Path}");
        }

        /// <summary>
        /// Process file in filesystem
        /// </summary>
        /// <param name="Fs"></param>
        /// <param name="Path"></param>
        /// <returns></returns>
        protected ScriptStepResult ProcessFile(ImageFormat.Elements.Fs.LinuxFilesystem Fs, string Path)
        {
            var Original = GetFileContent(Fs, Path);

            if (Original != null)
            {
                try
                {
                    var Processed = ProcessData(Path, Original);
                    SetFileContent(Fs, Path, Processed);
                }
                catch(Exception E)
                {
                    return new ScriptStepResult(ScriptStepStatus.Error, $"Processing {Path} is failed: {E.Message}");
                }
            }

            return new ScriptStepResult(ScriptStepStatus.Ok, $"Processed {Path}");
        }

        /// <summary>
        /// Process data in file
        /// </summary>
        /// <param name="Path"></param>
        /// <param name="Data"></param>
        /// <returns></returns>
        protected virtual byte[] ProcessData(string Path, byte[] Data)
        {
            throw new NotImplementedException("Data processing algo is not implemented");
        }

        /// <summary>
        /// Check is need to process dir
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        protected virtual bool IsNeedProcessDir(string Path)
        {
            return true;
        }

        /// <summary>
        /// Check is need to process file
        /// </summary>
        /// <param name="Path"></param>
        /// <returns></returns>
        protected virtual bool IsNeedProcessFile(string Path)
        {
            return false;
        }

        protected bool SetFileContent(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs, string Path, byte[] Content)
        {
            if (Fs.Exists(Path))
            {
                var File = Fs.GetElement(Path) as NyaFs.Filesystem.Universal.Items.File;
                File.Content = Content;

                return true;
            }
            else
                return false;
        }

        protected byte[] GetFileContent(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem Fs, string Path)
        {
            if (Fs.Exists(Path))
            {
                var File = Fs.GetElement(Path) as NyaFs.Filesystem.Universal.Items.File;
                return File.Content;
            }
            else
                return null;
        }
    }
}
