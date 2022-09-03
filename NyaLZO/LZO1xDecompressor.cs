using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NyaLZO
{
    /// <summary>
    ///  MiniLZO-based decompressor
    /// </summary>
    public class LZO1xDecompressor
    {
        private const uint M2_MAX_LEN = 8;
        private const uint M3_MAX_LEN = 33;
        private const uint M4_MAX_LEN = 9;
        private const byte M3_MARKER = 32;
        private const byte M4_MARKER = 16;
        private const uint M1_MAX_OFFSET = 0x0400;
        private const uint M2_MAX_OFFSET = 0x0800;
        private const uint M3_MAX_OFFSET = 0x4000;
        private const uint M4_MAX_OFFSET = 0xbfff;
        private const byte BITS = 14;
        private const uint D_MASK = (1 << BITS) - 1;
        private const uint DICT_SIZE = 65536 + 3;

        /// <summary>
        /// It works sometimes. Maybe this implementation has bugs or implements old version and cannot decompress any lzo1x_999 blocks (as original MiniLZO)
        /// Need reimplement lzo1x_decompress_safe
        /// https://elixir.bootlin.com/linux/v4.8/source/lib/lzo/lzo1x_decompress_safe.c#L38
        /// </summary>
        /// 
        public static byte[] Decompress(byte[] Src)
        {
            uint t = 0;
            var ip = new BytePointer(Src);
            var op = new BytePointer();

            BytePointer pos = null;

            var ip_end = new BytePointer(Src, Src.Length);

            bool match = false;
            bool match_next = false;
            bool match_done = false;
            bool copy_match = false;
            bool first_literal_run = false;
            bool eof_found = false;

            if (ip.Value > 17)
            {
                t = (uint)(ip.GetInc() - 17);

                if (t < 4)
                    match_next = true;
                else
                {
                    Debug.Assert(t > 0);
                    if ((ip_end - ip) < t + 1)
                        throw new OverflowException("Input Overrun");

                    op.CopyInc(ip, t);
                    first_literal_run = true;
                }

                while (!eof_found && ip < ip_end)
                {
                    if (!match_next && !first_literal_run)
                    {
                        t = ip.GetInc();
                        if (t >= 16)
                            match = true;
                        else
                        {
                            if (t == 0)
                            {
                                if ((ip_end - ip) < 1)
                                    throw new OverflowException("Input Overrun");
                                while (ip.Value == 0)
                                {
                                    t += 255;
                                    ip++;
                                    if ((ip_end - ip) < 1)
                                        throw new OverflowException("Input Overrun");
                                }
                                t += (uint)(15 + ip.GetInc());
                            }
                            Debug.Assert(t > 0);

                            if ((ip_end - ip) < t + 4)
                                throw new OverflowException("Input Overrun");

                            op.CopyInc(ip, 4 + t - 1);
                        }
                    }

                    if (!match && !match_next)
                    {
                        first_literal_run = false;

                        t = ip.GetInc();
                        if (t >= 16)
                            match = true;
                        else
                        {
                            pos = op - (1 + M2_MAX_OFFSET);
                            pos -= t >> 2;
                            pos -= ip.GetInc() << 2;

                            op.CopyInc(pos, 3);
                            match_done = true;
                        }
                    }
                    match = false;

                    do
                    {
                        if (t >= 64)
                        {
                            pos = op - 1;
                            pos -= (t >> 2) & 7;
                            pos -= ip.GetInc() << 3;

                            t = (t >> 5) - 1;

                            copy_match = true;
                        }
                        else if (t >= 32)
                        {
                            t &= 31;
                            if (t == 0)
                            {
                                if ((ip_end - ip) < 1)
                                    throw new OverflowException("Input Overrun");
                                while (ip.Value == 0)
                                {
                                    t += 255;
                                    ++ip;
                                    if ((ip_end - ip) < 1)
                                        throw new OverflowException("Input Overrun");
                                }
                                t += (uint)(31 + ip.GetInc());
                            }
                            pos = op - 1;
                            pos -= ip.UShortValue >> 2;
                            ip += 2;

                        }
                        else if (t >= 16)
                        {
                            pos = op;
                            pos -= (t & 8) << 11;

                            t &= 7;
                            if (t == 0)
                            {
                                if ((ip_end - ip) < 1)
                                    throw new OverflowException("Input Overrun");

                                while (ip.Value == 0)
                                {
                                    t += 255;
                                    ++ip;
                                    if ((ip_end - ip) < 1)
                                        throw new OverflowException("Input Overrun");
                                }
                                t += (uint)(7 + ip.GetInc());
                            }
                            pos -= ip.UShortValue >> 2;
                            ip += 2;
                            if (pos == op)
                                eof_found = true;
                            else
                                pos -= 0x4000;
                        }
                        else
                        {
                            pos = op - 1;
                            pos -= t >> 2;
                            pos -= ip.GetInc() << 2;

                            op.CopyInc(pos, 2);
                            match_done = true;
                        }
                        if (!eof_found && !match_done && !copy_match)
                        {
                            Debug.Assert(t > 0);
                        }
                        if (!eof_found && t >= 2 * 4 - 2 && (op - pos) >= 4 && !match_done && !copy_match)
                        {
                            op.CopyInc(pos, 4);

                            t -= 2;
                            if (t > 0)
                                op.CopyInc(pos, t);
                        }
                        else if (!eof_found && !match_done)
                        {
                            copy_match = false;

                            op.CopyInc(pos, 2 + t);
                        }

                        if (!eof_found && !match_next)
                        {
                            match_done = false;

                            t = (uint)(ip[-2] & 3);
                            if (t == 0)
                                break;
                        }
                        if (!eof_found)
                        {
                            match_next = false;
                            Debug.Assert(t > 0);
                            Debug.Assert(t < 4);
                            if ((ip_end - ip) < t + 1)
                                throw new OverflowException("Input Overrun");

                            op.CopyInc(ip, t);
                            t = ip.GetInc();
                        }
                    } while (!eof_found && ip < ip_end);
                }

                if (!eof_found)
                    throw new OverflowException("EOF Marker Not Found");
                else
                {
                    Debug.Assert(t == 1);
                    if (ip > ip_end)
                        throw new OverflowException("Input Overrun");
                    else if (ip < ip_end)
                        throw new OverflowException("Input Not Consumed");
                }
            }

            return op.Result;
        }

        private static uint D_MUL(uint A, uint B) => A * B;
        private static uint D_X2(BytePointer input, byte s1, byte s2) => (uint)((((input[2] << s2) ^ input[1]) << s1) ^ input[0]);
        private static uint D_X3(BytePointer input, byte s1, byte s2, byte s3) => (D_X2(input + 1, s2, s3) << s1) ^ input[0];
        private static uint D_MS(uint v, byte s) => (v & (D_MASK >> s)) << s;
        private static uint D_INDEX2(uint idx) => (idx & (D_MASK & 0x7FF)) ^ (((D_MASK >> 1) + 1) | 0x1F);
        private static uint D_INDEX1(BytePointer input) => D_MS(D_MUL(0x21, D_X3(input, 5, 5, 6)) >> 5, 0);
    }
}
