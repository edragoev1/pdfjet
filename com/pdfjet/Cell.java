/*
 * Cell.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import com.pdfjet.barcodes.*;

/**
 * Used to create table cell objects.
 * See the Table class for more information.
 */
public class Cell {
    /** The font of the cell text. */
    protected Font font;
    /** The font used for characters that the primary font does not have. */
    protected Font fallbackFont;
    /** The font size. */
    protected float fontSize;
    /** The cell text. */
    protected String text;
    /** The image drawn in this cell. */
    protected Image image;
    /** The barcode drawn in this cell. */
    protected Barcode barcode;
    /** The text box drawn in this cell. */
    protected TextBox textBox;
    /** The text block drawn in this cell. */
    protected TextBlock textBlock;
    /** The text column drawn in this cell. */
    protected TextColumn textColumn;
    /** The point drawn in this cell. */
    protected Point point;
    /** The composite text line drawn in this cell. */
    protected CompositeTextLine compositeTextLine;
    /** The width of this cell. */
    protected float width = 75f;
    /** The top padding. */
    protected float topPadding = 2f;
    /** The bottom padding. */
    protected float bottomPadding = 2f;
    /** The left padding. */
    protected float leftPadding = 2f;
    /** The right padding. */
    protected float rightPadding = 2f;

    /** The background color as an RGB array, or null. */
    protected float[] backgroundColor;
    /** The text color as an RGB array. */
    protected float[] textColor = new float[] {0f, 0f, 0f};
    /** The width of the cell borders. */
    protected float strokeWidth;
    /** The stroke color as an RGB array. */
    protected float[] strokeColor;

    // Cell properties
    // Colspan:
    // bits 0 to 15
    // Border:
    // bit 16 - top
    // bit 17 - bottom
    // bit 18 - left
    // bit 19 - right
    // Text Alignment:
    // bit 20
    // bit 21
    // Text Decoration:
    // bit 22 - underline
    // bit 23 - strikeout
    // Future use:
    // bits 24 to 31
    private int properties = 0x00050001;    // Set only left and top borders!
    private String uri;
    private int valign = Align.TOP;

    /**
     * Creates a cell object and sets the font.
     *
     * @param font the font.
     */
    public Cell(Font font) {
        this.font = font;
        this.fontSize = font.getSize();
        this.fallbackFont = font;
    }

    /**
     * Creates a cell object and sets the font and the cell text.
     *
     * @param font the font.
     * @param text the text.
     */
    public Cell(Font font, String text) {
        this.font = font;
        this.fontSize = font.getSize();
        this.fallbackFont = font;
        this.text = text;
    }

    /**
     * Sets the font for this cell. The font size does not change; set it with setFontSize.
     *
     * @param font the font.
     * @return this Cell object.
     */
    public Cell setFont(Font font) {
        this.font = font;
        return this;
    }

    /**
     * Sets the fallback font for this cell.
     *
     * @param fallbackFont the fallback font.
     * @return this Cell object.
     */
    public Cell setFallbackFont(Font fallbackFont) {
        this.fallbackFont = fallbackFont;
        return this;
    }

    /**
     * Returns the font used by this cell.
     *
     * @return the font.
     */
    public Font getFont() {
        return this.font;
    }

    /**
     * Returns the fallback font used by this cell.
     *
     * @return the fallback font.
     */
    public Font getFallbackFont() {
        return this.fallbackFont;
    }

    /**
     * Sets the cell text.
     *
     * @param text the cell text.
     * @return this Cell object.
     */
    public Cell setText(String text) {
        this.text = text;
        return this;
    }

    /**
     * Returns the cell text.
     *
     * @return the cell text.
     */
    public String getText() {
        return this.text;
    }

    /**
     * Sets the font size of the cell text.
     *
     * @param fontSize the font size.
     * @return this Cell object.
     */
    public Cell setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /**
     * Sets the image inside this cell and clears the cell text.
     *
     * @param image the image.
     * @return this Cell object.
     */
    public Cell setImage(Image image) {
        this.image = image;
        this.text = null;
        return this;
    }

    /**
     * Sets the barcode inside this cell and clears the cell text.
     *
     * @param barcode the barcode.
     * @return this Cell object.
     */
    public Cell setBarcode(Barcode barcode) {
        this.barcode = barcode;
        this.text = null;
        return this;
    }

