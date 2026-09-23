/*
 * Barcode.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.barcodes;

import com.pdfjet.*;
import java.util.*;

/**
 * Used to create one dimensional barcodes - EAN-13, UPC-A, Code 39 and Code 128.
 *
 * Please see Example_11.
 */
public class Barcode implements Drawable {
    /** Specifies EAN13 barcode */
    public static final int EAN_13 = 0;
    /** Specifies UPC barcode */
    public static final int UPC_A = 1;
    /** Specifies CODE128 barcode */
    public static final int CODE_128 = 2;
    /** Specifies CODE39 barcode */
    public static final int CODE_39 = 3;
    /**
     * Specifies GS1-128 barcode. Its text is GS1 data written as people read
     * it, each Application Identifier in parentheses and its data after it,
     * such as "(01)09506000134352(10)ABC123", and is drawn so under its bars.
     */
    public static final int GS1_128 = 4;

    private int barcodeType = 0;
    private String text = null;
    private float x1 = 0.0f;
    private float y1 = 0.0f;
    private float m1 = 0.75f;   // Module length
    private float barHeightFactor = 50.0f;
    private Direction direction = Direction.LEFT_TO_RIGHT;
    private Font font = null;

    private String[] lCode = {
        "3211","2221","2122","1411","1132",
        "1231","1114","1312","1213","3112"};
    private String[] gCode = new String[10];
    private String[] lgMap = {
        "LLLLLL", "LLGLGG", "LLGGLG", "LLGGGL", "LGLLGG",
        "LGGLLG", "LGGGLL", "LGLGLG", "LGLGGL", "LGGLGL"};

    private Map<Character, String> tableB = new HashMap<Character, String>();

    /**
     * The constructor.
     *
     * @param barcodeType the type of the barcode.
     * @param text the content text of the barcode.
     * @throws Exception if the text is not valid for the barcode type.
     */
    public Barcode(int barcodeType, String text) throws Exception {
        this.barcodeType = barcodeType;
        this.text = text;

        if (barcodeType == Barcode.UPC_A && (text.length() != 11 || !hasOnlyDigits(text))) {
            throw new Exception("UPC-A barcodes must have exactly 11 digits!");
        } else if (barcodeType == Barcode.EAN_13 && (text.length() != 12 || !hasOnlyDigits(text))) {
            throw new Exception("EAN-13 barcodes must have exactly 12 digits!");
        } else if (barcodeType == Barcode.CODE_128) {
            code128Codewords(text);     // Throws for a text that a barcode cannot hold
        } else if (barcodeType == Barcode.GS1_128) {
            gs1128Codewords(text);      // Throws for data that is not GS1, or too long
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 10; i++) {
            gCode[i] = sb.append(lCode[i]).reverse().toString();
            sb.setLength(0);
        }

        tableB.put('*', "bWbwBwBwb");
        tableB.put('-', "bWbwbwBwB");
        tableB.put('$', "bWbWbWbwb");
        tableB.put('%', "bwbWbWbWb");
        tableB.put(' ', "bWBwbwBwb");
        tableB.put('.', "BWbwbwBwb");
        tableB.put('/', "bWbWbwbWb");
        tableB.put('+', "bWbwbWbWb");
        tableB.put('0', "bwbWBwBwb");
        tableB.put('1', "BwbWbwbwB");
        tableB.put('2', "bwBWbwbwB");
        tableB.put('3', "BwBWbwbwb");
        tableB.put('4', "bwbWBwbwB");
        tableB.put('5', "BwbWBwbwb");
        tableB.put('6', "bwBWBwbwb");
        tableB.put('7', "bwbWbwBwB");
        tableB.put('8', "BwbWbwBwb");
        tableB.put('9', "bwBWbwBwb");
        tableB.put('A', "BwbwbWbwB");
        tableB.put('B', "bwBwbWbwB");
        tableB.put('C', "BwBwbWbwb");
        tableB.put('D', "bwbwBWbwB");
        tableB.put('E', "BwbwBWbwb");
        tableB.put('F', "bwBwBWbwb");
        tableB.put('G', "bwbwbWBwB");
        tableB.put('H', "BwbwbWBwb");
        tableB.put('I', "bwBwbWBwb");
        tableB.put('J', "bwbwBWBwb");
        tableB.put('K', "BwbwbwbWB");
        tableB.put('L', "bwBwbwbWB");
        tableB.put('M', "BwBwbwbWb");
        tableB.put('N', "bwbwBwbWB");
        tableB.put('O', "BwbwBwbWb");
        tableB.put('P', "bwBwBwbWb");
        tableB.put('Q', "bwbwbwBWB");
        tableB.put('R', "BwbwbwBWb");
        tableB.put('S', "bwBwbwBWb");
        tableB.put('T', "bwbwBwBWb");
        tableB.put('U', "BWbwbwbwB");
        tableB.put('V', "bWBwbwbwB");
        tableB.put('W', "BWBwbwbwb");
        tableB.put('X', "bWbwBwbwB");
        tableB.put('Y', "BWbwBwbwb");
        tableB.put('Z', "bWBwBwbwb");
    }

