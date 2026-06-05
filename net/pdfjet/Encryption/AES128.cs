/**
 * AES128.cs
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
public class AES128 {
    /// <summary>
    /// Encrypts <paramref name="K1"/> with AES‑128‑CBC, **no padding**.
    /// </summary>
    /// <param name="K1">Data to encrypt (must be a multiple of 16 bytes).</param>
    /// <param name="key">Exactly 16 bytes – the AES‑128 key.</param>
    /// <param name="iv">Exactly 16 bytes – the initialization vector.</param>
    /// <returns>The ciphertext.</returns>
    /// <exception cref="ArgumentException">If any argument is null or has an invalid length.</exception>
    internal static byte[] EncryptK1(byte[] K1, byte[] key, byte[] iv) {
        // ---------- basic argument validation ----------
        if (K1 == null || K1.Length == 0)
            throw new ArgumentException("K1 cannot be null or empty.", nameof(K1));

        if (key == null || key.Length != 16)
            throw new ArgumentException("Key must be exactly 16 bytes (AES‑128).", nameof(key));

        if (iv == null || iv.Length != 16)
            throw new ArgumentException("IV must be exactly 16 bytes.", nameof(iv));

        // ---------- AES‑128‑CBC, no padding ----------
        using var aes = Aes.Create();       // defaults: CBC, PKCS7, 256‑bit key
        aes.Key = key;                      // automatically sets KeySize = 128
        aes.IV = iv;
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.None;     // critical for Algorithm 2.B

        // ---------- encrypt in one shot ----------
        // TransformFinalBlock allocates the output array for us.
        using var encryptor = aes.CreateEncryptor();
        return encryptor.TransformFinalBlock(K1, 0, K1.Length);
    }
}
}   // End of namespace PDFjet.NET
