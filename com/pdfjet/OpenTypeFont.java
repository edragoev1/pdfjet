/*
 * OpenTypeFont.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.encryption.*;
import java.io.*;
import java.util.*;

class OpenTypeFont {
    protected static void register(
            PDF pdf, Font font, InputStream inputStream) throws Exception {
        OTF otf = new OTF(inputStream);
        setData(font, otf);

        // The font is written at complete(), a subset of the glyphs drawn; its
        // pages refer to the number reserved for it.
        if (otf.cff) {
            Subset.share(pdf, font, Arrays.copyOfRange(otf.buf, otf.cffOff, otf.cffOff + otf.cffLen),
                    true, (otf.fsType & 0x0100) != 0);
        } else {
            Subset.share(pdf, font, otf.buf, false, false);
        }
        font.objNumber = pdf.reserveObjNumber();
        pdf.fonts.add(font);
    }

    // Adds the font to the objects of an existing PDF; its program is
    // written when the objects are added to the PDF, a subset of the glyphs
    // drawn.
    protected static void register(
            List<PDFobj> objects, Font font, InputStream inputStream) throws Exception {
        OTF otf = new OTF(inputStream);
        setData(font, otf);
        font.program = new Subset.Program();
        if (otf.cff) {
            font.program.font = Arrays.copyOfRange(otf.buf, otf.cffOff, otf.cffOff + otf.cffLen);
            font.program.cff = true;
            font.program.forbidden = (otf.fsType & 0x0100) != 0;
        } else {
            font.program.font = otf.buf;
        }
        FontObjects.register(objects, font);
    }

    // Gives the font the name, the metrics and the character map of the
    // OpenType or TrueType font.
    private static void setData(Font font, OTF otf) {
        font.name = otf.fontName;
        font.firstChar = otf.firstChar;
        font.lastChar = otf.lastChar;
        font.unitsPerEm = otf.unitsPerEm;
        font.bBoxLLx = otf.bBoxLLx;
        font.bBoxLLy = otf.bBoxLLy;
        font.bBoxURx = otf.bBoxURx;
        font.bBoxURy = otf.bBoxURy;
        font.fontAscent = otf.ascent;
        font.fontDescent = otf.descent;
        font.fontLineGap = otf.lineGap;
        font.italicAngle = (int) otf.italicAngle;
        font.fontUnderlinePosition = otf.underlinePosition;
        font.fontUnderlineThickness = otf.underlineThickness;
        font.advanceWidth = otf.advanceWidth;
        font.unicodeToGID = otf.unicodeToGID;
        font.markToMarkOffsets = otf.markToMarkOffsets;
        font.markAnchors = otf.markAnchors;
        font.baseAnchors = otf.baseAnchors;
        font.info = otf.fontInfo;
        font.capHeight = otf.capHeight;
        font.cff = otf.cff;
        font.checksum = Font.checksumOf(font);
        font.setSize(font.size);
    }

    /**
     * Converts a value in font units to the glyph space units of the font
     * descriptor, 1/1000 em, rounded to the nearest integer with halves away
     * from zero. The integer arithmetic gives the same value in every port.
     */
    static int toGlyphSpace(int value, int unitsPerEm) {
        int rounded = (2000 * Math.abs(value) + unitsPerEm) / (2 * unitsPerEm);
        return (value < 0) ? -rounded : rounded;
    }

    /**
     * Returns the flags of the font descriptor: Nonsymbolic, 32, and Italic,
     * 64, for a font whose post table gives it an italic angle.
     */
    static int flagsOf(int italicAngle) {
        return (italicAngle != 0) ? 32 | 64 : 32;
    }

    /**
     * Returns the italic angle of the font descriptor: the 16.16 fixed number
     * of the post table in degrees, to two decimals with halves away from
     * zero and no trailing zeros. The integer arithmetic gives the same text
     * in every port.
     */
    static String italicAngleOf(int angle) {
        long rounded = (200L * Math.abs((long) angle) + 65536L) / (2L * 65536L);
        String text = String.valueOf(rounded / 100);
        if (rounded % 100 != 0) {
            // The two decimals, as 100 to 199 without the 1, and no trailing zero.
            text += "." + String.valueOf(100 + rounded % 100).substring(1).replaceAll("0$", "");
        }
        return (angle < 0 && rounded != 0) ? "-" + text : text;
    }

    private static String toHexString(int code) {
        String str = Integer.toHexString(code);
        if (str.length() == 1) {
            return "000" + str;
        } else if (str.length() == 2) {
            return "00" + str;
        } else if (str.length() == 3) {
            return "0" + str;
        }
        return str;
    }

    private static void writeListToBuffer(List<String> list, StringBuilder sb) {
        sb.append(list.size());
        sb.append(" beginbfchar\n");
        for (String str : list) {
            sb.append(str);
        }
        sb.append("endbfchar\n");
        list.clear();
    }
}   // End of OpenTypeFont.java
