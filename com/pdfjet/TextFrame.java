/**
 * TextFrame.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.*;

/**
 * Please see Example_47
 */
public class TextFrame implements Drawable {
    private Font f1;
    private float x;
    private float y;
    private float w;
    private float h;
    private float leading;
    private boolean border;
    private int borderColor = Color.blue;
    private List<List<String>> paragraphs;

    public TextFrame(Font f1, List<String> inputList) {
        this.f1 = f1;
        this.leading = f1.getAscent() + f1.getDescent();
        List<String> list = new ArrayList<>(inputList);
        Collections.reverse(list);
        paragraphs = new ArrayList<List<String>>();
        for (String text : list) {
            String[] split = text.trim().split("\\s+");
            List<String> tokens = new ArrayList<>();
            for (String token : split) {
                if (!token.isEmpty()) { // Filter empty tokens
                    tokens.add(token);
                }
            }
            Collections.reverse(tokens);
            paragraphs.add(tokens);
        }
    }

    public TextFrame setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public TextFrame setLocation(double x, double y) {
        return setLocation((float) x, (float) y);
    }

    public TextFrame setWidth(float w) {
        this.w = w;
        return this;
    }

    public TextFrame setWidth(double w) {
        return setWidth((float) w);
    }

    public TextFrame setHeight(float h) {
        this.h = h;
        return this;
    }

    public TextFrame setHeight(double h) {
        return setHeight((float) h);
    }

    public float getHeight() {
        return this.h;
    }

    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    public void setBorder(boolean border) {
        this.border = border;
    }

    public void setBorderColor(int borderColor) {
        this.borderColor = borderColor;
    }

    public boolean hasMoreText() {
        return paragraphs.size() > 0;
    }

    private void drawBorder(Page page) throws Exception {
        if (border) {
            Rect rect = new Rect(x, y, w, h);
            rect.setBorderColor(borderColor);
            rect.drawOn(page);
        }
    }

    public float[] drawOn(Page page) throws Exception {
        if (page == null) {
            throw new NullPointerException("Page cannot be null");
        }

        float yText = y + f1.getAscent();
        while (paragraphs.size() > 0) {
            List<String> tokens = paragraphs.remove(paragraphs.size() - 1);
            TextLine textLine = null;
            StringBuilder sb = new StringBuilder();
            String token = null;
            while (tokens.size() > 0) {
                if (yText + f1.getDescent() < (y + h)) {
                    token = tokens.remove(tokens.size() - 1);
                    if (f1.stringWidth(sb.toString() + token) < this.w) {
                        sb.append(token);
                        sb.append(Single.space);
                    } else {
                        textLine = new TextLine(f1, sb.toString().trim());
                        textLine.setLocation(x, yText);
                        textLine.drawOn(page);
                        sb.setLength(0);
                        tokens.add(token);
                        yText += leading;
                    }
                } else {
                    paragraphs.add(tokens);
                    drawBorder(page);
                    return new float[] {this.x + this.w, this.y + this.h};
                }
            }
            if (!sb.toString().trim().equals("")) {
                textLine = new TextLine(f1, sb.toString().trim());
                textLine.setLocation(x, yText);
                textLine.drawOn(page);
                yText += leading;
            }
            yText += leading;
        }

        drawBorder(page);
        return new float[] {this.x + this.w, this.y + this.h};
    }
}   // End of TextFrame.java
