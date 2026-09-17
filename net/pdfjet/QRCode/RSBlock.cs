/*
 * RSBlock.cs
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
using System.Collections.Generic;

namespace PDFjet.NET {
class RSBlock {
    private int totalCount;
    private int dataCount;

    private RSBlock(int totalCount, int dataCount) {
        this.totalCount = totalCount;
        this.dataCount  = dataCount;
    }

    public int GetDataCount() {
        return dataCount;
    }

    public int GetTotalCount() {
        return totalCount;
    }

    // The blocks of error correction codewords of each version and level, in
    // the order L, M, Q, H, as groups of (count, total codewords, data codewords).
    private static readonly int[][] RS_BLOCK_TABLE = new int[][] {
        // 1
        new int[] {1, 26, 19},
        new int[] {1, 26, 16},
        new int[] {1, 26, 13},
        new int[] {1, 26, 9},
        // 2
        new int[] {1, 44, 34},
        new int[] {1, 44, 28},
        new int[] {1, 44, 22},
        new int[] {1, 44, 16},
        // 3
        new int[] {1, 70, 55},
        new int[] {1, 70, 44},
        new int[] {2, 35, 17},
        new int[] {2, 35, 13},
        // 4
        new int[] {1, 100, 80},
        new int[] {2, 50, 32},
        new int[] {2, 50, 24},
        new int[] {4, 25, 9},
        // 5
        new int[] {1, 134, 108},
        new int[] {2, 67, 43},
        new int[] {2, 33, 15, 2, 34, 16},
        new int[] {2, 33, 11, 2, 34, 12},
        // 6
        new int[] {2, 86, 68},
        new int[] {4, 43, 27},
        new int[] {4, 43, 19},
        new int[] {4, 43, 15},
        // 7
        new int[] {2, 98, 78},
        new int[] {4, 49, 31},
        new int[] {2, 32, 14, 4, 33, 15},
        new int[] {4, 39, 13, 1, 40, 14},
        // 8
        new int[] {2, 121, 97},
        new int[] {2, 60, 38, 2, 61, 39},
        new int[] {4, 40, 18, 2, 41, 19},
        new int[] {4, 40, 14, 2, 41, 15},
        // 9
        new int[] {2, 146, 116},
        new int[] {3, 58, 36, 2, 59, 37},
        new int[] {4, 36, 16, 4, 37, 17},
        new int[] {4, 36, 12, 4, 37, 13},
        // 10
        new int[] {2, 86, 68, 2, 87, 69},
        new int[] {4, 69, 43, 1, 70, 44},
        new int[] {6, 43, 19, 2, 44, 20},
        new int[] {6, 43, 15, 2, 44, 16},
        // 11
        new int[] {4, 101, 81},
        new int[] {1, 80, 50, 4, 81, 51},
        new int[] {4, 50, 22, 4, 51, 23},
        new int[] {3, 36, 12, 8, 37, 13},
        // 12
        new int[] {2, 116, 92, 2, 117, 93},
        new int[] {6, 58, 36, 2, 59, 37},
        new int[] {4, 46, 20, 6, 47, 21},
        new int[] {7, 42, 14, 4, 43, 15},
        // 13
        new int[] {4, 133, 107},
        new int[] {8, 59, 37, 1, 60, 38},
        new int[] {8, 44, 20, 4, 45, 21},
        new int[] {12, 33, 11, 4, 34, 12},
        // 14
        new int[] {3, 145, 115, 1, 146, 116},
        new int[] {4, 64, 40, 5, 65, 41},
        new int[] {11, 36, 16, 5, 37, 17},
        new int[] {11, 36, 12, 5, 37, 13},
        // 15
        new int[] {5, 109, 87, 1, 110, 88},
        new int[] {5, 65, 41, 5, 66, 42},
        new int[] {5, 54, 24, 7, 55, 25},
        new int[] {11, 36, 12, 7, 37, 13},
        // 16
        new int[] {5, 122, 98, 1, 123, 99},
        new int[] {7, 73, 45, 3, 74, 46},
        new int[] {15, 43, 19, 2, 44, 20},
        new int[] {3, 45, 15, 13, 46, 16},
        // 17
        new int[] {1, 135, 107, 5, 136, 108},
        new int[] {10, 74, 46, 1, 75, 47},
        new int[] {1, 50, 22, 15, 51, 23},
        new int[] {2, 42, 14, 17, 43, 15},
        // 18
        new int[] {5, 150, 120, 1, 151, 121},
        new int[] {9, 69, 43, 4, 70, 44},
        new int[] {17, 50, 22, 1, 51, 23},
        new int[] {2, 42, 14, 19, 43, 15},
        // 19
        new int[] {3, 141, 113, 4, 142, 114},
        new int[] {3, 70, 44, 11, 71, 45},
        new int[] {17, 47, 21, 4, 48, 22},
        new int[] {9, 39, 13, 16, 40, 14},
        // 20
        new int[] {3, 135, 107, 5, 136, 108},
        new int[] {3, 67, 41, 13, 68, 42},
        new int[] {15, 54, 24, 5, 55, 25},
        new int[] {15, 43, 15, 10, 44, 16},
        // 21
        new int[] {4, 144, 116, 4, 145, 117},
        new int[] {17, 68, 42},
        new int[] {17, 50, 22, 6, 51, 23},
        new int[] {19, 46, 16, 6, 47, 17},
        // 22
        new int[] {2, 139, 111, 7, 140, 112},
        new int[] {17, 74, 46},
        new int[] {7, 54, 24, 16, 55, 25},
        new int[] {34, 37, 13},
        // 23
        new int[] {4, 151, 121, 5, 152, 122},
        new int[] {4, 75, 47, 14, 76, 48},
        new int[] {11, 54, 24, 14, 55, 25},
        new int[] {16, 45, 15, 14, 46, 16},
        // 24
        new int[] {6, 147, 117, 4, 148, 118},
        new int[] {6, 73, 45, 14, 74, 46},
        new int[] {11, 54, 24, 16, 55, 25},
        new int[] {30, 46, 16, 2, 47, 17},
        // 25
        new int[] {8, 132, 106, 4, 133, 107},
        new int[] {8, 75, 47, 13, 76, 48},
        new int[] {7, 54, 24, 22, 55, 25},
        new int[] {22, 45, 15, 13, 46, 16},
        // 26
        new int[] {10, 142, 114, 2, 143, 115},
        new int[] {19, 74, 46, 4, 75, 47},
        new int[] {28, 50, 22, 6, 51, 23},
        new int[] {33, 46, 16, 4, 47, 17},
        // 27
        new int[] {8, 152, 122, 4, 153, 123},
        new int[] {22, 73, 45, 3, 74, 46},
        new int[] {8, 53, 23, 26, 54, 24},
        new int[] {12, 45, 15, 28, 46, 16},
        // 28
        new int[] {3, 147, 117, 10, 148, 118},
        new int[] {3, 73, 45, 23, 74, 46},
        new int[] {4, 54, 24, 31, 55, 25},
        new int[] {11, 45, 15, 31, 46, 16},
        // 29
        new int[] {7, 146, 116, 7, 147, 117},
        new int[] {21, 73, 45, 7, 74, 46},
        new int[] {1, 53, 23, 37, 54, 24},
        new int[] {19, 45, 15, 26, 46, 16},
        // 30
        new int[] {5, 145, 115, 10, 146, 116},
        new int[] {19, 75, 47, 10, 76, 48},
        new int[] {15, 54, 24, 25, 55, 25},
        new int[] {23, 45, 15, 25, 46, 16},
        // 31
        new int[] {13, 145, 115, 3, 146, 116},
        new int[] {2, 74, 46, 29, 75, 47},
        new int[] {42, 54, 24, 1, 55, 25},
        new int[] {23, 45, 15, 28, 46, 16},
        // 32
        new int[] {17, 145, 115},
        new int[] {10, 74, 46, 23, 75, 47},
        new int[] {10, 54, 24, 35, 55, 25},
        new int[] {19, 45, 15, 35, 46, 16},
        // 33
        new int[] {17, 145, 115, 1, 146, 116},
        new int[] {14, 74, 46, 21, 75, 47},
        new int[] {29, 54, 24, 19, 55, 25},
        new int[] {11, 45, 15, 46, 46, 16},
        // 34
        new int[] {13, 145, 115, 6, 146, 116},
        new int[] {14, 74, 46, 23, 75, 47},
        new int[] {44, 54, 24, 7, 55, 25},
        new int[] {59, 46, 16, 1, 47, 17},
        // 35
        new int[] {12, 151, 121, 7, 152, 122},
        new int[] {12, 75, 47, 26, 76, 48},
        new int[] {39, 54, 24, 14, 55, 25},
        new int[] {22, 45, 15, 41, 46, 16},
        // 36
        new int[] {6, 151, 121, 14, 152, 122},
        new int[] {6, 75, 47, 34, 76, 48},
        new int[] {46, 54, 24, 10, 55, 25},
        new int[] {2, 45, 15, 64, 46, 16},
        // 37
        new int[] {17, 152, 122, 4, 153, 123},
        new int[] {29, 74, 46, 14, 75, 47},
        new int[] {49, 54, 24, 10, 55, 25},
        new int[] {24, 45, 15, 46, 46, 16},
        // 38
        new int[] {4, 152, 122, 18, 153, 123},
        new int[] {13, 74, 46, 32, 75, 47},
        new int[] {48, 54, 24, 14, 55, 25},
        new int[] {42, 45, 15, 32, 46, 16},
        // 39
        new int[] {20, 147, 117, 4, 148, 118},
        new int[] {40, 75, 47, 7, 76, 48},
        new int[] {43, 54, 24, 22, 55, 25},
        new int[] {10, 45, 15, 67, 46, 16},
        // 40
        new int[] {19, 148, 118, 6, 149, 119},
        new int[] {18, 75, 47, 31, 76, 48},
        new int[] {34, 54, 24, 34, 55, 25},
        new int[] {20, 45, 15, 61, 46, 16},
    };

    public static RSBlock[] GetRSBlocks(int typeNumber, ErrorCorrectionLevel errorCorrectionLevel) {
        int[] rsBlock = RS_BLOCK_TABLE[(typeNumber - 1) * 4 + GetTableIndex(errorCorrectionLevel)];
        int length = rsBlock.Length / 3;

        List<RSBlock> list = new List<RSBlock>();
        for (int i = 0; i < length; i++) {
            int count = rsBlock[3*i];
            int totalCount = rsBlock[3*i + 1];
            int dataCount  = rsBlock[3*i + 2];

            for (int j = 0; j < count; j++) {
                list.Add(new RSBlock(totalCount, dataCount));
            }
        }

        return list.ToArray();
    }

    // The value of a level is its format information bits; the table is in the order L, M, Q, H.
    private static int GetTableIndex(ErrorCorrectionLevel errorCorrectionLevel) {
        switch (errorCorrectionLevel) {
        case ErrorCorrectionLevel.L: return 0;
        case ErrorCorrectionLevel.M: return 1;
        case ErrorCorrectionLevel.Q: return 2;
        default: return 3;
        }
    }
}
}   // End of namespace PDFjet.NET
