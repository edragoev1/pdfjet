/*
 * ReviewFiveTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import com.pdfjet.barcodes.Barcode;
import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.*;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

/**
 * The findings of the fresh-eyes review of the charts, Markdown and the
 * barcodes, of 27 October 2026.
 */
class ReviewFiveTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    private static Markup markup(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new Markup(font, font, font, font, font);
    }

    @Test
    void theTextOfAParagraphHasNoSpaceWhereItsRunsAreJoined() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        assertEquals("Version 9.1.5: notes", Util.joinFields(markup.paragraph("Version **9.1.5**: notes").text()));
        assertEquals("The Tables", Util.joinFields(markup.paragraph("The `Table`s").text()));
        assertEquals("one two", Util.joinFields(markup.paragraph("one<br>two").text()));
    }

    @Test
    void theBookmarkOfAHeadingHasItsText() throws Exception {
        TestSupport.allowCoreFonts(true);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        List<Page> pages = new ArrayList<Page>();
        new Markdown(font, font, font, font, font).drawOn(pdf, "# Version **9.1.5**: notes", pages, Letter.PORTRAIT);
        pdf.addPages(pages);
        pdf.complete();
        String text = new String(bos.toByteArray(), StandardCharsets.ISO_8859_1);
        assertTrue(text.contains(TestSupport.textString("Version 9.1.5: notes")), "the title");
    }

    @Test
    void aBackslashAtTheEndOfAParagraphIsKept() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        List<Paragraph> paragraphs = markup.paragraphs("Save it to C:\\Temp\\");
        String text = paragraphs.get(0).text();
        assertEquals("Save it to C:\\Temp\\", text.trim());
    }

    @Test
    void theAltTextOfAnImageIsItsText() throws Exception {
        TestSupport.allowCoreFonts(true);
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<Page> pages = new ArrayList<Page>();
        // No image directory: the text is drawn in the image's place
        new Markdown(font, font, font, font, font)
                .drawOn(pdf, "![Fish &amp; Chips \\*](fish.png)", pages, Letter.PORTRAIT);
        String content = TestSupport.content(pages.get(0));
        assertTrue(content.contains(TestSupport.hex("Fish & Chips *")), content);
    }

    @Test
    void aLineValueOutsideTheRangeIsNotDrawnAtTheEdge() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        BarChart chart = new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 200f)
                .setCategories("a", "b").setValueAxisRange(0f, 100f);
        chart.addSeries("", new float[] {50f, 60f});
        chart.addLineSeries("target", new float[] {150f, 80f});
        chart.drawOn(page);
        assertTrue(TestSupport.content(page).contains("W\nn\n"), TestSupport.content(page));      // Clipped
        // In range, no clip
        page = new Page(pdf, Letter.PORTRAIT);
        chart = new BarChart(font, font).setLocation(50f, 50f).setSize(300f, 200f)
                .setCategories("a", "b").setValueAxisRange(0f, 100f);
        chart.addSeries("", new float[] {50f, 60f});
        chart.addLineSeries("target", new float[] {90f, 80f});
        chart.drawOn(page);
        assertFalse(TestSupport.content(page).contains("W\nn\n"), TestSupport.content(page));
    }

    @Test
    void aHeadingOnAPageThatIsNotAddedHasNoBookmark() throws Exception {
        TestSupport.allowCoreFonts(true);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        StringBuilder text = new StringBuilder("# One\n\n");
        for (int i = 0; i < 80; i++) {
            text.append("p").append(i).append("\n\n");
        }
        text.append("# Two");
        List<Page> pages = new ArrayList<Page>();
        new Markdown(font, font, font, font, font).drawOn(pdf, text.toString(), pages, Letter.PORTRAIT);
        assertTrue(pages.size() > 1);
        pdf.addPage(pages.get(0));      // The page of One only
        pdf.complete();                 // It failed: "Two" was on a page that is not added
        String out = new String(bos.toByteArray(), StandardCharsets.ISO_8859_1);
        assertTrue(out.contains(TestSupport.textString("One")));
        assertFalse(out.contains(TestSupport.textString("Two")));
    }

    @Test
    void anEmptyCode128IsRefused() {
        assertThrows(Exception.class, () -> new Barcode(Barcode.CODE_128, ""));
    }

    @Test
    void twoBreaksLeaveAnEmptyRow() throws Exception {
        TestSupport.allowCoreFonts(true);
        PDF pdf = TestSupport.newPDF();
        Markup markup = markup(pdf);
        assertEquals(2, markup.paragraph("p1<br><br>p2").lines.get(0).lineBreaks);
        float[] gaps = new float[2];
        String[] texts = {"p1<br>p2", "p1<br><br>p2"};
        for (int k = 0; k < 2; k++) {
            Page page = new Page(pdf, Letter.PORTRAIT);
            new TextFrame(Arrays.asList(markup.paragraph(texts[k]))).setLocation(50f, 50f).setWidth(400f).drawOn(page);
            String content = TestSupport.content(page);
            gaps[k] = TestSupport.positionOf(content, "p1")[1] - TestSupport.positionOf(content, "p2")[1];
        }
        assertEquals(2f * gaps[0], gaps[1], 0.05f);     // To the 0.01 of the positions written
    }

    @Test
    void aPipeInCodeInATableCell() throws Exception {
        assertEquals("TABLE(a;`x|y`|L)",
                MarkdownParserTest.describe(MarkdownParser.parse("| a |\n|---|\n| `x\\|y` |")));
    }

    @Test
    void hardBreaksOfCRLFText() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        assertEquals(1, markup.paragraph("one  \r\ntwo").lines.get(0).lineBreaks);
        assertEquals(1, markup.paragraph("one\\\r\ntwo").lines.get(0).lineBreaks);
        assertEquals(0, markup.paragraph("one \r\ntwo").lines.get(0).lineBreaks);
    }

    @Test
    void entitiesInTheAddressOfALink() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        assertEquals("https://x/?a=1&b=2",
                markup.paragraph("[a](https://x/?a=1&amp;b=2)").lines.get(0).getURIAction());
    }

    @Test
    void aControlCharacterOfAnEntityIsASpace() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        assertEquals("a b c d", markup.paragraph("a&#10;b&#9;c&#x7;d").lines.get(0).text);
    }

    @Test
    void theTrendIsOfThePointsDrawn() throws Exception {
        Font font = TestSupport.helvetica(TestSupport.newPDF());
        SimpleScatterChart chart = new SimpleScatterChart(font, font).setTitle("T")
                .add(1f, 1f).add(2f, 2f).add(3f, 3f).add(4f, -1000f).setTrendLine(true).setYAxisRange(0f, 10f);
        assertEquals("T: 3 points; the trend rises; 1 point outside the range.", chart.altDescription());
    }

    @Test
    void theIgnorableCharactersAtTheEdgesOfTheGapsSkipped() {
        int[] chars = {0x034F, 0x0350, 0x061B, 0x061C, 0x3164, 0x3165, 0xFDFF, 0xFE00};
        boolean[] ignorable = {true, false, false, true, true, false, false, true};
        for (int i = 0; i < chars.length; i++) {
            assertEquals(ignorable[i], Font.isIgnorable(chars[i]), Integer.toHexString(chars[i]));
        }
    }
}
