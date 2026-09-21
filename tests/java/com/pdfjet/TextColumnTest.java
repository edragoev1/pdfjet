/*
 * TextColumnTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.util.ArrayList;
import java.util.List;
import org.junit.jupiter.api.Test;

class TextColumnTest {
    private static final String EIGHT_WORDS = "alpha beta gamma delta epsilon zeta eta theta";

    private static TextColumn column(Font font, String text, Alignment alignment) {
        TextColumn column = new TextColumn();
        column.setWidth(200f);
        column.setTextAlignment(alignment);
        column.addParagraph(new Paragraph(new TextLine(font, text)));
        column.setLocation(100f, 100f);
        return column;
    }

    // The x of every Td of the content, in the order they are written, with the y.
    private static List<float[]> positions(String content) {
        List<float[]> list = new ArrayList<float[]>();
        for (String line : content.split("\n")) {
            if (line.endsWith(" Td")) {
                String[] parts = line.split(" ");
                list.add(new float[] {
                        Float.parseFloat(parts[0]), Float.parseFloat(parts[1])});
            }
        }
        return list;
    }

    @Test
    void aParagraphIsAsTallAsItsTextAndNoTaller() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        // The ascent and the descent of one line, not a whole line of spacing.
        TextColumn column = column(font, "one short line", Alignment.LEFT);
        assertEquals(font.getBodyHeight(font.getSize()), column.getSize().getHeight(),
                TestSupport.DELTA);
        // A cell measures the column as it measures its own text.
        Cell withColumn = new Cell(font);
        TextColumn inCell = new TextColumn();
        inCell.setWidth(200f);
        inCell.addParagraph(new Paragraph(new TextLine(font, "one short line")));
        withColumn.setTextColumn(inCell);
        assertEquals(new Cell(font, "one short line").getHeight(200f),
                withColumn.getHeight(200f), TestSupport.DELTA);
    }

    @Test
    void rightAlignedTextReachesTheRightEdge() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        column(font, EIGHT_WORDS, Alignment.RIGHT).drawOn(page);
        List<float[]> positions = positions(TestSupport.content(page));
        // The tokens of the first drawn line end at the right edge, 100 + 200.
        float y = positions.get(0)[1];
        float lastX = 0f;
        for (float[] position : positions) {
            if (position[1] == y) {
                lastX = position[0];
            }
        }
        // "zeta" is the last token of the first line; its space is not text.
        assertEquals(300f, lastX + font.stringWidth(font.getSize(), "zeta"), TestSupport.DELTA);
    }

    @Test
    void aJustifiedLineReachesBothEdges() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        column(font, EIGHT_WORDS + " iota kappa", Alignment.JUSTIFY).drawOn(page);
        List<float[]> positions = positions(TestSupport.content(page));
        float y = positions.get(0)[1];
        float firstX = positions.get(0)[0];
        float lastX = 0f;
        for (float[] position : positions) {
            if (position[1] == y) {
                lastX = position[0];
            }
        }
        assertEquals(100f, firstX, TestSupport.DELTA);
        assertEquals(300f, lastX + font.stringWidth(font.getSize(), "zeta"), TestSupport.DELTA);
    }

    @Test
    void aWordWiderThanTheColumnLeavesNoBlankLineAboveIt() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        TextColumn wide = new TextColumn();
        wide.setWidth(120f);
        wide.addParagraph(new Paragraph(
                new TextLine(font, "Supercalifragilisticexpialidocious bbb ccc")));
        wide.setLocation(0f, 0f);
        // The long word on a line of its own and the two short words on the next.
        assertEquals(2f * font.getBodyHeight(font.getSize()), wide.getSize().getHeight(),
                TestSupport.DELTA);
        Page page = new Page(pdf, Letter.PORTRAIT);
        wide.setLocation(100f, 100f);
        wide.drawOn(page);
        List<float[]> positions = positions(TestSupport.content(page));
        // The first token is drawn on the first line, at the ascent of the font.
        assertEquals(100f + font.getAscent(font.getSize()),
                792f - positions.get(0)[1], TestSupport.DELTA);
    }

    @Test
    void theUnderlineOfALineStopsAtItsTextAndRunsThroughIt() throws Exception {
        PDF pdf = TestSupport.newPDF();
        Font font = TestSupport.helvetica(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine underlined = new TextLine(font, EIGHT_WORDS);
        underlined.setUnderline(true);
        TextColumn column = new TextColumn();
        column.setWidth(200f);
        column.addParagraph(new Paragraph(underlined));
        column.setLocation(100f, 100f);
        column.drawOn(page);
        // The segments of a line follow each other without a gap, and the last
        // one stops at the text rather than after the space that follows it.
        List<float[]> segments = new ArrayList<float[]>();
        String[] rows = TestSupport.content(page).split("\n");
        for (int i = 0; i < rows.length - 1; i++) {
            if (rows[i].endsWith(" m") && rows[i + 1].endsWith(" l")) {
                segments.add(new float[] {
                        Float.parseFloat(rows[i].split(" ")[0]),
                        Float.parseFloat(rows[i + 1].split(" ")[0]),
                        Float.parseFloat(rows[i].split(" ")[1])});
            }
        }
        assertTrue(segments.size() > 2);
        for (int i = 1; i < segments.size(); i++) {
            if (segments.get(i)[2] == segments.get(i - 1)[2]) {   // the same line
                assertEquals(segments.get(i - 1)[1], segments.get(i)[0], TestSupport.DELTA);
            }
        }
        // The first line ends with "zeta", underlined up to its last character
        // and no further: the space after it is not text.
        float endOfFirstLine = 0f;
        for (float[] segment : segments) {
            if (segment[2] == segments.get(0)[2]) {
                endOfFirstLine = segment[1];
            }
        }
        float lastTokenX = 0f;
        List<float[]> positions = positions(TestSupport.content(page));
        for (float[] position : positions) {
            if (position[1] == positions.get(0)[1]) {
                lastTokenX = position[0];
            }
        }
        assertEquals(lastTokenX + font.stringWidth(font.getSize(), "zeta"), endOfFirstLine,
                TestSupport.DELTA);
    }
    // The number of times the text is in the string.
    private static int count(String str, String text) {
        return str.split(java.util.regex.Pattern.quote(text), -1).length - 1;
    }

    @Test
    void aParagraphIsOneStructureElementOfTheTypeItIsGiven() throws Exception {
        // The words of a paragraph are drawn one at a time, and each was an
        // element of its own, so a reader read every word as a paragraph.
        java.io.ByteArrayOutputStream bos = new java.io.ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1);
        pdf.setTitle("Title");
        Font font = TestSupport.helvetica(pdf);
        TextColumn column = new TextColumn();
        column.setWidth(200f);
        column.setLocation(100f, 100f);
        column.addParagraph(new Paragraph()
                .setStructureType(StructElem.H1).add(new TextLine(font, EIGHT_WORDS)));
        column.addParagraph(new Paragraph().add(new TextLine(font, EIGHT_WORDS)));
        Page page = new Page(pdf, Letter.PORTRAIT);
        column.drawOn(page);
        String content = TestSupport.latin1(page.getContent());
        // The eight words of each paragraph are its marked contents.
        assertEquals(8, count(content, "/H1 <</MCID"), content);
        assertEquals(8, count(content, "/P <</MCID"), content);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(1, count(raw, "/S /H1\n"), raw);
        assertEquals(1, count(raw, "/S /P\n"), raw);
        assertTrue(raw.contains("/K [0 1 2 3 4 5 6 7]"), raw);
    }

}
