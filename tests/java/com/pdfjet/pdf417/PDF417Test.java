/*
 * PDF417Test.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.pdf417;

import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.CapStyle;
import com.pdfjet.Letter;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import org.junit.jupiter.api.Test;

class PDF417Test {
    @Test
    void drawingTwiceDoesNotMoveTheSymbol() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        PDF417 symbol = new PDF417("Hello, World!");
        TestSupport.assertXY(281.25f, 11.25f, symbol.drawOn(page));
        TestSupport.assertXY(281.25f, 11.25f, symbol.drawOn(page));
    }

    // The bars have butt caps whatever cap the page was left with: a projecting
    // or round cap made each bar, one row tall, reach half its width into the
    // rows above and below it (ISO 32000, 8.4.3.3), and no reader read the symbol.
    @Test
    void theBarsHaveButtCapsOnAPageOfProjectingCaps() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        page.setLineCapStyle(CapStyle.PROJECTING_SQUARE);
        PDF417 symbol = new PDF417("Hello, PDF417 with projecting caps");
        symbol.setLocation(50f, 50f);
        symbol.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("q\n0 0 0 RG\n[] 0 d\n0 J\n"), content.substring(0, 80));
    }
}
