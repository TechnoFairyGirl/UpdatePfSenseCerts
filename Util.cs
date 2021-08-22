using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace UpdatePfSenseCerts
{
	static class Entensions
	{
		public static TResult Let<T, TResult>(this T arg, Func<T, TResult> func) => func(arg);
		public static T Also<T>(this T arg, Action<T> func) { func(arg); return arg; }

		public static string ToTitleCase(this string str) =>
			CultureInfo.InvariantCulture.TextInfo.ToTitleCase(str);

		public static string ToHexString(this byte[] bytes) =>
			BitConverter.ToString(bytes).Replace("-", "").ToLowerInvariant();

		public static byte[] GetBytes(this string str) =>
			Encoding.UTF8.GetBytes(str);

		public static string GetString(this byte[] bytes) =>
			Encoding.UTF8.GetString(bytes);

		public static string GetBase64(this byte[] bytes) =>
			Convert.ToBase64String(bytes);

		public static string ReadAllText(this Stream stream)
		{
			using var reader = new StreamReader(stream, new UTF8Encoding(false), false, 4096, true);
			return reader.ReadToEnd();
		}
	}

	static class Util
	{
		public static byte[] Random(int length)
		{
			using var rng = new RNGCryptoServiceProvider();
			var bytes = new byte[length];
			rng.GetBytes(bytes);
			return bytes;
		}

		public static byte[] Sha256(byte[] input)
		{
			using var sha256 = SHA256.Create();
			return sha256.ComputeHash(input);
		}
	}
}
