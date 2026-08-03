/**
 * AES256.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.encryption;

import javax.crypto.Cipher;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;
import java.security.SecureRandom;
import java.util.Arrays;
import java.security.GeneralSecurityException;

public class AES256 {
    /**
     * Encrypts a 32-byte File Encryption Key (FEK) with AES-256-CBC,
     * using a zero IV and no padding.
     *
     * @param fileEncryptionKey 32-byte FEK to encrypt.
     * @param key 32-byte key (hash) used for AES-256 encryption.
     * @return The encrypted 32-byte File Encryption Key.
     * @throws IllegalArgumentException if key lengths are invalid.
     */
    public static byte[] encryptWithZeroIV(byte[] fileEncryptionKey, byte[] key) {
        if (fileEncryptionKey == null || fileEncryptionKey.length != 32) {
            throw new IllegalArgumentException("File Encryption Key must be 32 bytes long.");
        }
        if (key == null || key.length != 32) {
            throw new IllegalArgumentException("The encryption key must be 32 bytes long.");
        }

        try {
            // Create zero IV (16 bytes)
            byte[] iv = new byte[16]; // All zeros by default

            // Set up AES-256-CBC with zero IV and no padding
            SecretKeySpec keySpec = new SecretKeySpec(key, "AES");
            IvParameterSpec ivSpec = new IvParameterSpec(iv);

            Cipher cipher = Cipher.getInstance("AES/CBC/NoPadding");
            cipher.init(Cipher.ENCRYPT_MODE, keySpec, ivSpec);

            // Perform the encryption in one block
            return cipher.doFinal(fileEncryptionKey);
        } catch (GeneralSecurityException e) {
            throw new RuntimeException("AES encryption failed", e);
        }
    }

    /**
     * Encrypts the provided data using AES-256 in CBC mode with a randomly generated IV.
     * The resulting byte array is structured as: [16-byte IV][encrypted data].
     *
     * @param data The data to be encrypted.
     * @param key The 32-byte encryption key for AES-256.
     * @return A byte array containing the IV prepended to the AES-256-CBC encrypted data.
     * @throws IllegalArgumentException if key length is invalid.
     */
    public static byte[] encrypt(byte[] data, byte[] key) {
        if (key == null || key.length != 32) {
            throw new IllegalArgumentException("The encryption key must be 32 bytes long.");
        }

        try {
            // Generate random IV (16 bytes)
            byte[] iv = new byte[16];
            SecureRandom random = new SecureRandom();
            random.nextBytes(iv);

            // Set up AES-256-CBC with PKCS5 padding
            SecretKeySpec keySpec = new SecretKeySpec(key, "AES");
            IvParameterSpec ivSpec = new IvParameterSpec(iv);

            Cipher cipher = Cipher.getInstance("AES/CBC/PKCS5Padding");
            cipher.init(Cipher.ENCRYPT_MODE, keySpec, ivSpec);

            // Encrypt the data
            byte[] encryptedData = cipher.doFinal(data);

            // Combine IV and encrypted data
            byte[] result = new byte[iv.length + encryptedData.length];
            System.arraycopy(iv, 0, result, 0, iv.length);
            System.arraycopy(encryptedData, 0, result, iv.length, encryptedData.length);

            return result;
        } catch (GeneralSecurityException e) {
            throw new RuntimeException("AES encryption failed", e);
        }
    }

    /**
     * Encrypts data using AES-256 in ECB mode with no padding.
     * Required when encrypting the Perms (permissions).
     *
     * @param data The data to be encrypted.
     * @param key The 32-byte encryption key for AES-256.
     * @return The encrypted data.
     * @throws IllegalArgumentException if key length is invalid.
     */
    public static byte[] encryptECB(byte[] data, byte[] key) {
        if (key == null || key.length != 32) {
            throw new IllegalArgumentException("The encryption key must be 32 bytes long.");
        }

        try {
            // Set up AES-256-ECB with no padding
            SecretKeySpec keySpec = new SecretKeySpec(key, "AES");

            Cipher cipher = Cipher.getInstance("AES/ECB/NoPadding");
            cipher.init(Cipher.ENCRYPT_MODE, keySpec);

            return cipher.doFinal(data);
        } catch (GeneralSecurityException e) {
            throw new RuntimeException("AES ECB encryption failed", e);
        }
    }
}