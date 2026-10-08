/*
 * Polyfills.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// What .NET 8 has and .NET Framework 4.8 does not, as PDFjet's .NET source
// uses it, for the .NET Framework 4.8 build alone: its sources are compiled as
// they are, and these fill the gaps. The static members are C# 14 extension
// members, compiled into calls of these methods, which need nothing of the
// runtime; the classes that .NET Framework does not have are classes of these
// names, in their namespaces. The hashes each have a class of their own, as
// the static HashData of two of them in one class would be one method twice.
// A new gap, when the .NET source uses another API of .NET 8, is a compile
// error of this build, filled here, never in the source.

using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PDFjet.NET {

internal static class Md5Extensions {
    extension(MD5) {
        public static byte[] HashData(byte[] source) {
            using (var h = MD5.Create()) {
                return h.ComputeHash(source);
            }
        }

        public static byte[] HashData(System.ReadOnlySpan<byte> source) => MD5.HashData(source.ToArray());
    }
}

internal static class Sha256Extensions {
    extension(SHA256) {
        public static byte[] HashData(byte[] source) {
            using (var h = SHA256.Create()) {
                return h.ComputeHash(source);
            }
        }
    }
}

internal static class Sha384Extensions {
    extension(SHA384) {
        public static byte[] HashData(byte[] source) {
            using (var h = SHA384.Create()) {
                return h.ComputeHash(source);
            }
        }
    }
}

internal static class Sha512Extensions {
    extension(SHA512) {
        public static byte[] HashData(byte[] source) {
            using (var h = SHA512.Create()) {
                return h.ComputeHash(source);
            }
        }
    }
}

internal static class Net48Extensions {
    extension(System.Array) {
        public static void Fill<T>(T[] array, T value) {
            for (int i = 0; i < array.Length; i++) {
                array[i] = value;
            }
        }

        public static void Fill<T>(T[] array, T value, int startIndex, int count) {
            for (int i = startIndex; i < startIndex + count; i++) {
                array[i] = value;
            }
        }
    }

    extension(System.Convert) {
        public static string ToHexString(byte[] bytes) {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes) {
                sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }

    extension(Encoding) {
        public static Encoding Latin1 => Encoding.GetEncoding(28591);
    }

    extension(Encoding encoding) {
        public int GetBytes(string s, System.Span<byte> bytes) {
            byte[] encoded = encoding.GetBytes(s);
            new System.ReadOnlySpan<byte>(encoded).CopyTo(bytes);
            return encoded.Length;
        }
    }

    extension(RandomNumberGenerator) {
        public static byte[] GetBytes(int count) {
            var bytes = new byte[count];
            using (var rng = RandomNumberGenerator.Create()) {
                rng.GetBytes(bytes);
            }
            return bytes;
        }

        public static void Fill(System.Span<byte> data) {
            byte[] bytes = RandomNumberGenerator.GetBytes(data.Length);
            new System.ReadOnlySpan<byte>(bytes).CopyTo(data);
        }
    }

    extension(CharUnicodeInfo) {
        public static UnicodeCategory GetUnicodeCategory(int codePoint) {
            if (codePoint >= 0 && codePoint <= 0xFFFF) {
                return CharUnicodeInfo.GetUnicodeCategory((char) codePoint);
            }
            if (codePoint > 0x10FFFF) {
                return UnicodeCategory.OtherNotAssigned;
            }
            return CharUnicodeInfo.GetUnicodeCategory(char.ConvertFromUtf32(codePoint), 0);
        }
    }

    extension(Aes aes) {
        public byte[] DecryptCbc(byte[] ciphertext, byte[] iv, PaddingMode paddingMode) {
            aes.Mode = CipherMode.CBC;
            aes.Padding = paddingMode;
            using (var decryptor = aes.CreateDecryptor(aes.Key, iv)) {
                return decryptor.TransformFinalBlock(ciphertext, 0, ciphertext.Length);
            }
        }
    }

    extension(Stream stream) {
        public void Write(System.ReadOnlySpan<byte> bytes) {
            byte[] array = bytes.ToArray();
            stream.Write(array, 0, array.Length);
        }
    }

    extension(int value) {
        public bool TryFormat(System.Span<byte> destination, out int bytesWritten,
                System.ReadOnlySpan<char> format, System.IFormatProvider provider) {
            string s = value.ToString(format.ToString(), provider);
            if (s.Length > destination.Length) {
                bytesWritten = 0;
                return false;
            }
            for (int i = 0; i < s.Length; i++) {
                destination[i] = (byte) s[i];
            }
            bytesWritten = s.Length;
            return true;
        }
    }

    extension(string s) {
        public bool EndsWith(char c) => s.Length > 0 && s[s.Length - 1] == c;

        public bool Contains(char c) => s.IndexOf(c) >= 0;

        public string[] Split(string separator, System.StringSplitOptions options) =>
                s.Split(new[] {separator}, options);
    }

    extension(System.ReadOnlySpan<char> span) {
        public int IndexOfAnyInRange(char low, char high) {
            for (int i = 0; i < span.Length; i++) {
                if (span[i] >= low && span[i] <= high) {
                    return i;
                }
            }
            return -1;
        }

        public int IndexOfAnyExceptInRange(char low, char high) {
            for (int i = 0; i < span.Length; i++) {
                if (span[i] < low || span[i] > high) {
                    return i;
                }
            }
            return -1;
        }
    }
}

}

namespace System {

/// <summary>MathF of .NET Core: the functions of Math, in single precision.</summary>
internal static class MathF {
    public const float PI = 3.14159265f;

    public static float Sqrt(float x) => (float) Math.Sqrt(x);
    public static float Atan2(float y, float x) => (float) Math.Atan2(y, x);
    public static float Sin(float x) => (float) Math.Sin(x);
    public static float Cos(float x) => (float) Math.Cos(x);
    public static float Abs(float x) => Math.Abs(x);
}

}

namespace System.Numerics {

internal static class BitOperations {
    public static int TrailingZeroCount(int value) => TrailingZeroCount((uint) value);

    public static int TrailingZeroCount(uint value) {
        if (value == 0) {
            return 32;
        }
        int count = 0;
        while ((value & 1) == 0) {
            value >>= 1;
            count++;
        }
        return count;
    }
}

}

namespace System.Security.Cryptography {

internal static class CryptographicOperations {
    public static void ZeroMemory(Span<byte> buffer) => buffer.Clear();

    public static bool FixedTimeEquals(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right) {
        if (left.Length != right.Length) {
            return false;
        }
        int difference = 0;
        for (int i = 0; i < left.Length; i++) {
            difference |= left[i] ^ right[i];
        }
        return difference == 0;
    }
}

}
