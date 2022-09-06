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

        /// <summary>
        /// Read link content by path
        /// </summary>
        /// <param name="Path">Path to symlink</param>
        /// <returns>Link</returns>
        string ReadLink(string Path);

        /// <summary>
        /// Read device information
        /// </summary>
        /// <param name="Path">Path to device</param>
        /// <returns>Device numbers (major/minor)</returns>
        Types.DeviceInfo ReadDevice(string Path);

        /// <summary>
        /// Read directory content
        /// </summary>
        /// <param name="Path">Path to directory</param>
        /// <returns>Array of entries</returns>
        public FilesystemEntry[] ReadDir(string Path);
    }
}
