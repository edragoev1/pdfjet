/**
 * ImageSize.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// The size an image is drawn at, and its pixels, read from the header of its
/// file alone, without its image data in memory: the size of a page laid out
/// before its images are drawn, and an image refused early, for its size or
/// for a header that Image refuses, before a byte of its image data is
/// decoded.
///
/// The size is the one Image gives the image: its pixels, or the physical
/// size the file asks for, the pHYs chunk of a PNG, the JFIF density of a JPEG
/// or the pixels per metre of a BMP; a JPEG turned a quarter of the way by its
/// Exif orientation is its height by its width. What only the image data
/// shows, a JPEG cut short or the rows of a PNG, is Image's to refuse.
///
public final class ImageSize {
    private let width: Float
    private let height: Float
    private let pixelWidth: Int
    private let pixelHeight: Int

    private init(_ pixelWidth: Int, _ pixelHeight: Int,
            _ physicalWidth: Float, _ physicalHeight: Float, turned: Bool) {
        self.pixelWidth = pixelWidth
        self.pixelHeight = pixelHeight
        var w = Float(pixelWidth)
        var h = Float(pixelHeight)
        if physicalWidth > 0.0 && physicalHeight > 0.0 {
            w = physicalWidth
            h = physicalHeight
        }
        self.width = turned ? h : w
        self.height = turned ? w : h
    }

    /// Reads the size of the PNG, JPEG or BMP file at the path.
    public static func read(_ filePath: String) throws -> ImageSize {
        guard let stream = InputStream(fileAtPath: filePath) else {
            throw PDFjetError(message: "Cannot open the file \(filePath).")
        }
        return try read(stream)
    }

    /// Reads the size of the PNG, JPEG or BMP image of the stream from its
    /// header, or throws why Image would refuse the image, as far as its
    /// header shows. A JPEG is read to its frame header, a BMP to its pixels
    /// per metre, and a PNG to its end, a chunk at a time, its image data
    /// checked and passed over, as a pHYs chunk may come after it. The stream
    /// is opened and closed, as Image opens and closes it.
    public static func read(_ stream: InputStream) throws -> ImageSize {
        stream.open()
        defer { stream.close() }
        let reader = StreamReader(stream)
        let head = reader.upTo(4)
        reader.unread(head)
        // Four bytes at least, as the other ports peek at them, before the
        // type is decided
        if head.count < 4 {
            throw PDFjetError(message: "The image is not a PNG, JPEG or BMP file.")
        }
        if head[0] == 0x89 && head[1] == 0x50 && head[2] == 0x4E && head[3] == 0x47 {
            let png = try PNGImage.readSize({ n in
                let bytes = reader.upTo(n)
                if bytes.count < n {
                    throw PDFjetError(message: "Unexpected end of the PNG stream.")
                }
                return bytes
            })
            return ImageSize(png.getWidth(), png.getHeight(),
                    png.getPhysicalWidth(), png.getPhysicalHeight(), turned: false)
        }
        if head[0] == 0xFF && head[1] == 0xD8 {
            let jpg = try JPGImage(jpgHeader(reader), headerOnly: true)
            // Turned a quarter of the way, as Image's setOrientation turns it
            return ImageSize(Int(jpg.getWidth()), Int(jpg.getHeight()),
                    jpg.getPhysicalWidth(), jpg.getPhysicalHeight(),
                    turned: jpg.orientation >= 5 && jpg.orientation <= 8)
        }
        if head[0] == 0x42 && head[1] == 0x4D {
            // The header to the colors used, which the palette's size checks
            let bmp = try BMPImage(InputStream(data: Data(reader.upTo(50))), headerOnly: true)
            return ImageSize(bmp.getWidth(), bmp.getHeight(),
                    bmp.getPhysicalWidth(), bmp.getPhysicalHeight(), turned: false)
        }
        throw PDFjetError(message: "The image is not a PNG, JPEG or BMP file.")
    }

    /// The width the image is drawn at, in points, as Image's getWidth before it is scaled.
    public func getWidth() -> Float {
        return width
    }

    /// The height the image is drawn at, in points, as Image's getHeight before it is scaled.
    public func getHeight() -> Float {
        return height
    }

    /// The width of the image in pixels, as stored.
    public func getPixelWidth() -> Int {
        return pixelWidth
    }

    /// The height of the image in pixels, as stored.
    public func getPixelHeight() -> Int {
        return pixelHeight
    }

    // The bytes of a JPEG for JPGImage to read to its frame header: its
    // markers, as JPGImage's nextMarker finds them, the bytes between them
    // that are not markers left out; its JFIF and Exif segments whole; its
    // frame header to the components, the length, the precision, the height,
    // the width and the components, as JPGImage reads them, the components'
    // specifications not read, as in the other ports (the review of
    // 9 October 2026); and the other segments empty, their bytes passed over, so
    // that no segment but those is held. The stream ending, what was read is
    // given, for JPGImage to refuse as it refuses a JPEG cut short there.
    private static func jpgHeader(_ reader: StreamReader) -> [UInt8] {
        var header = reader.upTo(2)     // The SOI marker
        while true {
            // The next marker: bytes that are not 0xFF passed over, 0xFF
            // bytes of padding swallowed, and a 0xFF a zero byte follows data
            var ch: UInt8 = 0
            repeat {
                guard var b = reader.byte() else { return header }
                while b != 0xFF {
                    guard let next = reader.byte() else { return header }
                    b = next
                }
                repeat {
                    guard let next = reader.byte() else { return header }
                    b = next
                } while b == 0xFF
                ch = b
            } while ch == 0x00
            header += [0xFF, ch]
            if ch == 0x01 || ch == 0xD8 || (ch >= 0xD0 && ch <= 0xD7) {
                continue        // A marker of no segment
            }
            if ch == 0xD9 || ch == 0xC3 || (ch >= 0xC5 && ch <= 0xC7) || (ch >= 0xC9 && ch <= 0xCB) ||
                    (ch >= 0xCD && ch <= 0xCF) {
                return header   // The end, or a frame JPGImage refuses
            }
            if ch == 0xC0 || ch == 0xC1 || ch == 0xC2 {
                return header + reader.upTo(2 + 6)     // The frame header
            }
            let lengthBytes = reader.upTo(2)
            if lengthBytes.count < 2 {
                return header + lengthBytes
            }
            let length = Int(lengthBytes[0]) << 8 | Int(lengthBytes[1])
            if length < 2 {
                return header + lengthBytes
            }
            if ch == 0xE0 || ch == 0xE1 {
                header += lengthBytes + reader.upTo(length - 2)
            } else {
                header += [0x00, 0x02]      // Empty
                if !reader.skip(length - 2) {
                    // Cut short within it: the length it has, and no bytes
                    header.removeLast(2)
                    return header + lengthBytes
                }
            }
        }
    }
}

// Reads the bytes of an open stream as asked, with bytes put back first.
private final class StreamReader {
    private let stream: InputStream
    private var pending = [UInt8]()
    private var buffer = [UInt8](repeating: 0, count: 65536)

    init(_ stream: InputStream) {
        self.stream = stream
    }

    func unread(_ bytes: [UInt8]) {
        pending = bytes + pending
    }

    // Up to n bytes, fewer only at the end of the stream
    func upTo(_ n: Int) -> [UInt8] {
        var bytes = [UInt8]()
        if !pending.isEmpty {
            let k = min(n, pending.count)
            bytes += pending[0..<k]
            pending.removeFirst(k)
        }
        while bytes.count < n {
            let read = stream.read(&buffer, maxLength: min(buffer.count, n - bytes.count))
            if read <= 0 {
                break
            }
            bytes += buffer[0..<read]
        }
        return bytes
    }

    func byte() -> UInt8? {
        return upTo(1).first
    }

    // Passes over n bytes; returns whether the stream had them
    func skip(_ n: Int) -> Bool {
        var left = n
        while left > 0 {
            let piece = upTo(min(left, buffer.count))
            if piece.isEmpty {
                return false
            }
            left -= piece.count
        }
        return true
    }
}
