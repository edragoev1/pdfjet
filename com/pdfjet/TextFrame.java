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
    private List<TextLine> lines;
    private final Font font;
    private final Font fallbackFont;
    private float fontSize;
    private float x;
    private float y;
    private float w;
    private float h;
    private float leading;
    private float paragraphLeading;
    private boolean border;
    private final List<float[]> beginParagraphPoints;

    public TextFrame(List<TextLine> lines) {
        this.lines = lines;
        this.font = lines.get(0).getFont();
        this.fallbackFont = lines.get(0).getFallbackFont();
        this.fontSize = font.size;
        this.leading = font.getBodyHeight(fontSize);
        this.paragraphLeading = leading;
        this.beginParagraphPoints = new ArrayList<float[]>();
        if (fallbackFont != null && (fallbackFont.getBodyHeight(fontSize) > this.leading)) {
            this.leading = fallbackFont.getBodyHeight(fontSize);
        }
        Collections.reverse(this.lines);
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

    public TextFrame setLeading(float leading) {
        this.leading = leading;
        return this;
    }

    public TextFrame setLeading(double leading) {
        return setLeading((float) leading);
    }

    public TextFrame setParagraphLeading(float paragraphLeading) {
        this.paragraphLeading = paragraphLeading;
        return this;
    }

    public TextFrame setParagraphLeading(double paragraphLeading) {
        return setParagraphLeading((float) paragraphLeading);
    }

    public void setParagraphs(List<TextLine> lines) {
        this.lines = lines;
    }

    public List<TextLine> getParagraphs() {
        return this.lines;
    }

    public List<float[]> getBeginParagraphPoints() {
        return this.beginParagraphPoints;
    }

    public void setPosition(float x, float y) {
        setLocation(x, y);
    }

    public void setBorder(boolean border) {
        this.border = border;
    }

    public void setDrawBorder(boolean border) {
        this.border = border;
    }

    public void SetFontSize(float fontSize) {
        this.fontSize = fontSize;
    }

    public float[] drawOn(Page page) throws Exception {
        float yText = y + font.getAscent(fontSize);
        while (lines.size() > 0) {
            TextLine textLine = lines.remove(lines.size() - 1);
            textLine.setLocation(x, yText);
            // beginParagraphPoints.add(new float[] {xText, yText});
            while (true) {
                textLine = drawLineOnPage(textLine, page);
                yText = textLine.advance(leading);
                if (yText + font.getDescent(fontSize) >= (y + h)) {
                    lines.add(textLine);
                    drawBorder(page);
                    return new float[] {this.x + this.w, this.y + this.h};
                }
                if (textLine.text.trim().equals("")) {
                    break;
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

    private TextLine drawLineOnPage(TextLine textLine, Page page) throws Exception {
        StringBuilder sb1 = new StringBuilder();
        StringBuilder sb2 = new StringBuilder();
        String[] tokens = textLine.getText().split("\\s+");
        boolean testForFit = true;
        for (String token : tokens) {
            if (testForFit && textLine.getStringWidth(sb1.toString() + token) < this.w) {
                sb1.append(token).append(Single.space);
            } else {
                testForFit = false;
                sb2.append(token).append(Single.space);
            }
        }
        textLine.setText(sb1.toString().trim());
        if (page != null) {
            textLine.drawOn(page);
        }
        textLine.setText(sb2.toString().trim());
        return textLine;
    }

    public boolean isNotEmpty() {
        return lines.size() > 0;
    }
}   // End of TextFrame.java
