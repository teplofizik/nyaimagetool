namespace NyaFsRemoteCore
{
    public sealed class RemoteEntry
    {
        public string Name;
        public string Path;
        public bool IsDirectory;
        public bool IsFile;
        public bool IsSymLink;
        public string LinkTarget;
        public uint Mode = 0x1A4;
        public uint User = 0;
        public uint Group = 0;
        public long Size = 0;
    }
}