    /**
     * Sets the location where this barcode will be drawn on the page.
     *
     * @param x1 the x coordinate of the top left corner of the barcode.
     * @param y1 the y coordinate of the top left corner of the barcode.
     * @return this Barcode object.
     */
    public Barcode setLocation(float x1, float y1) {
        this.x1 = x1;
        this.y1 = y1;
        return this;
    }

    /**
     * Sets the module length of this barcode.
     * The default value is 0.75
     *
     * @param moduleLength the specified module length.
     * @return this Barcode object.
     */
    public Barcode setModuleLength(float moduleLength) {
        this.m1 = moduleLength;
        return this;
    }

    /**
     * Sets the bar height factor.
     * The height of the bars is the moduleLength * barHeightFactor
     * The default value is 50.0f
     *
     * @param barHeightFactor the specified bar height factor.
     * @return this Barcode object.
     */
    public Barcode setBarHeightFactor(float barHeightFactor) {
        this.barHeightFactor = barHeightFactor;
        return this;
    }

    /**
     * Sets the direction in which this barcode is drawn.
     *
     * @param direction the specified direction.
     * @return this Barcode object.
     */
    public Barcode setDirection(Direction direction) {
        this.direction = direction;
        return this;
    }

    /**
     * Sets the font to be used with this barcode.
     *
     * @param font the specified font.
     * @return this Barcode object.
     */
    public Barcode setFont(Font font) {
        this.font = font;
        return this;
    }

    private static boolean hasOnlyDigits(String text) {
        for (int i = 0; i < text.length(); i++) {
            char ch = text.charAt(i);
            if (ch < '0' || ch > '9') {
                return false;
            }
        }
        return true;
    }

