/**
 * JPGImage.cs
 *
 * The authors make NO WARRANTY or representation, either express or implied,
 * with respect to this software, its quality, accuracy, merchantability, or
 * fitness for a particular purpose.  This software is provided "AS IS", and you,
 * its user, assume the entire risk as to its quality and accuracy.
 *
 * This software is copyright (C) 1991-1998, Thomas G. Lane.
 * All Rights Reserved except as specified below.
 *
 * Permission is hereby granted to use, copy, modify, and distribute this
 * software (or portions thereof) for any purpose, without fee, subject to these
 * conditions:
 * (1) If any part of the source code for this software is distributed, then this
 * README file must be included, with this copyright and no-warranty notice
 * unaltered; and any additions, deletions, or changes to the original files
 * must be clearly indicated in accompanying documentation.
 * (2) If only executable code is distributed, then the accompanying
 * documentation must state that "this software is based in part on the work of
 * the Independent JPEG Group".
 * (3) Permission for use of this software is granted only if the user accepts
 * full responsibility for any undesirable consequences; the authors accept
 * NO LIABILITY for damages of any kind.
 *
 * These conditions apply to any software derived from or based on the IJG code,
 * not just to the unmodified library.  If you use our work, you ought to
 * acknowledge us.
 *
 * Permission is NOT granted for the use of any IJG author's name or company name
 * in advertising or publicity relating to this software or products derived from
 * it.  This software may be referred to only as "the Independent JPEG Group's
 * software".
 *
 * We specifically permit and encourage the use of this software as the basis of
 * commercial products, provided that all warranty or liability claims are
 * assumed by the product vendor.
 */
using System;
using System.IO;


/**
 * Used to embed JPG images in the PDF document.
 *
 */
namespace PDFjet.NET {
class JPGImage {

    const char M_SOF0  = (char) 0x00C0;  // Start Of Frame N
    const char M_SOF1  = (char) 0x00C1;  // N indicates which compression process
    const char M_SOF2  = (char) 0x00C2;  // Only SOF0-SOF2 are now in common use
    const char M_SOF3  = (char) 0x00C3;
    const char M_SOF5  = (char) 0x00C5;  // NB: codes C4 and CC are NOT SOF markers
    const char M_SOF6  = (char) 0x00C6;
    const char M_SOF7  = (char) 0x00C7;
    const char M_SOF9  = (char) 0x00C9;
    const char M_SOF10 = (char) 0x00CA;
    const char M_SOF11 = (char) 0x00CB;
    const char M_SOF13 = (char) 0x00CD;
    const char M_SOF14 = (char) 0x00CE;
    const char M_SOF15 = (char) 0x00CF;

    int width;      // The image width in pixels
    int height;     // The image height in pixels
    int colorComponents;
    byte[] data;


    public JPGImage(Stream stream) {
        MemoryStream baos = new MemoryStream();
        byte[] buf = new byte[4096];
        int count;
        while ((count = stream.Read(buf, 0, buf.Length)) > 0) {
            baos.Write(buf, 0, count);
        }
        stream.Dispose();
        data = baos.ToArray();
        ReadJPGImage(new MemoryStream(data));
    }


    internal int GetWidth() {
        return this.width;
    }


    internal int GetHeight() {
        return this.height;
    }


    public long GetFileSize() {
        return this.data.Length;
    }


    internal int GetColorComponents() {
        return this.colorComponents;
    }


    internal byte[] GetData() {
        return this.data;
    }


    private void ReadJPGImage(System.IO.Stream stream) {
        char ch1 = (char) stream.ReadByte();
        char ch2 = (char) stream.ReadByte();
        if (ch1 != 0x00FF || ch2 != 0x00D8) {
            throw new Exception("Error: Invalid JPEG header.");
        }

        bool foundSOFn = false;
        while (true) {
            char ch = NextMarker(stream);
            switch (ch) {
                // Note that marker codes 0xC4, 0xC8, 0xCC are not,
                // and must not be treated as SOFn. C4 in particular
                // is actually DHT.
                case M_SOF0:    // Baseline
                case M_SOF1:    // Extended sequential, Huffman
                case M_SOF2:    // Progressive, Huffman
                case M_SOF3:    // Lossless, Huffman
                case M_SOF5:    // Differential sequential, Huffman
                case M_SOF6:    // Differential progressive, Huffman
                case M_SOF7:    // Differential lossless, Huffman
                case M_SOF9:    // Extended sequential, arithmetic
                case M_SOF10:   // Progressive, arithmetic
                case M_SOF11:   // Lossless, arithmetic
                case M_SOF13:   // Differential sequential, arithmetic
                case M_SOF14:   // Differential progressive, arithmetic
                case M_SOF15:   // Differential lossless, arithmetic
                // Skip 3 bytes to get to the image height and width
                stream.ReadByte();
                stream.ReadByte();
                stream.ReadByte();
                height = GetUInt16(stream);
                width = GetUInt16(stream);
                colorComponents = stream.ReadByte();
                foundSOFn = true;
                break;

                default:
                SkipVariable(stream);
                break;
            }

            if (foundSOFn) {
                break;
            }
        }
    }


    private int GetUInt16(System.IO.Stream stream) {
        return stream.ReadByte() << 8 | stream.ReadByte();
    }


    // Find the next JPEG marker and return its marker code.
    // We expect at least one FF byte, possibly more if the compressor
    // used FFs to pad the file.
    // There could also be non-FF garbage between markers. The treatment
    // of such garbage is unspecified; we choose to skip over it but
    // emit a warning msg.
    // NB: this routine must not be used after seeing SOS marker, since
    // it will not deal correctly with FF/00 sequences in the compressed
    // image data...
    private char NextMarker(System.IO.Stream stream) {
        // Find 0xFF byte; count and skip any non-FFs.
        char ch = (char) stream.ReadByte();
        if (ch != 0x00FF) {
            throw new Exception("0xFF byte expected.");
        }

        // Get marker code byte, swallowing any duplicate FF bytes.
        // Extra FFs are legal as pad bytes, so don't count them in discarded_bytes.
        do {
            ch = (char) stream.ReadByte();
        }
        while (ch == 0x00FF);

        return ch;
    }


    // Most types of marker are followed by a variable-length parameter
    // segment. This routine skips over the parameters for any marker we
    // don't otherwise want to process.
    // Note that we MUST skip the parameter segment explicitly in order
    // not to be fooled by 0xFF bytes that might appear within the
    // parameter segment such bytes do NOT introduce new markers.
    private void SkipVariable(System.IO.Stream stream) {
        // Get the marker parameter length count
        int length = GetUInt16(stream);
        if (length < 2) {
            // Length includes itself, so must be at least 2
            throw new Exception();
        }
        length -= 2;

        // Skip over the remaining bytes
        while (length > 0) {
            stream.ReadByte();
            length--;
        }
    }

}   // End of JPGImage.cs
}   // End of namespace PDFjet.NET
