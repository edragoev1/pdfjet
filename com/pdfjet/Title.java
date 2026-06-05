/**
 * Title.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Please see Example_51 and Example_52
 */
public class Title implements Drawable {
    public TextLine prefix;
    public TextLine textLine;

    public Title(Font font, String title, float x, float y) {
        this.prefix = new TextLine(font);
        this.prefix.setLocation(x, y);
        this.textLine = new TextLine(font, title);
        this.textLine.setLocation(x, y);
    }

    public Title setPrefix(String text) {
        prefix.setText(text);
        return this;
    }

    public Title setOffset(float offset) {
        textLine.setLocation(textLine.x + offset, textLine.y);
        return this;
    }

    public void setPosition(float x, float y) {
        prefix.setLocation(x, y);
        textLine.setLocation(x, y);
    }

    public void setPosition(double x, double y) {
        setPosition(x, y);
    }

    public Title setLocation(float x, float y) {
        textLine.setLocation(x, y);
        return this;
    }

    public Title setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    public float[] drawOn(Page page) throws Exception {
        if (!prefix.equals("")) {
            prefix.drawOn(page);
        }
        return textLine.drawOn(page);
    }
}
