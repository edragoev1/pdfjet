/*
 * ReviewRound3Test.java
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

import java.io.InputStream;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.Test;

/** The findings of the third review, of 12 October 2026, as tests. */
class ReviewRound3Test {
    /**
     * A text rendering mode set back to 0 is written: Tr lasts past ET, and
     * the text after an invisible line stayed invisible (F1).
     */
    @Test
    void theTextRenderingModeIsSetBackToZero() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "plain").setLocation(50f, 50f).drawOn(page);
        assertFalse(TestSupport.content(page).contains(" Tr\n"));
        page.setTextRenderingMode(3);
        new TextLine(font, "hidden").setLocation(50f, 100f).drawOn(page);
        page.setTextRenderingMode(0);
        new TextLine(font, "VISIBLE AGAIN").setLocation(50f, 150f).drawOn(page);
        String content = TestSupport.content(page);
        String before = content.substring(0, content.indexOf(TestSupport.hex("VISIBLE AGAIN")));
        int zero = before.lastIndexOf("0 Tr\n");
        assertTrue(zero != -1 && before.lastIndexOf("3 Tr\n") < zero, content);
    }

    /**
     * A text frame at NaN, or a Markdown text with NaN margins, is refused:
     * no word was placed, and pages were made until the memory ran out (F4).
     */
    @Test
    void aNaNLocationOrMarginIsRefused() throws Exception {
        float[][] cases = {
            {Float.NaN, 50f, 200f, 0f}, {50f, Float.NaN, 200f, 0f},
            {50f, 50f, Float.NaN, 0f}, {50f, 50f, 200f, Float.POSITIVE_INFINITY}};
        for (float[] xywh : cases) {
            final PDF pdf = TestSupport.newPDF();
            final TextFrame frame = new TextFrame(TestSupport.helvetica(pdf), Arrays.asList("Hello frame text"))
                    .setWidth(xywh[2]).setHeight(xywh[3]);
            frame.setLocation(xywh[0], xywh[1]);
            final List<Page> pages = new ArrayList<Page>();
            Exception e = assertThrows(IllegalArgumentException.class, () -> frame.drawOn(pdf, pages, Letter.PORTRAIT));
            assertEquals("The text frame's location and size must be numbers, not NaN or infinite.", e.getMessage());
            assertEquals(0, pages.size());
        }
        for (float margin : new float[] {Float.NaN, Float.POSITIVE_INFINITY}) {
            final PDF pdf = TestSupport.newPDF();
            Font font = TestSupport.helvetica(pdf);
            final Markdown markdown = new Markdown(font, font, font, font, font).setMargins(margin, 72f, 72f, 72f);
            final List<Page> pages = new ArrayList<Page>();
            Exception e = assertThrows(IllegalArgumentException.class,
                    () -> markdown.drawOn(pdf, "Some *text*.", pages, Letter.PORTRAIT));
            assertEquals("The margins of the Markdown must be numbers, not NaN or infinite.", e.getMessage());
            assertEquals(0, pages.size());
        }
    }

    /**
     * The rows of Markdown tables past 100,000 cells are counted, not kept:
     * each row was padded to the header's 64 columns first, and 2 MB of rows
     * of "|" made 1.65 GB of cells before the document was refused (C2).
     */
    @Test
    void theRowsPastTheTableLimitAreNotKept() throws Exception {
        StringBuilder text = new StringBuilder("|");
        for (int i = 0; i < 64; i++) {
            text.append(" a |");
        }
        text.append("\n|");
        for (int i = 0; i < 64; i++) {
            text.append("---|");
        }
        text.append("\n");
        for (int i = 0; i < 3000; i++) {
            text.append("|\n");
        }
        List<MarkdownParser.Block> blocks = MarkdownParser.parse(text.toString());
        assertTrue(blocks.get(0).rows.size() <= 100000 / 64 + 1, blocks.get(0).rows.size() + " rows kept");
        assertEquals(64 * 3001, MarkdownParser.tableCells(blocks));
        final PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        final Markdown markdown = new Markdown(font, font, font, font, font);
        final String markdownText = text.toString();
        Exception e = assertThrows(IllegalArgumentException.class,
                () -> markdown.drawOn(pdf, markdownText, new ArrayList<Page>(), Letter.PORTRAIT));
        assertEquals("The tables of the Markdown have more than 100000 cells: draw a table that large with BigTable.",
                e.getMessage());
    }

    /**
     * Two characters copied as the same text, as a letter and its
     * presentation form, need no actual text for the second: Arabic text had
     * three times the spans it needed, its content 27% larger (T2).
     */
    @Test
    void aCharacterCopiedAsTheFirstHasNoActualText() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"));
        assertEquals(font.unicodeToGID.get(0x0644), font.unicodeToGID.get(0xFEDD));
        font.useChar(0xFEDD, font.unicodeToGID.get(0xFEDD));
        assertFalse(font.drawnAsAnother(0x0644));
        font.useChar(0x0661, font.unicodeToGID.get(0x0661));
        assertTrue(font.drawnAsAnother(0x06F1));
    }

    /**
     * A glyph drawn for another character stays in the span of a word with
     * moved marks, whose actual text gives the character: in a span of its
     * own, the word's span began with a moved mark and the space that stands
     * for it, and "ةِمى" was copied as "ىمِ ة" (T1).
     */
    @Test
    void aWordWithMarksIsCopiedWithoutASpaceInside() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/NotoSansArabic/NotoSansArabic-Regular.ttf"));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "\u0628").setLocation(50f, 50f).drawOn(page);
        // The glyph of ة as drawn first for another character, which no two
        // characters of this font copied as different text are
        font.program.firstChar(font.unicodeToGID.get(0x0629), 'X');
        new TextLine(font, "\u0629\u0650\u0645\u0649").setLocation(50f, 100f).drawOn(page);
        String content = TestSupport.content(page);
        assertFalse(content.contains("/ActualText <FEFF0020"), content);
        assertTrue(content.contains("/ActualText <" + Page.toUTF16Hex("\u0629\u0650\u0645\u0649") + ">"), content);
    }

    /**
     * A cell's word broken between its characters breaks again at a soft
     * hyphen, as TextBlock and TextFrame do: "1234<SHY>5678" was
     * 123 / 4<SHY>56 / 78 (T4).
     */
    @Test
    void aCellBreaksAtASoftHyphenAfterACharacterBreak() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        java.lang.reflect.Method wrapCellText = Table.class.getDeclaredMethod("wrapCellText", List.class, int.class);
        wrapCellText.setAccessible(true);
        Cell cell = new Cell(font, "1234\u00AD5678");
        cell.setWidth(font.stringWidth("123"));
        cell.setLeftPadding(0f);
        cell.setRightPadding(0f);
        List<String> lines = new ArrayList<String>();
        @SuppressWarnings("unchecked")
        List<String> wrapped = (List<String>) wrapCellText.invoke(new Table(), Arrays.asList(cell), 0);
        for (String line : wrapped) {
            lines.add(line.replace("\u00AD", ""));
        }
        assertEquals(Arrays.asList("123", "4-", "567", "8"), lines);
        Cell wide = new Cell(font, "W");
        wide.setWidth(1f);
        assertEquals(Arrays.asList("W"), wrapCellText.invoke(new Table(), Arrays.asList(wide), 0));
    }

    /**
     * A CJK font fits all of a text in a width of more characters than it
     * has, as 1e30 or an infinity, in every port: Go and C# fit none (T6).
     */
    @Test
    void aCJKFontFitsAllOfATextInAHugeWidth() throws Exception {
        Font font = new Font(TestSupport.newPDF(), CJKFont.ADOBE_MING_STD_LIGHT);
        String text = "\u4E2D\u6587\u5B57";
        for (float width : new float[] {1e30f, Float.POSITIVE_INFINITY, Float.MAX_VALUE}) {
            assertEquals(3, font.getFitChars(text, width), "width " + width);
        }
        for (float width : new float[] {Float.NaN, -1f, 0f}) {
            assertEquals(0, font.getFitChars(text, width), "width " + width);
        }
        assertEquals(2, font.getFitChars(text, 2f * font.getSize()));
    }

    /**
     * A zero width joiner the font has no glyph for, in left to right text
     * between two LRMs, is in the actual text of the run, as the stamp keeps
     * it: it was left out (T7).
     */
    @Test
    void aJoinerInARunIsInItsActualText() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = new Font(pdf, TestSupport.open("fonts/NotoSansJP/NotoSansJP-Regular.ttf"));
        assertEquals(0, font.unicodeToGID.get(0x200D));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "d\u200D\u200E\u200Ce\u200D").setLocation(50f, 50f).drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("/ActualText <" + Page.toUTF16Hex("\u200Ee\u200D\u200E") + ">"), content);
    }

    private static long allocated(Runnable run) {
        java.lang.management.ThreadMXBean bean = java.lang.management.ManagementFactory.getThreadMXBean();
        if (!(bean instanceof com.sun.management.ThreadMXBean)
                || !((com.sun.management.ThreadMXBean) bean).isThreadAllocatedMemorySupported()) {
            return 0;
        }
        com.sun.management.ThreadMXBean threads = (com.sun.management.ThreadMXBean) bean;
        threads.setThreadAllocatedMemoryEnabled(true);
        long id = Thread.currentThread().getId();
        long before = threads.getThreadAllocatedBytes(id);
        run.run();
        return threads.getThreadAllocatedBytes(id) - before;
    }

    /**
     * A /MediaBox that refers to an object of more than a box is not read:
     * 1,000 pages over an array of a million numbers, a PDF of 2 MB, copied
     * the array for each page, 15 GB in Go (N1).
     */
    @Test
    void aMediaBoxOfAnotherObjectIsReadOnlyWhenItIsABox() throws Exception {
        int pages = 200;
        StringBuilder kids = new StringBuilder();
        for (int i = 0; i < pages; i++) {
            kids.append(5 + i).append(" 0 R ");
        }
        StringBuilder numbers = new StringBuilder("[");
        for (int i = 0; i < 100000; i++) {
            numbers.append("0 ");
        }
        List<String> objects = new ArrayList<String>(Arrays.asList(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [" + kids + "] /Count " + pages + " >>",
                numbers.append("]").toString(),
                "[0 0 612 792]"));
        for (int i = 0; i < pages; i++) {
            objects.add("<< /Type /Page /Parent 2 0 R /MediaBox " + ((i == 0) ? "4" : "3") + " 0 R >>");
        }
        final PDF pdf = TestSupport.newPDF();
        final List<PDFobj> read = TestSupport.read(PDFTest.pdfWithObjects(objects.toArray(new String[0])));
        final List<List<PDFobj>> found = new ArrayList<List<PDFobj>>();
        long bytes = allocated(() -> {
            try {
                found.add(pdf.getPageObjects(read));
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
        assertTrue(bytes <= 64 << 20, (bytes >> 20) + " MB allocated");
        assertEquals(pages, found.get(0).size());
        assertEquals("[ 0 0 612 792 ]", found.get(0).get(0).getValue("/MediaBox"));
        assertEquals("3", found.get(0).get(1).getValue("/MediaBox"));
    }

    /**
     * A /Font dictionary that is an object of its own, which small resources
     * of each page refer to, is added once: 4,000 pages over one of 4,000
     * fonts, a PDF of 1 MB, copied it for each page, 4.6 GB in Go (N2).
     */
    @Test
    void aFontDictionaryOfItsOwnIsAddedOnce() throws Exception {
        int pages = 2000;
        int fonts = 2000;
        StringBuilder kids = new StringBuilder();
        for (int i = 0; i < pages; i++) {
            kids.append(5 + i).append(" 0 R ");
        }
        StringBuilder entries = new StringBuilder("<< ");
        for (int i = 0; i < fonts; i++) {
            entries.append("/F").append(i).append(" 4 0 R ");
        }
        List<String> objects = new ArrayList<String>(Arrays.asList(
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [" + kids + "] /Count " + pages + " >>",
                entries.append(">>").toString(),
                "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"));
        for (int i = 0; i < pages; i++) {
            objects.add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font 3 0 R >> >>");
        }
        final PDF pdf = TestSupport.newPDF();
        final List<PDFobj> read = TestSupport.read(PDFTest.pdfWithObjects(objects.toArray(new String[0])));
        long bytes = allocated(() -> {
            try {
                pdf.addResourceObjects(read);
            } catch (Exception e) {
                throw new RuntimeException(e);
            }
        });
        assertTrue(bytes <= 64 << 20, (bytes >> 20) + " MB allocated");
        java.lang.reflect.Field imported = PDF.class.getDeclaredField("importedFonts");
        imported.setAccessible(true);
        assertEquals(4 * fonts, ((List<?>) imported.get(pdf)).size());
    }

    /**
     * A token of an object stream's header longer than any number is copied
     * no further than 20 bytes, and the TIFF predictor is undone in the data
     * the reader decoded: each copied a stream, uncharged (N3).
     */
    @Test
    void theReaderMakesNoUnchargedCopyOfAStream() throws Exception {
        StringBuilder digits = new StringBuilder();
        for (int i = 0; i < 100000; i++) {
            digits.append('7');
        }
        byte[] data = (digits + " 5").getBytes(java.nio.charset.StandardCharsets.ISO_8859_1);
        List<String> header = PDF.objectStreamHeader(data, data.length, 2);
        assertEquals(Arrays.asList(digits.substring(0, 20) + "...", "5"), header);
        byte[] samples = {1, 1, 1, 5, 0, 0};
        org.junit.jupiter.api.Assertions.assertSame(samples, Decompressor.applyPredictor(samples, 2, 1, 8, 3));
    }

    /**
     * A lone surrogate in a password is EF BF BD in its UTF-8, as in C# and
     * Swift: Java's UTF-8 made it '?' (S5).
     */
    @Test
    void aLoneSurrogateInAPasswordIsTheReplacementCharacter() {
        assertArrayEquals(new byte[] {'a', (byte) 0xEF, (byte) 0xBF, (byte) 0xBD, 'b'},
                SASLprep.truncate("a\uD800b"));
        assertArrayEquals(new byte[] {(byte) 0xF0, (byte) 0x9F, (byte) 0x98, (byte) 0x80, (byte) 0xEF, (byte) 0xBF,
                (byte) 0xBD}, SASLprep.truncate("\uD83D\uDE00\uDC00"));
    }

    /**
     * An image of a PDF whose /Width is past an int takes the largest or the
     * smallest, in every port: Go took the smallest int64, and C# the
     * smallest int (C8).
     */
    @Test
    void aWidthPastAnIntTakesTheLargestInt() throws Exception {
        String[] widths = {"1e19", "1e400", "-1e19", "NaN", "12.7"};
        float[] pixels = {Integer.MAX_VALUE, Integer.MAX_VALUE, Integer.MIN_VALUE, 0f, 12f};
        for (int i = 0; i < widths.length; i++) {
            PDFobj obj = new PDFobj();
            obj.dict = new ArrayList<String>(Arrays.asList(
                    "<<", "/Type", "/XObject", "/Subtype", "/Image", "/Width", widths[i], "/Height", "1", ">>"));
            obj.stream = new byte[0];
            assertEquals(pixels[i], new Image(TestSupport.newPDF(), obj).getWidth(), widths[i]);
        }
    }

    /**
     * An image turned a quarter of the way is as wide as it was tall: it gave
     * its corner unturned, and in a table cell ran over the rows after it (F3).
     */
    @Test
    void anImageTurnedAQuarterHasItsDrawnSize() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Image image = new Image(pdf, TestSupport.open("PngSuite/BASN2C08.PNG"));   // 32 by 32
        image.scaleBy(0.75f, 0.375f);                                               // 24 by 12
        image.setLocation(50f, 50f);
        for (int degrees : new int[] {90, 270}) {
            image.setRotation(degrees);
            TestSupport.assertXY(62f, 74f, image.drawOn(null));
            assertEquals(12f, image.getWidth(), 0.01f);
            assertEquals(24f, image.getHeight(), 0.01f);
            TestSupport.assertXY(62f, 74f, image.drawOn(new Page(pdf, Letter.PORTRAIT)));
        }
        image.setRotation(0);
        TestSupport.assertXY(74f, 62f, image.drawOn(null));
        // Turned, the image fits the page as it is drawn
        image.setRotation(90);
        image.resizeToFit(new Page(pdf, Letter.PORTRAIT), false);
        TestSupport.assertXY(612f, 792f, image.drawOn(null));
    }

    /**
     * A text line in a text column is raised by its vertical offset once: the
     * column added it, and drawing the line added it again, so a superscript
     * raised 13 points rose 26 (F6).
     */
    @Test
    void aTextColumnRaisesASuperscriptOnce() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        Paragraph paragraph = new Paragraph()
                .add(new TextLine(font, "Base")).add(new TextLine(font, "sup").setVerticalOffset(-13f));
        TextColumn column = new TextColumn();
        column.setLocation(50f, 100f);
        column.setWidth(400f);
        column.addParagraph(paragraph);
        column.drawOn(page);
        String content = TestSupport.content(page);
        assertEquals(13f, TestSupport.positionOf(content, "sup")[1] - TestSupport.positionOf(content, "Base")[1], 0.01f);
    }

    /**
     * A destination the document names keeps its name, and a bookmark whose
     * name it is, dest#1, is named again: a link to the document's dest#1
     * went to the bookmark (F7).
     */
    @Test
    void aDestinationNameOfTheDocumentComesFirst() throws Exception {
        java.io.ByteArrayOutputStream out = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(out);
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.addDestination("dest#1", 500f);
        new TextLine(font, "go").setGoToAction("dest#1").setLocation(50f, 50f).drawOn(page);
        Bookmark bookmark = new Bookmark(pdf).addBookmark(page, new Title(font, "A", 10f, 100f));
        pdf.complete();
        java.util.regex.Matcher m = java.util.regex.Pattern.compile("/Dest \\[\\d+ 0 R /XYZ 0 ([\\d.]+) 0\\]")
                .matcher(TestSupport.latin1(out.toByteArray()));
        List<String> ys = new ArrayList<String>();
        while (m.find()) {
            ys.add(m.group(1));
        }
        assertTrue(ys.size() == 2 && ys.get(0).equals("292") && !ys.get(1).equals("292"), ys.toString());
        assertFalse(bookmark.getDestinationName().equals("dest#1"));
    }

    /**
     * A bookmark on a page that is not added fails the document: it went to
     * /Dest [0 0 R], and readers refused the document (F8).
     */
    @Test
    void aBookmarkOnAPageNotAddedFailsTheDocument() throws Exception {
        final PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        new Page(pdf, Letter.PORTRAIT);
        Page detached = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        new Bookmark(pdf).addBookmark(detached, new Title(font, "Lost", 10f, 100f));
        Exception e = assertThrows(IllegalStateException.class, () -> pdf.complete());
        assertEquals("The bookmark Lost is on a page that is not added to the PDF.", e.getMessage());
    }

    /**
     * An empty dash pattern of an arc is refused, with a stroke color and
     * without, in every port: Go took it for none with a stroke color (S3, C7).
     */
    @Test
    void anArcRefusesAnEmptyDashPattern() throws Exception {
        for (boolean colored : new boolean[] {false, true}) {
            final Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
            final Arc arc = new Arc().setRadius(50f).setSweep(90f).setStrokeDashPattern("");
            arc.setLocation(100f, 100f);
            if (colored) {
                arc.setStrokeColor(Color.red);
            }
            Exception e = assertThrows(IllegalArgumentException.class, () -> arc.drawOn(page));
            assertEquals("The dash pattern \"\" is not an array of non-negative numbers, not all zero, followed by a phase, such as \"[3 3] 0\".", e.getMessage());
        }
    }

    /**
     * A NaN or infinite angle fails the document, as a NaN coordinate does:
     * it wrote 0 0 0 0 0 0 cm, and the arc or the container vanished (F12).
     */
    @Test
    void aNaNRotationFailsTheDocument() throws Exception {
        for (float degrees : new float[] {Float.NaN, Float.POSITIVE_INFINITY}) {
            final Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
            final Arc arc = new Arc().setRadius(50f).setSweep(90f).setRotation(degrees);
            arc.setLocation(100f, 100f);
            assertEquals("A coordinate, size or width is NaN, infinite or too large for a PDF.", assertThrows(IllegalArgumentException.class, () -> arc.drawOn(page)).getMessage());

            final Page page2 = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
            final Container container = new Container(100f, 100f).setRotation(degrees);
            container.add(new Arc().setRadius(10f).setSweep(90f));
            assertEquals("A coordinate, size or width is NaN, infinite or too large for a PDF.", assertThrows(IllegalArgumentException.class,
                    () -> container.drawOn(page2)).getMessage());
        }
    }

    /**
     * A curve has two "c" control points, or one "v" or "y" one: a "c" curve
     * of one control point wrote "c" with four numbers, not six, which
     * readers refused (F11).
     */
    @Test
    void aCurveOfTheWrongControlPointsIsRefused() throws Exception {
        Point start = new Point(10f, 10f);
        Point end = new Point(100f, 100f);
        List<List<Point>> paths = Arrays.asList(
                Arrays.asList(start, new Point(20f, 20f, Point.CONTROL_POINT_C), end),
                Arrays.asList(start, new Point(20f, 20f, Point.CONTROL_POINT_V),
                        new Point(30f, 30f, Point.CONTROL_POINT_V), end),
                Arrays.asList(start, new Point(20f, 20f, Point.CONTROL_POINT_C),
                        new Point(30f, 30f, Point.CONTROL_POINT_Y), end));
        for (final List<Point> path : paths) {
            final Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
            assertEquals("A curve of a path has two \"c\" control points, or one \"v\" or \"y\" control point, before the point it ends at.", assertThrows(IllegalArgumentException.class,
                    () -> page.drawPath(path, PathOperator.STROKE)).getMessage());
            final Stamp stamp = new Stamp(TestSupport.newPDF()).setSize(200f, 200f);
            assertEquals("A curve of a path has two \"c\" control points, or one \"v\" or \"y\" control point, before the point it ends at.", assertThrows(IllegalArgumentException.class,
                    () -> stamp.drawPath(path, PathOperator.STROKE)).getMessage());
        }
        new Page(TestSupport.newPDF(), Letter.PORTRAIT).drawPath(Arrays.asList(start,
                new Point(20f, 20f, Point.CONTROL_POINT_C), new Point(30f, 30f, Point.CONTROL_POINT_C), end),
                PathOperator.STROKE);
    }

    /**
     * A point a path lists twice is scaled once: (10, 10) listed twice and
     * scaled by 2 became (40, 40), not (20, 20) (F9).
     */
    @Test
    void aPointListedTwiceIsScaledOnce() {
        Point shared = new Point(10f, 10f);
        Point other = new Point(30f, 5f);
        new Path().add(shared).add(other).add(shared).scaleBy(2f);
        assertEquals(20f, shared.getX(), 0.01f);
        assertEquals(20f, shared.getY(), 0.01f);
        assertEquals(60f, other.getX(), 0.01f);
    }

    private static String roundedRect(float x, float y, float w, float h) throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        new Rect(x, y, w, h).setCornerRadius(10f).setFillColor(Color.red).drawOn(page);
        return TestSupport.content(page);
    }

    /**
     * A rounded rectangle of a negative width and height is the same
     * rectangle the other way round: its corners were turned inside out (F10).
     */
    @Test
    void aRoundedRectangleOfANegativeSizeIsTheSame() throws Exception {
        assertEquals(roundedRect(50f, 60f, 50f, 40f), roundedRect(100f, 100f, -50f, -40f));
    }

    /**
     * A stamp is given its size before anything is drawn on it, as its y runs
     * down from its top: drawn before, the content was below the stamp, and
     * was not seen (F13).
     */
    @Test
    void aStampIsSizedBeforeItIsDrawnOn() throws Exception {
        final Stamp stamp = new Stamp(TestSupport.newPDF());
        assertEquals("Set the size of the stamp before drawing on it: its y runs down from its top.", assertThrows(IllegalStateException.class,
                () -> stamp.fillRect(0f, 0f, 20f, 10f)).getMessage());
        final Stamp sized = new Stamp(TestSupport.newPDF()).setSize(20f, 10f);
        sized.fillRect(0f, 0f, 20f, 10f);
        assertEquals("Set the size of the stamp before drawing on it: its y runs down from its top.", assertThrows(IllegalStateException.class,
                () -> sized.setSize(40f, 20f)).getMessage());
        new Stamp(TestSupport.newPDF()).setSize(20f, 10f).fillRect(0f, 0f, 20f, 10f).setSize(40f, 10f);
    }

    /**
     * A point that is a link, drawn on its own, makes its link, and a marker
     * of lines is stroked in the point's color: the link was not made, and a
     * plus was drawn in the pen's color (F15).
     */
    @Test
    void aPointDrawnOnItsOwnHasItsLinkAndColor() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        new Point(100f, 100f).setURIAction("https://pdfjet.com").drawOn(page);
        assertEquals(1, page.annots.size());
        Page page2 = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        new Point(100f, 100f).setShape(Shape.PLUS).setFillColor(Color.red).drawOn(page2);
        assertTrue(TestSupport.content(page2).contains("1 0 0 RG\n"), TestSupport.content(page2));
    }

    /**
     * A paragraph a text column draws has its coordinates, as one a text
     * frame draws does: they stayed 0 (F17).
     */
    @Test
    void aParagraphOfATextColumnHasItsCoordinates() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Paragraph paragraph = new Paragraph().add(new TextLine(font, "Hello, world"));
        TextColumn column = new TextColumn();
        column.setLocation(50f, 100f);
        column.setWidth(400f);
        column.addParagraph(paragraph);
        column.drawOn(new Page(pdf, Letter.PORTRAIT));
        assertEquals(50f, paragraph.getX1(), 0.01f);
        assertEquals(100f, paragraph.getY1(), 0.01f);
        assertEquals(100f + font.getAscent(), paragraph.getTextY(), 0.01f);
        assertTrue(paragraph.getY2() > paragraph.getTextY() && paragraph.getX2() > 50f);
    }

    /**
     * Drawing on a page of a PDF that was read: the page's own content is
     * drawn in a q and a Q of its own; a page with no /Contents gets the
     * drawing, which was lost; and the drawing starts at the corner of the
     * /MediaBox (F2).
     */
    @Test
    void drawingOnAPageOfAPDFThatWasRead() throws Exception {
        List<PDFobj> objects = TestSupport.read(PDFTest.pdfWithObjects(new String[] {
                "<< /Type /Catalog /Pages 2 0 R >>",
                "<< /Type /Pages /Kids [3 0 R 5 0 R] /Count 2 >>",
                "<< /Type /Page /Parent 2 0 R /MediaBox [100 100 712 892] /Contents 4 0 R >>",
                "<< /Length 18 >>\nstream\n0.5 0 0 0.5 0 0 cm\nendstream",
                "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] >>"}));
        PDF pdf = TestSupport.newPDF();
        List<PDFobj> pages = pdf.getPageObjects(objects);
        for (PDFobj pageObj : pages) {
            Page page = new Page(pdf, pageObj);
            page.fillRect(0f, 0f, 30f, 30f);
            page.complete(objects);
        }
        List<String> wrapped = pages.get(0).dict;
        int i = wrapped.indexOf("/Contents");
        assertEquals("[ 6 0 R 4 0 R 7 0 R ]", String.join(" ", wrapped.subList(i + 1, i + 12)));
        assertEquals("q\n", TestSupport.latin1(objects.get(5).stream));
        assertTrue(TestSupport.latin1(objects.get(6).stream).startsWith("Q\nq\n1 0 0 1 100 100 cm\n"));
        List<String> added = pages.get(1).dict;
        int j = added.indexOf("/Contents");
        assertEquals("[ 8 0 R ]", String.join(" ", added.subList(j + 1, j + 6)));
    }

    /**
     * A document of PDF/A-1 of more than 8191 pages, the most entries of an
     * array of PDF/A-1, is refused, and a text of PDF/A of more than 16383
     * characters, a string longer than 32767 bytes: veraPDF failed both (W1).
     */
    @Test
    void theSizeLimitsOfPDFAAreChecked() throws Exception {
        final PDF pdf = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_A_1B);
        for (int i = 0; i < 8192; i++) {
            new Page(pdf, Letter.PORTRAIT);
        }
        assertEquals("PDF/A-1 limits an array to 8191 entries, and the page tree has 8192: make the document PDF/A-2 or PDF/A-3.", assertThrows(IllegalStateException.class, () -> pdf.complete()).getMessage());

        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 16383; i++) {
            text.append('a');
        }
        PDF fits = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_A_2B);
        new TextLine(TestSupport.plexSans(fits), text.toString()).setLocation(10f, 20f).drawOn(new Page(fits, Letter.PORTRAIT));
        final PDF tooLong = new PDF(new java.io.ByteArrayOutputStream(), Compliance.PDF_A_2B);
        final TextLine line = new TextLine(TestSupport.plexSans(tooLong), text.append('a').toString());
        line.setLocation(10f, 20f);
        final Page page = new Page(tooLong, Letter.PORTRAIT);
        assertEquals("A text of more than 16383 characters is a string longer than PDF/A allows, 32767 bytes: draw it as several lines.", assertThrows(IllegalArgumentException.class, () -> line.drawOn(page)).getMessage());
    }

    /**
     * A /MediaBox of a NaN or infinite size is no size, and the page is
     * Letter, as of one of 0: Swift stopped where such a page was watermarked
     * (K1).
     */
    @Test
    void aMediaBoxOfANaNSizeIsLetter() {
        String huge = new String(new char[40]).replace('\0', '9');
        for (String[] box : new String[][] {{"0", "0", "NaN", "792"}, {"0", "0", "Infinity", "792"},
                {huge, "0", "-" + huge, "792"}}) {
            PDFobj obj = new PDFobj();
            obj.dict = new ArrayList<String>(Arrays.asList("<<", "/Type", "/Page", "/MediaBox", "[",
                    box[0], box[1], box[2], box[3], "]", ">>"));
            PageSize size = obj.getPageSize();
            assertEquals(612f, size.getWidth(), 0f, Arrays.toString(box));
            assertEquals(792f, size.getHeight(), 0f, Arrays.toString(box));
        }
    }

    /**
     * A cell's vertical alignment that is not top, center or bottom gives the
     * same message in the four ports: Java and C# threw a bare "Invalid
     * vertical text alignment option."
     */
    @Test
    void aCellsVerticalAlignmentOfLeftHasTheMessageOfTheFourPorts() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        final Table table = new Table();
        table.setTableData(Arrays.asList(Arrays.asList(new Cell(font, "text").setVerticalAlignment(Alignment.LEFT))), 0);
        table.setLocation(50f, 50f);
        final Page page = new Page(pdf, Letter.PORTRAIT);
        assertEquals("The vertical alignment of a cell is top, center or bottom.", assertThrows(IllegalArgumentException.class, () -> table.drawOn(page)).getMessage());
    }

    /**
     * A word of a text column too wide for its line breaks at a soft hyphen,
     * the line ending with a hyphen, as TextBlock and TextFrame break it:
     * TextColumn never broke at one.
     */
    @Test
    void aTextColumnBreaksAWordAtASoftHyphen() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        TextColumn column = new TextColumn();
        column.setLocation(50f, 100f);
        column.setWidth(TextBlockTest.softHyphenWidth(font));
        column.addParagraph(new Paragraph().add(new TextLine(font, "Ab " + TextBlockTest.SOFT_HYPHEN_WORD)));
        Page page = new Page(pdf, Letter.PORTRAIT);
        column.drawOn(page);
        // Ab Donau- / dampfschiff- / fahrt
        String content = TestSupport.content(page);
        float[] donau = TestSupport.positionOf(content, "Donau-");
        float[] dampf = TestSupport.positionOf(content, "dampfschiff-");
        float[] fahrt = TestSupport.positionOf(content, "fahrt");
        assertTrue(dampf[1] < donau[1] && fahrt[1] < dampf[1], content);
    }

    /**
     * The number that tells a font program apart, and that the tag of its
     * subset is made of, folds the blocks of 256 characters the font has
     * glyphs in, and leaves out the rest: it folded all 65,536 characters,
     * 236 empty blocks of IBM Plex Sans's 256. The number is the same in the
     * four ports.
     */
    @Test
    void aFontChecksumLeavesOutEmptyBlocks() throws Exception {
        InputStream in = TestSupport.open("fonts/IBMPlexSans/IBMPlexSans-Regular.ttf");
        try {
            assertEquals(0xABF1C2C88C082202L, new Font(TestSupport.newPDF(), in).checksum);
        } finally {
            in.close();
        }
    }

    /**
     * A word too wide for the column whose first piece does not fit the rest
     * of its line breaks at its soft hyphens on the next lines: it was put on
     * the next line whole, past the column's edge.
     */
    @Test
    void aTextColumnBreaksAWordThatStartsANewLine() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        font.setSize(10f);
        TextColumn column = new TextColumn();
        column.setLocation(50f, 100f);
        column.setWidth(100f);
        column.addParagraph(new Paragraph().add(new TextLine(font, "aaaaaaaaaaaaaaaaa bbbbbbbbbb\u00ADbbbbbbbbbb\u00ADbbbbbbbbbb\u00ADbbbbbbbbbb")));
        Page page = new Page(pdf, Letter.PORTRAIT);
        column.drawOn(page);
        // aaaaaaaaaaaaaaaaa / bbbbbbbbbb- / bbbbbbbbbb- / bbbbbbbbbb- / bbbbbbbbbb
        String content = TestSupport.content(page);
        float[] a = TestSupport.positionOf(content, "aaaaaaaaaaaaaaaaa");
        float[] b = TestSupport.positionOf(content, "bbbbbbbbbb-");
        assertTrue(b[1] < a[1], content);
    }
}
