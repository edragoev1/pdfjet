/*
 * Example_56.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * The shapes PDFjet draws, each in a cell of its own under its name: lines
 * with dashes and caps, rectangles with square and rounded corners, the
 * markers of charts, an ellipse, arcs, a path of Bézier curves, and a closed
 * path, filled. The second page draws text at every angle around a point.
 * <p>
 * Each shape is a Drawable: it is given its location, its size, its colors
 * and the width of its stroke, and drawOn draws it as vector graphics, which
 * stay sharp at any zoom. The shapes carry no text, so they are artifacts of
 * the PDF/UA document, and the name under each shape says what it is.
 * </p>
 *
 * @see Line
 * @see Rect
 * @see Point
 * @see Ellipse
 * @see Arc
 * @see Path
 */
public class Example_56 {
    private static final int NAVY = 0x1d3557;
    private static final int TEAL = 0x2a9d8f;
    private static final int RED = 0xe63946;
    private static final int PALE = 0xe8f1f2;

    // The cells of the grid: four across, 128 points wide, and 160 tall.
    private static final float LEFT = 50f;
    private static final float TOP = 150f;
    private static final float CELL_WIDTH = 128f;
    private static final float CELL_HEIGHT = 160f;

    private final Font label;

    public Example_56() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_56.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Shapes");