    /**
     * Draws this barcode on the specified page.
     *
     * @param page the specified page.
     * @return x and y coordinates of the bottom right corner of this component.
     * @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        if (page != null) {
            // The bars are black whatever pen color the page was left with
            page.saveGraphicsState();
            page.setPenColor(Color.black);
        }
        try {
            if (barcodeType == Barcode.EAN_13) {
                return drawCodeEAN13(page, x1, y1);
            } else if (barcodeType == Barcode.UPC_A) {
                return drawCodeUPC(page, x1, y1);
            } else if (barcodeType == Barcode.CODE_128 || barcodeType == Barcode.GS1_128) {
                return drawCode128(page, x1, y1);
            } else if (barcodeType == Barcode.CODE_39) {
                return drawCode39(page, x1, y1);
            } else {
                throw new Exception("Unsupported Barcode Type.");
            }
        } finally {
            if (page != null) {
                page.restoreGraphicsState();
            }
        }
    }

    private float[] drawCodeUPC(Page page, float x1, float y1) throws Exception {
        float x = x1;
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        int sum = 0;
        for (int i = 0; i < 11; i += 2) {   // even digits
            sum += (text.charAt(i) - '0') * 3;
        }
        for (int i = 1; i < 11; i += 2) {   // odd digits
            sum += (text.charAt(i) - '0');
        }
        int checkDigit = 0;
        int remainder = sum % 10;
        if (remainder > 0) {
            checkDigit = (10 - remainder);
        }
        // Use a local variable instead of mutating the text field - drawOn()
        // must be safe to call more than once on the same Barcode instance
        // (e.g. drawing the same barcode on several pages).
        String fullText = text + Integer.toString(checkDigit);
        Bars bars = new Bars(x1, y1, 95f * m1, h + 8f, direction);  // 95 modules

        x = drawEGuard(page, bars, x, h + 8);
        float xGroup1Start = x;
        for (int i = 0; i < 6; i++) {
            int digit = fullText.charAt(i) - '0';
            String str = lCode[digit];
            for (int j = 0; j < 4; j++) {
                int n = str.charAt(j) - '0';
                if (j%2 != 0) {
                    drawBar(page, bars, x, n*m1, h);
                }
                x += n*m1;
            }
            if (i == 0) {
                xGroup1Start = x;   // Start of the 2nd-6th digit bars (digit 0 is drawn outside)
            }
        }
        float xLeftGroupEnd = x;
        x = drawMGuard(page, bars, x, h + 8);
        float xRightGroupStart = x;
        float xGroup2End = 0f;
        for (int i = 6; i < 12; i++) {
            if (i == 11) {
                xGroup2End = x;     // End of the 7th-11th digit bars (digit 11 is drawn outside)
            }
            int digit = fullText.charAt(i) - '0';
            String str = lCode[digit];
            for (int j = 0; j < 4; j++) {
                int n = str.charAt(j) - '0';
                if (j%2 == 0) {
                    drawBar(page, bars, x, n*m1, h);
                }
                x += n*m1;
            }
        }
        x = drawEGuard(page, bars, x, h + 8);

        float left = x1;
        float right = x;
        float bottom = y1 + h + 8;
        if (font != null) {
            // Standard UPC-A layout: the leading (number system) digit and
            // the trailing check digit are printed in the quiet zones
            // outside the guard bars, not centered under them together with
            // the rest of the label. The two groups of 5 digits are each
            // centered under their own bar section.
            String firstDigit = String.valueOf(fullText.charAt(0));
            String group1 = fullText.substring(1, 6);
            String group2 = fullText.substring(6, 11);
            String lastDigit = String.valueOf(fullText.charAt(11));

            float fontSize = font.getSize();
            font.setSize(10f);
            float yText = y1 + h + font.getBodyHeight();
            float gap = font.stringWidth(" ");

            left = x1 - gap - font.stringWidth(firstDigit);
            drawText(page, bars, firstDigit, left, yText);
            drawText(page, bars, group1,
                    xGroup1Start + ((xLeftGroupEnd - xGroup1Start) - font.stringWidth(group1))/2,
                    yText);
            drawText(page, bars, group2,
                    xRightGroupStart + ((xGroup2End - xRightGroupStart) - font.stringWidth(group2))/2,
                    yText);
            float[] xy = drawText(page, bars, lastDigit, x + gap, yText);
            right = xy[0];
            bottom = Math.max(bottom, xy[1]);

            font.setSize(fontSize);
        }

        return bars.getBottomRight(left, right, bottom);
    }

    private float drawEGuard(Page page, Bars bars, float x, float h) {
        if (page != null) {
            // 101
            page.addArtifactBMC();
            strokeBar(page, bars, x + (0.5f * m1), m1, h);
            strokeBar(page, bars, x + (2.5f * m1), m1, h);
            page.addEMC();
        }
        return (x + (3.0f * m1));
    }

    private float drawMGuard(Page page, Bars bars, float x, float h) {
        if (page != null) {
            // 01010
            page.addArtifactBMC();
            strokeBar(page, bars, x + (1.5f * m1), m1, h);
            strokeBar(page, bars, x + (3.5f * m1), m1, h);
            page.addEMC();
        }
        return (x + (5.0f * m1));
    }

    // Draws the bar of width w and height h that starts at x.
    private void drawBar(Page page, Bars bars, float x, float w, float h) {
        if (page != null) {
            page.addArtifactBMC();
            strokeBar(page, bars, x + w / 2, w, h);
            page.addEMC();
        }
    }

    // Strokes the bar of width w and height h centered on x.
    private void strokeBar(Page page, Bars bars, float x, float w, float h) {
        if (page != null) {
            float[] top = bars.turn(x, bars.y1);
            float[] bottom = bars.turn(x, bars.y1 + h);
            page.setPenWidth(w);
            page.moveTo(top[0], top[1]);
            page.lineTo(bottom[0], bottom[1]);
            page.strokePath();
        }
    }

    // Draws the text with its baseline starting at (x, y). Returns the end of
    // the baseline and the bottom of the text, before the turn.
    private float[] drawText(Page page, Bars bars, String str, float x, float y) throws Exception {
        TextLine textLine = new TextLine(font, str);
        float[] xy = bars.turn(x, y);
        textLine.setLocation(xy[0], xy[1]);
        if (direction == Direction.TOP_TO_BOTTOM) {
            textLine.setTextRotation(-270);
        } else if (direction == Direction.BOTTOM_TO_TOP) {
            textLine.setTextRotation(-90);
        }
        textLine.drawOn(page);
        return new float[] {x + font.stringWidth(str), y + font.getDescent()};
    }

    // Returns the codewords of the text in code set B: one for a character from
    // 32 to 127, and two, SHIFT or FNC 4 and the character, for one below 32 or
    // from 128 to 255. It throws for a character above 255, which code set B
    // cannot hold, and for a text of more than 48 codewords, the most a barcode
    // holds, rather than draw a barcode of another text.
    private static List<Integer> code128Codewords(String text) throws Exception {
        List<Integer> list = new ArrayList<Integer>();
        for (int i = 0; i < text.length(); i++) {
            char symchar = text.charAt(i);
            if (symchar < 32) {
                list.add(Code128Table.SHIFT);
                list.add(symchar + 64);
            } else if (symchar < 128) {
                list.add(symchar - 32);
            } else if (symchar < 256) {
                list.add(Code128Table.FNC_4);
                list.add(symchar - 160);    // 128 + 32
            } else {
                throw new Exception("Code 128 barcodes can only hold characters up to U+00FF!");
            }
        }
        if (list.size() > 48) {
            throw new Exception(
                    "Code 128 barcodes hold at most 48 codewords, and a character below 32 or from 128 to 255 takes two!");
        }
        return list;
    }

    // Returns the start and the codewords of the GS1 data written as people
    // read it: FNC1, then the fields, each followed by FNC1 when it is of no
    // set length and another follows. Runs of four digits or more are in code
    // set C, two digits to a codeword, and the rest in code set B. It throws
    // for data that is not GS1, see GS1.parse, or longer than the 48
    // characters a GS1-128 barcode holds, not counting the separators.
    private static List<Integer> gs1128Codewords(String text) {
        final int separator = -1;
        List<Integer> items = new ArrayList<Integer>();
        int characters = 0;
        for (GS1.Field field : GS1.parse(text)) {
            String s = field.ai + field.data;
            for (int i = 0; i < s.length(); i++) {
                items.add((int) s.charAt(i));
            }
            characters += s.length();
            if (field.separator) {
                items.add(separator);
            }
        }
        if (characters > 48) {
            throw new IllegalArgumentException(
                    "GS1-128 barcodes hold at most 48 characters, not counting the separators!");
        }

        boolean inC = digits(items, 0) >= 4;
        List<Integer> list = new ArrayList<Integer>();
        list.add(inC ? Code128Table.START_C : Code128Table.START_B);
        list.add(Code128Table.FNC_1);
        int i = 0;
        while (i < items.size()) {
            int c = items.get(i);
            if (c == separator) {
                list.add(Code128Table.FNC_1);
                i++;
            } else if (inC && digits(items, i) >= 2) {
                list.add(10*(c - '0') + (items.get(i + 1) - '0'));
                i += 2;
            } else if (inC) {
                list.add(Code128Table.CODE_B);
                inC = false;
            } else if (digits(items, i) >= 4 && digits(items, i) % 2 == 0) {
                list.add(Code128Table.CODE_C);
                inC = true;
            } else {
                list.add(c - 32);
                i++;
            }
        }
        return list;
    }

    // Returns how many digits there are in a row from i.
    private static int digits(List<Integer> items, int i) {
        int n = 0;
        while (i + n < items.size() && items.get(i + n) >= '0' && items.get(i + n) <= '9') {
            n++;
        }
        return n;
    }

    private float[] drawCode128(Page page, float x1, float y1) throws Exception {
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        int start = Code128Table.START_B;
        List<Integer> list;
        if (barcodeType == Barcode.GS1_128) {
            list = gs1128Codewords(text);
            start = list.remove(0);
        } else {
            list = code128Codewords(text);
        }

        StringBuilder buf = new StringBuilder();
        int checkDigit = start;
        buf.append((char) checkDigit);
        for (int i = 0; i < list.size(); i++) {
            int codeword = list.get(i);
            buf.append((char) codeword);
            checkDigit += codeword * (i + 1);
        }
        checkDigit %= Code128Table.START_A;
        buf.append((char) checkDigit);
        buf.append((char) Code128Table.STOP);

        float length = 0f;
        for (int i = 0; i < buf.length(); i++) {
            String symbol = Integer.toString(Code128Table.TABLE[buf.charAt(i)]);
            for (int j = 0; j < symbol.length(); j++) {
                length += (symbol.charAt(j) - 0x30) * m1;
            }
        }

        Bars bars = new Bars(x1, y1, length, h, direction);
        float x = x1;
        for (int i = 0; i < buf.length(); i++) {
            int si = buf.charAt(i);
            String symbol = Integer.toString(Code128Table.TABLE[si]);
            for (int j = 0; j < symbol.length(); j++) {
                int n = symbol.charAt(j) - 0x30;
                if (j%2 == 0) {
                    drawBar(page, bars, x, n * m1, h);
                }
                x += n * m1;
            }
        }

        float right = x;
        float bottom = y1 + h;
        if (font != null) {
            float[] xy = drawText(page, bars, text,
                    x1 + ((x - x1) - font.stringWidth(text))/2,
                    y1 + h + font.getBodyHeight());
            right = Math.max(right, xy[0]);
            bottom = xy[1];
        }

        return bars.getBottomRight(x1, right, bottom);
    }

    private float[] drawCode39(Page page, float x1, float y1) throws Exception {
        // Use a local variable instead of mutating the text field - drawOn()
        // must be safe to call more than once on the same Barcode instance
        // (e.g. drawing the same barcode on several pages).
        String fullText = "*" + text + "*";
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        float length = 0f;
        for (int i = 0; i < fullText.length(); i++) {
            String code = tableB.get(fullText.charAt(i));
            if (code == null) {
                throw new Exception("The input string '" + fullText +
                        "' contains characters that are invalid in a Code39 barcode.");
            }
            for (int j = 0; j < 9; j++) {
                char ch = code.charAt(j);
                length += (ch == 'W' || ch == 'B') ? 3 * m1 : m1;
            }
            length += m1;
        }
        length -= m1;   // There is no gap after the last character

        Bars bars = new Bars(x1, y1, length, h, direction);
        float x = x1;
        for (int i = 0; i < fullText.length(); i++) {
            String code = tableB.get(fullText.charAt(i));
            for (int j = 0; j < 9; j++) {
                char ch = code.charAt(j);
                if (ch == 'w') {
                    x += m1;
                } else if (ch == 'W') {
                    x += m1 * 3;
                } else if (ch == 'b') {
                    drawBar(page, bars, x, m1, h);
                    x += m1;
                } else if (ch == 'B') {
                    drawBar(page, bars, x, m1 * 3, h);
                    x += m1 * 3;
                }
            }
            x += m1;
        }

        float right = x1 + length;
        float bottom = y1 + h;
        if (font != null) {
            float[] xy = drawText(page, bars, fullText,
                    x1 + (length - font.stringWidth(fullText))/2,
                    y1 + h + font.getBodyHeight());
            right = Math.max(right, xy[0]);
            bottom = xy[1];
        }

        return bars.getBottomRight(x1, right, bottom);
    }

    private float[] drawCodeEAN13(Page page, float x1, float y1) throws Exception {
        float x = x1;
        float h = m1 * barHeightFactor; // Barcode height when drawn horizontally

        int sum = 0;
        for (int i = 0; i < 12; i += 2) {
            sum += (text.charAt(i) - 0x30);
        }
        for (int i = 1; i < 12; i += 2) {
            sum += (text.charAt(i) - 0x30) * 3;
        }
        int checkDigit = 0;
        int remainder = sum % 10;
        if (remainder > 0) {
            checkDigit = (10 - remainder);
        }
        // Use a local variable instead of mutating the text field - drawOn()
        // must be safe to call more than once on the same Barcode instance
        // (e.g. drawing the same barcode on several pages).
        String fullText = text + Integer.toString(checkDigit);
        Bars bars = new Bars(x1, y1, 95f * m1, h + 8f, direction);  // 95 modules

        x = drawEGuard(page, bars, x, h + 8);
        float xLeftGroupStart = x;
        String group1 = lgMap[fullText.charAt(0) - '0'];
        for (int i = 1; i < 7; i++) {
            int digit = fullText.charAt(i) - '0';
            String str = gCode[digit];
            if (group1.charAt(i - 1) == 'L') {
                str = lCode[digit];
            }
            int n = str.charAt(0) - '0';
            x += n*m1;
            n = str.charAt(1) - '0';
            drawBar(page, bars, x, n*m1, h);
            x += n*m1;
            n = str.charAt(2) - '0';
            x += n*m1;
            n = str.charAt(3) - '0';
            drawBar(page, bars, x, n*m1, h);
            x += n*m1;
        }
        float xLeftGroupEnd = x;
        x = drawMGuard(page, bars, x, h + 8);
        float xRightGroupStart = x;
        for (int i = 7; i < 13; i++) {
            int digit = fullText.charAt(i) - '0';
            String str = lCode[digit];
            int n = str.charAt(0) - '0';
            drawBar(page, bars, x, n*m1, h);
            x += n*m1;
            n = str.charAt(1) - '0';
            x += n*m1;
            n = str.charAt(2) - '0';
            drawBar(page, bars, x, n*m1, h);
            x += n*m1;
            n = str.charAt(3) - '0';
            x += n*m1;
        }
        float xRightGroupEnd = x;
        x = drawEGuard(page, bars, x, h + 8);

        float left = x1;
        float right = x;
        float bottom = y1 + h + 8;

        if (font != null) {
            // Standard EAN-13 layout: the leading (number system) digit sits
            // in the quiet zone to the left of the start guard bars, not
            // centered under them together with the rest of the label. The
            // two groups of 6 digits are each centered under their own bar
            // section (left group / right group), not under the barcode as
            // a whole.
            String firstDigit = String.valueOf(fullText.charAt(0));
            String leftGroup = fullText.substring(1, 7);
            String rightGroup = fullText.substring(7, 13);

            float fontSize = font.getSize();
            font.setSize(10f);
            float yText = y1 + h + font.getBodyHeight();
            float gap = font.stringWidth(" ");

            left = x1 - gap - font.stringWidth(firstDigit);
            drawText(page, bars, firstDigit, left, yText);
            drawText(page, bars, leftGroup,
                    xLeftGroupStart + ((xLeftGroupEnd - xLeftGroupStart) - font.stringWidth(leftGroup))/2,
                    yText);
            float[] xy = drawText(page, bars, rightGroup,
                    xRightGroupStart + ((xRightGroupEnd - xRightGroupStart) - font.stringWidth(rightGroup))/2,
                    yText);
            right = Math.max(right, xy[0]);
            bottom = Math.max(bottom, xy[1]);

            font.setSize(fontSize);
        }

        return bars.getBottomRight(left, right, bottom);
    }

    // The bars of a barcode, length long and height high, drawn left to right from
    // (x1, y1) and turned to the direction of the barcode: top to bottom is a quarter
    // turn clockwise and bottom to top a quarter turn counter-clockwise, and the bars
    // stay right of x1 and below y1. The draw methods take the coordinates of the
    // barcode drawn left to right.
    private static final class Bars {
        final float x1;
        final float y1;
        final float length;
        final float height;
        final Direction direction;

        Bars(float x1, float y1, float length, float height, Direction direction) {
            this.x1 = x1;
            this.y1 = y1;
            this.length = length;
            this.height = height;
            this.direction = direction;
        }

        // Returns the point (x, y) turned to the direction of the barcode.
        float[] turn(float x, float y) {
            if (direction == Direction.TOP_TO_BOTTOM) {
                return new float[] {x1 + height - (y - y1), y1 + (x - x1)};
            } else if (direction == Direction.BOTTOM_TO_TOP) {
                return new float[] {x1 + (y - y1), y1 + length - (x - x1)};
            }
            return new float[] {x, y};
        }

        // Returns the bottom right corner, turned to the direction of the barcode, of
        // a barcode that spans from left to right and from y1 to bottom.
        float[] getBottomRight(float left, float right, float bottom) {
            if (direction == Direction.TOP_TO_BOTTOM) {
                return new float[] {x1 + height, y1 + (right - x1)};
            } else if (direction == Direction.BOTTOM_TO_TOP) {
                return new float[] {x1 + (bottom - y1), y1 + length + (x1 - left)};
            }
            return new float[] {right, bottom};
        }
    }

    /**
     * Returns the height of this barcode as it is drawn: from its location to
     * the bottom of the bars, or of the text under them, in the direction the
     * barcode is drawn, so a barcode drawn top to bottom or bottom to top is as
     * tall as it is long.
     *
     * @return the height of this barcode.
     */
    public float getHeight() {
        try {
            return drawOn(null)[1] - y1;
        } catch (RuntimeException e) {
            throw e;
        } catch (Exception e) {     // Code 39 text with a character the code cannot encode
            throw new IllegalArgumentException(e.getMessage(), e);
        }
    }
}   // End of Barcode.java
