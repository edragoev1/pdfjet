/*
 * SVGText.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.List;

/**
 * A text element of an SVG file, with its tspan elements: the runs of its
 * characters, each with where it starts, if it says, and what it is drawn
 * with. The runs are placed when the text is drawn, as their widths are
 * those of the font it is drawn with.
 */
class SVGText {
    // The transform from the user space of the text element to the space of
    // the svg element, and after the svg element is drawn at its size, to the
    // space of the image.
    double[] matrix;
    final List<Run> runs = new ArrayList<Run>();

    // The characters of a text or a tspan element between the elements in it.
    static final class Run {
        String text;
        double x = Double.NaN;      // Where the run starts, if it says: a new chunk
        double y = Double.NaN;
        double dx = 0.0;            // How far it moves from where the run before it ends
        double dy = 0.0;
        float fontSize;
        int weight;                 // The font-weight, 400 normal and 700 bold
        boolean italic;
        int family;                 // BundledFonts.SANS, SERIF or MONO
        int anchor;                 // 0 start, 1 middle, 2 end
        int baseline;               // 0 alphabetic, 1 middle, 2 hanging
        int fill;
        int stroke;
        float fillAlpha;
        float strokeAlpha;
        float strokeWidth;          // In the user space of the text element
        boolean strokeFirst;
        JoinStyle lineJoin;
        float miterLimit;

        // Where the run is placed: its start, on its baseline, and its width.
        float px;
        float py;
        float width;
        Font font;
    }

    // Collapses the white space of the runs as a browser does: a newline or a
    // tab is a space, spaces one after the other are one, and the text has
    // none at its start and at its end.
    void collapseSpaces() {
        boolean space = true;   // A space before the text: one at its start is left out
        for (Run run : runs) {
            StringBuilder buf = new StringBuilder(run.text.length());
            for (int i = 0; i < run.text.length(); i++) {
                char ch = run.text.charAt(i);
                if (ch == '\n' || ch == '\r' || ch == '\t' || ch == ' ') {
                    if (!space) {
                        buf.append(' ');
                    }
                    space = true;
                } else {
                    buf.append(ch);
                    space = false;
                }
            }
            run.text = buf.toString();
        }
        for (int i = runs.size() - 1; i >= 0; i--) {
            Run run = runs.get(i);
            if (run.text.endsWith(" ")) {
                run.text = run.text.substring(0, run.text.length() - 1);
                break;
            }
            if (!run.text.isEmpty()) {
                break;
            }
        }
    }

    // Places the runs, in the user space of the text element, in the fonts
    // they are given: each where it says or after the run before it, and each
    // chunk, the runs from one that says where it starts to the next, moved
    // by its width as its text-anchor asks.
    void place() {
        double x = 0.0;
        double y = 0.0;
        int chunk = 0;      // The first run of the chunk
        for (int i = 0; i < runs.size(); i++) {
            Run run = runs.get(i);
            if (i == 0 || !Double.isNaN(run.x) || !Double.isNaN(run.y)) {
                anchor(chunk, i);
                chunk = i;
            }
            if (!Double.isNaN(run.x)) {
                x = run.x;
            }
            if (!Double.isNaN(run.y)) {
                y = run.y;
            }
            x += run.dx;
            y += run.dy;
            run.width = run.font.stringWidth(run.fontSize, text(run));
            run.px = (float) x;
            run.py = (float) y;
            if (run.baseline == 1) {
                run.py += 0.35f*run.fontSize;
            } else if (run.baseline == 2) {
                run.py += 0.8f*run.fontSize;
            }
            x += run.width;
        }
        anchor(chunk, runs.size());
    }

    // Moves the runs of a chunk by its width as the text-anchor of its first
    // run asks.
    private void anchor(int from, int to) {
        if (from >= to) {
            return;
        }
        int anchor = runs.get(from).anchor;
        if (anchor == 0) {
            return;
        }
        float width = 0f;
        for (int i = from; i < to; i++) {
            width += runs.get(i).width;
        }
        float shift = (anchor == 1) ? -width/2f : -width;
        for (int i = from; i < to; i++) {
            runs.get(i).px += shift;
        }
    }

    // The characters of the run as its font draws them: a core font, which
    // has no minus sign, draws the hyphen-minus for it, as the numbers of a
    // chart are written with it.
    static String text(Run run) {
        return run.font.isCoreFont ? run.text.replace('−', '-') : run.text;
    }
}   // End of SVGText.java
