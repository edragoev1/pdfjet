/*
 * JPGImage.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
/*
 * JPGImage.java
 *
 * The authors make NO WARRANTY or representation, either express or implied,
 * with respect to this software, its quality, accuracy, merchantability, or
 * fitness for a particular purpose. This software is provided "AS IS", and you,
 * its user, assume the entire risk as to its quality and accuracy.
 *
 * This software is copyright (C) 1991-1998, Thomas G. Lane.
 * All Rights Reserved except as specified below.
 *
 * Permission is hereby granted to use, copy, modify, and distribute this
 * software (or portions thereof) for any purpose, without fee, subject to these
 * conditions:
 * (1) If any part of the source code for this software is distributed, then this
 * README file must be included, with this copyright and no-warranty notice
 * unaltered; and any additions, deletions, or changes to the original files
 * must be clearly indicated in accompanying documentation.
 * (2) If only executable code is distributed, then the accompanying
 * documentation must state that "this software is based in part on the work of
 * the Independent JPEG Group".
 * (3) Permission for use of this software is granted only if the user accepts
 * full responsibility for any undesirable consequences; the authors accept
 * NO LIABILITY for damages of any kind.
 *
 * These conditions apply to any software derived from or based on the IJG code,
 * not just to the unmodified library.  If you use our work, you ought to
 * acknowledge us.
 *
 * Permission is NOT granted for the use of any IJG author's name or company name
 * in advertising or publicity relating to this software or products derived from
 * it.  This software may be referred to only as "the Independent JPEG Group's
 * software".
 *
 * We specifically permit and encourage the use of this software as the basis of
 * commercial products, provided that all warranty or liability claims are
 * assumed by the product vendor.
 */
package com.pdfjet;

import java.io.*;

/**
 * Used to embed JPG images in the PDF document.
 */
class JPGImage {
    static final char M_SOF0  = (char) 0x00C0;  // Start Of Frame N
    static final char M_SOF1  = (char) 0x00C1;  // N indicates which compression process
    static final char M_SOF2  = (char) 0x00C2;  // Only SOF0-SOF2 are now in common use
    static final char M_SOF3  = (char) 0x00C3;
    static final char M_SOF5  = (char) 0x00C5;  // NB: codes C4 and CC are NOT SOF markers
    static final char M_SOF6  = (char) 0x00C6;
    static final char M_SOF7  = (char) 0x00C7;
    static final char M_SOF9  = (char) 0x00C9;
    static final char M_SOF10 = (char) 0x00CA;
    static final char M_SOF11 = (char) 0x00CB;
    static final char M_SOF13 = (char) 0x00CD;
    static final char M_SOF14 = (char) 0x00CE;
    static final char M_SOF15 = (char) 0x00CF;
    static final char M_APP0  = (char) 0x00E0;  // The JFIF segment, among others
    static final char M_APP14 = (char) 0x00EE;
    // The markers that stand alone, with no parameter segment to skip.
    static final char M_TEM   = (char) 0x0001;  // Temporary, for arithmetic coding
    static final char M_RST0  = (char) 0x00D0;  // ReSTart 0 to 7
    static final char M_RST7  = (char) 0x00D7;
    static final char M_SOI   = (char) 0x00D8;  // Start Of Image
    static final char M_EOI   = (char) 0x00D9;  // End Of Image
    static final char M_SOS   = (char) 0x00DA;  // Start Of Scan, the end of the header

    int width;
    int height;
    int colorComponents;
    // True when an APP14 segment says that Adobe software wrote the image,
    // which stores the inks of a CMYK image inverted, 255 for no ink.
    boolean adobe;
    // The pixel density of the JFIF segment and the unit it is in: 1 is the
    // inch and 2 the centimetre, and 0 is the ratio of the two axes with no
    // size to it. The segment comes before the frame header, so the size the
    // image asks for is worked out after the frame header gives its pixels.
    private int densityUnit;
    private int xDensity;
    private int yDensity;
    byte[] data;

    public JPGImage(InputStream inputStream) throws Exception {
        data = Content.getFromStream(inputStream);
        readJPGImage(new ByteArrayInputStream(data));
    }

    int getWidth() {
        return this.width;
    }

    int getHeight() {
        return this.height;
    }

    long getFileSize() {
        return this.data.length;
    }

    int getColorComponents() {
        return this.colorComponents;
    }

    boolean isAdobe() {
        return this.adobe;
    }

