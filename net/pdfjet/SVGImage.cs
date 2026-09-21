/*
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

namespace PDFjet.NET {
/// <summary>
/// Used to embed SVG images in the PDF document.
/// </summary>
public class SVGImage : IDrawable {
    float x = 0f;
    float y = 0f;
    float w = 0f;       // SVG width
    float h = 0f;       // SVG height
    String viewBox = null;
    int fill = Color.transparent;
    int stroke = Color.transparent;
    bool fillNone = false;      // fill="none" on the svg element
    bool strokeNone = false;    // stroke="none" on the svg element
    float strokeWidth = 0f;

    List<SVGPath> paths = null;
    /// <summary>The URI opened when the image is clicked.</summary>
    protected String uri = null;
    /// <summary>The destination key used by the GoTo action.</summary>
    protected String key = null;
    private String language = null;
    private String actualText = null;
    private String altDescription = null;

    /// <summary>
    /// Used to embed SVG images in the PDF document.
    /// </summary>
    /// <param name="svgPath">the path to the SVG file.</param>
    public SVGImage(String svgPath) {
        using (FileStream stream = new FileStream(svgPath, FileMode.Open, FileAccess.Read)) {
            Read(stream);
        }
    }

    /// <summary>
    /// Used to embed SVG images in the PDF document.
    /// </summary>
    /// <param name="stream">the input stream.</param>
    public SVGImage(Stream stream) {
        Read(stream);
    }

    private void Read(Stream stream) {
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

    // The units of a length of an SVG file, in points. A number without a
    // unit is in the user unit of the file, which PDFjet draws as a point,
    // and so is a number in px.
    private static readonly string[] UNITS = {"px", "pt", "pc", "in", "mm", "cm"};
    private static readonly float[] POINTS = {1f, 1f, 12f, 72f, 72f/25.4f, 72f/2.54f};

    // Returns the width or the height of the svg element in points, and 0 for
    // a length that PDFjet cannot read, a percentage among them, which leaves
    // the size to the viewBox.
    private static float ParseLength(String value) {
        string text = value.Trim();
        float scale = 1f;
        for (int i = 0; i < UNITS.Length; i++) {
            if (text.EndsWith(UNITS[i])) {
                text = text.Substring(0, text.Length - UNITS[i].Length).Trim();
                scale = POINTS[i];
                break;
            }
        }
        float number;
        if (!float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) {
            return 0f;
        }
        return number*scale;
    }

    private void ReadSVGAttributes(XmlReader reader) {
        while (reader.MoveToNextAttribute()) {
            String name = reader.LocalName;
            String value = reader.Value;
            if (name.Equals("width")) {
                this.w = ParseLength(value);
            } else if (name.Equals("height")) {
                this.h = ParseLength(value);
            } else if (name.Equals("viewBox")) {
                this.viewBox = value;
            } else if (name.Equals("fill")) {
                this.fill = getColor(value);
                this.fillNone = IsNone(value);
            } else if (name.Equals("stroke")) {
                this.stroke = getColor(value);
                this.strokeNone = IsNone(value);
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
                path.fillNone = IsNone(value);
            } else if (name.Equals("stroke")) {
                path.stroke = getColor(value);
                path.strokeNone = IsNone(value);
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
            if (list.Length != 4) {
                throw new Exception("Invalid SVG viewBox \"" + viewBox + "\": four numbers are needed.");
            }
            for (int i = 0; i < 4; i++) {
                if (!float.TryParse(list[i], NumberStyles.Float, CultureInfo.InvariantCulture, out box[i])) {
                    throw new Exception("Invalid SVG viewBox \"" + viewBox + "\": four numbers are needed.");
                }
            }
            if (box[2] == 0f || box[3] == 0f) {
                throw new Exception(
                        "Invalid SVG viewBox \"" + viewBox + "\": its width and height cannot be zero.");
            }
            // A size the file does not give, or one in a unit PDFjet cannot
            // read, leaves the drawing the size of its viewBox, where scaling
            // it by a width of zero would draw every path at the origin.
            if (w == 0f) {
                w = box[2];
            }
            if (h == 0f) {
                h = box[3];
            }
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
                return Int32.Parse(colorName.Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            } else if (colorName.Length == 4) {
                String str = new String(new char[] {
                        colorName[1], colorName[1],
                        colorName[2], colorName[2],
                        colorName[3], colorName[3]
                });
                return Int32.Parse(str, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
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

    /// <summary>Sets the location of the top left corner of this image on the page.</summary>
    public SVGImage SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Scales this SVG image by the specified factor.</summary>
    public SVGImage ScaleBy(float factor) {
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
        this.w *= factor;
        this.h *= factor;
        return this;
    }

    /// <summary>Sets the URI for the "click box" action.</summary>
    public SVGImage SetURIAction(String uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>Sets the destination key for the action.</summary>
    public SVGImage SetGoToAction(String key) {
        this.key = key;
        return this;
    }

    /// <summary>Sets the alternate description of this image.</summary>
    public SVGImage SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>Sets the actual text of this image.</summary>
    public SVGImage SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /// <summary>Sets the language of this image, for example "en-US".</summary>
    public SVGImage SetLanguage(String language) {
        this.language = language;
        return this;
    }

    /// <summary>Returns the width of this SVG image.</summary>
    public float GetWidth() {
        return this.w;
    }

    /// <summary>Returns the height of this SVG image.</summary>
    public float GetHeight() {
        return this.h;
    }

    // Returns true for the value none, which turns the fill or the stroke off.
    private static bool IsNone(String value) {
        return value.Trim().Equals("none");
    }

    private void drawPath(SVGPath path, Page page) {
        if (path.operations == null || path.operations.Count == 0) {
            return;     // A path of no operations draws nothing, not even its colors.
        }
        // none on the path wins over the color of the svg element; a color
        // that is not set, or not understood, is taken from the svg element.
        bool noFill = path.fillNone || (path.fill == Color.transparent && this.fillNone);
        int fillColor = noFill ? Color.transparent : path.fill;
        if (!noFill && fillColor == Color.transparent) {
            fillColor = this.fill;
        }
        bool noStroke = path.strokeNone || (path.stroke == Color.transparent && this.strokeNone);
        int strokeColor = noStroke ? Color.transparent : path.stroke;
        if (!noStroke && strokeColor == Color.transparent) {
            strokeColor = this.stroke;
        }
        float strokeWidth = this.strokeWidth;
        if (path.strokeWidth > strokeWidth) {
            strokeWidth = path.strokeWidth;
        }

        // A path whose fill is not none, with no fill and no stroke color, is
        // filled black, as SVG fills a path black by default.
        if (!noFill && fillColor == Color.transparent &&
                strokeColor == Color.transparent) {
            fillColor = Color.black;
        }
        if (fillColor == Color.transparent && strokeColor == Color.transparent) {
            return;     // fill="none" and no stroke: nothing to draw
        }

        page.SetBrushColor(fillColor);
        page.SetPenColor(strokeColor);
        page.SetPenWidth(strokeWidth);

        if (fillColor != Color.transparent) {
            foreach (PathOp op in path.operations) {
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
            // Z closes and strokes a subpath; a path that is still open at the
            // end is stroked without closing it.
            bool open = false;
            foreach (PathOp op in path.operations) {
                if (op.cmd == 'M') {
                    page.MoveTo(op.x + x, op.y + y);
                    open = true;
                } else if (op.cmd == 'L') {
                    page.LineTo(op.x + x, op.y + y);
                    open = true;
                } else if (op.cmd == 'C') {
                    page.CurveTo(
                        op.x1 + x, op.y1 + y,
                        op.x2 + x, op.y2 + y,
                        op.x + x, op.y + y);
                    open = true;
                } else if (op.cmd == 'Z') {
                    page.ClosePath();
                    open = false;
                }
            }
            if (open) {
                page.StrokePath();
            }
        }
    }

    /// <summary>Draws this SVG image on the specified page.</summary>
    public float[] DrawOn(Page page) {
        if (page == null) {
            return new float[] {x + w, y + h};  // Measured, not drawn
        }
        page.AddBDC(StructElem.FIGURE, language, actualText, altDescription);
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
                    0f,     // Opacity
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