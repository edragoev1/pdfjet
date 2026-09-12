/*
 * Decryptor.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.ByteArrayOutputStream;
import java.security.MessageDigest;
import java.util.Arrays;
import java.util.List;
import javax.crypto.Cipher;
import javax.crypto.spec.IvParameterSpec;
import javax.crypto.spec.SecretKeySpec;

import com.pdfjet.encryption.AES128;

/**
 * Decrypts the strings and streams of a PDF that is encrypted with the
 * standard security handler, given its user or owner password, or no
 * password for a PDF that opens without one but restricts printing or
 * copying. RC4 and AES-128 (revisions 2 to 4) and AES-256 (revisions 5 and 6)
 * are supported.
 */
final class Decryptor {
    private static final byte[] PADDING = {
        (byte) 0x28, (byte) 0xBF, (byte) 0x4E, (byte) 0x5E, (byte) 0x4E, (byte) 0x75, (byte) 0x8A, (byte) 0x41,
        (byte) 0x64, (byte) 0x00, (byte) 0x4E, (byte) 0x56, (byte) 0xFF, (byte) 0xFA, (byte) 0x01, (byte) 0x08,
        (byte) 0x2E, (byte) 0x2E, (byte) 0x00, (byte) 0xB6, (byte) 0xD0, (byte) 0x68, (byte) 0x3E, (byte) 0x80,
        (byte) 0x2F, (byte) 0x0C, (byte) 0xA9, (byte) 0xFE, (byte) 0x64, (byte) 0x53, (byte) 0x69, (byte) 0x7A
    };

    // The methods of the crypt filters.
    private static final int NONE = 0;
    private static final int RC4 = 1;
    private static final int AES_128 = 2;
    private static final int AES_256 = 3;

    /** The object number of the encryption dictionary. */
    final int objNumber;
    private final byte[] key;
    private final int streamMethod;
    private final int stringMethod;
    private final boolean encryptMetadata;

    /**
     * Returns the decryptor of the PDF, or null when it is not encrypted.
     *
     * @param trailer the trailer, or the cross-reference stream object.
     * @param objects the objects of the PDF.
     * @param password the user or owner password, or an empty string.
     * @throws Exception if the password is wrong or missing, or the encryption is not supported.
     */
    static Decryptor getDecryptor(PDFobj trailer, List<PDFobj> objects, String password)
            throws Exception {
        int i = (trailer == null) ? -1 : trailer.dict.indexOf("/Encrypt");
        if (i == -1 || i + 1 >= trailer.dict.size()) {
            return null;
        }
        PDFobj encrypt = null;
        if (trailer.dict.get(i + 1).equals("<<")) {
            // The dictionary is in the trailer, so there is no object to skip.
            encrypt = new PDFobj();
            encrypt.number = -1;
            int level = 0;
            for (int j = i + 1; j < trailer.dict.size(); j++) {
                String token = trailer.dict.get(j);
                encrypt.dict.add(token);
                if (token.equals("<<")) {
                    level++;
                } else if (token.equals(">>") && --level == 0) {
                    break;
                }
            }
        } else {
            for (PDFobj obj : objects) {
                if (String.valueOf(obj.number).equals(trailer.dict.get(i + 1))) {
                    encrypt = obj;      // The last one is the newest.
                }
            }
        }
        if (encrypt == null) {
            throw new Exception("The encryption dictionary of the PDF was not found.");
        }
        byte[] id = new byte[0];
        i = trailer.dict.indexOf("/ID");
        if (i != -1 && i + 2 < trailer.dict.size() && trailer.dict.get(i + 1).equals("[")) {
            id = toBytes(trailer.dict.get(i + 2));
        }
        return new Decryptor(encrypt, id, password);
    }

