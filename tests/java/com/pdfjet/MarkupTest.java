/*
 * MarkupTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.Test;

class MarkupTest {
    private static Font regular;
    private static Font bold;
    private static Font italic;
    private static Font boldItalic;
    private static Font code;

    private static Markup markup() throws Exception {
        return markup(TestSupport.newPDF());
    }

    private static Markup markup(PDF pdf) throws Exception {
        regular = new Font(pdf, CoreFont.HELVETICA);
        bold = new Font(pdf, CoreFont.HELVETICA_BOLD);
        italic = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);
        boldItalic = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);
        code = new Font(pdf, CoreFont.COURIER);
        return new Markup(regular, bold, italic, boldItalic, code);
    }

    // The text lines of the paragraph, each as its text and its font: R, B, I,
    // X for bold italic, or C for code, with the link after a space, and a +
    // before the text of one that is joined to the text line before it.
    private static List<String> describe(Paragraph paragraph) {
        List<String> list = new ArrayList<String>();
        for (int i = 0; i < paragraph.lines.size(); i++) {
            TextLine line = paragraph.lines.get(i);
            Font font = line.getFont();
            String style = (font == bold) ? "B" : (font == italic) ? "I"
                    : (font == boldItalic) ? "X" : (font == code) ? "C" : "R";
            String link = (line.getURIAction() == null) ? "" : " " + line.getURIAction();
            list.add((paragraph.joinsPrevious(i) ? "+" : "") + line.getText() + "|" + style + link);
        }
        return list;
    }

    private static List<String> parse(String text) throws Exception {
        return describe(markup().paragraph(text));
    }

    @Test
    void plainTextIsOneTextLine() throws Exception {
        assertEquals(Arrays.asList("Just text, no marks.|R"), parse("Just text, no marks."));
        assertEquals(Arrays.asList("one two|R"), parse("one\ntwo"));
    }

    @Test
    void boldItalicAndBoth() throws Exception {
        assertEquals(Arrays.asList("a |R", "b|B", " c|R"), parse("a **b** c"));
        assertEquals(Arrays.asList("a |R", "b|I", " c|R"), parse("a *b* c"));
        assertEquals(Arrays.asList("a |R", "b|X", " c|R"), parse("a ***b*** c"));
        assertEquals(Arrays.asList("a |B", "b|X", " c|B"), parse("**a *b* c**"));
    }

    @Test
    void punctuationAfterAStyleIsJoinedToTheWord() throws Exception {
        assertEquals(Arrays.asList("Hello, |R", "world|B", "+!|R"), parse("Hello, **world**!"));
        assertEquals(Arrays.asList("un|R", "+believ|I", "+able|R"), parse("un*believ*able"));
    }

    @Test
    void codeKeepsItsTextAsItIs() throws Exception {
        assertEquals(Arrays.asList("Call |R", "a*b*c|C", "+.|R"), parse("Call `a*b*c`."));
        assertEquals(Arrays.asList("a `b` c|C"), parse("`` a `b` c ``"));
        assertEquals(Arrays.asList("x|B", "+y|C", "+z|B"), parse("**x`y`z**"));
    }

    @Test
    void links() throws Exception {
        assertEquals(Arrays.asList("See |R", "PDFjet|R https://pdfjet.com", "+.|R"),
                parse("See [PDFjet](https://pdfjet.com)."));
        assertEquals(Arrays.asList("a |R https://x", "b|B https://x"), parse("[a **b**](https://x)"));
        assertEquals(Arrays.asList("go|B u"), parse("**[go](u)**"));
    }

    @Test
    void aLinkNeedsItsBracketsAParenthesisAndAURLWithoutSpaces() throws Exception {
        assertEquals(Arrays.asList("[a] (b)|R"), parse("[a] (b)"));
        assertEquals(Arrays.asList("[a](b c)|R"), parse("[a](b c)"));
        assertEquals(Arrays.asList("[a]()|R"), parse("[a]()"));
        assertEquals(Arrays.asList("[a](b|R"), parse("[a](b"));
        // A link is not in a link.
        assertEquals(Arrays.asList("[b](u) c|R v"), parse("[[b](u) c](v)"));
    }

    @Test
    void marksWithNoMatchAreText() throws Exception {
        assertEquals(Arrays.asList("2 * 3 = 6|R"), parse("2 * 3 = 6"));
        assertEquals(Arrays.asList("**a|R"), parse("**a"));
        assertEquals(Arrays.asList("a*|R"), parse("a*"));
        assertEquals(Arrays.asList("*|R", "+a|I"), parse("**a*"));
        assertEquals(Arrays.asList("`not code|R"), parse("`not code"));
        assertEquals(Arrays.asList("[not a link|R"), parse("[not a link"));
    }

    @Test
    void aBackslashMakesAMarkText() throws Exception {
        assertEquals(Arrays.asList("*not italic*|R"), parse("\\*not italic\\*"));
        assertEquals(Arrays.asList("[a](b)|R"), parse("\\[a](b)"));
        assertEquals(Arrays.asList("a\\b \\|R"), parse("a\\b \\"));
    }

    @Test
    void paragraphsAreSeparatedByEmptyLines() throws Exception {
        List<Paragraph> paragraphs = markup().paragraphs("One **two**\nthree.\n\n  \nFour.\r\n\r\n");
        assertEquals(2, paragraphs.size());
        assertEquals(Arrays.asList("One |R", "two|B", " three. |R"), describe(paragraphs.get(0)));
        assertEquals(Arrays.asList("Four. |R"), describe(paragraphs.get(1)));
        assertEquals(0, markup().paragraphs(" \n\n").size());
    }

    @Test
    void aLinkIsColoredAndUnderlined() throws Exception {
        Paragraph paragraph = markup().setLinkColor(Color.red).paragraph("[a](u)");
        TextLine line = paragraph.lines.get(0);
        assertTrue(line.getUnderline());
        assertEquals(Arrays.asList(1f, 0f, 0f), Arrays.asList(line.getTextColor()[0], line.getTextColor()[1],
                line.getTextColor()[2]));
    }

    @Test
    void longInputsAreReadInLinearTime() throws Exception {
        // Unmatched brackets, backticks of every length and runs of * would
        // each take quadratic time with a naive search.
        StringBuilder text = new StringBuilder();
        for (int i = 0; i < 20000; i++) {
            text.append("[a](b *c `").append(i % 50 == 0 ? "``" : "").append(" [[");
        }
        long time0 = System.nanoTime();
        Paragraph paragraph = markup().paragraph(text.toString());
        long milliseconds = (System.nanoTime() - time0) / 1000000;
        assertTrue(!paragraph.lines.isEmpty());
        assertTrue(milliseconds < 2000, milliseconds + " ms");
    }

    @Test
    void theParagraphDrawsWithNoSpaceBeforeJoinedPunctuation() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Markup markup = markup(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextFrame(Arrays.asList(markup.paragraph("one **two**, three")))
                .setLocation(10f, 10f).setWidth(300f).drawOn(page);
        String content = TestSupport.content(page);
        float[] two = TestSupport.positionOf(content, "two");
        float[] comma = TestSupport.positionOf(content, ",");
        assertEquals(two[0] + bold.stringWidth("two"), comma[0], TestSupport.DELTA);
    }
}
