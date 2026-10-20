using System;

namespace NyaFsRemoteCore
{
    public interface IRemoteFs : IDisposable
    {
        void Connect();

        RemoteEntry GetEntry(string Path);
        RemoteEntry[] List(string Path);

        byte[] ReadFile(string Path);
        void WriteFile(string Path, byte[] Data);
        void CreateDirectory(string Path);
    }
}
