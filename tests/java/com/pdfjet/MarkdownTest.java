/*
 * MarkdownTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;

class MarkdownTest {
    // A Markdown text drawn in a PDF/UA document: its pages and the whole PDF.
    static final class Drawn {
        List<Page> pages = new ArrayList<Page>();
        String pdf;
        String text;    // The text of the pages, from their content streams
        List<String> contents = new ArrayList<String>();    // Of each page, before it is written
    }

    static Drawn draw(String text, String imageDirectory) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
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
    void headingLevelsSkipNone() throws Exception {
        // A text that starts at ### and goes on to ##### is H1, then H2.
        Drawn drawn = draw("### Three\n\n##### Five\n\n# One", null);
        assertEquals(2, count(drawn.pdf, "H1"));
        assertEquals(1, count(drawn.pdf, "H2"));
        assertEquals(0, count(drawn.pdf, "H3"));
    }

    @Test
    void theTextFlowsOntoAsManyPagesAsItNeeds() throws Exception {
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
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 80; i++) {
            text.append("- item ").append(i).append('\n');
        }
        Drawn drawn = draw(text.toString(), null);
        assertTrue(drawn.pages.size() >= 2, drawn.pages.size() + " pages");
        assertEquals(1, count(drawn.pdf, "L"));
        assertEquals(80, count(drawn.pdf, "LI"));
    }

    @Test
    void aTableGoesOnOnTheNextPageWithItsHeaderRow() throws Exception {
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
    void imagesAreReadOnlyFromTheImageDirectory() throws Exception {
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
        Drawn drawn = draw("3. three\n4. four", null);
        assertTrue(drawn.text.contains(TestSupport.hex("3.")));
        assertTrue(drawn.text.contains(TestSupport.hex("4.")));
        assertTrue(!drawn.text.contains("<" + TestSupport.hex("1.") + ">"));
    }

    @Test
    void htmlIsDrawnAsText() throws Exception {
        Drawn drawn = draw("<b>not bold</b>", null);
        assertTrue(drawn.text.contains(TestSupport.hex("<b>not")));
    }
}
