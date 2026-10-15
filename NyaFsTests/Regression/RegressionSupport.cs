using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using NyaFs.Filesystem.Universal;
using NyaFs.Processor;
using NyaFs.Processor.Scripting;
using NyaFs.Processor.Scripting.Helper;

namespace NyaFsTests.Regression
{
    public class FsEntry
    {
        public string Path { get; set; }
        public string Type { get; set; }
        public uint Uid { get; set; }
        public uint Gid { get; set; }
        public string Mode { get; set; }
        public long Size { get; set; }
        public string Sha { get; set; }
        public uint? DevMajor { get; set; }
        public uint? DevMinor { get; set; }
        public string Link { get; set; }
    }

    public class KernelSig
    {
        public long Size { get; set; }
        public string Sha { get; set; }
    }

    public class DtbSig
    {
        public int Nodes { get; set; }
        public int Properties { get; set; }
        public string Sha { get; set; }
    }

    public class ImageSignature
    {
        public string File { get; set; }
        public long Size { get; set; }
        public string Sha256 { get; set; }
        public string Detected { get; set; }
        public string Status { get; set; }
        public string Error { get; set; }
        public List<FsEntry> Fs { get; set; }
        public KernelSig Kernel { get; set; }
        public DtbSig Dtb { get; set; }
    }

    /// <summary>
    /// Builds a content-based signature of an image without depending on exact byte layout,
    /// timestamps or compression settings.
    /// </summary>
    public static class RegressionSupport
    {
        public static string Sha256Bytes(byte[] data)
        {
            using (var h = SHA256.Create())
                return Convert.ToHexString(h.ComputeHash(data)).ToLowerInvariant();
        }

        public static string Sha256File(string path)
        {
            using (var s = File.OpenRead(path))
            using (var h = SHA256.Create())
                return Convert.ToHexString(h.ComputeHash(s)).ToLowerInvariant();
        }

        public static ImageSignature Build(string path)
        {
            var fi = new FileInfo(path);
            var sig = new ImageSignature
            {
                File = fi.Name,
                Size = fi.Length,
                Sha256 = Sha256File(path)
            };

            try
            {
                var parser = new ScriptParser(new ScriptBaseAll());
                var proc = new ImageProcessor(parser, new string[0]);
                proc.Plugins.Load(new NyaFsMcuFs.LittleFsFilesystemPlugin());

                try
                {
                    var inspector = new NyaFs.ImageFormat.ImageInspector(proc.Plugins);
                    sig.Detected = BuildDetected(inspector.Inspect(fi.Name, File.ReadAllBytes(path)));
                }
                catch { }

                var script = parser.ParseScript("regress", new[] { "load \"" + path + "\"" });

                if (script.HasErrors)
                {
                    sig.Status = "parse-error";
                    return sig;
                }

                proc.Process(script);

                var fs = proc.GetFs();
                if ((fs != null) && fs.Loaded)
                    sig.Fs = WalkFs(fs);

                var kernel = proc.GetKernel();
                if ((kernel != null) && kernel.Loaded)
                    sig.Kernel = new KernelSig { Size = kernel.Image.Length, Sha = Sha256Bytes(kernel.Image) };

                var dtb = proc.GetDevTree();
                if ((dtb != null) && dtb.Loaded)
                    sig.Dtb = BuildDtb(dtb);

                sig.Status = ((sig.Fs != null) || (sig.Kernel != null) || (sig.Dtb != null)) ? "ok" : "unrecognized";
            }
            catch (Exception e)
            {
                sig.Status = "exception";
                sig.Error = e.GetType().Name + ": " + e.Message;
            }

            return sig;
        }

        /// <summary>
        /// Builds the "detected" chain from the plugin-driven inspector, preserving the
        /// container and compression, e.g. "ramfs/tar/gzip", "all/fit", "kernel/raw/xz".
        /// </summary>
        private static string BuildDetected(NyaFs.ImageFormat.ImageNode Root)
        {
            if (Root == null) return null;

            if (Root.Children.Count > 0)
            {
                var Container = Root.Container ?? Root.Format;
                return (Container != null) ? (Category(Root) + "/" + Container) : Category(Root);
            }

            if ((Root.Kind == NyaFs.ImageFormat.ImageKind.Unknown) && (Root.Format == null))
                return null;

            var Res = Category(Root.Kind) + "/" + (Root.Format ?? "?");

            var Comp = CompressionName(Root.Compression);
            if (Comp != null)
                Res += "/" + Comp;

            return Res;
        }

