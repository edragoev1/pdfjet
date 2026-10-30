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

    // What the image draws, without the miter limit that is set around an
    // image that strokes, which theMiterLimitOfAStrokeIsWritten checks.
    static func withoutMiterLimit(_ content: String) -> String {
        return content.hasPrefix("q\n4 M\n") ? String(content.dropFirst(6).dropLast(2)) : content
    }

    private func draw(_ svg: String, sourceLocation: SourceLocation = #_sourceLocation) throws -> String {
        return SVGImageTests.withoutMiterLimit(try drawAsItIs(svg, sourceLocation: sourceLocation))
    }

    private func drawAsItIs(_ svg: String, sourceLocation: SourceLocation = #_sourceLocation) throws -> String {
        let page = TestSupport.newPage()
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

    @Test func scalingScalesTheSizeWithThePaths() throws {
        let svgImage = try image("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>")
        svgImage.scaleBy(0.5)
        #expect(svgImage.getWidth() == 50)
        #expect(svgImage.getHeight() == 25)
        let page = TestSupport.newPage()
        _ = svgImage.setLocation(0, 0)
        TestSupport.expectXY(50, 25, svgImage.drawOn(page))
        let content = TestSupport.content(page)
        #expect(content.contains("5 787 m\n45 772 l\n"), "\(content)")
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
        #expect(content.hasSuffix("h\nS\n"), "\(content)")
    }

    @Test func anOpenPathWithAStrokeIsStroked() throws {
        let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\" fill=\"none\" stroke=\"red\"/></svg>")
        #expect(content.hasSuffix("10 782 m\n90 752 l\nS\n"), "\(content)")
    }

    @Test func aClosedSubpathIsClosedAndThePathStrokedOnceAtTheEnd() throws {
        let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 Z M20 20 L30 30\" fill=\"none\" stroke=\"red\"/></svg>")
        #expect(content.hasSuffix("10 782 m\n90 752 l\nh\n20 772 m\n30 762 l\nS\n"), "\(content)")
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
        #expect(content.components(separatedBy: "\nh\nS\n").count - 1 == 1, "\(content)")
    }

    @Test func aPathWithoutColorsIsFilledBlackAndOneOfAnUnknownURLIsNotFilled() throws {
        let unset = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\"/></svg>")
        #expect(unset.hasPrefix("0 0 0 rg\n") && unset.hasSuffix("\nf\n"), "\(unset)")
        // A url of an element that is not there is none, as a browser draws it.
        let unknown = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" fill=\"url(#g)\"/></svg>")
        #expect(unknown == "")
    }

    @Test func aGradientIsDrawnInTheMeanColorOfItsStops() throws {
        let stops = "<stop offset=\"0\" stop-color=\"#ff0000\"/><stop offset=\"1\" style=\"stop-color: blue\"/>"
        let content = try draw("<svg width=\"100\" height=\"50\"><defs><linearGradient id=\"g\">" + stops
                + "</linearGradient><radialGradient id=\"h\" href=\"#g\"/></defs>"
                + "<path d=\"M10 10 L90 40 L10 40 Z\" fill=\"url(#g)\"/><path d=\"M10 10 L90 40\" stroke=\"url('#h')\"/></svg>")
        // 255/2 rounds half up; the second gradient takes the stops of the first.
        #expect(content.hasPrefix("0.5 0 0.5 rg\n"), "\(content)")
        #expect(content.contains("0.5 0 0.5 RG\n"), "\(content)")
    }
    // The numbers of path data are written as SVG 1.1 section 8.3.9 gives them:
    // a sign, digits, a point and an exponent, and a sign or a point starts the
    // next number where no space or comma separates them.

    @Test func readsTheNumbersOfPathDataAsSVGWritesThem() throws {
        // Every path draws the line from 10, 10 to 90, 40, written another way.
        let paths = [
            "M10 10 L90 40",
            "M10,10L90,40",
            "M 1e1 1e1 L 9e1 4e1",
            "M 1000e-2 1000e-2 L 9000e-2 4000e-2",
            "M 1000E-2 1000E-2 L 9000E-2 4000E-2",
            "M+10+10L+90+40",
            "M 10.0 10.0 L 90.0 40.0",
            "M 10 10 L 9e+1 4e+1",
            "M10 10\nL90 40",      // Path data is often written over several lines
            "M10\t10\tL90\t40",
            "M10 10\r\nL90 40",
        ]
        for data in paths {
            let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>")
            #expect(content.contains("10 782 m\n90 752 l\n"), "\(data) draws \(content)")
        }
    }

    @Test func pathDataThatStartsWithANumberDrawsNothing() throws {
        // Path data starts with a moveto; the numbers before the first command
        // belong to no operation, and are left out rather than read as one.
        for data in ["10 10 L90 40", ".5.5L90 40", "-10L90 40"] {
            let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>")
            #expect(!content.contains(" m\n"), "\(data) draws \(content)")
        }
    }
    @Test func aPathWithoutDataDrawsNothing() throws {
        // A path element without a d attribute is one the other ports threw on.
        let content = try draw("<svg width=\"100\" height=\"50\"><path/><path d=\"M10 10 L90 40\"/></svg>")
        #expect(content.contains("10 782 m\n90 752 l\n"))
    }

    @Test func pathDataThatDoesNotStartWithAMovetoDrawsNothing() throws {
        // The first command of the path data needs a current point, which this
        // port left unset: it trapped. Path data starts with a moveto, and SVG
        // 1.1 section 8.3.2 draws data in error up to where it is in error,
        // which here is nothing; a PDF has no current point to draw a line
        // from either.
        let paths = [
            "L90 40", "H90", "V40", "Q10 10 90 40", "T90 40",
            "C1 1 2 2 90 40", "S1 1 90 40", "A5 5 0 0 1 90 40", "l90 40", "Z L90 40",
        ]
        for data in paths {
            let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/>"
                    + "<path d=\"M1 1 L2 2\" fill=\"none\" stroke=\"red\"/></svg>")
            #expect(content == "1 0 0 RG\n1 w\n1 791 m\n2 790 l\nS\n", "\(data) draws \(content)")
        }
    }

    @Test func theFlagsOfAnArcAreOneCharacterAndNeedNoSeparator() throws {
        // SVG 1.1 section 8.3.9 writes each flag of an elliptical arc as a
        // single character, so that nothing has to separate it from what
        // follows. Every path here draws the two half circles of the first.
        let separated = try draw("<svg width=\"100\" height=\"50\">" +
                "<path d=\"M10 10 A 20 20 0 0 1 50 10 A 20 20 0 1 0 90 10\"/></svg>")
        let paths = [
            "M10 10 A20 20 0 01 50 10 A20 20 0 10 90 10",
            "M10 10 A20 20 0 0150 10 A20 20 0 1090 10",
            "M10 10A20 20 0 0150,10A20 20 0 1090,10",
            "M10 10 a20 20 0 0140 0 a20 20 0 1040 0",
            "M10 10 A20,20,0,0,1,50,10 A20,20,0,1,0,90,10",
        ]
        for data in paths {
            let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>")
            #expect(content == separated, "\(data) draws \(content)")
        }
    }

    @Test func onlyTheFlagsOfAnArcAreReadOneCharacterAtATime() throws {
        // The radii, the rotation and the end point of an arc are numbers like
        // any other, and the digits of every other command are too.
        let content = try draw("<svg width=\"100\" height=\"50\">" +
                "<path d=\"M10 10 A10 10 0 0 1 10 40 L10 10 A10 10 0 0 0 10 40\"/></svg>")
        let other = try draw("<svg width=\"100\" height=\"50\">" +
                "<path d=\"M10 10 A10 10 0 01 10 40 L10 10 A10 10 0 00 10 40\"/></svg>")
        #expect(content == other)
        #expect(content.contains("10 782 m\n") && content.contains(" c\n"), "\(content)")
    }

    @Test func arcRadiiTooSmallForTheEndPointsAreScaledToFitThem() throws {
        // SVG 1.1 section F.6.6 scales up radii too small to reach the end
        // points until the ellipse just does: the arc is then half of it,
        // whichever way the large arc flag points, and the same as the arc of
        // the fitting radii.
        let fitted = try draw("<svg width=\"200\" height=\"200\"><path d=\"M0 0 A50 50 0 0 1 100 0\"/></svg>")
        #expect(curves(fitted) == 2, "\(fitted)")
        let paths = [
            "M0 0 A10 10 0 0 1 100 0",
            "M0 0 A1 1 0 0 1 100 0",
            "M0 0 A10 10 0 1 1 100 0",
        ]
        for data in paths {
            let content = try draw("<svg width=\"200\" height=\"200\"><path d=\"" + data + "\"/></svg>")
            #expect(content == fitted, "\(data) draws \(content)")
        }
    }

    @Test func anArcOfWholeQuarterTurnsIsDrawnInThatManyCurves() throws {
        // An arc is drawn in pieces of at most a quarter turn. One of exactly a
        // quarter, a half or a whole turn is not split once more for the last
        // bit of the sweep, which the four ports do not compute alike.
        let arcs = [
            ("M0 0 A 120 120 120 0 0 120 120", 1),  // A quarter turn, of the fuzz corpus
            ("M10 10 A 20 20 0 0 1 50 10", 2),
            ("M0 0 A50 50 0 0 1 100 0", 2),
        ]
        for (data, count) in arcs {
            let content = try draw("<svg width=\"200\" height=\"200\"><path d=\"" + data + "\"/></svg>")
            #expect(curves(content) == count, "\(data) draws \(content)")
        }
    }

    // An arc whose radii, scaled up to reach the end point, are past what a
    // Float holds, or whose end points are that far apart, is left out, as
    // the other ports leave it: converting its NaN sweep to an Int trapped
    // (review of 11 October 2026, #36).
    @Test func anArcPastTheRangeOfAFloatIsLeftOut() throws {
        for data in ["M 0 0 A 1e-30 1e-30 0 0 1 1e30 0", "M -3e38 0 A 1 1 0 0 1 3e38 0"] {
            let content = try draw("<svg width=\"200\" height=\"200\"><path d=\"" + data + " L 10 10\"/></svg>")
            #expect(curves(content) == 0, "\(data) draws \(content)")
        }
    }

    // A "\r\n" in path data, one Character in Swift, separates the numbers
    // as a "\n" does (review of 11 October 2026, #22).
    @Test func aCarriageReturnAndLineFeedIsWhiteSpace() throws {
        let args = SVG.getOperations("M0 0\r\nL10 10").map { "\($0.cmd)\($0.args)" }
        #expect(args == ["M[\"0\", \"0\"]", "L[\"10\", \"10\"]"], "\(args)")
    }

    @Test func aSizeWithAUnitIsReadInPoints() throws {
        // A number is in the user unit of the file, which PDFjet draws as a
        // point, and so is a number in px; the units of length are converted.
        let sizes: [(String, Float)] = [
            ("48", 48.0), ("48px", 48.0), ("48pt", 48.0), ("1in", 72.0), ("1pc", 12.0),
            ("210mm", 595.2756), ("21cm", 595.2756), (" 48 ", 48.0),
        ]
        for (size, points) in sizes {
            let svg = try image("<svg width=\"" + size + "\" height=\"" + size + "\"/>")
            #expect(abs(svg.getWidth() - points) < 0.001, "\(size)")
            #expect(abs(svg.getHeight() - points) < 0.001, "\(size)")
        }
    }

    @Test func aViewBoxInLowerCaseIsRead() throws {
        // A file saved from a web page may write viewbox, which an HTML page's
        // parser reads as viewBox; viewBox wins when a file has both.
        let svgs = [
            "<svg width=\"50\" height=\"25\" viewbox=\"10 10 100 50\"><path d=\"M20 20 L110 60\"/></svg>",
            "<svg width=\"50\" height=\"25\" viewBox=\"10 10 100 50\" viewbox=\"0 0 200 100\"><path d=\"M20 20 L110 60\"/></svg>",
        ]
        for svg in svgs {
            #expect(try draw(svg).contains("5 787 m\n50 767 l\n"), "\(svg)")
        }
    }

    @Test func aSizeThatCannotBeReadIsTheSizeOfTheViewBox() throws {
        // Scaling the paths by a width of zero would draw every one of them
        // at the origin, so a size in a unit PDFjet cannot read, a percentage
        // among them, and a size the file does not give, leave the drawing
        // 1:1 with its viewBox.
        let svgs = [
            "<svg viewBox=\"0 0 100 50\"><path d=\"M10 10 L90 40\"/></svg>",
            "<svg width=\"100%\" height=\"100%\" viewBox=\"0 0 100 50\"><path d=\"M10 10 L90 40\"/></svg>",
            "<svg width=\"10em\" height=\"5em\" viewBox=\"0 0 100 50\"><path d=\"M10 10 L90 40\"/></svg>",
        ]
        for svg in svgs {
            #expect(try image(svg).getWidth() == 100, "\(svg)")
            #expect(try image(svg).getHeight() == 50, "\(svg)")
            #expect(try draw(svg).contains("10 782 m\n90 752 l\n"), "\(svg)")
        }
    }

    @Test func aViewBoxThatIsNotFourNumbersOrHasNoSizeFails() throws {
        let boxes = [
            ("0 0", "four numbers are needed."),
            ("0 0 10 10 10", "four numbers are needed."),
            ("0 0 ten 10", "four numbers are needed."),
            ("0 0 0 10", "its width and height have to be above zero."),
            ("0 0 10 0", "its width and height have to be above zero."),
        ]
        for (box, message) in boxes {
            let error = #expect(throws: (any Error).self) {
                _ = try image("<svg width=\"10\" height=\"10\" viewBox=\"" + box + "\"/>")
            }
            #expect(TestSupport.message(error) == "Invalid SVG viewBox \"" + box + "\": " + message)
        }
    }

    // What SVGImage draws of the SVG files of drawing programs: groups and
    // what they give their paths, transforms, the basic shapes, style
    // attributes and classes, fill rules, opacity, line caps and joins.

    @Test func aGroupGivesItsPathsItsColorsAndWidthUnlessTheyHaveTheirOwn() throws {
        let content = try draw("<svg width=\"100\" height=\"100\"><g fill=\"red\" stroke=\"blue\" stroke-width=\"2\">"
                + "<path d=\"M10 10 H90 V90 Z\"/><path d=\"M10 10 H50 V50 Z\" fill=\"green\" stroke-width=\"4\"/></g></svg>")
        let want = "1 0 0 rg\n10 782 m\n90 782 l\n90 702 l\nf\n0 0 1 RG\n2 w\n10 782 m\n90 782 l\n90 702 l\nh\nS\n"
                + "0 0.5 0 rg\n10 782 m\n50 782 l\n50 742 l\nf\n4 w\n10 782 m\n50 782 l\n50 742 l\nh\nS\n"
        #expect(content == want, "\(content)")
    }

    @Test func aPathIsFilledBlackUnlessItsFillIsNoneAndStrokedOneUnitWide() throws {
        // As SVG draws them: a stroke does not take the fill away, and a
        // stroke width of 0 draws no stroke, where a PDF would draw the
        // thinnest line.
        var content = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" stroke=\"red\"/></svg>")
        #expect(content.hasPrefix("0 0 0 rg\n") && content.contains("1 0 0 RG\n1 w\n"), "\(content)")
        content = try draw("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40 L10 40 Z\" stroke=\"red\" stroke-width=\"0\"/></svg>")
        #expect(!content.contains("RG") && content.hasSuffix("\nf\n"), "\(content)")
    }

    @Test func transformsOfGroupsAndPathsAreAppliedInTurn() throws {
        // The path is scaled, then moved by the group; its stroke is scaled too.
        var content = try draw("<svg width=\"100\" height=\"100\"><g transform=\"translate(10 20)\">"
                + "<path d=\"M0 0 L10 0\" transform=\"scale(2)\" stroke=\"red\" fill=\"none\"/></g></svg>")
        #expect(content == "1 0 0 RG\n2 w\n10 772 m\n30 772 l\nS\n", "\(content)")
        let transforms = [
            ("rotate(90)", "0 792 m\n0 782 l\n"),
            ("rotate(90 10 10)", "20 792 m\n20 782 l\n"),
            ("matrix(1 0 0 1 5 6)", "5 786 m\n15 786 l\n"),
            ("translate(5,6)", "5 786 m\n15 786 l\n"),
            ("translate(5)", "5 792 m\n15 792 l\n"),
            ("scale(2 3)", "0 792 m\n20 792 l\n"),
            ("skewY(45)", "0 792 m\n10 782 l\n"),
            ("translate(10) scale(2)", "10 792 m\n30 792 l\n"),
            ("scale(2) translate(10)", "20 792 m\n40 792 l\n"),
            ("translate(10)\n,rotate(90)", "10 792 m\n10 782 l\n"),
            ("translate(10) rotate", "0 792 m\n10 792 l\n"),    // Cannot be read: none
            ("translate(10) unknown(1 2)", "0 792 m\n10 792 l\n"),
            ("translate(10) scale(1 2 3)", "0 792 m\n10 792 l\n"),
            ("translate(1e1) scale(.5.5)", "10 792 m\n15 792 l\n"),
            ("translate(-5-5) scale(+1+1)", "-5 797 m\n5 797 l\n"),
        ]
        for (transform, want) in transforms {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0\" transform=\"" + transform + "\"/></svg>")
            #expect(content.contains(want), "\(transform) draws \(content), not \(want)")
        }
        // skewX leaves the line along the x axis as it is, and moves the point below.
        content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L0 10\" transform=\"skewX(45)\"/></svg>")
        #expect(content.contains("0 792 m\n10 782 l\n"), "skewX draws \(content)")
    }

    @Test func aShapeWithATransformThatFlattensItIsNotDrawn() throws {
        let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" transform=\"scale(0)\"/></svg>")
        #expect(content == "", "\(content)")
    }

    @Test func drawsTheBasicShapes() throws {
        let shapes = [
            ("<rect x=\"10\" y=\"20\" width=\"30\" height=\"40\"/>",
                "0 0 0 rg\n10 772 m\n40 772 l\n40 732 l\n10 732 l\nf\n"),
            ("<rect x=\"10\" y=\"20\" width=\"30\" height=\"40\" rx=\"5\"/>",
                "0 0 0 rg\n15 772 m\n35 772 l\n"
                + "37.76 772 40 769.76 40 767 c\n40 737 l\n40 734.24 37.76 732 35 732 c\n15 732 l\n"
                + "12.24 732 10 734.24 10 737 c\n10 767 l\n10 769.76 12.24 772 15 772 c\nf\n"),
            // A radius larger than half the side is half the side, and one
            // that is not given is the other one.
            ("<rect width=\"10\" height=\"10\" ry=\"20\"/>",
                "0 0 0 rg\n5 792 m\n5 792 l\n"
                + "7.76 792 10 789.76 10 787 c\n10 787 l\n10 784.24 7.76 782 5 782 c\n5 782 l\n"
                + "2.24 782 0 784.24 0 787 c\n0 787 l\n0 789.76 2.24 792 5 792 c\nf\n"),
            ("<circle cx=\"50\" cy=\"50\" r=\"10\"/>",
                "0 0 0 rg\n60 742 m\n60 736.48 55.52 732 50 732 c\n"
                + "44.48 732 40 736.48 40 742 c\n40 747.52 44.48 752 50 752 c\n55.52 752 60 747.52 60 742 c\nf\n"),
            ("<ellipse cx=\"50\" cy=\"50\" rx=\"20\" ry=\"10\"/>",
                "0 0 0 rg\n70 742 m\n70 736.48 61.05 732 50 732 c\n"
                + "38.95 732 30 736.48 30 742 c\n30 747.52 38.95 752 50 752 c\n61.05 752 70 747.52 70 742 c\nf\n"),
            ("<line x1=\"10\" y1=\"20\" x2=\"30\" y2=\"40\" stroke=\"red\"/>",
                "1 0 0 RG\n1 w\n10 772 m\n30 752 l\nS\n"),
            ("<polyline points=\"10,10 20,20 10,20\" fill=\"none\" stroke=\"red\"/>",
                "1 0 0 RG\n1 w\n10 782 m\n20 772 l\n10 772 l\nS\n"),
            ("<polygon points=\"10 10 20 20 10 20 5\" fill=\"none\" stroke=\"red\"/>",
                "1 0 0 RG\n1 w\n10 782 m\n20 772 l\n10 772 l\nh\nS\n"),
            // Shapes of no size draw nothing.
            ("<rect width=\"0\" height=\"10\"/><circle r=\"0\"/><ellipse rx=\"5\"/><polygon points=\"1 2\"/><line/>", ""),
        ]
        for (shape, want) in shapes {
            let content = try draw("<svg width=\"100\" height=\"100\">" + shape + "</svg>")
            #expect(content == want, "\(shape) draws \(content), not \(want)")
        }
    }

    @Test func styleAttributesWinOverClassesWhichWinOverAttributes() throws {
        let svg = "<svg width=\"100\" height=\"100\"><defs><style>"
                + "/* the {colors} */ .red{fill:#ff0000} .blue, .other { fill: blue !important; stroke: red }"
                + "g .green{fill:green} .green:hover{fill:green}</style></defs>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" fill=\"yellow\" class=\"red\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" class=\"blue red\" style=\"stroke: none\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" class=\"red blue\" style=\"fill:yellow;stroke-width:3\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" class=\"green\"/></svg>"
        let content = try draw(svg)
        let fills = content.components(separatedBy: "\n").filter {
            $0.hasSuffix(" rg") || $0.hasSuffix(" RG") || $0.hasSuffix(" w")
        }
        // The rules are applied in the order of the style sheet, whatever the
        // order of the classes; the selectors that are not a class alone are
        // left out.
        let want = "1 0 0 rg|0 0 1 rg|1 1 0 rg|1 0 0 RG|3 w|0 0 0 rg"
        #expect(fills.joined(separator: "|") == want, "\(content)")
    }

    @Test func colorsAreHexadecimalRGBNamesOrTheCurrentColor() throws {
        let fills = [
            ("fill=\"#F00\"", "1 0 0 rg"),
            ("fill=\"rgb(255, 0, 0)\"", "1 0 0 rg"),
            ("fill=\"rgb(100%,0%,0%)\"", "1 0 0 rg"),
            ("fill=\"RGBA(255 0 0 / 1)\"", "1 0 0 rg"),
            ("fill=\"Red\"", "1 0 0 rg"),
            ("fill=\"currentColor\" color=\"red\"", "1 0 0 rg"),
            ("fill=\"currentColor\"", "0 0 0 rg"),
            ("fill=\"url(#gradient) red\"", "1 0 0 rg"),
            ("fill=\"unknown\"", "0 0 0 rg"),
            ("fill=\"rgb(nan, 0, 0)\"", "0 0 0 rg"),   // Not a number: as if not given
            ("fill=\"rgb(inf, 0, 0)\"", "0 0 0 rg"),
            ("style=\"fill: currentColor; color: #0000ff\"", "0 0 1 rg"),
        ]
        for (fill, want) in fills {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" " + fill + "/></svg>")
            #expect(content.hasPrefix(want + "\n"), "\(fill) draws \(content)")
        }
        // The current color is the color where the fill is used, not where it is set.
        let content = try draw("<svg width=\"100\" height=\"100\" fill=\"currentColor\"><g color=\"red\">"
                + "<path d=\"M0 0 L10 0 L10 10 Z\"/></g></svg>")
        #expect(content.hasPrefix("1 0 0 rg\n"), "\(content)")
        #expect(throws: (any Error).self) {
            _ = try image("<svg><path d=\"M0 0 L1 1\" fill=\"#zzzzzz\"/></svg>")
        }
    }

    @Test func theEvenOddRuleFillsWithFStar() throws {
        let content = try draw("<svg width=\"100\" height=\"100\" style=\"fill-rule:evenodd\">"
                + "<path d=\"M0 0 H30 V30 H0 Z M10 10 H20 V20 H10 Z\"/><path d=\"M0 0 H30 V30 Z\" fill-rule=\"nonzero\"/></svg>")
        #expect(content.components(separatedBy: "\nf*\n").count - 1 == 1 && content.hasSuffix("\nf\n"), "\(content)")
    }

    @Test func opacityIsSetInAGraphicsStateOfThePathsOwn() throws {
        let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" opacity=\"0.5\"/>"
                + "<path d=\"M0 0 L10 0 L10 10 Z\"/></svg>")
        #expect(content == "q\n/GS1 gs\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\nQ\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n",
                "\(content)")
        // The opacity of a group multiplies those of its paths, and a fill or
        // a stroke of no opacity is not drawn.
        let svgImage = try image("<svg width=\"100\" height=\"100\"><g opacity=\"0.5\"><g style=\"opacity:50%\">"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" fill-opacity=\"0.5\" stroke=\"red\" stroke-opacity=\"0\"/></g></g></svg>")
        let path = svgImage.paths[0]
        #expect(path.fillAlpha == 0.125 && path.strokeAlpha == 0.0 && path.stroke == -1,
                "fill alpha \(path.fillAlpha), stroke alpha \(path.strokeAlpha), stroke \(path.stroke)")
    }

    @Test func lineCapsAndJoinsAreSetInAGraphicsStateOfThePathsOwn() throws {
        let content = try draw("<svg width=\"100\" height=\"100\" stroke-linecap=\"round\" stroke-linejoin=\"bevel\">"
                + "<path d=\"M0 0 L10 0\" stroke=\"red\" fill=\"none\"/><path d=\"M0 0 L10 0 L10 10 Z\"/></svg>")
        #expect(content == "q\n1 J\n2 j\n1 0 0 RG\n1 w\n0 792 m\n10 792 l\nS\nQ\n0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n",
                "\(content)")
    }

    @Test func theContentOfDefsAndOfWhatIsNotDisplayedIsNotDrawn() throws {
        let content = try draw("<svg width=\"100\" height=\"100\">"
                + "<defs><path d=\"M0 0 L10 0 L10 10 Z\"/></defs>"
                + "<clipPath><rect width=\"10\" height=\"10\"/></clipPath>"
                + "<symbol><circle r=\"5\"/></symbol>"
                + "<g display=\"none\"><path d=\"M0 0 L10 0 L10 10 Z\"/></g>"
                + "<g style=\"display:none\"><path d=\"M0 0 L10 0 L10 10 Z\" display=\"inline\"/></g>"
                + "<path d=\"M20 20 L30 20 L30 30 Z\"/></svg>")
        #expect(content == "0 0 0 rg\n20 772 m\n30 772 l\n30 762 l\nf\n", "\(content)")
    }

    @Test func attributesOfOtherNamespacesAreLeftOut() throws {
        let content = try draw("<svg xmlns=\"http://www.w3.org/2000/svg\" xmlns:x=\"urn:x\" width=\"100\" height=\"100\">"
                + "<path d=\"M0 0 L10 0 L10 10 Z\" x:fill=\"red\" x:transform=\"scale(2)\"/></svg>")
        #expect(content == "0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", "\(content)")
    }

    @Test func theViewBoxScalesTheStrokesAndKeepsItsProportions() throws {
        // The viewBox is scaled by 10 to fit the height, and centered across.
        let content = try draw("<svg width=\"200\" height=\"100\" viewBox=\"0 0 10 10\">"
                + "<path d=\"M0 0 L10 10\" stroke=\"red\" fill=\"none\"/></svg>")
        #expect(content == "1 0 0 RG\n10 w\n50 792 m\n150 692 l\nS\n", "\(content)")
        let aspects = [
            ("xMinYMin", "0 792 m\n100 692 l\n"),
            ("xMaxYMax meet", "100 792 m\n200 692 l\n"),
            ("defer xMidYMid", "50 792 m\n150 692 l\n"),
            ("xMidYMin slice", "20 w\n0 792 m\n200 592 l\n"),    // Covers it, from the top
            ("none", "0 792 m\n200 692 l\n"),
        ]
        for (aspect, want) in aspects {
            let content = try draw("<svg width=\"200\" height=\"100\" viewBox=\"0 0 10 10\" preserveAspectRatio=\""
                    + aspect + "\"><path d=\"M0 0 L10 10\" stroke=\"red\" fill=\"none\"/></svg>")
            #expect(content.contains(want), "\(aspect) draws \(content), not \(want)")
        }
    }

    @Test func aSizeThatIsNotGivenIsInTheProportionsOfTheViewBox() throws {
        let sizes: [(String, Float, Float)] = [
            ("<svg width=\"48\" viewBox=\"0 0 960 480\"/>", 48, 24),
            ("<svg height=\"24\" viewBox=\"0 0 960 480\"/>", 48, 24),
            ("<svg width=\"10%\" viewBox=\"0 0 960 480\"/>", 960, 480),
            ("<svg width=\"100\" height=\"50\" viewBox=\"0 0 960 480\"/>", 100, 50),
        ]
        for (svg, width, height) in sizes {
            let svgImage = try image(svg)
            #expect(svgImage.getWidth() == width && svgImage.getHeight() == height,
                    "\(svg): size \(svgImage.getWidth()) x \(svgImage.getHeight())")
        }
    }

    @Test func scalingScalesTheStrokes() throws {
        let svgImage = try image("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\" stroke=\"red\" stroke-width=\"4\"/></svg>")
        svgImage.scaleBy(0.5)
        let page = TestSupport.newPage()
        _ = svgImage.setLocation(0, 0)
        svgImage.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("1 0 0 RG\n2 w\n"), "\(content)")
    }

    @Test func valuesThatCannotBeReadAreLeftOut() throws {
        let content = try draw("<svg width=\"100\" height=\"100\" viewBox=\"0,0,100,100\" stroke-width=\"3\">"
                + "<path d=\"M0 0 L10 0\" fill=\"none\" stroke=\"red\" stroke-width=\"calc(1px + 1px)\" opacity=\"half\"/>"
                + "<rect width=\"Inf\" height=\"10\" stroke-width=\"NaN\"/><polygon points=\"0 0 1e999 1 2 2\"/></svg>")
        #expect(content == "1 0 0 RG\n3 w\n0 792 m\n10 792 l\nS\n", "\(content)")
    }

    @Test func aSizeOrATransformTooLargeForAFloatDrawsNothing() throws {
        // A PDF holds numbers below 2^31 and no infinity: a size of inf is no
        // size, and a path a transform takes out of that range is not drawn.
        let svgImage = try image("<svg width=\"inf\" height=\"nan\" viewBox=\"0 0 100 50\"/>")
        #expect(svgImage.getWidth() == 100 && svgImage.getHeight() == 50,
                "size \(svgImage.getWidth()) x \(svgImage.getHeight())")
        let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" transform=\"scale(1e30)\"/>"
                + "<path d=\"M0 0 L10 0\" stroke=\"red\" transform=\"scale(1e20 1)\"/><path d=\"M0 0 L10 0 L10 10 Z\"/></svg>")
        #expect(content == "0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", "\(content)")
    }

    @Test func aSkewOfNinetyDegreesIsNotRead() throws {
        for transform in ["skewX(90)", "skewY(-270)"] {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" transform=\""
                    + transform + "\"/></svg>")
            #expect(content == "0 0 0 rg\n0 792 m\n10 792 l\n10 782 l\nf\n", "\(transform) draws \(content)")
        }
    }

    @Test func drawsTheGroupsTransformsShapesAndStylesOfTheFuzzSeed() throws {
        // The seed of the fuzz target of the Go port, which is drawn without
        // failing.
        let page = TestSupport.newPage()
        let svgImage = try image("<svg width=\"100\" height=\"50\" viewBox=\"0 0 200 100\" preserveAspectRatio=\"xMinYMax slice\">"
                + "<style>.a{fill:rgb(10%,20,30);stroke:currentColor;stroke-width:2px} .b{opacity:.5}</style>"
                + "<g transform=\"translate(10,5) rotate(30 5 5) skewX(10)\" color=\"blue\" class=\"a\" fill-rule=\"evenodd\">"
                + "<rect x=\"1\" y=\"2\" width=\"30\" height=\"20\" rx=\"4\" class=\"b\"/><circle cx=\"50\" cy=\"20\" r=\"8\"/>"
                + "<ellipse cx=\"80\" cy=\"20\" rx=\"10\" ry=\"5\" style=\"fill-opacity:0.3;stroke-linecap:round\"/>"
                + "<line x1=\"0\" y1=\"40\" x2=\"90\" y2=\"45\" stroke-linejoin=\"bevel\"/>"
                + "<polyline points=\"0,50 10,60 20,50\"/><polygon points=\"30 50 40 60 50 50\"/></g>"
                + "<defs><path d=\"M0 0 L5 5\"/></defs><g display=\"none\"><rect width=\"5\" height=\"5\"/></g></svg>")
        _ = svgImage.setLocation(10, 10)
        svgImage.drawOn(page)
        #expect(svgImage.paths.count == 6)
        // Drawn at 0, 0: every shape but the line, which has no inside, is
        // filled with the even-odd rule; the first in a graphics state of its
        // opacity. The slice draws past the size of the image, which cuts it
        // off.
        let content = try draw("<svg width=\"100\" height=\"50\" viewBox=\"0 0 200 100\" preserveAspectRatio=\"xMinYMax slice\">"
                + "<style>.a{fill:rgb(10%,20,30);stroke:currentColor;stroke-width:2px} .b{opacity:.5}</style>"
                + "<g transform=\"translate(10,5) rotate(30 5 5) skewX(10)\" color=\"blue\" class=\"a\" fill-rule=\"evenodd\">"
                + "<rect x=\"1\" y=\"2\" width=\"30\" height=\"20\" rx=\"4\" class=\"b\"/><circle cx=\"50\" cy=\"20\" r=\"8\"/>"
                + "<ellipse cx=\"80\" cy=\"20\" rx=\"10\" ry=\"5\" style=\"fill-opacity:0.3;stroke-linecap:round\"/>"
                + "<line x1=\"0\" y1=\"40\" x2=\"90\" y2=\"45\" stroke-linejoin=\"bevel\"/>"
                + "<polyline points=\"0,50 10,60 20,50\"/><polygon points=\"30 50 40 60 50 50\"/></g>"
                + "<defs><path d=\"M0 0 L5 5\"/></defs><g display=\"none\"><rect width=\"5\" height=\"5\"/></g></svg>")
        #expect(content.components(separatedBy: "\nf*\n").count - 1 == 5, "\(content)")
        #expect(content.hasPrefix("q\n0 792 m\n100 792 l\n100 742 l\n0 742 l\nW\nn\n"
                + "q\n/GS1 gs\n0.1 0.08 0.12 rg\n8.4 788.21 m\n17.93 782.71 l\n"), "\(content)")
    }

    private func curves(_ content: String) -> Int {
        return content.components(separatedBy: " c\n").count - 1
    }

    @Test func readsTheNumbersOfSVG() throws {
        for number in ["5.", ".5", "-.5", "+5", "-0.5e+2", "5E-1", "007"] {
            let svg = "<svg width=\"100\" height=\"100\"><path d=\"M 0 0 L \(number) 5\"/></svg>"
            do {
                _ = try image(svg)
            } catch {
                Issue.record("\(number): \(error)")
            }
        }
    }

    @Test func refusesPathDataWithANumberThatIsNotOne() {
        // The numbers the fuzzers made of path data: refused in words, in the
        // four ports, as Swift already did (found by the fuzz replay, 8
        // October 2026). And the numbers one language reads and the others do
        // not, refused by SVG's grammar in the four (the review of 9 October
        // 2026): Float(_:) reads 0x1p3.
        for number in ["-.", ".", "2.@5", "1d775", "1e40", "-1e40", "0x1p3", "1_0", "1f", "1e"] {
            let svg = "<svg width=\"100\" height=\"100\"><path d=\"M 0 0 L \(number) 5\"/></svg>"
            do {
                _ = try image(svg)
                Issue.record("\(number): no error")
            } catch {
                #expect("\(error)".lowercased().contains("invalid path data"), "\(number): \(error)")
            }
        }
    }

    // A number of an attribute is written in decimal: a hexadecimal one, as
    // 0x1p3, or one with underscores, as 1_000, is none of SVG's, and is not
    // read in any port (review of 17 October 2026)
    @Test func aNumberOfAnAttributeIsWrittenInDecimal() {
        for text in ["0x1p3", "1_000", "0x10", "1f"] {
            #expect(SVGState.parseDouble(text) == nil, "\(text)")
        }
        #expect(SVGState.parseDouble("1e3") == 1000)
        #expect(SVGState.parseDouble("-inf") == -Double.infinity)
    }

    // The text of a chart: text and tspan elements, placed as a browser
    // places them, in the fonts PDFjet carries unless a font is set.

    @Test func drawsATextAtItsXAndY() throws {
        let content = try draw("<svg width=\"200\" height=\"100\"><text x=\"10\" y=\"20\" font-size=\"12\">Hi</text></svg>")
        #expect(content.contains(" 12 Tf\n1 0 0 1 10 772 Tm\n<"), "\(content)")
        #expect(content.contains("> Tj\nET\n"), "\(content)")     // IBM Plex Sans, embedded
        #expect(!content.contains("W\n"), "\(content)")    // Inside the image: nothing is cut off
    }

    @Test func anAnchorMovesTheTextByItsWidthAndDyIsInEm() throws {
        // Hi is 957 thousandths of an em wide in IBM Plex Sans: 11.484 at 12.
        let content = try draw("<svg width=\"200\" height=\"100\"><text x=\"100\" y=\"20\" dy=\"0.5em\" "
                + "font-size=\"12\" text-anchor=\"end\">Hi</text></svg>")
        #expect(content.contains("1 0 0 1 88.52 766 Tm\n"), "\(content)")
    }

    @Test func theTextAndTheTspansAreDrawnInTheOrderOfTheDocument() throws {
        let content = try draw("<svg width=\"200\" height=\"100\"><text x=\"0\" y=\"20\">"
                + " A <tspan font-weight=\"bold\">B</tspan>C<title>not drawn</title></text></svg>")
        // Three runs: "A " (the space at the start left out), the bold B in
        // IBM Plex Sans Bold, and C; the title is not drawn
        #expect(content.components(separatedBy: " Tj\n").count - 1 == 3, "\(content)")
        let fonts = content.components(separatedBy: " 16 Tf\n")
        #expect(fonts.count == 4, "\(content)")
        if fonts.count == 4, let slash = fonts[0].range(of: "/", options: .backwards) {
            let regular = String(fonts[0][slash.lowerBound...])
            #expect(!fonts[1].hasSuffix(regular), "\(content)")
            #expect(fonts[2].hasSuffix(regular), "\(content)")
        }
    }

    @Test func aTspanWithAnXOrAYStartsAChunkAnchoredOnItsOwn() throws {
        let content = try draw("<svg width=\"200\" height=\"100\"><text text-anchor=\"middle\" "
                + "transform=\"translate(100,50)\" font-size=\"10\"><tspan y=\"-0.4em\">Go</tspan>"
                + "<tspan x=\"0\" y=\"0.7em\">12%</tspan></text></svg>")
        // Go is 1255 thousandths of an em, and 12% 2127.
        #expect(content.contains("1 0 0 1 93.72 746 Tm\n"), "\(content)")
        #expect(content.contains("1 0 0 1 89.36 735 Tm\n"), "\(content)")
    }

    @Test func theFontShorthandSetsTheSizeAndTheWeight() throws {
        let content = try draw("<svg width=\"200\" height=\"100\" style=\"font: bold 10px sans-serif\">"
                + "<text y=\"20\">A</text><text y=\"40\" font-weight=\"400\">B</text></svg>")
        #expect(content.components(separatedBy: " 10 Tf\n").count - 1 == 2, "\(content)")
        #expect(content.range(of: "/F")?.lowerBound != content.range(of: "/F", options: .backwards)?.lowerBound,
                "\(content)")
    }

    @Test func aCoreFontDrawsTheMinusSignAsAHyphenMinus() throws {
        let page = TestSupport.newPage()
        try image("<svg width=\"200\" height=\"100\"><text y=\"20\">\u{2212}5</text></svg>")
                .setFont(TestSupport.helvetica(page.pdf)).drawOn(page)
        #expect(TestSupport.content(page).contains("[<2D35>] TJ"), "\(TestSupport.content(page))")
    }

    @Test func theFamilyTheWeightAndTheStyleChooseTheFontPDFjetCarries() throws {
        let page = TestSupport.newPage()
        try image("<svg width=\"200\" height=\"100\">"
                + "<text y=\"10\" font-family=\"'Times New Roman', serif\" font-weight=\"600\" font-style=\"italic\">a</text>"
                + "<text y=\"20\" style=\"font: bold 10px Courier, monospace\">b</text>"
                + "<text y=\"30\" font-family=\"Unknown, Arial\">c</text>"
                + "<text y=\"40\" font-family=\"Unknown\" font-weight=\"bolder\">d</text></svg>").drawOn(page)
        // Another image draws in the same fonts of the document, not new ones
        try image("<svg width=\"200\" height=\"100\"><text y=\"10\" font-family=\"Georgia\" "
                + "font-weight=\"600\" font-style=\"oblique\">e</text></svg>").drawOn(page)
        #expect(Set(page.pdf.bundledFonts.keys) == [
                "IBMPlexSerif/IBMPlexSerif-SemiBoldItalic.ttf", "CascadiaMono/CascadiaMono-Bold.ttf",
                "IBMPlexSans/IBMPlexSans-Regular.ttf", "IBMPlexSans/IBMPlexSans-Bold.ttf"])
    }

    @Test func theTextOfADocumentOfPDFAOrPDFUAIsInEmbeddedFonts() throws {
        // The fonts PDFjet carries are embedded, so the text needs no font set
        let svg = "<svg width=\"200\" height=\"100\"><text y=\"20\">A</text></svg>"
        for compliance in [Compliance.PDF_A_3B, Compliance.PDF_UA_1] {
            let pdf = PDF(OutputStream(toMemory: ()), compliance)
            pdf.setTitle("SVG")
            let page = Page(pdf, Letter.PORTRAIT)
            try image(svg).setAltDescription("A").drawOn(page)
            try pdf.complete()
        }
    }

    @Test func aStrokeBeforeTheFillDrawsAHaloUnderTheText() throws {
        let content = try draw("<svg width=\"200\" height=\"100\"><text y=\"20\" stroke=\"white\" stroke-width=\"3\" "
                + "stroke-linejoin=\"round\" paint-order=\"stroke\">A</text></svg>")
        let stroke = content.range(of: "1 Tr\n")
        let fill = content.range(of: "0 Tr\n")
        #expect(stroke != nil && fill != nil && stroke!.lowerBound < fill!.lowerBound, "\(content)")
        #expect(content.contains("1 1 1 RG\n3 w\n"), "\(content)")
    }

    @Test func aSetFontDrawsTheText() throws {
        let page = TestSupport.newPage()
        let font = try TestSupport.plexSans(page.pdf)
        let svgImage = try image("<svg width=\"200\" height=\"100\"><text y=\"20\" font-weight=\"bold\">Hi</text></svg>")
        svgImage.setFont(font)  // The bold text too, as no bold font is set
        svgImage.drawOn(page)
        let content = TestSupport.content(page)
        #expect(content.contains("> Tj\n"), "\(content)")
        #expect(!content.contains("TJ"), "\(content)")
    }

    @Test func aDashArrayOfAnOddNumberOfLengthsIsRepeated() throws {
        let content = try draw("<svg width=\"200\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" "
                + "stroke-dasharray=\"4, 2 1\" stroke-dashoffset=\"1\"/>"
                + "<path d=\"M0 20 L100 20\" stroke=\"red\" stroke-dasharray=\"0 0\"/>"
                + "<path d=\"M0 30 L100 30\" stroke=\"red\" stroke-dasharray=\"4 -2\"/></svg>")
        #expect(content.contains("q\n[4 2 1 4 2 1] 1 d\n"), "\(content)")
        #expect(content.components(separatedBy: " d\n").count - 1 == 1, "\(content)")   // The others are solid
    }

    @Test func aUseDrawsTheElementItNamesMovedByItsXAndY() throws {
        let content = try draw("<svg xmlns:xlink=\"http://www.w3.org/1999/xlink\" width=\"200\" height=\"100\">"
                + "<defs><rect id=\"r\" width=\"10\" height=\"10\"/><symbol id=\"s\"><circle r=\"2\"/></symbol></defs>"
                + "<use href=\"#r\" x=\"20\" y=\"30\" fill=\"red\"/><use xlink:href=\"#s\" x=\"50\" y=\"50\"/>"
                + "<g id=\"loop\"><use href=\"#loop\"/></g></svg>")
        #expect(content.hasPrefix("1 0 0 rg\n20 762 m\n30 762 l\n"), "\(content)")
        #expect(content.contains("\n52 742 m\n"), "\(content)")
    }

    @Test func usesOfUsesThatDrawTooManyElementsThrow() {
        var svg = "<svg width=\"10\" height=\"10\"><rect id=\"g0\" width=\"1\" height=\"1\"/>"
        for level in 1...7 {
            svg += "<g id=\"g\(level)\">"
            for _ in 0..<10 {
                svg += "<use href=\"#g\(level - 1)\"/>"
            }
            svg += "</g>"
        }
        svg += "</svg>"
        let error = #expect(throws: (any Error).self) {
            _ = try image(svg)
        }
        #expect(TestSupport.message(error)
                == "The SVG draws more than 1000000 elements again, with use elements or markers.")
    }

    @Test func aMarkerIsDrawnAtTheEndTurnedWithThePath() throws {
        let content = try draw("<svg width=\"200\" height=\"100\"><defs><marker id=\"m\" markerWidth=\"4\" "
                + "markerHeight=\"4\" orient=\"auto\" refX=\"1\"><rect width=\"2\" height=\"1\"/></marker></defs>"
                + "<path d=\"M10 10 L10 50\" stroke=\"black\" stroke-width=\"2\" marker-end=\"url(#m)\"/></svg>")
        // Turned 90 degrees down the path and twice the size, for the stroke
        // width: from 10, 48 to 8, 52.
        #expect(content.contains("S\n10 744 m\n10 740 l\n8 740 l\n8 744 l\nf\n"), "\(content)")
    }

    @Test func aClipPathCutsOffWhatIsOutsideIt() throws {
        let content = try draw("<svg width=\"200\" height=\"100\"><clipPath id=\"c\"><rect width=\"50\" height=\"50\"/>"
                + "</clipPath><g clip-path=\"url(#c)\" transform=\"translate(10,0)\"><rect x=\"10\" y=\"10\" width=\"80\" "
                + "height=\"20\"/></g><clipPath id=\"none\"/><rect width=\"5\" height=\"5\" clip-path=\"url(#none)\"/></svg>")
        // The clip path is in the user space of the group, moved with it.
        #expect(content == "q\n10 792 m\n60 792 l\n60 742 l\n10 742 l\nh\nW\nn\n"
                + "0 0 0 rg\n20 782 m\n100 782 l\n100 762 l\n20 762 l\nf\nQ\n", "\(content)")
    }

    // An SVG of a chain of use elements, flat in the file.
    private func useChain(_ links: Int) -> String {
        var svg = "<svg width=\"10\" height=\"10\"><defs><rect id=\"u0\" width=\"1\" height=\"1\"/>"
        for i in stride(from: 1, through: links, by: 1) {
            svg += "<use id=\"u\(i)\" href=\"#u\(i - 1)\"/>"
        }
        return svg + "</defs><use href=\"#u\(links)\"/></svg>"
    }

    // An SVG of elements nested levels deep in the svg element, around the content.
    private func nested(_ open: String, _ end: String, _ content: String, _ levels: Int) -> String {
        return "<svg width=\"10\" height=\"10\">" + String(repeating: open, count: levels) + content
                + String(repeating: end, count: levels) + "</svg>"
    }

    // An SVG of markers each drawn by a path in the one before it.
    private func markerChain(_ links: Int) -> String {
        var svg = "<svg width=\"10\" height=\"10\">"
        for i in 0..<links {
            svg += "<marker id=\"m\(i)\"><path d=\"M0 0 L1 1\" stroke=\"red\" marker-end=\"url(#m\(i + 1))\"/></marker>"
        }
        svg += "<marker id=\"m\(links)\"><rect width=\"1\" height=\"1\"/></marker>"
        return svg + "<path d=\"M0 0 L1 1\" stroke=\"red\" marker-end=\"url(#m0)\"/></svg>"
    }

    private func tspans(_ levels: Int) -> String {
        return nested("<g>", "</g>", "<text>a" + String(repeating: "<tspan>b", count: levels)
                + String(repeating: "</tspan>", count: levels) + "</text>", 2)
    }

    // A secondary thread of Apple's has a stack of 512 KB, and an overflow
    // kills the process.
    @Test func theDepthOfWhatIsDrawnIsLimitedOnASmallStack() throws {
        let finished = DispatchSemaphore(value: 0)
        nonisolated(unsafe) var failures = [String]()
        let accepted = [useChain(22), markerChain(14), nested("<g>", "</g>", "<rect width=\"1\" height=\"1\"/>", 46), tspans(43)]
        let refused = [useChain(23), useChain(2000), markerChain(15), tspans(44),
                nested("<g>", "</g>", "<rect width=\"1\" height=\"1\"/>", 47), nested("<g>", "</g>", "", 250)]
        let thread = Thread {
            for svg in accepted {
                do {
                    _ = try SVGImage(stream: InputStream(data: Data(svg.utf8)))
                } catch {
                    failures.append("\(error)")
                }
            }
            for svg in refused {
                do {
                    _ = try SVGImage(stream: InputStream(data: Data(svg.utf8)))
                    failures.append("read: \(svg.prefix(60))")
                } catch {
                    if !"\(error)".contains("deep") {
                        failures.append("\(error)")
                    }
                }
            }
            finished.signal()
        }
        thread.stackSize = 512*1024
        thread.start()
        finished.wait()
        #expect(failures.isEmpty, "\(failures)")
    }

    @Test func manyGradientsOfOneGradientOfManyStopsAreReadOnce() throws {
        func svg(_ n: Int) -> String {
            var svg = "<svg width=\"10\" height=\"10\"><linearGradient id=\"s\">"
            svg += String(repeating: "<stop stop-color=\"red\"/>", count: n)
            svg += "</linearGradient>"
            for i in 0..<n {
                svg += "<linearGradient id=\"g\(i)\" href=\"#s\"/>"
            }
            return svg + "<rect width=\"5\" height=\"5\" fill=\"url(#g7)\"/></svg>"
        }
        let count = 2000
        let svgs = [count: svg(count), 2 * count: svg(2 * count)]
        var content = ""
        try expectLinearTime(count) { n in content = try draw(svgs[n]!) }
        #expect(content.hasPrefix("1 0 0 rg\n"), "\(content.prefix(80))")
    }

    private func polygon(_ points: Int) -> String {
        return "<polygon id=\"p\" points=\"" + String(repeating: "0,0 1,1 ", count: points/2) + "\"/>"
    }

    @Test func theOperationsDrawnAgainAreLimited() throws {
        let clipped = "<svg width=\"10\" height=\"10\"><clipPath id=\"c\">" + polygon(10000) + "</clipPath>"
        for svg in [
                "<svg width=\"10\" height=\"10\"><defs>" + polygon(10000) + "</defs>"
                        + String(repeating: "<use href=\"#p\"/>", count: 1000) + "</svg>",
                clipped + String(repeating: "<g clip-path=\"url(#c)\"/>", count: 1000) + "</svg>",
                clipped + "<g clip-path=\"url(#c)\">"
                        + String(repeating: "<rect width=\"1\" height=\"1\"/>", count: 1000) + "</g></svg>",
                clipped + "<g clip-path=\"url(#c)\"><text>"
                        + String(repeating: "<tspan>a</tspan>", count: 1000) + "</text></g></svg>",
                "<svg width=\"10\" height=\"10\"><defs><text id=\"p\">" + String(repeating: "a", count: 10000)
                        + "</text></defs>" + String(repeating: "<use href=\"#p\"/>", count: 1000) + "</svg>",
                "<svg width=\"10\" height=\"10\"><clipPath id=\"c\"><text>" + String(repeating: "a", count: 1000)
                        + "</text></clipPath><g clip-path=\"url(#c)\">"
                        + String(repeating: "<rect width=\"1\" height=\"1\"/>", count: 5000) + "</g></svg>",
                "<svg width=\"10\" height=\"10\"><clipPath id=\"c\"><text>a" + String(repeating: "<tspan/>", count: 100000)
                        + "</text></clipPath>" + String(repeating: "<g clip-path=\"url(#c)\"/>", count: 100) + "</svg>",
                nested("<svg width=\"1\" height=\"1\">", "</svg>",
                        String(repeating: "<rect width=\"1\" height=\"1\"/>", count: 20000), 45)] {
            #expect(throws: (any Error).self) {
                do {
                    _ = try image(svg)
                } catch {
                    #expect("\(error)".contains("path operations"), "\(error)")
                    throw error
                }
            }
        }
        // A file drawn once is not counted, however large
        _ = try image("<svg width=\"10\" height=\"10\"><defs>" + polygon(1000) + "</defs>"
                + String(repeating: "<use href=\"#p\"/>", count: 1000) + "</svg>")
    }

    @Test func aUseInAClipPathDrawsTheShapeItNames() throws {
        do {
            let content = try draw("<svg width=\"200\" height=\"100\"><defs><rect id=\"r\" width=\"50\" height=\"50\"/></defs><clipPath id=\"c\"><use href=\"#r\" x=\"5\" y=\"5\" transform=\"translate(10,0)\"/></clipPath><g clip-path=\"url(#c)\"><rect x=\"10\" y=\"10\" width=\"80\" height=\"20\"/></g></svg>")
            #expect(content == "q\n15 787 m\n65 787 l\n65 737 l\n15 737 l\nh\nW\nn\n0 0 0 rg\n10 782 m\n90 782 l\n90 762 l\n10 762 l\nf\nQ\n", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"200\" height=\"100\"><clipPath id=\"c\"><use href=\"#missing\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>")
            #expect(content == "", "\(content)")
        }
    }

    @Test func theClipRuleOfAClipPathIsDrawn() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><path clip-rule=\"evenodd\" d=\"M0 0 H50 V50 H0 Z\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>")
            #expect(content.contains("h\nW*\nn\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\" style=\"clip-rule:evenodd\"><rect width=\"50\" height=\"50\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>")
            #expect(content.contains("h\nW*\nn\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"50\" height=\"50\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>")
            #expect(content.contains("h\nW\nn\n"), "\(content)")
        }
    }

    @Test func aTextInAClipPathClips() throws {
        var content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text x=\"10\" y=\"20\" font-size=\"12\">Hi</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>")
        #expect(content.hasPrefix("q\nBT\n7 Tr\n/"), "\(content)")
        #expect(content.contains(" 12 Tf\n1 0 0 1 10 772 Tm\n<"), "\(content)")
        #expect(content.contains("> Tj\nET\n0 0 0 rg\n0 792 m\n"), "\(content)")
        #expect(content.hasSuffix("f\nQ\n"), "\(content)")
        #expect(!content.contains("W\n"), "\(content)")
        // A text drawn in it is drawn in the mode of its own.
        content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text y=\"20\">Hi</text></clipPath><text y=\"20\" clip-path=\"url(#c)\">Hi</text></svg>")
        #expect(content.contains("0 Tr\n1 0 0 1 0 772 Tm\n"), "\(content)")
        // All the texts are in one text object, each with its transform.
        content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\" transform=\"translate(5,0)\"><text x=\"1\" y=\"20\">a<tspan x=\"1\" y=\"40\" font-weight=\"bold\">b</tspan></text><text x=\"1\" y=\"60\" transform=\"scale(2)\">c</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>")
        #expect(content.components(separatedBy: "BT\n").count == 2, "\(content)")
        #expect(content.components(separatedBy: "> Tj\n").count == 4, "\(content)")
        #expect(content.components(separatedBy: " Tf\n").count == 4, "\(content)")
        #expect(content.contains("1 0 0 1 6 772 Tm\n"), "\(content)")
        #expect(content.contains("1 0 0 1 6 752 Tm\n"), "\(content)")
        #expect(content.contains("2 0 0 2 7 672 Tm\n"), "\(content)")
        // A use names a text.
        content = try draw("<svg width=\"100\" height=\"100\"><defs><text id=\"t\" y=\"20\">Hi</text></defs><clipPath id=\"c\"><use href=\"#t\" x=\"5\"/></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>")
        #expect(content.contains("1 0 0 1 5 772 Tm\n"), "\(content)")
        // Shapes and texts: a PDF cannot have them in one path, and the shapes stay.
        content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text y=\"20\">Hi</text><rect width=\"5\" height=\"5\"/></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>")
        #expect(!content.contains(" Tr\n"), "\(content)")
        #expect(content.contains("h\nW\nn\n"), "\(content)")
        // With no glyph it clips all away; out of the range of a PDF nothing is drawn.
        for text in ["<text y=\"20\"> </text>", "<text y=\"20\" font-size=\"0\">Hi</text>", "<text y=\"20\" display=\"none\">Hi</text>", "<text y=\"20\" visibility=\"hidden\">Hi</text>", "<text y=\"20\"><tspan visibility=\"hidden\">Hi</tspan></text>", "<text y=\"20\" transform=\"scale(0)\">Hi</text>", "<text y=\"20\" transform=\"scale(1e30)\">Hi</text>"] {
            #expect(try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\">" + text + "</clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>") == "", "\(text)")
        }
        // In the fill of no color, too: a clip path takes the shape of the glyphs.
        content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><text y=\"20\" fill=\"none\">Hi</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>")
        #expect(content.contains("7 Tr\n"), "\(content)")
        // In the space of the image, and of its scale.
        let svgImage = try image("<svg width=\"200\" height=\"200\" viewBox=\"0 0 100 100\"><clipPath id=\"c\"><text x=\"10\" y=\"20\" font-size=\"12\">Hi</text></clipPath><rect width=\"50\" height=\"50\" clip-path=\"url(#c)\"/></svg>")
        var page = TestSupport.newPage()
        _ = svgImage.setLocation(0, 0)
        _ = svgImage.drawOn(page)
        #expect(TestSupport.content(page).contains(" 12 Tf\n2 0 0 2 20 752 Tm\n"))
        svgImage.scaleBy(0.5)
        _ = svgImage.setLocation(10, 0)
        page = TestSupport.newPage()
        _ = svgImage.drawOn(page)
        #expect(TestSupport.content(page).contains(" 12 Tf\n1 0 0 1 20 772 Tm\n"))
    }

    @Test func theShapesOfAClipPathGoTheSameWayRound() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><polygon points=\"0,0 20,0 20,20 0,20\"/><polygon clip-rule=\"evenodd\" points=\"10,10 10,30 30,30 30,10\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>")
            #expect(content.contains("0 792 m\n20 792 l\n20 772 l\n0 772 l\nh\n10 782 m\n30 782 l\n30 762 l\n10 762 l\n10 782 l\nW\nn\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"20\" height=\"20\"/><path d=\"M50 0 C50 10 60 20 70 20 L70 0 Z\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>")
            #expect(content.contains("50 792 m\n70 792 l\n70 772 l\n60 772 50 782 50 792 c\nW\nn\n"), "\(content)")
        }
    }

    @Test func theAlphaOfAColorIsDrawn() throws {
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgba(255,0,0,0.5)\"/></svg>").paths[0].fillAlpha == Float(0.5))
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgb(255 0 0 / 25%)\"/></svg>").paths[0].fillAlpha == Float(0.25))
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgba(255,0,0,0.5)\" fill-opacity=\"0.5\"/></svg>").paths[0].fillAlpha == Float(0.25))
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#ff000040\"/></svg>").paths[0].fillAlpha == Float(0.2509804))
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#f004\"/></svg>").paths[0].fillAlpha == Float(0.26666668))
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" stroke=\"#00ff0080\"/></svg>").paths[0].strokeAlpha == Float(0.5019608))
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\"/></svg>").paths[0].fillAlpha == Float(1.0))
        #expect(try image("<svg width=\"100\" height=\"100\"><g fill=\"rgba(0,0,0,0.5)\"><path d=\"M0 0 L10 0 L10 10 Z\"/></g></svg>").paths[0].fillAlpha == Float(0.5))
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" color=\"rgba(0,0,255,0.5)\" fill=\"currentColor\"/></svg>").paths[0].fillAlpha == Float(0.5))
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"rgba(0,0,0,0.5)\"/></svg>")
            #expect(content.hasPrefix("q\n/GS1 gs\n0 0 0 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#ff000040\"/></svg>")
            #expect(content.hasPrefix("q\n/GS1 gs\n1 0 0 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#f004\"/></svg>")
            #expect(content.hasPrefix("q\n/GS1 gs\n1 0 0 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"#ff000000\"/></svg>")
            #expect(content == "", "\(content)")
        }
    }

    @Test func hslColorsAreDrawn() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(120, 100%, 50%)\"/></svg>")
            #expect(content.hasPrefix("0 1 0 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(240deg 100% 50%)\"/></svg>")
            #expect(content.hasPrefix("0 0 1 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"HSL(-120, 100%, 50%)\"/></svg>")
            #expect(content.hasPrefix("0 0 1 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(0.5turn 100% 50%)\"/></svg>")
            #expect(content.hasPrefix("0 1 1 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(0, 0%, 100%)\"/></svg>")
            #expect(content.hasPrefix("1 1 1 rg\n"), "\(content)")
        }
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsla(0, 100%, 50%, 0.5)\"/></svg>").paths[0].fillAlpha == Float(0.5))
        #expect(try image("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(0 100% 50% / 25%)\"/></svg>").paths[0].fillAlpha == Float(0.25))
    }

    @Test func theKeywordsOfAPaintAreInAnyCase() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" color=\"red\" fill=\"currentcolor\"/></svg>")
            #expect(content.hasPrefix("1 0 0 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" color=\"red\" fill=\"CURRENTCOLOR\"/></svg>")
            #expect(content.hasPrefix("1 0 0 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"NONE\"/></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"None\"/></svg>")
            #expect(content == "", "\(content)")
        }
    }

    @Test func whatIsNotVisibleIsNotDrawn() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><g visibility=\"hidden\"><rect width=\"5\" height=\"5\"/></g></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><rect width=\"5\" height=\"5\" style=\"visibility:collapse\"/></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><g visibility=\"hidden\"><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\" visibility=\"visible\"/></g></svg>")
            #expect(content.hasPrefix("0 0 1 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><text visibility=\"hidden\" x=\"1\" y=\"50\">a</text></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"9\">a<tspan visibility=\"hidden\">b</tspan>c</text></svg>")
            #expect(content.components(separatedBy: "BT\n").count - 1 == 2, "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><defs><rect id=\"r\" width=\"5\" height=\"5\"/></defs><use href=\"#r\" visibility=\"hidden\"/></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"5\" height=\"5\" visibility=\"hidden\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><marker id=\"m\"><rect width=\"5\" height=\"5\"/></marker><path d=\"M0 0 L1 1\" stroke=\"red\" visibility=\"hidden\" marker-end=\"url(#m)\"/></svg>")
            #expect(content == "", "\(content)")
        }
    }

    @Test func aSwitchDrawsTheFirstChildItCan() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>")
            #expect(content == "1 0 0 rg\n0 792 m\n5 792 l\n5 787 l\n0 787 l\nf\n", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><switch><title>a</title><foreignObject width=\"5\" height=\"5\"><rect width=\"5\" height=\"5\" fill=\"blue\"/></foreignObject><g><rect width=\"5\" height=\"5\" fill=\"red\"/></g><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>")
            #expect(content.hasPrefix("1 0 0 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" requiredExtensions=\"http://a\" fill=\"blue\"/><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>")
            #expect(content.components(separatedBy: "0 0 1 rg").count - 1 == 0, "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" requiredExtensions=\"http://a\" fill=\"blue\"/><rect width=\"5\" height=\"5\" fill=\"red\"/><rect width=\"5\" height=\"5\" fill=\"blue\"/></switch></svg>")
            #expect(content.components(separatedBy: "1 0 0 rg").count - 1 == 1, "\(content)")
        }
    }

    private func drawInLanguage(_ svg: String, _ language: String) throws -> String {
        let pdf = TestSupport.newPDF()
        if !language.isEmpty {
            _ = pdf.setLanguage(language)
        }
        let page = Page(pdf, Letter.PORTRAIT)
        let svgImage = try image(svg)
        _ = svgImage.setLocation(0, 0)
        _ = svgImage.drawOn(page)
        return TestSupport.content(page)
    }

    @Test func aSwitchHonorsTheSystemLanguage() throws {
        let svg = "<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" systemLanguage=\"de, fr-CA\" fill=\"red\"/><rect width=\"5\" height=\"5\" systemLanguage=\"\" fill=\"lime\"/><g systemLanguage=\"EN-gb\"><rect width=\"5\" height=\"5\" fill=\"blue\"/></g><rect width=\"5\" height=\"5\" fill=\"black\"/></switch></svg>"
        let tests = [("", "0 0 1 rg"), ("en-US", "0 0 1 rg"), ("en", "0 0 1 rg"), ("de", "1 0 0 rg"), ("de-AT", "1 0 0 rg"), ("FR", "1 0 0 rg"), ("bg", "0 0 0 rg"), ("fra", "0 0 0 rg")]
        for (language, color) in tests {
            let content = try drawInLanguage(svg, language)
            #expect(content.contains(color), "\(content)")
            #expect(content.components(separatedBy: " rg\n").count == 2, "\(content)")
        }
        // No child of the language, and none without a systemLanguage: nothing.
        #expect(try drawInLanguage("<svg width=\"100\" height=\"100\"><switch><rect width=\"5\" height=\"5\" systemLanguage=\"de\"/></switch></svg>", "") == "")
        // One image, drawn in the language of each PDF.
        let svgImage = try image(svg)
        for (language, color) in [("de", "1 0 0 rg"), ("en", "0 0 1 rg")] {
            let pdf = TestSupport.newPDF()
            _ = pdf.setLanguage(language)
            let page = Page(pdf, Letter.PORTRAIT)
            _ = svgImage.drawOn(page)
            #expect(TestSupport.content(page).contains(color))
        }
        // Texts, in a switch in a used group, nested, and a marker in a child.
        var content = try drawInLanguage("<svg width=\"100\" height=\"100\"><defs><g id=\"g\"><switch><text systemLanguage=\"de\" y=\"10\" fill=\"red\">a</text><switch><text systemLanguage=\"en\" y=\"10\" fill=\"blue\">b</text></switch></switch></g></defs><use href=\"#g\"/></svg>", "en")
        #expect(content.contains("0 0 1 rg") && !content.contains("1 0 0 rg"), "\(content)")
        #expect(try drawInLanguage("<svg width=\"100\" height=\"100\"><marker id=\"m\"><rect width=\"5\" height=\"5\" fill=\"red\"/></marker><switch><path systemLanguage=\"de\" d=\"M0 0 L9 9\" stroke=\"blue\" marker-end=\"url(#m)\"/></switch></svg>", "en") == "")
        // What is not drawn leaves no cut, and no miter limit, for itself.
        content = try drawInLanguage("<svg width=\"100\" height=\"100\"><switch><path systemLanguage=\"de\" d=\"M0 0 L900 900\" stroke=\"blue\"/><rect width=\"5\" height=\"5\"/></switch></svg>", "en")
        #expect(!content.contains("W\n") && !content.contains(" M\n"), "\(content)")
    }

    @Test func aUseGivesASymbolItsSize() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" x=\"5\" y=\"5\" width=\"20\" height=\"20\"/></svg>")
            #expect(content.contains("5 787 m\n25 787 l\n25 767 l\n5 767 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>")
            #expect(content.contains("0 782 m\n20 782 l\n20 762 l\n0 762 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMinYMin\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>")
            #expect(content.contains("0 792 m\n20 792 l\n20 772 l\n0 772 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"none\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>")
            #expect(content.contains("0 792 m\n20 792 l\n20 752 l\n0 752 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMaxYMax slice\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"20\" height=\"40\"/></svg>")
            #expect(content.contains("-20 792 m\n20 792 l\n20 752 l\n-20 752 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\"/></svg>")
            #expect(content.contains("0 792 m\n100 792 l\n100 692 l\n0 692 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></symbol><use href=\"#s\" width=\"0\" height=\"20\"/></svg>")
            #expect(content == "", "\(content)")
        }
    }

    @Test func aNestedSvgIsAViewport() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><svg x=\"10\" y=\"20\" width=\"20\" height=\"20\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>")
            #expect(content.contains("10 772 m\n30 772 l\n30 752 l\n10 752 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><svg x=\"10\" y=\"20\"><rect width=\"10\" height=\"10\"/></svg></svg>")
            #expect(content.contains("10 772 m\n20 772 l\n20 762 l\n10 762 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><svg x=\"50%\" y=\"10%\" width=\"20\" height=\"20\" viewBox=\"5 5 10 10\"><rect x=\"5\" y=\"5\" width=\"10\" height=\"10\"/></svg></svg>")
            #expect(content.contains("50 782 m\n70 782 l\n70 762 l\n50 762 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\" viewBox=\"0 0 200 200\"><svg width=\"50%\" height=\"50%\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>")
            #expect(content.contains("0 792 m\n50 792 l\n50 742 l\n0 742 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg><svg viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>")
            #expect(content.contains("0 792 m\n10 792 l\n10 782 l\n0 782 l\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><svg width=\"0\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><svg viewBox=\"0 0 -10 10\"><rect width=\"10\" height=\"10\"/></svg></svg>")
            #expect(content == "", "\(content)")
        }
    }

    @Test func aSymbolAndANestedSvgClipToTheirViewports() throws {
        let clip = "h\nW\nn\n"
        #expect(try draw("<svg width=\"100\" height=\"100\"><symbol id=\"s\" viewBox=\"0 0 10 10\"><rect width=\"20\" height=\"20\"/></symbol><use href=\"#s\" x=\"5\" y=\"5\" width=\"20\" height=\"20\"/></svg>") == "q\n5 787 m\n25 787 l\n25 767 l\n5 767 l\nh\nW\nn\n0 0 0 rg\n5 787 m\n45 787 l\n45 747 l\n5 747 l\nf\nQ\n")
        #expect(try draw("<svg width=\"100\" height=\"100\"><svg x=\"10\" y=\"20\" width=\"20\" height=\"20\"><rect width=\"50\" height=\"50\"/></svg></svg>").hasPrefix("q\n10 772 m\n30 772 l\n30 752 l\n10 752 l\n" + clip))
        // The viewport, not the viewBox, and in the space of the image.
        #expect(try draw("<svg width=\"100\" height=\"100\"><svg width=\"20\" height=\"40\" viewBox=\"0 0 10 10\" preserveAspectRatio=\"xMaxYMax slice\"><rect width=\"10\" height=\"10\"/></svg></svg>").contains("q\n0 792 m\n20 792 l\n20 752 l\n0 752 l\n" + clip))
        #expect(try draw("<svg width=\"200\" height=\"200\" viewBox=\"0 0 100 100\"><svg width=\"10\" height=\"10\"><rect width=\"5\" height=\"5\"/></svg></svg>").hasPrefix("q\n0 792 m\n20 792 l\n20 772 l\n0 772 l\n" + clip))
        // Moved and scaled by the scale of the image, too.
        let svgImage = try image("<svg width=\"100\" height=\"100\"><svg width=\"10\" height=\"10\"><rect width=\"5\" height=\"5\"/></svg></svg>")
        svgImage.scaleBy(2)
        _ = svgImage.setLocation(10, 0)
        let page = TestSupport.newPage()
        _ = svgImage.drawOn(page)
        #expect(TestSupport.content(page).hasPrefix("q\n10 792 m\n30 792 l\n30 772 l\n10 772 l\n" + clip))
        // Each viewport clips, and an overflow is not inherited.
        #expect(try draw("<svg width=\"100\" height=\"100\"><svg width=\"50\" height=\"50\"><svg width=\"20\" height=\"20\"><rect width=\"5\" height=\"5\"/></svg></svg></svg>").components(separatedBy: clip).count == 3)
        #expect(try draw("<svg width=\"100\" height=\"100\"><svg width=\"50\" height=\"50\" overflow=\"visible\"><svg width=\"20\" height=\"20\"><rect width=\"5\" height=\"5\"/></svg></svg></svg>").components(separatedBy: clip).count == 2)
        // Overflow, as an attribute, in a style and in rules; hidden and scroll clip.
        for overflow in ["overflow=\"visible\"", "overflow=\"auto\"", "overflow=\"VISIBLE\"", "style=\"overflow: visible\"", "class=\"o\""] {
            let content = try draw("<svg width=\"100\" height=\"100\"><style>.o { overflow: visible }</style><symbol id=\"s\" " + overflow + "><rect width=\"500\" height=\"5\"/></symbol><use href=\"#s\"/></svg>")
            #expect(!content.contains(clip), "\(content)")
        }
        for overflow in ["overflow=\"hidden\"", "overflow=\"scroll\""] {
            let content = try draw("<svg width=\"100\" height=\"100\"><svg width=\"5\" height=\"5\" " + overflow + "><rect width=\"5\" height=\"5\"/></svg></svg>")
            #expect(content.contains(clip), "\(content)")
        }
        // A viewport of no known size clips nothing, and one of no size hides.
        #expect(try !draw("<svg><svg x=\"1\"><rect width=\"5\" height=\"5\"/></svg></svg>").contains("W\n"))
        #expect(try draw("<svg width=\"100\" height=\"100\"><svg width=\"0\"><rect width=\"5\" height=\"5\"/></svg></svg>") == "")
    }

    @Test func thePaintOrderOfAShapeIsDrawn() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" paint-order=\"stroke\"/></svg>")
            #expect(content.contains("S\n1 0 0 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" style=\"paint-order: stroke fill markers\"/></svg>")
            #expect(content.contains("S\n1 0 0 rg\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" /></svg>")
            #expect(content.contains("f\n0 0 1 RG\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" paint-order=\"fill stroke\"/></svg>")
            #expect(content.contains("f\n0 0 1 RG\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"red\" stroke=\"blue\" paint-order=\"normal\"/></svg>")
            #expect(content.contains("f\n0 0 1 RG\n"), "\(content)")
        }
    }

    @Test func theMiterLimitOfAStrokeIsWritten() throws {
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" /></svg>")
            #expect(content.hasPrefix("q\n4 M\n1 0 0 RG\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"2.5\"/></svg>")
            #expect(content.hasPrefix("q\n2.5 M\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" style=\"stroke-miterlimit:1\"/></svg>")
            #expect(content.hasPrefix("q\n1 M\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"10\"/></svg>")
            #expect(content.hasPrefix("q\n10 M\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"0.5\"/></svg>")
            #expect(content.hasPrefix("q\n4 M\n1 0 0 RG\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-miterlimit=\"1e30\"/></svg>")
            #expect(content.hasPrefix("q\n4 M\n1 0 0 RG\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><g fill=\"none\" stroke=\"red\"><path d=\"M0 0 L10 0 L10 10 Z\"/><path d=\"M0 0 L9 0 L9 9 Z\"/></g></svg>")
            #expect(content.components(separatedBy: " M\n").count - 1 == 1, "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"none\" stroke=\"red\" stroke-linejoin=\"round\"/></svg>")
            #expect(!content.contains(" M\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\"/></svg>")
            #expect(!content.contains(" M\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"50\" stroke=\"red\">a</text></svg>")
            #expect(content.hasPrefix("q\n4 M\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"50\" stroke=\"red\" stroke-miterlimit=\"3\">a</text></svg>")
            #expect(content.contains("3 M\n"), "\(content)")
        }
        do {
            let content = try drawAsItIs("<svg width=\"100\" height=\"100\"><text x=\"1\" y=\"50\">a</text></svg>")
            #expect(!content.contains(" M\n"), "\(content)")
        }
    }

    @Test func aDashOffsetBelowZeroIsWrittenInsideThePattern() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"-1\"/></svg>")
            #expect(content.contains("[4 2] 5 d\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"-7\"/></svg>")
            #expect(content.contains("[4 2] 5 d\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"-6\"/></svg>")
            #expect(content.contains("[4 2] 0 d\n"), "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"7\"/></svg>")
            #expect(content.contains("[4 2] 7 d\n"), "\(content)")
        }
    }

    @Test func hugeDashesAndClipPathsSkipThePath() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"1e30 1\"/></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"4 2\" stroke-dashoffset=\"1e30\"/></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10\" stroke=\"red\" stroke-dasharray=\"1e30 1e30\" stroke-dashoffset=\"-1\"/></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"1e30\" height=\"5\"/></clipPath><rect width=\"5\" height=\"5\" clip-path=\"url(#c)\"/></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><clipPath id=\"c\"><rect width=\"1e30\" height=\"5\"/></clipPath><text x=\"1\" y=\"50\" clip-path=\"url(#c)\">a</text></svg>")
            #expect(content == "", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 10 L100 10 L0 20\" stroke-dasharray=\"1e30 1\"/></svg>")
            #expect(content.hasPrefix("0 0 0 rg\n"), "\(content)")
        }
    }

    @Test func aSizeInAUnitWithoutAViewBoxIsInPixels() throws {
        do {
            let content = try draw("<svg width=\"75pt\" height=\"75pt\"><rect width=\"100\" height=\"100\"/></svg>")
            #expect(content == "0 0 0 rg\n0 792 m\n75 792 l\n75 717 l\n0 717 l\nf\n", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"1in\" height=\"1in\"><rect width=\"96\" height=\"96\"/></svg>")
            #expect(content == "0 0 0 rg\n0 792 m\n72 792 l\n72 720 l\n0 720 l\nf\n", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"10mm\" height=\"10mm\"><rect width=\"37.79\" height=\"37.79\"/></svg>")
            #expect(!content.contains("W\n"), "\(content)")
        }
        #expect(try image("<svg width=\"75pt\" height=\"75pt\"><rect width=\"100\" height=\"100\"/></svg>").getWidth() == 75)
        #expect(try image("<svg width=\"75pt\" height=\"75pt\"><rect width=\"100\" height=\"100\"/></svg>").getHeight() == 75)
        do {
            let content = try draw("<svg width=\"100px\" height=\"100\"><rect width=\"100\" height=\"100\"/></svg>")
            #expect(content == "0 0 0 rg\n0 792 m\n100 792 l\n100 692 l\n0 692 l\nf\n", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"75pt\" height=\"75pt\" viewBox=\"0 0 10 10\"><rect width=\"10\" height=\"10\"/></svg>")
            #expect(content == "0 0 0 rg\n0 792 m\n75 792 l\n75 717 l\n0 717 l\nf\n", "\(content)")
        }
        do {
            let content = try draw("<svg width=\"75pt\" height=\"75pt\"><rect width=\"10\" height=\"10\" fill=\"none\" stroke=\"red\" stroke-width=\"4\"/></svg>")
            #expect(content.contains("3 w\n"), "\(content)")
        }
    }

    @Test func aViewBoxOfNegativeSizeIsRefused() throws {
        #expect(throws: (any Error).self) { _ = try image("<svg width=\"10\" height=\"10\" viewBox=\"0 0 -10 10\"/>") }
        #expect(throws: (any Error).self) { _ = try image("<svg width=\"10\" height=\"10\" viewBox=\"0 0 10 -10\"/>") }
    }

    @Test func aHueTooLargeForItsUnitIsNotAColor() throws {
        do {
            let content = try draw("<svg width=\"100\" height=\"100\"><path d=\"M0 0 L10 0 L10 10 Z\" fill=\"hsl(1e308turn, 100%, 50%)\"/></svg>")
            #expect(content.hasPrefix("0 0 0 rg\n"), "\(content)")
        }
    }
}
