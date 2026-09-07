/*
 * Util.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.nio.ByteBuffer;

public class Util {
    public static String toHexString(byte[] data) {
        StringBuilder sb = new StringBuilder(data.length * 2);
        for (byte b : data) {
            // & 0xFF makes the byte unsigned before formatting
            sb.append(String.format("%02x", b & 0xFF));
        }
        return sb.toString();
    }

    private static final char[] HEX = {
        '0', '1', '2', '3', '4', '5', '6', '7', '8', '9',
        'A', 'B', 'C', 'D', 'E', 'F'
    };

    private String toHex(String str) {
        if (str == null || str.isEmpty()) {
            return "";
        }

        StringBuilder buf = new StringBuilder(str.length() * 6);
        str.codePoints().forEach(codePoint -> {
            if (codePoint != 0xFEFF) {  // Skip BOM
                if (codePoint <= 0xFFFF) {
                    // BMP character (4 hex digits)
                    buf.append(HEX[(codePoint >> 12) & 0xF]);
                    buf.append(HEX[(codePoint >> 8)  & 0xF]);
                    buf.append(HEX[(codePoint >> 4)  & 0xF]);
                    buf.append(HEX[(codePoint)       & 0xF]);
                } else {
                    // Supplementary character (6 hex digits)
                    buf.append(HEX[(codePoint >> 20) & 0xF]);
                    buf.append(HEX[(codePoint >> 16) & 0xF]);
                    buf.append(HEX[(codePoint >> 12) & 0xF]);
                    buf.append(HEX[(codePoint >> 8)  & 0xF]);
                    buf.append(HEX[(codePoint >> 4)  & 0xF]);
                    buf.append(HEX[(codePoint)       & 0xF]);
                }
            }
        });

        return buf.toString();
    }
}
