/*
 * Paragraph.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Used to create paragraph objects.
 * See the TextColumn class for more information.
 */
public class Paragraph {
    float xText;

    float yText;

    float x1;

    float y1;

    float x2;

    float y2;

    /** The text lines of this paragraph. */
    protected List<TextLine> lines = null;
    /** The alignment of this paragraph. */
    protected Alignment alignment = Alignment.LEFT;
    // True after setTextAlignment. Otherwise the alignment of the text column applies.
    boolean explicitAlignment = false;
    // The structure element of the paragraph in a PDF/UA document, which is a
    // paragraph unless it is set to a heading; see setStructureType.
    StructElem structureType = StructElem.P;
    // The label of a paragraph that is an item of a list, and how far to the
    // left of the text it is drawn; see setListLabel.
    TextLine listLabel = null;
    float listLabelIndent = 0f;

    /**
     * Makes this paragraph an item of a list, labelled by the text line, which
     * is drawn indent points to the left of the text of the paragraph and on
     * the baseline of its first line. A run of paragraphs that have a label is
     * a list: in a PDF/UA document it is an L of an LI for each paragraph,
     * each holding the Lbl of its label and the LBody of its text, so that a
     * reader reads the label of an item before the item, however the two are
     * drawn.
     *
     * @param label the label of the item.
     * @param indent how far to the left of the text the label is drawn.
     * @return this paragraph.
     */
    public Paragraph setListLabel(TextLine label, float indent) {
        this.listLabel = label;
        this.listLabelIndent = indent;
        return this;
    }

    /**
     * Sets the structure element type of this paragraph, for a PDF/UA
     * document: StructElem.H1 to StructElem.H6 for a heading, and
     * StructElem.P, which it is, for a paragraph. The paragraph is one element
     * however many lines and words it is drawn in.
     *
     * @param structureType the structure element type.
     * @return this paragraph.
     */
    public Paragraph setStructureType(StructElem structureType) {
        this.structureType = structureType;
        return this;
    }

    /**
     * Constructor for creating paragraph objects.
     */
    public Paragraph() {
        lines = new ArrayList<TextLine>();
    }

    /**
     * Creates a paragraph with the specified text line.
     *
     * @param text the text line.
     */
    public Paragraph(TextLine text) {
        lines = new ArrayList<TextLine>();
        lines.add(text);
    }

    /**
     * Adds a text line to this paragraph.
     *
     * @param text the text line to add to this paragraph.
     * @return this paragraph.
     */
    public Paragraph add(TextLine text) {
        lines.add(text);
        return this;
    }

    /**
     * Sets the alignment of the text in this paragraph. A paragraph with no
     * alignment set takes the alignment of the text column it is drawn in.
     *
     * @param alignment the alignment code: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER or Alignment.JUSTIFY.
     * @return this paragraph.
     */
    public Paragraph setTextAlignment(Alignment alignment) {
        this.alignment = alignment;
        this.explicitAlignment = true;
        return this;
    }

    /**
     * Returns the text lines of this paragraph.
     *
     * @return the list of text lines.
     */
    public List<TextLine> getTextLines() {
        return lines;
    }

    /**
     * Returns true if the first line of this paragraph starts with the specified token.
     *
     * @param token the token.
     * @return true if the first line starts with the specified token.
     */
    public boolean startsWith(String token) {
        return lines.get(0).getText().startsWith(token);
    }

    /**
     * Sets the text color of all lines in this paragraph as a 0xRRGGBB value.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Paragraph object.
     */
    public Paragraph setTextColor(int color) {
        for (TextLine line : lines) {
            line.setTextColor(color);
        }
        return this;
    }

    /**
     * Sets the text color of every line of this paragraph.
     *
     * @param rgbColor the color as red, green and blue components from 0.0 to 1.0.
     * @return this Paragraph object.
     */
    public Paragraph setTextColor(float[] rgbColor) {
        for (TextLine line : lines) {
            line.setTextColor(rgbColor);
        }
        return this;
    }

    /**
     * Sets the word highlight colors of all lines in this paragraph.
     *
     * @param colorMap the words and their 0xRRGGBB colors.
     * @return this Paragraph object.
     */
    public Paragraph setHighlightColors(Map<String, Integer> colorMap) {
        for (TextLine line : lines) {
            line.setHighlightColors(colorMap);
        }
        return this;
    }

    /**
     * Reads a text file and returns its paragraphs. An empty line separates the paragraphs.
     *
     * @param f1 the font for the text.
     * @param filePath the path of the text file.
     * @return the paragraphs.
     * @throws Exception if the file cannot be read.
     */
    public static List<Paragraph> paragraphsFromFile(Font f1, String filePath) throws Exception {
        List<Paragraph> paragraphs = new ArrayList<>();
        String contents = Content.ofTextFile(filePath);
        Paragraph paragraph = new Paragraph();
        TextLine textLine = new TextLine(f1);
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < contents.length(); i++) {
            char ch = contents.charAt(i);
            // We need at least one character after the \n\n to begin new paragraph!
            if (i < (contents.length() - 2) &&
                    ch == '\n' && contents.charAt(i + 1) == '\n') {
                textLine.setText(sb.toString());
                paragraph.add(textLine);
                paragraphs.add(paragraph);
                paragraph = new Paragraph();
                textLine = new TextLine(f1);
                sb.setLength(0);
                i += 1;
            } else {
                sb.append(ch);
            }
        }
        if (!sb.toString().isEmpty()) {
            textLine.setText(sb.toString());
            paragraph.add(textLine);
            paragraphs.add(paragraph);
        }
        return paragraphs;
    }

    /**
     * Returns the x coordinate where the text of this paragraph starts, once a TextFrame or TextColumn has drawn it.
     *
     * @return the coordinate.
     */
    public float getTextX() {
        return xText;
    }

    /**
     * Returns the baseline y coordinate of the first line of this paragraph, once a TextFrame or TextColumn has drawn it.
     *
     * @return the coordinate.
     */
    public float getTextY() {
        return yText;
    }

    /**
     * Returns the x coordinate of the top left corner of this paragraph, once a TextFrame or TextColumn has drawn it.
     *
     * @return the coordinate.
     */
    public float getX1() {
        return x1;
    }

    /**
     * Returns the y coordinate of the top left corner of this paragraph, once a TextFrame or TextColumn has drawn it.
     *
     * @return the coordinate.
     */
    public float getY1() {
        return y1;
    }

    /**
     * Returns the x coordinate where the last line of this paragraph ends, once a TextFrame or TextColumn has drawn it.
     *
     * @return the coordinate.
     */
    public float getX2() {
        return x2;
    }

    /**
     * Returns the y coordinate of the bottom of the last line of this paragraph, once a TextFrame or TextColumn has drawn it.
     *
     * @return the coordinate.
     */
    public float getY2() {
        return y2;
    }
}   // End of Paragraph.java
