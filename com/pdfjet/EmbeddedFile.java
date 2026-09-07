/*
 * EmbeddedFile.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.encryption.*;
import java.io.BufferedInputStream;
import java.io.ByteArrayOutputStream;
import java.io.FileInputStream;
import java.io.InputStream;
import java.util.zip.Deflater;
import java.util.zip.DeflaterOutputStream;

/**
 * Used to embed file objects.
 * The file objects must added to the PDF before drawing on the first page.
 */
public class EmbeddedFile {
    protected int objNumber = -1;
    protected String fileName;

    /**
     * Embeds file with the specified name into the PDF.
     *
     * @param pdf the PDF.
     * @param fileName the file name.
     * @param compress the file if true do not compress if false.
     * @throws Exception if there is an issue.
     */
    public EmbeddedFile(PDF pdf, String fileName, Compress compress) throws Exception {
        this(pdf, fileName.substring(fileName.lastIndexOf("/") + 1),
                new BufferedInputStream(new FileInputStream(fileName)), compress);
    }

    /**
     * Embeds file with the specified name from the specified stream.
     *
     * @param pdf the PDF.
     * @param fileName the file name.
     * @param stream the input stream.
     * @param compress the file if true do not compress if false.
     * @throws Exception if there is an issue.
     */
    public EmbeddedFile(PDF pdf, String fileName, InputStream stream, Compress compress) throws Exception {
        this.fileName = fileName;
        byte[] buf = Content.getFromStream(stream);

        if (compress == Compress.YES) {
            ByteArrayOutputStream baos = new ByteArrayOutputStream();
            Deflater deflater = new Deflater();
            DeflaterOutputStream dos = new DeflaterOutputStream(baos, deflater);
            dos.write(buf, 0, buf.length);
            dos.finish();
            deflater.end();
            buf = baos.toByteArray();
        }

        if (pdf.encryption != null) {
            buf = AES256.encrypt(buf, pdf.encryption.getKey());
        }

        pdf.newobj();
        pdf.append(Token.BEGIN_DICTIONARY);
        pdf.append("/Type /EmbeddedFile\n");
        if (compress == Compress.YES) {
            pdf.append("/Filter /FlateDecode\n");
        }
        pdf.append(Token.LENGTH);
        pdf.append(buf.length);
        pdf.append(Token.NEWLINE);
        pdf.append(Token.END_DICTIONARY);
        pdf.append(Token.STREAM);
        pdf.append(buf);
        pdf.append(Token.END_STREAM);
        pdf.endobj();

        pdf.newobj();
        pdf.append(Token.BEGIN_DICTIONARY);
        pdf.append("/Type /Filespec\n");

        byte[] fileNameBytes = fileName.getBytes(java.nio.charset.StandardCharsets.UTF_8);
        if (pdf.encryption != null) {
            fileNameBytes = AES256.encrypt(fileNameBytes, pdf.encryption.getKey());
        }
        pdf.append("/F <");
        pdf.append(Util.toHexString(fileNameBytes));
        pdf.append(">\n");

        pdf.append("/EF <</F ");
        pdf.append(pdf.getObjNumber() - 1);
        pdf.append(" 0 R>>\n");
        pdf.append(Token.END_DICTIONARY);
        pdf.endobj();

        this.objNumber = pdf.getObjNumber();
    }

    /**
     * Returns the file name of the embedded file.
     *
     * @return the file name of the embedded file.
     */
    public String getFileName() {
        return fileName;
    }
}   // End of EmbeddedFile.java
