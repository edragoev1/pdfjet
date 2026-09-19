/*
 * DecompressorTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertArrayEquals;
import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;

import java.nio.charset.StandardCharsets;
import java.util.Arrays;
import java.util.Random;
import org.junit.jupiter.api.Test;

/** The stream filters and predictors that reading a PDF needs, and the compressor. */
class DecompressorTest {
    private static byte[] bytes(int... values) {
        byte[] result = new byte[values.length];
        for (int i = 0; i < values.length; i++) {
            result[i] = (byte) values[i];
        }
        return result;
    }

    private static byte[] ascii(String text) {
        return text.getBytes(StandardCharsets.US_ASCII);
    }

    @Test
    void lzwDecodesTheExampleOfTheStandard() throws Exception {
        // ISO 32000-1, 7.4.4.2: the encoding of "-----A---B".
        byte[] encoded = bytes(0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01);
        assertEquals("-----A---B", TestSupport.latin1(Decompressor.lzwDecode(encoded)));
    }

    @Test
    void asciiHexDecodeSkipsWhitespaceAndStopsAtTheEndMarker() {
        assertEquals("Hello", TestSupport.latin1(Decompressor.asciiHexDecode(ascii("48 65 6C6C6F>"))));
    }

    @Test
    void asciiHexDecodeReadsAMissingLastDigitAsZero() {
        assertArrayEquals(bytes(0x70), Decompressor.asciiHexDecode(ascii("7>")));
    }

    @Test
    void ascii85DecodesWithAndWithoutThePrefix() {
        // base64.a85encode(b"Hello world") in Python.
        assertEquals("Hello world", TestSupport.latin1(Decompressor.ascii85Decode(ascii("87cURD]j7BEbo7~>"))));
        assertEquals("Hello world", TestSupport.latin1(Decompressor.ascii85Decode(ascii("<~87cURD]j7BEbo7~>"))));
    }

    @Test
    void ascii85DecodesZAsFourZeroBytesAndAPartialLastGroup() {
        assertArrayEquals(bytes(0, 0, 0, 0), Decompressor.ascii85Decode(ascii("z~>")));
        assertArrayEquals(bytes(0, 0, 0, 0, 'a', 'b'), Decompressor.ascii85Decode(ascii("z@:B~>")));
    }

    @Test
    void runLengthDecodeCopiesLiteralsAndRepeatsRuns() throws Exception {
        byte[] encoded = bytes(2, 'a', 'b', 'c', 254, 'x', 128);
        assertEquals("abcxxx", TestSupport.latin1(Decompressor.runLengthDecode(encoded)));
    }

    @Test
    void pngPredictorsUndoEachRowFilter() {
        // Each row is the filter type and three one byte samples.
        assertArrayEquals(bytes(1, 2, 3), Decompressor.applyPredictor(bytes(1, 1, 1, 1), 11, 1, 8, 3));
        assertArrayEquals(bytes(1, 2, 3, 2, 3, 4), Decompressor.applyPredictor(bytes(0, 1, 2, 3, 2, 1, 1, 1), 12, 1, 8, 3));
        assertArrayEquals(bytes(2, 5, 8), Decompressor.applyPredictor(bytes(3, 2, 4, 6), 13, 1, 8, 3));
        assertArrayEquals(bytes(1, 2, 3), Decompressor.applyPredictor(bytes(4, 1, 1, 1), 14, 1, 8, 3));
    }

    @Test
    void tiffPredictorAddsTheSampleToTheLeft() {
        assertArrayEquals(bytes(1, 2, 3, 5, 5, 5), Decompressor.applyPredictor(bytes(1, 1, 1, 5, 0, 0), 2, 1, 8, 3));
    }

    @Test
    void predictorOneAndInvalidParametersLeaveTheDataAsItIs() {
        byte[] data = bytes(9, 8, 7);
        assertArrayEquals(data, Decompressor.applyPredictor(data, 1, 1, 8, 3));
        assertArrayEquals(data, Decompressor.applyPredictor(data, 12, 0, 8, 3));
    }

    @Test
    void inflateUndoesDeflate() throws Exception {
        byte[] data = new byte[100000];
        new Random(42).nextBytes(data);
        Arrays.fill(data, 50000, 90000, (byte) 'x');
        assertArrayEquals(data, Decompressor.inflate(Compressor.deflate(data)));
    }

    @Test
    void inflateRejectsATruncatedStream() {
        byte[] deflated = Compressor.deflate(ascii("hello hello hello hello"));
        assertThrows(Exception.class, () -> Decompressor.inflate(Arrays.copyOf(deflated, 6)));
    }

    @Test
    void inflateIgnoresBytesAfterTheEndOfAStream() throws Exception {
        byte[] data = ascii("hello hello hello hello");
        byte[] deflated = Compressor.deflate(data);
        byte[] padded = Arrays.copyOf(deflated, deflated.length + 3);
        padded[deflated.length] = '\r';
        padded[deflated.length + 1] = '\n';
        padded[deflated.length + 2] = 0x42;
        assertArrayEquals(data, Decompressor.inflate(padded));
    }

