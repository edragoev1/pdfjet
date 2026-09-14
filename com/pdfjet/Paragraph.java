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
    /** The x coordinate where the text of this paragraph starts. */
    public float xText;

    /** The baseline y coordinate of the first line of this paragraph. */
    public float yText;

    /** The x coordinate of the top left corner of this paragraph. */
    public float x1;

    /** The y coordinate of the top left corner of this paragraph. */
    public float y1;

    /** The x coordinate where the last line of this paragraph ends. */
    public float x2;

    /** The y coordinate of the bottom of the last line of this paragraph. */
    public float y2;

    /** The text lines of this paragraph. */
    protected List<TextLine> lines = null;
    /** The alignment of this paragraph. */
    protected Alignment alignment = Alignment.LEFT;
    // True after setTextAlignment. Otherwise the alignment of the text column applies.
    boolean explicitAlignment = false;

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
}   // End of Paragraph.java
