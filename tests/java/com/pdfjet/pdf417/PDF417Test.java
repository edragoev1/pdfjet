/*
 * PDF417Test.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.pdf417;

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
}