    byte[] getData() {
        return this.data;
    }

    private void readJPGImage(InputStream is) throws IOException {
        char ch1 = (char) is.read();
        char ch2 = (char) is.read();
        if (ch1 != 0x00FF || ch2 != 0x00D8) {
            throw new IOException("Error: Invalid JPEG header.");
        }

        boolean foundSOFn = false;
        while (true) {
            char ch = nextMarker(is);

            // The standalone markers carry no parameter segment, so there is
            // nothing to skip after them; reading two bytes of one as a length
            // would skip over the frame header that follows.
            if (ch == M_TEM || ch == M_SOI || (ch >= M_RST0 && ch <= M_RST7)) {
                continue;
            }
            if (ch == M_EOI) {
                throw new IOException("Error: The JPEG ends before its frame header.");
            }

            switch (ch) {
                // Note that marker codes 0xC4, 0xC8, 0xCC are not,
                // and must not be treated as SOFn. C4 in particular
                // is actually DHT.
                case M_SOF0:    // Baseline
                case M_SOF1:    // Extended sequential, Huffman
                case M_SOF2:    // Progressive, Huffman
                case M_SOF3:    // Lossless, Huffman
                case M_SOF5:    // Differential sequential, Huffman
                case M_SOF6:    // Differential progressive, Huffman
                case M_SOF7:    // Differential lossless, Huffman
                case M_SOF9:    // Extended sequential, arithmetic
                case M_SOF10:   // Progressive, arithmetic
                case M_SOF11:   // Lossless, arithmetic
                case M_SOF13:   // Differential sequential, arithmetic
                case M_SOF14:   // Differential progressive, arithmetic
                case M_SOF15:   // Differential lossless, arithmetic
                // The length of the frame header, then the sample precision:
                // a PDF image stream of DCTDecode data delivers eight bits per
                // color component, so a JPEG of another precision, a 12-bit one
                // among them, cannot be embedded as it is.
                int length = getUInt16(is);
                int precision = readByte(is);
                if (precision != 8) {
                    throw new IOException(
                            "Error: The JPEG has " + precision + " bits per color component, not 8.");
                }
                height = getUInt16(is);
                width = getUInt16(is);
                colorComponents = readByte(is);
                if (width <= 0 || height <= 0 ||
                    (colorComponents != 1 && colorComponents != 3 && colorComponents != 4)) {
                    throw new IOException("Invalid JPEG dimensions or component count.");
                }
                // The frame header holds three bytes for each component after
                // its eight, as libjpeg checks: a header of another length is
                // the "Bogus SOF length" of libjpeg and the "Bogus marker
                // length" of MuPDF, so the readers a PDF is drawn with refuse
                // the image where PDFjet embedded it.
                if (length != 3*colorComponents + 8) {
                    throw new IOException("Error: The JPEG frame header is " + length +
                            " bytes, not the " + (3*colorComponents + 8) +
                            " of its " + colorComponents + " color components.");
                }
                // The component specifications fill the rest of the frame
                // header; they can hold any bytes, the 0xFF of a marker among
                // them, so the markers are read after the whole segment.
                if (length >= 8) {
                    readAdobeMarker(is, length - 8);
                }
                foundSOFn = true;
                break;

                case M_APP0:
                readAPP0(is);
                break;

                case M_APP14:
                readAPP14(is);
                break;

                default:
                skipVariable(is);
                break;
            }

            if (foundSOFn) {
                break;
            }
        }
    }

    // Reads the markers from the frame header to the scan, for an APP14 segment:
    // libjpeg reads a header to the scan too, so a segment that follows the frame
    // header says that Adobe software wrote the image as one before it does. The
    // frame header is all the image needs, so whatever cannot be read after it
    // leaves the image as it is.
    private void readAdobeMarker(InputStream is, int skip) {
        try {
            for (int i = 0; i < skip; i++) {
                readByte(is);
            }
            while (true) {
                char ch = nextMarker(is);
                if (ch == M_SOS || ch == M_EOI) {
                    return;
                }
                if (ch == M_TEM || ch == M_SOI || (ch >= M_RST0 && ch <= M_RST7)) {
                    continue;
                }
                if (ch == M_APP14) {
                    readAPP14(is);
                } else {
                    skipVariable(is);
                }
            }
        } catch (IOException e) {
            return;
        }
    }

    private int readByte(InputStream is) throws IOException {
        int b = is.read();
        if (b < 0) {
            throw new IOException("Unexpected end of JPEG data.");
        }
        return b;
    }

