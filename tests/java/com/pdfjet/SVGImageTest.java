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
    void scalingScalesTheSizeWithThePaths() throws Exception {
        SVGImage image = new SVGImage(new ByteArrayInputStream(
                "<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>".getBytes(StandardCharsets.UTF_8)));
        image.scaleBy(0.5f);
        assertEquals(50f, image.getWidth(), 0f);
        assertEquals(25f, image.getHeight(), 0f);
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        image.setLocation(0f, 0f);
        TestSupport.assertXY(50f, 25f, image.drawOn(page));
        assertTrue(TestSupport.content(page).contains("5 787 m\n45 772 l\n"));
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
    // The numbers of path data are written as SVG 1.1 section 8.3.9 gives them:
    // a sign, digits, a point and an exponent, and a sign or a point starts the
    // next number where no space or comma separates them.

    @Test
    void readsTheNumbersOfPathDataAsSVGWritesThem() throws Exception {
        // Every path draws the line from 10, 10 to 90, 40, written another way.
        String[] paths = {
            "M10 10 L90 40",
            "M10,10L90,40",
            "M 1e1 1e1 L 9e1 4e1",
            "M 1000e-2 1000e-2 L 9000e-2 4000e-2",
            "M 1000E-2 1000E-2 L 9000E-2 4000E-2",
            "M+10+10L+90+40",
            "M 10.0 10.0 L 90.0 40.0",
            "M 10 10 L 9e+1 4e+1",
            "M10 10\nL90 40",      // Path data is often written over several lines
            "M10\t10\tL90\t40",
            "M10 10\r\nL90 40",
        };
        for (String data : paths) {
            String content = draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>");
            assertTrue(content.contains("10 782 m\n90 752 l\n"), data + " draws " + content);
        }
    }

    @Test
    void pathDataThatStartsWithANumberDrawsNothing() throws Exception {
        // Path data starts with a moveto; the numbers before the first command
        // belong to no operation, and are left out rather than read as one.
        for (String data : new String[] {"10 10 L90 40", ".5.5L90 40", "-10L90 40"}) {
            String content = draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>");
            assertFalse(content.contains(" m\n"), data + " draws " + content);
        }
    }
    @Test
    void aPathWithoutDataDrawsNothing() throws Exception {
        // A path element without a d attribute is one this port threw on.
        String content = draw("<svg width=\"100\" height=\"50\"><path/><path d=\"M10 10 L90 40\"/></svg>");
        assertTrue(content.contains("10 782 m\n90 752 l\n"), content);
    }

    @Test
    void pathDataThatNeedsTheCurrentPointStartsAtTheOrigin() throws Exception {
        // The first command of the path data needs a current point, which this
        // port left unset: it threw.
        String[] paths = {
            "L90 40", "H90", "V40", "Q10 10 90 40", "T90 40",
            "C1 1 2 2 90 40", "S1 1 90 40", "A5 5 0 0 1 90 40", "l90 40",
        };
        for (String data : paths) {
            String content = draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>");
            assertTrue(content.contains(" l\n") || content.contains(" c\n"), data + " draws " + content);
        }
    }
}
