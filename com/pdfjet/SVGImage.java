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
import java.util.HashMap;
import java.util.List;
import java.util.Map;


/**
 * Used to embed SVG images in the PDF document.
 */
public class SVGImage {
    float x = 0f;
    float y = 0f;
    float w = 0f;       // SVG width
    float h = 0f;       // SVG height

    List<SVGPath> paths = null;
    protected String uri = null;
    protected String key = null;
    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    private ColorMap colorMap = null;

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param stream the input stream.
     * @throws Exception  if exception occurred.
     */
    public SVGImage(InputStream stream) throws Exception {
        colorMap = new ColorMap();
        paths = new ArrayList<SVGPath>();
        SVGPath path = null;
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
            } else if (!token && buf.toString().endsWith(" d=")) {
                token = true;
                if (path != null) {
                    paths.add(path);
                }
                path = new SVGPath();
                param = "data";
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
                } else if (param.equals("data")) {
                    path.data = buf.toString();
                } else if (param.equals("fill")) {
                    if (buf.toString().equals("none")) {
                        path.fill = Color.transparent;
                    } else {
                        path.fill = colorMap.getColor(buf.toString());
                    }
                } else if (param.equals("stroke")) {
                    path.stroke = colorMap.getColor(buf.toString());
                } else if (param.equals("stroke-width")) {
                    try {
                        path.strokeWidth = Float.valueOf(buf.toString());
                    } catch (Exception e) {
                        path.strokeWidth = 2f;
                    }
                }
                buf.setLength(0);
            } else {
                buf.append((char) ch);
            }
        }
        if (path != null) {
            paths.add(path);
        }
        stream.close();

        for (int i = 0; i < paths.size(); i++) {
            path = paths.get(i);
            path.operations = SVG.getOperations(path.data);
            path.operations = SVG.toPDF(path.operations);
        }
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

    public void setScale(float factor) {
        // TODO:
    }

    public float getWidth() {
        return this.w;
    }

    public float getHeight() {
        return this.h;
    }

    private void drawPath(SVGPath path, Page page) {
        page.setBrushColor(path.fill);
        page.setPenColor(path.stroke);
        page.setPenWidth(path.strokeWidth);

        if (path.fill != Color.transparent) {
            for (int i = 0; i < path.operations.size(); i++) {
                PathOp op = path.operations.get(i);
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
                }
            }
            page.fillPath();
        }

        if (path.stroke != Color.transparent) {
            for (int i = 0; i < path.operations.size(); i++) {
                PathOp op = path.operations.get(i);
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
                    page.closePath();
                }
            }
        }
    }

    public float[] drawOn(Page page) {
        page.addBMC(StructElem.P, language, actualText, altDescription);
        for (int i = 0; i < paths.size(); i++) {
            SVGPath path = paths.get(i);
            drawPath(path, page);
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
