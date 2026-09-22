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
import java.nio.charset.StandardCharsets;
import java.util.zip.Deflater;
import java.util.zip.DeflaterOutputStream;

/**
 * Used to embed file objects.
 * The file objects must added to the PDF before drawing on the first page.
 */
public class EmbeddedFile {
    /** The object number of the embedded file. */
    protected int objNumber = -1;
    /** The name of the embedded file. */
    protected String fileName;
    // The PDF the file is embedded in.
    PDF pdf;
    // How the file relates to the document, for a file of a document of
    // PDF/A-3, and null for a file that is only attached to a page.
    Relationship relationship;

    /**
     * Embeds file with the specified name into the PDF.
     *
     * @param pdf the PDF.
     * @param fileName the file name.
     * @param compress true to compress the file with Flate.
     * @throws Exception if there is an issue.
     */
    public EmbeddedFile(PDF pdf, String fileName, boolean compress) throws Exception {
        this(pdf, fileName.substring(fileName.lastIndexOf("/") + 1),
                new BufferedInputStream(new FileInputStream(fileName)), compress);
    }

    /**
     * Embeds file with the specified name from the specified stream.
     *
     * @param pdf the PDF.
     * @param fileName the file name.
     * @param stream the input stream.
     * @param compress true to compress the file with Flate.
     * @throws Exception if there is an issue.
     */
    public EmbeddedFile(PDF pdf, String fileName, InputStream stream, boolean compress) throws Exception {
        this(pdf, fileName, stream, compress, null, null, null);
    }

    /**
     * Embeds the file and says what it holds and how it relates to the
     * document. A document of PDF/A-3 needs all three of them for each file it
     * carries, and so does a document that carries the XML of an invoice.
     * PDF.addAssociatedFile adds the embedded file to the document.
     *
     * @param pdf the PDF.
     * @param fileName the file name, such as "factur-x.xml".
     * @param stream the input stream.
     * @param compress true to compress the file with Flate.
     * @param mediaType what the file holds, such as "text/xml", or null.
     * @param relationship how the file relates to the document, or null.
     * @param description what the file is, in the words of a person, or null.
     * @throws Exception if there is an issue.
     */
    public EmbeddedFile(PDF pdf, String fileName, InputStream stream, boolean compress,
            String mediaType, Relationship relationship, String description) throws Exception {
        this.pdf = pdf;
        this.fileName = fileName;
        this.relationship = relationship;
        byte[] buf = Content.getFromStream(stream);
        int size = buf.length;

        if (compress) {
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

        pdf.newObj();
        pdf.append(Token.BEGIN_DICTIONARY);
        pdf.append("/Type /EmbeddedFile\n");
        if (mediaType != null) {
            // What the file holds, as a name: /text#2Fxml for "text/xml".
            pdf.append("/Subtype ");
            pdf.append(toName(mediaType));
            pdf.append(Token.NEWLINE);
            // The size before compression and the date, which PDF/A-3 asks
            // for. The date is the one the document itself carries, since the
            // file is written as the document is.
            pdf.append("/Params <</Size ");
            pdf.append(size);
            pdf.append(" /ModDate (");
            pdf.append(pdf.getDate());
            pdf.append(")>>\n");
        }
        if (compress) {
            pdf.append("/Filter /FlateDecode\n");
        }
        pdf.append(Token.LENGTH);
        pdf.append(buf.length);
        pdf.append(Token.NEWLINE);
        pdf.append(Token.END_DICTIONARY);
        pdf.append(Token.STREAM);
        pdf.append(buf);
        pdf.append(Token.END_STREAM);
        pdf.endObj();

        pdf.newObj();
        pdf.append(Token.BEGIN_DICTIONARY);
        pdf.append("/Type /Filespec\n");

        // The file name as a text string, which every reader decodes the same
        // way. /UF is the name that readers of PDF 1.7 look for first, and /F
        // is the one that older readers know; PDF/A-3 requires both.
        pdf.append("/F ");
        pdf.appendTextString(fileName);
        pdf.append("\n");
        pdf.append("/UF ");
        pdf.appendTextString(fileName);
        pdf.append("\n");

        if (relationship != null) {
            pdf.append("/AFRelationship ");
            pdf.append(relationship.value);
            pdf.append(Token.NEWLINE);
        }
        if (description != null && !description.isEmpty()) {
            pdf.append("/Desc ");
            pdf.appendTextString(description);
            pdf.append("\n");
        }

        pdf.append("/EF <</F ");
        pdf.append(pdf.getObjNumber() - 1);
        pdf.append(" 0 R /UF ");
        pdf.append(pdf.getObjNumber() - 1);
        pdf.append(" 0 R>>\n");
        pdf.append(Token.END_DICTIONARY);
        pdf.endObj();

        this.objNumber = pdf.getObjNumber();
    }

    // The media type as a name of PDF: the characters a name cannot hold
    // written as a number sign and two hexadecimal digits, so that "text/xml"
    // is /text#2Fxml.
    private static String toName(String mediaType) {
        StringBuilder sb = new StringBuilder("/");
        for (byte b : mediaType.getBytes(StandardCharsets.UTF_8)) {
            int ch = b & 0xFF;
            if (ch > 0x20 && ch < 0x7F && "()<>[]{}/%#".indexOf(ch) == -1) {
                sb.append((char) ch);
            } else {
                sb.append('#').append(String.format("%02X", ch));
            }
        }
        return sb.toString();
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