    /**
     * Returns the barcode drawn in this cell.
     *
     * @return the barcode.
     */
    public Barcode getBarcode() {
        return this.barcode;
    }

    /**
     * Returns the cell image.
     *
     * @return the image.
     */
    public Image getImage() {
        return this.image;
    }

    /**
     * Sets the point inside this cell.
     * See the Point class and Example_09 for more information.
     *
     * @param point the point.
     * @return this Cell object.
     */
    public Cell setPoint(Point point) {
        this.point = point;
        return this;
    }

    /**
     * Returns the cell point.
     *
     * @return the point.
     */
    public Point getPoint() {
        return this.point;
    }

    /**
     * Sets the composite text object.
     *
     * @param compositeTextLine the composite text object.
     * @return this Cell object.
     */
    public Cell setCompositeTextLine(CompositeTextLine compositeTextLine) {
        this.compositeTextLine = compositeTextLine;
        return this;
    }

    /**
     * Returns the composite text object.
     *
     * @return the composite text object.
     */
    public CompositeTextLine getCompositeTextLine() {
        return this.compositeTextLine;
    }

    /**
     * Sets the text box drawn inside this cell and clears the cell text.
     *
     * @param textBox the text box.
     * @return this Cell object.
     */
    public Cell setTextBox(TextBox textBox) {
        this.textBox = textBox;
        this.text = null;
        return this;
    }

    /**
     * Sets the text block drawn inside this cell.
     *
     * @param textBlock the text block.
     * @return this Cell object.
     */
    public Cell setTextBlock(TextBlock textBlock) {
        this.textBlock = textBlock;
        return this;
    }

    /**
     * Returns the text block drawn inside this cell.
     *
     * @return the text block.
     */
    public TextBlock getTextBlock() {
        return this.textBlock;
    }

    /**
     * Sets the text column drawn inside this cell, and widens the cell to fit it.
     *
     * @param textColumn the text column.
     * @return this Cell object.
     */
    public Cell setTextColumn(TextColumn textColumn) {
        this.textColumn = textColumn;
        this.width = textColumn.getWidth() + this.leftPadding + this.rightPadding;
        return this;
    }

    /**
     * Returns the text column drawn inside this cell.
     *
     * @return the text column.
     */
    public TextColumn getTextColumn() {
        return this.textColumn;
    }

    /**
     * Sets the width of this cell.
     *
     * @param width the specified width.
     * @return this Cell object.
     */
    public Cell setWidth(float width) {
        this.width = width;
        if (textBox != null) {
            textBox.setWidth(this.width - (this.leftPadding + this.rightPadding));
        } else if (textBlock != null) {
            textBlock.setWidth(this.width - (this.leftPadding + this.rightPadding));
        }
        return this;
    }

    /**
     * Returns the cell width.
     *
     * @return the cell width.
     */
    public float getWidth() {
        return this.width;
    }

    /**
     * Sets the top padding of this cell.
     *
     * @param padding the top padding.
     * @return this Cell object.
     */
    public Cell setTopPadding(float padding) {
        this.topPadding = padding;
        return this;
    }

    /**
     * Returns the top padding of this cell.
     *
     * @return the top padding.
     */
    public float getTopPadding() {
        return this.topPadding;
    }

    /**
     * Sets the bottom padding of this cell.
     *
     * @param padding the bottom padding.
     * @return this Cell object.
     */
    public Cell setBottomPadding(float padding) {
        this.bottomPadding = padding;
        return this;
    }

    /**
     * Returns the bottom padding of this cell.
     *
     * @return the bottom padding.
     */
    public float getBottomPadding() {
        return this.bottomPadding;
    }

    /**
     * Sets the left padding of this cell.
     *
     * @param padding the left padding.
     * @return this Cell object.
     */
    public Cell setLeftPadding(float padding) {
        this.leftPadding = padding;
        return this;
    }

    /**
     * Returns the left padding of this cell.
     *
     * @return the left padding.
     */
    public float getLeftPadding() {
        return this.leftPadding;
    }

    /**
     * Sets the right padding of this cell.
     *
     * @param padding the right padding.
     * @return this Cell object.
     */
    public Cell setRightPadding(float padding) {
        this.rightPadding = padding;
        return this;
    }

