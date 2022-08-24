// Copyright 2017 Derek Will
// The code in this file is licensed under the Simplified BSD License. See LICENSE.txt for details.

using System;

namespace CrcSharp
{
	/// <summary>
	/// CRC algorithm parameters.
	/// </summary>
	public class CrcParameters
	{
		private readonly int _width;
		private readonly ulong _polynomial;
		private readonly ulong _initialValue;
		private readonly ulong _xorOutValue;
		private readonly bool _reflectIn;
		private readonly bool _reflectOut;

		/// <summary>
		/// Gets the width of the CRC algorithm in bits.
		/// </summary>
		/// <value>The width of the CRC algorithm in bits.</value>
		public int Width 
		{
			get 
			{
				return _width;
			}
		}

		/// <summary>
		/// Gets the polynomial of the CRC algorithm.
		/// </summary>
		/// <value>The polynomial of the CRC algorithm.</value>
		public ulong Polynomial 
		{
			get 
			{
				return _polynomial;
			}
		}

		/// <summary>
		/// Gets the initial value used in the computation of the CRC check value.
		/// </summary>
		/// <value>The initial value used in the computation of the CRC check value.</value>
		public ulong InitialValue
		{
			get 
			{
				return _initialValue;
			}
		}

		/// <summary>
		/// Gets the value which is XORed to the final computed value before returning the check value.
		/// </summary>
		/// <value>The value which is XORed to the final computed value before returning the check value.</value>
		public ulong XorOutValue
		{
			get
			{
				return _xorOutValue;
			}
		}

		/// <summary>
		/// Gets a value indicating whether bytes are reflected before being processed.
		/// </summary>
		/// <value><c>true</c> if each byte is to be reflected before being processed; otherwise, <c>false</c>.</value>
		public bool ReflectIn
		{
			get 
			{
				return _reflectIn;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the final computed value is reflected before the XOR stage.
		/// </summary>
		/// <value><c>true</c> if the final computed value is reflected before the XOR stage; otherwise, <c>false</c>.</value>
		public bool ReflectOut
		{
			get 
			{
				return _reflectOut;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CrcSharp.CrcParameters"/> class.
		/// </summary>
		/// <param name="width">Width of the CRC algorithm in bits.</param>
		/// <param name="polynomial">Polynomial of the CRC algorithm.</param>
		/// <param name="initialValue">Initial value used in the computation of the CRC check value.</param>
		/// <param name="xorOutValue">The value which is XORed to the final computed value before returning the check value.</param>
		/// <param name="reflectIn">If set to <c>true</c> each byte is to be reflected before being processed.</param>
		/// <param name="reflectOut">If set to <c>true</c> the final computed value is reflected before the XOR stage.</param>
		public CrcParameters (int width, ulong polynomial, ulong initialValue, ulong xorOutValue, bool reflectIn, bool reflectOut)
		{
			ThrowIfParametersInvalid (width, polynomial, initialValue, xorOutValue);

			_width = width;
			_polynomial = polynomial;
			_initialValue = initialValue;
			_xorOutValue = xorOutValue;
			_reflectIn = reflectIn;
			_reflectOut = reflectOut;
		}

		/// <summary>
		/// Verifies if the parameter values are valid.
		/// </summary>
		/// <param name="width">Width of the CRC algorithm in bits.</param>
		/// <param name="polynomial">Polynomial of the CRC algorithm.</param>
		/// <param name="initialValue">Initial value used in the computation of the CRC check value.</param>
		/// <param name="xorOutValue">The value which is XORed to the final computed value before returning the check value.</param>
		private void ThrowIfParametersInvalid(int width, ulong polynomial, ulong initialValue, ulong xorOutValue)
		{
			if (width < 8 || width > 64)
				throw new ArgumentOutOfRangeException ("width", "Width must be between 8-64 bits.");

			ulong maxValue = (UInt64.MaxValue >> (64 - width));

			if (polynomial > maxValue)
				throw new ArgumentOutOfRangeException ("polynomial", string.Format("Polynomial exceeds {0} bits.", width));

			if (initialValue > maxValue)
				throw new ArgumentOutOfRangeException ("initialValue", string.Format("Initial Value exceeds {0} bits.", width));

			if (xorOutValue > maxValue)
				throw new ArgumentOutOfRangeException ("xorOutValue", string.Format ("XOR Out Value exceeds {0} bits.", width));
		}
	}
}

