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

    @Test func scalingScalesTheSizeWithThePaths() throws {
        let svgImage = try image("<svg width=\"100\" height=\"50\"><path d=\"M10 10 L90 40\"/></svg>")
        svgImage.scaleBy(0.5)
        #expect(svgImage.getWidth() == 50)
        #expect(svgImage.getHeight() == 25)
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
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

    @Test func pathDataThatNeedsTheCurrentPointStartsAtTheOrigin() throws {
        // The first command of the path data needs a current point, which this
        // port left unset: it trapped.
        let paths = [
            "L90 40", "H90", "V40", "Q10 10 90 40", "T90 40",
            "C1 1 2 2 90 40", "S1 1 90 40", "A5 5 0 0 1 90 40", "l90 40",
        ]
        for data in paths {
            let content = try draw("<svg width=\"100\" height=\"50\"><path d=\"" + data + "\"/></svg>")
            #expect(content.contains(" l\n") || content.contains(" c\n"), "\(data) draws \(content)")
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
            ("A 120 120 120 0 0 120 120", 1),   // A quarter turn, of the fuzz corpus
            ("M10 10 A 20 20 0 0 1 50 10", 2),
            ("M0 0 A50 50 0 0 1 100 0", 2),
        ]
        for (data, count) in arcs {
            let content = try draw("<svg width=\"200\" height=\"200\"><path d=\"" + data + "\"/></svg>")
            #expect(curves(content) == count, "\(data) draws \(content)")
        }
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
            ("0 0 0 10", "its width and height cannot be zero."),
            ("0 0 10 0", "its width and height cannot be zero."),
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
        let want = "1 0 0 rg\n10 782 m\n90 782 l\n90 702 l\nf\n0 0 1 RG\n2 w\n10 782 m\n90 782 l\n90 702 l\ns\n"
                + "0 0.5 0 rg\n10 782 m\n50 782 l\n50 742 l\nf\n4 w\n10 782 m\n50 782 l\n50 742 l\ns\n"
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
                "1 0 0 RG\n1 w\n10 782 m\n20 772 l\n10 772 l\ns\n"),
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
            ("fill=\"RGBA(255 0 0 / 0.5)\"", "1 0 0 rg"),
            ("fill=\"Red\"", "1 0 0 rg"),
            ("fill=\"currentColor\" color=\"red\"", "1 0 0 rg"),
            ("fill=\"currentColor\"", "0 0 0 rg"),
            ("fill=\"url(#gradient) red\"", "1 0 0 rg"),
            ("fill=\"url(#gradient)\"", "0 0 0 rg"),    // Not drawn: as if not given
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
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
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
        let page = Page(TestSupport.newPDF(), Letter.PORTRAIT)
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
    }

    private func curves(_ content: String) -> Int {
        return content.components(separatedBy: " c\n").count - 1
    }
}
