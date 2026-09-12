/**
 * Paragraph.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create paragraph objects.
/// See the TextColumn class for more information.
///
public class Paragraph {
    /// The x coordinate where the text of this paragraph starts.
    public var xText: Float = 0.0
    /// The baseline y coordinate of the first line of this paragraph.
    public var yText: Float = 0.0
    /// The x coordinate of the top left corner of this paragraph.
    public var x1: Float = 0.0
    /// The y coordinate of the top left corner of this paragraph.
    public var y1: Float = 0.0
    /// The x coordinate where the last line of this paragraph ends.
    public var x2: Float = 0.0
    /// The y coordinate of the bottom of the last line of this paragraph.
    public var y2: Float = 0.0
    var lines = [TextLine]()
    var alignment: UInt32 = Align.LEFT

    ///
    /// Constructor for creating paragraph objects.
    ///
    public init() {
    }

    /// Creates a paragraph with the specified text line.
    public init(_ text: TextLine) {
        lines.append(text)
    }

    ///
    /// Adds a text line to this paragraph.
    ///
    /// - Parameter text: the text line to add to this paragraph.
    /// - Returns: this paragraph.
    ///
    @discardableResult
    public func add(_ text: TextLine) -> Paragraph {
        lines.append(text)
        return self
    }

    ///
    /// Sets the alignment of the text in this paragraph.
    ///
    /// - Parameter alignment: the alignment code: Align.LEFT, Align.RIGHT, Align.CENTER or Align.JUSTIFY.
    /// - Returns: this paragraph.
    ///
    @discardableResult
    public func setAlignment(_ alignment: UInt32) -> Paragraph {
        self.alignment = alignment
        return self
    }

    /// Returns the text lines of this paragraph.
    public func getTextLines() -> [TextLine] {
        return lines
    }

    /// Returns true if the first line of this paragraph starts with the specified token.
    public func startsWith(_ token: String) -> Bool {
        return lines[0].getText()!.hasPrefix(token)
    }

    /// Sets the text color of all lines in this paragraph as a 0xRRGGBB value.
    @discardableResult
    public func setColor(_ color: Int32) -> Paragraph {
        for line in lines {
            line.setTextColor(color)
        }
        return self
    }

    /// Sets the word highlight colors of all lines in this paragraph.
    @discardableResult
    public func setColorMap(_ colorMap: [String: Int32]) -> Paragraph {
        for line in lines {
            line.setColorMap(colorMap)
        }
        return self
    }
}   // End of Paragraph.swift
