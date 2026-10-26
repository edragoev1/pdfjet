/*
 * MarkdownTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertNotEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

class MarkdownTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    // A Markdown text drawn in a PDF/UA document: its pages and the whole PDF.
    static final class Drawn {
        List<Page> pages = new ArrayList<Page>();
        String pdf;
        String text;    // The text of the pages, from their content streams
        List<String> contents = new ArrayList<String>();    // Of each page, before it is written
    }

    static Drawn draw(String text, String imageDirectory) throws Exception {
        return draw(text, imageDirectory, markdown -> {});
    }

    // The Markdown drawn after the change, as an option set on it.
    static Drawn draw(String text, String imageDirectory, java.util.function.Consumer<Markdown> change)
            throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Test");
        pdf.setTitle("Markdown");
        Font regular = new Font(pdf, CoreFont.HELVETICA);
        Font bold = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font italic = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);
        Font boldItalic = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);
        Font code = new Font(pdf, CoreFont.COURIER);
        Markdown markdown = new Markdown(regular, bold, italic, boldItalic, code);
        if (imageDirectory != null) {
            markdown.setImageDirectory(imageDirectory);
        }
        change.accept(markdown);
        Drawn drawn = new Drawn();
        markdown.drawOn(pdf, text, drawn.pages, Letter.PORTRAIT);
        StringBuilder content = new StringBuilder();
        for (Page page : drawn.pages) {
            drawn.contents.add(TestSupport.content(page));
            content.append(TestSupport.content(page));
        }
        drawn.text = content.toString();
        if (!drawn.pages.isEmpty()) {
            pdf.addPages(drawn.pages);
            pdf.complete();
            drawn.pdf = TestSupport.latin1(bos.toByteArray());
        }
        return drawn;
    }

    static int count(String pdf, String structure) {
        return pdf.split("/S /" + structure + "\n", -1).length - 1;
    }

    @Test
    void anEmptyTextNeedsNoPage() throws Exception {
        assertEquals(0, draw("", null).pages.size());
        assertEquals(0, draw("\n  \n", null).pages.size());
    }

    @Test
    void everyBlockIsTaggedForPDFUA() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        Drawn drawn = draw("# Title\n\nText with **bold**.\n\n- one\n- two\n\n> quoted\n\n"
                + "```\ncode\n```\n\n| A | B |\n|---|---|\n| 1 | 2 |\n\n---\n\n## Next", null);
        assertEquals(1, drawn.pages.size());
        assertEquals(1, count(drawn.pdf, "H1"));
        assertEquals(1, count(drawn.pdf, "H2"));
        assertEquals(1, count(drawn.pdf, "L"));
        assertEquals(2, count(drawn.pdf, "LI"));
        assertEquals(2, count(drawn.pdf, "Lbl"));
        assertEquals(2, count(drawn.pdf, "LBody"));
        assertEquals(1, count(drawn.pdf, "BlockQuote"));
        assertEquals(1, count(drawn.pdf, "Code"));
        assertEquals(1, count(drawn.pdf, "Table"));
    }

    @Test
    void tasksAreLabeledByTheirBoxes() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        Drawn drawn = draw("- [x] done\n- [ ] open\n- plain", null);
        assertEquals(1, count(drawn.pdf, "L"));
        assertEquals(3, count(drawn.pdf, "LI"));
        assertEquals(3, count(drawn.pdf, "Lbl"));
        assertEquals(3, count(drawn.pdf, "LBody"));
        // The boxes are read as ☑ and ☐, in UTF-16BE, their actual text and their
        // description, the plain item by its bullet
        for (String actualText : new String[] {"<feff2611>", "<feff2610>"}) {
            assertEquals(1, drawn.pdf.split("/ActualText " + actualText, -1).length - 1, "/ActualText " + actualText);
            assertEquals(1, drawn.pdf.split("/Alt " + actualText, -1).length - 1, "/Alt " + actualText);
        }
    }

    // Color.transparent leaves the color of a done task's box green: it filled
    // the box with the color last set, black (review of 15 October 2026).
    @Test
    void aTransparentCheckColorKeepsTheGreen() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        String[] contents = new String[2];
        for (int i = 0; i < contents.length; i++) {
            PDF pdf = TestSupport.newPDF();
            Font font = TestSupport.helvetica(pdf);
            Markdown markdown = new Markdown(font, font, font, font, font);
            if (i == 1) {
                markdown.setCheckColor(Color.transparent);
            }
            List<Page> pages = new ArrayList<Page>();
            markdown.drawOn(pdf, "- [x] done", pages, Letter.PORTRAIT);
            contents[i] = TestSupport.content(pages.get(0));
        }
        assertEquals(contents[0], contents[1]);
    }

    @Test
    void headingLevelsSkipNone() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        // A text that starts at ### and goes on to ##### is H1, then H2.
        Drawn drawn = draw("### Three\n\n##### Five\n\n# One", null);
        assertEquals(2, count(drawn.pdf, "H1"));
        assertEquals(1, count(drawn.pdf, "H2"));
        assertEquals(0, count(drawn.pdf, "H3"));
    }

    @Test
    void theTextFlowsOntoAsManyPagesAsItNeeds() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        StringBuilder text = new StringBuilder("# A long text\n\n");
        for (int i = 0; i < 150; i++) {
            text.append("Paragraph p").append(1000 + i)
                    .append(" has words enough to take a line or two of the page.\n\n");
        }
        Drawn drawn = draw(text.toString(), null);
        assertTrue(drawn.pages.size() >= 3, drawn.pages.size() + " pages");
        for (int i = 0; i < 150; i++) {
            String word = TestSupport.hex("p" + (1000 + i));
            assertEquals(1, drawn.text.split(word, -1).length - 1, "p" + (1000 + i));
        }
        // No text is drawn under the bottom margin of 72 points.
        java.util.regex.Matcher m = java.util.regex.Pattern.compile("[-0-9.]+ ([-0-9.]+) Td\n").matcher(drawn.text);
        while (m.find()) {
            assertTrue(Float.parseFloat(m.group(1)) >= 72f, "a baseline at y = " + m.group(1));
        }
    }

    @Test
    void aListOverPagesIsOneList() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 80; i++) {
            text.append("- item ").append(i).append('\n');
        }
        Drawn drawn = draw(text.toString(), null);
        assertTrue(drawn.pages.size() >= 2, drawn.pages.size() + " pages");
        assertEquals(1, count(drawn.pdf, "L"));
        assertEquals(80, count(drawn.pdf, "LI"));
    }

    // The font selected last before the text is drawn: the Tf of its line.
    private static String fontBefore(String content, String text) {
        int at = content.indexOf("<" + TestSupport.hex(text) + ">");
        assertTrue(at >= 0, text + " is not drawn");
        java.util.regex.Matcher m = java.util.regex.Pattern.compile("/F\\d+ [\\d.]+ Tf").matcher(content.substring(0, at));
        String font = null;
        while (m.find()) {
            font = m.group();
        }
        return font;
    }

    @Test
    void aTableCellInOneEmphasisIsDrawnInItsFont() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        // A whole cell in bold is drawn in the bold font, as the header is, so
        // that a table can mark a value; a cell of mixed emphasis stays regular.
        Drawn drawn = draw("| N | V |\n|---|---:|\n| a | 15 |\n| b | **25** |\n| c | **7** 8 |", null);
        String content = drawn.contents.get(0);
        String header = fontBefore(content, "V");
        assertEquals(header, fontBefore(content, "25"), "the bold cell is in the bold font");
        String plain = fontBefore(content, "15");
        assertNotEquals(header, plain);
        assertEquals(plain, fontBefore(content, "7 8"), "a cell of mixed emphasis is regular");
    }

    @Test
    void aTableIsAsWideAsTheTextWhenSet() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        // The right-aligned header of the last column is drawn at the right
        // end of the table: near the text's right margin, 72 pt from the edge
        // of the page, only when the tables are as wide as the text.
        String text = "| N | V |\n|---|---:|\n| a | 15 |";
        float narrow = TestSupport.positionOf(draw(text, null).contents.get(0), "V")[0];
        float wide = TestSupport.positionOf(
                draw(text, null, markdown -> markdown.setFullWidthTables(true)).contents.get(0), "V")[0];
        assertTrue(narrow < 200f, "a table is as wide as its text: " + narrow);
        assertTrue(wide > 612f - 72f - 30f, "a table is as wide as the text: " + wide);
    }

    @Test
    void aTableGoesOnOnTheNextPageWithItsHeaderRow() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        // Letters of the core fonts that are kerned are drawn apart, so the
        // header is one letter a column.
        StringBuilder text = new StringBuilder("Some text first.\n\n| N | S |\n|---:|---:|\n");
        for (int i = 1; i <= 120; i++) {
            text.append("| ").append(i).append(" | ").append(i * i).append(" |\n");
        }
        Drawn drawn = draw(text.toString(), null);
        assertTrue(drawn.pages.size() >= 2, drawn.pages.size() + " pages");
        assertEquals(1, count(drawn.pdf, "Table"));
        for (String content : drawn.contents) {
            assertTrue(content.contains("<" + TestSupport.hex("S") + ">"), "the header row is on every page");
        }
        assertTrue(drawn.text.contains(TestSupport.hex("14400")));
    }

    @Test
    void aTableCellThatStartsWithACheckMarkIsInTheCheckColor() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        // A cell that starts with a check mark is green, as a table of
        // features marks what each has; a header and other cells are not.
        String green = TestSupport.content(fillOf(0x1A7F37));
        String red = TestSupport.content(fillOf(0xFF0000));
        String table = "| Feature | \u2713 |\n|---|---|\n| Tables | \u2713 |\n| SVG | \u2713 with a module |\n| Charts | no |";
        String content = draw(table, null).contents.get(0);
        assertEquals(2, content.split(java.util.regex.Pattern.quote(green), -1).length - 1,
                "the two cells that start with a check mark are green");
        assertFalse(draw("| Feature | A |\n|---|---|\n| Tables | yes |", null).contents.get(0).contains(green));
        // The color is the check color, which colors the task boxes too.
        content = draw(table, null, markdown -> markdown.setCheckColor(0xFF0000)).contents.get(0);
        assertTrue(content.contains(red) && !content.contains(green));
    }

    // A page with only the fill color drawn: how the color is written.
    private static Page fillOf(int color) throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Page page = new Page(pdf, Letter.PORTRAIT, Page.DETACHED);
        page.setBrushColor(color);
        return page;
    }

    // The text of k short paragraphs, p0, p1 and so on, then the lead-in, then
    // a table of the rows, the cells of row r named r1, r2 and so on: words
    // of no letters that the core fonts kern, so that each is drawn whole.
    private static String fillerAndTable(int k, String leadIn, int rows) {
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < k; i++) {
            text.append("p").append(i).append("\n\n");
        }
        if (leadIn != null) {
            text.append(leadIn).append("\n\n");
        }
        text.append("| N | V |\n|---|---|\n");
        for (int r = 1; r <= rows; r++) {
            text.append("| r").append(r).append(" | v").append(r).append(" |\n");
        }
        return text.toString();
    }

    // The pages, counted from 0, that draw the text.
    private static List<Integer> pagesOf(Drawn drawn, String text) {
        List<Integer> pages = new ArrayList<Integer>();
        for (int i = 0; i < drawn.contents.size(); i++) {
            if (drawn.contents.get(i).contains(TestSupport.hex(text))) {
                pages.add(i);
            }
        }
        return pages;
    }

    @Test
    void aTableThatFitsOnAPageIsNotSplit() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        // Whatever the text before it, the table of eight rows is on one
        // page, its first row and its last; at some length of the text it is
        // moved to the next page, which shows the rule at work.
        boolean moved = false;
        for (int k = 0; k <= 60; k++) {
            Drawn drawn = draw(fillerAndTable(k, null, 8), null);
            List<Integer> first = pagesOf(drawn, "r1");
            List<Integer> last = pagesOf(drawn, "r8");
            assertEquals(first, last, k + " paragraphs: the table is split");
            int lastFiller = (k > 0) ? pagesOf(drawn, "p" + (k - 1)).get(0) : 0;
            moved |= first.get(0) > lastFiller;
        }
        assertTrue(moved, "the table never moved to the next page");
    }

    @Test
    void aParagraphThatEndsInAColonStaysWithItsTable() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        boolean moved = false;
        for (int k = 0; k <= 60; k++) {
            Drawn drawn = draw(fillerAndTable(k, "555:", 8), null);
            List<Integer> leadIn = pagesOf(drawn, "555:");
            assertEquals(leadIn, pagesOf(drawn, "r1"), k + " paragraphs: the lead-in is apart from its table");
            assertEquals(leadIn, pagesOf(drawn, "r8"), k + " paragraphs: the table is split");
            int lastFiller = (k > 0) ? pagesOf(drawn, "p" + (k - 1)).get(0) : 0;
            moved |= leadIn.get(0) > lastFiller;
        }
        assertTrue(moved, "the lead-in never moved with its table");
    }

    @Test
    void aTableLongerThanAPageStillStartsWhereItIs() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        Drawn drawn = draw(fillerAndTable(3, null, 120), null);
        assertEquals(Arrays.asList(0), pagesOf(drawn, "p2"));
        assertEquals(0, (int) pagesOf(drawn, "r1").get(0), "the long table starts after the text");
        assertTrue(pagesOf(drawn, "r120").get(0) > 0, "the long table goes on to the next pages");
    }

    @Test
    void imagesAreReadOnlyFromTheImageDirectory() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        String text = "![Tux](linux-logo.png)";
        // With no directory, the image's text is drawn instead.
        Drawn drawn = draw(text, null);
        assertEquals(0, count(drawn.pdf, "Figure"));
        assertTrue(drawn.text.contains(TestSupport.hex("Tux")));
        // From the directory, the image is a figure.
        String images = TestSupport.file("images").getPath();
        drawn = draw(text, images);
        assertEquals(1, count(drawn.pdf, "Figure"));
        java.util.regex.Matcher alt = java.util.regex.Pattern.compile("/Alt <([0-9A-Fa-f]+)>").matcher(drawn.pdf);
        assertTrue(alt.find() && TestSupport.utf16Hex(alt.group(1)).equals("Tux"),
                "the text of the image is its description");
        // Not above it, not an absolute path, not a URL.
        for (String source : new String[] {"../images/linux-logo.png", "/etc/passwd",
                TestSupport.file("images/linux-logo.png").getAbsolutePath(), "https://pdfjet.com/logo.png",
                "missing.png"}) {
            drawn = draw("![Not read](" + source + ")", images);
            assertEquals(0, count(drawn.pdf, "Figure"), source);
            assertTrue(drawn.text.contains(TestSupport.hex("Not read")), source);
        }
    }

    @Test
    void codeKeepsItsLinesAndGoesOnOnTheNextPage() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        StringBuilder text = new StringBuilder("```\n");
        for (int i = 0; i < 90; i++) {
            text.append("  line ").append(i).append(" *not emphasis*\n");
        }
        text.append("```\n");
        Drawn drawn = draw(text.toString(), null);
        assertTrue(drawn.pages.size() >= 2, drawn.pages.size() + " pages");
        assertEquals(1, count(drawn.pdf, "Code"));
        assertTrue(drawn.text.contains(TestSupport.hex("  line 0 *not emphasis*")));
        assertTrue(drawn.text.contains(TestSupport.hex("  line 89 *not emphasis*")));
    }

    @Test
    void numberedListsStartAtTheirFirstNumber() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        Drawn drawn = draw("3. three\n4. four", null);
        assertTrue(drawn.text.contains(TestSupport.hex("3.")));
        assertTrue(drawn.text.contains(TestSupport.hex("4.")));
        assertTrue(!drawn.text.contains("<" + TestSupport.hex("1.") + ">"));
    }

    @Test
    void htmlIsDrawnAsText() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        Drawn drawn = draw("<b>not bold</b>", null);
        assertTrue(drawn.text.contains(TestSupport.hex("<b>not")));
    }

    @Test
    void longCodeLinesAreCutInLinearTime() throws Exception {
        TestSupport.allowCoreFonts(true); // What the document tags, in a core font
        StringBuilder line = new StringBuilder();
        for (int i = 0; i < 200000; i++) {
            line.append((char) ('a' + i % 26));
        }
        final Drawn[] drawn = new Drawn[1];
        long milliseconds = TestSupport.shortestMillis(5000, () -> drawn[0] = draw("```\n" + line + "\n```", null));
        assertTrue(drawn[0].pages.size() > 1);
        assertTrue(milliseconds < 5000, milliseconds + " ms");
    }

    @Test
    void aCodeLineOfOneColumnKeepsItsSurrogatePairs() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        Font code = new Font(pdf, CoreFont.COURIER);
        // A width that one character of the code fills.
        Markdown markdown = new Markdown(font, font, font, font, code).setMargins(300f, 72f, 300f, 72f);
        List<Page> pages = new ArrayList<Page>();
        markdown.drawOn(pdf, "```\n\uD83D\uDE00x\uD83D\uDE00\n```", pages, Letter.PORTRAIT);
        assertEquals(1, pages.size());
    }

    // A table of more than 64 columns, and tables of more than 100,000 cells,
    // make the text an error: the first was drawn as a paragraph of pipes, and a
    // header of 64 columns over rows of "|" made 64 cells of a row of 2 bytes,
    // 895 MB in Go from 64 KB (review of 12 October 2026, R33).
    @Test
    void aTableTooWideOrTooLargeIsAnError() throws Exception {
        drawTable(64, 2);
        Exception e = assertThrows(IllegalArgumentException.class, () -> drawTable(65, 2));
        assertTrue(e.getMessage().contains("more than 64 columns"), e.getMessage());
        e = assertThrows(IllegalArgumentException.class, () -> drawTable(64, 1600));
        assertTrue(e.getMessage().contains("more than 100000 cells"), e.getMessage());
        // A line of pipes that is no table, with no row of delimiters, is text
        drawText(repeat("| a ", 70) + "|\n");
    }

    // Draws a table of the columns over rows of "|" alone.
    private static void drawTable(int columns, int rows) throws Exception {
        drawText(repeat("| a ", columns) + "|\n" + repeat("| - ", columns) + "|\n" + repeat("|\n", rows));
    }

    private static void drawText(String text) throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Font font = TestSupport.helvetica(pdf);
        new Markdown(font, font, font, font, font).drawOn(pdf, text, new ArrayList<Page>(), Letter.PORTRAIT);
    }

    private static String repeat(String text, int count) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < count; i++) {
            sb.append(text);
        }
        return sb.toString();
    }

    // A heading that starts a list item or a quote keeps a line of the text
    // after it on its page, as any other heading does: it was drawn into the
    // bottom margin, with nothing after it (review of 12 October 2026, F16).
    @Test
    void aHeadingThatStartsAnItemStaysAboveTheMargin() throws Exception {
        TestSupport.allowCoreFonts(true);
        try {
            headingsStayAboveTheMargin();
        } finally {
            TestSupport.allowCoreFonts(false);
        }
    }

    private static void headingsStayAboveTheMargin() throws Exception {
        for (String container : new String[] {"- ", "> "}) {
            for (int lines = 15; lines < 35; lines++) {
                StringBuilder text = new StringBuilder();
                for (int i = 0; i < lines; i++) {
                    text.append("Line\n\n");
                }
                text.append(container).append("# HEADING\n").append(container).append("\n")
                        .append(container).append("after\n");
                for (String content : draw(text.toString(), null).contents) {
                    if (content.contains(TestSupport.hex("HEADING"))) {
                        float y = TestSupport.positionOf(content, "HEADING")[1];
                        assertTrue(y >= 72f + 12f, container + lines + ": the heading is at " + y);
                        assertTrue(content.contains(TestSupport.hex("after")), container + lines + ": nothing after it");
                    }
                }
            }
        }
    }
}