    private Decryptor(PDFobj encrypt, byte[] id, String password) throws Exception {
        this.objNumber = encrypt.number;
        if (!encrypt.getValue("/Filter").equals("/Standard")) {
            throw new Exception("The security handler of the PDF is not supported: " +
                    encrypt.getValue("/Filter"));
        }
        int v = getInt(encrypt, "/V");
        int r = getInt(encrypt, "/R");
        this.encryptMetadata = !encrypt.getValue("/EncryptMetadata").equals("false");
        if (v == 1 || v == 2) {
            this.streamMethod = RC4;
            this.stringMethod = RC4;
        } else if (v == 4 || v == 5) {
            this.streamMethod = getMethod(encrypt, encrypt.getValue("/StmF"));
            this.stringMethod = getMethod(encrypt, encrypt.getValue("/StrF"));
        } else {
            throw new Exception("The encryption of the PDF is not supported: /V " + v);
        }
        byte[] u = toBytes(encrypt.getValue("/U"));
        byte[] o = toBytes(encrypt.getValue("/O"));
        if (r == 5 || r == 6) {
            this.key = getKey(r, utf8Password(password), u, o,
                    toBytes(encrypt.getValue("/UE")), toBytes(encrypt.getValue("/OE")));
        } else if (r >= 2 && r <= 4) {
            int length = (v == 1 || r == 2) ? 5 : (v == 4) ? 16 : getInt(encrypt, "/Length") / 8;
            this.key = getKey(r, Math.max(5, Math.min(length, 16)), latin1Password(password),
                    o, u, getInt(encrypt, "/P"), id, encryptMetadata);
        } else {
            throw new Exception("The encryption of the PDF is not supported: /R " + r);
        }
    }

    // Returns the UTF-8 bytes of the password, at most 127 of them, which is
    // the password of revisions 5 and 6.
    private static byte[] utf8Password(String password) {
        byte[] bytes = password.getBytes(java.nio.charset.StandardCharsets.UTF_8);
        return bytes.length > 127 ? Arrays.copyOf(bytes, 127) : bytes;
    }

    // Returns the password in PDFDocEncoding, which is Latin-1 for the
    // characters a password is likely to have, at most 32 bytes of it, which
    // is the password of revisions 2 to 4.
    private static byte[] latin1Password(String password) {
        byte[] bytes = new byte[Math.min(password.length(), 32)];
        for (int i = 0; i < bytes.length; i++) {
            char c = password.charAt(i);
            bytes[i] = (byte) (c < 256 ? c : '?');
        }
        return bytes;
    }

    // The exception for a password that does not open the PDF.
    private static Exception passwordException(byte[] password) {
        return new Exception(password.length == 0 ?
                "The PDF needs a password." : "The password of the PDF is not correct.");
    }

