/*
 * CrossPort.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import com.pdfjet.*;
import java.io.BufferedOutputStream;
import java.io.ByteArrayOutputStream;
import java.io.FileOutputStream;
import java.io.OutputStream;
import java.nio.charset.StandardCharsets;

/**
 * The Java port's side of the cross-port check,
 * tests/crossport/check-crossport.sh: the shapes of tests/crossport/go, drawn
 * with the same random numbers, and the content of each page written.
 *
 *     java CrossPort SEED CASES OUT
 */
public class CrossPort {
    // SplitMix64, which every port computes the same.
    private long state;

    private CrossPort(long seed) {
        this.state = seed;
    }

    private long next() {
        state += 0x9E3779B97F4A7C15L;
        long z = state;
        z = (z ^ (z >>> 30)) * 0xBF58476D1CE4E5B9L;
        z = (z ^ (z >>> 27)) * 0x94D049BB133111EBL;
        return z ^ (z >>> 31);
    }

    // A number from lo up to hi, of 24 bits, which a float holds exactly.
    private float between(float lo, float hi) {
        float unit = (float) (next() >>> 40) / 16777216f;
        return lo + unit * (hi - lo);
    }

    // A whole number from 0 up to n.
    private int below(int n) {
        return (int) Long.remainderUnsigned(next(), n);
    }

    private int color() {
        return below(0x1000000);
    }

    // A number from lo up to hi, and one time in two hundred a number no page
    // holds: NaN, an infinity or a huge one.
    private float place(float lo, float hi) {
        if (below(200) == 0) {
            switch (below(8)) {
            case 0:
                return Float.NaN;
            case 1:
                return Float.POSITIVE_INFINITY;
            case 2:
                return Float.NEGATIVE_INFINITY;
            case 3:
                return 1e30f;
            default:
                return -1e30f;
            }
        }
        return between(lo, hi);
    }

    // A dash pattern.
    private String dash() {
        int on = 1 + below(9);
        return "[" + on + " " + (1 + below(9)) + "] 0";
    }

    // The start of the message of a document that failed before it completed.
    private static final String EARLIER = "The PDF was not completed because of an earlier error: ";

    public static void main(String[] args) throws Exception {
        long seed = Long.parseUnsignedLong(args[0]);
        int cases = Integer.parseInt(args[1]);
        try (OutputStream out = new BufferedOutputStream(new FileOutputStream(args[2]))) {
            for (int i = 0; i < cases; i++) {
                out.write(("case " + i + "\n").getBytes(StandardCharsets.US_ASCII));
                out.write(new CrossPort(seed + i).drawCase());
            }
        }
    }

    // Gives the arc a stroke width, a stroke color or none, a dash pattern or
    // none, a fill color or none, and a rotation.
    private void styleArc(Arc arc) {
        arc.setStrokeWidth(between(0f, 10f));
        if (below(2) == 0) {
            arc.setStrokeColor(color());
        }
        if (below(2) == 0) {
            arc.setStrokeDashPattern(dash());
        }
        if (below(2) == 0) {
            arc.setFillColor(color());
        }
        arc.setRotation(between(-360f, 360f));
    }

