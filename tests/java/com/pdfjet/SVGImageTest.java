/*
 * SVGImageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.Locale;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import org.junit.jupiter.api.Test;

class SVGImageTest {
    // What the image draws, without the miter limit that is set around an
    // image that strokes, which theMiterLimitOfAStrokeIsWritten checks.
    static String withoutMiterLimit(String content) {
        return content.startsWith("q\n4 M\n") ? content.substring(6, content.length() - 2) : content;
    }

    private static String draw(String svg) throws Exception {
        return withoutMiterLimit(drawAsItIs(svg));
    }

    private static String drawAsItIs(String svg) throws Exception {
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
        assertTrue(content.endsWith("h\nS\n"), content);
    }

    @Test
    void anOpenPathWithAStrokeIsStroked() throws Exception {
        String content = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\" fill=\"none\" stroke=\"red\"/></svg>");
        assertTrue(content.endsWith("10 782 m\n90 752 l\nS\n"), content);
    }

    @Test
    void aClosedSubpathIsClosedAndThePathStrokedOnceAtTheEnd() throws Exception {
        String content = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 Z M20 20 L30 30\" fill=\"none\" stroke=\"red\"/></svg>");
        assertTrue(content.endsWith("10 782 m\n90 752 l\nh\n20 772 m\n30 762 l\nS\n"), content);
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
        assertEquals(1, content.split("\nh\nS\n", -1).length - 1, content);
    }

    @Test
    void aPathWithoutColorsIsFilledBlackAndOneOfAnUnknownURLIsNotFilled() throws Exception {
        String unset = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\"/></svg>");
        assertTrue(unset.startsWith("0 0 0 rg\n") && unset.endsWith("\nf\n"), unset);
        // A url of an element that is not there is none, as a browser draws it.
        String unknown = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"url(#g)\"/></svg>");
        assertEquals("", unknown);
    }

    @Test
    void aGradientIsDrawnInTheMeanColorOfItsStops() throws Exception {
        String stops = "<stop offset=\"0\" stop-color=\"#ff0000\"/><stop offset=\"1\" style=\"stop-color: blue\"/>";
        String content = draw("<svg width=\"100\" height=\"50\"><defs><linearGradient id=\"g\">" + stops
                + "</linearGradient><radialGradient id=\"h\" href=\"#g\"/></defs>"
                + "<path d=\"M10 10 L90 40 L10 40 Z\" fill=\"url(#g)\"/><path d=\"M10 10 L90 40\" stroke=\"url('#h')\"/></svg>");
        // 255/2 rounds half up; the second gradient takes the stops of the first.
        assertTrue(content.startsWith("0.5 0 0.5 rg\n"), content);
        assertTrue(content.contains("0.5 0 0.5 RG\n"), content);
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
    void pathDataThatDoesNotStartWithAMovetoDrawsNothing() throws Exception {
        // The first command of the path data needs a current point, which this
        // port left unset: it threw. Path data starts with a moveto, and SVG
        // 1.1 section 8.3.2 draws data in error up to where it is in error,
        // which here is nothing; a PDF has no current point to draw a line
        // from either.
        String[] paths = {
            "L90 40", "H90", "V40", "Q10 10 90 40", "T90 40",
            "C1 1 2 2 90 40", "S1 1 90 40", "A5 5 0 0 1 90 40", "l90 40", "Z L90 40",
        };
        for (String data : paths) {
            String content = draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/>"
                    + "<path d=\"M1 1 L2 2\" fill=\"none\" stroke=\"red\"/></svg>");
            assertEquals("1 0 0 RG\n1 w\n1 791 m\n2 790 l\nS\n", content, data);
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
            "M0 0 A 120 120 120 0 0 120 120",   // A quarter turn, of the fuzz corpus
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
    void aViewBoxInLowerCaseIsRead() throws Exception {
        // A file saved from a web page may write viewbox, which an HTML page's
        // parser reads as viewBox; viewBox wins when a file has both.
        String[] svgs = {
            "<svg width=\"50\" height=\"25\" viewbox=\"10 10 100 50\"><path d=\"M20 20 L110 60\"/></svg>",
            "<svg width=\"50\" height=\"25\" viewBox=\"10 10 100 50\" viewbox=\"0 0 200 100\"><path d=\"M20 20 L110 60\"/></svg>",
        };
        for (String svg : svgs) {
            assertTrue(draw(svg).contains("5 787 m\n50 767 l\n"), svg);
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
            "its width and height have to be above zero.", "its width and height have to be above zero.",
        };
        for (int i = 0; i < boxes.length; i++) {
            String svg = "<svg width=\"10\" height=\"10\" viewBox=\"" + boxes[i] + "\"/>";
            Exception e = assertThrows(Exception.class, () -> image(svg));
            assertEquals("Invalid SVG viewBox \"" + boxes[i] + "\": " + messages[i], e.getMessage());
        }
    }

    // What SVGImage draws of the SVG files of drawing programs: groups and what
    // they give their paths, transforms, the basic shapes, style attributes and
    // classes, fill rules, opacity, line caps and joins.

    @Test
    void aGroupGivesItsPathsItsColorsAndWidthUnlessTheyHaveTheirOwn() throws Exception {
        String content = draw("<svg width=\"100\" height=\"100\"><g fill=\"red\" stroke=\"blue\" stroke-width=\"2\">"
                + "<path d=\"M10 10 H90 V90 Z\"/><path d=\"M10 10 H50 V50 Z\" fill=\"green\" stroke-width=\"4\"/></g></svg>");
        assertEquals("1 0 0 rg\n10 782 m\n90 782 l\n90 702 l\nf\n0 0 1 RG\n2 w\n10 782 m\n90 782 l\n90 702 l\nh\nS\n"
                + "0 0.5 0 rg\n10 782 m\n50 782 l\n50 742 l\nf\n4 w\n10 782 m\n50 782 l\n50 742 l\nh\nS\n", content);
    }

    @Test
    void aPathIsFilledBlackUnlessItsFillIsNoneAndStrokedOneUnitWide() throws Exception {
        // As SVG draws them: a stroke does not take the fill away, and a stroke
        // width of 0 draws no stroke, where a PDF would draw the thinnest line.
        String content = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" stroke=\"red\"/></svg>");
        assertTrue(content.startsWith("0 0 0 rg\n") && content.contains("1 0 0 RG\n1 w\n"), content);
        content = draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" stroke=\"red\" stroke-width=\"0\"/></svg>");
        assertTrue(!content.contains("RG") && content.endsWith("\nf\n"), content);
    }

    @Test
    void transformsOfGroupsAndPathsAreAppliedInTurn() throws Exception {
        // The path is scaled, then moved by the group; its stroke is scaled too.
        String content = draw("<svg width=\"100\" height=\"100\"><g transform=\"translate(10 20)\">"
                + "<path d=\"M0 0 L10 0\" transform=\"scale(2)\" stroke=\"red\" fill=\"none\"/></g></svg>");
        assertEquals("1 0 0 RG\n2 w\n10 772 m\n30 772 l\nS\n", content);
        String[][] transforms = {
            {"rotate(90)", "0 792 m\n0 782 l\n"},
            {"rotate(90 10 10)", "20 792 m\n20 782 l\n"},
            {"matrix(1 0 0 1 5 6)", "5 786 m\n15 786 l\n"},
            {"translate(5,6)", "5 786 m\n15 786 l\n"},
            {"translate(5)", "5 792 m\n15 792 l\n"},
            {"scale(2 3)", "0 792 m\n20 792 l\n"},
            {"skewY(45)", "0 792 m\n10 782 l\n"},
            {"translate(10) scale(2)", "10 792 m\n30 792 l\n"},
            {"scale(2) translate(10)", "20 792 m\n40 792 l\n"},
            {"translate(10)\n,rotate(90)", "10 792 m\n10 782 l\n"},
            {"translate(10) rotate", "0 792 m\n10 792 l\n"},    // Cannot be read: none
            {"translate(10) unknown(1 2)", "0 792 m\n10 792 l\n"},
            {"translate(10) scale(1 2 3)", "0 792 m\n10 792 l\n"},
            {"translate(1e1) scale(.5.5)", "10 792 m\n15 792 l\n"},
            {"translate(-5-5) scale(+1+1)", "-5 797 m\n5 797 l\n"},
        };
        for (String[] transform : transforms) {
            content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0\" transform=\"" + transform[0] + "\"/></svg>");
            assertTrue(content.contains(transform[1]), transform[0] + " draws " + content + ", not " + transform[1]);
        }
        // skewX leaves the line along the x axis as it is, and moves the point below.
        content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L0 10\" transform=\"skewX(45)\"/></svg>");
        assertTrue(content.contains("0 792 m\n10 782 l\n"), content);
    }

    @Test
    void aShapeWithATransformThatFlattensItIsNotDrawn() throws Exception {
        assertEquals("", draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" transform=\"scale(0)\"/></svg>"));
    }

    @Test
    void drawsTheBasicShapes() throws Exception {
        String[][] shapes = {
            {"<rect x=\"10\" y=\"20\" width=\"30\" height=\"40\"/>", "0 0 0 rg\n10 772 m\n40 772 l\n40 732 l\n10 732 l\nf\n"},
            {"<rect x=\"10\" y=\"20\" width=\"30\" height=\"40\" rx=\"5\"/>", "0 0 0 rg\n15 772 m\n35 772 l\n"
                    + "37.76 772 40 769.76 40 767 c\n40 737 l\n40 734.24 37.76 732 35 732 c\n15 732 l\n"
                    + "12.24 732 10 734.24 10 737 c\n10 767 l\n10 769.76 12.24 772 15 772 c\nf\n"},
            // A radius larger than half the side is half the side, and one that
            // is not given is the other one.
            {"<rect width=\"10\" height=\"10\" ry=\"20\"/>", "0 0 0 rg\n5 792 m\n5 792 l\n"
                    + "7.76 792 10 789.76 10 787 c\n10 787 l\n10 784.24 7.76 782 5 782 c\n5 782 l\n"
                    + "2.24 782 0 784.24 0 787 c\n0 787 l\n0 789.76 2.24 792 5 792 c\nf\n"},
            {"<circle cx=\"50\" cy=\"50\" r=\"10\"/>", "0 0 0 rg\n60 742 m\n60 736.48 55.52 732 50 732 c\n"
                    + "44.48 732 40 736.48 40 742 c\n40 747.52 44.48 752 50 752 c\n55.52 752 60 747.52 60 742 c\nf\n"},
            {"<ellipse cx=\"50\" cy=\"50\" rx=\"20\" ry=\"10\"/>", "0 0 0 rg\n70 742 m\n70 736.48 61.05 732 50 732 c\n"
                    + "38.95 732 30 736.48 30 742 c\n30 747.52 38.95 752 50 752 c\n61.05 752 70 747.52 70 742 c\nf\n"},
            {"<line x1=\"10\" y1=\"20\" x2=\"30\" y2=\"40\" stroke=\"red\"/>", "1 0 0 RG\n1 w\n10 772 m\n30 752 l\nS\n"},
            {"<polyline points=\"10,10 20,20 10,20\" fill=\"none\" stroke=\"red\"/>", "1 0 0 RG\n1 w\n"
                    + "10 782 m\n20 772 l\n10 772 l\nS\n"},
            {"<polygon points=\"10 10 20 20 10 20 5\" fill=\"none\" stroke=\"red\"/>", "1 0 0 RG\n1 w\n"
                    + "10 782 m\n20 772 l\n10 772 l\nh\nS\n"},
            // Shapes of no size draw nothing.
            {"<rect width=\"0\" height=\"10\"/><circle r=\"0\"/><ellipse rx=\"5\"/><polygon points=\"1 2\"/><line/>", ""},
        };
        for (String[] shape : shapes) {
            assertEquals(shape[1], draw("<svg width=\"100\" height=\"100\">" + shape[0] + "</svg>"), shape[0]);
        }
    }

    @Test
    void styleAttributesWinOverClassesWhichWinOverAttributes() throws Exception {
        String content = draw("<svg width=\"100\" height=\"100\"><defs><style>"
                + "/* the {colors} */ .red{fill:#ff0000} .blue, .other { fill: blue !important; stroke: red }"
                + "g .green{fill:green} .green:hover{fill:green}</style></defs>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" fill=\"yellow\" class=\"red\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" class=\"blue red\" style=\"stroke: none\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" class=\"red blue\" style=\"fill:yellow;stroke-width:3\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" class=\"green\"/></svg>");
        StringBuilder fills = new StringBuilder();
        for (String line : content.split("\n")) {
            if (line.endsWith(" rg") || line.endsWith(" RG") || line.endsWith(" w")) {
                fills.append(fills.length() == 0 ? "" : "|").append(line);
            }
        }
        // The rules are applied in the order of the style sheet, whatever the
        // order of the classes; the selectors that are not a class alone are left out.
        assertEquals("1 0 0 rg|0 0 1 rg|1 1 0 rg|1 0 0 RG|3 w|0 0 0 rg", fills.toString(), content);
    }

    @Test
    void colorsAreHexadecimalRGBNamesOrTheCurrentColor() throws Exception {
        String[][] fills = {
            {"fill=\"#F00\"", "1 0 0 rg"},
            {"fill=\"rgb(255, 0, 0)\"", "1 0 0 rg"},
            {"fill=\"rgb(100%,0%,0%)\"", "1 0 0 rg"},
            {"fill=\"RGBA(255 0 0 / 1)\"", "1 0 0 rg"},
            {"fill=\"Red\"", "1 0 0 rg"},
            {"fill=\"currentColor\" color=\"red\"", "1 0 0 rg"},
            {"fill=\"currentColor\"", "0 0 0 rg"},
            {"fill=\"url(#gradient) red\"", "1 0 0 rg"},
            {"fill=\"unknown\"", "0 0 0 rg"},
            {"fill=\"rgb(nan, 0, 0)\"", "0 0 0 rg"},
            {"fill=\"rgb(inf, 0, 0)\"", "0 0 0 rg"},
            {"style=\"fill: currentColor; color: #0000ff\"", "0 0 1 rg"},
        };
        for (String[] fill : fills) {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" " + fill[0] + "/></svg>");
            assertTrue(content.startsWith(fill[1] + "\n"), fill[0] + " draws " + content);
        }
        // The current color is the color where the fill is used, not where it is set.
        String content = draw("<svg width=\"100\" height=\"100\" fill=\"currentColor\"><g color=\"red\">"
                + "<path d=\"M0 0 L10 0 L10 10 Z\"/></g></svg>");
        assertTrue(content.startsWith("1 0 0 rg\n"), content);
        assertThrows(Exception.class, () -> image("<svg><path d=\"M0 0 L1 1\" fill=\"#zzzzzz\"/></svg>"));
    }

    @Test
    void theEvenOddRuleFillsWithFStar() throws Exception {
        String content = draw("<svg width=\"100\" height=\"100\" style=\"fill-rule:evenodd\">"
                + "<path d=\"M0 0 H30 V30 H0 Z M10 10 H20 V20 H10 Z\"/><path d=\"M0 0 H30 V30 Z\" fill-rule=\"nonzero\"/></svg>");
        assertEquals(1, content.split("\nf\\*\n", -1).length - 1, content);
        assertTrue(content.endsWith("\nf\n"), content);
    }

    @Test
    void opacityIsSetInAGraphicsStateOfThePathsOwn() throws Exception {
        String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" opacity=\"0.5\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\"/></svg>");
        assertEquals("q\n/GS1 gs\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\nQ\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content);
        // The opacity of a group multiplies those of its paths, and a fill or a
        // stroke of no opacity is not drawn.
        SVGImage image = image("<svg width=\"100\" height=\"100\"><g opacity=\"0.5\"><g style=\"opacity:50%\">"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" fill-opacity=\"0.5\" stroke=\"red\" stroke-opacity=\"0\"/></g></g></svg>");
        SVGPath path = image.paths.get(0);
        assertEquals(0.125f, path.fillAlpha, 0f);
        assertEquals(0f, path.strokeAlpha, 0f);
        assertEquals(-1, path.stroke);
    }

    @Test
    void lineCapsAndJoinsAreSetInAGraphicsStateOfThePathsOwn() throws Exception {
        String content = draw("<svg width=\"100\" height=\"100\" stroke-linecap=\"round\" stroke-linejoin=\"bevel\">"
                + "<path d=\"M0 0 L10 0\" stroke=\"red\" fill=\"none\"/><path d=\"M0 0 L10 0 L10 10 Z\"/></svg>");
        assertEquals("q\n1 J\n2 j\n1 0 0 RG\n1 w\n0 792 m\n10 792 l\nS\nQ\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content);
    }

    @Test
    void theContentOfDefsAndOfWhatIsNotDisplayedIsNotDrawn() throws Exception {
        String content = draw("<svg width=\"100\" height=\"100\">"
                + "<defs><path d=\"M0 0 L10 0 L10 10 Z\"/></defs>"
                + "<clipPath><rect width=\"10\" height=\"10\"/></clipPath>"
                + "<symbol><circle r=\"5\"/></symbol>"
                + "<g display=\"none\"><path d=\"M0 0 L10 0 L10 10 Z\"/></g>"
                + "<g style=\"display:none\"><path d=\"M0 0 L10 0 L10 10 Z\" display=\"inline\"/></g>"
                + "<path d=\"M20 20 L30 20 L30 30 Z\"/></svg>");
        assertEquals("0 0 0 rg\n20 772 m\n30 772 l\n30 762 l\nf\n", content);
    }

    @Test
    void attributesOfOtherNamespacesAreLeftOut() throws Exception {
        String content = draw("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:x=\"urn:x\" width=\"100\" height=\"100\">"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" x:fill=\"red\" x:transform=\"scale(2)\"/></svg>");
        assertEquals("0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content);
    }

    @Test
    void theViewBoxScalesTheStrokesAndKeepsItsProportions() throws Exception {
        // The viewBox is scaled by 10 to fit the height, and centered across.
        String content = draw("<svg width=\"200\" height=\"100\" viewBox=\"0 0 10 10\">"
                + "<path d=\"M0 0 L10 10\" stroke=\"red\" fill=\"none\"/></svg>");
        assertEquals("1 0 0 RG\n10 w\n50 792 m\n150 692 l\nS\n", content);
        String[][] aspects = {
            {"xMinYMin", "0 792 m\n100 692 l\n"},
            {"xMaxYMax meet", "100 792 m\n200 692 l\n"},
            {"defer xMidYMid", "50 792 m\n150 692 l\n"},
            {"xMidYMin slice", "20 w\n0 792 m\n200 592 l\n"},  // Covers it, from the top
            {"none", "0 792 m\n200 692 l\n"},
        };
        for (String[] aspect : aspects) {
            content = draw("<svg width=\"200\" height=\"100\" viewBox=\"0 0 10 10\" preserveAspectRatio=\""
                    + aspect[0] + "\"><path d=\"M0 0 L10 10\" stroke=\"red\" fill=\"none\"/></svg>");
            assertTrue(content.contains(aspect[1]), aspect[0] + " draws " + content + ", not " + aspect[1]);
        }
    }

    @Test
    void aSizeThatIsNotGivenIsInTheProportionsOfTheViewBox() throws Exception {
        String[] svgs = {
            "<svg width=\"48\" viewBox=\"0 0 960 480\"/>",
            "<svg height=\"24\" viewBox=\"0 0 960 480\"/>",
            "<svg width=\"10%\" viewBox=\"0 0 960 480\"/>",
            "<svg width=\"100\" height=\"50\" viewBox=\"0 0 960 480\"/>",
        };
        float[][] sizes = {{48f, 24f}, {48f, 24f}, {960f, 480f}, {100f, 50f}};
        for (int i = 0; i < svgs.length; i++) {
            SVGImage image = image(svgs[i]);
            assertEquals(sizes[i][0], image.getWidth(), 0f, svgs[i]);
            assertEquals(sizes[i][1], image.getHeight(), 0f, svgs[i]);
        }
    }

    @Test
    void scalingScalesTheStrokes() throws Exception {
        SVGImage image = image("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\" stroke=\"red\" stroke-width=\"4\"/></svg>");
        image.scaleBy(0.5f);
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        image.setLocation(0f, 0f);
        image.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("1 0 0 RG\n2 w\n"), content);
    }

    @Test
    void valuesThatCannotBeReadAreLeftOut() throws Exception {
        String content = draw("<svg width=\"100\" height=\"100\" viewBox=\"0,0,100,100\" stroke-width=\"3\">"
                + "<path d=\"M0 0 L10 0\" fill=\"none\" stroke=\"red\" stroke-width=\"calc(1px + 1px)\" opacity=\"half\"/>"
                + "<rect width=\"Inf\" height=\"10\" stroke-width=\"NaN\"/><polygon points=\"0 0 1e999 1 2 2\"/></svg>");
        assertEquals("1 0 0 RG\n3 w\n0 792 m\n10 792 l\nS\n", content);
    }

    @Test
    void drawsTheSeedOfTheFuzzTestOfGroupsTransformsShapesAndStyles() throws Exception {
        // The seed of the Go port's fuzz test for what drawing programs write.
        String content = draw("<svg width=\"100\" height=\"50\" viewBox=\"0 0 200 100\" preserveAspectRatio=\"xMinYMax slice\">"
                + "<style>.a{fill:rgb(10%,20,30);stroke:currentColor;stroke-width:2px} .b{opacity:.5}</style>"
                + "<g transform=\"translate(10,5) rotate(30 5 5) skewX(10)\" color=\"blue\" class=\"a\" fill-rule=\"evenodd\">"
                + "<rect x=\"1\" y=\"2\" width=\"30\" height=\"20\" rx=\"4\" class=\"b\"/><circle cx=\"50\" cy=\"20\" r=\"8\"/>"
                + "<ellipse cx=\"80\" cy=\"20\" rx=\"10\" ry=\"5\" style=\"fill-opacity:0.3;stroke-linecap:round\"/>"
                + "<line x1=\"0\" y1=\"40\" x2=\"90\" y2=\"45\" stroke-linejoin=\"bevel\"/>"
                + "<polyline points=\"0,50 10,60 20,50\"/><polygon points=\"30 50 40 60 50 50\"/></g>"
                + "<defs><path d=\"M0 0 L5 5\"/></defs><g display=\"none\"><rect width=\"5\" height=\"5\"/></g></svg>");
        // Every shape but the line, which has no inside, is filled with the
        // even-odd rule; the first in a graphics state of its opacity.
        assertEquals(5, content.split("\nf\\*\n", -1).length - 1, content);
        // The slice draws past the size of the image, which cuts it off.
        assertTrue(content.startsWith("q\n0 792 m\n100 792 l\n100 742 l\n0 742 l\nW\nn\n"
                + "q\n/GS1 gs\n0.1 0.08 0.12 rg\n8.4 788.21 m\n17.93 782.71 l\n"), content);
    }

    @Test
    void aSizeOrATransformTooLargeForAFloatDrawsNothing() throws Exception {
        // A PDF holds numbers below 2^31 and no infinity: a size of inf is no
        // size, and a path a transform takes out of that range is not drawn.
        SVGImage image = new SVGImage(new ByteArrayInputStream(
                "<svg width=\"inf\" height=\"nan\" viewBox=\"0 0 100 50\"/>".getBytes(StandardCharsets.UTF_8)));
        assertEquals(100f, image.getWidth());
        assertEquals(50f, image.getHeight());
        String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" transform=\"scale(1e30)\"/>"
                + "<path d=\"M0 0 L10 0\" stroke=\"red\" transform=\"scale(1e20 1)\"/><path d=\"M0 0 L10 0 L10 10 Z\"/></svg>");
        assertEquals("0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content);
    }

    @Test
    void aSkewOfNinetyDegreesIsNotRead() throws Exception {
        for (String transform : new String[] {"skewX(90)", "skewY(-270)"}) {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" transform=\""
                    + transform + "\"/></svg>");
            assertEquals("0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", content, transform);
        }
    }

    private static int curves(String content) {
        int count = 0;
        for (int i = content.indexOf(" c\n"); i != -1; i = content.indexOf(" c\n", i + 1)) {
            count++;
        }
        return count;
    }

    @Test
    void readsTheNumbersOfSVG() throws Exception {
        for (String number : new String[] {"5.", ".5", "-.5", "+5", "-0.5e+2", "5E-1", "007"}) {
            String svg = "<svg width=\"100\" height=\"100\"><path d=\"M 0 0 L " + number + " 5\"/></svg>";
            new SVGImage(new ByteArrayInputStream(svg.getBytes(StandardCharsets.UTF_8)));
        }
    }

    @Test
    void refusesPathDataWithANumberThatIsNotOne() throws Exception {
        // The numbers the fuzzers made of path data: refused in words, in the
        // four ports, where Java failed with a NumberFormatException (found by
        // the fuzz replay, 8 October 2026).
        // And the numbers one language reads and the others do not, refused by
        // SVG's grammar in the four (the review of 9 October 2026).
        for (String number : new String[] {"-.", ".", "2.@5", "1d775", "1e40", "-1e40", "0x1p3", "1_0", "1f", "1e"}) {
            String svg = "<svg width=\"100\" height=\"100\"><path d=\"M 0 0 L " + number + " 5\"/></svg>";
            Exception e = assertThrows(Exception.class,
                    () -> new SVGImage(new ByteArrayInputStream(svg.getBytes(StandardCharsets.UTF_8))));
            assertTrue(e.getMessage().toLowerCase(Locale.ROOT).contains("invalid path data"), number + ": " + e);
        }
    }

    // The style sheet applies MAX_APPLIED declarations at most: N copies of a
    // rule for a class and N elements of it took seconds; and an element of
    // many classes is linear (review of 17 October 2026)
    @Test
    void aStyleSheetAppliesALimitedNumberOfDeclarations() throws Exception {
        StringBuilder svg = new StringBuilder("<svg viewBox=\"0 0 10 10\"><style>");
        for (int i = 0; i < 1001; i++) {
            svg.append(".a{fill:red}");
        }
        svg.append("</style>");
        for (int i = 0; i < 1001; i++) {
            svg.append("<g class=\"a\"/>");
        }
        svg.append("</svg>");
        Exception e = org.junit.jupiter.api.Assertions.assertThrows(Exception.class,
                () -> new SVGImage(new ByteArrayInputStream(svg.toString().getBytes(StandardCharsets.UTF_8))));
        assertTrue(e.getMessage().contains("applies more than 1000000 declarations"), e.getMessage());

        StringBuilder classes = new StringBuilder("<svg viewBox=\"0 0 10 10\"><style>.c1{fill:red}</style><path class=\"");
        for (int i = 0; i < 50000; i++) {
            classes.append('c').append(i).append(' ');
        }
        classes.append("\" d=\"M0 0L5 5\"/></svg>");
        long start = System.nanoTime();
        new SVGImage(new ByteArrayInputStream(classes.toString().getBytes(StandardCharsets.UTF_8)));
        assertTrue(System.nanoTime() - start < 5_000_000_000L, "50,000 classes took too long");
    }

    // A number of an attribute is written in decimal: a hexadecimal one, as
    // 0x1p3, or one with underscores, as 1_000, is none of SVG's, and is not
    // read in any port (review of 17 October 2026)
    @Test
    void aNumberOfAnAttributeIsWrittenInDecimal() {
        for (String text : new String[] {"0x1p3", "1_000", "0x10", "1f"}) {
            org.junit.jupiter.api.Assertions.assertThrows(NumberFormatException.class,
                    () -> SVGState.parseNumber(text, false), text);
        }
        assertEquals(1000.0, SVGState.parseNumber("1e3", false));
        assertEquals(0.5, SVGState.parseNumber("+.5", false));
        assertEquals(Double.NEGATIVE_INFINITY, SVGState.parseNumber("-inf", false));
        assertTrue(Double.isNaN(SVGState.parseNumber("NaN", false)));
    }

    // The text of a chart: text and tspan elements, placed as a browser
    // places them, in the fonts PDFjet carries unless a font is set.

    @Test
    void drawsATextAtItsXAndY() throws Exception {
        String content = draw("<svg width=\"200\" height=\"100\"><text x=\"10\" y=\"20\" font-size=\"12\">Hi</text></svg>");
        assertTrue(content.contains(" 12 Tf\n1 0 0 1 10 772 Tm\n<"), content);
        assertTrue(content.contains("> Tj\nET\n"), content);    // IBM Plex Sans, embedded
        assertFalse(content.contains("W\n"), content);   // Inside the image: nothing is cut off
    }

    @Test
    void anAnchorMovesTheTextByItsWidthAndDyIsInEm() throws Exception {
        // Hi is 957 thousandths of an em wide in IBM Plex Sans: 11.484 at 12.
        String content = draw("<svg width=\"200\" height=\"100\"><text x=\"100\" y=\"20\" dy=\"0.5em\" "
                + "font-size=\"12\" text-anchor=\"end\">Hi</text></svg>");
        assertTrue(content.contains("1 0 0 1 88.52 766 Tm\n"), content);
    }

    @Test
    void theTextAndTheTspansAreDrawnInTheOrderOfTheDocument() throws Exception {
        String content = draw("<svg width=\"200\" height=\"100\"><text x=\"0\" y=\"20\">"
                + " A <tspan font-weight=\"bold\">B</tspan>C<title>not drawn</title></text></svg>");
        // Three runs: "A " (the space at the start left out), the bold B in
        // IBM Plex Sans Bold, and C; the title is not drawn
        assertEquals(3, content.split(" Tj\n", -1).length - 1, content);
        String[] fonts = content.split(" 16 Tf\n");
        assertEquals(4, fonts.length, content);
        String regular = fonts[0].substring(fonts[0].lastIndexOf('/'));
        assertFalse(fonts[1].endsWith(regular), content);
        assertTrue(fonts[2].endsWith(regular), content);
    }

    @Test
    void aTspanWithAnXOrAYStartsAChunkAnchoredOnItsOwn() throws Exception {
        String content = draw("<svg width=\"200\" height=\"100\"><text text-anchor=\"middle\" "
                + "transform=\"translate(100,50)\" font-size=\"10\"><tspan y=\"-0.4em\">Go</tspan>"
                + "<tspan x=\"0\" y=\"0.7em\">12%</tspan></text></svg>");
        // Go is 1255 thousandths of an em, and 12% 2127.
        assertTrue(content.contains("1 0 0 1 93.72 746 Tm\n"), content);
        assertTrue(content.contains("1 0 0 1 89.36 735 Tm\n"), content);
    }

    @Test
    void theFontShorthandSetsTheSizeAndTheWeight() throws Exception {
        String content = draw("<svg width=\"200\" height=\"100\" style=\"font: bold 10px sans-serif\">"
                + "<text y=\"20\">A</text><text y=\"40\" font-weight=\"400\">B</text></svg>");
        assertEquals(2, content.split(" 10 Tf\n", -1).length - 1, content);
        assertTrue(content.indexOf("/F") != content.lastIndexOf("/F"), content);
    }

    @Test
    void aCoreFontDrawsTheMinusSignAsAHyphenMinus() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        image("<svg width=\"200\" height=\"100\"><text y=\"20\">\u22125</text></svg>")
                .setFont(new Font(pdf, CoreFont.HELVETICA)).drawOn(page);
        assertTrue(TestSupport.content(page).contains("[<2D35>] TJ"), TestSupport.content(page));
    }

    @Test
    void theFamilyTheWeightAndTheStyleChooseTheFontPDFjetCarries() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        image("<svg width=\"200\" height=\"100\">"
                + "<text y=\"10\" font-family=\"'Times New Roman', serif\" font-weight=\"600\" font-style=\"italic\">a</text>"
                + "<text y=\"20\" style=\"font: bold 10px Courier, monospace\">b</text>"
                + "<text y=\"30\" font-family=\"Unknown, Arial\">c</text>"
                + "<text y=\"40\" font-family=\"Unknown\" font-weight=\"bolder\">d</text></svg>").drawOn(page);
        // Another image draws in the same fonts of the document, not new ones
        image("<svg width=\"200\" height=\"100\"><text y=\"10\" font-family=\"Georgia\" "
                + "font-weight=\"600\" font-style=\"oblique\">e</text></svg>").drawOn(page);
        assertEquals(new java.util.TreeSet<String>(java.util.Arrays.asList(
                "IBMPlexSerif/IBMPlexSerif-SemiBoldItalic.ttf", "CascadiaMono/CascadiaMono-Bold.ttf",
                "IBMPlexSans/IBMPlexSans-Regular.ttf", "IBMPlexSans/IBMPlexSans-Bold.ttf")),
                new java.util.TreeSet<String>(pdf.bundledFonts.keySet()));
    }

    @Test
    void aStrokeBeforeTheFillDrawsAHaloUnderTheText() throws Exception {
        String content = draw("<svg width=\"200\" height=\"100\"><text y=\"20\" stroke=\"white\" stroke-width=\"3\" "
                + "stroke-linejoin=\"round\" paint-order=\"stroke\">A</text></svg>");
        int stroke = content.indexOf("1 Tr\n");
        int fill = content.indexOf("0 Tr\n");
        assertTrue(stroke > 0 && fill > stroke, content);
        assertTrue(content.contains("1 1 1 RG\n3 w\n"), content);
    }

    @Test
    void aSetFontDrawsTheText() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        SVGImage image = image("<svg width=\"200\" height=\"100\"><text y=\"20\" font-weight=\"bold\">Hi</text></svg>");
        image.setFont(font);    // The bold text too, as no bold font is set
        image.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("> Tj\n"), content);
        assertFalse(content.contains("TJ"), content);
    }

    @Test
    void theTextOfADocumentOfPDFAOrPDFUAIsInEmbeddedFonts() throws Exception {
        // The fonts PDFjet carries are embedded, so the text needs no font set
        String svg = "<svg width=\"200\" height=\"100\"><text y=\"20\">A</text></svg>";
        for (Compliance compliance : new Compliance[] {Compliance.PDF_A_3B, Compliance.PDF_UA_1}) {
            PDF pdf = new PDF(new ByteArrayOutputStream(), compliance);
            pdf.setTitle("SVG");
            Page page = new Page(pdf, Letter.PORTRAIT);
            image(svg).setAltDescription("A").drawOn(page);
            pdf.complete();
        }
    }

    @Test
    void aDashArrayOfAnOddNumberOfLengthsIsRepeated() throws Exception {
        String content = draw("<svg width=\"200\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" "
                + "stroke-dasharray=\"4, 2 1\" stroke-dashoffset=\"1\"/>"
                + "<path d=\"M0 20 L100 20\" stroke=\"red\" stroke-dasharray=\"0 0\"/>"
                + "<path d=\"M0 30 L100 30\" stroke=\"red\" stroke-dasharray=\"4 -2\"/></svg>");
        assertTrue(content.contains("q\n[4 2 1 4 2 1] 1 d\n"), content);
        assertEquals(1, content.split(" d\n", -1).length - 1, content);    // The others are solid
    }

    @Test
    void aUseDrawsTheElementItNamesMovedByItsXAndY() throws Exception {
        String content = draw("<svg xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"200\" height=\"100\">"
                + "<defs><rect id=\"r\" width=\"10\" height=\"10\"/><symbol id=\"s\"><circle r=\"2\"/></symbol></defs>"
                + "<use href=\"#r\" x=\"20\" y=\"30\" fill=\"red\"/><use xlink:href=\"#s\" x=\"50\" y=\"50\"/>"
                + "<g id=\"loop\"><use href=\"#loop\"/></g></svg>");
        assertTrue(content.startsWith("1 0 0 rg\n20 762 m\n30 762 l\n"), content);
        assertTrue(content.contains("\n52 742 m\n"), content);
    }

    @Test
    void usesOfUsesThatDrawTooManyElementsThrow() {
        StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\"><rect id=\"g0\" width=\"1\" height=\"1\"/>");
        for (int level = 1; level <= 7; level++) {
            svg.append("<g id=\"g").append(level).append("\">");
            for (int i = 0; i < 10; i++) {
                svg.append("<use href=\"#g").append(level - 1).append("\"/>");
            }
            svg.append("</g>");
        }
        svg.append("</svg>");
        assertThrows(IllegalArgumentException.class, () -> image(svg.toString()));
    }

    @Test
    void aMarkerIsDrawnAtTheEndTurnedWithThePath() throws Exception {
        String content = draw("<svg width=\"200\" height=\"100\"><defs><marker id=\"m\" markerWidth=\"4\" "
                + "markerHeight=\"4\" orient=\"auto\" refX=\"1\"><rect width=\"2\" height=\"1\"/></marker></defs>"
                + "<path d=\"M10 10 L10 50\" stroke=\"black\" stroke-width=\"2\" marker-end=\"url(#m)\"/></svg>");
        // Turned 90 degrees down the path and twice the size, for the stroke
        // width: from 10, 48 to 8, 52.
        assertTrue(content.contains("S\n10 744 m\n10 740 l\n8 740 l\n8 744 l\nf\n"), content);
    }

    @Test
    void aClipPathCutsOffWhatIsOutsideIt() throws Exception {
        String content = draw("<svg width=\"200\" height=\"100\"><clipPath id=\"c\"><rect width=\"50\" height=\"50\"/>"
                + "</clipPath><g clip-path=\"url(#c)\" transform=\"translate(10,0)\"><rect x=\"10\" y=\"10\" width=\"80\" "
                + "height=\"20\"/></g><clipPath id=\"none\"/><rect width=\"5\" height=\"5\" clip-path=\"url(#none)\"/></svg>");
        // The clip path is in the user space of the group, moved with it.
        assertEquals("q\n10 792 m\n60 792 l\n60 742 l\n10 742 l\nh\nW\nn\n"
                + "0 0 0 rg\n20 782 m\n100 782 l\n100 762 l\n20 762 l\nf\nQ\n", content);
    }

    // An SVG of a chain of use elements, flat in the file.
    private static String useChain(int links) {
        StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\"><defs><rect id=\"u0\" width=\"1\" height=\"1\"/>");
        for (int i = 1; i <= links; i++) {
            svg.append("<use id=\"u").append(i).append("\" href=\"#u").append(i - 1).append("\"/>");
        }
        return svg.append("</defs><use href=\"#u").append(links).append("\"/></svg>").toString();
    }

    // An SVG of elements nested levels deep in the svg element, around the content.
    private static String nested(String open, String end, String content, int levels) {
        StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\">");
        for (int i = 0; i < levels; i++) {
            svg.append(open);
        }
        svg.append(content);
        for (int i = 0; i < levels; i++) {
            svg.append(end);
        }
        return svg.append("</svg>").toString();
    }

    // An SVG of markers each drawn by a path in the one before it.
    private static String markerChain(int links) {
        StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\">");
        for (int i = 0; i < links; i++) {
            svg.append("<marker id=\"m").append(i).append("\"><path d=\"M0 0 L1 1\" stroke=\"red\" marker-end=\"url(#m")
                    .append(i + 1).append(")\"/></marker>");
        }
        svg.append("<marker id=\"m").append(links).append("\"><rect width=\"1\" height=\"1\"/></marker>");
        return svg.append("<path d=\"M0 0 L1 1\" stroke=\"red\" marker-end=\"url(#m0)\"/></svg>").toString();
    }

    private static SVGImage read(String svg) throws Exception {
        return new SVGImage(new ByteArrayInputStream(svg.getBytes(StandardCharsets.UTF_8)));
    }

    private static String tspans(int levels) {
        return nested("<g>", "</g>", "<text>a" + nested("<tspan>b", "</tspan>", "", levels)
                .replace("<svg width=\"10\" height=\"10\">", "").replace("</svg>", "") + "</text>", 2);
    }

    @Test
    void theDepthOfWhatIsDrawnIsLimitedOnASmallStack() throws Exception {
        Throwable[] failure = new Throwable[1];
        Thread thread = new Thread(null, () -> {
            try {
                read(useChain(22));
                read(markerChain(14));
                read(nested("<g>", "</g>", "<rect width=\"1\" height=\"1\"/>", 46));
                read(tspans(43));
                for (String svg : new String[] {
                        useChain(23), useChain(2000), markerChain(15), tspans(44),
                        nested("<g>", "</g>", "<rect width=\"1\" height=\"1\"/>", 47), nested("<g>", "</g>", "", 250)}) {
                    IllegalArgumentException e = assertThrows(IllegalArgumentException.class, () -> read(svg));
                    assertTrue(e.getMessage().contains("deep"), e.getMessage());
                }
            } catch (Throwable e) {
                failure[0] = e;
            }
        }, "svg", 512*1024);
        thread.start();
        thread.join();
        if (failure[0] != null) {
            throw new AssertionError(failure[0]);
        }
    }

    @Test
    void manyGradientsOfOneGradientOfManyStopsAreReadOnce() throws Exception {
        StringBuilder svg = new StringBuilder("<svg width=\"10\" height=\"10\"><linearGradient id=\"s\">");
        for (int i = 0; i < 10000; i++) {
            svg.append("<stop stop-color=\"red\"/>");
        }
        svg.append("</linearGradient>");
        for (int i = 0; i < 10000; i++) {
            svg.append("<linearGradient id=\"g").append(i).append("\" href=\"#s\"/>");
        }
        svg.append("<rect width=\"5\" height=\"5\" fill=\"url(#g7)\"/></svg>");
        long start = System.nanoTime();
        String content = draw(svg.toString());
        long elapsed = (System.nanoTime() - start)/1000000;
        assertTrue(elapsed < 5000, elapsed + " ms");
        assertTrue(content.startsWith("1 0 0 rg\n"), content);
    }

    private static String repeat(String text, int count) {
        StringBuilder repeated = new StringBuilder();
        for (int i = 0; i < count; i++) {
            repeated.append(text);
        }
        return repeated.toString();
    }

    private static String polygon(int points) {
        return "<polygon id=\"p\" points=\"" + repeat("0,0 1,1 ", points/2) + "\"/>";
    }

    @Test
    void theOperationsDrawnAgainAreLimited() throws Exception {
        String clipped = "<svg width=\"10\" height=\"10\"><clipPath id=\"c\">" + polygon(10000) + "</clipPath>";
        for (String svg : new String[] {
                "<svg width=\"10\" height=\"10\"><defs>" + polygon(10000) + "</defs>"
                        + repeat("<use href=\"#p\"/>", 1000) + "</svg>",
                clipped + repeat("<g clip-path=\"url(#c)\"/>", 1000) + "</svg>",
                clipped + "<g clip-path=\"url(#c)\">" + repeat("<rect width=\"1\" height=\"1\"/>", 1000) + "</g></svg>",
                clipped + "<g clip-path=\"url(#c)\"><text>" + repeat("<tspan>a</tspan>", 1000) + "</text></g></svg>",
                "<svg width=\"10\" height=\"10\"><defs><text id=\"p\">" + repeat("a", 10000) + "</text></defs>"
                        + repeat("<use href=\"#p\"/>", 1000) + "</svg>",
                "<svg width=\"10\" height=\"10\"><clipPath id=\"c\"><text>" + repeat("a", 1000)
                        + "</text></clipPath><g clip-path=\"url(#c)\">" + repeat("<rect width=\"1\" height=\"1\"/>", 5000) + "</g></svg>",
                "<svg width=\"10\" height=\"10\"><clipPath id=\"c\"><text>a" + repeat("<tspan/>", 100000)
                        + "</text></clipPath>" + repeat("<g clip-path=\"url(#c)\"/>", 100) + "</svg>",
                nested("<svg width=\"1\" height=\"1\">", "</svg>", repeat("<rect width=\"1\" height=\"1\"/>", 20000), 45)}) {
            IllegalArgumentException e = assertThrows(IllegalArgumentException.class, () -> read(svg));
            assertTrue(e.getMessage().contains("path operations"), e.getMessage());
        }
        // A file drawn once is not counted, however large
        read("<svg width=\"10\" height=\"10\">" + repeat(polygon(10000), 500) + "</svg>");
        read("<svg width=\"10\" height=\"10\"><defs>" + polygon(1000) + "</defs>"
                + repeat("<use href=\"#p\"/>", 1000) + "</svg>");
    }

    @Test
    void aUseInAClipPathDrawsTheShapeItNames() throws Exception {
        {
            String content = draw("<svg width=\"200\" height=\"100\"><defs><rect id=\"r\" width=\"50\" height=\"50\"/></defs><clipPath id=\"c\"><use href=\"#r\" x=\"5\" y=\"5\" transform=\"translate(10,0)\"/></clipPath><g clip-path=\"url(#c)\"><rect x=\"10\" y=\"10\" width=\"80\" height=\"20\"/></g></svg>");
            assertEquals("q\n15 787 m\n65 787 l\n65 737 l\n15 737 l\nh\nW\nn\n0 0 0 rg\n10 782 m\n90 782 l\n90 762 l\n10 762 l\nf\nQ\n", content);
        }
        {
            String content = draw("<svg width=\"200\" height=\"100\"><clipPath id=\"c\"><use href=\"#missing\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            assertEquals("", content);
        }
    }

    @Test
    void theClipRuleOfAClipPathIsDrawn() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><path clip-rule=\"evenodd\" d=\"M0 0 H50 V50 H0 Z\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            assertTrue(content.contains("h\nW*\nn\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\" style=\"clip-rule:evenodd\"><rect width=\"50\" height=\"50\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            assertTrue(content.contains("h\nW*\nn\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"50\" height=\"50\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            assertTrue(content.contains("h\nW\nn\n"), content);
        }
    }

    @Test
    void aTextInAClipPathClips() throws Exception {
        String content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text x=\"10\" y=\"20\" font-size=\"12\">Hi</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>");
        assertTrue(content.startsWith("q\nBT\n7 Tr\n/"), content);
        assertTrue(content.contains(" 12 Tf\n1 0 0 1 10 772 Tm\n<"), content);
        assertTrue(content.contains("> Tj\nET\n0 0 0 rg\n0 792 m\n"), content);
        assertTrue(content.endsWith("f\nQ\n"), content);
        assertFalse(content.contains("W\n"), content);
        // A text drawn in it is drawn in the mode of its own.
        content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text y=\"20\">Hi</text></clipPath><text y=\"20\" clip-path=\"url(#c)\">Hi</text></svg>");
        assertTrue(content.contains("0 Tr\n1 0 0 1 0 772 Tm\n"), content);
        // All the texts are in one text object, each with its transform.
        content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\" transform=\"translate(5,0)\"><text x=\"1\" y=\"20\">a<tspan x=\"1\" y=\"40\" font-weight=\"bold\">b</tspan></text><text x=\"1\" y=\"60\" transform=\"scale(2)\">c</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>");
        assertEquals(2, content.split("BT\n", -1).length, content);
        assertEquals(4, content.split("> Tj\n", -1).length, content);
        assertEquals(4, content.split(" Tf\n", -1).length, content);
        assertTrue(content.contains("1 0 0 1 6 772 Tm\n"), content);
        assertTrue(content.contains("1 0 0 1 6 752 Tm\n"), content);
        assertTrue(content.contains("2 0 0 2 7 672 Tm\n"), content);
        // A use names a text.
        content = draw("<svg width=\"100\" height=\"100\"><defs><text id=\"t\" y=\"20\">Hi</text></defs><clipPath id=\"c\"><use href=\"#t\" x=\"5\"/></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>");
        assertTrue(content.contains("1 0 0 1 5 772 Tm\n"), content);
        // Shapes and texts: a PDF cannot have them in one path, and the shapes stay.
        content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text y=\"20\">Hi</text><rect width=\"5\" height=\"5\"/></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>");
        assertFalse(content.contains(" Tr\n"), content);
        assertTrue(content.contains("h\nW\nn\n"), content);
        // With no glyph it clips all away; out of the range of a PDF nothing is drawn.
        for (String text : new String[] {"<text y=\"20\"> </text>", "<text y=\"20\" font-size=\"0\">Hi</text>", "<text y=\"20\" display=\"none\">Hi</text>", "<text y=\"20\" visibility=\"hidden\">Hi</text>", "<text y=\"20\"><tspan visibility=\"hidden\">Hi</tspan></text>", "<text y=\"20\" transform=\"scale(0)\">Hi</text>", "<text y=\"20\" transform=\"scale(1e30)\">Hi</text>"}) {
            assertEquals("", draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\">" + text + "</clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>"), text);
        }
        // In the fill of no color, too: a clip path takes the shape of the glyphs.
        content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text y=\"20\" fill=\"none\">Hi</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>");
        assertTrue(content.contains("7 Tr\n"), content);
        // In the space of the image, and of its scale.
        SVGImage image = new SVGImage(new ByteArrayInputStream("<svg width=\"200\" height=\"200\" viewBox=\"0 0 100 100\"><clipPath id=\"c\"><text x=\"10\" y=\"20\" font-size=\"12\">Hi</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>".getBytes(StandardCharsets.UTF_8)));
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        image.setLocation(0f, 0f);
        image.drawOn(page);
        assertTrue(TestSupport.content(page).contains(" 12 Tf\n2 0 0 2 20 752 Tm\n"), TestSupport.content(page));
        image.scaleBy(0.5f);
        image.setLocation(10f, 0f);
        page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        image.drawOn(page);
        assertTrue(TestSupport.content(page).contains(" 12 Tf\n1 0 0 1 20 772 Tm\n"), TestSupport.content(page));
    }

    @Test
    void theShapesOfAClipPathGoTheSameWayRound() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><polygon points=\"0,0 20,0 20,20 0,20\"/><polygon clip-rule=\"evenodd\" points=\"10,10 10,30 30,30 30,10\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            assertTrue(content.contains("0 792 m\n20 792 l\n20 772 l\n0 772 l\nh\n10 782 m\n30 782 l\n30 762 l\n10 762 l\n10 782 l\nW\nn\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"20\" height=\"20\"/><path d=\"M50 0 C50 10 60 20 70 20 L70 0 Z\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            assertTrue(content.contains("50 792 m\n70 792 l\n70 772 l\n60 772 50 782 50 792 c\nW\nn\n"), content);
        }
    }

    @Test
    void theAlphaOfAColorIsDrawn() throws Exception {
        assertEquals(0.5f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgba(255,0,0,0.5)\"/></svg>").paths.get(0).fillAlpha, 0f);
        assertEquals(0.25f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgb(255 0 0 / 25%)\"/></svg>").paths.get(0).fillAlpha, 0f);
        assertEquals(0.25f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgba(255,0,0,0.5)\" fill-opacity=\"0.5\"/></svg>").paths.get(0).fillAlpha, 0f);
        assertEquals(0.2509804f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#ff000040\"/></svg>").paths.get(0).fillAlpha, 0f);
        assertEquals(0.26666668f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#f004\"/></svg>").paths.get(0).fillAlpha, 0f);
        assertEquals(0.5019608f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" stroke=\"#00ff0080\"/></svg>").paths.get(0).strokeAlpha, 0f);
        assertEquals(1.0f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\"/></svg>").paths.get(0).fillAlpha, 0f);
        assertEquals(0.5f, read("<svg width=\"100\" height=\"100\"><g fill=\"rgba(0,0,0,0.5)\"><path d=\"M0 0 L10 0 L10 10 Z\"/></g></svg>").paths.get(0).fillAlpha, 0f);
        assertEquals(0.5f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" color=\"rgba(0,0,255,0.5)\" fill=\"currentColor\"/></svg>").paths.get(0).fillAlpha, 0f);
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgba(0,0,0,0.5)\"/></svg>");
            assertTrue(content.startsWith("q\n/GS1 gs\n0 0 0 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#ff000040\"/></svg>");
            assertTrue(content.startsWith("q\n/GS1 gs\n1 0 0 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#f004\"/></svg>");
            assertTrue(content.startsWith("q\n/GS1 gs\n1 0 0 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#ff000000\"/></svg>");
            assertEquals("", content);
        }
    }

    @Test
    void hslColorsAreDrawn() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(120, 100%, 50%)\"/></svg>");
            assertTrue(content.startsWith("0 1 0 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(240deg 100% 50%)\"/></svg>");
            assertTrue(content.startsWith("0 0 1 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"HSL(-120, 100%, 50%)\"/></svg>");
            assertTrue(content.startsWith("0 0 1 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(0.5turn 100% 50%)\"/></svg>");
            assertTrue(content.startsWith("0 1 1 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(0, 0%, 100%)\"/></svg>");
            assertTrue(content.startsWith("1 1 1 rg\n"), content);
        }
        assertEquals(0.5f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsla(0, 100%, 50%, 0.5)\"/></svg>").paths.get(0).fillAlpha, 0f);
        assertEquals(0.25f, read("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(0 100% 50% / 25%)\"/></svg>").paths.get(0).fillAlpha, 0f);
    }

    @Test
    void theKeywordsOfAPaintAreInAnyCase() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" color=\"red\" fill=\"currentcolor\"/></svg>");
            assertTrue(content.startsWith("1 0 0 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" color=\"red\" fill=\"CURRENTCOLOR\"/></svg>");
            assertTrue(content.startsWith("1 0 0 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"NONE\"/></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"None\"/></svg>");
            assertEquals("", content);
        }
    }

    @Test
    void whatIsNotVisibleIsNotDrawn() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><g visibility=\"hidden\"><rect width=\"5\" height=\"5\"/></g></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><rect width=\"5\" height=\"5\" style=\"visibility:collapse\"/></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><g visibility=\"hidden\"><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\" visibility=\"visible\"/></g></svg>");
            assertTrue(content.startsWith("0 0 1 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><text visibility=\"hidden\" x=\"1\" y=\"50\">a</text></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"9\">a<tspan visibility=\"hidden\">b</tspan>c</text></svg>");
            assertEquals(2, content.split("BT\n", -1).length - 1, content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><defs><rect id=\"r\" width=\"5\" height=\"5\"/></defs><use href=\"#r\" visibility=\"hidden\"/></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"5\" height=\"5\" visibility=\"hidden\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><marker id=\"m\"><rect width=\"5\" height=\"5\"/></marker><path d=\"M0 0 L1 1\" stroke=\"red\" visibility=\"hidden\" marker-end=\"url(#m)\"/></svg>");
            assertEquals("", content);
        }
    }

    @Test
    void aSwitchDrawsTheFirstChildItCan() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>");
            assertEquals("1 0 0 rg\n0 792 m\n5 792 l\n5 787 l\n0 787 l\nf\n", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><switch><title>a</title><foreignObject width=\"5\" height=\"5\"><rect width=\"5\" height=\"5\" fill=\"blue\"/></foreignObject><g><rect width=\"5\" height=\"5\" fill=\"red\"/></g><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>");
            assertTrue(content.startsWith("1 0 0 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" requiredExtensions=\"http://a\" fill=\"blue\"/><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>");
            assertEquals(0, content.split("0 0 1 rg", -1).length - 1, content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" requiredExtensions=\"http://a\" fill=\"blue\"/><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>");
            assertEquals(1, content.split("1 0 0 rg", -1).length - 1, content);
        }
    }

    private static String drawInLanguage(String svg, String language) throws Exception {
        PDF pdf = TestSupport.newPDF();
        if (!language.isEmpty()) {
            pdf.setLanguage(language);
        }
        Page page = new Page(pdf, Letter.PORTRAIT);
        SVGImage image = new SVGImage(new ByteArrayInputStream(svg.getBytes(StandardCharsets.UTF_8)));
        image.setLocation(0f, 0f);
        image.drawOn(page);
        return TestSupport.content(page);
    }

    @Test
    void aSwitchHonorsTheSystemLanguage() throws Exception {
        String svg = "<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" systemLanguage=\"de, fr-CA\" fill=\"red\"/><rect width=\"5\" height=\"5\" systemLanguage=\"\" fill=\"lime\"/><g systemLanguage=\"EN-gb\"><rect width=\"5\" height=\"5\" fill=\"blue\"/></g><rect width=\"5\" height=\"5\" fill=\"black\"/></switch></svg>";
        String[][] tests = {{"", "0 0 1 rg"}, {"en-US", "0 0 1 rg"}, {"en", "0 0 1 rg"}, {"de", "1 0 0 rg"}, {"de-AT", "1 0 0 rg"}, {"FR", "1 0 0 rg"}, {"bg", "0 0 0 rg"}, {"fra", "0 0 0 rg"}};
        for (String[] test : tests) {
            String content = drawInLanguage(svg, test[0]);
            assertTrue(content.contains(test[1]), content);
            assertEquals(1, content.split(" rg\n", -1).length - 1, content);
        }
        // No child of the language, and none without a systemLanguage: nothing.
        assertEquals("", drawInLanguage("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" systemLanguage=\"de\"/></switch></svg>", ""));
        // One image, drawn in the language of each PDF.
        SVGImage image = new SVGImage(new ByteArrayInputStream(svg.getBytes(StandardCharsets.UTF_8)));
        String[][] languages = {{"de", "1 0 0 rg"}, {"en", "0 0 1 rg"}};
        for (String[] test : languages) {
            PDF pdf = TestSupport.newPDF();
            pdf.setLanguage(test[0]);
            Page page = new Page(pdf, Letter.PORTRAIT);
            image.drawOn(page);
            assertTrue(TestSupport.content(page).contains(test[1]), TestSupport.content(page));
        }
        // Texts, in a switch in a used group, nested, and a marker in a child.
        String content = drawInLanguage("<svg width=\"100\" height=\"100\"><defs><g id=\"g\"><switch><text systemLanguage=\"de\" y=\"10\" fill=\"red\">a</text><switch><text systemLanguage=\"en\" y=\"10\" fill=\"blue\">b</text></switch></switch></g></defs><use href=\"#g\"/></svg>", "en");
        assertTrue(content.contains("0 0 1 rg") && !content.contains("1 0 0 rg"), content);
        assertEquals("", drawInLanguage("<svg width=\"100\" height=\"100\"><marker id=\"m\"><rect width=\"5\" height=\"5\" fill=\"red\"/></marker><switch><path systemLanguage=\"de\" d=\"M0 0 L9 9\" stroke=\"blue\" marker-end=\"url(#m)\"/></switch></svg>", "en"));
        // What is not drawn leaves no cut, and no miter limit, for itself.
        content = drawInLanguage("<svg width=\"100\" height=\"100\"><switch><path systemLanguage=\"de\" d=\"M0 0 L900 900\" stroke=\"blue\"/><rect width=\"5\" height=\"5\"/></switch></svg>", "en");
        assertFalse(content.contains("W\n") || content.contains(" M\n"), content);
    }

    @Test
    void aUseGivesASymbolItsSize() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" x=\"5\" y=\"5\" width=\"20\" height=\"20\"/></svg>");
            assertTrue(content.contains("5 787 m\n25 787 l\n25 767 l\n5 767 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>");
            assertTrue(content.contains("0 782 m\n20 782 l\n20 762 l\n0 762 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMinYMin\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>");
            assertTrue(content.contains("0 792 m\n20 792 l\n20 772 l\n0 772 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"none\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>");
            assertTrue(content.contains("0 792 m\n20 792 l\n20 752 l\n0 752 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMaxYMax slice\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>");
            assertTrue(content.contains("-20 792 m\n20 792 l\n20 752 l\n-20 752 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\"/></svg>");
            assertTrue(content.contains("0 792 m\n100 792 l\n100 692 l\n0 692 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"0\" height=\"20\"/></svg>");
            assertEquals("", content);
        }
    }

    @Test
    void aNestedSvgIsAViewport() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><svg x=\"10\" y=\"20\" width=\"20\" height=\"20\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            assertTrue(content.contains("10 772 m\n30 772 l\n30 752 l\n10 752 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><svg x=\"10\" y=\"20\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            assertTrue(content.contains("10 772 m\n20 772 l\n20 762 l\n10 762 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><svg x=\"50%\" y=\"10%\" width=\"20\" height=\"20\" viewBox=\"5 5 10 10\"><rect x=\"5\" y=\"5\" width=\"10\" height=\"10\"/></svg></svg>");
            assertTrue(content.contains("50 782 m\n70 782 l\n70 762 l\n50 762 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\" viewBox=\"0 0 200 200\"><svg width=\"50%\" height=\"50%\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            assertTrue(content.contains("0 792 m\n50 792 l\n50 742 l\n0 742 l\n"), content);
        }
        {
            String content = draw("<svg><svg viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            assertTrue(content.contains("0 792 m\n10 792 l\n10 782 l\n0 782 l\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><svg width=\"0\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><svg viewBox=\"0 0 -10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>");
            assertEquals("", content);
        }
    }

    @Test
    void aSymbolAndANestedSvgClipToTheirViewports() throws Exception {
        final String clip = "h\nW\nn\n";
        assertEquals("q\n5 787 m\n25 787 l\n25 767 l\n5 767 l\nh\nW\nn\n0 0 0 rg\n5 787 m\n45 787 l\n45 747 l\n5 747 l\nf\nQ\n", draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"20\" height=\"20\"/></symbol><use href=\"#s\" x=\"5\" y=\"5\" width=\"20\" height=\"20\"/></svg>"));
        assertTrue(draw("<svg width=\"100\" height=\"100\"><svg x=\"10\" y=\"20\" width=\"20\" height=\"20\"><rect width=\"50\" height=\"50\"/></svg></svg>").startsWith("q\n10 772 m\n30 772 l\n30 752 l\n10 752 l\n" + clip));
        // The viewport, not the viewBox, and in the space of the image.
        assertTrue(draw("<svg width=\"100\" height=\"100\"><svg width=\"20\" height=\"40\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMaxYMax slice\"><rect width=\"10\" height=\"10\"/></svg></svg>").contains("q\n0 792 m\n20 792 l\n20 752 l\n0 752 l\n" + clip));
        assertTrue(draw("<svg width=\"200\" height=\"200\" viewBox=\"0 0 100 100\"><svg width=\"10\" height=\"10\"><rect width=\"5\" height=\"5\"/></svg></svg>").startsWith("q\n0 792 m\n20 792 l\n20 772 l\n0 772 l\n" + clip));
        // Moved and scaled by the scale of the image, too.
        SVGImage image = new SVGImage(new ByteArrayInputStream("<svg width=\"100\" height=\"100\"><svg width=\"10\" height=\"10\"><rect width=\"5\" height=\"5\"/></svg></svg>".getBytes(StandardCharsets.UTF_8)));
        image.scaleBy(2f);
        image.setLocation(10f, 0f);
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        image.drawOn(page);
        assertTrue(TestSupport.content(page).startsWith("q\n10 792 m\n30 792 l\n30 772 l\n10 772 l\n" + clip));
        // Each viewport clips, and an overflow is not inherited.
        assertEquals(3, draw("<svg width=\"100\" height=\"100\"><svg width=\"50\" height=\"50\"><svg width=\"20\" height=\"20\"><rect width=\"5\" height=\"5\"/></svg></svg></svg>").split(clip, -1).length);
        assertEquals(2, draw("<svg width=\"100\" height=\"100\"><svg width=\"50\" height=\"50\" overflow=\"visible\"><svg width=\"20\" height=\"20\"><rect width=\"5\" height=\"5\"/></svg></svg></svg>").split(clip, -1).length);
        // Overflow, as an attribute, in a style and in rules; hidden and scroll clip.
        for (String overflow : new String[] {"overflow=\"visible\"", "overflow=\"auto\"", "overflow=\"VISIBLE\"", "style=\"overflow: visible\"", "class=\"o\""}) {
            String content = draw("<svg width=\"100\" height=\"100\"><style>.o { overflow: visible }</style><symbol id=\"s\" " + overflow + "><rect width=\"500\" height=\"5\"/></symbol><use href=\"#s\"/></svg>");
            assertFalse(content.contains(clip), content);
        }
        for (String overflow : new String[] {"overflow=\"hidden\"", "overflow=\"scroll\""}) {
            String content = draw("<svg width=\"100\" height=\"100\"><svg width=\"5\" height=\"5\" " + overflow + "><rect width=\"5\" height=\"5\"/></svg></svg>");
            assertTrue(content.contains(clip), content);
        }
        // A viewport of no known size clips nothing, and one of no size hides.
        assertFalse(draw("<svg><svg x=\"1\"><rect width=\"5\" height=\"5\"/></svg></svg>").contains("W\n"));
        assertEquals("", draw("<svg width=\"100\" height=\"100\"><svg width=\"0\"><rect width=\"5\" height=\"5\"/></svg></svg>"));
    }

    @Test
    void thePaintOrderOfAShapeIsDrawn() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" paint-order=\"stroke\"/></svg>");
            assertTrue(content.contains("S\n1 0 0 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" style=\"paint-order: stroke fill markers\"/></svg>");
            assertTrue(content.contains("S\n1 0 0 rg\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" /></svg>");
            assertTrue(content.contains("f\n0 0 1 RG\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" paint-order=\"fill stroke\"/></svg>");
            assertTrue(content.contains("f\n0 0 1 RG\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" paint-order=\"normal\"/></svg>");
            assertTrue(content.contains("f\n0 0 1 RG\n"), content);
        }
    }

    @Test
    void theMiterLimitOfAStrokeIsWritten() throws Exception {
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" /></svg>");
            assertTrue(content.startsWith("q\n4 M\n1 0 0 RG\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"2.5\"/></svg>");
            assertTrue(content.startsWith("q\n2.5 M\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" style=\"stroke-miterlimit:1\"/></svg>");
            assertTrue(content.startsWith("q\n1 M\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"10\"/></svg>");
            assertTrue(content.startsWith("q\n10 M\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"0.5\"/></svg>");
            assertTrue(content.startsWith("q\n4 M\n1 0 0 RG\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"1e30\"/></svg>");
            assertTrue(content.startsWith("q\n4 M\n1 0 0 RG\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><g fill=\"none\" stroke=\"red\"><path d=\"M0 0 L10 0 L10 10 Z\"/><path d=\"M0 0 L9 0 L9 9 Z\"/></g></svg>");
            assertEquals(1, content.split(" M\n", -1).length - 1, content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-linejoin=\"round\"/></svg>");
            assertFalse(content.contains(" M\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\"/></svg>");
            assertFalse(content.contains(" M\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"50\" stroke=\"red\">a</text></svg>");
            assertTrue(content.startsWith("q\n4 M\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"50\" stroke=\"red\" stroke-miterlimit=\"3\">a</text></svg>");
            assertTrue(content.contains("3 M\n"), content);
        }
        {
            String content = drawAsItIs("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"50\">a</text></svg>");
            assertFalse(content.contains(" M\n"), content);
        }
    }

    @Test
    void aDashOffsetBelowZeroIsWrittenInsideThePattern() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"-1\"/></svg>");
            assertTrue(content.contains("[4 2] 5 d\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"-7\"/></svg>");
            assertTrue(content.contains("[4 2] 5 d\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"-6\"/></svg>");
            assertTrue(content.contains("[4 2] 0 d\n"), content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"7\"/></svg>");
            assertTrue(content.contains("[4 2] 7 d\n"), content);
        }
    }

    @Test
    void hugeDashesAndClipPathsSkipThePath() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"1e30 1\"/></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"1e30\"/></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"1e30 1e30\" stroke-dashoffset=\"-1\"/></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"1e30\" height=\"5\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"1e30\" height=\"5\"/></clipPath><text x=\"1\" y=\"50\" clip-path=\"url(#c)\">a</text></svg>");
            assertEquals("", content);
        }
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10 L0 20\" stroke-dasharray=\"1e30 1\"/></svg>");
            assertTrue(content.startsWith("0 0 0 rg\n"), content);
        }
    }

    @Test
    void aSizeInAUnitWithoutAViewBoxIsInPixels() throws Exception {
        {
            String content = draw("<svg width=\"75pt\" height=\"75pt\"><rect width=\"100\" height=\"100\"/></svg>");
            assertEquals("0 0 0 rg\n0 792 m\n75 792 l\n75 717 l\n0 717 l\nf\n", content);
        }
        {
            String content = draw("<svg width=\"1in\" height=\"1in\"><rect width=\"96\" height=\"96\"/></svg>");
            assertEquals("0 0 0 rg\n0 792 m\n72 792 l\n72 720 l\n0 720 l\nf\n", content);
        }
        {
            String content = draw("<svg width=\"10mm\" height=\"10mm\"><rect width=\"37.79\" height=\"37.79\"/></svg>");
            assertFalse(content.contains("W\n"), content);
        }
        assertEquals(75f, read("<svg width=\"75pt\" height=\"75pt\"><rect width=\"100\" height=\"100\"/></svg>").getWidth(), 0f);
        assertEquals(75f, read("<svg width=\"75pt\" height=\"75pt\"><rect width=\"100\" height=\"100\"/></svg>").getHeight(), 0f);
        {
            String content = draw("<svg width=\"100px\" height=\"100\"><rect width=\"100\" height=\"100\"/></svg>");
            assertEquals("0 0 0 rg\n0 792 m\n100 792 l\n100 692 l\n0 692 l\nf\n", content);
        }
        {
            String content = draw("<svg width=\"75pt\" height=\"75pt\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg>");
            assertEquals("0 0 0 rg\n0 792 m\n75 792 l\n75 717 l\n0 717 l\nf\n", content);
        }
        {
            String content = draw("<svg width=\"75pt\" height=\"75pt\"><rect width=\"10\" height=\"10\" fill=\"none\" stroke=\"red\" stroke-width=\"4\"/></svg>");
            assertTrue(content.contains("3 w\n"), content);
        }
    }

    @Test
    void aViewBoxOfNegativeSizeIsRefused() throws Exception {
        assertTrue(assertThrows(Exception.class, () -> read("<svg width=\"10\" height=\"10\" viewBox=\"0 0 -10 10\"/>")).getMessage().contains("have to be above zero"));
        assertTrue(assertThrows(Exception.class, () -> read("<svg width=\"10\" height=\"10\" viewBox=\"0 0 10 -10\"/>")).getMessage().contains("have to be above zero"));
    }

    @Test
    void aHueTooLargeForItsUnitIsNotAColor() throws Exception {
        {
            String content = draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(1e308turn, 100%, 50%)\"/></svg>");
            assertTrue(content.startsWith("0 0 0 rg\n"), content);
        }
    }
}
