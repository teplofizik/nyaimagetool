using System;
using System.Text;

namespace NyaFsAncientFs
{
    /// <summary>
    /// SCP flux reader for Apple II 5.25" images. Flux timing is converted to
    /// bitcells with a PLL and decoded with the shared Apple II 6-and-2 GCR
    /// decoder.
    /// </summary>
    public sealed class ScpImage
    {
        private const int HeaderSize = 16;
        private const int TrackTableEntries = 168;
        private const int Tracks = 35;
        private const int SectorsPerTrack = 16;
        private const int SectorSize = 256;
        private const double Clock = 160.0;
        private const double ClockMin = Clock * 0.90;
        private const double ClockMax = Clock * 1.10;
        private const double PeriodAdj = 0.05;
        private const double PhaseAdj = 0.60;

        public readonly byte[] ProDosImage;

        private ScpImage(byte[] ProDosImage)
        {
            this.ProDosImage = ProDosImage;
        }

        public static bool TryRead(byte[] Data, out ScpImage Image)
        {
            Image = null;
            if ((Data == null) || (Data.Length < HeaderSize + TrackTableEntries * 4))
                return false;
            if ((Data[0] != (byte)'S') || (Data[1] != (byte)'C') || (Data[2] != (byte)'P'))
                return false;

            var Output = new byte[Tracks * SectorsPerTrack * SectorSize];
            bool Found = false;

            for (int Track = 0; Track < Tracks; Track++)
            {
                int Index = Track * 2;
                if (Index + 1 >= TrackTableEntries)
                    continue;

                int Offset = ReadLe32(Data, HeaderSize + Index * 4);
                if ((Offset <= 0) || (Offset + 16 > Data.Length))
                    continue;
                if ((Data[Offset] != (byte)'T') || (Data[Offset + 1] != (byte)'R') || (Data[Offset + 2] != (byte)'K'))
                    continue;

                int FluxCount = ReadLe32(Data, Offset + 8);
                int DataOffset = ReadLe32(Data, Offset + 12);
                int FluxOffset = Offset + DataOffset;
                if ((FluxCount <= 0) || (DataOffset <= 0) || (FluxOffset + (long)FluxCount * 2 > Data.Length))
                    continue;

                var Bits = DecodeFlux(Data, FluxOffset, FluxCount);
                WozImage.DecodeBits(Bits, Track, Output, Data[Offset + 3]);
                Found = true;
            }

            if (!Found)
                return false;

            Image = new ScpImage(Output);
            return true;
        }

        private static string DecodeFlux(byte[] Data, int Offset, int Count)
        {
            var Result = new StringBuilder();
            double ClockValue = Clock;
            double Ticks = 0;
            long Pending = 0;

            for (int I = 0; I < Count; I++)
            {
                int Interval = (Data[Offset + I * 2] << 8) | Data[Offset + I * 2 + 1];
                if (Interval == 0)
                {
                    Pending += 65536;
                    continue;
                }

                Ticks += Pending + Interval;
                Pending = 0;
                if (Ticks < ClockValue / 2)
                    continue;

                int Zeros = 0;
                while (true)
                {
                    Ticks -= ClockValue;
                    if (Ticks < ClockValue / 2)
                        break;
                    Zeros++;
                    Result.Append('0');
                }
                Result.Append('1');

                double NewTicks = Ticks * (1 - PhaseAdj);
                if (Zeros <= 3)
                    ClockValue += Ticks * PeriodAdj;
                else
                    ClockValue += (Clock - ClockValue) * PeriodAdj;

                if (ClockValue < ClockMin) ClockValue = ClockMin;
                if (ClockValue > ClockMax) ClockValue = ClockMax;
                Ticks = NewTicks;
            }

            return Result.ToString();
        }

        private static int ReadLe32(byte[] Data, int Offset) =>
            Data[Offset] | (Data[Offset + 1] << 8) | (Data[Offset + 2] << 16) | (Data[Offset + 3] << 24);
    }
}
