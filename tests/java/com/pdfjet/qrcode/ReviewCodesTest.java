/*
 * ReviewCodesTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.qrcode;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.Color;
import com.pdfjet.Compliance;
import com.pdfjet.Letter;
import com.pdfjet.PDF;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import java.io.ByteArrayOutputStream;
import org.junit.jupiter.api.Test;

class ReviewCodesTest {
    private static String repeat(String str, int count) {
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < count; i++) {
            buf.append(str);
        }
        return buf.toString();
    }

    @Test
    void textThatIsNotASCIIStartsWithTheECIOfUTF8() throws Exception {
        // ECI 0111, the assignment number 26 in 8 bits, then byte mode 0100.
        // Version 4 at level L has one block, so the data codewords come first.
        byte[] data = new QRCode("Grüße", ErrorCorrectionLevel.L).createData(ErrorCorrectionLevel.L);
        assertEquals(0x71, data[0] & 0xff);
        assertEquals(0xA4, data[1] & 0xff);
        // ASCII starts with byte mode, as before
        data = new QRCode("Hello", ErrorCorrectionLevel.L).createData(ErrorCorrectionLevel.L);
        assertEquals(0x40, data[0] & 0xff);
        assertEquals(0x54, data[1] & 0xff);
    }

    @Test
    void theECIIsCountedInTheCapacity() throws Exception {
        // 2,953 bytes fit at level L, and 2,952 when they are not all ASCII
        String fits = repeat("é", 1476);
        assertEquals(177, new QRCode(fits, ErrorCorrectionLevel.L).getModules().length);
        Exception e = assertThrows(IllegalArgumentException.class,
                () -> new QRCode(fits + "a", ErrorCorrectionLevel.L));
        assertEquals("The data is too long for a QR code at level L: 2953 bytes, at most 2952.", e.getMessage());
    }

    @Test
    void thePenaltyIsThatOfISO18004() {
        // 5 by 5 light modules: N1 10 runs of 5, 30; N2 16 blocks, 48; N4 no
        // dark module, 100.
        assertEquals(178, QRUtil.getLostPoint(new boolean[5][5]));
        // 7 by 7 with 1011101 in the first row: N1 60; N2 30 blocks, 90; N3
        // the pattern after the light quiet zone, 40; N4 5 of 49 dark, 70.
        boolean[][] matrix = new boolean[7][7];
        matrix[0] = new boolean[] {true, false, true, true, true, false, true};
        assertEquals(260, QRUtil.getLostPoint(matrix));
    }

    @Test
    void theFormatInformationHasTheMaskOfTheLowestPenalty() throws Exception {
        QRCode qr = new QRCode("https://pdfjet.com", ErrorCorrectionLevel.M);
        int n = qr.getModuleCount();
        // The mask of the format information, read back from the modules
        int bits = 0;
        for (int i = 0; i < 15; i++) {
            int row = (i < 6) ? i : (i < 8) ? i + 1 : n - 15 + i;
            if (qr.modules()[row][8]) {
                bits |= 1 << i;
            }
        }
        int g15Mask = (1 << 14) | (1 << 12) | (1 << 10) | (1 << 4) | (1 << 1);
        int mask = ((bits ^ g15Mask) >> 10) & 7;
        // The penalty of each mask, with the modules made again unmasked
        qr.resetForTest();
        qr.setupPositionProbePattern(0, 0);
        qr.setupPositionProbePattern(n - 7, 0);
        qr.setupPositionProbePattern(0, n - 7);
        qr.setupPositionAdjustPattern();
        qr.setupTimingPattern();
        qr.setupTypeInfo(qr.modules(), 0);
        qr.mapData(qr.createData(ErrorCorrectionLevel.M));
        int[] penalty = new int[8];
        for (int i = 0; i < 8; i++) {
            penalty[i] = QRUtil.getLostPoint(qr.applyMask(i));
        }
        // Every other mask has a higher penalty, or the same and a higher number
        for (int i = 0; i < 8; i++) {
            assertTrue(penalty[i] > penalty[mask] || (penalty[i] == penalty[mask] && i >= mask),
                    "mask " + i + " has the penalty " + penalty[i] + ", mask " + mask + " " + penalty[mask]);
        }
    }

    @Test
    void theDarkModulesOfARowAreOneRectangle() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setBrushColor(Color.blue);
        QRCode qr = new QRCode("https://pdfjet.com", ErrorCorrectionLevel.M).setModuleColor(Color.red);
        qr.drawOn(page);
        String content = TestSupport.content(page);
        int runs = 0;
        for (boolean[] row : qr.modules()) {
            for (int col = 0; col < row.length; col++) {
                if (row[col] && (col == 0 || !row[col - 1])) {
                    runs++;
                }
            }
        }
        assertEquals(runs, content.split(" re\n", -1).length - 1);
        // The brush is saved and restored around the modules
        assertTrue(content.contains("/Artifact BMC\nq\n") && content.endsWith("Q\nEMC\n"), content);
        // The page knows the brush is blue again, and sets red when asked
        page.setBrushColor(Color.red);
        assertTrue(TestSupport.content(page).endsWith("EMC\n1 0 0 rg\n"));
    }
}
