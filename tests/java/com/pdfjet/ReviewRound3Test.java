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
        font.program.chars.put(font.unicodeToGID.get(0x0629), (int) 'X');
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
    @SuppressWarnings("deprecation")    // The CJK fonts are deprecated, and still tested
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
}