    @Test
    void inflateRejectsEveryTruncationOfAStream() throws Exception {
        byte[] data = new byte[2000];
        new Random(3).nextBytes(data);
        Arrays.fill(data, 1000, 2000, (byte) 'a');
        byte[] deflated = Compressor.deflate(data);
        assertArrayEquals(data, Decompressor.inflate(deflated));
        // The last 4 bytes are the Adler-32 checksum, which is checked too.
        for (int length = 0; length < deflated.length; length++) {
            final byte[] truncated = Arrays.copyOf(deflated, length);
            assertThrows(Exception.class, () -> Decompressor.inflate(truncated), "length " + length);
        }
    }

    @Test
    void inflateRejectsAWrongChecksumOrHeader() throws Exception {
        byte[] deflated = Compressor.deflate("PDFjet PDFjet PDFjet".getBytes(StandardCharsets.US_ASCII));
        final byte[] wrongChecksum = deflated.clone();
        wrongChecksum[wrongChecksum.length - 1] ^= 1;
        assertThrows(Exception.class, () -> Decompressor.inflate(wrongChecksum));
        final byte[] wrongMethod = deflated.clone();
        wrongMethod[0] = 0x77;     // Method 7, and the check no longer fits
        assertThrows(Exception.class, () -> Decompressor.inflate(wrongMethod));
        final byte[] wrongCheck = deflated.clone();
        wrongCheck[1] ^= 1;
        assertThrows(Exception.class, () -> Decompressor.inflate(wrongCheck));
    }

    @Test
    void inflatePrefixWithItsBytesNeedsNoChecksum() throws Exception {
        byte[] data = "PDFjet PDFjet PDFjet".getBytes(StandardCharsets.US_ASCII);
        final byte[] deflated = Compressor.deflate(data);
        deflated[deflated.length - 1] ^= 1;
        assertArrayEquals(Arrays.copyOf(data, 5), Decompressor.inflatePrefix(deflated, 5));
        assertThrows(Exception.class, () -> Decompressor.inflatePrefix(deflated, 100));
    }

    @Test
    void theDecodedLengthLimitIs256MiB() {
        assertEquals(256 * 1024 * 1024, Decompressor.MAX_DECODED_LENGTH);
    }

    @Test
    void inflateRejectsDataThatDecodesToMoreThanTheLimit() throws Exception {
        final byte[] deflated = Compressor.deflate(new byte[1000]);
        assertEquals(1000, Decompressor.inflate(deflated, 1000).length);
        Exception e = assertThrows(Exception.class, () -> Decompressor.inflate(deflated, 999));
        assertEquals("Flate data decodes to more than 999 bytes", e.getMessage());
    }

    @Test
    void inflatePrefixReturnsTheFirstBytesAndIgnoresTheRest() throws Exception {
        byte[] data = ascii("hello hello hello hello");
        final byte[] deflated = Compressor.deflate(data);
        assertArrayEquals(Arrays.copyOf(data, 5), Decompressor.inflatePrefix(deflated, 5));
        assertArrayEquals(data, Decompressor.inflatePrefix(deflated, 100));
        assertThrows(Exception.class, () -> Decompressor.inflatePrefix(Arrays.copyOf(deflated, 6), 20));
    }

    @Test
    void lzwDecodeRejectsDataThatDecodesToMoreThanTheLimit() throws Exception {
        final byte[] encoded = bytes(0x80, 0x0B, 0x60, 0x50, 0x22, 0x0C, 0x0C, 0x85, 0x01);
        assertEquals(10, Decompressor.lzwDecode(encoded, 10).length);
        Exception e = assertThrows(Exception.class, () -> Decompressor.lzwDecode(encoded, 9));
        assertEquals("LZW data decodes to more than 9 bytes", e.getMessage());
    }

    @Test
    void runLengthDecodeRejectsDataThatDecodesToMoreThanTheLimit() throws Exception {
        final byte[] encoded = bytes(2, 'a', 'b', 'c', 254, 'x', 128);
        assertEquals(6, Decompressor.runLengthDecode(encoded, 6).length);
        Exception e = assertThrows(Exception.class, () -> Decompressor.runLengthDecode(encoded, 5));
        assertEquals("RunLength data decodes to more than 5 bytes", e.getMessage());
    }

    @Test
    void deflateOfNoBytesIsAnEmptyZlibStream() {
        byte[] deflated = Compressor.deflate(new byte[0]);
        assertEquals(8, deflated.length);
        assertEquals(0x78, deflated[0] & 0xFF);
    }

    @Test
    void inflateAcceptsAnEmptyStream() throws Exception {
        assertEquals(0, Decompressor.inflate(Compressor.deflate(new byte[0])).length);
    }
}
