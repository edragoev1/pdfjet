/**
 *  SVGImage.java
 *
Copyright 2023 Innovatics Inc.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
package com.pdfjet;

import java.io.*;
import java.util.ArrayList;
import java.util.List;


/**
 * Used to embed SVG images in the PDF document.
 */
public class SVGImage {
    float x = 0f;
    float y = 0f;
    float w = 0f;       // SVG width
    float h = 0f;       // SVG height
    List<PathOp> pdfPathOps = null;

    private int color = Color.black;
    private int penColor = Color.black;
    private float penWidth = 2.0f;
    private boolean fillPath = true;
    private boolean strokePath = false;

    protected String uri = null;
    protected String key = null;
    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param stream the input stream.
     * @throws Exception  if exception occurred.
     */
    public SVGImage(InputStream stream) throws Exception {
        List<String> paths = new ArrayList<String>();
        StringBuilder buf = new StringBuilder();
        boolean token = false;
        String param = null;
        int ch;
        while ((ch = stream.read()) != -1) {
            if (!token && buf.toString().endsWith(" width=")) {
                token = true;
                param = "width";
                buf.setLength(0);
            } else if (!token && buf.toString().endsWith(" height=")) {
                token = true;
                param = "height";
                buf.setLength(0);
            } else if (!token && buf.toString().endsWith("<path d=")) {
                token = true;
                param = "path";
                buf.setLength(0);
            } else if (!token && buf.toString().endsWith(" fill=")) {
                token = true;
                param = "fill";
                buf.setLength(0);
            } else if (!token && buf.toString().endsWith(" stroke=")) {
                token = true;
                param = "stroke";
                buf.setLength(0);
            } else if (!token && buf.toString().endsWith(" stroke-width=")) {
                token = true;
                param = "stroke-width";
                buf.setLength(0);
            } else if (token && ch == '\"') {
                token = false;
                if (param.equals("width")) {
                    w = Float.valueOf(buf.toString());
                } else if (param.equals("height")) {
                    h = Float.valueOf(buf.toString());
                } else if (param.equals("path")) {
                    paths.add(buf.toString());
                } else if (param.equals("fill")) {
                    if (buf.toString().equals("none")) {
                        fillPath = false;
                    } else {
                        color = mapColorNameToValue(buf.toString());
                    }
                } else if (param.equals("stroke")) {
                    strokePath = true;
                    penColor = mapColorNameToValue(buf.toString());
                } else if (param.equals("stroke-width")) {
                    penWidth = Float.valueOf(buf.toString());
                }
                buf.setLength(0);
            } else {
                buf.append((char) ch);
            }
        }
        stream.close();
        List<PathOp> svgPathOps = SVG.getSVGPathOps(paths);
        pdfPathOps = SVG.getPDFPathOps(svgPathOps);
    }

    private int mapColorNameToValue(String colorName) {
        int color = Color.black;
        try {
            color = (int) Color.class.getDeclaredField(colorName).get(null);
        } catch (Exception e) {
        }
        return color;
    }

    public List<PathOp> getPDFPathOps() {
        return pdfPathOps;
    }

    /**
     *  Sets the location of this SVG on the page.
     *
     *  @param x the x coordinate of the top left corner of this box when drawn on the page.
     *  @param y the y coordinate of the top left corner of this box when drawn on the page.
     *  @return this SVG object.
     */
    public SVGImage setLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /**
     *  Sets the fill path flag to true or false.
     *
     *  @param fillPath if true fills that SVG path, strokes otherwise.
     */
    public void setFillPath(boolean fillPath) {
        this.fillPath = fillPath;
    }

    /**
     *  Sets the size of this box.
     *
     *  @param w the width of this box.
     *  @param h the height of this box.
     */
    public void setSize(float w, float h) {
        this.w = w;
        this.h = h;
    }

    public void setPenWidth(float w) {
        this.w = w;
    }

    public void setHeight(float h) {
        this.h = h;
    }

    public float getPenWidth() {
        return this.w;
    }

    public float getHeight() {
        return this.h;
    }

    private void drawPath(Page page, boolean fill, boolean stroke) {
        for (int i = 0; i < pdfPathOps.size(); i++) {
            PathOp op = pdfPathOps.get(i);
            if (op.cmd == 'M') {
                page.moveTo(op.x + x, op.y + y);
            } else if (op.cmd == 'L') {
                page.lineTo(op.x + x, op.y + y);
            } else if (op.cmd == 'C') {
                page.curveTo(
                    op.x1 + x, op.y1 + y,
                    op.x2 + x, op.y2 + y,
                    op.x + x, op.y + y);
            } else if (op.cmd == 'Z') {
                if (stroke) {
                    page.closePath();
                }
            }
        }
        if (fill) {
            page.fillPath();
        }
    }

    public float[] drawOn(Page page) {
        page.addBMC(StructElem.P, language, actualText, altDescription);
        page.setBrushColor(color);
        page.setPenColor(penColor);
        page.setPenWidth(penWidth);
        if (fillPath) {
            drawPath(page, true, false);
        }
        if (strokePath) {
            drawPath(page, false, true);
        }
        page.addEMC();
        if (uri != null || key != null) {
            page.addAnnotation(new Annotation(
                    uri,
                    key,    // The destination name
                    x,
                    y,
                    x + w,
                    y + h,
                    language,
                    actualText,
                    altDescription));
        }
        return new float[] {x + w, y + h};
    }
}   // End of SVGImage.java
