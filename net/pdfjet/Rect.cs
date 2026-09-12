/*
 * Rect.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
/// <summary>A rectangle that can be drawn on a page.</summary>
public class Rect  : IDrawable {
    internal float x;
    internal float y;
    private float w;
    private float h;
    private float r;

    private float[] fillColor;
    private float[] borderColor;
    private float borderWidth;
    private string borderPattern = "[] 0";

    private string uri;
    private string key;
    private string language = null;
    private string actualText = null;
    private string altDescription = null;

    /// <summary>
    /// The default constructor.
    /// </summary>
    public Rect() {
    }

    /// <summary>Creates a rectangle with its top left corner at x, y and the specified width and height.</summary>
    public Rect(float x, float y, float w, float h) {
        this.x = x;
        this.y = y;
        this.w = w;
        this.h = h;
    }

    /// <summary>Creates a rectangle with its top left corner at x, y and the specified width and height.</summary>
    public Rect(double x, double y, double w, double h) {
        this.x = (float) x;
        this.y = (float) y;
        this.w = (float) w;
        this.h = (float) h;
    }

    /// <summary>Sets the location of the top left corner of this rectangle.</summary>
    public Rect SetLocation(float x, float y) {
        this.x = x;
        this.y = y;
        return this;
    }

    /// <summary>Sets the location of the top left corner of this rectangle.</summary>
    public Rect SetLocation(double x, double y) {
        return SetLocation((float) x, (float) y);
    }

    IDrawable IDrawable.SetLocation(float x, float y) {
        return SetLocation(x, y);
    }

    /// <summary>Sets the size of this rectangle.</summary>
    public Rect SetSize(float w, float h) {
        this.w = w;
        this.h = h;
        return this;
    }

    /// <summary>Sets the fill color as a 0xRRGGBB value.</summary>
    public Rect SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetFillColor(r, g, b);
        return this;
    }

    /// <summary>Sets the fill color from red, green and blue values between 0.0 and 1.0.</summary>
    public Rect SetFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the fill color from an array of red, green and blue values.</summary>
    public Rect SetFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    /// <summary>Sets the border width.</summary>
    public Rect SetBorderWidth(float width) {
        this.borderWidth = width;
        return this;
    }

    /// <summary>Sets the border color as a 0xRRGGBB value. Color.transparent removes the border.</summary>
    public Rect SetBorderColor(int color) {
        if (color == Color.transparent) {
            this.borderColor = null;
            return this;
        }
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        SetBorderColor(r, g, b);
        return this;
    }

    /// <summary>Sets the border color from red, green and blue values between 0.0 and 1.0.</summary>
    public Rect SetBorderColor(float r, float g, float b) {
        this.borderColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the border color from an array of red, green and blue values, or null for no border.</summary>
    public Rect SetBorderColor(float[] rgbColor) {
        this.borderColor = rgbColor;
        return this;
    }

    /// <summary>Sets the corner radius.</summary>
    public Rect SetCornerRadius(float r) {
        this.r = r;
        return this;
    }

    /// <summary>Sets the URI opened when this rectangle is clicked.</summary>
    public Rect SetURIAction(string uri) {
        this.uri = uri;
        return this;
    }

    /// <summary>Sets the destination key used when this rectangle is clicked.</summary>
    public Rect SetGoToAction(string key) {
        this.key = key;
        return this;
    }

    /// <summary>Sets the language of this rectangle, used for accessibility. When it is not set, the language of the document is used.</summary>
    public Rect SetLanguage(String language) {
        this.language = language;
        return this;
    }

    /// <summary>Sets the actual text of this rectangle, used for accessibility.</summary>
    public Rect SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /// <summary>Sets the alternate description of this rectangle, used for accessibility.</summary>
    public Rect SetAltDescription(string altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>Sets the dash pattern of the border.</summary>
    public Rect SetBorderPattern(String borderPattern) {
        this.borderPattern = borderPattern;
        return this;
    }

    /// <summary>Multiplies the x and y coordinates of this rectangle by the specified factor.</summary>
    public void ScaleBy(float factor) {
        this.x *= factor;
        this.y *= factor;
    }

    /// <summary>Draws this rectangle on the specified page.</summary>
    public float[] DrawOn(Page page) {
        if (page == null) {
            return new float[] {x + w, y + h};
        }

        // A rectangle carries no text, so it is decorative content.
        page.AddArtifactBMC();
        page.SaveGraphicsState();

        const float k = 0.55228f;
        if (this.r == 0.0f) {
            if (fillColor != null) {
                page.MoveTo(this.x, this.y);
                page.LineTo(this.x + this.w, this.y);
                page.LineTo(this.x + this.w, this.y + this.h);
                page.LineTo(this.x, this.y + this.h);
                page.LineTo(this.x, this.y);
                page.SetBrushColor(this.fillColor);
                page.FillPath();
            }
            if (borderColor != null) {
                page.MoveTo(this.x, this.y);
                page.LineTo(this.x + this.w, this.y);
                page.LineTo(this.x + this.w, this.y + this.h);
                page.LineTo(this.x, this.y + this.h);
                page.SetPenColor(this.borderColor);
                page.SetPenWidth(this.borderWidth);
                page.SetStrokeDashPattern(this.borderPattern);
                page.ClosePath();
            }
        } else {
            // The pen and brush must be set before the path is painted,
            // otherwise the rounded rectangle is drawn with whatever state
            // the page happened to be left in.
            if (borderColor != null && borderPattern != null) {
                page.SetStrokeDashPattern(borderPattern);
            }
            if (fillColor != null) {
                page.SetBrushColor(fillColor);
            }
            if (borderColor != null) {
                page.SetPenWidth(borderWidth);
                page.SetPenColor(borderColor);
            }

            List<Point> points = new List<Point> {
                new Point((this.x + this.r), this.y),
                new Point((this.x + this.w) - this.r, this.y),
                new Point((this.x + this.w - this.r) + this.r * k, this.y, Point.ControlPointC),
                new Point((this.x + this.w), (this.y + this.r) - this.r * k, Point.ControlPointC),
                new Point((this.x + this.w), (this.y + this.r)),
                new Point((this.x + this.w), (this.y + this.h) - this.r),
                new Point((this.x + this.w), ((this.y + this.h) - this.r) + this.r * k, Point.ControlPointC),
                new Point(((this.x + this.w) - this.r) + this.r * k, (this.y + this.h), Point.ControlPointC),
                new Point(((this.x + this.w) - this.r), (this.y + this.h)),
                new Point((this.x + this.r), (this.y + this.h)),
                new Point(((this.x + this.r) - this.r * k), (this.y + this.h), Point.ControlPointC),
                new Point(this.x, ((this.y + this.h) - this.r) + this.r * k, Point.ControlPointC),
                new Point(this.x, (this.y + this.h) - this.r),
                new Point(this.x, (this.y + this.r)),
                new Point(this.x, (this.y + this.r) - this.r * k, Point.ControlPointC),
                new Point((this.x + this.r) - this.r * k, this.y, Point.ControlPointC),
                new Point((this.x + this.r), this.y)
            };
            if (fillColor != null && borderColor == null) {
                page.DrawPath(points, PathOperator.Fill);
            } else if (fillColor == null && borderColor != null) {
                page.DrawPath(points, PathOperator.Stroke);
            } else if (fillColor != null && borderColor != null) {
                page.DrawPath(points, PathOperator.FillAndStroke);
            }
        }

        page.RestoreGraphicsState();
        page.AddEMC();

        if (this.uri != null || this.key != null) {
            page.AddAnnotation(new Annotation(
                Annotation.Link,
                this.x,
                this.y,
                this.x + this.w,
                this.y + this.h,
                null,       // Vertices
                null,       // Fill Color
                0f,         // Transparency
                null,       // Title
                null,       // Contents
                this.uri,
                this.key,
                this.language,
                this.actualText,
                this.altDescription
            ));
        }

        return new float[] { this.x + this.w, this.y + this.h };
    }
}
}
