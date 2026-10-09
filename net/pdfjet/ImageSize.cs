/**
 *  ImageSize.cs
 *
 *  Copyright (c) 2026 PDFjet Software
 *  Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;

namespace PDFjet.NET {
/// <summary>
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
/// </summary>
public sealed class ImageSize {
    private readonly float width;
    private readonly float height;
    private readonly int pixelWidth;
    private readonly int pixelHeight;

    private ImageSize(int pixelWidth, int pixelHeight, float physicalWidth, float physicalHeight, bool turned) {
        this.pixelWidth = pixelWidth;
        this.pixelHeight = pixelHeight;
        float w = pixelWidth;
        float h = pixelHeight;
        if (physicalWidth > 0f && physicalHeight > 0f) {
            w = physicalWidth;
            h = physicalHeight;
        }
        this.width = turned ? h : w;
        this.height = turned ? w : h;
    }

    /// <summary>Reads the size of the PNG, JPEG or BMP file at the path.</summary>
    public static ImageSize Read(String filePath) {
        using (FileStream stream = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
            return Read(stream);
        }
    }

    /// <summary>
    /// Reads the size of the PNG, JPEG or BMP image of the stream from its
    /// header, or throws why Image would refuse the image, as far as its
    /// header shows. A JPEG is read to its frame header, a BMP to its pixels
    /// per metre, and a PNG to its end, a chunk at a time, its image data
    /// checked and passed over, as a pHYs chunk may come after it. The stream
    /// is not closed.
    /// </summary>
    public static ImageSize Read(Stream stream) {
        BufferedStream input = new BufferedStream(stream);
        byte[] head = new byte[4];
        int n = 0;
        while (n < 4) {
            int r = input.Read(head, n, 4 - n);
            if (r <= 0) {
                break;
            }
            n += r;
        }
        // The bytes read so far, then the rest of the stream
        Stream all = new ConcatStream(head, n, input);
        if (n >= 4 && head[0] == 0x89 && head[1] == 'P' && head[2] == 'N' && head[3] == 'G') {
            PNGImage png = PNGImage.ReadSize(all);
            return new ImageSize(png.GetWidth(), png.GetHeight(),
                    png.GetPhysicalWidth(), png.GetPhysicalHeight(), false);
        }
        if (n >= 2 && head[0] == 0xFF && head[1] == 0xD8) {
            JPGImage jpg = new JPGImage(all, true);
            // Turned a quarter of the way, as Image's SetOrientation turns it
            return new ImageSize(jpg.GetWidth(), jpg.GetHeight(),
                    jpg.GetPhysicalWidth(), jpg.GetPhysicalHeight(),
                    jpg.orientation >= 5 && jpg.orientation <= 8);
        }
        if (n >= 2 && head[0] == 'B' && head[1] == 'M') {
            BMPImage bmp = new BMPImage(all, true);
            return new ImageSize(bmp.GetWidth(), bmp.GetHeight(),
                    bmp.GetPhysicalWidth(), bmp.GetPhysicalHeight(), false);
        }
        throw new Exception("The image is not a PNG, JPEG or BMP file.");
    }

    /// <summary>The width the image is drawn at, in points, as Image's GetWidth before it is scaled.</summary>
    public float GetWidth() {
        return width;
    }

    /// <summary>The height the image is drawn at, in points, as Image's GetHeight before it is scaled.</summary>
    public float GetHeight() {
        return height;
    }

    /// <summary>The width of the image in pixels, as stored.</summary>
    public int GetPixelWidth() {
        return pixelWidth;
    }

    /// <summary>The height of the image in pixels, as stored.</summary>
    public int GetPixelHeight() {
        return pixelHeight;
    }

    // The first bytes of a stream, read to tell the kind of the image, then
    // the rest of it, read on
    private sealed class ConcatStream : Stream {
        private readonly byte[] head;
        private readonly int count;
        private int at;
        private readonly Stream rest;

        internal ConcatStream(byte[] head, int count, Stream rest) {
            this.head = head;
            this.count = count;
            this.rest = rest;
        }

        public override int Read(byte[] buffer, int offset, int length) {
            if (at < count) {
                int n = Math.Min(length, count - at);
                Array.Copy(head, at, buffer, offset, n);
                at += n;
                return n;
            }
            return rest.Read(buffer, offset, length);
        }

        public override int ReadByte() {
            if (at < count) {
                return head[at++];
            }
            return rest.ReadByte();
        }

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }
        public override void Flush() {
        }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int length) => throw new NotSupportedException();
    }
}
}   // End of namespace PDFjet.NET
