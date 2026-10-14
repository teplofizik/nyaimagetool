using System;
using System.Text;
using NyaLZO;
using Xunit;

namespace NyaFsTests
{
    public class LzoTests
    {
        private static void RoundTrip(byte[] data)
        {
            var compressed = LZO1xCompressor.Compress(data);
            var decompressed = LZO1xDecompressor.Decompress(compressed);

            Assert.Equal(data, decompressed);
        }

        [Fact]
        public void Empty()
        {
            RoundTrip(new byte[0]);
        }

        [Fact]
        public void Tiny()
        {
            RoundTrip(Encoding.ASCII.GetBytes("a"));
            RoundTrip(Encoding.ASCII.GetBytes("hello"));
            RoundTrip(Encoding.ASCII.GetBytes("Hello, LZO!"));
        }

        [Fact]
        public void Repetitive()
        {
            RoundTrip(Encoding.ASCII.GetBytes(string.Concat(System.Linq.Enumerable.Repeat("abcabcabc", 5000))));
            RoundTrip(Encoding.ASCII.GetBytes(new string('A', 100000)));
        }

        [Fact]
        public void CompressibleText()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < 4000; i++)
                sb.Append("line ").Append(i).Append(": the quick brown fox jumps over the lazy dog\n");
            RoundTrip(Encoding.ASCII.GetBytes(sb.ToString()));
        }

        [Fact]
        public void Pseudorandom()
        {
            var rnd = new Random(12345);
            for (int size = 0; size < 60000; size += 7919)
            {
                var data = new byte[size];
                rnd.NextBytes(data);
                RoundTrip(data);
            }
        }

        [Fact]
        public void LargeMixed()
        {
            var rnd = new Random(6789);
            var data = new byte[300000];
            for (int i = 0; i < data.Length; i++)
                data[i] = (byte)((i % 1000 < 700) ? (i % 7) : rnd.Next(256));
            RoundTrip(data);
        }
    }
}
