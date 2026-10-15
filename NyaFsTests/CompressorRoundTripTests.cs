using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NyaFs.ImageFormat;
using NyaFs.ImageFormat.Plugins;
using NyaFs.ImageFormat.Plugins.Base;
using NyaFs.ImageFormat.Types;
using Xunit;

namespace NyaFsTests
{
    /// <summary>
    /// Round-trip tests for compressor plugins: Compress -> Decompress -> compare,
    /// detection of the produced container, and the read/write capability contract.
    /// </summary>
    public class CompressorRoundTripTests
    {
        private static CompressorPlugin Plugin(CompressionType type) =>
            BuiltinPlugins.Compressors.Single(p => p.Compression == type);

        private static readonly CompressionType[] Writable =
        {
            CompressionType.IH_COMP_GZIP,
            CompressionType.IH_COMP_BZIP2,
            CompressionType.IH_COMP_LZ4,
            CompressionType.IH_COMP_LZMA,
            CompressionType.IH_COMP_ZSTD,
            CompressionType.IH_COMP_XZ
        };

        private static readonly string[] PayloadNames =
            { "empty", "one", "hello", "repetitive", "compressible", "pseudorandom", "mixed" };

        public static IEnumerable<object[]> Cases()
        {
            foreach (var type in Writable)
                foreach (var name in PayloadNames)
                    yield return new object[] { type, name };
        }

        private static byte[] Payload(string name)
        {
            switch (name)
            {
                case "empty":
                    return new byte[0];

                case "one":
                    return Encoding.ASCII.GetBytes("a");

                case "hello":
                    return Encoding.ASCII.GetBytes("Hello, compressor!");

                case "repetitive":
                    return Encoding.ASCII.GetBytes(new string('A', 100000));

                case "compressible":
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < 4000; i++)
                        sb.Append("line ").Append(i).Append(": the quick brown fox jumps over the lazy dog\n");
                    return Encoding.ASCII.GetBytes(sb.ToString());
                }

                case "pseudorandom":
                {
                    var rnd = new Random(12345);
                    var data = new byte[60000];
                    rnd.NextBytes(data);
                    return data;
                }

                case "mixed":
                {
                    var rnd = new Random(6789);
                    var data = new byte[300000];
                    for (int i = 0; i < data.Length; i++)
                        data[i] = (byte)((i % 1000 < 700) ? (i % 7) : rnd.Next(256));
                    return data;
                }

                default:
                    throw new ArgumentException($"Unknown payload '{name}'", nameof(name));
            }
        }

        [Theory]
        [MemberData(nameof(Cases))]
        public void Compress_Decompress_RoundTrip(CompressionType type, string payloadName)
        {
            var plugin = Plugin(type);
            var data = Payload(payloadName);

            Assert.True(plugin.SupportRead, $"{plugin.CompressorName}: read not supported");
            Assert.True(plugin.SupportWrite, $"{plugin.CompressorName}: write not supported");

            var compressed = plugin.Compress(data);
            Assert.NotNull(compressed);

            Assert.True(plugin.DetectCompressor(new ImageData(compressed)),
                $"{plugin.CompressorName}: produced stream not detected");

            Assert.Equal(data, plugin.Decompress(compressed));
        }

        [Theory]
        [InlineData(CompressionType.IH_COMP_LZO)]
        public void ReadOnly_Compressor_Does_Not_Support_Write(CompressionType type)
        {
            var plugin = Plugin(type);

            Assert.True(plugin.SupportRead);
            Assert.False(plugin.SupportWrite);
        }

        [Fact]
        public void Xz_Decompress_Known_Vector()
        {
            var payload = Encoding.ASCII.GetBytes("hello xz world hello xz world");
            var vector = Convert.FromBase64String(
                "/Td6WFoAAATm1rRGAgAhARYAAAB0L+Wj4AAcABVdADQZSe6N6RfQtA5NCWQW7ArQOEUAAAAAAADHii/FFs/mygABMR1A3t2FH7bzfQEAAAAABFla");

            Assert.Equal(payload, Plugin(CompressionType.IH_COMP_XZ).Decompress(vector));
        }
    }
}
