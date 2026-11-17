/**
 * Shaper.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// A text engine that shapes text for ComplexText: it wraps the text to a
/// width and finds, for each line, the glyphs of the fonts and their places.
/// PDFjet does not shape scripts such as Hindi and Khmer itself; a shaper
/// does, and ComplexText draws what it found.
///
/// ComplexText picks the platform's shaper by itself: Apple's Core Text on
/// iPhone, iPad and Mac. Swift on Linux has none: a program gives its own,
/// such as one that calls HarfBuzz.
///
public protocol Shaper {
    ///
    /// Wraps the text to the width and shapes each line.
    ///
    /// - Parameter text: the text.
    /// - Parameter language: the language of the text, such as "hi" or "km", or nil.
    /// - Parameter fontFiles: the bytes of the font files: the first for the
    ///   text, the others, in order, for the characters it does not have.
    /// - Parameter fontSize: the font size, in points.
    /// - Parameter width: the width of the lines, in points.
    /// - Returns: the lines, in order.
    /// - Throws: an error if the text cannot be shaped.
    ///
    func shape(_ text: String, _ language: String?, _ fontFiles: [[UInt8]],
            _ fontSize: Float, _ width: Float) throws -> [ShapedLine]
}

///
/// A line of text a Shaper shaped: the part of the text it holds, and its
/// glyphs, each with its font and its place.
///
public struct ShapedLine {
    /// The index in the text's UTF-16 code units of the line's first character.
    public let start: Int
    /// The index in the text's UTF-16 code units after the line's last character.
    public let end: Int
    /// For each glyph, the index of its font among the font files the shaper
    /// was given, or -1 for a glyph of none of them, which is not drawn.
    public let fonts: [Int]
    /// The glyph numbers, in their fonts.
    public let glyphs: [Int]
    /// The x and the y of each glyph, in points, from the left end of the
    /// line at its baseline, y growing down: x0, y0, x1, y1 and so on.
    public let positions: [Float]

    public init(_ start: Int, _ end: Int, _ fonts: [Int], _ glyphs: [Int], _ positions: [Float]) {
        precondition(fonts.count == glyphs.count && positions.count == 2 * glyphs.count,
                "A shaped line needs a font and an x and a y for each glyph.")
        self.start = start
        self.end = end
        self.fonts = fonts
        self.glyphs = glyphs
        self.positions = positions
    }
}   // End of Shaper.swift