    /**
     * Returns the right padding of this cell.
     *
     * @return the right padding.
     */
    public float getRightPadding() {
        return this.rightPadding;
    }

    /**
     * Sets the top, bottom, left and right paddings of this cell.
     *
     * @param padding the right padding.
     * @return this Cell object.
     */
    public Cell setPadding(float padding) {
        this.topPadding = padding;
        this.bottomPadding = padding;
        this.leftPadding = padding;
        this.rightPadding = padding;
        return this;
    }

    /**
     * Sets the stroke color of this cell.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Cell object.
     */
    public Cell setStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the stroke color of this cell.
     *
     * @param strokeColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Cell object.
     */
    public Cell setStrokeColor(float[] strokeColor) {
        this.strokeColor = strokeColor;
        return this;
    }

    /**
     * Returns the stroke color.
     *
     * @return the stroke color.
     */
    public float[] getStrokeColor() {
        return this.strokeColor;
    }

    /**
     * Sets the width of the cell borders.
     *
     * @param strokeWidth the width of the cell borders.
     * @return this Cell object.
     */
    public Cell setStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /**
     * Returns the width of the cell borders.
     *
     * @return the width of the cell borders.
     */
    public float getStrokeWidth() {
        return this.strokeWidth;
    }

    /**
     * Returns the cell height.
     *
     * @param width the cell width.
     * @throws Exception is there is an error.
     * @return the cell height.
     */
    public float getHeight(float width) throws Exception {
        float cellHeight = 0f;
        if (textBox != null) {
            textBox.setWidth(width);
            cellHeight = (textBox.drawOn(null)[1] - textBox.y) + topPadding + bottomPadding;
        } else if (textBlock != null) {
            textBlock.setWidth(width);
            cellHeight = (textBlock.drawOn(null)[1] - textBlock.y) + topPadding + bottomPadding;
        } else if (textColumn != null) {
            cellHeight = (textColumn.drawOn(null)[1] - textColumn.y) + topPadding + bottomPadding;
        } else if (image != null) {
            cellHeight = image.getHeight() + topPadding + bottomPadding;
        } else if (barcode != null) {
            cellHeight = barcode.getHeight() + topPadding + bottomPadding;
        } else if (text != null) {
            float fontHeight = font.getHeight();
            if (fallbackFont != null && fallbackFont.getHeight() > fontHeight) {
                fontHeight = fallbackFont.getHeight();
            }
            cellHeight = fontHeight + topPadding + bottomPadding;
        }
        return cellHeight;
    }

    /**
     * Sets the text color of this cell.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Cell object.
     */
    public Cell setTextColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.textColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the text color of this cell.
     *
     * @param textColor the red, green and blue components, from 0.0 to 1.0.
     * @return this Cell object.
     */
    public Cell setTextColor(float[] textColor) {
        this.textColor = textColor;
        return this;
    }

    /**
     * Returns the text color of this cell.
     *
     * @return the red, green and blue components, from 0.0 to 1.0.
     */
    public float[] getTextColor() {
        return textColor;
    }

