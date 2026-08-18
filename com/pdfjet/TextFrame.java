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
    private Font f1 = null;
    private List<String> list;
    private float x;
    private float y;
    private float w;
    private float h;
    private float leading;
    private boolean border;
    private List<List<String>> paragraphs;
    // private final List<float[]> beginParagraphPoints;

    public TextFrame(Font f1, List<String> list) {
        this.f1 = f1;
        this.list = list;
        this.leading = f1.getAscent() + f1.getDescent();
        Collections.reverse(this.list);
        paragraphs = new ArrayList<List<String>>();
        for (String text : this.list) {
            List<String> tokens = new ArrayList<>(Arrays.asList(text.split("\\s+")));
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

//     public List<float[]> getBeginParagraphPoints() {
//         return this.beginParagraphPoints;
//     }

    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    public void setBorder(boolean border) {
        this.border = border;
    }

    public void setDrawBorder(boolean border) {
        this.border = border;
    }

    public float[] drawOn(Page page) throws Exception {
        float yText = y + f1.getAscent();
        while (paragraphs.size() > 0) {
            List<String> tokens = paragraphs.remove(paragraphs.size() - 1);
            StringBuilder sb = new StringBuilder();
            String token = null;
            while (tokens.size() > 0) {
                if (yText + f1.getDescent() < (y + h)) {
                    token = tokens.remove(tokens.size() - 1);
                    if (f1.stringWidth(sb.toString() + token) < this.w) {
                        sb.append(token);
                        sb.append(Single.space);
                    } else {
                        TextLine textLine = new TextLine(f1, sb.toString().trim());
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
                if (yText + f1.getDescent() < (y + h)) {
                    TextLine textLine = new TextLine(f1, sb.toString().trim());
                    textLine.setLocation(x, yText);
                    textLine.drawOn(page);
                    sb.setLength(0);
                    tokens.add(token);
                    yText += leading;
                } else {
                    paragraphs.add(tokens);
                    drawBorder(page);
                    return new float[] {this.x + this.w, this.y + this.h};
                }
            }
            yText += leading;
        }

        drawBorder(page);
        return new float[] {this.x + this.w, this.y + this.h};
    }

    private void drawBorder(Page page) throws Exception {
        if (page != null && border) {
            Rect rect = new Rect(x, y, w, h);
            rect.setBorderColor(Color.blue);
            rect.drawOn(page);
        }
    }

    public boolean isNotEmpty() {
        return paragraphs.size() > 0;
    }
}   // End of TextFrame.java
