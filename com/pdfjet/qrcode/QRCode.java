/*
 * QRCode.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * Original author: Kazuhiko Arase, 2009
 * URL: http://www.d-project.com/
 * Licensed under MIT: http://www.opensource.org/licenses/mit-license.php
 *
 * The word "QR Code" is a registered trademark of
 * DENSO WAVE INCORPORATED
 * http://www.denso-wave.com/qrcode/faqpatent-e.html
 *
 * Modified and adapted for use in PDFjet by PDFjet Software
 */
package com.pdfjet.qrcode;

import com.pdfjet.*;

import java.io.UnsupportedEncodingException;
import java.nio.charset.StandardCharsets;
import com.pdfjet.*;

/**
 * Used to create 2D QR Code barcodes. Please see Example_20.
 */
final public class QRCode implements Drawable {
    private static final int PAD0 = 0xEC;
    private static final int PAD1 = 0x11;
    private Boolean[][] modules;
    // The version of the symbol, from 4 to 40, and its size: 4 * typeNumber + 17 modules.
    private int typeNumber;
    private int moduleCount;
    private ErrorCorrectionLevel errorCorrectionLevel = ErrorCorrectionLevel.M;
    private float x;
    private float y;
    private final byte[] qrData;
    private float m1 = 2.0f;        // Module length
    private int color = Color.black;

    /**
     * Used to create 2D QR Code barcodes. The string is encoded in UTF-8, and
     * the symbol is the smallest that holds it, from version 4, 33 by 33 modules,
     * to version 40, 177 by 177 modules. At version 40 it holds up to 2,953 bytes
     * at level L, 2,331 at M, 1,663 at Q and 1,273 at H.
     *
     * @param str the string to encode.
     * @param errorCorrectionLevel the desired error correction level.
     * @throws UnsupportedEncodingException If an input or output exception occurred
     * @throws IllegalArgumentException If the string does not fit in a version 40 symbol.
     */
    public QRCode(String str, ErrorCorrectionLevel errorCorrectionLevel) throws UnsupportedEncodingException {
        this.qrData = str.getBytes(StandardCharsets.UTF_8);
        this.errorCorrectionLevel = errorCorrectionLevel;
        this.typeNumber = getTypeNumber(qrData.length, errorCorrectionLevel);
        this.moduleCount = 4 * typeNumber + 17;
        this.make(false, getBestMaskPattern());
    }

    // Returns the smallest version, from 4, whose data codewords hold the data.
    private static int getTypeNumber(int dataLength, ErrorCorrectionLevel errorCorrectionLevel) {
        for (int typeNumber = 4; typeNumber <= 40; typeNumber++) {
            if (getLengthInBits(dataLength, typeNumber) <= getDataCount(typeNumber, errorCorrectionLevel) * 8) {
                return typeNumber;
            }
        }
        int maxLength = (getDataCount(40, errorCorrectionLevel) * 8 - 4 - 16) / 8;
        throw new IllegalArgumentException("The data is too long for a QR code at level "
                + errorCorrectionLevel + ": " + dataLength + " bytes, at most " + maxLength + ".");
    }

    // The bits of the mode indicator, the character count and the data, in byte mode.
    private static int getLengthInBits(int dataLength, int typeNumber) {
        return 4 + getCharacterCountBits(typeNumber) + 8 * dataLength;
    }

    // The character count of byte mode has 8 bits up to version 9, and 16 bits after it.
    private static int getCharacterCountBits(int typeNumber) {
        return (typeNumber < 10) ? 8 : 16;
    }

    private static int getDataCount(int typeNumber, ErrorCorrectionLevel errorCorrectionLevel) {
        int dataCount = 0;
        for (RSBlock rsBlock : RSBlock.getRSBlocks(typeNumber, errorCorrectionLevel)) {
            dataCount += rsBlock.getDataCount();
        }
        return dataCount;
    }

    /**
     *  Sets the location where this barcode will be drawn on the page.
     *
     *  @param x the x coordinate of the top left corner of the barcode.
     *  @param y the y coordinate of the top left corner of the barcode.
     *  @return this QRCode object.
     */
    public QRCode setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     *  Sets the module length of this barcode.
     *  The default value is 2.0f
     *
     *  @param moduleLength the specified module length.
     *  @return this QRCode object.
     */
    public QRCode setModuleLength(float moduleLength) {
        this.m1 = moduleLength;
        return this;
    }

    /**
     * Sets the color of the QR code.
     *
     * @param color the color.
     * @return this QRCode object.
     */
    public QRCode setModuleColor(int color) {
        this.color = color;
        return this;
    }

