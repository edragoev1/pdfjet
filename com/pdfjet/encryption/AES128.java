/**
 * AES128.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.encryption;

import javax.crypto.Cipher;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import java.security.GeneralSecurityException;

public class AES128 {
    /**
     * Encrypts K1 with AES-128-CBC, no padding.
     *
     * @param K1 Data to encrypt (must be a multiple of 16 bytes).
     * @param key Exactly 16 bytes – the AES-128 key.
     * @param iv Exactly 16 bytes – the initialization vector.
     * @return The ciphertext.
     * @throws IllegalArgumentException If any argument is null or has an invalid length.
     */
    public static byte[] encryptK1(byte[] K1, byte[] key, byte[] iv) {
        // ---------- basic argument validation ----------
        if (K1 == null || K1.length == 0) {
            throw new IllegalArgumentException("K1 cannot be null or empty.");
        }

        if (key == null || key.length != 16) {
            throw new IllegalArgumentException("Key must be exactly 16 bytes (AES-128).");
        }

        if (iv == null || iv.length != 16) {
            throw new IllegalArgumentException("IV must be exactly 16 bytes.");
        }

        try {
            // ---------- AES-128-CBC, no padding ----------
            Cipher cipher = Cipher.getInstance("AES/CBC/NoPadding");

            SecretKeySpec keySpec = new SecretKeySpec(key, "AES");
            IvParameterSpec ivSpec = new IvParameterSpec(iv);

            cipher.init(Cipher.ENCRYPT_MODE, keySpec, ivSpec);

            // ---------- encrypt in one shot ----------
            return cipher.doFinal(K1);
        } catch (GeneralSecurityException e) {
            throw new RuntimeException("AES-128 encryption failed", e);
        }
    }
}