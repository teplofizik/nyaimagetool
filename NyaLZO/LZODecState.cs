using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NyaLZO
{
    class LZODecState
    {
        BytePointer m_pos = null;
        BytePointer ip;
        BytePointer ip_end;
        BytePointer op;

        long t = 0;
        long next = 0;
        long state = 0;

        LZOState decstate = LZOState.Default;

        public LZODecState(byte[] Data, uint BlockSize)
        {
            if (Data.Length < 3)
                throw new ArgumentException("Input length is too small!");

            ip = new BytePointer(Data);
            ip_end = new BytePointer(Data, Data.Length);
            op = new BytePointer(new byte[BlockSize]);
        }

        /// <summary>
        /// Reimplemented lzo1x_decompress_safe
        /// https://elixir.bootlin.com/linux/v4.8/source/lib/lzo/lzo1x_decompress_safe.c#L38
        /// </summary>
        /// <returns></returns>
        public byte[] Decompress()
        {
            decstate = LZOState.Default;

            if (ip.Value > 17)
            {
                t = ip.GetInc() - 17;
                if (t < 4)
                {
                    next = t;
                    decstate = LZOState.MatchNext;
                }
                else
                    decstate = LZOState.CopyLiteralRun;
            }

            while (true)
            {
                switch(decstate)
                {
                    case LZOState.Default: DefaultProcessing(); break;
                    case LZOState.CopyLiteralRun: CopyLiteralRun(); break;
                    case LZOState.MatchNext: MatchNext(); break;
                    case LZOState.EOFFound: EOFFound(); break;
                }

                if (decstate == LZOState.EOFFound) break;
            }

            return op.Result;
        }

        private void DefaultProcessing()
        {
            //Debug.WriteLine($"Default: {ip.CurrentOffset:x08} {op.CurrentOffset:x08} {t}");
            t = ip.GetInc();
            if (t < 16)
            {
                if (state == 0)
                {
                    if (t == 0) ProcessPart(15);
                    t += 3;
                    decstate = LZOState.CopyLiteralRun;
                    return;
                }
                else if (state != 4)
                {
                    next = t & 3;
                    m_pos = op - 1;
                    m_pos -= t >> 2;
                    m_pos -= ip.GetInc() << 2;

                    op[0] = m_pos[0];
                    op[1] = m_pos[1];
                    op += 2;
                    decstate = LZOState.MatchNext;
                    return;
                }
                else
                {
                    next = t & 3;
                    m_pos = op - (1 + M2_MAX_OFFSET);
                    m_pos -= t >> 2;
                    m_pos -= ip.GetInc() << 2;
                    t = 3;
                }
            }
            else if (t >= 64)
            {
                next = t & 3;
                m_pos = op - 1;
                m_pos -= (t >> 2) & 0x07;
                m_pos -= ip.GetInc() << 3;
                t = (t >> 5) - 1 + (3 - 1);
            }
            else if (t >= 32)
            {
                t = (t & 31) + (3 - 1);
                if(t == 2)
                {
                    ProcessPart(31);
                    NEED_IP(2);
                }
                m_pos = op - 1;
                next = ip.UShortValue;
                ip += 2;
                m_pos -= (next >> 2);
                next &= 3;
            }
            else
            {
                m_pos = op;
                m_pos -= (t & 8) << 11;
                t = (t & 7) + (3 - 1);
                if (t == 2)
                {
                    ProcessPart(7);
                    NEED_IP(2);
                }
                next = ip.UShortValue;
                ip += 2;
                m_pos -= (next >> 2);
                next &= 3;
                if (m_pos == op)
                {
                    decstate = LZOState.EOFFound;
                    return;
                }

                m_pos -= 0x4000;
            }
            //Debug.WriteLine($"Copy from pos {ip.CurrentOffset:x08} {op.CurrentOffset:x08}: {m_pos.CurrentOffset:x08} {t} bytes");
            op.CopyInc(m_pos, t);

            decstate = LZOState.MatchNext;
            return;
        }

        private void CopyLiteralRun()
        {
            //Debug.WriteLine($"CopyLiteralRun {ip.CurrentOffset:x08} {op.CurrentOffset:x08}: {t} bytes");
            NEED_IP(t + 3);
            //DumpBytes(ip, t);
            op.CopyInc(ip, t);
            state = 4;
            t = 0;
            decstate = LZOState.Default;
        }

        private void MatchNext()
        {
            //Debug.WriteLine($"MatchNext {ip.CurrentOffset:x08} {op.CurrentOffset:x08}: {t} bytes");
            state = next;
            t = next;

            NEED_IP(t + 3);
            //DumpBytes(ip, t);
            op.CopyInc(ip, t);
            t = 0;
            decstate = LZOState.Default;
        }
        private void DumpBytes(BytePointer pointer, long size)
        {
            var dump = pointer + 0;
            for (int i = 0; i < t; i++)
            {
                Debug.Write($"{dump.GetInc():x02} ");
            }
            Debug.WriteLine("");
        }

        private void EOFFound()
        {
            //Debug.WriteLine($"EOFFound {ip.CurrentOffset:x08} {op.CurrentOffset:x08}");
            if (t != 3)
                throw new InvalidOperationException("Invalid state at end of data!");
            if (ip != ip_end)
                throw new InvalidOperationException("Detected EOF, but input data has unprocessed part!");
        }

        private void ProcessPart(long Offset)
        {
            //Debug.WriteLine($"  ProcessPart {ip.CurrentOffset}");
            var ip_last = ip + 0;
            while (ip.Value == 0)
            {
                ip++;
                NEED_IP(1);
            }
            var offset = ip - ip_last;
            if (offset > MAX_255_COUNT)
                throw new InvalidOperationException("LZO ERROR");

            offset = (offset << 8) - offset;
            t += offset + Offset + ip.GetInc();
        }

        private bool HAVE_IP(long Size) => (ip_end - ip) >= Size;

        private void NEED_IP(long Size)
        {
            if (!HAVE_IP(Size)) throw new OverflowException("No enough input data");
        }

        private const uint MAX_255_COUNT = 255 * 2;

        private enum LZOState
        {
            Default,
            MatchNext,
            CopyLiteralRun,
            EOFFound
        }

        private const uint M1_MAX_OFFSET = 0x0400;
        private const uint M2_MAX_OFFSET = 0x0800;
        private const uint M3_MAX_OFFSET = 0x4000;
        private const uint M4_MAX_OFFSET = 0xbfff;

        // ===================================================
        private const uint M2_MAX_LEN = 8;
        private const uint M3_MAX_LEN = 33;
        private const uint M4_MAX_LEN = 9;
        private const byte M3_MARKER = 32;
        private const byte M4_MARKER = 16;
        private const byte BITS = 14;
        private const uint D_MASK = (1 << BITS) - 1;
        private const uint DICT_SIZE = 65536 + 3;

        private static uint D_MUL(uint A, uint B) => A * B;
        private static uint D_X2(BytePointer input, byte s1, byte s2) => (uint)((((input[2] << s2) ^ input[1]) << s1) ^ input[0]);
        private static uint D_X3(BytePointer input, byte s1, byte s2, byte s3) => (D_X2(input + 1, s2, s3) << s1) ^ input[0];
        private static uint D_MS(uint v, byte s) => (v & (D_MASK >> s)) << s;
        private static uint D_INDEX2(uint idx) => (idx & (D_MASK & 0x7FF)) ^ (((D_MASK >> 1) + 1) | 0x1F);
        private static uint D_INDEX1(BytePointer input) => D_MS(D_MUL(0x21, D_X3(input, 5, 5, 6)) >> 5, 0);
    }
}