    /**
     * Sets the background color of this cell.
     *
     * @param color the color as a 0xRRGGBB value, for example Color.blue.
     * @return this Cell object.
     */
    public Cell setBackgroundColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.backgroundColor = new float[] {r, g, b};
        return this;
    }

    /**
     * Sets the background color of this cell, or removes the background.
     *
     * @param color the red, green and blue components, from 0.0 to 1.0, or null.
     * @return this Cell object.
     */
    public Cell setBackgroundColor(float[] color) {
        this.backgroundColor = color;
        return this;
    }

    /**
     * Returns the background color.
     *
     * @return the background color, or null if the cell has no background.
     */
    public float[] getBackgroundColor() {
        return this.backgroundColor;
    }

    /**
     * Sets the properties bit field: colspan, borders, text alignment and decoration.
     *
     * @param properties the properties.
     */
    protected void setProperties(int properties) {
        this.properties = properties;
    }

    /**
     * Returns the properties bit field: colspan, borders, text alignment and decoration.
     *
     * @return the properties.
     */
    protected int getProperties() {
        return this.properties;
    }

    /**
     * Sets the column span private variable.
     *
     * @param colspan the specified column span value.
     * @return this Cell object.
     */
    public Cell setColSpan(int colspan) {
        this.properties &= 0x00FF0000;
        this.properties |= (colspan & 0x0000FFFF);
        return this;
    }

    /**
     * Returns the column span private variable value.
     *
     * @return the column span value.
     */
    public int getColSpan() {
        return (this.properties & 0x0000FFFF);
    }

    /**
     * Sets the cell border object.
     *
     * @param border the border object.
     * @param visible the visibility of the border.
     * @return this Cell object.
     */
    public Cell setBorder(int border, boolean visible) {
        if (visible) {
            this.properties |= border;
        } else {
            this.properties &= (~border & 0x00FFFFFF);
        }
        return this;
    }

    /**
     * Returns the cell border object.
     *
     * @param border the border.
     * @return the cell border object.
     */
    public boolean getBorder(int border) {
        return (this.properties & border) != 0;
    }

    /**
     * Sets all cell borders.
     * @param borders true or false.
     * @return this Cell object.
     */
    public Cell setBorders(boolean borders) {
        if (borders) {
            this.properties |= 0x000F0000;
        } else {
            this.properties &= 0x00F0FFFF;
        }
        return this;
    }

    /**
     * Sets whether the top border of this cell is drawn.
     *
     * @param topBorder true to draw the top border.
     * @return this Cell object.
     */
    public Cell setTopBorder(boolean topBorder) {
        return setBorder(Border.TOP, topBorder);
    }

    /**
     * Returns true if the top border of this cell is drawn.
     *
     * @return true if the top border is drawn.
     */
    public boolean getTopBorder() {
        return getBorder(Border.TOP);
    }

    /**
     * Sets whether the bottom border of this cell is drawn.
     *
     * @param bottomBorder true to draw the bottom border.
     * @return this Cell object.
     */
    public Cell setBottomBorder(boolean bottomBorder) {
        return setBorder(Border.BOTTOM, bottomBorder);
    }

    /**
     * Returns true if the bottom border of this cell is drawn.
     *
     * @return true if the bottom border is drawn.
     */
    public boolean getBottomBorder() {
        return getBorder(Border.BOTTOM);
    }

    /**
     * Sets whether the left border of this cell is drawn.
     *
     * @param leftBorder true to draw the left border.
     * @return this Cell object.
     */
    public Cell setLeftBorder(boolean leftBorder) {
        return setBorder(Border.LEFT, leftBorder);
    }

    /**
     * Returns true if the left border of this cell is drawn.
     *
     * @return true if the left border is drawn.
     */
    public boolean getLeftBorder() {
        return getBorder(Border.LEFT);
    }

    /**
     * Sets whether the right border of this cell is drawn.
     *
     * @param rightBorder true to draw the right border.
     * @return this Cell object.
     */
    public Cell setRightBorder(boolean rightBorder) {
        return setBorder(Border.RIGHT, rightBorder);
    }

    /**
     * Returns true if the right border of this cell is drawn.
     *
     * @return true if the right border is drawn.
     */
    public boolean getRightBorder() {
        return getBorder(Border.RIGHT);
    }

    /**
     * Sets the cell text alignment.
     *
     * @param alignment the alignment code.
     * Supported values: Align.LEFT, Align.RIGHT, Align.CENTER and Align.JUSTIFY,
     * which draws the single line of cell text left aligned.
     * @return this Cell object.
     */
    public Cell setTextAlignment(int alignment) {
        this.properties &= 0x00CFFFFF;
        this.properties |= (alignment & 0x00300000);
        return this;
    }

    /**
     * Returns the text alignment.
     *
     * @return the text horizontal alignment code.
     */
    public int getTextAlignment() {
        return (this.properties & 0x00300000);
    }

    /**
     * Sets the cell text vertical alignment.
     *
     * @param alignment the alignment code.
     * Supported values: Align.TOP, Align.CENTER and Align.BOTTOM.
     * @return this Cell object.
     */
    public Cell setVerTextAlignment(int alignment) {
        this.valign = alignment;
        return this;
    }

    /**
     * Returns the cell text vertical alignment.
     *
     * @return the vertical alignment code.
     */
    public int getVerTextAlignment() {
        return this.valign;
    }

    /**
     * Sets the underline text parameter.
     * If the value of the underline variable is 'true' - the text is underlined.
     *
     * @param underline the underline text parameter.
     * @return this Cell object.
     */
    public Cell setUnderline(boolean underline) {
        if (underline) {
            this.properties |= 0x00400000;
        } else {
            this.properties &= 0x00BFFFFF;
        }
        return this;
    }

    /**
     * Returns the underline text parameter.
     *
     * @return the underline text parameter.
     */
    public boolean getUnderline() {
        return (properties & 0x00400000) != 0;
    }

    /**
     * Sets the strikeout text parameter.
     *
     * @param strikeout the strikeout text parameter.
     * @return this Cell object.
     */
    public Cell setStrikeout(boolean strikeout) {
        if (strikeout) {
            this.properties |= 0x00800000;
        } else {
            this.properties &= 0x007FFFFF;
        }
        return this;
    }

    /**
     * Returns the strikeout text parameter.
     *
     * @return the strikeout text parameter.
     */
    public boolean getStrikeout() {
        return (properties & 0x00800000) != 0;
    }

    /**
     * Sets the URI action.
     *
     * @param uri the URI.
     * @return this Cell object.
     */
    public Cell setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     * Draws the point, text and borders of this cell.
     *
     * @param page the page to draw on.
     * @param x the x coordinate of the top left corner.
     * @param y the y coordinate of the top left corner.
     * @param w the width of the cell.
     * @param h the height of the cell.
     * @throws Exception if an input or output exception occurred.
     */
    protected void drawOn(
            Page page,
            float x,
            float y,
            float w,
            float h) throws Exception {
        if (backgroundColor != null) {
            drawBackground(page, x, y, w, h);
        }

        if (text != null && !text.equals("")) {
            drawText(page, x, y, w, h);
        } else if (textBox != null) {
            textBox.setLocation(x + leftPadding, y + topPadding);
            textBox.setWidth(w - (leftPadding + rightPadding));
            textBox.drawOn(page);
        } else if (textBlock != null) {
            textBlock.setLocation(x + leftPadding, y + topPadding);
            textBlock.setWidth(w - (leftPadding + rightPadding));
            textBlock.drawOn(page);
        } else if (textColumn != null) {
            textColumn.setLocation(x + leftPadding, y + topPadding);
            textColumn.drawOn(page);
        } else if (image != null) {
            if (getTextAlignment() == Align.RIGHT) {
                image.setLocation((x + w) - (image.getWidth() + rightPadding), y + topPadding);
            } else if (getTextAlignment() == Align.CENTER) {
                image.setLocation((x + w/2f) - image.getWidth()/2f, y + topPadding);
            } else {
                image.setLocation(x + leftPadding, y + topPadding);
            }
            image.drawOn(page);
        } else if (barcode != null) {
            try {
                if (getTextAlignment() == Align.RIGHT) {
                    float barcodeWidth = barcode.drawOn(null)[0];
                    barcode.drawOnPageAtLocation(page, (x + w) - (barcodeWidth + rightPadding), y + topPadding);
                } else if (getTextAlignment() == Align.CENTER) {
                    float barcodeWidth = barcode.drawOn(null)[0];
                    barcode.drawOnPageAtLocation(page, (x + w/2f) - barcodeWidth/2f, y + topPadding);
                } else {
                    barcode.drawOnPageAtLocation(page, x + leftPadding, y + topPadding);
                }
            } catch (Exception e) {
                e.printStackTrace();
            }
        }

        drawBorders(page, x, y, w, h);
        if (point != null) {
            if (point.align == Align.LEFT) {
                point.x = x + 2*point.r;
            } else if (point.align == Align.RIGHT) {
                point.x = (x + w) - this.rightPadding/2;
            }
            point.y = y + h/2;
            page.setBrushColor(point.getFillColor());
            if (point.getURIAction() != null) {
                page.addAnnotation(new Annotation(
                        Annotation.Link,
                        point.x - point.r,
                        point.y - point.r,
                        point.x + point.r,
                        point.y + point.r,
                        null,   // Vertices
                        null,   // Fill Color
                        0f,     // Transparency
                        null,   // Title
                        null,   // Contents
                        point.getURIAction(),
                        null,
                        null,
                        null,
                        null));
            }
            page.drawPoint(point);
        }
    }

    private void drawBackground(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.addArtifactBMC();
        page.setBrushColor(backgroundColor);
        page.fillRect(x, y + strokeWidth/2, cellW, cellH);
        page.addEMC();
    }

    private void drawBorders(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) {
        page.addArtifactBMC();
        page.setPenColor(strokeColor);
        page.setPenWidth(strokeWidth);
        float qWidth = strokeWidth / 4;
        if (getBorder(Border.TOP)) {
            page.moveTo(x - qWidth, y);
            page.lineTo(x + cellW, y);
            page.strokePath();
        }
        if (getBorder(Border.BOTTOM)) {
            page.moveTo(x - qWidth, y + cellH);
            page.lineTo(x + cellW, y + cellH);
            page.strokePath();
        }
        if (getBorder(Border.LEFT)) {
            page.moveTo(x, y - qWidth);
            page.lineTo(x, y + cellH + qWidth);
            page.strokePath();
        }
        if (getBorder(Border.RIGHT)) {
            page.moveTo(x + cellW, y - qWidth);
            page.lineTo(x + cellW, y + cellH + qWidth);
            page.strokePath();
        }
        page.addEMC();
    }

    private void drawText(
            Page page,
            float x,
            float y,
            float cellW,
            float cellH) throws Exception {
        float ascent = font.getAscent(fontSize);
        float yText;
        if (valign == Align.TOP) {
            yText = y + ascent + this.topPadding;
        } else if (valign == Align.CENTER) {
            yText = y + cellH/2 + ascent/2;
        } else if (valign == Align.BOTTOM) {
            yText = (y + cellH) - this.bottomPadding;
        } else {
            throw new Exception("Invalid vertical text alignment option.");
        }

        page.setPenColor(strokeColor);
        float xText;
        if (getTextAlignment() == Align.RIGHT) {
            xText = (x + cellW) - (getTextWidth() + this.rightPadding);
        } else if (getTextAlignment() == Align.CENTER) {
            xText = x + this.leftPadding +
                    (((cellW - (leftPadding + rightPadding)) - getTextWidth()) / 2);
        } else {
            // Align.LEFT, and Align.JUSTIFY, which a single line of text cannot use.
            xText = x + this.leftPadding;
        }
        if (compositeTextLine == null) {
            page.addBMC(StructElem.P, text, text);
            page.drawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
            page.addEMC();
            if (getUnderline()) {
                underlineText(page, xText, yText);
            }
            if (getStrikeout()) {
                strikeoutText(page, xText, yText);
            }
        } else {
            compositeTextLine.setLocation(xText, yText);
            // The text lines of the composite mark their own text.
            compositeTextLine.drawOn(page);
        }

        if (uri != null) {
            page.addAnnotation(new Annotation(
                    Annotation.Link,
                    xText,
                    yText - ascent,
                    xText + getTextWidth(),
                    yText + font.getDescent(fontSize),
                    null,       // Vertices
                    null,       // Fill Color
                    0f,         // Transparency
                    null,       // Title
                    null,       // Contents
                    uri,
                    null,
                    null,
                    null,
                    null));
        }
    }

    // Returns the width of the composite text line, or of the cell text drawn
    // with the font and the fallback font at the font size of this cell.
    private float getTextWidth() throws Exception {
        if (compositeTextLine != null) {
            return compositeTextLine.getWidth();
        }
        return font.stringWidth(fallbackFont, fontSize, text);
    }

    private void underlineText(Page page, float x, float y) throws Exception {
        float descent = font.getDescent(fontSize);
        page.addBMC(StructElem.P, "underline", "underline");
        page.setPenWidth(font.getUnderlineThickness(fontSize));
        page.moveTo(x, y + descent);
        page.lineTo(x + getTextWidth(), y + descent);
        page.strokePath();
        page.addEMC();
    }

    private void strikeoutText(Page page, float x, float y) throws Exception {
        float ascent = font.getAscent(fontSize);
        page.addBMC(StructElem.P, "strike out", "strike out");
        page.setPenWidth(font.getUnderlineThickness(fontSize));
        page.moveTo(x, y - ascent/3f);
        page.lineTo(x + getTextWidth(), y - ascent/3f);
        page.strokePath();
        page.addEMC();
    }

    /**
     * Returns the text box.
     *
     * @return the text box.
     */
    public TextBox getTextBox() {
        return textBox;
    }
}   // End of Cell.java