        private static string Category(NyaFs.ImageFormat.ImageNode Node)
        {
            if (Node.Children.Count > 0)
            {
                var Cats = new System.Collections.Generic.HashSet<string>();
                foreach (var C in Node.Children)
                    Cats.Add(Category(C));

                return (Cats.Count == 1) ? System.Linq.Enumerable.First(Cats) : "all";
            }

            return Category(Node.Kind);
        }

        private static string Category(NyaFs.ImageFormat.ImageKind Kind)
        {
            switch (Kind)
            {
                case NyaFs.ImageFormat.ImageKind.Filesystem: return "ramfs";
                case NyaFs.ImageFormat.ImageKind.Kernel: return "kernel";
                case NyaFs.ImageFormat.ImageKind.Devtree: return "devtree";
                default: return "unknown";
            }
        }

        private static string CompressionName(NyaFs.ImageFormat.Types.CompressionType C)
        {
            switch (C)
            {
                case NyaFs.ImageFormat.Types.CompressionType.IH_COMP_GZIP: return "gzip";
                case NyaFs.ImageFormat.Types.CompressionType.IH_COMP_BZIP2: return "bzip2";
                case NyaFs.ImageFormat.Types.CompressionType.IH_COMP_LZMA: return "lzma";
                case NyaFs.ImageFormat.Types.CompressionType.IH_COMP_LZ4: return "lz4";
                case NyaFs.ImageFormat.Types.CompressionType.IH_COMP_LZO: return "lzo";
                case NyaFs.ImageFormat.Types.CompressionType.IH_COMP_XZ: return "xz";
                case NyaFs.ImageFormat.Types.CompressionType.IH_COMP_ZSTD: return "zstd";
                default: return null;
            }
        }

        private static List<FsEntry> WalkFs(NyaFs.ImageFormat.Elements.Fs.LinuxFilesystem fs)
        {
            var res = new List<FsEntry>();
            WalkDir(fs.Fs.Root, res);
            res.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            return res;
        }

        private static void WalkDir(NyaFs.Filesystem.Universal.Items.Dir dir, List<FsEntry> res)
        {
            foreach (var item in dir.Items)
            {
                res.Add(MakeEntry(item));
                if (item is NyaFs.Filesystem.Universal.Items.Dir d)
                    WalkDir(d, res);
            }
        }

        private static FsEntry MakeEntry(FilesystemItem item)
        {
            var e = new FsEntry
            {
                Path = item.Filename,
                Type = item.ItemType.ToString(),
                Uid = item.User,
                Gid = item.Group,
                Mode = Convert.ToString(item.Mode, 8).PadLeft(4, '0'),
                Size = item.Size
            };

            switch (item)
            {
                case NyaFs.Filesystem.Universal.Items.File f:
                    e.Sha = Sha256Bytes(f.Content);
                    break;
                case NyaFs.Filesystem.Universal.Items.SymLink s:
                    e.Link = s.Target;
                    break;
                case NyaFs.Filesystem.Universal.Items.Block b:
                    e.DevMajor = b.Major;
                    e.DevMinor = b.Minor;
                    break;
                case NyaFs.Filesystem.Universal.Items.Char c:
                    e.DevMajor = c.Major;
                    e.DevMinor = c.Minor;
                    break;
            }

            return e;
        }

        private static DtbSig BuildDtb(NyaFs.ImageFormat.Elements.Dtb.DeviceTree dtb)
        {
            int nodes = 0, props = 0;
            CountNodes(dtb.DevTree.Root, ref nodes, ref props);

            var binary = new NyaFs.FlattenedDeviceTree.Writer.FDTWriter(dtb.DevTree).GetBinary();

            return new DtbSig { Nodes = nodes, Properties = props, Sha = Sha256Bytes(binary) };
        }

        private static void CountNodes(NyaFs.FlattenedDeviceTree.Types.Node node, ref int nodes, ref int props)
        {
            nodes++;
            props += node.Properties.Count;
            foreach (var child in node.Nodes)
                CountNodes(child, ref nodes, ref props);
        }
    }
}
