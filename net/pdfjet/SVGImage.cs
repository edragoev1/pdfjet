/**
 * SVGImage.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

/**
 * Used to embed SVG images in the PDF document.
 */
namespace PDFjet.NET {
public class SVGImage {
    float x = 0f;
    float y = 0f;
    float w = 0f;       // SVG width
    float h = 0f;       // SVG height
    String viewBox = null;
    int fill = Color.transparent;
    int stroke = Color.transparent;
    float strokeWidth = 0f;

    List<SVGPath> paths = null;
    protected String uri = null;
    protected String key = null;
    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param svgPath the path to the SVG file.
     */
    public SVGImage(String svgPath) : this(
        new FileStream(svgPath, FileMode.Open, FileAccess.Read)) {
    }

    /**
     * Used to embed SVG images in the PDF document.
     *
     * @param stream the input stream.
     */
    public SVGImage(Stream stream) {
        paths = new List<SVGPath>();

        XmlReaderSettings settings = new XmlReaderSettings();
        // Disable DTD and external entity processing to prevent XXE attacks.
        settings.DtdProcessing = DtdProcessing.Ignore;
        settings.XmlResolver = null;

        XmlReader reader = XmlReader.Create(stream, settings);
        try {
            while (reader.Read()) {
                if (!reader.IsStartElement()) {
                    continue;
                }
                String localName = reader.LocalName;
                if (localName.Equals("svg")) {
                    ReadSVGAttributes(reader);
                } else if (localName.Equals("path")) {
                    ReadPathAttributes(reader);
                }
            }
        } finally {
            // Close only the reader we created. The caller remains
            // responsible for the underlying stream.
            reader.Close();
        }

        ProcessPaths(paths);
    }

    private void ReadSVGAttributes(XmlReader reader) {
        while (reader.MoveToNextAttribute()) {
            String name = reader.LocalName;
            String value = reader.Value;
            if (name.Equals("width")) {
                try {
                    this.w = float.Parse(value, CultureInfo.InvariantCulture);
                } catch (Exception) {
                    this.w = 0f;
                }
            } else if (name.Equals("height")) {
                try {
                    this.h = float.Parse(value, CultureInfo.InvariantCulture);
                } catch (Exception) {
                    this.h = 0f;
                }
            } else if (name.Equals("viewBox")) {
                this.viewBox = value;
            } else if (name.Equals("fill")) {
                this.fill = getColor(value);
            } else if (name.Equals("stroke")) {
                this.stroke = getColor(value);
            } else if (name.Equals("stroke-width")) {
                try {
                    this.strokeWidth = float.Parse(value, CultureInfo.InvariantCulture);
                } catch (Exception) {
                    this.strokeWidth = 0f;
                }
            }
        }
        reader.MoveToElement();
    }

    private void ReadPathAttributes(XmlReader reader) {
        SVGPath path = new SVGPath();
        while (reader.MoveToNextAttribute()) {
            String name = reader.LocalName;
            String value = reader.Value;
            if (name.Equals("d")) {
                path.data = value;
            } else if (name.Equals("fill")) {
                path.fill = getColor(value);
            } else if (name.Equals("stroke")) {
                path.stroke = getColor(value);
            } else if (name.Equals("stroke-width")) {
                try {
                    path.strokeWidth = float.Parse(value, CultureInfo.InvariantCulture);
                } catch (Exception) {
                    path.strokeWidth = 0f;
                }
            }
        }
        reader.MoveToElement();
        paths.Add(path);
    }

    private void ProcessPaths(List<SVGPath> paths) {
        float[] box = new float[4];
        if (viewBox != null) {
            String[] list = viewBox.Trim().Split(default(char[]),
                    StringSplitOptions.RemoveEmptyEntries);
            box[0] = float.Parse(list[0], CultureInfo.InvariantCulture);
            box[1] = float.Parse(list[1], CultureInfo.InvariantCulture);
            box[2] = float.Parse(list[2], CultureInfo.InvariantCulture);
            box[3] = float.Parse(list[3], CultureInfo.InvariantCulture);
        }
        foreach (SVGPath path in paths) {
            path.operations = SVG.GetOperations(path.data);
            path.operations = SVG.ToPDF(path.operations);
            if (viewBox != null) {
                foreach (PathOp op in path.operations) {
                    op.x = (op.x - box[0]) * w / box[2];
                    op.y = (op.y - box[1]) * h / box[3];
                    op.x1 = (op.x1 - box[0]) * w / box[2];
                    op.y1 = (op.y1 - box[1]) * h / box[3];
                    op.x2 = (op.x2 - box[0]) * w / box[2];
                    op.y2 = (op.y2 - box[1]) * h / box[3];
                }
            }
        }
    }

