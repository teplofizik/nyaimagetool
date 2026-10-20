using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFsRemoteCore;
using Xunit;
using DirItem = NyaFs.Filesystem.Universal.Items.Dir;
using FileItem = NyaFs.Filesystem.Universal.Items.File;
using SymLinkItem = NyaFs.Filesystem.Universal.Items.SymLink;

namespace NyaFsTests
{
    public class RemoteTransferTests
    {
        private sealed class FakeFs : IRemoteFs
        {
            public readonly Dictionary<string, RemoteEntry> Entries = new Dictionary<string, RemoteEntry>(StringComparer.Ordinal);
            public readonly Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            public readonly List<string> Created = new List<string>();

            public void Connect() { }

            public void AddDir(string Path, uint Mode, uint User = 0, uint Group = 0) =>
                Entries[Path] = new RemoteEntry { Path = Path, Name = Name(Path), IsDirectory = true, Mode = Mode, User = User, Group = Group };

            public void AddFile(string Path, byte[] Content, uint Mode, uint User = 0, uint Group = 0)
            {
                Entries[Path] = new RemoteEntry { Path = Path, Name = Name(Path), IsFile = true, Mode = Mode, User = User, Group = Group, Size = Content.Length };
                Data[Path] = Content;
            }

            public void AddSymLink(string Path, string Target, uint Mode) =>
                Entries[Path] = new RemoteEntry { Path = Path, Name = Name(Path), IsSymLink = true, LinkTarget = Target, Mode = Mode };

            private static string Name(string Path) => Path.Substring(Path.LastIndexOf('/') + 1);

            public RemoteEntry GetEntry(string Path) => Entries.TryGetValue(Path, out var E) ? E : null;

            public RemoteEntry[] List(string Path)
            {
                var Prefix = Path.TrimEnd('/') + "/";
                return Entries.Values
                    .Where(E => E.Path.StartsWith(Prefix, StringComparison.Ordinal))
                    .Where(E => E.Path.Substring(Prefix.Length).IndexOf('/') < 0)
                    .ToArray();
            }

            public byte[] ReadFile(string Path) => Data[Path];

            public void WriteFile(string Path, byte[] Content)
            {
                Data[Path] = Content;
                Entries[Path] = new RemoteEntry { Path = Path, Name = Name(Path), IsFile = true, Mode = 0x1A4, Size = Content.Length };
            }

            public void CreateDirectory(string Path) => Created.Add(Path);

            public void Dispose() { }
        }

        [Fact]
        public void RemoteGet_File_Directory_SymLink()
        {
            var R = new FakeFs();
            R.AddDir("/dir", 0x1ED, 1, 2);
            R.AddFile("/dir/a.txt", Encoding.UTF8.GetBytes("hello"), 0x1A4, 3, 4);
            R.AddSymLink("/dir/link", "a.txt", 0x1FF);

            var Fs = new LinuxFilesystem();
            RemoteTransfer.Download(R, "/dir", Fs, "target");

            var File = (FileItem)Fs.GetElement("target/a.txt");
            Assert.NotNull(File);
            Assert.Equal("hello", Encoding.UTF8.GetString(File.Content));
            Assert.Equal(0x1A4u, File.Mode);
            Assert.Equal(3u, File.User);
            Assert.Equal(4u, File.Group);

            var Link = (SymLinkItem)Fs.GetElement("target/link");
            Assert.NotNull(Link);
            Assert.Equal("a.txt", Link.Target);
        }

        [Fact]
        public void RemotePut_File_And_Directory()
        {
            var Content = Encoding.UTF8.GetBytes("uploaded");

            var Fs = new LinuxFilesystem();
            var Dir = new DirItem("d", 0, 0, 0x1ED);
            Dir.Items.Add(new FileItem("d/a.txt", 10, 20, 0x1A4, Content));
            Fs.Fs.Root.Items.Add(Dir);

            var R = new FakeFs();
            RemoteTransfer.Upload(Fs, "d", R, "/remote/d");

            Assert.Contains("/remote/d", R.Created);
            Assert.Equal(Content, R.Data["/remote/d/a.txt"]);
        }

        [Theory]
        [InlineData("host", 22, "host", 22)]
        [InlineData("host:2222", 22, "host", 2222)]
        [InlineData("[::1]", 22, "::1", 22)]
        [InlineData("[::1]:2200", 22, "::1", 2200)]
        public void SplitHostPort_Works(string Value, int Default, string Host, int Port)
        {
            var (H, P) = RemoteUri.SplitHostPort(Value, Default);
            Assert.Equal(Host, H);
            Assert.Equal(Port, P);
        }

        [Fact]
        public void ShellQuote_QuotesSingleQuotes()
        {
            Assert.Equal("'a'\"'\"'b c'", ShellQuote.Quote("a'b c"));
        }
    }
}
