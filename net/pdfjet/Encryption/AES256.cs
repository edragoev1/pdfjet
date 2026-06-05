/**
 * AES256.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Numerics;
using System.Collections.Generic;

namespace PDFjet.NET {
public class AES256 {
    /// <summary>
    /// Encrypts a 32‑byte File Encryption Key (FEK) with AES‑256‑CBC,
    /// using a zero IV and no padding.
    /// </summary>
    /// <param name="fileEncryptionKey">
    /// 32‑byte FEK to encrypt.
    /// </param>
    /// <param name="key">
    /// 32‑byte key (hash) used for AES‑256 encryption.
    /// </param>
    /// <returns>The encrypted 32‑byte File Encryption Key.</returns>
    internal static byte[] EncryptWithZeroIV(byte[] fileEncryptionKey, byte[] key) {
        if (fileEncryptionKey == null || fileEncryptionKey.Length != 32)
            throw new ArgumentException("File Encryption Key must be 32 bytes long.", nameof(fileEncryptionKey));
        if (key == null || key.Length != 32)
            throw new ArgumentException("The encryption key must be 32 bytes long.", nameof(key));

        // Set up AES‑256 with a zero IV and no padding
        using var aes = Aes.Create();
        aes.Key = key;                          // 32 bytes key (AES-256)
        aes.IV = new byte[aes.BlockSize / 8];   // 16 bytes all‑zero IV
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;

        // Perform the encryption in one block
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(fileEncryptionKey, 0, fileEncryptionKey.Length);
    }

    /// <summary>
    /// Encrypts the provided data using AES-256 in CBC mode with a randomly generated IV.
    /// The resulting byte array is structured as: [16-byte IV][encrypted data].
    /// </summary>
    /// <param name="data">The data to be encrypted.</param>
    /// <param name="key">The 32-byte encryption key for AES-256.</param>
    /// <returns>A byte array containing the IV prepended to the AES-256-CBC encrypted data.</returns>
    internal static byte[] Encrypt(byte[] data, byte[] key) {
        // Configure AES‑256‑CBC with PKCS#7 padding (PKCS#5 is a subset of PKCS#7)
        using var aes = Aes.Create();
        aes.Key = key;                  // 32 bytes key (AES-256)
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;
        aes.GenerateIV();               // !!! Generate a random IV here !!!

        using var ms = new MemoryStream();
        ms.Write(aes.IV, 0, aes.IV.Length);

        using var encryptor = aes.CreateEncryptor();
        using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
        cs.Write(data, 0, data.Length);
        cs.FlushFinalBlock();
        return ms.ToArray();
    }

    // Required when encrypting the Perms (permissions)
    internal static byte[] EncryptECB(byte[] data, byte[] key) {
        using var aes = Aes.Create();
        aes.Key = key;
        aes.IV = new byte[aes.BlockSize / 8];   // 16 bytes all‑zero IV
        aes.Mode = CipherMode.ECB;              // Critical: Use ECB mode, not CBC
        aes.Padding = PaddingMode.None;         // Critical: No padding for this operation

        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(data, 0, data.Length);
    }
}
}   // End of namespace PDFjet.NET
