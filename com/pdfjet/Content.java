/*
 * Content.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.*;
import java.util.*;
import java.nio.charset.StandardCharsets;

/**
 * The Content.java class.
 */
public class Content {
    private Content() {
    }

    /**
     * Returns the file contents as string.
     *
     * @param fileName the name of the file.
     * @return the content of the file.
     * @throws IOException if there is an issue.
     */
    public static String ofTextFile(String fileName) throws IOException {
        StringBuilder sb = new StringBuilder(4096);
        try (Reader reader = new InputStreamReader(
                new BufferedInputStream(new FileInputStream(fileName)), StandardCharsets.UTF_8)) {
            int ch = 0;
            while ((ch = reader.read()) != -1) {
                if (ch == '\r') {
                    // Skip it
                } else if (ch == '"') {
                    sb.append("\"");
                } else {
                    sb.append((char) ch);
                }
            }
        }
        // A byte order mark at the start of the file is not part of the text.
        if (sb.length() > 0 && sb.charAt(0) == '\uFEFF') {
            sb.deleteCharAt(0);
        }
        return sb.toString();
    }

    /**
     * Returns the content of the file as byte array.
     *
     * @param fileName the name of the file.
     * @return the content of the file as byte array.
     * @throws Exception if there is an issue.
     */
    public static byte[] ofBinaryFile(String fileName) throws Exception {
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        try (InputStream stream = new BufferedInputStream(new FileInputStream(fileName))) {
            byte[] buffer = new byte[4096];
            int read = 0;
            while ((read = stream.read(buffer, 0, buffer.length)) > 0) {
                baos.write(buffer, 0, read);
            }
        }
        return baos.toByteArray();
    }

    /**
     * Returns the content of the input stream as byte array.
     *
     * @param stream the input stream.
     * @param bufferSize the buffer size.
     * @return the byte array.
     * @throws Exception if there is an issue.
     */
    public static byte[] getFromStream(InputStream stream, int bufferSize) throws Exception {
        ByteArrayOutputStream baos = new ByteArrayOutputStream();
        try {
            byte[] buffer = new byte[bufferSize];
            int read = 0;
            while ((read = stream.read(buffer, 0, bufferSize)) > 0) {
                baos.write(buffer, 0, read);
            }
        } finally {
            stream.close();
        }
        return baos.toByteArray();
    }

    /**
     * Returns the content of the input stream as byte array.
     *
     * @param stream the input stream.
     * @return the byte array.
     * @throws Exception if there is an issue.
     */
    public static byte[] getFromStream(InputStream stream) throws Exception {
        return getFromStream(stream, 4096);
    }

    /**
     * Returns the lines of a UTF-8 text file, without a byte order mark and
     * without the line separators. An empty line is an empty string.
     *
     * @param fileName the path of the file.
     * @return the lines.
     * @throws IOException if the file cannot be read.
     */
    public static List<String> linesOfTextFile(String fileName) throws IOException {
        List<String> lines = new ArrayList<String>();
        StringBuilder buffer = new StringBuilder();
        for (char ch : ofTextFile(fileName).toCharArray()) {
            if (ch == '\n') {
                lines.add(buffer.toString());
                buffer.setLength(0);
            } else {
                buffer.append(ch);
            }
        }
        if (buffer.length() > 0) {
            lines.add(buffer.toString());
        }
        return lines;
    }
}