        Font regular = new Font(pdf, IBMPlexSans.Regular);
        Font semiBold = new Font(pdf, IBMPlexSans.SemiBold);
        label = new Font(pdf, IBMPlexSans.Regular);
        label.setSize(9f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(semiBold, "Shapes");
        title.setStructureType(StructElem.H1);
        title.setFontSize(22f);
        title.setLocation(LEFT, 70f);
        title.drawOn(page);

        TextBlock about = new TextBlock(regular,
                "PDFjet draws shapes as vector graphics, which stay sharp at any zoom. Each "
                + "shape is a Drawable: give it a location, a size, its colors and the width "
                + "of its stroke, and drawOn draws it on the page.");
        about.setFontSize(11f);
        about.setLineSpacing(1.4f);
        about.setLocation(LEFT, 85f);
        about.setWidth(512f);
        about.drawOn(page);

        // Lines: solid, dashed and dotted with round caps
        float[] c = cell(page, 0, 0, "Line: solid, dashed, and dotted with round caps");
        new Line(c[0] + 10f, c[1] + 30f, c[0] + 110f, c[1] + 30f)
                .setStrokeWidth(3f).setStrokeColor(NAVY).drawOn(page);
        new Line(c[0] + 10f, c[1] + 60f, c[0] + 110f, c[1] + 60f)
                .setStrokeWidth(3f).setStrokeColor(TEAL).setStrokeDashPattern("[8 4] 0").drawOn(page);
        new Line(c[0] + 10f, c[1] + 90f, c[0] + 110f, c[1] + 90f)
                .setStrokeWidth(5f).setStrokeColor(RED).setStrokeDashPattern("[0 10] 0")
                .setLineCapStyle(CapStyle.ROUND).drawOn(page);

        // A rectangle, filled and outlined
        c = cell(page, 1, 0, "Rect: filled, with a border");
        new Rect(c[0] + 10f, c[1] + 20f, 100f, 80f)
                .setFillColor(PALE).setBorderColor(NAVY).setBorderWidth(2f).drawOn(page);

        // A rectangle with rounded corners
        c = cell(page, 2, 0, "Rect: setCornerRadius");
        new Rect(c[0] + 10f, c[1] + 20f, 100f, 80f)
                .setFillColor(TEAL).setBorderColor(NAVY).setBorderWidth(2f)
                .setCornerRadius(16f).drawOn(page);

        // The markers of a chart, each a Point with a shape
        c = cell(page, 3, 0, "Point: the markers of charts");
        Shape[] shapes = {
            Shape.CIRCLE, Shape.DIAMOND, Shape.BOX, Shape.STAR,
            Shape.UP_ARROW, Shape.DOWN_ARROW, Shape.PLUS, Shape.X_MARK,
        };
        for (int i = 0; i < shapes.length; i++) {
            Point point = new Point(c[0] + 20f + (i % 4) * 27f, c[1] + 35f + (i / 4) * 45f);
            point.setShape(shapes[i]);
            point.setRadius(9f);
            point.setFillColor(i % 2 == 0 ? TEAL : RED);
            point.setStrokeColor(NAVY);
            point.drawOn(page);
        }

        // An ellipse, turned by 30 degrees
        c = cell(page, 0, 1, "Ellipse: setRotation");
        new Ellipse()
                .setLocation(c[0] + 60f, c[1] + 60f)
                .setRadiusX(50f)
                .setRadiusY(25f)
                .setFillColor(PALE)
                .setStrokeWidth(2f)
                .setStrokeColor(NAVY)
                .setRotation(30f)
                .drawOn(page);

        // An arc of three quarters of a circle
        c = cell(page, 1, 1, "Arc: setStartAngle and setSweep");
        new Arc()
                .setLocation(c[0] + 60f, c[1] + 60f)
                .setRadius(40f)
                .setStartAngle(0f)
                .setSweep(270f)
                .setStrokeWidth(6f)
                .setStrokeColor(TEAL)
                .drawOn(page);

        // A wave of Bézier curves: a point, two control points, a point
        c = cell(page, 2, 1, "Path: Bézier curves");
        Path wave = new Path();
        wave.add(new Point(0f, 30f));
        wave.add(new Point(20f, 0f, Point.CONTROL_POINT_C));
        wave.add(new Point(30f, 0f, Point.CONTROL_POINT_C));
        wave.add(new Point(50f, 30f));
        wave.add(new Point(70f, 60f, Point.CONTROL_POINT_C));
        wave.add(new Point(80f, 60f, Point.CONTROL_POINT_C));
        wave.add(new Point(100f, 30f));
        wave.setStrokeColor(RED);
        wave.setStrokeWidth(3f);
        wave.setLocation(c[0] + 10f, c[1] + 30f);
        wave.drawOn(page);

        // A closed path of lines, filled: a six-pointed star
        c = cell(page, 3, 1, "Path: closed and filled");
        Path star = new Path();
        for (int i = 0; i < 12; i++) {
            double angle = Math.PI / 6.0 * i - Math.PI / 2.0;
            float r = (i % 2 == 0) ? 50f : 25f;
            star.add(new Point(50f + r * (float) Math.cos(angle), 50f + r * (float) Math.sin(angle)));
        }
        star.setClosed(true);
        star.setFillShape(true);
        star.setStrokeColor(NAVY);
        star.setLocation(c[0] + 10f, c[1] + 10f);
        star.drawOn(page);

        // The second page: text at every angle around a point
        page = new Page(pdf, Letter.PORTRAIT);
        TextLine heading = new TextLine(semiBold, "Text at every angle");
        heading.setStructureType(StructElem.H2);
        heading.setFontSize(18f);
        heading.setLocation(LEFT, 70f);
        heading.drawOn(page);

        TextBlock rotation = new TextBlock(regular,
                "setTextRotation turns a TextLine about the point of its location, here every "
                + "15 degrees about the middle of the page, underlined with setUnderline.");
        rotation.setFontSize(11f);
        rotation.setLineSpacing(1.4f);
        rotation.setLocation(LEFT, 85f);
        rotation.setWidth(512f);
        rotation.drawOn(page);

        float cx = page.getWidth() / 2f;
        float cy = 380f;
        TextLine text = new TextLine(regular);
        text.setFontSize(10f);
        text.setUnderline(true);
        text.setLocation(cx, cy);
        for (int i = 0; i < 360; i += 15) {
            text.setTextRotation(-i);
            // The spaces keep the start of the text clear of the circle
            text.setText("                        Hello, World: " + i + " degrees");
            text.drawOn(page);
        }
        Point hub = new Point(cx, cy);
        hub.setShape(Shape.CIRCLE);
        hub.setFillColor(NAVY);
        hub.setRadius(46f);
        hub.drawOn(page);
        hub.setFillColor(Color.white);
        hub.setRadius(32f);
        hub.drawOn(page);

        pdf.complete();
    }

    // Draws the name of the cell of the grid under it, and returns the top
    // left corner of its drawing area.
    private float[] cell(Page page, int column, int row, String name) throws Exception {
        float x = LEFT + column * CELL_WIDTH;
        float y = TOP + row * CELL_HEIGHT;
        TextBlock caption = new TextBlock(label, name);
        caption.setTextColor(Color.gray);
        caption.setLocation(x, y + 125f);
        caption.setWidth(CELL_WIDTH - 12f);
        caption.drawOn(page);
        return new float[] {x, y};
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_56();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_56 => %4d ms%n", time1 - time0);
    }
}   // End of Example_56.java