    // Draws the shapes of one case on a page of a document of its own, and
    // returns the content of the page, or "error: " and the message of the
    // first error when the document fails.
    private byte[] drawCase() {
        try {
            PDF pdf = new PDF(new ByteArrayOutputStream());
            Font font = new Font(pdf, CoreFont.HELVETICA);
            Page page = new Page(pdf, Letter.PORTRAIT);
            int shapes = 1 + below(4);
            for (int k = 0; k < shapes; k++) {
                switch (below(8)) {
                case 0: {
                    Arc arc = new Arc();
                    arc.setLocation(place(0f, 612f), place(0f, 792f));
                    arc.setRadiusX(between(0f, 300f));
                    arc.setRadiusY(between(0f, 300f));
                    arc.setStartAngle(between(-720f, 720f));
                    arc.setSweep(between(-720f, 720f));
                    styleArc(arc);
                    arc.drawOn(page);
                    break;
                }
                case 1: {
                    // The whole of an ellipse, which a random sweep never is
                    Ellipse ellipse = new Ellipse();
                    ellipse.setLocation(place(0f, 612f), place(0f, 792f));
                    ellipse.setRadiusX(between(0f, 300f));
                    ellipse.setRadiusY(between(0f, 300f));
                    styleArc(ellipse);
                    ellipse.drawOn(page);
                    break;
                }
                case 2: {
                    DonutChart chart = new DonutChart(font, font);
                    chart.setLocation(place(0f, 612f), place(0f, 792f));
                    float outer = between(1f, 200f);
                    chart.setRadii(outer, between(0f, outer));
                    int slices = 1 + below(6);
                    for (int s = 0; s < slices; s++) {
                        chart.addSlice(new Slice(between(0f, 100f), color(), "s" + s));
                    }
                    chart.drawOn(page);
                    break;
                }
                case 3: {
                    Path path = new Path();
                    int points = 2 + below(6);
                    for (int p = 0; p < points; p++) {
                        float x = place(0f, 612f);
                        float y = place(0f, 792f);
                        if (p > 0 && p < points - 2 && below(3) == 0) {
                            // A curve: its two control points and the point it ends at
                            path.add(new Point(x, y, Point.CONTROL_POINT_C));
                            path.add(new Point(between(0f, 612f), between(0f, 792f), Point.CONTROL_POINT_C));
                            path.add(new Point(between(0f, 612f), between(0f, 792f)));
                            p += 2;
                        } else {
                            path.add(new Point(x, y));
                        }
                    }
                    path.setClosed(below(2) == 0);
                    path.setFillShape(below(2) == 0);
                    path.setStrokeWidth(between(0f, 5f));
                    path.drawOn(page);
                    break;
                }
                case 4: {
                    Line line = new Line(place(0f, 612f), place(0f, 792f), place(0f, 612f), place(0f, 792f));
                    line.setStrokeWidth(between(0f, 10f));
                    if (below(2) == 0) {
                        line.setStrokeColor(color());
                    }
                    if (below(2) == 0) {
                        line.setStrokeDashPattern(dash());
                    }
                    line.drawOn(page);
                    break;
                }
                case 5: {
                    // A size below zero too, and rounded corners
                    Rect rect = new Rect(place(0f, 612f), place(0f, 792f), between(-200f, 300f), between(-200f, 300f));
                    if (below(2) == 0) {
                        rect.setCornerRadius(between(0f, 50f));
                    }
                    if (below(2) == 0) {
                        rect.setFillColor(color());
                    }
                    rect.setBorderColor(color());
                    rect.setBorderWidth(between(0f, 5f));
                    rect.drawOn(page);
                    break;
                }
                case 6: {
                    // Every shape of a point, in the order of the enum in every port
                    Point point = new Point(place(0f, 612f), place(0f, 792f));
                    point.setRadius(between(0f, 20f));
                    point.setShape(Shape.values()[below(14)]);
                    if (below(2) == 0) {
                        point.setFillColor(color());
                    }
                    point.setStrokeColor(color());
                    point.drawOn(page);
                    break;
                }
                default: {
                    TextLine line = new TextLine(font, "Text " + k);
                    line.setFontSize(between(4f, 40f));
                    line.setLocation(place(0f, 612f), place(0f, 792f));
                    line.setTextRotation(below(361) - 180);
                    line.drawOn(page);
                    break;
                }
                }
            }
            byte[] content = page.getContent();
            pdf.complete();
            byte[] withNewline = new byte[content.length + 1];
            System.arraycopy(content, 0, withNewline, 0, content.length);
            withNewline[content.length] = '\n';
            return withNewline;
        } catch (Exception e) {
            String message = e.getMessage();
            if (message.startsWith(EARLIER)) {
                message = message.substring(EARLIER.length());
            }
            return ("error: " + message + "\n").getBytes(StandardCharsets.UTF_8);
        }
    }
}
