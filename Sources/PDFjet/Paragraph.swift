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
    var alignment = Alignment.LEFT
    // True after setTextAlignment. Otherwise the alignment of the text column applies.
    var explicitAlignment = false

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
    /// Sets the alignment of the text in this paragraph. A paragraph with no
    /// alignment set takes the alignment of the text column it is drawn in.
    ///
    /// - Parameter alignment: the alignment code: Alignment.LEFT, Alignment.RIGHT, Alignment.CENTER or Alignment.JUSTIFY.
    /// - Returns: this paragraph.
    ///
    @discardableResult
    public func setTextAlignment(_ alignment: Alignment) -> Paragraph {
        self.alignment = alignment
        self.explicitAlignment = true
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
    public func setTextColor(_ color: Int32) -> Paragraph {
        for line in lines {
            line.setTextColor(color)
        }
        return self
    }

    /// Sets the text color of all lines in this paragraph from an array of red, green and blue values between 0.0 and 1.0.
    @discardableResult
    public func setTextColor(_ rgbColor: [Float]) -> Paragraph {
        for line in lines {
            line.setTextColor(rgbColor)
        }
        return self
    }

    /// Sets the word highlight colors of all lines in this paragraph.
    @discardableResult
    public func setHighlightColors(_ colorMap: [String: Int32]) -> Paragraph {
        for line in lines {
            line.setHighlightColors(colorMap)
        }
        return self
    }

    /// Reads a text file and returns its paragraphs. An empty line separates the paragraphs.
    public static func paragraphsFromFile(_ f1: Font, _ filePath: String) throws -> [Paragraph] {
        var paragraphs = [Paragraph]()
        let contents = try Content.ofTextFile(filePath)
        var paragraph = Paragraph()
        var textLine = TextLine(f1)
        var sb = String()
        let scalars = Array(contents.unicodeScalars)
        var i = 0
        while i < scalars.count {
            let ch = scalars[i]
            // We need at least one character after the \n\n to begin new paragraph!
            if i < (scalars.count - 2) &&
                    ch == "\n" && scalars[i + 1] == "\n" {
                textLine.setText(sb)
                paragraph.add(textLine)
                paragraphs.append(paragraph)
                paragraph = Paragraph()
                textLine = TextLine(f1)
                sb = ""
                i += 1
            } else {
                sb.append(String(ch))
            }
            i += 1
        }
        if !sb.isEmpty {
            textLine.setText(sb)
            paragraph.add(textLine)
            paragraphs.append(paragraph)
        }
        return paragraphs
    }
}   // End of Paragraph.swift
