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
    protected float width = 75f;     // TODO: Rename to cellWidth
    /** The top padding. */
    protected float topPadding = 2f;
    /** The bottom padding. */
    protected float bottomPadding = 2f;
    /** The left padding. */
    protected float leftPadding = 2f;
    /** The right padding. */
    protected float rightPadding = 2f;

    /** The width of the cell borders. */
    protected float lineWidth = 0f;  // TODO: Rename to borderWidth

    /** The background color as an RGB array, or null. */
    protected float[] backgroundColor;
    /** The text color as an RGB array. */
    protected float[] textColor = new float[] {0f, 0f, 0f};
    /** The stroke width. */
    protected float strokeWidth;
    /** The stroke color as an RGB array. */
    protected float[] strokeColor;
    /** The stroke dash pattern. */
    protected String strokeDashPattern = "[] 0";    // Solid

    /** The number of columns this cell spans. */
    protected int colspan = 1;

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
     * Sets the font for this cell.
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
     * Sets the image inside this cell.
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
     * Sets the barcode inside this cell.
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
     * Sets the text box.
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
     * Sets the stroke width.
     *
     * @param strokeWidth the stroke width.
     * @return this Cell object.
     */
    public Cell setStrokeWidth(float strokeWidth) {
        this.strokeWidth = strokeWidth;
        return this;
    }

    /**
     * Returns the stroke width.
     *
     * @return the stroke width.
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
     * Sets the border line width.
     *
     * @param lineWidth the border line width.
     * @return this Cell object.
     */
    public Cell setLineWidth(float lineWidth) {
        this.lineWidth = lineWidth;
        return this;
    }

    /**
     * Returns the border line width.
     *
     * @return the border line width.
     */
    public float getLineWidth() {
        return this.lineWidth;
    }

    /**
     * Sets the text color.
     *
     * @param textColor the text color.
     * @return this Cell object.
     */
    public Cell setBrushColor(float[] textColor) {
        this.textColor = textColor;
        return this;
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
     * Returns the brush color.
     *
     * @return the brush color.
     */
    public float[] getBrushColor() {
        return textColor;
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
     * Sets the background color of this cell.
     *
     * @param color the red, green and blue components, from 0.0 to 1.0.
     * @return this Cell object.
     */
    public Cell setBackgroundColor(float[] color) {
        this.backgroundColor = color;
        return this;
    }

    /**
     * Returns the background color.
     *
     * @return the background color.
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
            this.properties &= 0x00FFFFFF;
        } else {
            this.properties &= 0x00F0FFFF;
        }
        return this;
    }

    /**
     * Sets the cell text alignment.
     *
     * @param alignment the alignment code.
     * Supported values: Align.LEFT, Align.RIGHT and Align.CENTER.
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
            if (getTextAlignment() == Align.LEFT) {
                image.setLocation(x + leftPadding, y + topPadding);
                image.drawOn(page);
            } else if (getTextAlignment() == Align.CENTER) {
                image.setLocation((x + w/2f) - image.getWidth()/2f, y + topPadding);
                image.drawOn(page);
            } else if (getTextAlignment() == Align.RIGHT) {
                image.setLocation((x + w) - (image.getWidth() + leftPadding), y + topPadding);
                image.drawOn(page);
            }
        } else if (barcode != null) {
            try {
                if (getTextAlignment() == Align.LEFT) {
                    barcode.drawOnPageAtLocation(page, x + leftPadding, y + topPadding);
                } else if (getTextAlignment() == Align.CENTER) {
                    float barcodeWidth = barcode.drawOn(null)[0];
                    barcode.drawOnPageAtLocation(page, (x + w/2f) - barcodeWidth/2f, y + topPadding);
                } else if (getTextAlignment() == Align.RIGHT) {
                    float barcodeWidth = barcode.drawOn(null)[0];
                    barcode.drawOnPageAtLocation(page, (x + w) - (barcodeWidth + leftPadding), y + topPadding);
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
        page.fillRect(x, y + lineWidth/2, cellW, cellH);
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
        page.setPenWidth(lineWidth);
        float qWidth = lineWidth / 4;
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
        float xText;
        float yText;
        if (valign == Align.TOP) {
            yText = y + font.getAscent(fontSize) + this.topPadding;
        } else if (valign == Align.CENTER) {
            yText = y + cellH/2 + font.getAscent(fontSize)/2;
        } else if (valign == Align.BOTTOM) {
            yText = (y + cellH) - this.bottomPadding;
        } else {
            throw new Exception("Invalid vertical text alignment option.");
        }

        page.setPenColor(strokeColor);
        if (getTextAlignment() == Align.RIGHT) {
            if (compositeTextLine == null) {
                xText = (x + cellW) - (font.stringWidth(text) + this.rightPadding);
                page.addBMC(StructElem.P, text, text);
                page.drawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
                page.addEMC();
                if (getUnderline()) {
                    underlineText(page, font, text, xText, yText);
                }
                if (getStrikeout()) {
                    strikeoutText(page, font, text, xText, yText);
                }
            } else {
                xText = (x + cellW) - (compositeTextLine.getWidth() + this.rightPadding);
                compositeTextLine.setLocation(xText, yText);
                page.addBMC(StructElem.P, text, text);
                compositeTextLine.drawOn(page);
                page.addEMC();
            }
        } else if (getTextAlignment() == Align.CENTER) {
            if (compositeTextLine == null) {
                xText = x + this.leftPadding +
                        (((cellW - (leftPadding + rightPadding)) - font.stringWidth(text)) / 2);
                page.addBMC(StructElem.P, text, text);
                page.drawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
                page.addEMC();
                if (getUnderline()) {
                    underlineText(page, font, text, xText, yText);
                }
                if (getStrikeout()) {
                    strikeoutText(page, font, text, xText, yText);
                }
            } else {
                xText = x + this.leftPadding +
                        (((cellW - (leftPadding + rightPadding)) - compositeTextLine.getWidth()) / 2);
                compositeTextLine.setLocation(xText, yText);
                page.addBMC(StructElem.P, text, text);
                compositeTextLine.drawOn(page);
                page.addEMC();
            }
        } else if (getTextAlignment() == Align.LEFT) {
            xText = x + this.leftPadding;
            if (compositeTextLine == null) {
                page.addBMC(StructElem.P, text, text);
                page.drawString(font, fallbackFont, fontSize, text, xText, yText, textColor, null);
                page.addEMC();
                if (getUnderline()) {
                    underlineText(page, font, text, xText, yText);
                }
                if (getStrikeout()) {
                    strikeoutText(page, font, text, xText, yText);
                }
            } else {
                compositeTextLine.setLocation(xText, yText);
                page.addBMC(StructElem.P, text, text);
                compositeTextLine.drawOn(page);
                page.addEMC();
            }
        } else {
            throw new Exception("Invalid Text Alignment!");
        }

        if (uri != null) {
            float w = (compositeTextLine != null) ?
                    compositeTextLine.getWidth() : font.stringWidth(text);
            page.addAnnotation(new Annotation(
                    Annotation.Link,
                    xText,
                    (page.height - yText) - font.getAscent(), // (page.height - yText) - font.GetAscent(fontSize),
                    xText + w,
                    (page.height - yText) + font.getDescent(),
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

    private void underlineText(
            Page page, Font font, String text, float x, float y) {
        page.addBMC(StructElem.P, "underline", "underline");
        page.setPenWidth(font.underlineThickness);
        page.moveTo(x, y + font.descent);
        page.lineTo(x + font.stringWidth(text), y + font.descent);
        page.strokePath();
        page.addEMC();
    }

    private void strikeoutText(
            Page page, Font font, String text, float x, float y) {
        page.addBMC(StructElem.P, "strike out", "strike out");
        page.setPenWidth(font.underlineThickness);
        page.moveTo(x, y - font.getAscent()/3f);
        page.lineTo(x + font.stringWidth(text), y - font.getAscent()/3f);
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
