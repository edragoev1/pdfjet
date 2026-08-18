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
    private List<String> paragraphs;
    private Font f1 = null;
    private Font f2 = null;
    private float fontSize;
    private float x;
    private float y;
    private float w;
    private float h;
    private float leading;
    private float paragraphLeading;
    private boolean border;
//    private final List<float[]> beginParagraphPoints;

    public TextFrame(Font f1, List<String> paragraphs) {
        this.f1 = f1;
        this.paragraphs = paragraphs;
//         this.font = paragraphs.get(0).getFont();
//         this.fallbackFont = paragraphs.get(0).getFallbackFont();
//         this.fontSize = font.size;
//         this.leading = font.getBodyHeight(fontSize);
//        this.paragraphLeading = leading;
//        this.beginParagraphPoints = new ArrayList<float[]>();
//         if (f2 != null && (f2.getBodyHeight(fontSize) > this.leading)) {
//             this.leading = f2.getBodyHeight(fontSize);
//         }

        // this.leading = f1.getBodyHeight(fontSize);   // TODO!!!!
        this.leading = f1.getAscent() + f1.getDescent();
        Collections.reverse(this.paragraphs);
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

    public void setParagraphs(List<String> paragraphs) {
        this.paragraphs = paragraphs;
    }

    public List<String> getParagraphs() {
        return this.paragraphs;
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

    public void SetFontSize(float fontSize) {
        this.fontSize = fontSize;
    }

// TODO: Create the complete structure before the drawOn method!!!!
// Do the check for reaching the bottom of the page immediately!!
    public float[] drawOn(Page page) throws Exception {
        float yText = y + f1.getAscent();
        while (paragraphs.size() > 0) {
            String text = paragraphs.remove(paragraphs.size() - 1);

            List<String> tokens = new ArrayList<>(Arrays.asList(text.split("\\s+")));
            Collections.reverse(tokens);
            List<String> temp = new ArrayList<>();

            StringBuilder sb = new StringBuilder();
            String token = null;
            while (tokens.size() > 0) {
                token = tokens.remove(tokens.size() - 1);
                if (f1.stringWidth(sb.toString() + token) < this.w) {
                    sb.append(token);
                    sb.append(Single.space);
                    temp.add(token);
                } else {
                    if (yText + f1.getDescent() < (y + h)) {
                        TextLine textLine = new TextLine(f1, sb.toString().trim());
                        textLine.setLocation(x, yText);
                        textLine.drawOn(page);
                        sb.setLength(0);
                        temp.clear();
                        temp.add(token);
                        yText += leading;
                    } else {
                        tokens.addAll(temp);
                        return new float[] {this.x + this.w, this.y + this.h};
                    }
                }
            }
            if (!sb.toString().trim().equals("")) {
                if (yText + f1.getDescent() < (y + h)) {
                    TextLine textLine = new TextLine(f1, sb.toString().trim());
                    textLine.setLocation(x, yText);
                    textLine.drawOn(page);
                    sb.setLength(0);
                    yText += leading;
                } else {
                    tokens.addAll(temp);
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
