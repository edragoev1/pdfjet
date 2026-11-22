/**
 * Shaper.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// A text engine that shapes text for TextBlock and TextLine: it wraps the
/// text to a width and finds, for each line, the glyphs of the fonts and their
/// places. PDFjet does not shape scripts such as Hindi and Khmer itself; a
/// shaper does, and PDFjet draws what it found, as outlines.
///
/// PDFjet picks the platform's shaper by itself: Apple's Core Text on
/// iPhone, iPad and Mac. Swift on Linux has none: a program gives one, such
/// as HBShape(), which runs HarfBuzz's program hb-shape, with setShaper or
/// TextBlock.setDefaultShaper.
///
public protocol Shaper {
    ///
    /// Wraps the text to the width and shapes each line.
    ///
    /// - Parameter request: the text, its language, the font files, the font
    ///   size and the width of the lines.
    /// - Returns: the lines, in order.
    /// - Throws: an error if the text cannot be shaped.
    ///
    func shape(_ request: ShapingRequest) throws -> [ShapedLine]
}

///
/// What a Shaper is asked to shape: the text, its language, the font files,
/// the font size and the width of the lines. One value, so that what a
/// shaper is told can grow without changing the Shaper protocol.
///
public struct ShapingRequest {
    /// The text.
    public let text: String
    /// The language of the text, such as "hi" or "km", or nil.
    public let language: String?
    /// The bytes of the font files: the first for the text, the others, in
    /// order, for the characters it does not have.
    public let fontFiles: [[UInt8]]
    /// The font size, in points.
    public let fontSize: Float
    /// The width of the lines, in points.
    public let width: Float

    public init(_ text: String, _ language: String?, _ fontFiles: [[UInt8]], _ fontSize: Float, _ width: Float) {
        self.text = text
        self.language = language
        self.fontFiles = fontFiles
        self.fontSize = fontSize
        self.width = width
    }
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
    public let fontIndexes: [Int]
    /// The glyph numbers, in their fonts.
    public let glyphs: [Int]
    /// The x and the y of each glyph, in points, from the left end of the
    /// line at its baseline, y growing down: x0, y0, x1, y1 and so on.
    public let positions: [Float]
    /// The width of the line, in points, without the white space at its
    /// end: what is centered, aligned to the right or justified.
    public let width: Float

    public init(_ start: Int, _ end: Int, _ fontIndexes: [Int], _ glyphs: [Int], _ positions: [Float],
            _ width: Float) {
        precondition(fontIndexes.count == glyphs.count && positions.count == 2 * glyphs.count,
                "A shaped line needs a font and an x and a y for each glyph.")
        self.start = start
        self.end = end
        self.fontIndexes = fontIndexes
        self.glyphs = glyphs
        self.positions = positions
        self.width = width
    }
}   // End of Shaper.swift
