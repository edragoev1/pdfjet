/*
 * Decryptor.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Decrypts the strings and streams of a PDF that is encrypted with the
/// standard security handler and an empty user password, like PDFs that open
/// without a password but restrict printing or copying. RC4 and AES-128
/// (revisions 2 to 4) and AES-256 (revisions 5 and 6) are supported.
/// </summary>
internal sealed class Decryptor {
    private static readonly byte[] PADDING = {
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41,
        0x64, 0x00, 0x4E, 0x56, 0xFF, 0xFA, 0x01, 0x08,
        0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80,
        0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A
    };

    // The methods of the crypt filters.
    private const int NONE = 0;
    private const int RC4 = 1;
    private const int AES_128 = 2;
    private const int AES_256 = 3;

    // The object number of the encryption dictionary.
    internal readonly int objNumber;
    private readonly byte[] key;
    private readonly int streamMethod;
    private readonly int stringMethod;
    private readonly bool encryptMetadata;

    // Returns the decryptor of the PDF, or null when it is not encrypted. The
    // trailer is the cross-reference stream object when there is no trailer.
    internal static Decryptor GetDecryptor(PDFobj trailer, List<PDFobj> objects) {
        int i = (trailer == null) ? -1 : trailer.dict.IndexOf("/Encrypt");
        if (i == -1 || i + 1 >= trailer.dict.Count) {
            return null;
        }
        PDFobj encrypt = null;
        if (trailer.dict[i + 1].Equals("<<")) {
            // The dictionary is in the trailer, so there is no object to skip.
            encrypt = new PDFobj();
            encrypt.number = -1;
            int level = 0;
            for (int j = i + 1; j < trailer.dict.Count; j++) {
                String token = trailer.dict[j];
                encrypt.dict.Add(token);
                if (token.Equals("<<")) {
                    level++;
                } else if (token.Equals(">>") && --level == 0) {
                    break;
                }
            }
        } else {
            foreach (PDFobj obj in objects) {
                if (obj.number.ToString().Equals(trailer.dict[i + 1])) {
                    encrypt = obj;      // The last one is the newest.
                }
            }
        }
        if (encrypt == null) {
            throw new Exception("The encryption dictionary of the PDF was not found.");
        }
        byte[] id = new byte[0];
        i = trailer.dict.IndexOf("/ID");
        if (i != -1 && i + 2 < trailer.dict.Count && trailer.dict[i + 1].Equals("[")) {
            id = ToBytes(trailer.dict[i + 2]);
        }
        return new Decryptor(encrypt, id);
    }

    private Decryptor(PDFobj encrypt, byte[] id) {
        this.objNumber = encrypt.number;
        if (!encrypt.GetValue("/Filter").Equals("/Standard")) {
            throw new Exception("The security handler of the PDF is not supported: " +
                    encrypt.GetValue("/Filter"));
        }
        int v = GetInt(encrypt, "/V");
        int r = GetInt(encrypt, "/R");
        this.encryptMetadata = !encrypt.GetValue("/EncryptMetadata").Equals("false");
        if (v == 1 || v == 2) {
            this.streamMethod = RC4;
            this.stringMethod = RC4;
        } else if (v == 4 || v == 5) {
            this.streamMethod = GetMethod(encrypt, encrypt.GetValue("/StmF"));
            this.stringMethod = GetMethod(encrypt, encrypt.GetValue("/StrF"));
        } else {
            throw new Exception("The encryption of the PDF is not supported: /V " + v);
        }
        byte[] u = ToBytes(encrypt.GetValue("/U"));
        if (r == 5 || r == 6) {
            this.key = GetKey(r, u, ToBytes(encrypt.GetValue("/UE")));
        } else if (r >= 2 && r <= 4) {
            int length = (v == 1 || r == 2) ? 5 : (v == 4) ? 16 : GetInt(encrypt, "/Length") / 8;
            this.key = GetKey(r, Math.Max(5, Math.Min(length, 16)),
                    ToBytes(encrypt.GetValue("/O")), u, GetInt(encrypt, "/P"), id, encryptMetadata);
        } else {
            throw new Exception("The encryption of the PDF is not supported: /R " + r);
        }
    }

    // Returns the method of the crypt filter with the name in the /CF dictionary.
    private static int GetMethod(PDFobj encrypt, String name) {
        List<String> dict = encrypt.dict;
        int level = 0;
        for (int i = dict.IndexOf("/CF") + 1; i > 0 && i < dict.Count; i++) {
            String token = dict[i];
            if (token.Equals("<<")) {
                level++;
            } else if (token.Equals(">>")) {
                if (--level == 0) {
                    break;
                }
            } else if (level == 1 && token.Equals(name)) {
                for (int j = i + 1; j + 1 < dict.Count && !dict[j].Equals(">>"); j++) {
                    if (dict[j].Equals("/CFM")) {
                        String method = dict[j + 1];
                        if (method.Equals("/V2")) {
                            return RC4;
                        } else if (method.Equals("/AESV2")) {
                            return AES_128;
                        } else if (method.Equals("/AESV3")) {
                            return AES_256;
                        }
                        break;
                    }
                }
                break;
            }
        }
        return NONE;                    // Like /Identity, the default.
    }

    // Algorithm 2 of ISO 32000-2 computes the key of revisions 2 to 4 from
    // the password, which is checked against /U with algorithms 4 and 5.
    private static byte[] GetKey(
            int r, int length, byte[] o, byte[] u, int p, byte[] id, bool encryptMetadata) {
        byte[] hash = MD5.HashData(Concat(PADDING, o,
                new byte[] {(byte) p, (byte) (p >> 8), (byte) (p >> 16), (byte) (p >> 24)}, id,
                (r >= 4 && !encryptMetadata) ? new byte[] {0xff, 0xff, 0xff, 0xff} : new byte[0]));
        if (r >= 3) {
            for (int i = 0; i < 50; i++) {
                hash = MD5.HashData(hash.AsSpan(0, length));
            }
        }
        byte[] key = hash.AsSpan(0, length).ToArray();
        byte[] check;
        int n;
        if (r == 2) {
            check = RC4Crypt(key, PADDING);
            n = 32;
        } else {
            check = RC4Crypt(key, MD5.HashData(Concat(PADDING, id)));
            for (int i = 1; i <= 19; i++) {
                byte[] k = new byte[length];
                for (int j = 0; j < length; j++) {
                    k[j] = (byte) (key[j] ^ i);
                }
                check = RC4Crypt(k, check);
            }
            n = 16;
        }
        if (u.Length < n || !check.AsSpan(0, n).SequenceEqual(u.AsSpan(0, n))) {
            throw new Exception("The PDF can only be opened with a password, which is not supported.");
        }
        return key;
    }

    // Algorithms 2.A and 11 of ISO 32000-2 get the key of revisions 5 and 6
    // from /UE, after checking the password against /U.
    private static byte[] GetKey(int r, byte[] u, byte[] ue) {
        if (u.Length < 48 || ue.Length < 32) {
            throw new Exception("The encryption dictionary of the PDF is not valid.");
        }
        byte[] hash = GetHash(r, u.AsSpan(32, 8).ToArray());
        if (!hash.AsSpan().SequenceEqual(u.AsSpan(0, 32))) {
            throw new Exception("The PDF can only be opened with a password, which is not supported.");
        }
        hash = GetHash(r, u.AsSpan(40, 8).ToArray());
        return AESDecrypt(hash, new byte[16], ue.AsSpan(0, 32).ToArray());
    }

    // Returns the hash of the empty password and the salt, which is SHA-256
    // in revision 5, and algorithm 2.B of ISO 32000-2 in revision 6.
    private static byte[] GetHash(int r, byte[] salt) {
        byte[] k = SHA256.HashData(salt);
        if (r == 5) {
            return k;
        }
        for (int round = 1; ; round++) {
            byte[] k1 = new byte[64 * k.Length];
            for (int i = 0; i < 64; i++) {
                Array.Copy(k, 0, k1, i * k.Length, k.Length);
            }
            byte[] e = AES128.EncryptK1(k1, k.AsSpan(0, 16).ToArray(), k.AsSpan(16, 16).ToArray());
            int sum = 0;
            for (int i = 0; i < 16; i++) {
                sum += e[i];
            }
            if (sum % 3 == 0) {
                k = SHA256.HashData(e);
            } else if (sum % 3 == 1) {
                k = SHA384.HashData(e);
            } else {
                k = SHA512.HashData(e);
            }
            if (round >= 64 && e[e.Length - 1] <= round - 32) {
                break;
            }
        }
        return k.AsSpan(0, 32).ToArray();
    }

    // Decrypts the strings in the dictionary of the object, which become
    // hexadecimal strings.
    internal void DecryptStrings(PDFobj obj) {
        if (stringMethod == NONE) {
            return;
        }
        for (int i = 0; i < obj.dict.Count; i++) {
            String token = obj.dict[i];
            if (token.StartsWith("(") || (token.StartsWith("<") && !token.Equals("<<"))) {
                byte[] bytes = Decrypt(ToBytes(token), stringMethod, obj);
                obj.dict[i] = "<" + Convert.ToHexString(bytes).ToLowerInvariant() + ">";
            }
        }
    }

    // Returns the decrypted stream of the object.
    internal byte[] DecryptStream(PDFobj obj, byte[] stream) {
        if (!encryptMetadata && obj.GetValue("/Type").Equals("/Metadata")) {
            return stream;
        }
        return Decrypt(stream, streamMethod, obj);
    }

    // Algorithm 1 of ISO 32000-2 decrypts with a key for each object in
    // revisions 2 to 4, and revisions 5 and 6 use the file key.
    private byte[] Decrypt(byte[] data, int method, PDFobj obj) {
        if (method == NONE) {
            return data;
        }
        byte[] objectKey = key;
        if (method != AES_256) {
            int number = obj.number;
            int generation = Int32.TryParse(obj.dict[1], out int value) ? value : 0;
            byte[] salt = (method == AES_128) ? new byte[] {(byte) 's', (byte) 'A', (byte) 'l', (byte) 'T'} : new byte[0];
            byte[] hash = MD5.HashData(Concat(key, new byte[] {
                    (byte) number, (byte) (number >> 8), (byte) (number >> 16),
                    (byte) generation, (byte) (generation >> 8)}, salt));
            objectKey = hash.AsSpan(0, Math.Min(key.Length + 5, 16)).ToArray();
        }
        if (method == RC4) {
            return RC4Crypt(objectKey, data);
        }
        // The data starts with the initialization vector, and is padded to whole blocks.
        if (data.Length < 32) {
            return new byte[0];
        }
        byte[] decrypted = AESDecrypt(objectKey, data.AsSpan(0, 16).ToArray(),
                data.AsSpan(16, (data.Length - 16) / 16 * 16).ToArray());
        int padding = decrypted[decrypted.Length - 1];
        if (padding >= 1 && padding <= 16) {
            return decrypted.AsSpan(0, decrypted.Length - padding).ToArray();
        }
        return decrypted;
    }

    private static byte[] AESDecrypt(byte[] key, byte[] iv, byte[] data) {
        using var aes = Aes.Create();
        aes.Key = key;
        return aes.DecryptCbc(data, iv, PaddingMode.None);
    }

    private static byte[] RC4Crypt(byte[] key, byte[] data) {
        int[] s = new int[256];
        for (int i = 0; i < 256; i++) {
            s[i] = i;
        }
        for (int i = 0, j = 0; i < 256; i++) {
            j = (j + s[i] + key[i % key.Length]) & 0xff;
            (s[i], s[j]) = (s[j], s[i]);
        }
        byte[] result = new byte[data.Length];
        for (int k = 0, i = 0, j = 0; k < data.Length; k++) {
            i = (i + 1) & 0xff;
            j = (j + s[i]) & 0xff;
            (s[i], s[j]) = (s[j], s[i]);
            result[k] = (byte) (data[k] ^ s[(s[i] + s[j]) & 0xff]);
        }
        return result;
    }

    private static byte[] Concat(params byte[][] arrays) {
        using var bos = new MemoryStream();
        foreach (byte[] array in arrays) {
            bos.Write(array, 0, array.Length);
        }
        return bos.ToArray();
    }

    // Returns the integer value of the key, or 0. /P can be written as an
    // unsigned number.
    private static int GetInt(PDFobj obj, String key) {
        return Int64.TryParse(obj.GetValue(key), out long value) ? (int) value : 0;
    }

    // Returns the bytes of a literal string like (a\)b) or of a hexadecimal
    // string like &lt;612962&gt;.
    internal static byte[] ToBytes(String token) {
        using var bos = new MemoryStream(token.Length);
        if (token.StartsWith("<")) {
            int high = -1;
            for (int i = 1; i < token.Length; i++) {
                int digit = Decompressor.HexValue(token[i]);
                if (digit == -1) {
                    continue;
                }
                if (high == -1) {
                    high = digit;
                } else {
                    bos.WriteByte((byte) ((high << 4) | digit));
                    high = -1;
                }
            }
            if (high != -1) {
                bos.WriteByte((byte) (high << 4));
            }
        } else if (token.StartsWith("(")) {
            int end = token.EndsWith(")") ? token.Length - 1 : token.Length;
            for (int i = 1; i < end; i++) {
                char c = token[i];
                if (c == '\\' && i + 1 < end) {
                    c = token[++i];
                    int k = "nrtbf".IndexOf(c);
                    if (k != -1) {
                        bos.WriteByte((byte) "\n\r\t\b\f"[k]);
                    } else if (c >= '0' && c <= '7') {
                        int value = c - '0';
                        for (int n = 1; n < 3 && i + 1 < end &&
                                token[i + 1] >= '0' && token[i + 1] <= '7'; n++) {
                            value = value * 8 + (token[++i] - '0');
                        }
                        bos.WriteByte((byte) value);
                    } else if (c == '\r') {
                        // A backslash at the end of a line continues the string.
                        if (i + 1 < end && token[i + 1] == '\n') {
                            i++;
                        }
                    } else if (c != '\n') {
                        bos.WriteByte((byte) c);    // Like \( \) and \\
                    }
                } else if (c == '\r') {
                    // An end of line in a string is a line feed.
                    if (i + 1 < end && token[i + 1] == '\n') {
                        i++;
                    }
                    bos.WriteByte((byte) '\n');
                } else {
                    bos.WriteByte((byte) c);
                }
            }
        }
        return bos.ToArray();
    }
}
}   // End of namespace PDFjet.NET