    /**
     *  Draws this barcode on the specified page.
     *
     *  @param page the specified page.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        if (page != null) {
            // The modules carry no text, so they are decorative content.
            page.addArtifactBMC();
            page.setBrushColor(this.color);
            for (int row = 0; row < modules.length; row++) {
                for (int col = 0; col < modules.length; col++) {
                    if (isDark(row, col)) {
                        page.fillRect(x + col*m1, y + row*m1, m1, m1);
                    }
                }
            }
            page.addEMC();
        }
        float w = m1*modules.length;
        float h = m1*modules.length;
        return new float[] {x + w, y + h};
    }

    /**
     * Returns the modules of the QR code: true for dark and false for light modules.
     *
     * @return the modules.
     */
    public Boolean[][] getModules() {
        return modules;
    }

    /**
     *  Returns true if the module at the specified row and column is dark.
     *
     *  @param row the row.
     *  @param col the column.
     *  @return true if the module is dark.
     */
    protected boolean isDark(int row, int col) {
        if (modules[row][col] != null) {
            return modules[row][col];
        } else {
            return false;
        }
    }

    /**
     * Returns the number of modules in each row and column.
     *
     * @return the module count.
     */
    protected int getModuleCount() {
        return moduleCount;
    }

    /**
     * Returns the mask pattern with the lowest penalty score.
     *
     * @return the mask pattern.
     */
    protected int getBestMaskPattern() {
        int minLostPoint = 0;
        int pattern = 0;
        for (int i = 0; i < 8; i++) {
            make(true, i);
            int lostPoint = QRUtil.getLostPoint(this);
            if (i == 0 || minLostPoint > lostPoint) {
                minLostPoint = lostPoint;
                pattern = i;
            }
        }
        return pattern;
    }

    /**
     * Places the patterns and data in the modules using the specified mask pattern.
     *
     * @param test true when trying out a mask pattern.
     * @param maskPattern the mask pattern.
     */
    protected void make(boolean test, int maskPattern) {
        modules = new Boolean[moduleCount][moduleCount];
        setupPositionProbePattern(0, 0);
        setupPositionProbePattern(moduleCount - 7, 0);
        setupPositionProbePattern(0, moduleCount - 7);
        setupPositionAdjustPattern();
        setupTimingPattern();
        setupTypeInfo(test, maskPattern);
        if (typeNumber >= 7) {
            setupTypeNumber(test);
        }
        mapData(createData(errorCorrectionLevel), maskPattern);
    }

    private void mapData(byte[] data, int maskPattern) {
        int inc = -1;
        int row = moduleCount - 1;
        int bitIndex = 7;
        int byteIndex = 0;
        for (int col = moduleCount - 1; col > 0; col -= 2) {
            if (col == 6) col--;
            while (true) {
                for (int c = 0; c < 2; c++) {
                    if (modules[row][col - c] == null) {
                        boolean dark = false;
                        if (byteIndex < data.length) {
                            dark = (((data[byteIndex] >>> bitIndex) & 1) == 1);
                        }
                        boolean mask = QRUtil.getMask(maskPattern, row, col - c);
                        if (mask) {
                            dark = !dark;
                        }
                        modules[row][col - c] = dark;
                        bitIndex--;
                        if (bitIndex == -1) {
                            byteIndex++;
                            bitIndex = 7;
                        }
                    }
                }
                row += inc;
                if (row < 0 || moduleCount <= row) {
                    row -= inc;
                    inc = -inc;
                    break;
                }
            }
        }
    }

    private void setupPositionAdjustPattern() {
        int[] pos = QRUtil.getPatternPosition(typeNumber);
        for (int row : pos) {
            for (int col : pos) {
                if (modules[row][col] != null) {
                    continue;
                }
                for (int r = -2; r <= 2; r++) {
                    for (int c = -2; c <= 2; c++) {
                        modules[row + r][col + c] =
                                r == -2 || r == 2 || c == -2 || c == 2 || (r == 0 && c == 0);
                    }
                }
            }
        }
    }

    private void setupPositionProbePattern(int row, int col) {
        for (int r = -1; r <= 7; r++) {
            for (int c = -1; c <= 7; c++) {
                if (row + r <= -1 || moduleCount <= row + r
                        || col + c <= -1 || moduleCount <= col + c) {
                    continue;
                }

                modules[row + r][col + c] =
                        (0 <= r && r <= 6 && (c == 0 || c == 6)) ||
                        (0 <= c && c <= 6 && (r == 0 || r == 6)) ||
                        (2 <= r && r <= 4 && 2 <= c && c <= 4);
            }
        }
    }

    private void setupTimingPattern() {
        for (int r = 8; r < moduleCount - 8; r++) {
            if (modules[r][6] != null) {
                continue;
            }
            modules[r][6] = (r % 2 == 0);
        }
        for (int c = 8; c < moduleCount - 8; c++) {
            if (modules[6][c] != null) {
                continue;
            }
            modules[6][c] = (c % 2 == 0);
        }
    }

