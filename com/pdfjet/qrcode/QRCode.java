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
    private boolean[][] modules;    // The dark modules
    private boolean[][] reserved;   // The modules of the function patterns and the format information
    // The version of the symbol, from 4 to 40, and its size: 4 * typeNumber + 17 modules.
    private int typeNumber;
    private int moduleCount;
    private ErrorCorrectionLevel errorCorrectionLevel = ErrorCorrectionLevel.M;
    private float x;
    private float y;
    private final byte[] qrData;
    private boolean eci;            // The data is not all ASCII, and starts with the ECI of UTF-8
    private float m1 = 2.0f;        // Module length
    private int color = Color.black;
    private String altDescription;

    // The bits of the ECI that says the bytes are UTF-8: the mode indicator
    // 0111 and the ECI assignment number 26 in 8 bits.
    private static final int ECI_BITS = 12;

    /**
     * Used to create 2D QR Code barcodes. The string is encoded in UTF-8, and
     * the symbol is the smallest that holds it, from version 4, 33 by 33 modules,
     * to version 40, 177 by 177 modules. At version 40 it holds up to 2,953 bytes
     * at level L, 2,331 at M, 1,663 at Q and 1,273 at H, a byte fewer when the
     * string is not all ASCII: it then starts with the ECI that tells the reader
     * the bytes are UTF-8.
     *
     * @param str the string to encode.
     * @param errorCorrectionLevel the desired error correction level.
     * @throws UnsupportedEncodingException If an input or output exception occurred
     * @throws IllegalArgumentException If the string does not fit in a version 40 symbol.
     */
    public QRCode(String str, ErrorCorrectionLevel errorCorrectionLevel) throws UnsupportedEncodingException {
        this.qrData = str.getBytes(StandardCharsets.UTF_8);
        for (byte b : qrData) {
            if (b < 0) {
                eci = true;
            }
        }
        this.errorCorrectionLevel = errorCorrectionLevel;
        this.typeNumber = getTypeNumber(qrData.length, eci, errorCorrectionLevel);
        this.moduleCount = 4 * typeNumber + 17;
        this.make(createData(errorCorrectionLevel));
    }

    // Returns the smallest version, from 4, whose data codewords hold the
    // data, and the ECI before it when there is one.
    private static int getTypeNumber(int dataLength, boolean eci, ErrorCorrectionLevel errorCorrectionLevel) {
        for (int typeNumber = 4; typeNumber <= 40; typeNumber++) {
            if (getLengthInBits(dataLength, eci, typeNumber) <= getDataCount(typeNumber, errorCorrectionLevel) * 8) {
                return typeNumber;
            }
        }
        int maxLength = (getDataCount(40, errorCorrectionLevel) * 8 - getLengthInBits(0, eci, 40)) / 8;
        throw new IllegalArgumentException("The data is too long for a QR code at level "
                + errorCorrectionLevel + ": " + dataLength + " bytes, at most " + maxLength + ".");
    }

    // The bits of the ECI, if any, and of the mode indicator, the character
    // count and the data, in byte mode.
    private static int getLengthInBits(int dataLength, boolean eci, int typeNumber) {
        int bits = 4 + getCharacterCountBits(typeNumber) + 8 * dataLength;
        if (eci) {
            bits += ECI_BITS;
        }
        return bits;
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
     * Sets what the QR code says, such as the web address it carries, for a
     * screen reader: a tagged document, PDF/UA or a PDF/A of level A, then has
     * the QR code as a figure of that description. Without one, it is
     * decoration, which a screen reader skips.
     *
     * @param altDescription the description.
     * @return this QRCode object.
     */
    public QRCode setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     *  Draws this barcode on the specified page. The dark modules next to each
     *  other in a row are filled as one rectangle, and the pen and the brush of
     *  the page are as they were after it.
     *
     *  @param page the specified page.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        float size = m1*moduleCount;
        if (page != null) {
            // Described, the QR code is a figure of a tagged document; not
            // described, its modules, which carry no text, are decoration.
            if (altDescription != null && !altDescription.isEmpty()) {
                page.addBDC(StructElem.FIGURE, null, null, altDescription);
            } else {
                page.addArtifactBMC();
            }
            page.saveGraphicsState();
            page.setBrushColor(this.color);
            for (int row = 0; row < moduleCount; row++) {
                int col = 0;
                while (col < moduleCount) {
                    if (!modules[row][col]) {
                        col++;
                        continue;
                    }
                    int start = col;
                    while (col < moduleCount && modules[row][col]) {
                        col++;
                    }
                    page.fillRect(x + start*m1, y + row*m1, (col - start)*m1, m1);
                }
            }
            page.restoreGraphicsState();
            if (altDescription != null && !altDescription.isEmpty()) {
                page.setFigureBoundingBox(x, y, size, size);
            }
            page.addEMC();
        }
        return new float[] {x + size, y + size};
    }

    /**
     * Returns the modules of the QR code: true for dark and false for light modules.
     *
     * @return the modules.
     */
    public Boolean[][] getModules() {
        Boolean[][] result = new Boolean[moduleCount][moduleCount];
        for (int row = 0; row < moduleCount; row++) {
            for (int col = 0; col < moduleCount; col++) {
                result[row][col] = modules[row][col];
            }
        }
        return result;
    }

    // Places the function patterns and the codewords, and masks them with the
    // mask pattern of the lowest penalty, with its format information.
    void make(byte[] data) {
        modules = new boolean[moduleCount][moduleCount];
        reserved = new boolean[moduleCount][moduleCount];
        setupPositionProbePattern(0, 0);
        setupPositionProbePattern(moduleCount - 7, 0);
        setupPositionProbePattern(0, moduleCount - 7);
        setupPositionAdjustPattern();
        setupTimingPattern();
        setupTypeInfo(modules, 0);  // Reserves the modules of the format information
        if (typeNumber >= 7) {
            setupTypeNumber();
        }
        mapData(data);

        // Each mask is tried with its format information in place, as ISO/IEC
        // 18004 asks, and the first of the lowest penalty is taken.
        boolean[][] best = null;
        int minLostPoint = 0;
        for (int maskPattern = 0; maskPattern < 8; maskPattern++) {
            boolean[][] masked = applyMask(maskPattern);
            int lostPoint = QRUtil.getLostPoint(masked);
            if (best == null || lostPoint < minLostPoint) {
                minLostPoint = lostPoint;
                best = masked;
            }
        }
        modules = best;
        reserved = null;
    }

    // Sets the module of a function pattern or of the format or version information.
    private void set(int row, int col, boolean dark) {
        modules[row][col] = dark;
        reserved[row][col] = true;
    }

    // Returns the modules with the mask pattern applied to the codewords, and
    // the format information of the mask pattern.
    boolean[][] applyMask(int maskPattern) {
        boolean[][] masked = new boolean[moduleCount][moduleCount];
        for (int row = 0; row < moduleCount; row++) {
            for (int col = 0; col < moduleCount; col++) {
                boolean dark = modules[row][col];
                if (!reserved[row][col] && QRUtil.getMask(maskPattern, row, col)) {
                    dark = !dark;
                }
                masked[row][col] = dark;
            }
        }
        setupTypeInfo(masked, maskPattern);
        return masked;
    }

    // Places the bits of the codewords in the modules that are not reserved,
    // in two module wide columns from the bottom right corner, up and down in
    // turn; the modules left over are light.
    void mapData(byte[] data) {
        int inc = -1;
        int row = moduleCount - 1;
        int bitIndex = 7;
        int byteIndex = 0;
        for (int col = moduleCount - 1; col > 0; col -= 2) {
            if (col == 6) col--;
            while (true) {
                for (int c = 0; c < 2; c++) {
                    if (!reserved[row][col - c]) {
                        boolean dark = false;
                        if (byteIndex < data.length) {
                            dark = (((data[byteIndex] >>> bitIndex) & 1) == 1);
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

    void setupPositionAdjustPattern() {
        int[] pos = QRUtil.getPatternPosition(typeNumber);
        for (int row : pos) {
            for (int col : pos) {
                if (reserved[row][col]) {
                    continue;
                }
                for (int r = -2; r <= 2; r++) {
                    for (int c = -2; c <= 2; c++) {
                        set(row + r, col + c, r == -2 || r == 2 || c == -2 || c == 2 || (r == 0 && c == 0));
                    }
                }
            }
        }
    }

    void setupPositionProbePattern(int row, int col) {
        for (int r = -1; r <= 7; r++) {
            for (int c = -1; c <= 7; c++) {
                if (row + r <= -1 || moduleCount <= row + r
                        || col + c <= -1 || moduleCount <= col + c) {
                    continue;
                }
                set(row + r, col + c,
                        (0 <= r && r <= 6 && (c == 0 || c == 6)) ||
                        (0 <= c && c <= 6 && (r == 0 || r == 6)) ||
                        (2 <= r && r <= 4 && 2 <= c && c <= 4));
            }
        }
    }

    void setupTimingPattern() {
        for (int r = 8; r < moduleCount - 8; r++) {
            if (!reserved[r][6]) {
                set(r, 6, r % 2 == 0);
            }
        }
        for (int c = 8; c < moduleCount - 8; c++) {
            if (!reserved[6][c]) {
                set(6, c, c % 2 == 0);
            }
        }
    }

    // Versions 7 and up carry their version number twice, next to two finder patterns.
    private void setupTypeNumber() {
        int bits = QRUtil.getBCHTypeNumber(typeNumber);
        for (int i = 0; i < 18; i++) {
            boolean dark = ((bits >> i) & 1) == 1;
            set(i / 3, i % 3 + moduleCount - 8 - 3, dark);
            set(i % 3 + moduleCount - 8 - 3, i / 3, dark);
        }
    }

    // Places the format information, the error correction level and the mask
    // pattern, twice in the modules, and the dark module next to the bottom
    // left finder pattern. The modules it takes are reserved.
    void setupTypeInfo(boolean[][] matrix, int maskPattern) {
        int data = (errorCorrectionLevel.value << 3) | maskPattern;
        int bits = QRUtil.getBCHTypeInfo(data);

        for (int i = 0; i < 15; i++) {
            boolean dark = ((bits >> i) & 1) == 1;
            if (i < 6) {
                put(matrix, i, 8, dark);
            } else if (i < 8) {
                put(matrix, i + 1, 8, dark);
            } else {
                put(matrix, moduleCount - 15 + i, 8, dark);
            }
        }

        for (int i = 0; i < 15; i++) {
            boolean dark = ((bits >> i) & 1) == 1;
            if (i < 8) {
                put(matrix, 8, moduleCount - i - 1, dark);
            } else if (i < 9) {
                put(matrix, 8, 15 - i - 1 + 1, dark);
            } else {
                put(matrix, 8, 15 - i - 1, dark);
            }
        }

        put(matrix, moduleCount - 8, 8, true);
    }

    private void put(boolean[][] matrix, int row, int col, boolean dark) {
        matrix[row][col] = dark;
        reserved[row][col] = true;
    }

    // For the tests: the modules, the size, and the modules as they are before the masks.
    boolean[][] modules() {
        return modules;
    }

    int getModuleCount() {
        return moduleCount;
    }

    void resetForTest() {
        modules = new boolean[moduleCount][moduleCount];
        reserved = new boolean[moduleCount][moduleCount];
    }

    byte[] createData(ErrorCorrectionLevel errorCorrectionLevel) {
        RSBlock[] rsBlocks = RSBlock.getRSBlocks(typeNumber, errorCorrectionLevel);

        BitBuffer buffer = new BitBuffer();
        if (eci) {
            buffer.put(7, 4);   // ECI
            buffer.put(26, 8);  // UTF-8
        }
        buffer.put(4, 4);       // Byte mode
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
