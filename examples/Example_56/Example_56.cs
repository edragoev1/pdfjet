/*
 * Example_56.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_56.cs
 *
 * The shapes PDFjet draws, each in a cell of its own under its name: lines
 * with dashes and caps, rectangles with square and rounded corners, the
 * markers of charts, an ellipse, arcs, a path of Bézier curves, and a closed
 * path, filled. The second page draws text at every angle around a point.
 *
 * Each shape is a Drawable: it is given its location, its size, its colors
 * and the width of its stroke, and DrawOn draws it as vector graphics, which
 * stay sharp at any zoom. The shapes carry no text, so they are artifacts of
 * the PDF/UA document, and the name under each shape says what it is.
 */
public class Example_56 {
    private const int NAVY = 0x1d3557;
    private const int TEAL = 0x2a9d8f;
    private const int RED = 0xe63946;
    private const int PALE = 0xe8f1f2;

    // The cells of the grid: four across, 128 points wide, and 160 tall.
    private const float LEFT = 50f;
    private const float TOP = 150f;
    private const float CELL_WIDTH = 128f;
    private const float CELL_HEIGHT = 160f;

    private readonly Font label;

    public Example_56() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_56.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Shapes");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        label = new Font(pdf, IBMPlexSans.Regular);
        label.SetSize(9f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(semiBold, "Shapes");
        title.SetStructureType(StructElem.H1);
        title.SetFontSize(22f);
        title.SetLocation(LEFT, 70f);
        title.DrawOn(page);

        TextBlock about = new TextBlock(regular,
                "PDFjet draws shapes as vector graphics, which stay sharp at any zoom. Each "
                + "shape is a Drawable: give it a location, a size, its colors and the width "
                + "of its stroke, and drawOn draws it on the page.");
        about.SetFontSize(11f);
        about.SetLineSpacing(1.4f);
        about.SetLocation(LEFT, 85f);
        about.SetWidth(512f);
        about.DrawOn(page);

        // Lines: solid, dashed and dotted with round caps
        float[] c = Cell(page, 0, 0, "Line: solid, dashed, and dotted with round caps");
        new Line(c[0] + 10f, c[1] + 30f, c[0] + 110f, c[1] + 30f)
                .SetStrokeWidth(3f).SetStrokeColor(NAVY).DrawOn(page);
        new Line(c[0] + 10f, c[1] + 60f, c[0] + 110f, c[1] + 60f)
                .SetStrokeWidth(3f).SetStrokeColor(TEAL).SetStrokeDashPattern("[8 4] 0").DrawOn(page);
        new Line(c[0] + 10f, c[1] + 90f, c[0] + 110f, c[1] + 90f)
                .SetStrokeWidth(5f).SetStrokeColor(RED).SetStrokeDashPattern("[0 10] 0")
                .SetLineCapStyle(CapStyle.ROUND).DrawOn(page);

        // A rectangle, filled and outlined
        c = Cell(page, 1, 0, "Rect: filled, with a border");
        new Rect(c[0] + 10f, c[1] + 20f, 100f, 80f)
                .SetFillColor(PALE).SetBorderColor(NAVY).SetBorderWidth(2f).DrawOn(page);

        // A rectangle with rounded corners
        c = Cell(page, 2, 0, "Rect: setCornerRadius");
        new Rect(c[0] + 10f, c[1] + 20f, 100f, 80f)
                .SetFillColor(TEAL).SetBorderColor(NAVY).SetBorderWidth(2f)
                .SetCornerRadius(16f).DrawOn(page);

        // The markers of a chart, each a Point with a shape
        c = Cell(page, 3, 0, "Point: the markers of charts");
        Shape[] shapes = {
            Shape.CIRCLE, Shape.DIAMOND, Shape.BOX, Shape.STAR,
            Shape.UP_ARROW, Shape.DOWN_ARROW, Shape.PLUS, Shape.X_MARK,
        };
        for (int i = 0; i < shapes.Length; i++) {
            Point point = new Point(c[0] + 20f + (i % 4) * 27f, c[1] + 35f + (i / 4) * 45f);
            point.SetShape(shapes[i]);
            point.SetRadius(9f);
            point.SetFillColor(i % 2 == 0 ? TEAL : RED);
            point.SetStrokeColor(NAVY);
            point.DrawOn(page);
        }

        // An ellipse, turned by 30 degrees
        c = Cell(page, 0, 1, "Ellipse: setRotation");
        new Ellipse()
                .SetLocation(c[0] + 60f, c[1] + 60f)
                .SetRadiusX(50f)
                .SetRadiusY(25f)
                .SetFillColor(PALE)
                .SetStrokeWidth(2f)
                .SetStrokeColor(NAVY)
                .SetRotation(30f)
                .DrawOn(page);

        // An arc of three quarters of a circle
        c = Cell(page, 1, 1, "Arc: setStartAngle and setSweep");
        new Arc()
                .SetLocation(c[0] + 60f, c[1] + 60f)
                .SetRadius(40f)
                .SetStartAngle(0f)
                .SetSweep(270f)
                .SetStrokeWidth(6f)
                .SetStrokeColor(TEAL)
                .DrawOn(page);

        // A wave of Bézier curves: a point, two control points, a point
        c = Cell(page, 2, 1, "Path: Bézier curves");
        PDFjet.NET.Path wave = new PDFjet.NET.Path();
        wave.Add(new Point(0f, 30f));
        wave.Add(new Point(20f, 0f, Point.CONTROL_POINT_C));
        wave.Add(new Point(30f, 0f, Point.CONTROL_POINT_C));
        wave.Add(new Point(50f, 30f));
        wave.Add(new Point(70f, 60f, Point.CONTROL_POINT_C));
        wave.Add(new Point(80f, 60f, Point.CONTROL_POINT_C));
        wave.Add(new Point(100f, 30f));
        wave.SetStrokeColor(RED);
        wave.SetStrokeWidth(3f);
        wave.SetLocation(c[0] + 10f, c[1] + 30f);
        wave.DrawOn(page);

        // A closed path of lines, filled: a six-pointed star
        c = Cell(page, 3, 1, "Path: closed and filled");
        PDFjet.NET.Path star = new PDFjet.NET.Path();
        for (int i = 0; i < 12; i++) {
            double angle = Math.PI / 6.0 * i - Math.PI / 2.0;
            float r = (i % 2 == 0) ? 50f : 25f;
            star.Add(new Point(50f + r * (float) Math.Cos(angle), 50f + r * (float) Math.Sin(angle)));
        }
        star.SetClosed(true);
        star.SetFillShape(true);
        star.SetStrokeColor(NAVY);
        star.SetLocation(c[0] + 10f, c[1] + 10f);
        star.DrawOn(page);

        // The second page: text at every angle around a point
        page = new Page(pdf, Letter.PORTRAIT);
        TextLine heading = new TextLine(semiBold, "Text at every angle");
        heading.SetStructureType(StructElem.H2);
        heading.SetFontSize(18f);
        heading.SetLocation(LEFT, 70f);
        heading.DrawOn(page);

        TextBlock rotation = new TextBlock(regular,
                "setTextRotation turns a TextLine about the point of its location, here every "
                + "15 degrees about the middle of the page, underlined with setUnderline.");
        rotation.SetFontSize(11f);
        rotation.SetLineSpacing(1.4f);
        rotation.SetLocation(LEFT, 85f);
        rotation.SetWidth(512f);
        rotation.DrawOn(page);

        float cx = page.GetWidth() / 2f;
        float cy = 380f;
        TextLine text = new TextLine(regular);
        text.SetFontSize(10f);
        text.SetUnderline(true);
        text.SetLocation(cx, cy);
        for (int i = 0; i < 360; i += 15) {
            text.SetTextRotation(-i);
            // The spaces keep the start of the text clear of the circle
            text.SetText("                        Hello, World: " + i + " degrees");
            text.DrawOn(page);
        }
        Point hub = new Point(cx, cy);
        hub.SetShape(Shape.CIRCLE);
        hub.SetFillColor(NAVY);
        hub.SetRadius(46f);
        hub.DrawOn(page);
        hub.SetFillColor(Color.white);
        hub.SetRadius(32f);
        hub.DrawOn(page);

        pdf.Complete();
    }

    // Draws the name of the cell of the grid under it, and returns the top
    // left corner of its drawing area.
    private float[] Cell(Page page, int column, int row, String name) {
        float x = LEFT + column * CELL_WIDTH;
        float y = TOP + row * CELL_HEIGHT;
        TextBlock caption = new TextBlock(label, name);
        caption.SetTextColor(Color.gray);
        caption.SetLocation(x, y + 125f);
        caption.SetWidth(CELL_WIDTH - 12f);
        caption.DrawOn(page);
        return new float[] {x, y};
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_56();
        long time1 = sw.ElapsedMilliseconds;
        Console.WriteLine($"Example_56 => {time1 - time0,4} ms");
    }
}   // End of Example_56.cs
