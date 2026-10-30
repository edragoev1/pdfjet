/*
 * ReviewCodesTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.barcodes;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.Letter;
import com.pdfjet.PDF;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import java.util.Arrays;
import org.junit.jupiter.api.Test;

class ReviewCodesTest {
    private static String repeat(char ch, int count) {
        return new String(new char[count]).replace('\0', ch);
    }

    @Test
    void code128TakesACharacterFrom128To159AsFNC4ShiftAndTheControlCharacter() throws Exception {
        assertEquals(Arrays.asList(Code128Table.START_B,
                Code128Table.FNC_4, Code128Table.SHIFT, 0x05 + 64, 'x' - 32,
                Code128Table.FNC_4, Code128Table.SHIFT, 0x1f + 64),
                Barcode.code128Codewords("\u0085x\u009f"));
        // Each takes three of the 48 codewords
        new Barcode(Barcode.CODE_128, repeat('\u0080', 16)).drawOn(new Page(TestSupport.newPDF(), Letter.PORTRAIT));
        Exception e = assertThrows(Exception.class, () -> new Barcode(Barcode.CODE_128, repeat('\u0080', 17)));
        assertTrue(e.getMessage().contains("one from 128 to 159 three"), e.getMessage());
    }

    // The bars are solid whatever dash pattern the page was left with, as in the
    // charts: a dashed bar is not a bar a scanner reads (the review of 30 October
    // 2026, E4).
    @Test
    void theBarsAreSolidOnADashedPage() throws Exception {
        TestSupport.allowCoreFonts(true);
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setStrokeDashPattern("[3 3] 0");
        new Barcode(Barcode.CODE_128, "A1").setFont(TestSupport.helvetica(pdf)).drawOn(page);
        TestSupport.allowCoreFonts(false);
        String content = TestSupport.content(page);
        assertTrue(content.contains("q\n0 0 0 RG\n[] 0 d\n"), content.substring(0, 80));
    }
}
