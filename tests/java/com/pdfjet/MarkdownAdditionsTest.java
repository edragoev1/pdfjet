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
            breaks.add(line.lineBreaks > 0);
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
        assertEquals(1, bold.lines.get(0).lineBreaks);
        assertFalse(bold.joinsPrevious(1));
    }

    // A hard line break at the end of a text is no break, as in CommonMark,
    // and the spaces before it are not in the last text line; paragraphs
    // strips the whitespace at the end of each paragraph.
    @Test
    void aHardLineBreakAtTheEndIsNoBreak() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        for (String text : new String[] {"0  \n", "0  \r\n", "0   \n", "0  \n\n", "0  \n  "}) {
            Paragraph paragraph = markup.paragraph(text);
            assertEquals(1, paragraph.lines.size(), text);
            assertEquals("0", paragraph.lines.get(0).text, text);
            assertEquals(0, paragraph.lines.get(0).lineBreaks, text);
            List<Paragraph> paragraphs = markup.paragraphs(text);
            assertEquals(1, paragraphs.size(), text);
            assertEquals(1, paragraphs.get(0).lines.size(), text);
            assertEquals("0", paragraphs.get(0).lines.get(0).text, text);
        }
        // In bold, and a break before the last line
        Paragraph bold = markup.paragraph("**0**  \n");
        assertEquals(1, bold.lines.size());
        assertEquals("0", bold.lines.get(0).text);
        assertEquals(0, bold.lines.get(0).lineBreaks);
        Paragraph two = markup.paragraph("one  \ntwo  \n");
        assertEquals(Arrays.asList(true, false), breaks(two));
        assertEquals("two", two.lines.get(1).text);
        // Spaces at the end without a line break are text in paragraph
        assertEquals("0  ", markup.paragraph("0  ").lines.get(0).text);
        assertEquals("0", markup.paragraphs("0  ").get(0).lines.get(0).text);
    }

    // The spaces before a hard line break are not in the text line before
    // it, as in CommonMark.
    @Test
    void theSpacesBeforeAHardLineBreakAreNotText() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        for (String text : new String[] {"one  \ntwo", "one  \r\ntwo", "one   \ntwo", "one\t  \ntwo",
                "**one**  \ntwo", "[one  \ntwo](u)"}) {
            Paragraph paragraph = markup.paragraph(text);
            assertEquals(Arrays.asList(true, false), breaks(paragraph), text);
            assertEquals("one", paragraph.lines.get(0).text, text);
            assertEquals("two", paragraph.lines.get(1).text, text);
        }
    }

    // A backslash at the end of a text is a backslash, and no hard line
    // break, as in CommonMark; in the middle it is a break, as before.
    @Test
    void aBackslashAtTheEndIsText() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        for (String text : new String[] {"0\\\n", "0\\\r\n", "0\\\n  ", "0\\\n\n"}) {
            Paragraph paragraph = markup.paragraph(text);
            assertEquals(1, paragraph.lines.size(), text);
            assertEquals("0\\", paragraph.lines.get(0).text, text);
            assertEquals(0, paragraph.lines.get(0).lineBreaks, text);
            List<Paragraph> paragraphs = markup.paragraphs(text);
            assertEquals(1, paragraphs.size(), text);
            assertEquals(1, paragraphs.get(0).lines.size(), text);
            assertEquals("0\\", paragraphs.get(0).lines.get(0).text, text);
        }
        Paragraph two = markup.paragraph("one\\\ntwo\\\n");
        assertEquals(Arrays.asList(true, false), breaks(two));
        assertEquals("two\\", two.lines.get(1).text);
    }

    // The spaces and tabs at the start of the line after a hard line break are
    // not in the text, as in CommonMark; a space written &#32; is, and so are
    // the spaces after a <br>, which is no line ending.
    @Test
    void theSpacesAfterAHardLineBreakAreNotText() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        for (String text : new String[] {"one  \n  two", "one  \r\n  two", "one   \n\t two", "one\\\n  two",
                "one\\\r\n  two", "**one**  \n  two", "[one  \n  two](u)"}) {
            Paragraph paragraph = markup.paragraph(text);
            assertEquals(Arrays.asList(true, false), breaks(paragraph), text);
            assertEquals("one", paragraph.lines.get(0).text, text);
            assertEquals("two", paragraph.lines.get(1).text, text);
            List<Paragraph> paragraphs = markup.paragraphs(text);
            assertEquals(1, paragraphs.size(), text);
            assertEquals(2, paragraphs.get(0).lines.size(), text);
            assertEquals("two", paragraphs.get(0).lines.get(1).text, text);
        }
        assertEquals(" two", markup.paragraph("one  \n&#32;two").lines.get(1).text);
        assertEquals("  two", markup.paragraph("one<br>  two").lines.get(1).text);
    }

    // The spaces and tabs at the start of the line after a soft line break
    // are not in the text, as in CommonMark: the line break is one space; a
    // space written &#32; is text.
    @Test
    void theSpacesAfterASoftLineBreakAreNotText() throws Exception {
        Markup markup = markup(TestSupport.newPDF());
        for (String text : new String[] {"one\n  two", "one\r\n  two", "one\n\t two", "one\ntwo", "one\r\ntwo"}) {
            Paragraph paragraph = markup.paragraph(text);
            assertEquals(1, paragraph.lines.size(), text);
            assertEquals("one two", paragraph.lines.get(0).text, text);
            assertEquals(0, paragraph.lines.get(0).lineBreaks, text);
            List<Paragraph> paragraphs = markup.paragraphs(text);
            assertEquals(1, paragraphs.size(), text);
            assertEquals(1, paragraphs.get(0).lines.size(), text);
            assertEquals("one two", paragraphs.get(0).lines.get(0).text, text);
        }
        assertEquals("one  two", markup.paragraph("one\n&#32;two").lines.get(0).text);
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
