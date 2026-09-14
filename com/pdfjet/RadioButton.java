/*
 * RadioButton.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Creates a RadioButton, which can be set selected or unselected.
 */
public class RadioButton implements Drawable {
    private boolean selected = false;
    private float x;
    private float y;
    private float r1;
    private float r2;
    private float penWidth;
    private Font font;
    private float fontSize;
    private String label = "";
    private String uri = null;

    private String language = null;
    private String actualText = null;
    private String altDescription = null;

    /**
     *  Creates a RadioButton that is not selected.
     *
     *  @param font the font to use.
     *  @param label the label to use.
     */
    public RadioButton(Font font, String label) {
        this.font = font;
        this.fontSize = font.getSize();
        this.label = label;
    }

    /**
     *  Sets the size of the label text. The font keeps its own size.
     *
     *  @param fontSize the fontSize to use.
     *  @return this RadioButton.
     */
    public RadioButton setFontSize(float fontSize) {
        this.fontSize = fontSize;
        return this;
    }

    /**
     *  Set the x,y location on the Page.
     *
     *  @param x the x coordinate on the Page.
     *  @param y the y coordinate on the Page.
     *  @return this RadioButton.
     */
    public RadioButton setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     *  Sets the URI for the "click text line" action.
     *
     *  @param uri the URI.
     *  @return this RadioButton.
     */
    public RadioButton setURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /**
     *  Selects or deselects this radio button.
     *
     *  @param selected the selection flag.
     *  @return this RadioButton.
     */
    public RadioButton select(boolean selected) {
        this.selected = selected;
        return this;
    }

    /**
     *  Sets the alternate description of this radio button.
     *
     *  @param altDescription the alternate description of the radio button.
     *  @return this RadioButton.
     */
    public RadioButton setAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /**
     *  Sets the actual text for this radio button.
     *
     *  @param actualText the actual text for the radio button.
     *  @return this RadioButton.
     */
    public RadioButton setActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /**
     *  Draws this RadioButton on the specified Page.
     *
     *  @param page the Page where the RadioButton is to be drawn.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception  If an input or output exception occurred
     */
    public float[] drawOn(Page page) throws Exception {
        page.addBDC(StructElem.P, language, actualText, altDescription);

        this.r1 = font.getAscent(fontSize)/2;
        this.r2 = r1/2;
        this.penWidth = r1/10;

        float yBox = y;
        page.setPenWidth(1f);
        page.setPenColor(Color.black);
        page.setStrokeDashPattern("[] 0");
        page.setBrushColor(Color.black);
        page.drawCircle(x + r1 + penWidth, yBox + r1 + penWidth, r1);

        if (this.selected) {
            page.drawCircle(x + r1 + penWidth, yBox + r1 + penWidth, r2, PathOperator.FILL);
        }

        // A linked label is blue.
        float[] textColor = (uri != null) ? new float[] {0f, 0f, 1f} : new float[] {0f, 0f, 0f};
        page.drawString(font, fontSize, label, x + 3*r1, y + font.getAscent(fontSize), textColor, null);
        page.setPenWidth(0f);
        page.setBrushColor(Color.black);

        page.addEMC();

        if (uri != null) {
            page.addAnnotation(new Annotation(
                    Annotation.Link,
                    x + 3*r1,
                    y,
                    x + 3*r1 + font.stringWidth(fontSize, label),
                    y + font.getBodyHeight(fontSize),
                    null,   // Vertices
                    null,   // Fill Color
                    0f,     // Opacity
                    null,   // Title
                    null,   // Contents
                    uri,
                    null,
                    language,
                    actualText,
                    altDescription));
        }

        return new float[] { x + 6*r1 + font.stringWidth(fontSize, label), y + font.getBodyHeight(fontSize) };
    }
}   // End of RadioButton.java
