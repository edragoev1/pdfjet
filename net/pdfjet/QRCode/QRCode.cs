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
    private Boolean?[][] modules;
    private int moduleCount = 33;    // Magic Number
    private int errorCorrectLevel = ErrorCorrectLevel.M;

    private float x;
    private float y;

    private byte[] qrData;
    private float m1 = 2.0f;        // Module length

    private int color = Color.black;

    /// <summary>
    /// Used to create 2D QR Code barcodes.
    /// </summary>
    /// <param name="str">the string to encode.</param>
    /// <param name="errorCorrectLevel">the desired error correction level.</param>
    public QRCode(String str, int errorCorrectLevel) {
        this.qrData = Encoding.GetEncoding("utf-8").GetBytes(str);
        this.errorCorrectLevel = errorCorrectLevel;
        this.Make(false, GetBestMaskPattern());
    }

    /// <summary>
    /// Sets the position where this barcode will be drawn on the page.
    /// </summary>
    /// <param name="x">the x coordinate of the top left corner of the barcode.</param>
    /// <param name="y">the y coordinate of the top left corner of the barcode.</param>
    /// <returns>this QRCode object.</returns>
    public QRCode SetLocation(double x, double y) {
        SetLocation((float) x, (float) y);
        return this;
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
    public QRCode SetModuleLength(double moduleLength) {
        this.m1 = (float) moduleLength;
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
    public QRCode SetColor(int color) {
        this.color = color;
        return this;
    }

    /// <summary>
    /// Draws this barcode on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        page.SetBrushColor(this.color);
        for (int row = 0; row < modules.Length; row++) {
            for (int col = 0; col < modules.Length; col++) {
                if (IsDark(row, col)) {
                    page.FillRect(x + col*m1, y + row*m1, m1, m1);
                }
            }
        }

        float w = m1*modules.Length;
        float h = m1*modules.Length;
        return new float[] {x + w, y + h};
    }

    internal bool IsDark(int row, int col) {
        if (modules[row][col] != null) {
            return (bool) modules[row][col];
        } else {
            return false;
        }
    }

    internal int GetModuleCount() {
        return moduleCount;
    }

    private int GetBestMaskPattern() {
        int minLostPoint = 0;
        int pattern = 0;

        for (int i = 0; i < 8; i++) {
            Make(true, i);
            int lostPoint = QRUtil.GetLostPoint(this);

            if (i == 0 || minLostPoint >  lostPoint) {
                minLostPoint = lostPoint;
                pattern = i;
            }
        }

        return pattern;
    }

    private void Make(bool test, int maskPattern) {
        modules = new Boolean?[moduleCount][];
        for (int i = 0; i < modules.Length; i++) {
            modules[i] = new Boolean?[moduleCount];
        }

        SetupPositionProbePattern(0, 0);
        SetupPositionProbePattern(moduleCount - 7, 0);
        SetupPositionProbePattern(0, moduleCount - 7);

        SetupPositionAdjustPattern();
        SetupTimingPattern();
        SetupTypeInfo(test, maskPattern);

        MapData(CreateData(errorCorrectLevel), maskPattern);
    }

    private void MapData(byte[] data, int maskPattern) {
        int inc = -1;
        int row = moduleCount - 1;
        int bitIndex = 7;
        int byteIndex = 0;

        for (int col = moduleCount - 1; col > 0; col -= 2) {
            if (col == 6) col--;
            while (true) {
                for (int c = 0; c < 2; c++) {
                    if (modules[row][col - c] == null) {
                        bool dark = false;

                        if (byteIndex < data.Length) {
                            dark = (((int) (((uint) data[byteIndex]) >> bitIndex) & 1) == 1);
                        }

                        bool mask = QRUtil.GetMask(maskPattern, row, col - c);
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

    private void SetupPositionAdjustPattern() {
        int[] pos = {6, 26};    // Magic Numbers
        foreach (int row in pos) {
            foreach (int col in pos) {

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

    private void SetupPositionProbePattern(int row, int col) {
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

    private void SetupTimingPattern() {
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

    private void SetupTypeInfo(bool test, int maskPattern) {
        int data = (errorCorrectLevel << 3) | maskPattern;
        int bits = QRUtil.GetBCHTypeInfo(data);

        for (int i = 0; i < 15; i++) {
            bool mod = (!test && ((bits >> i) & 1) == 1);
            if (i < 6) {
                modules[i][8] = mod;
            } else if (i < 8) {
                modules[i + 1][8] = mod;
            } else {
                modules[moduleCount - 15 + i][8] = mod;
            }
        }

        for (int i = 0; i < 15; i++) {
            bool mod = (!test && ((bits >> i) & 1) == 1);
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

    private byte[] CreateData(int errorCorrectLevel) {
        RSBlock[] rsBlocks = RSBlock.GetRSBlocks(errorCorrectLevel);

        BitBuffer buffer = new BitBuffer();
        buffer.Put(4, 4);
        buffer.Put(qrData.Length, 8);
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
