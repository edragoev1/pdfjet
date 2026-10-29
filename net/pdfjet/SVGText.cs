/*
 * SVGText.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// A text element of an SVG file, with its tspan elements: the runs of its
/// characters, each with where it starts, if it says, and what it is drawn
/// with. The runs are placed when the text is drawn, as their widths are
/// those of the font it is drawn with.
/// </summary>
internal class SVGText {
    // The transform from the user space of the text element to the space of
    // the svg element, and after the svg element is drawn at its size, to the
    // space of the image.
    internal double[] matrix;
    internal readonly List<Run> runs = new List<Run>();

    // The characters of a text or a tspan element between the elements in it.
    internal sealed class Run {
        internal String text;
        internal double x = double.NaN;     // Where the run starts, if it says: a new chunk
        internal double y = double.NaN;
        internal double dx = 0.0;           // How far it moves from where the run before it ends
        internal double dy = 0.0;
        internal float fontSize;
        internal int weight;                // The font-weight, 400 normal and 700 bold
        internal bool italic;
        internal int family;                // BundledFonts.SANS, SERIF or MONO
        internal int anchor;                // 0 start, 1 middle, 2 end
        internal int baseline;              // 0 alphabetic, 1 middle, 2 hanging
        internal int fill;
        internal int stroke;
        internal float fillAlpha;
        internal float strokeAlpha;
        internal float strokeWidth;         // In the user space of the text element
        internal bool strokeFirst;
        internal JoinStyle lineJoin;

        // Where the run is placed: its start, on its baseline, and its width.
        internal float px;
        internal float py;
        internal float width;
        internal Font font;
    }

    // Collapses the white space of the runs as a browser does: a newline or a
    // tab is a space, spaces one after the other are one, and the text has
    // none at its start and at its end.
    internal void CollapseSpaces() {
        bool space = true;  // A space before the text: one at its start is left out
        foreach (Run run in runs) {
            StringBuilder buf = new StringBuilder(run.text.Length);
            foreach (char ch in run.text) {
                if (ch == '\n' || ch == '\r' || ch == '\t' || ch == ' ') {
                    if (!space) {
                        buf.Append(' ');
                    }
                    space = true;
                } else {
                    buf.Append(ch);
                    space = false;
                }
            }
            run.text = buf.ToString();
        }
        for (int i = runs.Count - 1; i >= 0; i--) {
            Run run = runs[i];
            if (run.text.EndsWith(" ", StringComparison.Ordinal)) {
                run.text = run.text.Substring(0, run.text.Length - 1);
                break;
            }
            if (run.text.Length > 0) {
                break;
            }
        }
    }

    // Places the runs, in the user space of the text element, in the fonts
    // they are given: each where it says or after the run before it, and each
    // chunk, the runs from one that says where it starts to the next, moved
    // by its width as its text-anchor asks.
    internal void Place() {
        double x = 0.0;
        double y = 0.0;
        int chunk = 0;      // The first run of the chunk
        for (int i = 0; i < runs.Count; i++) {
            Run run = runs[i];
            if (i == 0 || !double.IsNaN(run.x) || !double.IsNaN(run.y)) {
                Anchor(chunk, i);
                chunk = i;
            }
            if (!double.IsNaN(run.x)) {
                x = run.x;
            }
            if (!double.IsNaN(run.y)) {
                y = run.y;
            }
            x += run.dx;
            y += run.dy;
            run.width = run.font.StringWidth(run.fontSize, Text(run));
            run.px = (float) x;
            run.py = (float) y;
            if (run.baseline == 1) {
                run.py += 0.35f*run.fontSize;
            } else if (run.baseline == 2) {
                run.py += 0.8f*run.fontSize;
            }
            x += run.width;
        }
        Anchor(chunk, runs.Count);
    }

    // Moves the runs of a chunk by its width as the text-anchor of its first
    // run asks.
    private void Anchor(int from, int to) {
        if (from >= to) {
            return;
        }
        int anchor = runs[from].anchor;
        if (anchor == 0) {
            return;
        }
        float width = 0f;
        for (int i = from; i < to; i++) {
            width += runs[i].width;
        }
        float shift = (anchor == 1) ? -width/2f : -width;
        for (int i = from; i < to; i++) {
            runs[i].px += shift;
        }
    }

    // The characters of the run as its font draws them: a core font, which
    // has no minus sign, draws the hyphen-minus for it, as the numbers of a
    // chart are written with it.
    internal static String Text(Run run) {
        return run.font.isCoreFont ? run.text.Replace('−', '-') : run.text;
    }
}   // End of SVGText.cs
}   // End of PDFjet.NET namespace
