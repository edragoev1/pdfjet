/*
 * SVGImageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
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

    @Test
    void theFlagsOfAnArcAreOneCharacterAndNeedNoSeparator() throws Exception {
        // SVG 1.1 section 8.3.9 writes each flag of an elliptical arc as a
        // single character, so that nothing has to separate it from what
        // follows. Every path here draws the two half circles of the first.
        String separated = draw("<svg width=\"100\" height=\"50\">"
                + "<path d=\"M10 10 A 20 20 0 0 1 50 10 A 20 20 0 1 0 90 10\"/></svg>");
        String[] paths = {
            "M10 10 A20 20 0 01 50 10 A20 20 0 10 90 10",
            "M10 10 A20 20 0 0150 10 A20 20 0 1090 10",
            "M10 10A20 20 0 0150,10A20 20 0 1090,10",
            "M10 10 a20 20 0 0140 0 a20 20 0 1040 0",
            "M10 10 A20,20,0,0,1,50,10 A20,20,0,1,0,90,10",
        };
        for (String data : paths) {
            String content = draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>");
            assertEquals(separated, content, data);
        }
    }

    @Test
    void onlyTheFlagsOfAnArcAreReadOneCharacterAtATime() throws Exception {
        // The radii, the rotation and the end point of an arc are numbers like
        // any other, and the digits of every other command are too.
        String content = draw("<svg width=\"100\" height=\"50\">"
                + "<path d=\"M10 10 A10 10 0 0 1 10 40 L10 10 A10 10 0 0 0 10 40\"/></svg>");
        String other = draw("<svg width=\"100\" height=\"50\">"
                + "<path d=\"M10 10 A10 10 0 01 10 40 L10 10 A10 10 0 00 10 40\"/></svg>");
        assertEquals(content, other);
        assertTrue(content.contains("10 782 m\n") && content.contains(" c\n"), content);
    }

    @Test
    void arcRadiiTooSmallForTheEndPointsAreScaledToFitThem() throws Exception {
        // SVG 1.1 section F.6.6 scales up radii too small to reach the end
        // points until the ellipse just does: the arc is then half of it,
        // whichever way the large arc flag points, and the same as the arc of
        // the fitting radii.
        String fitted = draw("<svg width=\"200\" height=\"200\"><path d=\"M0 0 A50 50 0 0 1 100 0\"/></svg>");
        assertEquals(2, curves(fitted), fitted);
        String[] paths = {
            "M0 0 A10 10 0 0 1 100 0",
            "M0 0 A1 1 0 0 1 100 0",
            "M0 0 A10 10 0 1 1 100 0",
        };
        for (String data : paths) {
            assertEquals(fitted, draw("<svg width=\"200\" height=\"200\"><path d=\"" + data + "\"/></svg>"), data);
        }
    }

    @Test
    void anArcOfWholeQuarterTurnsIsDrawnInThatManyCurves() throws Exception {
        // An arc is drawn in pieces of at most a quarter turn. One of exactly a
        // quarter, a half or a whole turn is not split once more for the last
        // bit of the sweep, which the four ports do not compute alike.
        String[] paths = {
            "A 120 120 120 0 0 120 120",    // A quarter turn, of the fuzz corpus
            "M10 10 A 20 20 0 0 1 50 10",
            "M0 0 A50 50 0 0 1 100 0",
        };
        int[] curves = {1, 2, 2};
        for (int i = 0; i < paths.length; i++) {
            String content = draw("<svg width=\"200\" height=\"200\"><path d=\"" + paths[i] + "\"/></svg>");
            assertEquals(curves[i], curves(content), paths[i] + " draws " + content);
        }
    }

    private static SVGImage image(String svg) throws Exception {
        return new SVGImage(new ByteArrayInputStream(svg.getBytes(StandardCharsets.UTF_8)));
    }

    @Test
    void aSizeWithAUnitIsReadInPoints() throws Exception {
        // A number is in the user unit of the file, which PDFjet draws as a
        // point, and so is a number in px; the units of length are converted.
        String[] sizes = {"48", "48px", "48pt", "1in", "1pc", "210mm", "21cm", " 48 "};
        float[] points = {48f, 48f, 48f, 72f, 12f, 595.2756f, 595.2756f, 48f};
        for (int i = 0; i < sizes.length; i++) {
            SVGImage svg = image("<svg width=\"" + sizes[i] + "\" height=\"" + sizes[i] + "\"/>");
            assertEquals(points[i], svg.getWidth(), 0.001f, sizes[i]);
            assertEquals(points[i], svg.getHeight(), 0.001f, sizes[i]);
        }
    }

    @Test
    void aSizeThatCannotBeReadIsTheSizeOfTheViewBox() throws Exception {
        // Scaling the paths by a width of zero would draw every one of them
        // at the origin, so a size in a unit PDFjet cannot read, a percentage
        // among them, and a size the file does not give, leave the drawing
        // 1:1 with its viewBox.
        String[] svgs = {
            "<svg viewBox=\"0 0 100 50\"><path d=\"M10 10 L90 40\"/></svg>",
            "<svg width=\"100%\" height=\"100%\" viewBox=\"0 0 100 50\"><path d=\"M10 10 L90 40\"/></svg>",
            "<svg width=\"10em\" height=\"5em\" viewBox=\"0 0 100 50\"><path d=\"M10 10 L90 40\"/></svg>",
        };
        for (String svg : svgs) {
            assertEquals(100f, image(svg).getWidth(), 0f, svg);
            assertEquals(50f, image(svg).getHeight(), 0f, svg);
            assertTrue(draw(svg).contains("10 782 m\n90 752 l\n"), svg);
        }
    }

    @Test
    void aViewBoxThatIsNotFourNumbersOrHasNoSizeFails() {
        String[] boxes = {"0 0", "0 0 10 10 10", "0 0 ten 10", "0 0 0 10", "0 0 10 0"};
        String[] messages = {
            "four numbers are needed.", "four numbers are needed.", "four numbers are needed.",
            "its width and height cannot be zero.", "its width and height cannot be zero.",
        };
        for (int i = 0; i < boxes.length; i++) {
            String svg = "<svg width=\"10\" height=\"10\" viewBox=\"" + boxes[i] + "\"/>";
            Exception e = assertThrows(Exception.class, () -> image(svg));
            assertEquals("Invalid SVG viewBox \"" + boxes[i] + "\": " + messages[i], e.getMessage());
        }
    }

    private static int curves(String content) {
        int count = 0;
        for (int i = content.indexOf(" c\n"); i != -1; i = content.indexOf(" c\n", i + 1)) {
            count++;
        }
        return count;
    }
}
