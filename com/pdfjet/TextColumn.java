/*
 * TextColumn.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Used to create text column objects and draw them on a page.
 * Please see Example_10.
 */
public class TextColumn implements Drawable {
    protected int alignment = Align.LEFT;
    protected int rotate;
    protected float x;  // This variable is set in the beginning and only reset after the drawOn
    protected float y;  // This variable is set in the beginning and only reset after the drawOn
    private float w;
    private float h;
    private float x1;
    private float y1;
    private float lineSpacing = 1.0f;
    private float paragraphSpacing = 1.0f;
    private final List<Paragraph> paragraphs;
    private boolean lineBetweenParagraphs = false;

    /**
     *  Create a text column object.
     */
    public TextColumn() {
        this.paragraphs = new ArrayList<Paragraph>();
    }

    /**
     * Create a text column object and set the rotation angle.
     *
     * @param rotateByDegrees the specified rotation angle in degrees.
     * @throws Exception  If an input or output exception occurred
     */
    public TextColumn(int rotateByDegrees) throws Exception {
        this.rotate = rotateByDegrees;
        if (rotate == 0 || rotate == 90 || rotate == 270) {
        } else {
            throw new Exception(
                    "Invalid rotation angle. Please use 0, 90 or 270 degrees.");
        }
        this.paragraphs = new ArrayList<Paragraph>();
    }

    /**
     * Sets the lineBetweenParagraphs private variable value.
     * If the value is set to true - an empty line will be inserted between the current and next paragraphs.
     *
     * @param lineBetweenParagraphs the specified boolean value.
     */
    public void setLineBetweenParagraphs(boolean lineBetweenParagraphs) {
        this.lineBetweenParagraphs = lineBetweenParagraphs;
    }

    public void setParagraphSpacing(float paragraphSpacing) {
        this.paragraphSpacing = paragraphSpacing;
    }

    /**
     * Sets the position of this text column on the page.
     *
     * @param x the x coordinate of the top left corner of this text column when drawn on the page.
     * @param y the y coordinate of the top left corner of this text column when drawn on the page.
     */
    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    /**
     * Sets the position of this text column on the page.
     *
     * @param x the x coordinate of the top left corner of this text column when drawn on the page.
     * @param y the y coordinate of the top left corner of this text column when drawn on the page.
     */
    public void setPosition(double x, double y) {
        setLocation(x, y);
    }

