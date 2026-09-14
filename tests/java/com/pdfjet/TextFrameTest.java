/*
 * TextFrameTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.Arrays;
import org.junit.jupiter.api.Test;

class TextFrameTest {
    // Draws two paragraphs of one line and returns how far below the first the second starts.
    private static float paragraphDistance(Float gap) throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Paragraph first = new Paragraph(new TextLine(font, "one"));
        Paragraph second = new Paragraph(new TextLine(font, "two"));
        TextFrame frame = new TextFrame(Arrays.asList(first, second)).setLocation(10f, 10f).setWidth(300f);
        if (gap != null) {
            frame.setParagraphGap(gap);
        }
        frame.drawOn(new Page(pdf, Letter.PORTRAIT));
        return second.getY1() - first.getY1();
    }

    @Test
    void theGapIsAddedToTheLineSoParagraphsNeverOverlap() throws Exception {
        float line = TestSupport.helvetica(TestSupport.newPDF()).getBodyHeight();
        assertEquals(2f * line, paragraphDistance(null), TestSupport.DELTA);    // one empty line
        assertEquals(line, paragraphDistance(0f), TestSupport.DELTA);
        assertEquals(line + 10f, paragraphDistance(10f), TestSupport.DELTA);
    }
}
