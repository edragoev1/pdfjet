/**
 * SVGText.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A text element of an SVG file, with its tspan elements: the runs of its
/// characters, each with where it starts, if it says, and what it is drawn
/// with. The runs are placed when the text is drawn, as their widths are
/// those of the font it is drawn with.
class SVGText {
    // The transform from the user space of the text element to the space of
    // the svg element, and after the svg element is drawn at its size, to the
    // space of the image.
    var matrix = SVGState.identity
    var runs = [Run]()

    // A tspan with a systemLanguage: an image is read before the language is
    // known, so all the runs are kept as they were read, and the ones drawn are
    // chosen, placed and collapsed when it is drawn.
    var languages = false
    var all: [Run]?
    var moves = [Move]()    // Of the elements read since the last run

    // The x, y, dx and dy of an element of a text, NaN where it has none, in the
    // choices of the element: one that is not chosen does not move the first
    // character after it, whose place stays with the elements around it.
    struct Move {
        let choices: [Choice]?
        let at: [Double]

        // Adds the move to the x, y, dx and dy kept for the first character.
        func apply(to pending: inout [Double]) {
            if !at[0].isNaN {
                pending[0] = at[0]
                pending[2] = 0.0
            }
            if !at[1].isNaN {
                pending[1] = at[1]
                pending[3] = 0.0
            }
            if !at[2].isNaN {
                pending[2] += at[2]
            }
            if !at[3].isNaN {
                pending[3] += at[3]
            }
        }
    }

    // The characters of a text or a tspan element between the elements in it.
    final class Run {
        var text = ""
        var x = Double.nan          // Where the run starts, if it says: a new chunk
        var y = Double.nan
        var dx = 0.0                // How far it moves from where the run before it ends
        var dy = 0.0
        var fontSize: Float = 0.0
        var weight = 400            // The font-weight, 400 normal and 700 bold
        var italic = false
        var family = BundledFonts.SANS  // BundledFonts.SANS, SERIF or MONO
        var anchor = 0              // 0 start, 1 middle, 2 end
        var baseline = 0            // 0 alphabetic, 1 middle, 2 hanging
        var fill: Int32 = 0
        var stroke: Int32 = 0
        var fillAlpha: Float = 0.0
        var strokeAlpha: Float = 0.0
        var strokeWidth: Float = 0.0    // In the user space of the text element
        var strokeFirst = false
        var lineJoin = JoinStyle.MITER
        var miterLimit: Float = 4.0
        var invisible = false           // It keeps its place in the text, and a clip path leaves it out
        var choices: [Choice]?          // Those of its element, and of the text
        var moves = [Move]()            // Of the elements that start at the run
        var read = ""                   // The text as read, of a text with languages

        // Where the run is placed: its start, on its baseline, and its width.
        var px: Float = 0.0
        var py: Float = 0.0
        var width: Float = 0.0
        var font: Font?

        // Returns true when the run has glyphs for a clip path to clip with.
        func clips() -> Bool {
            return !text.isEmpty && fontSize > 0.0 && !invisible
        }
    }

    // Returns true when the text has glyphs for a clip path to clip with.
    func clips() -> Bool {
        return runs.contains { $0.clips() }
    }

    // Returns what a clip path writes for the text, as operations.
    func operations() -> Int {
        return runs.reduce(0) { $0 + 1 + $1.text.unicodeScalars.count }
    }

    // Keeps the runs as they were read, for choose.
    func keepRuns() {
        all = runs
        for run in runs {
            run.read = run.text
        }
    }

    // Makes the runs those of the language chosen, each where the moves of the
    // elements chosen put it, and white space collapsed among them alone.
    func choose() {
        guard let all = all else {
            return
        }
        runs = []
        var pending = [Double.nan, Double.nan, 0.0, 0.0]
        for run in all {
            for move in run.moves where SVGPath.areChosen(move.choices) {
                move.apply(to: &pending)
            }
            if !SVGPath.areChosen(run.choices) {
                continue
            }
            run.text = run.read
            run.x = pending[0]
            run.y = pending[1]
            run.dx = pending[2]
            run.dy = pending[3]
            pending = [Double.nan, Double.nan, 0.0, 0.0]
            runs.append(run)
        }
        collapseSpaces()
    }

    // Collapses the white space of the runs as a browser does: a newline or a
    // tab is a space, spaces one after the other are one, and the text has
    // none at its start and at its end.
    func collapseSpaces() {
        var space = true    // A space before the text: one at its start is left out
        for run in runs {
            var buf = String.UnicodeScalarView()
            for ch in run.text.unicodeScalars {
                if ch == "\n" || ch == "\r" || ch == "\t" || ch == " " {
                    if !space {
                        buf.append(" ")
                    }
                    space = true
                } else {
                    buf.append(ch)
                    space = false
                }
            }
            run.text = String(buf)
        }
        for run in runs.reversed() {
            if SVGState.hasSuffix(run.text, " ") {
                run.text = SVGState.string(run.text.unicodeScalars.dropLast())
                break
            }
            if !run.text.isEmpty {
                break
            }
        }
    }

    // Places the runs, in the user space of the text element, in the fonts
    // they are given: each where it says or after the run before it, and each
    // chunk, the runs from one that says where it starts to the next, moved
    // by its width as its text-anchor asks.
    func place() {
        var x = 0.0
        var y = 0.0
        var chunk = 0       // The first run of the chunk
        for (i, run) in runs.enumerated() {
            if i == 0 || !run.x.isNaN || !run.y.isNaN {
                anchor(chunk, i)
                chunk = i
            }
            if !run.x.isNaN {
                x = run.x
            }
            if !run.y.isNaN {
                y = run.y
            }
            x += run.dx
            y += run.dy
            run.width = run.font!.stringWidth(run.fontSize, SVGText.text(run))
            run.px = Float(x)
            run.py = Float(y)
            if run.baseline == 1 {
                run.py += 0.35 * run.fontSize
            } else if run.baseline == 2 {
                run.py += 0.8 * run.fontSize
            }
            x += Double(run.width)
        }
        anchor(chunk, runs.count)
    }

    // Moves the runs of a chunk by its width as the text-anchor of its first
    // run asks.
    private func anchor(_ from: Int, _ to: Int) {
        if from >= to {
            return
        }
        let anchor = runs[from].anchor
        if anchor == 0 {
            return
        }
        var width: Float = 0.0
        for i in from..<to {
            width += runs[i].width
        }
        let shift = (anchor == 1) ? -width / 2.0 : -width
        for i in from..<to {
            runs[i].px += shift
        }
    }

    // The characters of the run as its font draws them: a core font, which
    // has no minus sign, draws the hyphen-minus for it, as the numbers of a
    // chart are written with it.
    static func text(_ run: Run) -> String {
        if !run.font!.isCoreFont {
            return run.text
        }
        return SVGState.string(run.text.unicodeScalars.map { $0 == "\u{2212}" ? "-" : $0 })
    }
}   // End of SVGText.swift
