using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Processor.Scripting.Helper
{
    internal static class FsHelper
    {
        internal static string DetectFilePath(this ScriptStep S, string Path)
        {
            if (System.IO.File.Exists(Path))
                return Path;

            var RelPath = System.IO.Path.Combine(S.ScriptPath, Path);
            if(System.IO.File.Exists(RelPath))
                return RelPath;

            return null;
        }
    }
}
