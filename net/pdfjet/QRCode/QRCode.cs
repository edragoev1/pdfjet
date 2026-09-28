/*
 * QRCode.cs
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
using System;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// Used to create 2D QR Code barcodes. Please see Example_20.
/// </summary>
public class QRCode : IDrawable {
    private const int PAD0 = 0xEC;
    private const int PAD1 = 0x11;
    // The bits of the ECI that says the bytes are UTF-8: the mode indicator
    // 0111 and the ECI assignment number 26 in 8 bits.
    private const int ECI_BITS = 12;
    private bool[][] modules;           // The dark modules
    private bool[][] reserved;          // The modules of the function patterns and the format information
    // The version of the symbol, from 4 to 40, and its size: 4 * typeNumber + 17 modules.
    private int typeNumber;
    private int moduleCount;
    private ErrorCorrectionLevel errorCorrectionLevel = ErrorCorrectionLevel.M;

    private float x;
    private float y;

    private byte[] qrData;
    private bool eci;                   // The data is not all ASCII, and starts with the ECI of UTF-8
    private float m1 = 2.0f;        // Module length

    private int color = Color.black;
    private String altDescription;

    /// <summary>
    /// Used to create 2D QR Code barcodes. The string is encoded in UTF-8, and
    /// the symbol is the smallest that holds it, from version 4, 33 by 33 modules,
    /// to version 40, 177 by 177 modules. At version 40 it holds up to 2,953 bytes
    /// at level L, 2,331 at M, 1,663 at Q and 1,273 at H, a byte fewer when the
    /// string is not all ASCII: it then starts with the ECI that tells the reader
    /// the bytes are UTF-8.
    /// </summary>
    /// <param name="str">the string to encode.</param>
    /// <param name="errorCorrectionLevel">the desired error correction level.</param>
    /// <exception cref="ArgumentException">If the string does not fit in a version 40 symbol.</exception>
    public QRCode(String str, ErrorCorrectionLevel errorCorrectionLevel) {
        this.qrData = Encoding.GetEncoding("utf-8").GetBytes(str);
        foreach (byte b in qrData) {
            if (b > 127) {
                eci = true;
            }
        }
        this.errorCorrectionLevel = errorCorrectionLevel;
        this.typeNumber = GetTypeNumber(qrData.Length, eci, errorCorrectionLevel);
        this.moduleCount = 4 * typeNumber + 17;
        this.Make(CreateData(errorCorrectionLevel));
    }

    // Returns the smallest version, from 4, whose data codewords hold the data,
    // and the ECI before it when there is one.
    private static int GetTypeNumber(int dataLength, bool eci, ErrorCorrectionLevel errorCorrectionLevel) {
        for (int typeNumber = 4; typeNumber <= 40; typeNumber++) {
            if (GetLengthInBits(dataLength, eci, typeNumber) <= GetDataCount(typeNumber, errorCorrectionLevel) * 8) {
                return typeNumber;
            }
        }
        int maxLength = (GetDataCount(40, errorCorrectionLevel) * 8 - GetLengthInBits(0, eci, 40)) / 8;
        throw new ArgumentException("The data is too long for a QR code at level "
                + errorCorrectionLevel + ": " + dataLength + " bytes, at most " + maxLength + ".");
    }

    // The bits of the ECI, if any, and of the mode indicator, the character
    // count and the data, in byte mode.
    private static int GetLengthInBits(int dataLength, bool eci, int typeNumber) {
        int bits = 4 + GetCharacterCountBits(typeNumber) + 8 * dataLength;
        return eci ? bits + ECI_BITS : bits;
    }

    // The character count of byte mode has 8 bits up to version 9, and 16 bits after it.
    private static int GetCharacterCountBits(int typeNumber) {
        return (typeNumber < 10) ? 8 : 16;
    }

    private static int GetDataCount(int typeNumber, ErrorCorrectionLevel errorCorrectionLevel) {
        int dataCount = 0;
        foreach (RSBlock rsBlock in RSBlock.GetRSBlocks(typeNumber, errorCorrectionLevel)) {
            dataCount += rsBlock.GetDataCount();
        }
        return dataCount;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>
    /// Sets the location where this barcode will be drawn on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner of the barcode.</param>
    /// <param name="y">the y coordinate of the top left corner of the barcode.</param>
    /// <returns>this QRCode object.</returns>
    public QRCode SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>
    /// Sets the module length of this barcode.
    /// The default value is 2.0f
    /// </summary>
    /// <param name="moduleLength">the specified module length.</param>
    /// <returns>this QRCode object.</returns>
    public QRCode SetModuleLength(float moduleLength) {
        this.m1 = moduleLength;
        return this;
    }

    /// <summary>Sets the color of the QR code as a 0xRRGGBB value.</summary>
    public QRCode SetModuleColor(int color) {
        this.color = color;
        return this;
    }

    /// <summary>
    /// Sets what the QR code says, such as the web address it carries, for a
    /// screen reader: a tagged document, PDF/UA or a PDF/A of level A, then has
    /// the QR code as a figure of that description. Without one, it is
    /// decoration, which a screen reader skips.
    /// </summary>
    /// <param name="altDescription">the description.</param>
    /// <returns>this QRCode object.</returns>
    public QRCode SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    /// Draws this barcode on the specified page. The dark modules next to each
    /// other in a row are filled as one rectangle, and the pen and the brush of
    /// the page are as they were after it.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        float size = m1*moduleCount;
        if (page != null) {
            // Described, the QR code is a figure of a tagged document; not
            // described, its modules, which carry no text, are decoration.
            if (!String.IsNullOrEmpty(altDescription)) {
                page.AddBDC(StructElem.FIGURE, null, null, altDescription);
            } else {
                page.AddArtifactBMC();
            }
            page.SaveGraphicsState();
            page.SetBrushColor(this.color);
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
                    page.FillRect(x + start*m1, y + row*m1, (col - start)*m1, m1);
                }
            }
            page.RestoreGraphicsState();
            if (!String.IsNullOrEmpty(altDescription)) {
                page.SetFigureBoundingBox(x, y, size, size);
            }
            page.AddEMC();
        }
        return new float[] {x + size, y + size};
    }

    /// <summary>
    /// Returns the modules of the QR code: true for dark and false for light modules.
    /// </summary>
    /// <returns>the modules.</returns>
    public Boolean?[][] GetModules() {
        Boolean?[][] copy = new Boolean?[moduleCount][];
        for (int row = 0; row < moduleCount; row++) {
            copy[row] = new Boolean?[moduleCount];
            for (int col = 0; col < moduleCount; col++) {
                copy[row][col] = modules[row][col];
            }
        }
        return copy;
    }

    // Places the function patterns and the codewords, and masks them with the
    // mask pattern of the lowest penalty, with its format information.
    private void Make(byte[] data) {
        modules = NewMatrix(moduleCount);
        reserved = NewMatrix(moduleCount);

        SetupPositionProbePattern(0, 0);
        SetupPositionProbePattern(moduleCount - 7, 0);
        SetupPositionProbePattern(0, moduleCount - 7);

        SetupPositionAdjustPattern();
        SetupTimingPattern();
        SetupTypeInfo(modules, 0);  // Reserves the modules of the format information
        if (typeNumber >= 7) {
            SetupTypeNumber();
        }
        MapData(data);

        // Each mask is tried with its format information in place, as ISO/IEC
        // 18004 asks, and the first of the lowest penalty is taken.
        bool[][] best = null;
        int minLostPoint = 0;
        for (int maskPattern = 0; maskPattern < 8; maskPattern++) {
            bool[][] masked = ApplyMask(maskPattern);
            int lostPoint = QRUtil.GetLostPoint(masked);
            if (best == null || lostPoint < minLostPoint) {
                minLostPoint = lostPoint;
                best = masked;
            }
        }
        modules = best;
        reserved = null;
    }

    // Returns a square matrix of light modules.
    internal static bool[][] NewMatrix(int moduleCount) {
        bool[][] matrix = new bool[moduleCount][];
        for (int i = 0; i < moduleCount; i++) {
            matrix[i] = new bool[moduleCount];
        }
        return matrix;
    }

    // Sets the module of a function pattern or of the format or version information.
    private void Set(int row, int col, bool dark) {
        modules[row][col] = dark;
        reserved[row][col] = true;
    }

    // Returns the modules with the mask pattern applied to the codewords, and
    // the format information of the mask pattern.
    private bool[][] ApplyMask(int maskPattern) {
        bool[][] masked = NewMatrix(moduleCount);
        for (int row = 0; row < moduleCount; row++) {
            for (int col = 0; col < moduleCount; col++) {
                bool dark = modules[row][col];
                if (!reserved[row][col] && QRUtil.GetMask(maskPattern, row, col)) {
                    dark = !dark;
                }
                masked[row][col] = dark;
            }
        }
        SetupTypeInfo(masked, maskPattern);
        return masked;
    }

    // Places the bits of the codewords in the modules that are not reserved, in
    // two module wide columns from the bottom right corner, up and down in turn;
    // the modules left over are light.
    private void MapData(byte[] data) {
        int inc = -1;
        int row = moduleCount - 1;
        int bitIndex = 7;
        int byteIndex = 0;

        for (int col = moduleCount - 1; col > 0; col -= 2) {
            if (col == 6) col--;
            while (true) {
                for (int c = 0; c < 2; c++) {
                    if (!reserved[row][col - c]) {
                        bool dark = false;

                        if (byteIndex < data.Length) {
                            dark = (((int) (((uint) data[byteIndex]) >> bitIndex) & 1) == 1);
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

    private void SetupPositionAdjustPattern() {
        int[] pos = QRUtil.GetPatternPosition(typeNumber);
        foreach (int row in pos) {
            foreach (int col in pos) {

                if (reserved[row][col]) {
                    continue;
                }

                for (int r = -2; r <= 2; r++) {
                    for (int c = -2; c <= 2; c++) {
                        Set(row + r, col + c,
                                r == -2 || r == 2 || c == -2 || c == 2 || (r == 0 && c == 0));
                    }
                }
            }
        }
    }

    private void SetupPositionProbePattern(int row, int col) {
        for (int r = -1; r <= 7; r++) {
            for (int c = -1; c <= 7; c++) {
                if (row + r <= -1 || moduleCount <= row + r
                        || col + c <= -1 || moduleCount <= col + c) {
                    continue;
                }

                Set(row + r, col + c,
                        (0 <= r && r <= 6 && (c == 0 || c == 6)) ||
                        (0 <= c && c <= 6 && (r == 0 || r == 6)) ||
                        (2 <= r && r <= 4 && 2 <= c && c <= 4));
            }
        }
    }

    private void SetupTimingPattern() {
        for (int r = 8; r < moduleCount - 8; r++) {
            if (!reserved[r][6]) {
                Set(r, 6, r % 2 == 0);
            }
        }
        for (int c = 8; c < moduleCount - 8; c++) {
            if (!reserved[6][c]) {
                Set(6, c, c % 2 == 0);
            }
        }
    }

    // Versions 7 and up carry their version number twice, next to two finder patterns.
    private void SetupTypeNumber() {
        int bits = QRUtil.GetBCHTypeNumber(typeNumber);
        for (int i = 0; i < 18; i++) {
            bool dark = ((bits >> i) & 1) == 1;
            Set(i / 3, i % 3 + moduleCount - 8 - 3, dark);
            Set(i % 3 + moduleCount - 8 - 3, i / 3, dark);
        }
    }

    // Places the format information, the error correction level and the mask
    // pattern, twice in the modules, and the dark module next to the bottom
    // left finder pattern. The modules it takes are reserved.
    private void SetupTypeInfo(bool[][] matrix, int maskPattern) {
        int data = ((int) errorCorrectionLevel << 3) | maskPattern;
        int bits = QRUtil.GetBCHTypeInfo(data);

        for (int i = 0; i < 15; i++) {
            bool dark = ((bits >> i) & 1) == 1;
            if (i < 6) {
                Put(matrix, i, 8, dark);
            } else if (i < 8) {
                Put(matrix, i + 1, 8, dark);
            } else {
                Put(matrix, moduleCount - 15 + i, 8, dark);
            }
        }

        for (int i = 0; i < 15; i++) {
            bool dark = ((bits >> i) & 1) == 1;
            if (i < 8) {
                Put(matrix, 8, moduleCount - i - 1, dark);
            } else if (i < 9) {
                Put(matrix, 8, 15 - i - 1 + 1, dark);
            } else {
                Put(matrix, 8, 15 - i - 1, dark);
            }
        }

        Put(matrix, moduleCount - 8, 8, true);
    }

    private void Put(bool[][] matrix, int row, int col, bool dark) {
        matrix[row][col] = dark;
        reserved[row][col] = true;
    }

    internal byte[] CreateData(ErrorCorrectionLevel errorCorrectionLevel) {
        RSBlock[] rsBlocks = RSBlock.GetRSBlocks(typeNumber, errorCorrectionLevel);

        BitBuffer buffer = new BitBuffer();
        if (eci) {
            buffer.Put(7, 4);   // ECI
            buffer.Put(26, 8);  // UTF-8
        }
        buffer.Put(4, 4);       // Byte mode
        buffer.Put(qrData.Length, GetCharacterCountBits(typeNumber));
        foreach (byte b in qrData) {
            buffer.Put(b, 8);
        }

        int totalDataCount = 0;
        foreach (RSBlock rsBlock in rsBlocks) {
            totalDataCount += rsBlock.GetDataCount();
        }

        if (buffer.GetLengthInBits() > totalDataCount * 8) {
            throw new ArgumentException("String length overflow. ("
                + buffer.GetLengthInBits()
                + ">"
                +  totalDataCount * 8
                + ")");
        }

        if (buffer.GetLengthInBits() + 4 <= totalDataCount * 8) {
            buffer.Put(0, 4);
        }

        // padding
        while (buffer.GetLengthInBits() % 8 != 0) {
            buffer.Put(false);
        }

        // padding
        while (true) {
            if (buffer.GetLengthInBits() >= totalDataCount * 8) {
                break;
            }
            buffer.Put(PAD0, 8);

            if (buffer.GetLengthInBits() >= totalDataCount * 8) {
                break;
            }
            buffer.Put(PAD1, 8);
        }

        return CreateBytes(buffer, rsBlocks);
    }

    private static byte[] CreateBytes(BitBuffer buffer, RSBlock[] rsBlocks) {
        int offset = 0;
        int maxDcCount = 0;
        int maxEcCount = 0;

        int[][] dcdata = new int[rsBlocks.Length][];
        int[][] ecdata = new int[rsBlocks.Length][];

        for (int r = 0; r < rsBlocks.Length; r++) {
            int dcCount = rsBlocks[r].GetDataCount();
            int ecCount = rsBlocks[r].GetTotalCount() - dcCount;

            maxDcCount = Math.Max(maxDcCount, dcCount);
            maxEcCount = Math.Max(maxEcCount, ecCount);

            dcdata[r] = new int[dcCount];
            for (int i = 0; i < dcdata[r].Length; i++) {
                dcdata[r][i] = 0xff & buffer.GetBuffer()[i + offset];
            }
            offset += dcCount;

            Polynomial rsPoly = QRUtil.GetErrorCorrectPolynomial(ecCount);
            Polynomial rawPoly = new Polynomial(dcdata[r], rsPoly.GetLength() - 1);

            Polynomial modPoly = rawPoly.Mod(rsPoly);
            ecdata[r] = new int[rsPoly.GetLength() - 1];
            for (int i = 0; i < ecdata[r].Length; i++) {
                int modIndex = i + modPoly.GetLength() - ecdata[r].Length;
                ecdata[r][i] = (modIndex >= 0)? modPoly.Get(modIndex) : 0;
            }
        }

        int totalCodeCount = 0;
        foreach (RSBlock rsBlock in rsBlocks) {
            totalCodeCount += rsBlock.GetTotalCount();
        }

        byte[] data = new byte[totalCodeCount];
        int index = 0;
        for (int i = 0; i < maxDcCount; i++) {
            for (int r = 0; r < rsBlocks.Length; r++) {
                if (i < dcdata[r].Length) {
                    data[index++] = (byte) dcdata[r][i];
                }
            }
        }

        for (int i = 0; i < maxEcCount; i++) {
            for (int r = 0; r < rsBlocks.Length; r++) {
                if (i < ecdata[r].Length) {
                    data[index++] = (byte) ecdata[r][i];
                }
            }
        }

        return data;
    }
}
}   // End of namespace PDFjet.NET
