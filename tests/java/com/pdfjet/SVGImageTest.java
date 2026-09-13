/*
 * SVGImageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.nio.charset.StandardCharsets;
import org.junit.jupiter.api.Test;

class SVGImageTest {
    private static String draw(String svg) throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        SVGImage image = new SVGImage(new ByteArrayInputStream(svg.getBytes(StandardCharsets.UTF_8)));
        image.setLocation(0f, 0f);
        TestSupport.assertXY(image.getWidth(), image.getHeight(), image.drawOn(page));
        return TestSupport.content(page);
    }

    @Test
    void readsTheSizeFromTheAttributes() throws Exception {
        SVGImage image = new SVGImage(new ByteArrayInputStream(
                "<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>".getBytes(StandardCharsets.UTF_8)));
        assertEquals(100f, image.getWidth(), 0f);
        assertEquals(50f, image.getHeight(), 0f);
    }

    @Test
    void keepsTheLastNumberOfAPathThatIsNotClosed() throws Exception {
        String content = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>");
        assertTrue(content.contains("10 782 m\n90 752 l\n"), content);
    }

    @Test
    void singleQuotesAndLineBreaksBetweenAttributesParseLikeDoubleQuotes() throws Exception {
        String doubleQuotes = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"red\"/></svg>");
        String singleQuotes = draw("<svg width='100' height='50'><path\n fill='red'\n d='M10 10 L90 40 L10 40 Z'/></svg>");
        assertEquals(doubleQuotes, singleQuotes);
        assertTrue(doubleQuotes.startsWith("1 0 0 rg\n"), doubleQuotes);
    }

    @Test
    void ellipticalArcsBecomeCubicCurves() throws Exception {
        String content = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 A 20 20 0 0 1 50 10\"/></svg>");
        // A half circle over the top, from (10, 10) to (50, 10) in SVG coordinates.
        assertTrue(content.contains("10 793.05 18.95 802 30 802 c\n41.05 802 50 793.05 50 782 c\n"), content);
    }

    @Test
    void aStrokeOnlyPathIsStroked() throws Exception {
        String content = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"none\" stroke=\"red\"/></svg>");
        assertTrue(content.contains("1 0 0 RG\n"), content);
        assertTrue(content.endsWith("s\n"), content);
    }

    @Test
    void anOpenPathWithAStrokeIsStroked() throws Exception {
        String content = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\" fill=\"none\" stroke=\"red\"/></svg>");
        assertTrue(content.endsWith("10 782 m\n90 752 l\nS\n"), content);
    }

    @Test
    void aClosedSubpathIsClosedAndAnOpenOneStrokedAtTheEnd() throws Exception {
        String content = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 Z M20 20 L30 30\" fill=\"none\" stroke=\"red\"/></svg>");
        assertTrue(content.endsWith("10 782 m\n90 752 l\ns\n20 772 m\n30 762 l\nS\n"), content);
    }

    @Test
    void fillNoneWithoutAStrokeDrawsNothing() throws Exception {
        assertEquals("", draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"none\"/></svg>"));
        assertEquals("", draw("<svg width=\"100\" height=\"50\" fill=\"none\"><path d=\"M10 10 L90 40 L10 40 Z\"/></svg>"));
    }

    @Test
    void noneOnThePathWinsOverTheColorsOfTheSvgElement() throws Exception {
        String content = draw("<svg width=\"100\" height=\"50\" fill=\"red\" stroke=\"green\">"
                + "<path d=\"M10 10 L90 40 L10 40 Z\" fill=\"none\" stroke=\"blue\"/>"
                + "<path d=\"M20 20 L80 30 L20 30 Z\" stroke=\"none\"/></svg>");
        assertTrue(content.contains("0 0 1 RG\n"), content);
        assertFalse(content.contains("0 0.5 0 RG"), content);
        // The second path takes the red fill of the svg element and has no stroke.
        assertTrue(content.contains("1 0 0 rg\n"), content);
        assertEquals(1, content.split("\nf\n", -1).length - 1, content);
        assertEquals(1, content.split("\ns\n", -1).length - 1, content);
    }

    @Test
    void aPathWithoutColorsOrWithAnUnknownFillIsFilledBlack() throws Exception {
        String unset = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\"/></svg>");
        assertTrue(unset.startsWith("0 0 0 rg\n") && unset.endsWith("\nf\n"), unset);
        String gradient = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"url(#g)\"/></svg>");
        assertEquals(unset, gradient);
    }
}
