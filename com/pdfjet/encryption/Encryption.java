/**
 * Encryption.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.encryption;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.nio.charset.StandardCharsets;
import java.security.MessageDigest;
import java.security.NoSuchAlgorithmException;
import java.security.SecureRandom;
import java.util.Arrays;
// import java.math.BigInteger;

import com.pdfjet.*;

public class Encryption {
    private final byte[] fileEncryptionKey;
    private final int objNumber;
    private final MessageDigest sha256;
    private final MessageDigest sha384;
    private final MessageDigest sha512;
    private final ByteArrayOutputStream stream;

    // Internal helper classes
    private static class User {
        public byte[] U;
        public byte[] UE;
        public User(byte[] U, byte[] UE) {
            this.U = U;
            this.UE = UE;
        }
    }

    private static class Owner {
        public byte[] O;
        public byte[] OE;
        public Owner(byte[] O, byte[] OE) {
            this.O = O;
            this.OE = OE;
        }
    }

    /**
     * Creates a new encryption dictionary and adds it to the PDF.
     */
    public Encryption(PDF pdf, Passwords passwords, Permissions permissions) throws Exception {
        // === Generate a random 256-bit (32-byte) File Encryption Key ===
        this.fileEncryptionKey = new byte[32];
        SecureRandom random = new SecureRandom();
        random.nextBytes(this.fileEncryptionKey);

        stream = new ByteArrayOutputStream(32768);  // 32 KB buffer
        try {
            sha256 = MessageDigest.getInstance("SHA-256");
            sha384 = MessageDigest.getInstance("SHA-384");
            sha512 = MessageDigest.getInstance("SHA-512");
        } catch (NoSuchAlgorithmException e) {
            throw new RuntimeException("SHA algorithm not available", e);
        }

        String userPassword = passwords.getUserPassword();
        if (userPassword.length() > 127) {
            userPassword = userPassword.substring(0, 127);
        }
        String ownerPassword = passwords.getOwnerPassword();
        if (ownerPassword.length() > 127) {
            ownerPassword = ownerPassword.substring(0, 127);
        }
        byte[] userPasswordBytes = userPassword.getBytes(StandardCharsets.UTF_8);
        byte[] ownerPasswordBytes = ownerPassword.getBytes(StandardCharsets.UTF_8);

        User user = computeUserKeys(userPasswordBytes);
        Owner owner = computeOwnerKeys(ownerPasswordBytes, user.U);

        // Zero out sensitive data
        Arrays.fill(userPasswordBytes, (byte) 0);
        Arrays.fill(ownerPasswordBytes, (byte) 0);

        try {
            stream.close();
        } catch (IOException e) {
            // Ignore close exception
        }

        // === Encryption Dictionary ===
        pdf.newobj();
        pdf.append(Token.BEGIN_DICTIONARY);
        pdf.append("/Filter /Standard\n");
        pdf.append("/V 5\n");           // Algorithm 2.A / 2.B
        pdf.append("/R 6\n");           // Security revision 6 (strong password hashing)
        pdf.append("/CF <<\n");
        pdf.append("/StdCF <<\n");
        pdf.append("/CFM /AESV3\n");    // AESV3 = AES-256 in CBC
        pdf.append("/Length 32\n");     // 32 bytes = 256-bit file key
        pdf.append("/AuthEvent /DocOpen\n");
        pdf.append(">>\n");
        pdf.append(">>\n");
        pdf.append("/StmF /StdCF\n");
        pdf.append("/StrF /StdCF\n");

        pdf.append("/U <");             // === User Key (U) ===
        pdf.append(toHex(user.U));
        pdf.append(">\n");

        pdf.append("/O <");             // === Owner Key (O) ===
        pdf.append(toHex(owner.O));
        pdf.append(">\n");

        pdf.append("/UE <");            // === User Encryption Key (UE) ===
        pdf.append(toHex(user.UE));
        pdf.append(">\n");

        pdf.append("/OE <");            // === Owner Encryption Key (OE) ===
        pdf.append(toHex(owner.OE));
        pdf.append(">\n");

        pdf.append("/EncryptMetadata false\n");

        // A set of flags specifying which operations shall be permitted
        pdf.append("/P ");
        pdf.append(String.valueOf(permissions.getRawValue()));
        pdf.append("\n");

        // Create the unencrypted block per Algorithm 10
        byte[] perms = createUnencryptedPermsBlock(permissions.getRawValue());
        perms[8]  = (byte) 'F'; // for EncryptMetadata false and 'T' for true
        perms[9]  = (byte) 'a';
        perms[10] = (byte) 'd';
        perms[11] = (byte) 'b';
        perms[12] = (byte) '-';
        perms[13] = (byte) '-';
        perms[14] = (byte) '-';
        perms[15] = (byte) '-';

        // A 16-byte string, encrypted with the file encryption key
        byte[] encryptedPermsBlock = AES256.encryptECB(perms, fileEncryptionKey);

        pdf.append("/Perms <");
        pdf.append(toHex(encryptedPermsBlock));
        pdf.append(">\n");

        pdf.append(Token.END_DICTIONARY);
        pdf.endobj();

        objNumber = pdf.getObjNumber();
    }

    public byte[] getKey() {
        return fileEncryptionKey.clone(); // Return copy for safety
    }

    public int getObjNumber() {
        return objNumber;
    }

    /**
     * Computes a hash value based on the provided password, salt and an user key U.
     */
    private byte[] computeHash(byte[] password, byte[] salt, byte[] U) throws Exception {
        // Take the SHA-256 hash of the original input
        sha256.reset();
        byte[] K = sha256.digest(concatenate(password, salt, U));

        // Perform the following steps (a)-(d) 64 times or more:
        int round = 0;
        while (true) {
            round++;
            // a) Make a new string, K1, consisting of 64 repetitions
            byte[] K1 = computeK1(password, K, U);

            // b) Encrypt K1 with the AES-128 (CBC, no padding) algorithm
            byte[] tempKey = new byte[16];
            System.arraycopy(K, 0, tempKey, 0, 16);
            byte[] tempIV = new byte[16];
            System.arraycopy(K, 16, tempIV, 0, 16);
            byte[] E = AES128.encryptK1(K1, tempKey, tempIV);

            // c) & d) Determine hash algorithm and compute hash
            int algorithm = nextHashAlgorithm(E);
            if (algorithm == 0) {
                sha256.reset();
                K = sha256.digest(E);
            } else if (algorithm == 1) {
                sha384.reset();
                K = sha384.digest(E);
            } else if (algorithm == 2) {
                sha512.reset();
                K = sha512.digest(E);
            }

            // Termination check (For rounds 64+ only)
            if (round >= 64 && (E[E.length - 1] & 0xFF) <= (round - 32)) {
                break;
            }
        }

        byte[] finalOutput = new byte[32];
        System.arraycopy(K, 0, finalOutput, 0, 32);
        return finalOutput;
    }

    private byte[] computeK1(byte[] password, byte[] K, byte[] U) {
        stream.reset();
        for (int i = 0; i < 64; i++) {
            stream.write(password, 0, password.length);
            stream.write(K, 0, K.length);
            stream.write(U, 0, U.length);
        }
        return stream.toByteArray();
    }

    /**
     * Analyzes the first 16 bytes of the 'E' to determine the next hash algorithm to use.
     */
    private int nextHashAlgorithm(byte[] E) {
        if (E.length < 16) {
            throw new IllegalArgumentException("The input array must be at least 16 bytes long.");
        }

        // Alternative code that also works and follows the specification exactly!
        // BigInteger sum = new BigInteger(1, Arrays.copyOf(E, 16));    // 1 = force positive (unsigned)
        // BigInteger reminder = sum.mod(new BigInteger("3"));
        // return reminder.intValueExact();

        int sum = 0;
        for (int i = 0; i < 16; i++) {
            sum += (E[i] & 0xFF);   // Convert to unsigned
        }
        return sum % 3;
    }

    private String toHex(byte[] bytes) {
        StringBuilder hex = new StringBuilder(bytes.length * 2);
        for (byte b : bytes) {
            hex.append(String.format("%02x", b & 0xFF));
        }
        return hex.toString();
    }

    /**
     * Algorithm 8: Computing the encryption dictionary's U (user password) and UE (user encryption) values
     */
    private User computeUserKeys(byte[] userPasswordBytes) throws Exception {
        byte[] randomBytes = new byte[16];
        SecureRandom random = new SecureRandom();
        random.nextBytes(randomBytes);

        byte[] userValidationSalt = new byte[8];
        byte[] userKeySalt = new byte[8];
        byte[] hash = computeHash(userPasswordBytes, userValidationSalt, new byte[] {});
        byte[] U = concatenate(hash, userValidationSalt, userKeySalt);

        hash = computeHash(userPasswordBytes, userKeySalt, new byte[] {});
        byte[] UE = AES256.encryptWithZeroIV(fileEncryptionKey, hash);

        return new User(U, UE);
    }

    /**
     * Algorithm 9: Computing the encryption dictionary's O (owner password) and OE (owner encryption) values
     */
    private Owner computeOwnerKeys(byte[] ownerPasswordBytes, byte[] U) throws Exception {
        byte[] randomBytes = new byte[16];
        SecureRandom random = new SecureRandom();
        random.nextBytes(randomBytes);

        byte[] ownerValidationSalt = new byte[8];
        byte[] ownerKeySalt = new byte[8];
        byte[] hash = computeHash(ownerPasswordBytes, ownerValidationSalt, U);
        byte[] O = concatenate(hash, ownerValidationSalt, ownerKeySalt);

        hash = computeHash(ownerPasswordBytes, ownerKeySalt, U);
        byte[] OE = AES256.encryptWithZeroIV(fileEncryptionKey, hash);

        return new Owner(O, OE);
    }

    private byte[] concatenate(byte[] array1, byte[] array2, byte[] array3) {
        byte[] result = new byte[array1.length + array2.length + array3.length];
        System.arraycopy(array1, 0, result, 0, array1.length);
        System.arraycopy(array2, 0, result, array1.length, array2.length);
        System.arraycopy(array3, 0, result, array1.length + array2.length, array3.length);
        return result;
    }

    /**
     * Creates the unencrypted permissions block for Algorithm 10
     */
    public static byte[] createUnencryptedPermsBlock(long permissionsValue) {
        // Extend the 32-bit permission to 64 bits with upper 32 bits set to 1
        long extendedPermissions = 0xFFFF_FFFF_0000_0000L | (permissionsValue & 0xFFFFFFFFL);

        // Get the 8 bytes of the permission in little-endian order
        byte[] permsBlock = new byte[16];
        byte[] permissionBytes = new byte[8];

        // Convert to little-endian
        for (int i = 0; i < 8; i++) {
            permissionBytes[i] = (byte) ((extendedPermissions >> (i * 8)) & 0xFF);
        }

        System.arraycopy(permissionBytes, 0, permsBlock, 0, 8);
        // Bytes 8-15 remain 0 for now (will be filled with validation code)
        return permsBlock;
    }
}