    private int getColor(String colorName) {
        if (colorName.StartsWith("#")) {
            if (colorName.Length == 7) {
                return Int32.Parse(colorName.Substring(1), NumberStyles.HexNumber);
            } else if (colorName.Length == 4) {
                String str = new String(new char[] {
                        colorName[1], colorName[1],
                        colorName[2], colorName[2],
                        colorName[3], colorName[3]
                });
                return Int32.Parse(str, NumberStyles.HexNumber);
            } else {
                return Color.transparent;
            }
        }
        int color = Color.transparent;
        try {
            color = (int) typeof(Color).GetField(colorName).GetValue(null);
        } catch (Exception) {
            return color;
        }
        return color;
    }

    // SetLocation, ScaleBy, getWidth, getHeight, drawPath, DrawOn
    // — unchanged from the original file.

    public SVGImage SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    public void ScaleBy(float factor) {
        foreach (SVGPath path in paths) {
            foreach (PathOp op in path.operations) {
                op.x1 *= factor;
                op.y1 *= factor;
                op.x2 *= factor;
                op.y2 *= factor;
                op.x *= factor;
                op.y *= factor;
            }
        }
    }

    public float getWidth() {
        return this.w;
    }

    public float getHeight() {
        return this.h;
    }

    private void drawPath(SVGPath path, Page page) {
        int fillColor = path.fill;
        if (fillColor == Color.transparent) {
            fillColor = this.fill;
        }
        int strokeColor = path.stroke;
        if (strokeColor == Color.transparent) {
            strokeColor = this.stroke;
        }
        float strokeWidth = this.strokeWidth;
        if (path.strokeWidth > strokeWidth) {
            strokeWidth = path.strokeWidth;
        }

        if (fillColor == Color.transparent &&
                strokeColor == Color.transparent) {
            fillColor = Color.black;
        }

        page.SetBrushColor(fillColor);
        page.SetPenColor(strokeColor);
        page.SetPenWidth(strokeWidth);

        if (fillColor != Color.transparent) {
            for (int i = 0; i < path.operations.Count; i++) {
                PathOp op = path.operations[i];
                if (op.cmd == 'M') {
                    page.MoveTo(op.x + x, op.y + y);
                } else if (op.cmd == 'L') {
                    page.LineTo(op.x + x, op.y + y);
                } else if (op.cmd == 'C') {
                    page.CurveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y);
                } else if (op.cmd == 'Z') {
                }
            }
            page.FillPath();
        }

        if (strokeColor != Color.transparent) {
            for (int i = 0; i < path.operations.Count; i++) {
                PathOp op = path.operations[i];
                if (op.cmd == 'M') {
                    page.MoveTo(op.x + x, op.y + y);
                } else if (op.cmd == 'L') {
                    page.LineTo(op.x + x, op.y + y);
                } else if (op.cmd == 'C') {
                    page.CurveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y);
                } else if (op.cmd == 'Z') {
                    page.ClosePath();
                }
            }
        }
    }

    public float[] DrawOn(Page page) {
        page.AddBMC(StructElem.P, language, actualText, altDescription);
        foreach (SVGPath path in paths) {
            drawPath(path, page);
        }
        page.AddEMC();
        if (uri != null || key != null) {
            page.AddAnnotation(new Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + w,
                    y + h,
                    null,   // Vertices
                    null,   // Fill Color
                    0f,     // Transparency
                    null,   // Title
                    null,   // Contents
                    uri,
                    key,    // The destination name
                    language,
                    actualText,
                    altDescription));
        }
        return new float[] {x + w, y + h};
    }
}   // End of SVGImage.cs
}   // End of PDFjet.NET namespace