    // Versions 7 and up carry their version number twice, next to two finder patterns.
    private void setupTypeNumber(boolean test) {
        int bits = QRUtil.getBCHTypeNumber(typeNumber);
        for (int i = 0; i < 18; i++) {
            boolean mod = (!test && ((bits >> i) & 1) == 1);
            modules[i / 3][i % 3 + moduleCount - 8 - 3] = mod;
        }
        for (int i = 0; i < 18; i++) {
            boolean mod = (!test && ((bits >> i) & 1) == 1);
            modules[i % 3 + moduleCount - 8 - 3][i / 3] = mod;
        }
    }

    private void setupTypeInfo(boolean test, int maskPattern) {
        int data = (errorCorrectionLevel.value << 3) | maskPattern;
        int bits = QRUtil.getBCHTypeInfo(data);

        for (int i = 0; i < 15; i++) {
            Boolean mod = (!test && ((bits >> i) & 1) == 1);
            if (i < 6) {
                modules[i][8] = mod;
            } else if (i < 8) {
                modules[i + 1][8] = mod;
            } else {
                modules[moduleCount - 15 + i][8] = mod;
            }
        }

        for (int i = 0; i < 15; i++) {
            boolean mod = (!test && ((bits >> i) & 1) == 1);
            if (i < 8) {
                modules[8][moduleCount - i - 1] = mod;
            } else if (i < 9) {
                modules[8][15 - i - 1 + 1] = mod;
            } else {
                modules[8][15 - i - 1] = mod;
            }
        }

        modules[moduleCount - 8][8] = !test;
    }

    private byte[] createData(ErrorCorrectionLevel errorCorrectionLevel) {
        RSBlock[] rsBlocks = RSBlock.getRSBlocks(typeNumber, errorCorrectionLevel);

        BitBuffer buffer = new BitBuffer();
        buffer.put(4, 4);
        buffer.put(qrData.length, getCharacterCountBits(typeNumber));
        for (byte b : qrData) {
            buffer.put(b, 8);
        }

        int totalDataCount = 0;
        for (RSBlock rsBlock : rsBlocks) {
            totalDataCount += rsBlock.getDataCount();
        }

        if (buffer.getLengthInBits() > totalDataCount * 8) {
            throw new IllegalArgumentException("String length overflow. ("
                + buffer.getLengthInBits()
                + ">"
                +  totalDataCount * 8
                + ")");
        }

        if (buffer.getLengthInBits() + 4 <= totalDataCount * 8) {
            buffer.put(0, 4);
        }

        // padding
        while (buffer.getLengthInBits() % 8 != 0) {
            buffer.put(false);
        }

        // padding
        while (true) {
            if (buffer.getLengthInBits() >= totalDataCount * 8) {
                break;
            }
            buffer.put(PAD0, 8);

            if (buffer.getLengthInBits() >= totalDataCount * 8) {
                break;
            }
            buffer.put(PAD1, 8);
        }

        return createBytes(buffer, rsBlocks);
    }

    private byte[] createBytes(BitBuffer buffer, RSBlock[] rsBlocks) {
        int offset = 0;
        int maxDcCount = 0;
        int maxEcCount = 0;

        int[][] dcdata = new int[rsBlocks.length][];
        int[][] ecdata = new int[rsBlocks.length][];

        for (int r = 0; r < rsBlocks.length; r++) {
            int dcCount = rsBlocks[r].getDataCount();
            int ecCount = rsBlocks[r].getTotalCount() - dcCount;

            maxDcCount = Math.max(maxDcCount, dcCount);
            maxEcCount = Math.max(maxEcCount, ecCount);

            dcdata[r] = new int[dcCount];
            for (int i = 0; i < dcdata[r].length; i++) {
                dcdata[r][i] = 0xff & buffer.getBuffer()[i + offset];
            }
            offset += dcCount;

            Polynomial rsPoly = QRUtil.getErrorCorrectPolynomial(ecCount);
            Polynomial rawPoly = new Polynomial(dcdata[r], rsPoly.getLength() - 1);

            Polynomial modPoly = rawPoly.mod(rsPoly);
            ecdata[r] = new int[rsPoly.getLength() - 1];
            for (int i = 0; i < ecdata[r].length; i++) {
                int modIndex = i + modPoly.getLength() - ecdata[r].length;
                ecdata[r][i] = (modIndex >= 0) ? modPoly.get(modIndex) : 0;
            }
        }

        int totalCodeCount = 0;
        for (RSBlock rsBlock : rsBlocks) {
            totalCodeCount += rsBlock.getTotalCount();
        }

        byte[] data = new byte[totalCodeCount];
        int index = 0;
        for (int i = 0; i < maxDcCount; i++) {
            for (int r = 0; r < rsBlocks.length; r++) {
                if (i < dcdata[r].length) {
                    data[index++] = (byte) dcdata[r][i];
                }
            }
        }

        for (int i = 0; i < maxEcCount; i++) {
            for (int r = 0; r < rsBlocks.length; r++) {
                if (i < ecdata[r].length) {
                    data[index++] = (byte) ecdata[r][i];
                }
            }
        }

        return data;
    }
}
