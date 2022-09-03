using System;
using System.Collections.Generic;
using System.Text;

//
// teplofizik 2022
// No unsafe code
//

/**
 * Based on ManagedLZO.MiniLZO
 * 
 * Minimalistic reimplementation of minilzo in C#
 * 
 * @author Shane Eric Bryldt, Copyright (C) 2006, All Rights Reserved
 * @note Uses unsafe/fixed pointer contexts internally
 * @liscence Bound by same licence as minilzo as below, see file COPYING
 */

/* Based on minilzo.c -- mini subset of the LZO real-time data compression library

   This file is part of the LZO real-time data compression library.

   Copyright (C) 2005 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2004 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2003 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2002 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2001 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 2000 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1999 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1998 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1997 Markus Franz Xaver Johannes Oberhumer
   Copyright (C) 1996 Markus Franz Xaver Johannes Oberhumer
   All Rights Reserved.

   The LZO library is free software; you can redistribute it and/or
   modify it under the terms of the GNU General Public License,
   version 2, as published by the Free Software Foundation.

   The LZO library is distributed in the hope that it will be useful,
   but WITHOUT ANY WARRANTY; without even the implied warranty of
   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
   GNU General Public License for more details.

   You should have received a copy of the GNU General Public License
   along with the LZO library; see the file COPYING.
   If not, write to the Free Software Foundation, Inc.,
   51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.

   Markus F.X.J. Oberhumer
   <markus@oberhumer.com>
   http://www.oberhumer.com/opensource/lzo/
 */

/*
 * NOTE:
 *   the full LZO package can be found at
 *   http://www.oberhumer.com/opensource/lzo/
 */

namespace NyaLZO
{
    class BytePointer 
    {
        private const int BlockSize = 1024;
        private byte[] Data;
        private long Offset;

        public BytePointer(byte[] Data, long Offset)
        {
            if (Offset < 0)
                throw new ArgumentException("Offset is negative!");
            if(Offset > Data.Length)
                throw new ArgumentException("Offset is greater than data size!");

            this.Data = Data;
            this.Offset = Offset;
        }

        public BytePointer(byte[] Data)
        {
            this.Data = Data;
            Offset = 0;
        }

        public BytePointer()
        {
            this.Data = new byte[BlockSize];
            Offset = 0;
        }


        public static BytePointer operator + (BytePointer pointer, long Offset) => new BytePointer(pointer.Data, pointer.Offset + Offset);
        public static BytePointer operator - (BytePointer pointer, long Offset) => new BytePointer(pointer.Data, pointer.Offset - Offset);
        public static long operator - (BytePointer a, BytePointer b)
        {
           // if (a.Data != b.Data)
           //     throw new ArgumentException("Cannot compare pointers to different arrays");

            return a.Offset - b.Offset;
        }

        private static bool Compare(BytePointer a, BytePointer b, Func<long,long,bool> comp)
        {
            //if (a.Data != b.Data)
            //    throw new ArgumentException("Cannot compare pointers to different arrays");

            return comp(a.Offset, b.Offset);
        }

        public override bool Equals(object obj)
        {
            var o = obj as BytePointer;
            if(o != null)
                return (o.Data == Data) && (o.Offset == Offset);
            else
                return false;
        }

        public override int GetHashCode() => HashCode.Combine(Data, Offset);

        public static bool operator !=(BytePointer a, BytePointer b) => Compare(a, b, (oa, ob) => oa != ob);

        public static bool operator ==(BytePointer a, BytePointer b) => Compare(a, b, (oa, ob) => oa == ob);

        public static bool operator <(BytePointer a, BytePointer b) => Compare(a, b, (oa, ob) => oa < ob);

        public static bool operator > (BytePointer a, BytePointer b) => Compare(a, b, (oa, ob) => oa > ob);

        public static BytePointer operator ++(BytePointer pointer)
        {
            pointer.Offset++;
            return pointer;
        }

        public static BytePointer operator --(BytePointer pointer)
        {
            pointer.Offset--;
            return pointer;
        }

        public UInt16 UShortValue=> Convert.ToUInt16((this[0]) | (this[1] << 8));

        public void CopyInc(BytePointer src, long Count = 1)
        {
            for (int i = 0; i < Count; i++)
            {
                Value = src.Value;
                Offset++;
                src++;
            }
        }

        public byte GetInc()
        {
            var V = Value;
            Offset++;
            return V;
        }

        public byte Value
        {
            get { return this[0]; }
            set { this[0] = value; }
        }

        public long CurrentOffset => Offset;

        public byte this[int Index]
        {
            get { return Data[Offset + Index]; }
            set {
                var Idx = Offset + Index;
                if(Idx < Data.Length)
                {
                    long NeedSize = Data.Length + BlockSize;
                    while(NeedSize < Idx) NeedSize += BlockSize;

                    var Temp = new byte[NeedSize];
                    Data.CopyTo(Temp, 0);
                    Data = Temp;
                }

                Data[Idx] = value; 
            }
        }

        public byte[] Result
        {
            get
            {
                byte[] Res = new byte[Offset];
                Array.Copy(Data, Res, Offset);
                return Res;
            }
        }
    }
}
