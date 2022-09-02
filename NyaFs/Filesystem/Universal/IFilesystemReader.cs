using System;
using System.Collections.Generic;
using System.Text;

namespace NyaFs.Filesystem.Universal
{
    public interface IFilesystemReader
    {
        /// <summary>
        /// Read file by path
        /// </summary>
        /// <param name="Path">Path to file</param>
        /// <returns>Content of file or null if file is not exists</returns>
        byte[] Read(string Path);

        public FilesystemEntry[] ReadDir(string Path);
    }
}
