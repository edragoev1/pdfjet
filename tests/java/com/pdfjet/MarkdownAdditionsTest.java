/*
 * MarkdownAdditionsTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.*;

import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.*;
import org.junit.jupiter.api.AfterEach;
import org.junit.jupiter.api.Test;

/**
 * The additions to Markdown of 27 October 2026: hard line breaks, <br> in a
 * table cell, entities, and the bookmarks of the headings of a document that
 * is not tagged.
 */
class MarkdownAdditionsTest {
    @AfterEach
    void forbidCoreFonts() {
        TestSupport.allowCoreFonts(false);
    }

    private static Markup markup(PDF pdf) throws Exception {
        Font font = TestSupport.helvetica(pdf);
        return new Markup(font, font, font, font, font);
    }

    // The breaks of the text lines of the paragraph, in order.
    private static List<Boolean> breaks(Paragraph paragraph) {
        List<Boolean> breaks = new ArrayList<Boolean>();
        for (TextLine line : paragraph.lines) {
            breaks.add(line.lineBreak);
        }
        return breaks;
    }

    @Test
    void hardLineBreaks() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        assertEquals(Arrays.asList(true, false), breaks(markup.paragraph("one  \ntwo")));
        assertEquals(Arrays.asList(true, false), breaks(markup.paragraph("one\\\ntwo")));
        assertEquals(Arrays.asList(true, false), breaks(markup.paragraph("one<br>two")));
        assertEquals(Arrays.asList(true, false), breaks(markup.paragraph("one<BR />two")));
        assertEquals(Arrays.asList(true, false), breaks(markup.paragraph("one<br/>two")));
        // A line break after one space, and a <br> in code, are no breaks
        assertEquals(Arrays.asList(false), breaks(markup.paragraph("one \ntwo")));
        assertEquals(Arrays.asList(false), breaks(markup.paragraph("`a<br>b`")));
        assertEquals("<br", markup.paragraph("<br").lines.get(0).text);
        // Emphasis goes on over a break
        Paragraph bold = markup.paragraph("**one<br>two**");
        assertEquals(2, bold.lines.size());
        assertTrue(bold.lines.get(0).lineBreak);
        assertFalse(bold.joinsPrevious(1));
    }

    @Test
    void aTextFrameStartsARowAtABreak() throws Exception {
        TestSupport.allowCoreFonts(true);
        PDF pdf = TestSupport.newPDF();
        Page page = new Page(pdf, Letter.PORTRAIT);
        Markup markup = markup(pdf);
        new TextFrame(Arrays.asList(markup.paragraph("p1<br>p2 p3")))
                .setLocation(50f, 50f).setWidth(400f).drawOn(page);
        String content = TestSupport.content(page);
        float[] first = TestSupport.positionOf(content, "p1");
        float[] second = TestSupport.positionOf(content, "p2");
        assertEquals(first[0], second[0], 0.01f);
        assertTrue(second[1] < first[1], content);     // Below it, y up
    }

    @Test
    void entitiesAreTheirCharacters() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        assertEquals("Fish & Chips — — — ©   < >",
                markup.paragraph("Fish &amp; Chips &mdash; &#8212; &#x2014; &copy; &nbsp; &lt; &gt;").lines.get(0).text.trim());
        // Not an entity: an unknown name, no ;, no character, an escaped &
        assertEquals("&bogus; &amp &#0; &#xD800; &#12345678; &amp;",
                markup.paragraph("&bogus; &amp &#0; &#xD800; &#12345678; \\&amp;").lines.get(0).text.trim());
        // &lt;br&gt; is the text <br>, not a break
        Paragraph paragraph = markup.paragraph("a &lt;br&gt; b");
        assertEquals(Arrays.asList(false), breaks(paragraph));
        assertEquals("a <br> b", paragraph.lines.get(0).text.trim());
        // In code an entity is its text
        assertEquals("&amp;", markup.paragraph("`&amp;`").lines.get(0).text);
    }

    @Test
    void aBrInATableCellBreaksItsLine() throws Exception {
        TestSupport.allowCoreFonts(true);
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<Page> pages = new ArrayList<Page>();
        new Markdown(font, font, font, font, font)
                .drawOn(pdf, "| r0 |\n|---|\n| r1<br>r2 |", pages, Letter.PORTRAIT);
        String content = TestSupport.content(pages.get(0));
        float[] first = TestSupport.positionOf(content, "r1");
        float[] second = TestSupport.positionOf(content, "r2");
        assertEquals(first[0], second[0], 0.01f);
        assertTrue(second[1] < first[1], content);     // Below it, y up
        assertFalse(content.contains(TestSupport.hex("<br>")), content);
    }

    @Test
    void theParagraphOfAMarkdownTextKeepsItsBreaks() throws Exception {
        TestSupport.allowCoreFonts(true);
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        List<Page> pages = new ArrayList<Page>();
        new Markdown(font, font, font, font, font)
                .drawOn(pdf, "p1  \np2\\\np3\np4", pages, Letter.PORTRAIT);
        String content = TestSupport.content(pages.get(0));
        float y1 = TestSupport.positionOf(content, "p1")[1];
        float y2 = TestSupport.positionOf(content, "p2")[1];
        float y3 = TestSupport.positionOf(content, "p3")[1];
        assertTrue(y2 < y1 && y3 < y2, content);     // Each below the one before it, y up
        assertTrue(content.contains(TestSupport.hex("p4")), content);
        assertFalse(content.contains(TestSupport.hex("\\")), content);
    }

    // The PDF of the text drawn as Markdown, in a document that is not tagged.
    private static String pdfOf(String text) throws Exception {
        TestSupport.allowCoreFonts(true);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        List<Page> pages = new ArrayList<Page>();
        new Markdown(font, font, font, font, font).drawOn(pdf, text, pages, Letter.PORTRAIT);
        pdf.addPages(pages);
        pdf.complete();
        return new String(bos.toByteArray(), StandardCharsets.ISO_8859_1);
    }

    @Test
    void theHeadingsOfAnUntaggedDocumentAreItsBookmarks() throws Exception {
        String pdf = pdfOf("# One\n\ntext\n\n## Two\n\nmore");
        assertTrue(pdf.contains("/Outlines"), "no outline");
        assertTrue(pdf.contains(TestSupport.textString("One")) && pdf.contains(TestSupport.textString("Two")), pdf);
        assertFalse(pdfOf("text alone").contains("/Outlines"));
    }

    @Test
    void aDocumentWithBookmarksOfItsOwnKeepsThem() throws Exception {
        TestSupport.allowCoreFonts(true);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.helvetica(pdf);
        List<Page> pages = new ArrayList<Page>();
        new Markdown(font, font, font, font, font).drawOn(pdf, "# One", pages, Letter.PORTRAIT);
        Bookmark toc = new Bookmark(pdf);
        Title title = new Title(font, "Mine", 50f, 100f);
        toc.addBookmark(pages.get(0), title);
        title.drawOn(pages.get(0));
        pdf.addPages(pages);
        pdf.complete();
        String text = new String(bos.toByteArray(), StandardCharsets.ISO_8859_1);
        assertTrue(text.contains(TestSupport.textString("Mine")), "their bookmark");
        assertFalse(text.contains(TestSupport.textString("One")), "the heading's bookmark");
    }
}
