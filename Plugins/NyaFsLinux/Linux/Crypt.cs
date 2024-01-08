using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace NyaFsLinux.Linux
{
	// https://gist.github.com/ygoe/33832285434eac3dc299a2b821470d19#file-managedunixcrypt-cs

	/// <summary>
	/// A managed implementation of the Unix C library crypt function. It supports the MD5, SHA-256
	/// and SHA-512 algorithms.
	/// </summary>
	/// <remarks>
	/// This code is based on https://github.com/ahall/PasswordSharp, which is based on
	/// https://gist.github.com/otac0n/1092558 and adds SHA-512 support. This is an implementation
	/// of the crypt format described in http://www.akkadia.org/drepper/SHA-crypt.txt. The code has
	/// been cleaned up, modernised and optimised for current C# versions.
	/// </remarks>
	public static class ManagedUnixCrypt
	{
		#region Public constants

		public const string TypeMD5 = "$1$";
		public const string TypeSHA256 = "$5$";
		public const string TypeSHA512 = "$6$";

		public const string DefaultType = TypeSHA512;

		#endregion Public constants

		#region Public methods

		/// <summary>
		/// Computes the crypt value for the specified password and salt.
		/// </summary>
		/// <param name="password">The plaintext password.</param>
		/// <param name="salt">The salt, including the algorithm and its parameters.</param>
		/// <returns>The crypt value, including the chosen parameters.</returns>
		public static string Crypt(string password, string salt)
		{
			var keyPtr = new ArrayPointer<byte>(Encoding.UTF8.GetBytes(password + "\0"));
			var saltPtr = new ArrayPointer<byte>(Encoding.UTF8.GetBytes(salt + "\0"));

			if (Strncmp(md5SaltPrefix, saltPtr, Strlen(md5SaltPrefix)) == 0)
			{
				return CryptMD5(keyPtr, saltPtr);
			}
			if (Strncmp(sha256SaltPrefix, saltPtr, Strlen(sha256SaltPrefix)) == 0)
			{
				return CryptSHA256(keyPtr, saltPtr);
			}
			if (Strncmp(sha512SaltPrefix, saltPtr, Strlen(sha512SaltPrefix)) == 0)
			{
				return CryptSHA512(keyPtr, saltPtr);
			}
			throw new ArgumentException("Unsupported algorithm");
		}

		/// <summary>
		/// Verifies a plaintext password against a crypt value.
		/// </summary>
		/// <param name="hash">The crypt value.</param>
		/// <param name="password">The plaintext password.</param>
		/// <returns>true, if the crypt value matches the password; otherwise, false.</returns>
		public static bool Verify(string hash, string password)
		{
			var splittedHash = SplittedHash.Parse(hash);
			string newHash = Crypt(password, splittedHash.GetFullSalt());
			return ConstantTimeEquals(hash, newHash);
		}

		/// <summary>
		/// Generates a random salt for a new crypt value.
		/// </summary>
		/// <param name="algoType">The crypt algorithm type.</param>
		/// <param name="rounds">The number of rounds. This is ignored for MD5.</param>
		/// <returns>The formatted salt string, including the chosen parameters.</returns>
		public static string GenerateSalt(string algoType = DefaultType, int rounds = RoundsDefault)
		{
			// Base64 has an overhead of 4/3, so we need less bytes to get 8 resp. 16 chars.
			int saltByteCount = algoType == TypeMD5 ? 6 : 12;
			byte[] randomBytes = RandomNumberGenerator.GetBytes(saltByteCount);

			string roundsParam = "";
			if (algoType != TypeMD5 && rounds != RoundsDefault)
			{
				roundsParam = $"rounds={rounds}$";
			}
			string randomB64 = Convert.ToBase64String(randomBytes);
			return $"{algoType}{roundsParam}{randomB64}";
		}

		#endregion Public methods

		#region Private constants

		// Table with characters for base64 transformation.
		private const string B64Chars = "./0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

		// Define our magic string to mark salt for MD5 "encryption" replacement. This is meant to
		// be the same as for other MD5 based encryption implementations.
		private static ArrayPointer<byte> md5SaltPrefix = new(new byte[] { (byte)'$', (byte)'1', (byte)'$', 0 });
        private static ArrayPointer<byte> dollarSign = new(new byte[] { (byte)'$', 0 });

		// Define our magic string to mark salt for SHA256 "encryption" replacement.
		private static ArrayPointer<byte> sha256SaltPrefix = new(new byte[] { (byte)'$', (byte)'5', (byte)'$', 0 });
		private static ArrayPointer<byte> sha512SaltPrefix = new(new byte[] { (byte)'$', (byte)'6', (byte)'$', 0 });

		// Prefix for optional rounds specification.
		private static ArrayPointer<byte> sha256RoundsPrefix = new(new byte[] { (byte)'r', (byte)'o', (byte)'u', (byte)'n', (byte)'d', (byte)'s', (byte)'=', 0 });
		private static ArrayPointer<byte> sha512RoundsPrefix = new(new byte[] { (byte)'r', (byte)'o', (byte)'u', (byte)'n', (byte)'d', (byte)'s', (byte)'=', 0 });

		// Maximum salt string length
		private const int SaltLenMax = 16;

		// Default number of rounds if not explicitly specified
		private const int RoundsDefault = 5000;

		// Minimum number of rounds
		private const int RoundsMin = 1000;

		// Maximum number of rounds
		private const int RoundsMax = 999999999;

		#endregion Private constants

		#region Crypt implementation

		private static string CryptMD5(ArrayPointer<byte> key, ArrayPointer<byte> salt)
		{
			// We don't want to have an arbitrary limit in the size of the password. We can compute
			// the size of the result in advance and so we can prepare the buffer we pass to
			// MD5CryptR.
			int buflen = Strlen(md5SaltPrefix) +
				Strlen(salt) + 1 + 26 + 1;
			var buffer = new ArrayPointer<byte>(new byte[buflen]);
			return ExtractString(MD5CryptR(key, salt, buffer, buflen));
		}

		private static string CryptSHA256(ArrayPointer<byte> key, ArrayPointer<byte> salt)
		{
			// We don't want to have an arbitrary limit in the size of the password. We can compute
			// an upper bound for the size of the result in advance and so we can prepare the buffer
			// we pass to SHA256CryptR.
			int buflen = Strlen(sha256SaltPrefix) +
				Strlen(sha256RoundsPrefix) + 1 + 9 + 1 +
				Strlen(salt) + 1 + 43 + 1;
			var buffer = new ArrayPointer<byte>(new byte[buflen]);
			return ExtractString(SHA256CryptR(key, salt, buffer, buflen));
		}

		private static string CryptSHA512(ArrayPointer<byte> key, ArrayPointer<byte> salt)
		{
			// We don't want to have an arbitrary limit in the size of the password. We can compute
			// an upper bound for the size of the result in advance and so we can prepare the buffer
			// we pass to SHA512CryptR.
			int buflen = Strlen(sha512SaltPrefix) +
				Strlen(sha512RoundsPrefix) + 1 + 9 + 1 +
				Strlen(salt) + 1 + 86 + 1;
			var buffer = new ArrayPointer<byte>(new byte[buflen]);
			return ExtractString(SHA512CryptR(key, salt, buffer, buflen));
		}

		private static ArrayPointer<byte> MD5CryptR(ArrayPointer<byte> key, ArrayPointer<byte> salt, ArrayPointer<byte> buffer, int buflen)
		{
			var altResult = new ArrayPointer<byte>(new byte[16]);
			int cnt;
			using var md5 = MD5.Create();

			// Find beginning of salt string. The prefix should normally always be present.
			// Just in case it is not.
			if (Strncmp(md5SaltPrefix, salt, Strlen(md5SaltPrefix)) == 0)
			{
				salt += Strlen(md5SaltPrefix);
			}

			int saltLen = Math.Min(Strcspn(salt, dollarSign), 8);
			int keyLen = Strlen(key);

			byte[] temp = new byte[key.SourceArray.Length];
			key.SourceArray.CopyTo(temp, 0);
			key = new ArrayPointer<byte>(temp) { Address = key.Address };

			temp = new byte[salt.SourceArray.Length];
			salt.SourceArray.CopyTo(temp, 0);
			salt = new ArrayPointer<byte>(temp) { Address = salt.Address };

			// Prepare for the real work.
			using (var ctx = new MemoryStream())
			{
				// Add the key string.
				ProcessBytes(key, keyLen, ctx);

				// Because the SALT argument need not always have the salt prefix we add it separately.
				ProcessBytes(md5SaltPrefix, Strlen(md5SaltPrefix), ctx);

				// The last part is the salt string. This must be at most 8 characters and it ends
				// at the first '$' character (for compatibility with existing implementations).
				ProcessBytes(salt, saltLen, ctx);

				// Compute alternate MD5 sum with input KEY, SALT, and KEY. The final result will be
				// added to the first context.
				using var altCtx = new MemoryStream();

				// Add key.
				ProcessBytes(key, keyLen, altCtx);

				// Add salt.
				ProcessBytes(salt, saltLen, altCtx);

				// Add key again.
				ProcessBytes(key, keyLen, altCtx);

				// Now get result of this (16 bytes) and add it to the other context.
				FinishCtx(md5, altCtx, altResult);

				// Add for any character in the key one byte of the alternate sum.
				for (cnt = keyLen; cnt > 16; cnt -= 16)
				{
					ProcessBytes(altResult, 16, ctx);
				}
				ProcessBytes(altResult, cnt, ctx);

				// For the following code we need a NUL byte.
				altResult.Value = 0;

				// The original implementation now does something weird: for every 1 bit in the key
				// the first 0 is added to the buffer, for every 0 bit the first character of the
				// key. This does not seem to be what was intended but we have to follow this to be
				// compatible.
				for (cnt = keyLen; cnt > 0; cnt >>= 1)
				{
					ProcessBytes((cnt & 1) != 0 ? altResult : key, 1, ctx);
				}

				// Create intermediate result.
				FinishCtx(md5, ctx, altResult);
			}

			// Now comes another weirdness. In fear of password crackers here comes a quite long
			// loop which just processes the output of the previous round again. We cannot ignore
			// this here.
			for (cnt = 0; cnt < 1000; cnt++)
			{
				// New context.
				using var ctx = new MemoryStream();

				// Add key or last result.
				if ((cnt & 1) != 0)
					ProcessBytes(key, keyLen, ctx);
				else
					ProcessBytes(altResult, 16, ctx);

				// Add salt for numbers not divisible by 3.
				if (cnt % 3 != 0)
					ProcessBytes(salt, saltLen, ctx);

				// Add key for numbers not divisible by 7.
				if (cnt % 7 != 0)
					ProcessBytes(key, keyLen, ctx);

				// Add key or last result.
				if ((cnt & 1) != 0)
					ProcessBytes(altResult, 16, ctx);
				else
					ProcessBytes(key, keyLen, ctx);

				// Create intermediate result.
				FinishCtx(md5, ctx, altResult);
			}

			// Now we can construct the result string. It consists of three parts.
			var cp = Stpncpy(buffer, md5SaltPrefix, Math.Max(0, buflen));
			buflen -= Strlen(md5SaltPrefix);

			cp = Stpncpy(cp, salt, Math.Min(Math.Max(0, buflen), saltLen));
			buflen -= Math.Min(Math.Max(0, buflen), saltLen);

			if (buflen > 0)
			{
				cp.Value = (byte)'$';
				cp++;
				buflen--;
			}

			cp = B64From24bit(altResult[0], altResult[6], altResult[12], 4, cp, ref buflen);
			cp = B64From24bit(altResult[1], altResult[7], altResult[13], 4, cp, ref buflen);
			cp = B64From24bit(altResult[2], altResult[8], altResult[14], 4, cp, ref buflen);
			cp = B64From24bit(altResult[3], altResult[9], altResult[15], 4, cp, ref buflen);
			cp = B64From24bit(altResult[4], altResult[10], altResult[5], 4, cp, ref buflen);
			cp = B64From24bit(0, 0, altResult[11], 2, cp, ref buflen);

			if (buflen <= 0)
				throw new IndexOutOfRangeException();
			else
				cp.Value = 0;   // Terminate the string.

			return buffer;
		}

		private static ArrayPointer<byte> SHA256CryptR(ArrayPointer<byte> key, ArrayPointer<byte> salt, ArrayPointer<byte> buffer, int buflen)
		{
			var altResult = new ArrayPointer<byte>(new byte[32]);
			var tempResult = new ArrayPointer<byte>(new byte[32]);
			int cnt;
			// Default number of rounds.
			int rounds = RoundsDefault;
			bool roundsCustom = false;
			using var sha256 = SHA256.Create();

			// Find beginning of salt string. The prefix should normally always be present.
			// Just in case it is not.
			if (Strncmp(sha256SaltPrefix, salt, Strlen(sha256SaltPrefix)) == 0)
			{
				// Skip salt prefix.
				salt += Strlen(sha256SaltPrefix);
			}

			if (Strncmp(salt, sha256RoundsPrefix, Strlen(sha256RoundsPrefix)) == 0)
			{
				ArrayPointer<byte> num = salt + Strlen(sha256RoundsPrefix);
				ulong srounds = Strtoul(num, out ArrayPointer<byte> endp);
				if (endp.Value == (byte)'$')
				{
					salt = endp + 1;
					rounds = (int)Math.Max(RoundsMin, Math.Min(srounds, RoundsMax));
					roundsCustom = true;
				}
			}

			int saltLen = Math.Min(Strcspn(salt, dollarSign), SaltLenMax);
			int keyLen = Strlen(key);

			byte[] temp = new byte[key.SourceArray.Length];
			key.SourceArray.CopyTo(temp, 0);
			key = new ArrayPointer<byte>(temp) { Address = key.Address };

			temp = new byte[salt.SourceArray.Length];
			salt.SourceArray.CopyTo(temp, 0);
			salt = new ArrayPointer<byte>(temp) { Address = salt.Address };

			// Prepare for the real work.
			using (var ctx = new MemoryStream())
			{
				// Add the key string.
				ProcessBytes(key, keyLen, ctx);

				// The last part is the salt string. This must be at most 16 characters and it ends
				// at the first '$' character.
				ProcessBytes(salt, saltLen, ctx);

				// Compute alternate SHA256 sum with input KEY, SALT, and KEY. The final result will
				// be added to the first context.
				using var altCtx = new MemoryStream();

				// Add key.
				ProcessBytes(key, keyLen, altCtx);

				// Add salt.
				ProcessBytes(salt, saltLen, altCtx);

				// Add key again.
				ProcessBytes(key, keyLen, altCtx);

				// Now get result of this (32 bytes) and add it to the other context.
				FinishCtx(sha256, altCtx, altResult);

				// Add for any character in the key one byte of the alternate sum.
				for (cnt = keyLen; cnt > 32; cnt -= 32)
				{
					ProcessBytes(altResult, 32, ctx);
				}
				ProcessBytes(altResult, cnt, ctx);

				// Take the binary representation of the length of the key and for every 1 add the
				// alternate sum, for every 0 the key.
				for (cnt = keyLen; cnt > 0; cnt >>= 1)
				{
					if ((cnt & 1) != 0)
						ProcessBytes(altResult, 32, ctx);
					else
						ProcessBytes(key, keyLen, ctx);
				}

				// Create intermediate result.
				FinishCtx(sha256, ctx, altResult);
			}

			// Start computation of P byte sequence.
			using (var altCtx = new MemoryStream())
			{
				// For every character in the password add the entire password.
				for (cnt = 0; cnt < keyLen; cnt++)
				{
					ProcessBytes(key, keyLen, altCtx);
				}

				// Finish the digest.
				FinishCtx(sha256, altCtx, tempResult);
			}

			// Create byte sequence P.
			ArrayPointer<byte> pBytes;
			var cp = pBytes = new ArrayPointer<byte>(new byte[keyLen]);
			for (cnt = keyLen; cnt >= 32; cnt -= 32)
			{
				cp = Mempcpy(cp, tempResult, 32);
			}
			Memcpy(cp, tempResult, cnt);

			// Start computation of S byte sequence.
			using (var altCtx = new MemoryStream())
			{
				// For every character in the password add the entire password.
				for (cnt = 0; cnt < 16 + altResult[0]; cnt++)
				{
					ProcessBytes(salt, saltLen, altCtx);
				}

				// Finish the digest.
				FinishCtx(sha256, altCtx, tempResult);
			}

			// Create byte sequence S.
			ArrayPointer<byte> sBytes;
			cp = sBytes = new ArrayPointer<byte>(new byte[saltLen]);
			for (cnt = saltLen; cnt >= 32; cnt -= 32)
			{
				cp = Mempcpy(cp, tempResult, 32);
			}
			Memcpy(cp, tempResult, cnt);

			// Repeatedly run the collected hash value through SHA256 to burn CPU cycles.
			for (cnt = 0; cnt < rounds; cnt++)
			{
				// New context.
				using var ctx = new MemoryStream();

				// Add key or last result.
				if ((cnt & 1) != 0)
					ProcessBytes(pBytes, keyLen, ctx);
				else
					ProcessBytes(altResult, 32, ctx);

				// Add salt for numbers not divisible by 3.
				if (cnt % 3 != 0)
					ProcessBytes(sBytes, saltLen, ctx);

				// Add key for numbers not divisible by 7.
				if (cnt % 7 != 0)
					ProcessBytes(pBytes, keyLen, ctx);

				// Add key or last result.
				if ((cnt & 1) != 0)
					ProcessBytes(altResult, 32, ctx);
				else
					ProcessBytes(pBytes, keyLen, ctx);

				// Create intermediate result.
				FinishCtx(sha256, ctx, altResult);
			}

			// Now we can construct the result string. It consists of three parts.
			cp = Stpncpy(buffer, sha256SaltPrefix, Math.Max(0, buflen));
			buflen -= Strlen(sha256SaltPrefix);

			if (roundsCustom)
			{
				cp = Stpncpy(cp, sha256RoundsPrefix, Math.Max(0, buflen));
				buflen -= Strlen(sha256RoundsPrefix);

				char[] temp1 = (rounds.ToString() + "$\0").ToCharArray();
				byte[] temp2 = new byte[temp1.Length];
				for (int i = 0; i < temp1.Length; i++) temp2[i] = (byte)temp1[i];
				var temp3 = new ArrayPointer<byte>(temp2);

				cp = Stpncpy(cp, temp3, Math.Max(0, buflen));
				buflen -= Strlen(temp3);
			}

			cp = Stpncpy(cp, salt, Math.Min(Math.Max(0, buflen), saltLen));
			buflen -= Math.Min(Math.Max(0, buflen), saltLen);

			if (buflen > 0)
			{
				cp.Value = (byte)'$';
				cp++;
				buflen--;
			}

			cp = B64From24bit(altResult[0], altResult[10], altResult[20], 4, cp, ref buflen);
			cp = B64From24bit(altResult[21], altResult[1], altResult[11], 4, cp, ref buflen);
			cp = B64From24bit(altResult[12], altResult[22], altResult[2], 4, cp, ref buflen);
			cp = B64From24bit(altResult[3], altResult[13], altResult[23], 4, cp, ref buflen);
			cp = B64From24bit(altResult[24], altResult[4], altResult[14], 4, cp, ref buflen);
			cp = B64From24bit(altResult[15], altResult[25], altResult[5], 4, cp, ref buflen);
			cp = B64From24bit(altResult[6], altResult[16], altResult[26], 4, cp, ref buflen);
			cp = B64From24bit(altResult[27], altResult[7], altResult[17], 4, cp, ref buflen);
			cp = B64From24bit(altResult[18], altResult[28], altResult[8], 4, cp, ref buflen);
			cp = B64From24bit(altResult[9], altResult[19], altResult[29], 4, cp, ref buflen);
			cp = B64From24bit(0, altResult[31], altResult[30], 3, cp, ref buflen);

			if (buflen <= 0)
			{
				throw new IndexOutOfRangeException();
			}
			else
				cp.Value = 0;   // Terminate the string.

			return buffer;
		}

		private static ArrayPointer<byte> SHA512CryptR(ArrayPointer<byte> key, ArrayPointer<byte> salt, ArrayPointer<byte> buffer, int buflen)
		{
			var altResult = new ArrayPointer<byte>(new byte[64]);
			var tempResult = new ArrayPointer<byte>(new byte[64]);
			int cnt;
			// Default number of rounds.
			int rounds = RoundsDefault;
			bool roundsCustom = false;
			using var sha512 = SHA512.Create();

			// Find beginning of salt string. The prefix should normally always be present.
			// Just in case it is not.
			if (Strncmp(sha512SaltPrefix, salt, Strlen(sha512SaltPrefix)) == 0)
			{
				// Skip salt prefix.
				salt += Strlen(sha512SaltPrefix);
			}

			if (Strncmp(salt, sha512RoundsPrefix, Strlen(sha512RoundsPrefix)) == 0)
			{
				ArrayPointer<byte> num = salt + Strlen(sha512RoundsPrefix);
				ulong srounds = Strtoul(num, out ArrayPointer<byte> endp);
				if (endp.Value == (byte)'$')
				{
					salt = endp + 1;
					rounds = (int)Math.Max(RoundsMin, Math.Min(srounds, RoundsMax));
					roundsCustom = true;
				}
			}

			int saltLen = Math.Min(Strcspn(salt, dollarSign), SaltLenMax);
			int keyLen = Strlen(key);

			byte[] temp = new byte[key.SourceArray.Length];
			key.SourceArray.CopyTo(temp, 0);
			key = new ArrayPointer<byte>(temp) { Address = key.Address };

			temp = new byte[salt.SourceArray.Length];
			salt.SourceArray.CopyTo(temp, 0);
			salt = new ArrayPointer<byte>(temp) { Address = salt.Address };

			// Prepare for the real work.
			using (var ctx = new MemoryStream())
			{
				// Add the key string.
				ProcessBytes(key, keyLen, ctx);

				// The last part is the salt string. This must be at most 16 characters and it ends
				// at the first '$' character.
				ProcessBytes(salt, saltLen, ctx);

				// Compute alternate SHA256 sum with input KEY, SALT, and KEY. The final result will
				// be added to the first context.
				using var altCtx = new MemoryStream();

				// Add key.
				ProcessBytes(key, keyLen, altCtx);

				// Add salt.
				ProcessBytes(salt, saltLen, altCtx);

				// Add key again.
				ProcessBytes(key, keyLen, altCtx);

				// Now get result of this (32 bytes) and add it to the other context.
				FinishCtx(sha512, altCtx, altResult);

				// Add for any character in the key one byte of the alternate sum.
				for (cnt = keyLen; cnt > 64; cnt -= 64)
				{
					ProcessBytes(altResult, 64, ctx);
				}
				ProcessBytes(altResult, cnt, ctx);

				// Take the binary representation of the length of the key and for every 1 add the
				// alternate sum, for every 0 the key.
				for (cnt = keyLen; cnt > 0; cnt >>= 1)
				{
					if ((cnt & 1) != 0)
						ProcessBytes(altResult, 64, ctx);
					else
						ProcessBytes(key, keyLen, ctx);
				}

				// Create intermediate result.
				FinishCtx(sha512, ctx, altResult);
			}

			// Start computation of P byte sequence.
			using (var altCtx = new MemoryStream())
			{
				// For every character in the password add the entire password.
				for (cnt = 0; cnt < keyLen; cnt++)
				{
					ProcessBytes(key, keyLen, altCtx);
				}

				// Finish the digest.
				FinishCtx(sha512, altCtx, tempResult);
			}

			// Create byte sequence P.
			ArrayPointer<byte> pBytes;
			var cp = pBytes = new ArrayPointer<byte>(new byte[keyLen]);
			for (cnt = keyLen; cnt >= 64; cnt -= 64)
			{
				cp = Mempcpy(cp, tempResult, 64);
			}
			Memcpy(cp, tempResult, cnt);

			// Start computation of S byte sequence.
			using (var altCtx = new MemoryStream())
			{
				// For every character in the password add the entire password.
				for (cnt = 0; cnt < 16 + altResult[0]; cnt++)
				{
					ProcessBytes(salt, saltLen, altCtx);
				}

				// Finish the digest.
				FinishCtx(sha512, altCtx, tempResult);
			}

			// Create byte sequence S.
			ArrayPointer<byte> sBytes;
			cp = sBytes = new ArrayPointer<byte>(new byte[saltLen]);
			for (cnt = saltLen; cnt >= 64; cnt -= 64)
			{
				cp = Mempcpy(cp, tempResult, 64);
			}
			Memcpy(cp, tempResult, cnt);

			// Repeatedly run the collected hash value through SHA512 to burn CPU cycles.
			for (cnt = 0; cnt < rounds; cnt++)
			{
				// New context.
				using var ctx = new MemoryStream();

				// Add key or last result.
				if ((cnt & 1) != 0)
					ProcessBytes(pBytes, keyLen, ctx);
				else
					ProcessBytes(altResult, 64, ctx);

				// Add salt for numbers not divisible by 3.
				if (cnt % 3 != 0)
					ProcessBytes(sBytes, saltLen, ctx);

				// Add key for numbers not divisible by 7.
				if (cnt % 7 != 0)
					ProcessBytes(pBytes, keyLen, ctx);

				// Add key or last result.
				if ((cnt & 1) != 0)
					ProcessBytes(altResult, 64, ctx);
				else
					ProcessBytes(pBytes, keyLen, ctx);

				// Create intermediate result.
				FinishCtx(sha512, ctx, altResult);
			}

			// Now we can construct the result string. It consists of three parts.
			cp = Stpncpy(buffer, sha512SaltPrefix, Math.Max(0, buflen));
			buflen -= Strlen(sha512SaltPrefix);

			if (roundsCustom)
			{
				cp = Stpncpy(cp, sha512RoundsPrefix, Math.Max(0, buflen));
				buflen -= Strlen(sha512RoundsPrefix);

				char[] temp1 = (rounds.ToString() + "$\0").ToCharArray();
				byte[] temp2 = new byte[temp1.Length];
				for (int i = 0; i < temp1.Length; i++) temp2[i] = (byte)temp1[i];
				var temp3 = new ArrayPointer<byte>(temp2);

				cp = Stpncpy(cp, temp3, Math.Max(0, buflen));
				buflen -= Strlen(temp3);
			}

			cp = Stpncpy(cp, salt, Math.Min(Math.Max(0, buflen), saltLen));
			buflen -= Math.Min(Math.Max(0, buflen), saltLen);

			if (buflen > 0)
			{
				cp.Value = (byte)'$';
				cp++;
				buflen--;
			}

			cp = B64From24bit(altResult[0], altResult[21], altResult[42], 4, cp, ref buflen);
			cp = B64From24bit(altResult[22], altResult[43], altResult[1], 4, cp, ref buflen);
			cp = B64From24bit(altResult[44], altResult[2], altResult[23], 4, cp, ref buflen);
			cp = B64From24bit(altResult[3], altResult[24], altResult[45], 4, cp, ref buflen);
			cp = B64From24bit(altResult[25], altResult[46], altResult[4], 4, cp, ref buflen);
			cp = B64From24bit(altResult[47], altResult[5], altResult[26], 4, cp, ref buflen);
			cp = B64From24bit(altResult[6], altResult[27], altResult[48], 4, cp, ref buflen);
			cp = B64From24bit(altResult[28], altResult[49], altResult[7], 4, cp, ref buflen);
			cp = B64From24bit(altResult[50], altResult[8], altResult[29], 4, cp, ref buflen);
			cp = B64From24bit(altResult[9], altResult[30], altResult[51], 4, cp, ref buflen);
			cp = B64From24bit(altResult[31], altResult[52], altResult[10], 4, cp, ref buflen);
			cp = B64From24bit(altResult[53], altResult[11], altResult[32], 4, cp, ref buflen);
			cp = B64From24bit(altResult[12], altResult[33], altResult[54], 4, cp, ref buflen);
			cp = B64From24bit(altResult[34], altResult[55], altResult[13], 4, cp, ref buflen);
			cp = B64From24bit(altResult[56], altResult[14], altResult[35], 4, cp, ref buflen);
			cp = B64From24bit(altResult[15], altResult[36], altResult[57], 4, cp, ref buflen);
			cp = B64From24bit(altResult[37], altResult[58], altResult[16], 4, cp, ref buflen);
			cp = B64From24bit(altResult[59], altResult[17], altResult[38], 4, cp, ref buflen);
			cp = B64From24bit(altResult[18], altResult[39], altResult[60], 4, cp, ref buflen);
			cp = B64From24bit(altResult[40], altResult[61], altResult[19], 4, cp, ref buflen);
			cp = B64From24bit(altResult[62], altResult[20], altResult[41], 4, cp, ref buflen);
			cp = B64From24bit(0, 0, altResult[63], 2, cp, ref buflen);

			if (buflen <= 0)
				throw new IndexOutOfRangeException();
			else
				cp.Value = 0;   // Terminate the string.

			return buffer;
		}

		#endregion Crypt implementation

		#region Processing helper methods

		private static ArrayPointer<byte> B64From24bit(uint b2, uint b1, uint b0, int n, ArrayPointer<byte> cp, ref int buflen)
		{
			uint w = (b2 << 16) | (b1 << 8) | b0;
			while (n-- > 0 && buflen > 0)
			{
				cp.Value = (byte)B64Chars[(int)(w & 0x3f)];
				cp++;
				buflen--;
				w >>= 6;
			}
			return cp;
		}

		private static void ProcessBytes(ArrayPointer<byte> buffer, int count, MemoryStream ctx)
		{
			ctx.Write(buffer.SourceArray, buffer.Address, count);
		}

		private static void FinishCtx(HashAlgorithm hashAlg, MemoryStream ctx, ArrayPointer<byte> buffer)
		{
			byte[] temp = hashAlg.ComputeHash(ctx.ToArray());
			for (int i = 0; i < temp.Length; i++, buffer++)
			{
				buffer.Value = temp[i];
			}
		}

		#endregion Processing helper methods

		#region String and memory helper methods

		private static int Strlen(ArrayPointer<byte> str)
		{
			for (int i = 0; ; i++, str++)
			{
				if (str.Value == 0)
					return i;
			}
		}

		private static int Strncmp(ArrayPointer<byte> str1, ArrayPointer<byte> str2, int length)
		{
			for (int i = 0; i < length; i++, str1++, str2++)
			{
				if (str1.Value > str2.Value)
					return 1;
				else if (str2.Value > str1.Value)
					return -1;
			}
			return 0;
		}

		private static int Strcspn(ArrayPointer<byte> str1, ArrayPointer<byte> str2)
		{
			int location = 0;
			ArrayPointer<byte> i;
			ArrayPointer<byte> j;
			int str1Len = Strlen(str1);

			for (i = str1; i.Value != 0; i++, location++)
			{
				for (j = str2; j.Value != 0; j++)
				{
					if (i.Value == j.Value)
						return location;
				}
			}
			return str1Len;
		}

		private static ulong Strtoul(ArrayPointer<byte> str, out ArrayPointer<byte> endptr)
		{
			string num = "";
			while (str.Value >= '0' && str.Value <= '9')
			{
				num += (char)str.Value;
				str++;
			}
			endptr = str;
			if (ulong.TryParse(num, out ulong value))
				return value;
			else
				throw new ArgumentException(nameof(str));
		}

		private static ArrayPointer<byte> Stpncpy(ArrayPointer<byte> buffer, ArrayPointer<byte> source, int max)
		{
			for (int i = 0; i < max && source[i] != 0; i++, buffer++)
			{
				buffer.Value = source[i];
			}
			return buffer;
		}

		private static string ExtractString(ArrayPointer<byte> str)
		{
			var sb = new StringBuilder(Strlen(str));
			for (int i = 0; str[i] != 0; i++)
			{
				sb.Append((char)str[i]);
			}
			return sb.ToString();
		}

		private static void Memcpy(ArrayPointer<byte> dest, ArrayPointer<byte> src, int n)
		{
			for (int i = 0; i < n; i++)
			{
				dest[i] = src[i];
			}
		}

		private static ArrayPointer<byte> Mempcpy(ArrayPointer<byte> dest, ArrayPointer<byte> src, int n)
		{
			for (int i = 0; i < n; i++)
			{
				dest[i] = src[i];
			}
			return dest + n;
		}

		#endregion String and memory helper methods

		#region Constant-time comparison

		private static bool ConstantTimeEquals(string a, string b)
		{
			if ((a == null) != (b == null))
				return false;
			if (a.Length != b.Length)
				return false;

			int differentBits = 0;
			for (int i = 0; i < a.Length; i++)
			{
				differentBits |= a[i] ^ b[i];
			}
			return differentBits == 0;
		}

		#endregion Constant-time comparison

		#region Private classes

		private struct ArrayPointer<T>
		{
			#region Constructors

			public ArrayPointer(T[] array)
			{
				SourceArray = array;
				LongAddress = 0;
			}

			#endregion Constructors

			#region Properties

			public T[] SourceArray { get; }

			public long LongAddress { get; set; }

			public int Address
			{
				get => (int)LongAddress;
				set => LongAddress = value;
			}

			public T Value
			{
                get => SourceArray[LongAddress];
				set => SourceArray[LongAddress] = value;
			}

			public int Length => SourceArray.Length;

			public long LongLength => SourceArray.LongLength;

			public T this[int index]
			{
				get => SourceArray[LongAddress + index];
				set => SourceArray[LongAddress + index] = value;
			}

			#endregion Properties

			#region Equals implementation

			public override bool Equals(object obj) => obj is ArrayPointer<T> pointer && this == pointer;

			public override int GetHashCode() => (int)LongAddress;

			#endregion Equals implementation

			#region Operators

			public static ArrayPointer<T> operator +(ArrayPointer<T> ap, int offset)
			{
				var temp = new ArrayPointer<T>(ap.SourceArray)
				{
					LongAddress = ap.LongAddress + offset
				};
				return temp;
			}

			public static ArrayPointer<T> operator +(ArrayPointer<T> ap, long offset)
			{
				var temp = new ArrayPointer<T>(ap.SourceArray)
				{
					LongAddress = ap.LongAddress + offset
				};
				return temp;
			}

			public static ArrayPointer<T> operator +(int offset, ArrayPointer<T> ap)
			{
				var temp = new ArrayPointer<T>(ap.SourceArray)
				{
					LongAddress = ap.LongAddress + offset
				};
				return temp;
			}

			public static ArrayPointer<T> operator +(long offset, ArrayPointer<T> ap)
			{
				var temp = new ArrayPointer<T>(ap.SourceArray)
				{
					LongAddress = ap.LongAddress + offset
				};
				return temp;
			}

			public static ArrayPointer<T> operator ++(ArrayPointer<T> ap)
			{
				var temp = new ArrayPointer<T>(ap.SourceArray)
				{
					LongAddress = ap.LongAddress + 1
				};
				return temp;
			}

			public static ArrayPointer<T> operator --(ArrayPointer<T> ap)
			{
				var temp = new ArrayPointer<T>(ap.SourceArray)
				{
					LongAddress = ap.LongAddress - 1
				};
				return temp;
			}

			public static bool operator ==(ArrayPointer<T> ap1, ArrayPointer<T> ap2)
			{
				return ap1.SourceArray == ap2.SourceArray && ap1.LongAddress == ap2.LongAddress;
			}

			public static bool operator !=(ArrayPointer<T> ap1, ArrayPointer<T> ap2)
			{
				return ap1.SourceArray != ap2.SourceArray || ap1.LongAddress != ap2.LongAddress;
			}

			#endregion Operators
		}

		private class SplittedHash
		{
			public string Protocol { get; private set; }
			public string Rounds { get; private set; }
			public string Hash { get; private set; }

			/// <summary>
			/// Actual salt param of the hash, does NOT include the protocol and rounds.
			/// </summary>
			public string Salt { get; private set; }

			public static SplittedHash Parse(string str)
			{
				var sh = new SplittedHash();

				string[] ret = str.Split(new[] { '$' }, 4, StringSplitOptions.RemoveEmptyEntries);
				if (ret.Length < 3)
					throw new FormatException("Invalid MCF string");

				sh.Protocol = ret[0];
				if (!ret[1].StartsWith("rounds="))
				{
					sh.Salt = ret[1];
					sh.Hash = ret[2];
				}
				else
				{
					if (ret.Length < 4)
						throw new FormatException("Invalid MCF string");
					sh.Rounds = ret[1];
					sh.Salt = ret[2];
					sh.Hash = ret[3];
				}
				return sh;
			}

			public string GetFullSalt()
			{
				if (string.IsNullOrEmpty(Rounds))
				{
					return $"${Protocol}${Salt}";
				}
				return $"${Protocol}${Rounds}${Salt}";
			}
		}

		#endregion Private classes
	}
}