    /**
     * Sets the position of this text column on the page.
     *
     * @param x the x coordinate of the top left corner of this text column when drawn on the page.
     * @param y the y coordinate of the top left corner of this text column when drawn on the page.
     * @return this text column.
     */
    public TextColumn setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        this.x1 = x;
        this.y1 = y;
        return this;
    }

    /**
     * Sets the position of this text column on the page.
     *
     * @param x the x coordinate of the top left corner of this text column when drawn on the page.
     * @param y the y coordinate of the top left corner of this text column when drawn on the page.
     * @return this text column.
     */
    public TextColumn setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    /**
     * Sets the size of this text column.
     *
     * @param w the width of this text column.
     * @param h the height of this text column.
     */
    public void setSize(double w, double h) {
        setSize((float) w, (float) h);
    }

    /**
     * Sets the size of this text column.
     *
     * @param w the width of this text column.
     * @param h the height of this text column.
     */
    public void setSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    /**
     * Sets the desired width of this text column.
     *
     * @param w the width of this text column.
     */
    public void setWidth(float w) {
        this.w = w;
    }

    public float getWidth() {
        return this.w;
    }

    public float getHeight() {
        return this.h;
    }

    /**
     * Sets the text alignment.
     *
     * @param alignment the specified alignment code.
     *                  Supported values: Align.LEFT, Align.RIGHT. Align.CENTER and Align.JUSTIFY
     */
    public void setAlignment(int alignment) {
        this.alignment = alignment;
    }

    /**
     * Sets the spacing between the lines in this text column.
     *
     * @param lineSpacing the line spacing value.
     */
    public void setLineSpacing(double lineSpacing) {
        this.lineSpacing = (float) lineSpacing;
    }

    /**
     * Sets the spacing between the lines in this text column.
     *
     * @param lineSpacing the line spacing value.
     */
    public void setLineSpacing(float lineSpacing) {
        this.lineSpacing = lineSpacing;
    }

    /**
     * Adds a new paragraph to this text column.
     *
     * @param paragraph the new paragraph object.
     */
    public void addParagraph(Paragraph paragraph) {
        this.paragraphs.add(paragraph);
    }

    /**
     * Removes the last paragraph added to this text column.
     */
    public void removeLastParagraph() {
        if (this.paragraphs.size() >= 1) {
            this.paragraphs.remove(this.paragraphs.size() - 1);
        }
    }

    /**
     * Returns dimension object containing the width and height of this component.
     * Please see Example_29.
     *
     * @return dimension object containing the width and height of this component.
     * @throws Exception  If an input or output exception occurred
     */
    public Dimension getSize() throws Exception {
        float[] xy = drawOn(null);
        return new Dimension(this.w, xy[1] - this.y);
    }

    /**
     * Draws this text column on the specified page if the 'draw' boolean value is 'true'.
     *
     * @param page the page to draw this text column on.
     * @return the point with x and y coordinates of the location where to draw the next component.
     * @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        float[] xy = null;
        for (int i = 0; i < paragraphs.size(); i++) {
            Paragraph paragraph = paragraphs.get(i);
            this.alignment = paragraph.alignment;
            xy = drawParagraphOn(page, paragraph);
        }
        // Restore the original location
        setLocation(this.x, this.y);
        if (this.getHeight() > xy[1]) {
            xy[1] = this.getHeight();
        }
        return xy;
    }

    private float[] drawParagraphOn(Page page, Paragraph paragraph) throws Exception {
        List<TextLine> list = new ArrayList<TextLine>();
        float lineHeight = 0f;
        float maxAscent = 0f;
        for (TextLine line : paragraph.lines) {
            if ((line.getHeight() * lineSpacing) > lineHeight) {
                lineHeight = line.getHeight() * lineSpacing;
            }
            if (line.font.getAscent(line.fontSize) > maxAscent) {
                maxAscent = line.font.getAscent(line.fontSize);
            }
        }
        if (rotate == 0) {
            y1 += maxAscent;
        } else if (rotate == 90) {
            x1 += maxAscent;
        } else if (rotate == 270) {
            x1 -= maxAscent;
        }

        float runLength = 0f;
        for (TextLine line : paragraph.lines) {
            String[] tokens = line.text.split("\\s+");
            TextLine text = null;
            for (String token : tokens) {
                text = new TextLine(line.font, token + Single.space);
                text.setFallbackFont(line.getFallbackFont());
                text.setFontSize(line.getFontSize());
                text.setTextColor(line.getTextColor());
                text.setUnderline(line.getUnderline());
                text.setStrikeout(line.getStrikeout());
                text.setVerticalOffset(line.getVerticalOffset());
                text.setURIAction(line.getURIAction());
                text.setGoToAction(line.getGoToAction());
                runLength += text.getWidth();
                if (runLength < this.w) {
                    list.add(text);
                } else {
                    drawLineOfText(page, list);
                    moveToNextLine(lineHeight);
                    list.clear();
                    list.add(text);
                    runLength = text.getWidth();
                }
            }
            text.isLastToken = true;
        }
        drawNonJustifiedLine(page, list);

        if (lineBetweenParagraphs) {
            moveToNextLine(lineHeight);
        }

        return moveToNextParagraph(lineHeight * this.paragraphSpacing);
    }

    private float[] moveToNextLine(float lineHeight) {
        if (rotate == 0) {
            this.x1 = x;
            this.y1 += lineHeight;
        } else if (rotate == 90) {
            this.x1 += lineHeight;
            this.y1 = y;
        } else if (rotate == 270) {
            this.x1 -= lineHeight;
            this.y1 = y;
        }
        return new float[] {x1, y1};
    }

    private float[] moveToNextParagraph(float paragraphSpacing) {
        if (rotate == 0) {
            x1 = x;
            y1 += paragraphSpacing;
        } else if (rotate == 90) {
            x1 += paragraphSpacing;
            y1 = y;
        } else if (rotate == 270) {
            x1 -= paragraphSpacing;
            y1 = y;
        }
        return new float[] {x1, y1};
    }

    private float[] drawLineOfText(Page page, List<TextLine> list) throws Exception {
        if (alignment == Align.JUSTIFY) {
            float sumOfWordWidths = 0f;
            for (TextLine textLine : list) {
                sumOfWordWidths += textLine.getWidth();
            }
            float dx = (w - sumOfWordWidths) / (list.size() - 1);

            for (TextLine textLine : list) {
                textLine.setLocation(x1, y1 + textLine.getVerticalOffset());
                if (textLine.getGoToAction() != null) {
                    page.addAnnotation(new Annotation(
                            Annotation.Link,
                            x,
                            y - textLine.font.getAscent(),
                            x + textLine.getWidth(),
                            y + textLine.font.getDescent(),
                            null,                       // Vertices
                            null,                       // Fill Color
                            0f,                         // Transparency
                            null,                       // Title
                            null,                       // Contents
                            null,                       // The URI
                            textLine.getGoToAction(),   // The destination name
                            null,
                            null,
                            null));
                }

                if (rotate == 0) {
                    textLine.setTextDirection(0);
                    textLine.drawOn(page);
                    x1 += textLine.getWidth() + dx;
                } else if (rotate == 90) {
                    textLine.setTextDirection(90);
                    textLine.drawOn(page);
                    y1 -= textLine.getWidth() + dx;
                } else if (rotate == 270) {
                    textLine.setTextDirection(270);
                    textLine.drawOn(page);
                    y1 += textLine.getWidth() + dx;
                }
            }
        } else {
            return drawNonJustifiedLine(page, list);
        }
        return new float[] {x1, y1};
    }

    private float[] drawNonJustifiedLine(Page page, List<TextLine> list) throws Exception {
        float runLength = 0f;
        for (TextLine textLine : list) {
            runLength += textLine.getWidth();
        }

        if (alignment == Align.CENTER) {
            if (rotate == 0) {
                x1 = x + ((w - runLength) / 2);
            } else if (rotate == 90) {
                y1 = y - ((w - runLength) / 2);
            } else if (rotate == 270) {
                y1 = y + ((w - runLength) / 2);
            }
        } else if (alignment == Align.RIGHT) {
            if (rotate == 0) {
                x1 = x + (w - runLength);
            } else if (rotate == 90) {
                y1 = y - (w - runLength);
            } else if (rotate == 270) {
                y1 = y + (w - runLength);
            }
        }

        for (TextLine textLine : list) {
            textLine.setLocation(x1, y1 + textLine.getVerticalOffset());
            if (textLine.getGoToAction() != null) {
                page.addAnnotation(new Annotation(
                        Annotation.Link,
                        x,
                        y - textLine.font.getAscent(),
                        x + textLine.getWidth(),
                        y + textLine.font.getDescent(),
                        null,                       // Vertices
                        null,                       // Fill Color
                        0f,                         // Transparency
                        null,                       // Title
                        null,                       // Contents
                        null,                       // The URI
                        textLine.getGoToAction(),   // The destination name
                        null,
                        null,
                        null));
            }

            if (rotate == 0) {
                textLine.setTextDirection(0);
                textLine.drawOn(page);
                x1 += textLine.getWidth();
            } else if (rotate == 90) {
                textLine.setTextDirection(90);
                textLine.drawOn(page);
                y1 -= textLine.getWidth();
            } else if (rotate == 270) {
                textLine.setTextDirection(270);
                textLine.drawOn(page);
                y1 += textLine.getWidth();
            }
        }
        return new float[] {x1, y1};
    }

    /**
     * Adds a new paragraph with Chinese text to this text column.
     *
     * @param font the font used by this paragraph.
     * @param chinese the Chinese text.
     */
    public void addChineseParagraph(Font font, String chinese) {
        Paragraph paragraph;
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < chinese.length(); i++) {
            char ch = chinese.charAt(i);
            if (font.stringWidth(buf.toString() + ch) > w) {
                paragraph = new Paragraph();
                paragraph.add(new TextLine(font, buf.toString()));
                addParagraph(paragraph);
                buf.setLength(0);
            }
            buf.append(ch);
        }
        paragraph = new Paragraph();
        paragraph.add(new TextLine(font, buf.toString()));
        addParagraph(paragraph);
    }

    /**
     * Adds a new paragraph with Japanese text to this text column.
     *
     * @param font the font used by this paragraph.
     * @param japanese the Japanese text.
     */
    public void addJapaneseParagraph(Font font, String japanese) {
        addChineseParagraph(font, japanese);
    }
}   // End of TextColumn.java
