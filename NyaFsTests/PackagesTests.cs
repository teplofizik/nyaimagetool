using System;
using System.Text;
using NyaFs.Filesystem.Universal.Items;
using NyaFs.Filesystem.Universal.Types;
using NyaFs.ImageFormat.Elements.Fs;
using NyaFs.ImageFormat.Types;
using NyaFs.Packages;
using Xunit;
using FFile = NyaFs.Filesystem.Universal.Items.File;
using CChar = NyaFs.Filesystem.Universal.Items.Char;

namespace NyaFsTests
{
    public class PackagesTests
    {
        private static LinuxFilesystem SourceFs()
        {
            var Fs = new LinuxFilesystem();
            var Usr = new Dir("usr", 0, 0, 0x1ED);
            Fs.Fs.Root.Items.Add(Usr);
            var Share = new Dir("usr/share", 0, 0, 0x1ED);
            Share.Xattrs["user.dir"] = new byte[] { 1, 2, 3 };
            Usr.Items.Add(Share);
            var A = new FFile("usr/share/a.txt", 0, 0, 0x1A4, Encoding.UTF8.GetBytes("hello"));
            A.Xattrs["user.test"] = Encoding.UTF8.GetBytes("v1");
            A.Xattrs["security.selinux"] = Encoding.UTF8.GetBytes("u:object_r:foo:s0");
            Share.Items.Add(A);
            Share.Items.Add(new SymLink("usr/share/l", 0, 0, 0x1FF, "a.txt"));
            Share.Items.Add(new CChar("usr/share/dev", 0, 0, 0x1A4) { Major = 1, Minor = 3 });
            Share.Items.Add(new Fifo("usr/share/fifo", 0, 0, 0x1A4));
            return Fs;
        }

        private static PackageMetadata Meta() => new PackageMetadata
        {
            Name = "foo",
            Version = "1.2",
            Arch = "arm",
            Os = "linux",
            Description = "test"
        };

        [Fact]
        public void Package_Roundtrip_Preserves_Metadata_And_Entries()
        {
            var P = NyaPackage.FromFilesystem(SourceFs(), "/usr", Meta());
            P.Metadata.Set("x-custom", "42");

            var Q = NyaPackage.Open(P.ToFile());

            Assert.Equal("foo", Q.Metadata.Name);
            Assert.Equal("1.2", Q.Metadata.Version);
            Assert.Equal("arm", Q.Metadata.Arch);
            Assert.Equal("42", Q.Metadata.Get("x-custom"));
            Assert.Equal(5, Q.Entries.Count);

            var FileEntry = Q.Entries.Find(E => E.Path == "share/a.txt");
            Assert.NotNull(FileEntry);
            Assert.NotNull(FileEntry.Xattrs);
            Assert.Equal("v1", Encoding.UTF8.GetString(FileEntry.Xattrs["user.test"]));
            Assert.Equal("u:object_r:foo:s0", Encoding.UTF8.GetString(FileEntry.Xattrs["security.selinux"]));

            var DirEntry = Q.Entries.Find(E => E.Path == "share");
            Assert.NotNull(DirEntry);
            Assert.NotNull(DirEntry.Xattrs);
            Assert.Equal(new byte[] { 1, 2, 3 }, DirEntry.Xattrs["user.dir"]);

            var DevEntry = Q.Entries.Find(E => E.Path == "share/dev");
            Assert.NotNull(DevEntry);
            Assert.Equal(FilesystemItemType.Character, DevEntry.Type);
            Assert.Equal(1u, DevEntry.Major);
            Assert.Equal(3u, DevEntry.Minor);

            var FifoEntry = Q.Entries.Find(E => E.Path == "share/fifo");
            Assert.NotNull(FifoEntry);
            Assert.Equal(FilesystemItemType.Fifo, FifoEntry.Type);
        }

        [Fact]
        public void Package_Database_Resolves_Dependencies()
        {
            var Dep = new NyaPackage { Metadata = new PackageMetadata { Name = "lib", Version = "1.2" } };
            var App = new NyaPackage { Metadata = new PackageMetadata { Name = "app", Version = "1.0", Depends = "lib>=1.0" } };
            var Bad = new NyaPackage { Metadata = new PackageMetadata { Name = "bad", Version = "1.0", Depends = "missing" } };

            System.Collections.Generic.List<NyaPackage> Order;
            string Error;
            Assert.True(PackageDatabase.Resolve(new[] { App, Dep }, "app", out Order, out Error), Error);
            Assert.Equal(2, Order.Count);
            Assert.Equal("lib", Order[0].Metadata.Name);

            Assert.False(PackageDatabase.Resolve(new[] { Bad, Dep }, "bad", out Order, out Error));

            var Fs = new LinuxFilesystem();
            var Db = PackageDatabase.Load(Fs);
            Db.Set(Dep.Metadata);
            Assert.True(Db.CheckInstall(App.Metadata, out Error), Error);
            Assert.False(Db.CheckInstall(Bad.Metadata, out Error));
        }

        [Fact]
        public void Package_FromDirectory_With_Zstd()
        {
            var Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "nyapkg-dir-" + Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(Root, "etc"));
            System.IO.File.WriteAllText(System.IO.Path.Combine(Root, "etc", "hostname"), "pkg");

            var Meta = new PackageMetadata { Name = "dirpkg", Version = "1.0" };
            var P = NyaPackage.FromDirectory(Root, Meta);
            var Bytes = P.ToFile("zstd");
            var Q = NyaPackage.Open(Bytes);

            Assert.Equal("dirpkg", Q.Metadata.Name);
            Assert.Equal("zstd", Q.Metadata.Compression);
            var Entry = Q.Entries.Find(E => E.Path == "etc/hostname");
            Assert.NotNull(Entry);
            Assert.Equal("pkg", Encoding.UTF8.GetString(Entry.Content));
        }

        [Fact]
        public void Package_Filters_By_Architecture_And_Installs()
        {
            var Q = NyaPackage.Open(NyaPackage.FromFilesystem(SourceFs(), "/usr", Meta()).ToFile());

            var Arm = new LinuxFilesystem();
            Arm.Info.Architecture = CPU.IH_ARCH_ARM;
            Arm.Info.OperatingSystem = OS.IH_OS_LINUX;

            var Mips = new LinuxFilesystem();
            Mips.Info.Architecture = CPU.IH_ARCH_MIPS;
            Mips.Info.OperatingSystem = OS.IH_OS_LINUX;

            Assert.True(Q.Matches(Arm));
            Assert.False(Q.Matches(Mips));

            Assert.Equal(5, Q.AddTo(Arm, "/opt"));

            var File = Arm.Fs.GetElement("opt/share/a.txt") as FFile;
            Assert.NotNull(File);
            Assert.Equal("hello", Encoding.UTF8.GetString(File.Content));
            Assert.Equal("v1", Encoding.UTF8.GetString(File.Xattrs["user.test"]));

            var Link = Arm.Fs.GetElement("opt/share/l") as SymLink;
            Assert.NotNull(Link);
            Assert.Equal("a.txt", Link.Target);

            var Dev = Arm.Fs.GetElement("opt/share/dev") as CChar;
            Assert.NotNull(Dev);
            Assert.Equal(1u, Dev.Major);
            Assert.Equal(3u, Dev.Minor);

            Assert.NotNull(Arm.Fs.GetElement("opt/share/fifo") as Fifo);
        }
    }
}
