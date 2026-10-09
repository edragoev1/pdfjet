/**
 * ImageSizeTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

@Suite struct ImageSizeTests {
    // The size read from the header against Image: an image Image takes has
    // the size it draws it at, and an image whose header is refused Image
    // refuses too. Returns whether Image took it.
    private func agrees(_ name: String, _ data: [UInt8], _ problems: inout [String]) -> Bool {
        var size: ImageSize?
        var sizeError: Error?
        do {
            size = try ImageSize.read(InputStream(data: Data(data)))
        } catch {
            sizeError = error
        }
        guard let image = try? Image(TestSupport.newPDF(), InputStream(data: Data(data))) else {
            return false
        }
        if let error = sizeError {
            problems.append("\(name): Image takes it, ImageSize refuses it: \(error)")
        } else if let size, size.getWidth() != image.getWidth() || size.getHeight() != image.getHeight() {
            problems.append("\(name): \(size.getWidth()) by \(size.getHeight()); Image \(image.getWidth()) by \(image.getHeight())")
        }
        return true
    }

    @Test func theSizeIsTheOneImageDrawsAt() throws {
        var files = [String]()
        for dir in ["images", "tests/data", ".images"] {
            let root = TestSupport.path(dir)
            guard let walk = FileManager.default.enumerator(atPath: root) else {
                continue
            }
            for case let file as String in walk {
                let ext = (file as NSString).pathExtension.lowercased()
                if ["png", "jpg", "jpeg", "bmp"].contains(ext) {
                    files.append(root + "/" + file)
                }
            }
        }
        var problems = [String]()
        var taken = 0
        for file in files {
            let data = [UInt8](try Data(contentsOf: URL(fileURLWithPath: file)))
            if agrees(file, data, &problems) {
                taken += 1
            }
        }
        #expect(problems.isEmpty, "\(problems.prefix(20).joined(separator: "\n"))")
        #expect(taken >= 20, "\(taken) images taken, of \(files.count)")
    }

    @Test func aHugePNGIsRefusedFromItsHeader() throws {
        var png: [UInt8] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
        png += chunk("IHDR", [0, 1, 0x86, 0xA0, 0, 1, 0x86, 0xA0, 8, 6, 0, 0, 0])
        png += chunk("IEND", [])
        do {
            _ = try ImageSize.read(InputStream(data: Data(png)))
            Issue.record("a PNG of 100,000 by 100,000 taken")
        } catch {
            #expect("\(error)".contains("larger than"))
        }
    }

    @Test func aJPEGTurnedIsItsHeightByItsWidth() throws {
        for orientation in ["1", "3", "6", "8"] {
            let size = try ImageSize.read(TestSupport.path("tests/data/jpeg/orientation-\(orientation).jpg"))
            #expect(size.getWidth() == 32 && size.getHeight() == 16, "orientation \(orientation)")
        }
    }

    @Test func refusesWhatImageRefusesInTheHeader() throws {
        // The review of 9 October 2026: an 8-bit BMP of 1,000 colors and a PNG
        // whose one IDAT is empty were sizes, and Image refused them.
        var bmp = [UInt8](repeating: 0, count: 54)
        bmp[0] = 0x42
        bmp[1] = 0x4D
        func le(_ at: Int, _ value: Int, _ count: Int = 4) {
            for i in 0..<count {
                bmp[at + i] = UInt8(truncatingIfNeeded: value >> (8 * i))
            }
        }
        le(10, 54)
        le(14, 40)
        le(18, 1)
        le(22, 1)
        le(26, 1, 2)
        le(28, 8, 2)
        le(46, 1000)
        do {
            _ = try ImageSize.read(InputStream(data: Data(bmp)))
            Issue.record("a BMP of 1,000 colors taken")
        } catch {
            #expect("\(error)".contains("palette"), "\(error)")
        }

        var png: [UInt8] = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]
        png += chunk("IHDR", [0, 0, 0, 1, 0, 0, 0, 1, 8, 0, 0, 0, 0])
        png += chunk("IDAT", [])
        png += chunk("IEND", [])
        #expect(throws: (any Error).self, "a PNG of no image data taken") {
            _ = try ImageSize.read(InputStream(data: Data(png)))
        }

        // A JPEG that ends inside its frame header, after the size, is a size,
        // as in the other ports: its data is Image's to refuse.
        let jpg: [UInt8] = [0xFF, 0xD8, 0xFF, 0xC0, 0x00, 0x11, 0x08, 0x00, 0x10, 0x00, 0x20, 0x03]
        let size = try ImageSize.read(InputStream(data: Data(jpg)))
        #expect(size.getPixelWidth() == 32 && size.getPixelHeight() == 16)

        // Fewer than four bytes are no image, as the other ports say.
        for short: [UInt8] in [[0xFF, 0xD8], [0xFF, 0xD8, 0xFF], [0x42, 0x4D]] {
            do {
                _ = try ImageSize.read(InputStream(data: Data(short)))
                Issue.record("\(short) taken")
            } catch {
                #expect(TestSupport.message(error) == "The image is not a PNG, JPEG or BMP file.", "\(error)")
            }
        }
    }

    private func chunk(_ type: String, _ data: [UInt8]) -> [UInt8] {
        let n = data.count
        var out: [UInt8] = [UInt8(n >> 24 & 0xFF), UInt8(n >> 16 & 0xFF), UInt8(n >> 8 & 0xFF), UInt8(n & 0xFF)]
        let t = [UInt8](type.utf8)
        out += t + data
        let crc = CRC32()
        crc.update(t, 0, 4)
        crc.update(data, 0, data.count)
        let c = crc.getValue()
        out += [UInt8(c >> 24 & 0xFF), UInt8(c >> 16 & 0xFF), UInt8(c >> 8 & 0xFF), UInt8(c & 0xFF)]
        return out
    }
}
