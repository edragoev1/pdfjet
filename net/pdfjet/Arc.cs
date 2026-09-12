/*
 * Arc.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to create arc objects.
/// </summary>
public class Arc : IDrawable {
    private float cx;
    private float cy;
    private float rx;
    private float ry;
    private float startAngle;
    private float sweepDegrees;
    private float rotateDegrees;

    private float[] fillColor;
    private float[] strokeColor = new float[] {0f, 0f, 0f};   // Black color
    private float strokeWidth = 0f;
    private String strokeDashPattern = "[] 0";

    private String language = null;
    private String actualText = Single.space;
    private String altDescription = Single.space;

    private Line line;

    /// <summary>
    /// The default constructor.
    /// </summary>
    public Arc() {
    }

    /// <summary>Sets the center of this arc.</summary>
    public Arc SetLocation(float cx, float cy) {
        SetCenterXY(cx, cy);
        return this;
    }

    IDrawable IDrawable.SetLocation(float cx, float cy) {
        return SetLocation(cx, cy);
    }

    /// <summary>Starts this arc at the end point of the specified line.</summary>
    public Arc SetStartPointToEndOf(Line line) {
        this.line = line;
        return this;
    }

    /// <summary>Sets the center of this arc.</summary>
    public Arc SetCenterXY(float cx, float cy) {
        this.cx = cx;
        this.cy = cy;
        return this;
    }

    /// <summary>Sets the horizontal radius of this arc.</summary>
    public Arc SetRadiusX(float rx) {
        this.rx = rx;
        return this;
    }

    /// <summary>Sets the vertical radius of this arc.</summary>
    public Arc SetRadiusY(float ry) {
        this.ry = ry;
        return this;
    }

    /// <summary>Sets both radii of this arc to the same value, making it circular.</summary>
    public Arc SetRadius(float r) {
        this.rx = r;
        this.ry = r;
        return this;
    }

    /// <summary>Sets the angle in degrees where this arc starts.</summary>
    public Arc SetStartAngle(float angle) {
        this.startAngle = angle;
        return this;
    }

    /// <summary>Sets how many degrees this arc sweeps clockwise from its start angle.</summary>
    public Arc SetSweepDegreesCW(float sweepDegrees) {
        this.sweepDegrees = sweepDegrees;
        return this;
    }

    /// <summary>Sets how many degrees this arc sweeps counterclockwise from its start angle.</summary>
    public Arc SetSweepDegreesCCW(float sweepDegrees) {
        this.sweepDegrees = -sweepDegrees;
        return this;
    }

    /// <summary>
    /// The line dash pattern controls the pattern of dashes and gaps used to stroke paths.
    /// It is specified by a dash array and a dash phase.
    /// The elements of the dash array are positive numbers that specify the lengths of
    /// alternating dashes and gaps.
    /// The dash phase specifies the distance into the dash pattern at which to start the dash.
    /// The elements of both the dash array and the dash phase are expressed in user space units.
    /// <code>
    /// Examples of line dash patterns:
    ///
    ///     "[Array] Phase"     Appearance          Description
    ///     _______________     _________________   ____________________________________
    ///     "[] 0"              -----------------   Solid line
    ///     "[3] 0"             ---   ---   ---     3 units on, 3 units off, ...
    ///     "[2] 1"             -  --  --  --  --   1 on, 2 off, 2 on, 2 off, ...
    ///     "[2 1] 0"           -- -- -- -- -- --   2 on, 1 off, 2 on, 1 off, ...
    ///     "[3 5] 6"             ---     ---       2 off, 3 on, 5 off, 3 on, 5 off, ...
    ///     "[2 3] 11"          -   --   --   --    1 on, 3 off, 2 on, 3 off, 2 on, ...
    /// </code>
    /// </summary>
    /// <param name="strokeDashPattern">the stroke dash pattern.</param>
    /// <returns>this Arc object.</returns>
    public Arc SetStrokeDashPattern(String strokeDashPattern) {
        this.strokeDashPattern = strokeDashPattern;
        return this;
    }

    /// <summary>
    /// Sets the width of this line.
    /// </summary>
    /// <param name="width">the width.</param>
    /// <returns>this Arc object.</returns>
    public Arc SetStrokeWidth(double width) {
        this.strokeWidth = (float) width;
        return this;
    }

    /// <summary>
    /// Sets the width of this line.
    /// </summary>
    /// <param name="width">the width.</param>
    /// <returns>this Arc object.</returns>
    public Arc SetStrokeWidth(float width) {
        this.strokeWidth = width;
        return this;
    }

    /// <summary>
    /// Sets the color for this line.
    /// </summary>
    /// <param name="color">the color specified as an integer.</param>
    /// <returns>this Arc object.</returns>
    public Arc SetStrokeColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.SetStrokeColor(r, g, b);
        return this;
    }

    /// <summary>Sets the stroke color from red, green and blue values between 0.0 and 1.0.</summary>
    public Arc SetStrokeColor(float r, float g, float b) {
        this.strokeColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the stroke color from an array of red, green and blue values.</summary>
    public Arc SetStrokeColor(float[] rgbColor) {
        this.strokeColor = rgbColor;
        return this;
    }

    /// <summary>Sets the fill color as a 0xRRGGBB value, for example Color.blue.</summary>
    public Arc SetFillColor(int color) {
        float r = ((color >> 16) & 0xff)/255f;
        float g = ((color >>  8) & 0xff)/255f;
        float b = ((color)       & 0xff)/255f;
        this.SetFillColor(r, g, b);
        return this;
    }

    /// <summary>Sets the fill color from red, green and blue values between 0.0 and 1.0.</summary>
    public Arc SetFillColor(float r, float g, float b) {
        this.fillColor = new float[] {r, g, b};
        return this;
    }

    /// <summary>Sets the fill color from an array of red, green and blue values.</summary>
    public Arc SetFillColor(float[] rgbColor) {
        this.fillColor = rgbColor;
        return this;
    }

    /// <summary>Rotates this arc clockwise by the specified degrees.</summary>
    public Arc SetRotateDegreesCW(float degrees) {
        this.rotateDegrees = -degrees;
        return this;
    }

    /// <summary>Rotates this arc clockwise by the specified degrees.</summary>
    public Arc SetRotateDegreesCW(double degrees) {
        this.rotateDegrees = (float) -degrees;
        return this;
    }

    /// <summary>Rotates this arc counterclockwise by the specified degrees.</summary>
    public Arc SetRotateDegreesCCW(float degrees) {
        this.rotateDegrees = degrees;
        return this;
    }

    /// <summary>Rotates this arc counterclockwise by the specified degrees.</summary>
    public Arc SetRotateDegreesCCW(double degrees) {
        this.rotateDegrees = (float) degrees;
        return this;
    }

    /// <summary>
    /// Sets the alternate description of this line.
    /// </summary>
    /// <param name="altDescription">the alternate description of the line.</param>
    /// <returns>this Arc.</returns>
    public Arc SetAltDescription(String altDescription) {
        this.altDescription = altDescription;
        return this;
    }

    /// <summary>
    /// Sets the actual text for this line.
    /// </summary>
    /// <param name="actualText">the actual text for the line.</param>
    /// <returns>this Arc.</returns>
    public Arc SetActualText(String actualText) {
        this.actualText = actualText;
        return this;
    }

    /// <summary>
    /// Scales this line by the specified factor.
    /// </summary>
    /// <param name="factor">the factor used to scale the line.</param>
    /// <returns>this Arc object.</returns>
    public Arc SetScaleFactor(double factor) {
        return SetScaleFactor((float) factor);
    }

    /// <summary>
    /// Scales this line by the specified factor.
    /// </summary>
    /// <param name="factor">the factor used to scale the line.</param>
    /// <returns>this Arc object.</returns>
    public Arc SetScaleFactor(float factor) {
        this.rx *= factor;
        this.ry *= factor;
        return this;
    }

    /// <summary>
    /// Draws this line on the specified page.
    /// </summary>
    /// <param name="page">the page to draw on.</param>
    /// <returns>x and y coordinates of the bottom right corner of this component.</returns>
    public float[] DrawOn(Page page) {
        // If a start point was set, calculate center so arc begins there
        if (line != null) {
            float dx = line.x2 - line.x1;
            float dy = line.y2 - line.y1;
            // Normalize and rotate 90° (clockwise perpendicular)
            float invLength = 1f / MathF.Sqrt(dx*dx + dy*dy);
            float nx = -dy * invLength;
            float ny = dx * invLength;
            // Adjust direction based on sweep
            float sign = sweepDegrees > 0f ? 1f : -1f;
            cx = line.x2 + nx * rx * sign;
            cy = line.y2 + ny * ry * sign;
            startAngle = MathF.Atan2(line.y2 - cy, line.x2 - cx) * (180f / MathF.PI);
        }

        page.AddBMC(StructElem.P, language, actualText, altDescription);

        page.SaveGraphicsState();

        float centerX = cx;
        float centerY = page.height - cy;
        page.RotateAroundCenter(centerX, centerY, rotateDegrees);
        page.DrawArc(
                cx,
                cy,
                rx,
                ry,
                startAngle,
                sweepDegrees);
        if (strokeColor != null && strokeDashPattern != null) {
            page.SetStrokeDashPattern(strokeDashPattern);
        }
        if (fillColor != null && strokeColor != null) {
            page.SetBrushColor(fillColor);
            page.SetPenWidth(strokeWidth);
            page.SetPenColor(strokeColor);
            page.Append("B\n");
        } else if (fillColor != null && strokeColor == null) {
            page.SetBrushColor(fillColor);
            page.Append("f\n");
        } else if (fillColor == null && strokeColor != null) {
            page.SetPenWidth(strokeWidth);
            page.SetPenColor(strokeColor);
            page.Append("S\n");
        } else {    // Both brushColor == null and penColor == null
            page.SetPenWidth(0f);
            page.SetPenColor(Color.black);
            page.Append("S\n");
        }

        page.RestoreGraphicsState();

        page.AddEMC();
        return new float[] {cx + rx, cy + ry};
    }
}   // End of Arc.cs
}   // End of namespace PDFjet.NET
