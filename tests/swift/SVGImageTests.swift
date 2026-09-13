/**
 * SVGImageTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct SVGImageTests {
    private func image(_ svg: String) throws -> SVGImage {
        return try SVGImage(stream: InputStream(data: Data(svg.utf8)))
    }

    private func draw(_ svg: String, sourceLocation: SourceLocation = #_sourceLocation) throws -> String {
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
        let svgImage = try image(svg)
        _ = svgImage.setLocation(0, 0)
        TestSupport.expectXY(svgImage.getWidth(), svgImage.getHeight(), svgImage.drawOn(page), sourceLocation: sourceLocation)
        return TestSupport.content(page)
    }

    @Test func readsTheSizeFromTheAttributes() throws {
        let svgImage = try image("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>")
        #expect(svgImage.getWidth() == 100)
        #expect(svgImage.getHeight() == 50)
    }

    @Test func keepsTheLastNumberOfAPathThatIsNotClosed() throws {
        let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>")
        #expect(content.contains("10 782 m\n90 752 l\n"), "\(content)")
    }

    @Test func singleQuotesAndLineBreaksBetweenAttributesParseLikeDoubleQuotes() throws {
        let doubleQuotes = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"red\"/></svg>")
        let singleQuotes = try draw("<svg width='100' height='50'><path\n fill='red'\n d='M10 10 L90 40 L10 40 Z'/></svg>")
        #expect(doubleQuotes == singleQuotes)
        #expect(doubleQuotes.hasPrefix("1 0 0 rg\n"), "\(doubleQuotes)")
    }

    @Test func ellipticalArcsBecomeCubicCurves() throws {
        let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 A 20 20 0 0 1 50 10\"/></svg>")
        // A half circle over the top, from (10, 10) to (50, 10) in SVG coordinates.
        #expect(content.contains("10 793.05 18.95 802 30 802 c\n41.05 802 50 793.05 50 782 c\n"), "\(content)")
    }

    @Test func aStrokeOnlyPathIsStroked() throws {
        let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"none\" stroke=\"red\"/></svg>")
        #expect(content.contains("1 0 0 RG\n"), "\(content)")
        #expect(content.hasSuffix("s\n"), "\(content)")
    }

    @Test func anOpenPathWithAStrokeIsStroked() throws {
        let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\" fill=\"none\" stroke=\"red\"/></svg>")
        #expect(content.hasSuffix("10 782 m\n90 752 l\nS\n"), "\(content)")
    }

    @Test func aClosedSubpathIsClosedAndAnOpenOneStrokedAtTheEnd() throws {
        let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 Z M20 20 L30 30\" fill=\"none\" stroke=\"red\"/></svg>")
        #expect(content.hasSuffix("10 782 m\n90 752 l\ns\n20 772 m\n30 762 l\nS\n"), "\(content)")
    }

    @Test func fillNoneWithoutAStrokeDrawsNothing() throws {
        #expect(try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"none\"/></svg>") == "")
        #expect(try draw("<svg width=\"100\" height=\"50\" fill=\"none\"><path d=\"M10 10 L90 40 L10 40 Z\"/></svg>") == "")
    }

    @Test func noneOnThePathWinsOverTheColorsOfTheSvgElement() throws {
        let content = try draw("<svg width=\"100\" height=\"50\" fill=\"red\" stroke=\"green\">"
                + "<path d=\"M10 10 L90 40 L10 40 Z\" fill=\"none\" stroke=\"blue\"/>"
                + "<path d=\"M20 20 L80 30 L20 30 Z\" stroke=\"none\"/></svg>")
        #expect(content.contains("0 0 1 RG\n"), "\(content)")
        #expect(!content.contains("0 0.5 0 RG"), "\(content)")
        // The second path takes the red fill of the svg element and has no stroke.
        #expect(content.contains("1 0 0 rg\n"), "\(content)")
        #expect(content.components(separatedBy: "\nf\n").count - 1 == 1, "\(content)")
        #expect(content.components(separatedBy: "\ns\n").count - 1 == 1, "\(content)")
    }

    @Test func aPathWithoutColorsOrWithAnUnknownFillIsFilledBlack() throws {
        let unset = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\"/></svg>")
        #expect(unset.hasPrefix("0 0 0 rg\n") && unset.hasSuffix("\nf\n"), "\(unset)")
        let gradient = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"url(#g)\"/></svg>")
        #expect(gradient == unset)
    }
}
