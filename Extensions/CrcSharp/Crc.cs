// Copyright 2017 Derek Will
// The code in this file is licensed under the Simplified BSD License. See LICENSE.txt for details.

using System;
using System.Linq;

namespace CrcSharp
{
	/// <summary>
	/// CRC algorithm.
	/// </summary>
	public class Crc
	{
		private readonly CrcParameters _parameters;
		private readonly ulong[] _lookupTable; 

		/// <summary>
		/// Gets the CRC algorithm parameters.
		/// </summary>
		/// <value>The CRC algorithm parameters.</value>
		public CrcParameters Parameters
		{
			get 
			{
				return _parameters;
			}
		}

		/// <summary>
		/// Gets the lookup table used in calculating check values.
		/// </summary>
		/// <value>The lookup table.</value>
		public ulong[] LookupTable
		{
			get 
			{
				return _lookupTable;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CrcSharp.Crc"/> class.
		/// </summary>
		/// <param name="parameters">CRC algorithm parameters.</param>
		public Crc(CrcParameters parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException ("parameters", "Parameters argument cannot be null.");

			_parameters = parameters;
			_lookupTable = GenerateLookupTable();
		}

		/// <summary>
		/// Calculates the CRC check value as a numeric value.
		/// </summary>
		/// <returns>The CRC check value as a numeric value.</returns>
		/// <param name="data">Data to compute the check value of.</param>
		public ulong CalculateAsNumeric(byte[] data)
		{
			byte[] crcCheckVal = CalculateCheckValue (data);
			Array.Resize (ref crcCheckVal, 8);
			return BitConverter.ToUInt64(crcCheckVal, 0);
		}

		/// <summary>
		/// Calculates the CRC check value as a byte array.
		/// </summary>
		/// <returns>The CRC check value as a byte array.</returns>
		/// <param name="data">Data to compute the check value of.</param>
		public byte[] CalculateCheckValue(byte[] data)
		{
			if (data == null)
				throw new ArgumentNullException ("data", "Data argument cannot be null.");

			ulong crc = _parameters.InitialValue;

			if (_parameters.ReflectIn) 
			{
				crc = ReflectBits (crc, _parameters.Width);
			}

			foreach (byte b in data) 
			{
				if (_parameters.ReflectIn) 
				{
					crc = _lookupTable [(crc ^ b) & 0xFF] ^ (crc >> 8);
				} 
				else 
				{
					crc = _lookupTable[((crc >> (_parameters.Width - 8)) ^ b) & 0xFF] ^ (crc << 8);
				}

				crc &= (UInt64.MaxValue >> (64 - _parameters.Width));
			}

			// Source: https://stackoverflow.com/questions/28656471/how-to-configure-calculation-of-crc-table/28661073#28661073
			// Per Mark Adler - ...the reflect out different from the reflect in (CRC-12/3GPP). 
			// In that one case, you need to bit reverse the output since the input is not reflected, but the output is.
			if (_parameters.ReflectIn ^ _parameters.ReflectOut) 
			{
				crc = ReflectBits (crc, _parameters.Width);
			}

			ulong crcFinalValue = crc ^ _parameters.XorOutValue;
			return BitConverter.GetBytes(crcFinalValue).Take((_parameters.Width + 7)/ 8).ToArray();
		}

		/// <summary>
		/// Generates the lookup table using the CRC algorithm parameters.
		/// </summary>
		/// <returns>The lookup table.</returns>
		private ulong[] GenerateLookupTable()
		{
			if (_parameters == null)
				throw new InvalidOperationException ("CRC parameters must be set prior to calling this method.");

			var lookupTable = new ulong[256];
			ulong topBit = (ulong)((ulong)1 << (_parameters.Width - 1));

			for (int i = 0; i < lookupTable.Length; i++) 
			{
				byte inByte = (byte)i;
				if (_parameters.ReflectIn) 
				{
					inByte = (byte)ReflectBits(inByte, 8);
				}

				ulong r = (ulong)((ulong)inByte << (_parameters.Width - 8));
				for (int j = 0; j < 8; j++)
				{
					if ((r & topBit) != 0)
					{
						r = ((r << 1) ^ _parameters.Polynomial);
					}
					else
					{
						r = (r << 1);
					}
				}

				if (_parameters.ReflectIn)
				{
					r = ReflectBits(r, _parameters.Width);
				}

				lookupTable[i] = r & (UInt64.MaxValue >> (64 - _parameters.Width));
			}

			return lookupTable;
		}

		/// <summary>
		/// Reflects the bits of a provided numeric value.
		/// </summary>
		/// <returns>Bit-reflected version of the provided numeric value.</returns>
		/// <param name="b">Value to reflect the bits of.</param>
		/// <param name="bitCount">Number of bits in the provided value.</param>
		private static ulong ReflectBits(ulong b, int bitCount)
		{
			ulong reflection = 0x00;

			for (int bitNumber = 0; bitNumber < bitCount; ++bitNumber)
			{
				if (((b >> bitNumber) & 0x01) == 0x01)
				{
					reflection |= (ulong)(((ulong)1 << ((bitCount - 1) - bitNumber)));
				}
			}

			return reflection;
		}
	}
}

