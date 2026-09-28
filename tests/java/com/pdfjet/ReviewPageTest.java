/*
 * ReviewPageTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.barcodes.Barcode;
import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import org.junit.jupiter.api.Test;
import org.junit.jupiter.api.function.Executable;

/**
 * The drawing of a page: shapes, text lines, containers and annotations, as
 * the review of the page drawing found them.
 */
class ReviewPageTest {
    private static final float DELTA = TestSupport.DELTA;

    private static Page taggedPage() throws Exception {
        return new Page(new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1), Letter.PORTRAIT);
    }

    private static Page newPage() throws Exception {
        return new Page(TestSupport.newPDF(), Letter.PORTRAIT);
    }

    private static int count(String text, String part) {
        int n = 0;
        for (int i = text.indexOf(part); i != -1; i = text.indexOf(part, i + part.length())) {
            n++;
        }
        return n;
    }

    @Test
    void aPolygonInATurnedContainerIsTurned() throws Exception {
        Page page = newPage();
        float[] vertices = new float[] {0f, 0f, 20f, 0f, 0f, 10f};
        PolygonAnnotation polygon = new PolygonAnnotation();
        polygon.setLocation(10f, 10f);
        polygon.setVertices(vertices);
        Container container = new Container(100f, 100f);
        container.setLocation(100f, 100f);
        container.setRotation(90);
        container.add(polygon);
        // Drawn twice, the container turns the polygon the same both times
        container.drawOn(page);
        container.drawOn(page);
        for (Annotation annot : page.annots) {
            // The first vertex is 40 left of and above the center, 150 and
            // 150, and a quarter turn clockwise puts it 40 right of it and above it.
            assertEquals(190f, annot.x1, DELTA);
            assertEquals(792f - 110f, annot.y1, DELTA);
            assertArrayEquals(new float[] {0f, 0f, 0f, 20f, -10f, 0f}, annot.vertices, DELTA);
        }
        assertEquals(20f, vertices[2]);
        assertEquals(0f, vertices[3]);
    }

    @Test
    void aPointTwiceInAPathIsOffsetOnce() throws Exception {
        Page page = newPage();
        Point point = new Point(10f, 10f);
        Path path = new Path();
        path.add(point);
        path.add(new Point(50f, 10f));
        path.add(point);
        path.setLocation(100f, 0f);
        float[] xy = path.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("110 782 m\n150 782 l\n110 782 l\n"), content);
        assertEquals(10f, point.x);
        assertEquals(10f, point.y);
        TestSupport.assertXY(150f, 10f, xy);
    }

    @Test
    void aPathThatEndsOnAControlPointIsRefused() throws Exception {
        final Page page = newPage();
        final List<Point> path = Arrays.asList(
                new Point(10f, 10f),
                new Point(20f, 20f, Point.CONTROL_POINT_C),
                new Point(30f, 20f, Point.CONTROL_POINT_C));
        String message = assertThrows(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { page.drawPath(path, PathOperator.STROKE); }
        }).getMessage();
        assertEquals(Page.PATH_ENDS_ON_CONTROL_POINT, message);
        assertEquals("", TestSupport.content(page));

        final Stamp stamp = new Stamp(TestSupport.newPDF()).setSize(50f, 50f);
        message = assertThrows(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { stamp.drawPath(path, PathOperator.STROKE); }
        }).getMessage();
        assertEquals(Page.PATH_ENDS_ON_CONTROL_POINT, message);
    }

    @Test
    void theLinkOfATurnedTextLineCoversTheText() throws Exception {
        Page page = newPage();
        Font font = TestSupport.helvetica(page.pdf);
        TextLine line = new TextLine(font, "Turned");
        line.setTextRotation(90);
        line.setURIAction("https://pdfjet.com");
        line.setLocation(100f, 200f);
        line.drawOn(page);
        Annotation annot = page.annots.get(0);
        // A quarter turn clockwise runs the text down the page from its location.
        assertEquals(100f - font.getDescent(12f), annot.x1, DELTA);
        assertEquals(100f + font.getAscent(12f), annot.x2, DELTA);
        assertEquals(792f - 200f, annot.y1, DELTA);
        assertEquals(792f - (200f + font.stringWidth(12f, "Turned")), annot.y2, DELTA);
    }

    @Test
    void aSuperscriptIsRaisedOnce() throws Exception {
        Page page = newPage();
        Font font = TestSupport.helvetica(page.pdf);
        CompositeTextLine composite = new CompositeTextLine(100f, 100f);
        composite.setFontSize(12f);
        composite.addFormula(font, "x^2");
        composite.drawOn(page);
        TextLine two = composite.getTextLine(1);
        // Raised by the superscript position of the base font size, 0.35 of 12
        assertEquals(792f - (100f - 4.2f), TestSupport.positionOf(TestSupport.content(page), "2")[1], DELTA);
        // Measured where it is drawn, the superscript reaches no higher than the x
        float top = Math.min(100f - font.getAscent(12f), 100f - 4.2f - font.getAscent(two.getFontSize()));
        assertEquals(top, composite.getMinMaxY()[0], DELTA);
        assertEquals(100f - top, composite.getAscent(), DELTA);
        assertEquals(composite.getWidth() + 100f, composite.drawOn(null)[0], DELTA);
        assertEquals(100f - 4.2f, two.drawOn(null)[1], DELTA);
    }

    @Test
    void aHighlightedWordKeepsItsCombiningMarks() throws Exception {
        Page page = newPage();
        Font font = TestSupport.helvetica(page.pdf);
        TextLine line = new TextLine(font, "the cafe\u0301 is open");
        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("cafe\u0301", Color.red);
        line.setHighlightColors(colors);
        line.setLocation(100f, 100f);
        line.drawOn(page);
        // The mark is drawn with its letter, in the color of the word; a core
        // font has no mark, and draws a space for it.
        assertEquals("1 0 0 rg", TestSupport.fillColorBefore(TestSupport.content(page), "cafe "),
                TestSupport.content(page));
    }

    @Test
    void aShapeWithNoDescriptionIsAnArtifact() throws Exception {
        Page page = taggedPage();
        new Line(10f, 10f, 100f, 10f).drawOn(page);
        Arc arc = new Arc();
        arc.setLocation(50f, 50f);
        arc.setRadius(10f);
        arc.setSweep(90f);
        arc.drawOn(page);
        new CheckBox(TestSupport.helvetica(page.pdf), "").setLocation(10f, 100f).drawOn(page);
        Stamp stamp = new Stamp(page.pdf).setSize(20f, 20f);
        stamp.drawRect(0f, 0f, 20f, 20f);
        stamp.complete();
        stamp.setLocation(200f, 200f).drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains("/P <<"), content);
        assertEquals(4, count(content, "/Artifact BMC"), content);

        // Described, a line is read
        Page page2 = taggedPage();
        new Line(10f, 10f, 100f, 10f).setAltDescription("A rule").drawOn(page2);
        assertTrue(TestSupport.content(page2).contains("/P <</MCID 0>>"), TestSupport.content(page2));
    }

    @Test
    void aRunningFooterAndAWatermarkAreArtifacts() throws Exception {
        Page page = taggedPage();
        Font font = TestSupport.helvetica(page.pdf);
        page.addFooter(new TextLine(font, "Page 1"));
        page.addHeader(new TextLine(font, "Report"));
        page.addWatermark(font, "DRAFT");
        String content = TestSupport.content(page);
        for (String subtype : new String[] {"Footer", "Header", "Watermark"}) {
            assertTrue(content.contains("/Artifact <</Type /Pagination /Subtype /" + subtype + ">> BDC\n"), content);
        }
        assertFalse(content.contains("/P <<"), content);
    }

    @Test
    void aRotationOfOneDegreeIsWrittenAsOne() throws Exception {
        Page page = newPage();
        Font font = TestSupport.helvetica(page.pdf);
        TextLine line = new TextLine(font, "Turned");
        line.setTextRotation(1);
        line.setLocation(100f, 100f);
        line.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("0.99985 -0.01745 0.01745 0.99985 100 692 Tm\n"), content);

        Page page2 = newPage();
        Container container = new Container(10f, 10f);
        container.setRotation(1);
        container.drawOn(page2);
        content = TestSupport.content(page2);
        assertTrue(content.contains("0.99985 -0.01745 0.01745 0.99985 0 0 cm\n"), content);
    }

    @Test
    void preciseNumbersHaveFiveDecimals() {
        assertEquals("0.01745", TestSupport.latin1(FastFloat.toPreciseByteArray((float) Math.sin(Math.PI / 180))));
        assertEquals("0.99985", TestSupport.latin1(FastFloat.toPreciseByteArray((float) Math.cos(Math.PI / 180))));
        assertEquals("-0.70711", TestSupport.latin1(FastFloat.toPreciseByteArray((float) -Math.sqrt(0.5))));
        assertEquals("1", TestSupport.latin1(FastFloat.toPreciseByteArray(1f)));
        assertEquals("-1", TestSupport.latin1(FastFloat.toPreciseByteArray(-1f)));
        assertEquals("0.5", TestSupport.latin1(FastFloat.toPreciseByteArray(0.5f)));
        assertEquals("0.0001", TestSupport.latin1(FastFloat.toPreciseByteArray(0.0001f)));
        assertEquals("0", TestSupport.latin1(FastFloat.toPreciseByteArray((float) Math.cos(Math.PI / 2))));
        assertEquals("0", TestSupport.latin1(FastFloat.toPreciseByteArray(-0f)));
        assertEquals("0", TestSupport.latin1(FastFloat.toPreciseByteArray(-0.000004f)));
        assertEquals("12.25", TestSupport.latin1(FastFloat.toPreciseByteArray(12.25f)));
        assertEquals("0", TestSupport.latin1(FastFloat.toPreciseByteArray(Float.NaN)));
    }

    @Test
    void theSizeOfAnAnnotationFollowsItsLocation() throws Exception {
        Page page = newPage();
        SquareAnnotation square = new SquareAnnotation();
        square.setSize(60f, 30f);
        square.setLocation(100f, 200f);
        TestSupport.assertXY(160f, 230f, square.drawOn(page));
        Annotation annot = page.annots.get(0);
        assertEquals(160f, annot.x2, DELTA);
        assertEquals(792f - 230f, annot.y2, DELTA);
    }

    @Test
    void aCheckBoxIsTheSizeOfItsFont() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        CheckBox checkBox = new CheckBox(font, "Yes");
        checkBox.setFontSize(24f);
        checkBox.setLocation(100f, 100f);
        float[] xy = checkBox.drawOn(null);
        assertEquals(100f + 3f*font.getAscent(24f) + font.stringWidth(24f, "Yes"), xy[0], DELTA);
    }

    @Test
    void shapesLeaveThePenAsTheyFoundIt() throws Exception {
        Page page = newPage();
        Font font = TestSupport.helvetica(page.pdf);
        page.setPenWidth(2f);
        page.setPenColor(Color.red);
        Path path = new Path();
        path.add(new Point(10f, 10f));
        path.add(new Point(20f, 20f));
        path.setStrokeWidth(5f);
        path.drawOn(page);
        TextLine line = new TextLine(font, "Underlined");
        line.setUnderline(true);
        line.setStrikeout(true);
        line.setLocation(10f, 50f);
        line.drawOn(page);
        new CheckBox(font, "Check").setLocation(10f, 80f).drawOn(page);
        new RadioButton(font, "Radio").setLocation(10f, 110f).drawOn(page);
        assertEquals(2f, page.getPenWidth());
        TestSupport.assertRGB(1f, 0f, 0f, page.getPenColor());
        String content = TestSupport.content(page);
        assertEquals(4, count(content, "q\n"), content);
        assertEquals(4, count(content, "Q\n"), content);
    }

    @Test
    void anArcOfNoSweepOrOfTooMuchIsBounded() throws Exception {
        Page page = newPage();
        final Arc arc = new Arc();
        arc.setLocation(50f, 50f);
        arc.setRadius(10f);
        arc.setSweep(0f);
        arc.drawOn(page);
        assertEquals("", TestSupport.content(page));
        // A sweep of a billion degrees is a full turn, four curves
        arc.setSweep(1e9f);
        arc.drawOn(page);
        assertEquals(4, count(TestSupport.content(page), " c\n"));

        final Page page2 = newPage();
        for (final float sweep : new float[] {Float.NaN, Float.POSITIVE_INFINITY}) {
            arc.setSweep(sweep);
            String message = assertThrows(IllegalArgumentException.class, new Executable() {
                public void execute() throws Throwable { arc.drawOn(page2); }
            }).getMessage();
            assertEquals("The sweep of an arc must be a finite number of degrees.", message);
            assertThrows(IllegalArgumentException.class, new Executable() {
                public void execute() throws Throwable { page2.addArcToPath(50f, 50f, 10f, 10f, 0f, sweep); }
            });
        }
        assertEquals("", TestSupport.content(page2));
    }

    @Test
    void theBoundingBoxAfterAFigureIsNotItsOwn() throws Exception {
        Page page = taggedPage();
        page.addBDC(StructElem.FIGURE, null, null, "A figure");
        page.setFigureBoundingBox(10f, 10f, 20f, 20f);
        page.addEMC();
        page.setFigureBoundingBox(50f, 50f, 5f, 5f);
        assertEquals("<</O /Layout /BBox [10 762 30 782]>>", page.structures.get(0).attributes);
    }

    @Test
    void anEmptyTextLineAddsItsDestination() throws Exception {
        Page page = newPage();
        TextLine line = new TextLine(TestSupport.helvetica(page.pdf), "");
        line.setDestination("top");
        line.setLocation(10f, 100f);
        line.drawOn(page);
        assertEquals(1, page.destinations.size());
        assertEquals("top", page.destinations.get(0).name);
    }

    @Test
    void aFormWithoutItsFontsIsRefused() throws Exception {
        final Page page = newPage();
        List<Field> fields = new ArrayList<Field>();
        fields.add(new Field(0f, "Name", "Value"));
        final Form form = new Form(fields);
        form.setLocation(10f, 10f);
        String message = assertThrows(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { form.drawOn(page); }
        }).getMessage();
        assertEquals("A form needs a label font and a value font: setLabelFont and setValueFont.", message);
    }

    @Test
    void theLinksInNestedContainersAreWhereTheirTextIs() throws Exception {
        Page page = newPage();
        Font font = TestSupport.helvetica(page.pdf);
        TextLine line = new TextLine(font, "Link");
        line.setURIAction("https://pdfjet.com");
        line.setLocation(1f, 20f);
        Container inner = new Container(50f, 50f);
        inner.setLocation(5f, 5f);
        inner.add(line);
        Container middle = new Container(100f, 100f);
        middle.setLocation(10f, 10f);
        middle.add(inner);
        Container outer = new Container(200f, 200f);
        outer.setLocation(100f, 100f);
        outer.add(middle);
        outer.drawOn(page);
        Annotation annot = page.annots.get(0);
        assertEquals(116f, annot.x1, DELTA);
        assertEquals(792f - (135f - font.getAscent(12f)), annot.y1, DELTA);

        // A figure is where it is drawn
        Page page2 = taggedPage();
        Barcode barcode = new Barcode(Barcode.CODE_128, "12345");
        barcode.setAltDescription("12345");
        barcode.setLocation(110f, 110f);
        barcode.drawOn(page2);
        Page page3 = taggedPage();
        barcode = new Barcode(Barcode.CODE_128, "12345");
        barcode.setAltDescription("12345");
        barcode.setLocation(10f, 10f);
        Container container = new Container(100f, 100f);
        container.add(barcode);
        container.setLocation(100f, 100f);
        container.drawOn(page3);
        assertEquals(page2.structures.get(0).attributes, page3.structures.get(0).attributes);

        // Turned a quarter, the link of a text line is turned with it
        Page page4 = new Page(page.pdf, Letter.PORTRAIT);
        TextLine link = new TextLine(font, "Link");
        link.setURIAction("https://pdfjet.com");
        link.setLocation(0f, 50f);
        Container turned = new Container(100f, 100f);
        turned.setLocation(100f, 100f);
        turned.setRotation(90);
        turned.add(link);
        turned.drawOn(page4);
        annot = page4.annots.get(0);
        // The text runs down the page, from 50 left of the center to 50 right of it
        assertEquals(150f - font.getDescent(12f), annot.x1, DELTA);
        assertEquals(792f - 100f, annot.y1, DELTA);
        assertEquals(150f + font.getAscent(12f), annot.x2, DELTA);
        assertEquals(792f - (100f + font.stringWidth(12f, "Link")), annot.y2, DELTA);
    }

    @Test
    void aDashPatternWithAVerticalTabIsRefused() throws Exception {
        final Page page = newPage();
        String message = assertThrows(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { page.setStrokeDashPattern("[3\u000B3] 0"); }
        }).getMessage();
        assertEquals("The dash pattern \"[3\u000B3] 0\" is not an array of non-negative numbers, "
                + "not all zero, followed by a phase, such as \"[3 3] 0\".", message);
    }

    @Test
    void theSpacesOfUnicodeAreSpaces() throws Exception {
        final Page page = taggedPage();
        Font font = TestSupport.helvetica(page.pdf);
        TextLine line = new TextLine(font, "\u3000Annual\u3000 report\u00a0");
        line.setStructureType(StructElem.H1);
        line.setLocation(10f, 50f);
        line.drawOn(page);
        assertEquals("Annual report", page.pdf.headings.get(0).title);
        String message = assertThrows(IllegalStateException.class, new Executable() {
            public void execute() throws Throwable { page.addBDC(StructElem.FIGURE, null, null, "\u3000"); }
        }).getMessage();
        assertEquals("A figure of a tagged document, PDF/UA or PDF/A of level A, needs an alternative description.",
                message);
    }

    @Test
    void anEmptyURIIsNoLink() throws Exception {
        Page page = newPage();
        Font font = TestSupport.helvetica(page.pdf);
        TextLine line = new TextLine(font, "Text");
        line.setURIAction("");
        line.setGoToAction("");
        line.setLocation(10f, 50f);
        line.drawOn(page);
        TextBlock block = new TextBlock(font, "Text");
        block.setURIAction("");
        block.setLocation(10f, 100f);
        block.drawOn(page);
        assertEquals(0, page.annots.size());
    }

    @Test
    void aLoneSurrogateIsDrawnAsNotdef() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.otf.stream"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine line = new TextLine(font, "a\uD800b");
        line.setLocation(10f, 50f);
        line.drawOn(page);
        // The glyph of the font's .notdef, with the surrogate as its actual text
        String content = TestSupport.content(page);
        assertTrue(content.contains("<0001> Tj\n/Span <</ActualText <FEFFD800>>> BDC\n<0000> Tj\nEMC\n<0003> Tj\n"),
                content);
    }

    @Test
    void aKeywordIsLowerCasedWhateverTheLocale() throws Exception {
        java.util.Locale locale = java.util.Locale.getDefault();
        java.util.Locale.setDefault(new java.util.Locale("tr", "TR"));
        try {
            Page page = newPage();
            Font font = TestSupport.helvetica(page.pdf);
            TextLine line = new TextLine(font, "LINK");
            Map<String, Integer> colors = new HashMap<String, Integer>();
            colors.put("link", Color.red);
            line.setHighlightColors(colors);
            line.setLocation(10f, 50f);
            line.drawOn(page);
            assertEquals("1 0 0 rg", TestSupport.fillColorBefore(TestSupport.content(page), "LINK"));
        } finally {
            java.util.Locale.setDefault(locale);
        }
    }

    @Test
    void aKeywordIsMatchedByItsCodePoints() throws Exception {
        // A keyword of an e with an acute accent is not the same word as one
        // of an e and a combining accent.
        Page page = newPage();
        Font font = TestSupport.helvetica(page.pdf);
        TextLine line = new TextLine(font, "the cafe\u0301 is open");
        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("caf\u00e9", Color.red);
        line.setHighlightColors(colors);
        line.setLocation(100f, 100f);
        line.drawOn(page);
        assertEquals("0 0 0 rg", TestSupport.fillColorBefore(TestSupport.content(page), "cafe "));

        // Nor is a description of the one the text of the other
        Page page2 = taggedPage();
        TextLine described = new TextLine(TestSupport.helvetica(page2.pdf), "cafe\u0301");
        described.setAltDescription("caf\u00e9");
        described.setLocation(100f, 100f);
        described.drawOn(page2);
        assertEquals("caf\u00e9", page2.structures.get(0).altDescription);
    }

    @Test
    void aShortColorOrTransformIsRefused() throws Exception {
        final Page page = newPage();
        String message = assertThrows(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { page.setPenColor(new float[] {1f, 0f}); }
        }).getMessage();
        assertEquals(Page.RGB_COUNT, message);
        final Page page2 = newPage();
        message = assertThrows(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { page2.setBrushColor(new float[] {1f}); }
        }).getMessage();
        assertEquals(Page.RGB_COUNT, message);
        final Page page3 = newPage();
        message = assertThrows(IllegalArgumentException.class, new Executable() {
            public void execute() throws Throwable { page3.transform(new float[] {1f, 0f, 0f}); }
        }).getMessage();
        assertEquals(Page.TRANSFORM_COUNT, message);
        assertEquals("", TestSupport.content(page3));
        // The most negative and the most positive angles are angles too
        Page page4 = newPage();
        page4.setTextRotation(Integer.MIN_VALUE);
        page4.setTextRotation(Integer.MAX_VALUE);
    }
}
