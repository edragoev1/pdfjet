/**
 * RSBlock.java
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
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

class RSBlock {
    private int totalCount;
    private int dataCount;

    private RSBlock(int totalCount, int dataCount) {
        this.totalCount = totalCount;
        this.dataCount  = dataCount;
    }

    public int getDataCount() {
        return dataCount;
    }

    public int getTotalCount() {
        return totalCount;
    }

    public static RSBlock[] getRSBlocks(int errorCorrectLevel) {
        int[] rsBlock = getRsBlockTable(errorCorrectLevel);
        int length = rsBlock.length / 3;
        List<RSBlock> list = new ArrayList<RSBlock>();
        for (int i = 0; i < length; i++) {
            int count = rsBlock[3*i];
            int totalCount = rsBlock[3*i + 1];
            int dataCount  = rsBlock[3*i + 2];

            for (int j = 0; j < count; j++) {
                list.add(new RSBlock(totalCount, dataCount));
            }
        }
        return list.toArray(new RSBlock[list.size()]);
    }

    private static int[] getRsBlockTable(int errorCorrectLevel) {
        switch(errorCorrectLevel) {
        case ErrorCorrectLevel.L :
            return new int[] {1, 100, 80};
        case ErrorCorrectLevel.M :
            return new int[] {2, 50, 32};
        case ErrorCorrectLevel.Q :
            return new int[] {2, 50, 24};
        case ErrorCorrectLevel.H :
            return new int[] {4, 25, 9};
        }
        return null;
    }
}