    // Returns the method of the crypt filter with the name in the /CF dictionary.
    private static int getMethod(PDFobj encrypt, String name) {
        List<String> dict = encrypt.dict;
        int level = 0;
        for (int i = dict.indexOf("/CF") + 1; i > 0 && i < dict.size(); i++) {
            String token = dict.get(i);
            if (token.equals("<<")) {
                level++;
            } else if (token.equals(">>")) {
                if (--level == 0) {
                    break;
                }
            } else if (level == 1 && token.equals(name)) {
                for (int j = i + 1; j + 1 < dict.size() && !dict.get(j).equals(">>"); j++) {
                    if (dict.get(j).equals("/CFM")) {
                        String method = dict.get(j + 1);
                        if (method.equals("/V2")) {
                            return RC4;
                        } else if (method.equals("/AESV2")) {
                            return AES_128;
                        } else if (method.equals("/AESV3")) {
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

    // Computes the key of revisions 2 to 4 from the password with algorithm 2
    // of ISO 32000-2, and checks it against /U with algorithms 4 and 5. When
    // the password is not the user password, it is tried as the owner
    // password, which algorithm 7 turns into the user password with /O.
    private static byte[] getKey(
            int r, int length, byte[] password, byte[] o, byte[] u, int p, byte[] id,
            boolean encryptMetadata) throws Exception {
        byte[] padded = pad(password);
        byte[] key = computeKey(r, length, padded, o, p, id, encryptMetadata);
        if (isUserKey(r, length, key, u, id)) {
            return key;
        }
        MessageDigest md5 = MessageDigest.getInstance("MD5");
        byte[] hash = md5.digest(padded);
        if (r >= 3) {
            for (int i = 0; i < 50; i++) {
                hash = md5.digest(hash);
            }
        }
        byte[] ownerKey = Arrays.copyOf(hash, length);
        byte[] userPassword = Arrays.copyOf(o, 32);
        if (r == 2) {
            userPassword = rc4(ownerKey, userPassword);
        } else {
            for (int i = 19; i >= 0; i--) {
                userPassword = rc4(xor(ownerKey, i), userPassword);
            }
        }
        key = computeKey(r, length, userPassword, o, p, id, encryptMetadata);
        if (isUserKey(r, length, key, u, id)) {
            return key;
        }
        throw passwordException(password);
    }

    // Returns the password padded or cut to 32 bytes, step a of algorithm 2.
    private static byte[] pad(byte[] password) {
        byte[] padded = new byte[32];
        int n = Math.min(password.length, 32);
        System.arraycopy(password, 0, padded, 0, n);
        System.arraycopy(PADDING, 0, padded, n, 32 - n);
        return padded;
    }

    // Steps b to h of algorithm 2: the key from the padded password.
    private static byte[] computeKey(
            int r, int length, byte[] padded, byte[] o, int p, byte[] id, boolean encryptMetadata)
            throws Exception {
        MessageDigest md5 = MessageDigest.getInstance("MD5");
        md5.update(padded);
        md5.update(o);
        md5.update(new byte[] {(byte) p, (byte) (p >> 8), (byte) (p >> 16), (byte) (p >> 24)});
        md5.update(id);
        if (r >= 4 && !encryptMetadata) {
            md5.update(new byte[] {(byte) 0xff, (byte) 0xff, (byte) 0xff, (byte) 0xff});
        }
        byte[] hash = md5.digest();
        if (r >= 3) {
            for (int i = 0; i < 50; i++) {
                md5.update(hash, 0, length);
                hash = md5.digest();
            }
        }
        return Arrays.copyOf(hash, length);
    }

    // Algorithms 4 and 5: whether the key computed from a password is the
    // user key, which /U holds encrypted.
    private static boolean isUserKey(int r, int length, byte[] key, byte[] u, byte[] id)
            throws Exception {
        byte[] check;
        int n;
        if (r == 2) {
            check = rc4(key, PADDING);
            n = 32;
        } else {
            MessageDigest md5 = MessageDigest.getInstance("MD5");
            md5.update(PADDING);
            md5.update(id);
            check = rc4(key, md5.digest());
            for (int i = 1; i <= 19; i++) {
                check = rc4(xor(key, i), check);
            }
            n = 16;
        }
        return u.length >= n && Arrays.equals(check, Arrays.copyOf(u, n));
    }

    private static byte[] xor(byte[] key, int i) {
        byte[] k = new byte[key.length];
        for (int j = 0; j < key.length; j++) {
            k[j] = (byte) (key[j] ^ i);
        }
        return k;
    }

    // Algorithms 2.A, 11 and 12 of ISO 32000-2 get the key of revisions 5 and
    // 6 from /UE with the user password, or from /OE with the owner password.
    private static byte[] getKey(int r, byte[] password, byte[] u, byte[] o, byte[] ue, byte[] oe)
            throws Exception {
        if (u.length < 48 || ue.length < 32) {
            throw new Exception("The encryption dictionary of the PDF is not valid.");
        }
        byte[] none = new byte[0];
        if (Arrays.equals(getHash(r, password, Arrays.copyOfRange(u, 32, 40), none),
                Arrays.copyOf(u, 32))) {
            byte[] hash = getHash(r, password, Arrays.copyOfRange(u, 40, 48), none);
            return aesDecrypt(hash, new byte[16], Arrays.copyOf(ue, 32));
        }
        byte[] u48 = Arrays.copyOf(u, 48);
        if (o.length >= 48 && oe.length >= 32 &&
                Arrays.equals(getHash(r, password, Arrays.copyOfRange(o, 32, 40), u48),
                        Arrays.copyOf(o, 32))) {
            byte[] hash = getHash(r, password, Arrays.copyOfRange(o, 40, 48), u48);
            return aesDecrypt(hash, new byte[16], Arrays.copyOf(oe, 32));
        }
        throw passwordException(password);
    }

    // Returns the hash of the password, the salt and, for the owner password,
    // the 48 bytes of /U: SHA-256 in revision 5, and algorithm 2.B of
    // ISO 32000-2 in revision 6.
    private static byte[] getHash(int r, byte[] password, byte[] salt, byte[] udata)
            throws Exception {
        MessageDigest sha256 = MessageDigest.getInstance("SHA-256");
        sha256.update(password);
        sha256.update(salt);
        byte[] k = sha256.digest(udata);
        if (r == 5) {
            return k;
        }
        for (int round = 1; ; round++) {
            byte[] block = new byte[password.length + k.length + udata.length];
            System.arraycopy(password, 0, block, 0, password.length);
            System.arraycopy(k, 0, block, password.length, k.length);
            System.arraycopy(udata, 0, block, password.length + k.length, udata.length);
            byte[] k1 = new byte[64 * block.length];
            for (int i = 0; i < 64; i++) {
                System.arraycopy(block, 0, k1, i * block.length, block.length);
            }
            byte[] e = AES128.encryptK1(k1, Arrays.copyOf(k, 16), Arrays.copyOfRange(k, 16, 32));
            int sum = 0;
            for (int i = 0; i < 16; i++) {
                sum += e[i] & 0xff;
            }
            String algorithm = (sum % 3 == 0) ? "SHA-256" : (sum % 3 == 1) ? "SHA-384" : "SHA-512";
            k = MessageDigest.getInstance(algorithm).digest(e);
            if (round >= 64 && (e[e.length - 1] & 0xff) <= round - 32) {
                break;
            }
        }
        return Arrays.copyOf(k, 32);
    }

    /**
     * Decrypts the strings in the dictionary of the object, which become
     * hexadecimal strings.
     *
     * @param obj the object.
     * @throws Exception if the strings cannot be decrypted.
     */
    void decryptStrings(PDFobj obj) throws Exception {
        if (stringMethod == NONE) {
            return;
        }
        for (int i = 0; i < obj.dict.size(); i++) {
            String token = obj.dict.get(i);
            if (token.startsWith("(") || (token.startsWith("<") && !token.equals("<<"))) {
                byte[] bytes = decrypt(toBytes(token), stringMethod, obj);
                StringBuilder buf = new StringBuilder("<");
                for (byte b : bytes) {
                    buf.append(Character.forDigit((b >> 4) & 0xf, 16));
                    buf.append(Character.forDigit(b & 0xf, 16));
                }
                obj.dict.set(i, buf.append('>').toString());
            }
        }
    }

    /**
     * Returns the decrypted stream of the object.
     *
     * @param obj the object.
     * @param stream the encrypted stream.
     * @return the decrypted stream.
     * @throws Exception if the stream cannot be decrypted.
     */
    byte[] decryptStream(PDFobj obj, byte[] stream) throws Exception {
        if (!encryptMetadata && obj.getValue("/Type").equals("/Metadata")) {
            return stream;
        }
        return decrypt(stream, streamMethod, obj);
    }

    // Algorithm 1 of ISO 32000-2 decrypts with a key for each object in
    // revisions 2 to 4, and revisions 5 and 6 use the file key.
    private byte[] decrypt(byte[] data, int method, PDFobj obj) throws Exception {
        if (method == NONE) {
            return data;
        }
        byte[] objectKey = key;
        if (method != AES_256) {
            int number = obj.number;
            int generation = 0;
            try {
                generation = Integer.parseInt(obj.dict.get(1));
            } catch (NumberFormatException e) {
                generation = 0;
            }
            MessageDigest md5 = MessageDigest.getInstance("MD5");
            md5.update(key);
            md5.update(new byte[] {
                    (byte) number, (byte) (number >> 8), (byte) (number >> 16),
                    (byte) generation, (byte) (generation >> 8)});
            if (method == AES_128) {
                md5.update(new byte[] {'s', 'A', 'l', 'T'});
            }
            objectKey = Arrays.copyOf(md5.digest(), Math.min(key.length + 5, 16));
        }
        if (method == RC4) {
            return rc4(objectKey, data);
        }
        // The data starts with the initialization vector, and is padded to whole blocks.
        if (data.length < 32) {
            return new byte[0];
        }
        byte[] decrypted = aesDecrypt(objectKey, Arrays.copyOf(data, 16),
                Arrays.copyOfRange(data, 16, 16 + (data.length - 16) / 16 * 16));
        int padding = decrypted[decrypted.length - 1] & 0xff;
        if (padding >= 1 && padding <= 16) {
            return Arrays.copyOf(decrypted, decrypted.length - padding);
        }
        return decrypted;
    }

    private static byte[] aesDecrypt(byte[] key, byte[] iv, byte[] data) throws Exception {
        Cipher cipher = Cipher.getInstance("AES/CBC/NoPadding");
        cipher.init(Cipher.DECRYPT_MODE, new SecretKeySpec(key, "AES"), new IvParameterSpec(iv));
        return cipher.doFinal(data);
    }

    private static byte[] rc4(byte[] key, byte[] data) {
        int[] s = new int[256];
        for (int i = 0; i < 256; i++) {
            s[i] = i;
        }
        for (int i = 0, j = 0; i < 256; i++) {
            j = (j + s[i] + (key[i % key.length] & 0xff)) & 0xff;
            int t = s[i];
            s[i] = s[j];
            s[j] = t;
        }
        byte[] result = new byte[data.length];
        for (int k = 0, i = 0, j = 0; k < data.length; k++) {
            i = (i + 1) & 0xff;
            j = (j + s[i]) & 0xff;
            int t = s[i];
            s[i] = s[j];
            s[j] = t;
            result[k] = (byte) (data[k] ^ s[(s[i] + s[j]) & 0xff]);
        }
        return result;
    }

    // Returns the integer value of the key, or 0. /P can be written as an
    // unsigned number.
    private static int getInt(PDFobj obj, String key) {
        try {
            return (int) Long.parseLong(obj.getValue(key));
        } catch (NumberFormatException e) {
            return 0;
        }
    }

    /**
     * Returns the bytes of a literal string like (a\)b) or of a hexadecimal
     * string like &lt;612962&gt;.
     *
     * @param token the string token.
     * @return the bytes of the string.
     */
    static byte[] toBytes(String token) {
        ByteArrayOutputStream bos = new ByteArrayOutputStream(token.length());
        if (token.startsWith("<")) {
            int high = -1;
            for (int i = 1; i < token.length(); i++) {
                int digit = Decompressor.hexValue(token.charAt(i));
                if (digit == -1) {
                    continue;
                }
                if (high == -1) {
                    high = digit;
                } else {
                    bos.write((high << 4) | digit);
                    high = -1;
                }
            }
            if (high != -1) {
                bos.write(high << 4);
            }
        } else if (token.startsWith("(")) {
            int end = token.endsWith(")") ? token.length() - 1 : token.length();
            for (int i = 1; i < end; i++) {
                char c = token.charAt(i);
                if (c == '\\' && i + 1 < end) {
                    c = token.charAt(++i);
                    int k = "nrtbf".indexOf(c);
                    if (k != -1) {
                        bos.write("\n\r\t\b\f".charAt(k));
                    } else if (c >= '0' && c <= '7') {
                        int value = c - '0';
                        for (int n = 1; n < 3 && i + 1 < end &&
                                token.charAt(i + 1) >= '0' && token.charAt(i + 1) <= '7'; n++) {
                            value = value * 8 + (token.charAt(++i) - '0');
                        }
                        bos.write(value);
                    } else if (c == '\r') {
                        // A backslash at the end of a line continues the string.
                        if (i + 1 < end && token.charAt(i + 1) == '\n') {
                            i++;
                        }
                    } else if (c != '\n') {
                        bos.write(c);   // Like \( \) and \\
                    }
                } else if (c == '\r') {
                    // An end of line in a string is a line feed.
                    if (i + 1 < end && token.charAt(i + 1) == '\n') {
                        i++;
                    }
                    bos.write('\n');
                } else {
                    bos.write(c);
                }
            }
        }
        return bos.toByteArray();
    }
}