    private int getUInt16(InputStream is) throws IOException {
        return (readByte(is) << 8) | readByte(is);
    }

    // Skip any non-marker bytes and duplicate FF padding, then return the marker code.
    // An FF byte that a zero byte follows is data and not a marker, so the search goes on.
    // NB: not valid after the SOS marker (doesn't handle FF/00 in compressed data).
    private char nextMarker(InputStream is) throws IOException {
        while (true) {
            while (readByte(is) != 0x00FF) { /* skip garbage */ }
            int ch;
            do {
                ch = readByte(is);
            } while (ch == 0x00FF);
            if (ch != 0x0000) {
                return (char) ch;
            }
        }
    }

    // Reads an APP14 segment, which Adobe software writes starting with "Adobe".
    // Reads the pixel density of a JFIF segment, which is the first segment of
    // a JFIF file: "JFIF", a zero byte, the version, the unit of the density
    // and the density of each axis. A segment of another kind, the JFXX
    // extension among them, is passed over. The resolution an Exif segment
    // holds is not read: it is a TIFF image file directory, which is a format
    // of its own, and a JFIF segment is what the density of a JPEG is.
    private void readAPP0(InputStream is) throws IOException {
        int length = getUInt16(is);
        if (length < 2) {
            throw new IOException("Invalid marker segment length.");
        }
        byte[] segment = new byte[length - 2];
        for (int i = 0; i < segment.length; i++) {
            segment[i] = (byte) readByte(is);   // throws on EOF
        }
        if (segment.length >= 12 &&
                segment[0] == 'J' && segment[1] == 'F' && segment[2] == 'I' &&
                segment[3] == 'F' && segment[4] == 0) {
            densityUnit = segment[7] & 0xFF;
            xDensity = ((segment[8] & 0xFF) << 8) | (segment[9] & 0xFF);
            yDensity = ((segment[10] & 0xFF) << 8) | (segment[11] & 0xFF);
        }
    }

    // The points a unit of the density is: an inch is 72 of them and a
    // centimetre 72/2.54. Unit 0 is a ratio of the axes with no size to it.
    private double pointsPerUnit() {
        if (densityUnit == 1) {
            return 72.0;
        } else if (densityUnit == 2) {
            return 72.0/2.54;
        }
        return 0.0;
    }

    // The size the image asks to be drawn at, in points, or 0 when the JFIF
    // segment gives none: no segment, a unit that is a ratio rather than a
    // size, no density at all, or a size too large for a PDF number.
    float getPhysicalWidth() {
        return physicalSize(this.width, this.xDensity);
    }

    float getPhysicalHeight() {
        return physicalSize(this.height, this.yDensity);
    }

    private float physicalSize(int pixels, int density) {
        double points = pointsPerUnit();
        if (points == 0.0 || density <= 0 || xDensity <= 0 || yDensity <= 0) {
            return 0f;
        }
        float size = (float) (pixels*points/density);
        return FastFloat.isWritable(size) ? size : 0f;
    }

    private void readAPP14(InputStream is) throws IOException {
        int length = getUInt16(is);
        if (length < 2) {
            throw new IOException("Invalid marker segment length.");
        }
        byte[] segment = new byte[length - 2];
        for (int i = 0; i < segment.length; i++) {
            segment[i] = (byte) readByte(is);   // throws on EOF
        }
        // Adobe's segment is twelve bytes: "Adobe", the version, two flags and
        // the color transform. A shorter one is not Adobe's, as in libjpeg. An
        // APP14 segment of another kind after it does not unmark the image.
        if (segment.length >= 12 &&
                segment[0] == 'A' && segment[1] == 'd' && segment[2] == 'o' &&
                segment[3] == 'b' && segment[4] == 'e') {
            adobe = true;
        }
    }

    // Most types of marker are followed by a variable-length parameter
    // segment. This routine skips over the parameters for any marker we
    // don't otherwise want to process.
    // Note that we MUST skip the parameter segment explicitly in order
    // not to be fooled by 0xFF bytes that might appear within the
    // parameter segment such bytes do NOT introduce new markers.
    private void skipVariable(InputStream is) throws IOException {
        int length = getUInt16(is);
        if (length < 2) {
            throw new IOException("Invalid marker segment length.");
        }
        for (int i = 0; i < length - 2; i++) {
            readByte(is);   // throws on EOF
        }
    }
}   // End of JPGImage.java
