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

    public static RSBlock[] GetRSBlocks(ErrorCorrectionLevel errorCorrectionLevel) {
        int[] rsBlock = GetRsBlockTable(errorCorrectionLevel);
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

    private static int[] GetRsBlockTable(ErrorCorrectionLevel errorCorrectionLevel) {
        switch(errorCorrectionLevel) {
        case ErrorCorrectionLevel.L:
            return new int[] {1, 100, 80};
        case ErrorCorrectionLevel.M:
            return new int[] {2, 50, 32};
        case ErrorCorrectionLevel.Q:
            return new int[] {2, 50, 24};
        case ErrorCorrectionLevel.H:
            return new int[] {4, 25, 9};
        }
        return null;
    }
}
}   // End of namespace PDFjet.NET
