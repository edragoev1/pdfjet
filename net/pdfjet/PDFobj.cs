/*
 * PDFobj.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// An object of a PDF that was read with PDF.Read, which holds the tokens of
/// its dictionary and its stream. See Example_34, Example_48 and Example_50.
/// </summary>
public class PDFobj {
    internal int number;           // The object number
    internal int offset;           // The object offset
    internal int end;              // Where the object ends at the latest
    internal int read;             // The offset up to which its tokens were read
    internal List<String> dict;
    internal int streamOffset;
    internal byte[] stream;        // The compressed stream
    internal byte[] data;          // The decompressed data
    internal bool undecoded;       // The stream is decoded when the data is first asked for
    internal DecodeBudget budget;  // What the streams of the PDF this was read from may still decode to
    internal int gsNumber = -1;
    internal bool root;            // The catalog that the trailer's /Root names
    internal Font font;            // A font added to the objects, filled in when they are added to a PDF
    // The /Resources a page inherits from the page tree, when it is a large
    // dictionary in a node: shared by the pages under the node, not copied
    // into each (see PDF.AddInheritedEntries)
    internal List<String> inheritedResources;
    internal int inheritedFrom;     // The number of that node
    // The objects of the PDF it was read from, by number, for the masks of an
    // image copied into another PDF; null for an object not read
    internal Dictionary<int, PDFobj> readObjects;
    // The number in the PDF being written of an object that a page that was
    // read uses, like a stamp: this object stands for it among the objects,
    // and only Merge reads the number
    internal int writtenNumber;

    /// <summary>
    /// Creates an object with an empty dictionary.
    /// </summary>
    internal PDFobj() {
        this.dict = new List<String>();
    }

    /// <summary>Returns the object number.</summary>
    public int GetNumber() {
        return this.number;
    }

    /// <summary>Returns the tokens of the object dictionary.</summary>
    public List<String> GetDict() {
        return this.dict;
    }

    /// <summary>
    /// Returns the decompressed stream data, or null when there is none. A
    /// stream that is not a cross-reference or an object stream is decoded
    /// the first time its data is asked for, and the data is kept in the
    /// object: the first call changes the object, so two threads do not call
    /// it at once on the same object. The streams of one PDF that Read
    /// returns share a budget of 256 MiB of decoded data, MAX_DECODED_TOTAL:
    /// a stream past what is left of it, or one that cannot be decoded, has
    /// no data.
    /// </summary>
    public byte[] GetData() {
        if (undecoded) {
            // A stream that is not a cross-reference or an object stream is
            // decoded when its data is first asked for, not when the PDF is
            // read, so that reading a PDF of large images takes the memory
            // of none of them.
            undecoded = false;
            try {
                data = DecodeStream();
            } catch (Exception) {
                data = null;    // A stream that cannot be decoded has no data.
            }
        }
        return this.data;
    }

    // What all the streams of one PDF may decode to together, 256 MiB: a PDF
    // of a few megabytes can hold hundreds of streams that each decode to
    // hundreds of megabytes, and each one alone is within the limit of a
    // stream. Not a constant, for the tests.
    internal static int MAX_DECODED_TOTAL = Decompressor.MAX_DECODED_LENGTH;

    // How many times the length of a PDF the reader may read in all to find
    // the objects that its cross-reference sections list and the endstream of
    // the streams whose /Length is wrong. Each of those reads ends where its
    // object does, but a PDF can list one object many times, and the last
    // object of a section ends at the end of the PDF.
    internal const int READ_PER_BYTE = 8;

    // The bytes of a PDF for each object of its object streams that takes
    // nothing of the budget, as many as a PDF of its length can hold of its own;
    // those past them take PDF.OBJECT_COST of it. Each one took it, and a real PDF of 5 MB
    // of 1.1 million objects was refused (review of 12 October 2026, R32).
    internal const int BYTES_PER_FREE_OBJECT = 8;

    // What the streams of one PDF may still decode to, shared by its objects.
    // It also holds what the reader may still read of the PDF, READ_PER_BYTE
    // times its length, so that no PDF is read in more than linear time:
    // thousands of streams with a wrong /Length and no endstream each searched
    // to the end of the PDF.
    internal sealed class DecodeBudget {
        internal int left = MAX_DECODED_TOTAL;
        internal int readLeft;
        internal int freeObjects;

        internal DecodeBudget(int size) {
            this.readLeft = (int) Math.Min((long) READ_PER_BYTE * size, int.MaxValue);
            this.freeObjects = size / BYTES_PER_FREE_OBJECT;
        }
    }

    // The error of a PDF whose streams decode to more than the budget together.
    internal sealed class DecodedTotalException : Exception {
        internal DecodedTotalException() :
                base("the streams of the PDF decode to more than " + MAX_DECODED_TOTAL + " bytes together") {
        }
    }

    // Copies the stream from the buffer, and decodes it with the filters of
    // its /Filter entry.
    internal void SetStreamAndData(byte[] buf, int length, DecodeBudget budget) {
        SetStreamAndData(buf, length, null, budget);
    }

    // Copies the stream from the buffer, decrypts it when the PDF is encrypted,
    // and decodes it with the filters of its /Filter entry. The decrypted
    // stream replaces the encrypted one, so that it can be copied.
    internal void SetStreamAndData(byte[] buf, int length, Decryptor decryptor, DecodeBudget budget) {
        if (this.stream == null) {
            this.budget = budget;
            int actual = StreamLength(buf, streamOffset, length, end, budget);
            if (actual != length) {
                length = actual;
                SetLength(length);
            }
            // A /Length the PDF does not have is checked before the stream is
            // made, so that a file of a few bytes that says its stream is a
            // gigabyte takes no memory.
            if (length < 0 || streamOffset < 0 || length > buf.Length - streamOffset) {
                throw new InvalidDataException("The stream of an object is not in the PDF.");
            }
            this.stream = new byte[length];
            Array.Copy(buf, streamOffset, stream, 0, length);
            if (decryptor != null) {
                this.stream = decryptor.DecryptStream(this, stream);
                SetLength(stream.Length);
            }
            String type = GetValue("/Type");
            if (type.Equals("/XRef") || type.Equals("/ObjStm")) {
                this.data = DecodeStream();     // The objects in it are read now
            } else {
                this.undecoded = true;
            }
        }
    }

    // Returns the decoded stream. A cross-reference stream or an object stream
    // that cannot be decoded throws, as the objects in it cannot be read. Any
    // other stream that cannot be decoded, like an image or the content of a
    // page that is cut short, has no data: the rest of the PDF is read, and a
    // merge copies the stream as it is.
    private byte[] DecodeStream() {
        String type = GetValue("/Type");
        if (type.Equals("/XRef") || type.Equals("/ObjStm")) {
            try {
                return Decode(stream);
            } catch (Exception e) {
                if (budget != null && budget.left < Decompressor.MAX_DECODED_LENGTH &&
                        e.Message != null && e.Message.Contains("decodes to more than")) {
                    throw new DecodedTotalException();
                }
                throw;
            }
        }
        try {
            return Decode(stream);
        } catch (InvalidDataException) {
            return null;
        }
    }

    // Returns the length of the stream that starts at the offset. It is the
    // /Length when the endstream keyword follows it, as it must; when it does
    // not -- a /Length that is missing, too short or too long -- the stream
    // ends at the end of line before the next endstream, as MuPDF and pdf.js
    // read it. A stream with no endstream after it keeps its /Length.
    //
    // The endstream is looked for before end, where the object ends at the
    // latest, and within what is left of what the budget may read: a PDF of
    // thousands of streams with a wrong /Length and no endstream was searched
    // to its end for each of them. A search the budget does not finish keeps
    // the /Length too.
    private static int StreamLength(byte[] buf, int offset, int length, int end, DecodeBudget budget) {
        if (offset < 0 || offset > buf.Length || EndstreamAfter(buf, offset, length, budget) != -1) {
            return length;
        }
        end = (int) Math.Max(Math.Min(Math.Min(end, buf.Length), (long) offset + budget.readLeft), offset);
        int found = EndstreamOf(buf, offset, end);
        if (found == -1) {
            found = PDF.IndexOf(buf, "endstream", offset, end);
        }
        if (found == -1) {
            budget.readLeft -= end - offset;
            return length;
        }
        budget.readLeft -= found - offset;
        if (found > offset && buf[found - 1] == '\n') {
            found--;
        }
        if (found > offset && buf[found - 1] == '\r') {
            found--;
        }
        return found - offset;
    }

    // Returns the offset of the endstream keyword that ends the stream that
    // starts at from, before to, or -1: the first endstream that ends no stream
    // nested in it, as the stream of an embedded PDF, unfiltered, holds objects
    // and streams of its own, each a stream keyword after a dictionary and an
    // endstream, and the first endstream would cut it short. The endstream is
    // the stream's only when every object that started in the stream has
    // ended: one inside an object still open is that object's, whose stream
    // keyword was not found, and the search gives up, -1, for the caller to
    // read the stream as before.
    internal static int EndstreamOf(byte[] buf, int from, int to) {
        to = Math.Min(to, buf.Length);
        int streams = 0;
        int objects = 0;
        for (int i = Math.Max(from, 0); i < to; i++) {
            if (buf[i] == 's' && i + 6 <= to && PDF.StartsWith(buf, i, "stream")) {
                if (i - 3 >= from && PDF.StartsWith(buf, i - 3, "end")) {
                    if (streams == 0) {
                        return (objects == 0) ? i - 3 : -1;
                    }
                    streams--;
                } else if (OpensStream(buf, i)) {
                    streams++;
                }
            } else if (buf[i] == 'e' && PDF.StartsWith(buf, i, "endobj")) {
                if (objects > 0) {
                    objects--;
                }
            } else if (buf[i] >= '0' && buf[i] <= '9' && PDF.IsObjectStart(buf, i)) {
                objects++;
                while (i < to && buf[i] != 'j') {     // Past the obj
                    i++;
                }
            }
        }
        return -1;
    }

    // Returns true when the stream keyword at the offset opens a stream: after
    // the >> of its dictionary and white space, and before an end of line, as
    // the syntax of a stream object has it.
    private static bool OpensStream(byte[] buf, int off) {
        int after = off + 6;
        if (after >= buf.Length || (buf[after] != '\r' && buf[after] != '\n')) {
            return false;
        }
        int i = off - 1;
        while (i >= 0 && PDF.IsWhiteSpace(buf[i])) {
            i--;
        }
        return i >= 1 && buf[i] == '>' && buf[i - 1] == '>';
    }

    // Returns the offset after the endobj keyword that follows the endstream
    // at the offset, after white space, or -1.
    internal static int EndobjAfter(byte[] buf, int endstream) {
        int i = endstream + 9;
        while (i < buf.Length && PDF.IsWhiteSpace(buf[i])) {
            i++;
        }
        return PDF.StartsWith(buf, i, "endobj") ? i + 6 : -1;
    }

    // Returns the offset of the endstream keyword that follows the /Length of
    // the stream at the offset, after white space, or -1. The white space is
    // taken from the budget, as streams whose /Length ends in the same run of
    // white space each read it.
    internal static int EndstreamAfter(byte[] buf, int offset, int length, DecodeBudget budget) {
        if (offset < 0 || length < 0 || length > buf.Length - offset) {
            return -1;
        }
        int i = offset + length;
        int end = (int) Math.Min(buf.Length, (long) i + Math.Max(budget.readLeft, 0));
        while (i < end && PDF.IsWhiteSpace(buf[i])) {
            i++;
        }
        budget.readLeft -= i - (offset + length);
        return PDF.StartsWith(buf, i, "endstream") ? i : -1;
    }

    // Sets the /Length of the stream, replacing a reference to the length,
    // and adds it to a stream dictionary that has none.
    private void SetLength(int length) {
        int i = dict.IndexOf("/Length");
        if (i == -1) {
            int open = dict.IndexOf("<<");
            if (open != -1) {
                dict.InsertRange(open + 1, new String[] {"/Length", length.ToString(CultureInfo.InvariantCulture)});
            }
            return;
        }
        if (i + 1 >= dict.Count) {
            return;
        }
        if (i + 3 < dict.Count && dict[i + 3].Equals("R")) {
            dict.RemoveAt(i + 3);
            dict.RemoveAt(i + 2);
        }
        dict[i + 1] = length.ToString(CultureInfo.InvariantCulture);
    }

    // Decodes the stream with each filter of its /Filter entry in turn. A
    // filter that is not supported, like DCTDecode, ends the decoding, and
    // the data is what the filters before it decoded.
    private byte[] Decode(byte[] stream) {
        List<String> filters = GetFilters();
        byte[] decoded = stream;
        for (int i = 0; i < filters.Count; i++) {
            String filter = filters[i];
            if (filter.Equals("/FlateDecode") || filter.Equals("/Fl")) {
                decoded = ApplyDecodeParms(Decoded(Decompressor.InflateStream(decoded, MaxDecodedLength())), i);
            } else if (filter.Equals("/LZWDecode") || filter.Equals("/LZW")) {
                int earlyChange = GetDecodeParm(GetDecodeParms(i), "/EarlyChange", 1);
                decoded = ApplyDecodeParms(Decoded(Decompressor.LZWDecode(decoded, MaxDecodedLength(), earlyChange)), i);
            } else if (filter.Equals("/ASCIIHexDecode") || filter.Equals("/AHx")) {
                decoded = DecodedHex(Decompressor.ASCIIHexDecode(decoded));
            } else if (filter.Equals("/ASCII85Decode") || filter.Equals("/A85")) {
                decoded = Decoded(Decompressor.ASCII85Decode(decoded, MaxDecodedLength()));
            } else if (filter.Equals("/RunLengthDecode") || filter.Equals("/RL")) {
                decoded = Decoded(Decompressor.RunLengthDecode(decoded, MaxDecodedLength()));
            } else if (!filter.Equals("/Crypt")) {  // DecryptStream reads that one (review
                break;                              // of 30 October 2026, B10)
            }
        }
        return decoded;
    }

    // Returns what a stream of the object may decode to: the limit of a
    // stream, or what is left of the PDF's budget when that is less. A budget
    // with nothing left decodes nothing, which the decoders report as too long.
    private int MaxDecodedLength() {
        if (budget != null && budget.left < Decompressor.MAX_DECODED_LENGTH) {
            return budget.left;
        }
        return Decompressor.MAX_DECODED_LENGTH;
    }

    // Takes the decoded data from the PDF's budget and returns it.
    private byte[] Decoded(byte[] data) {
        if (budget != null) {
            budget.left = Math.Max(0, budget.left - data.Length);
        }
        return data;
    }

    // Takes the output of ASCIIHexDecode from the PDF's budget, as the other
    // filters' is (review of 11 October 2026, #32). ASCII85Decode stops at the
    // budget itself, as a 'z' of it is four bytes.
    private byte[] DecodedHex(byte[] data) {
        if (data.Length > MaxDecodedLength()) {
            throw new InvalidDataException(
                    "the stream decodes to more than " + MaxDecodedLength() + " bytes");
        }
        return Decoded(data);
    }

    // Returns the filters of the stream, whose /Filter can be an object of its
    // own, a name or an array: the data was the bytes as they are.
    private List<String> GetFilters() {
        int i = dict.IndexOf("/Filter") + 1;
        if (i == 0 || !PDF.IsReference(dict, i)) {
            return GetValues("/Filter");
        }
        List<String> filters = new List<String>();
        PDFobj filter;
        if (readObjects != null
                && readObjects.TryGetValue(Int32.Parse(dict[i], CultureInfo.InvariantCulture), out filter)) {
            foreach (String token in PDF.ValueOf(filter)) {
                if (token != "[" && token != "]") {
                    filters.Add(token);
                }
            }
        }
        return filters;
    }

    // Returns true for a stream with a /Crypt filter that is the Identity one,
    // whose name is the default: the stream is not encrypted.
    internal bool HasIdentityCrypt() {
        List<String> filters = GetFilters();
        for (int i = 0; i < filters.Count; i++) {
            if (filters[i].Equals("/Crypt")) {
                List<String> parms = GetDecodeParms(i);
                int j = parms.IndexOf("/Name");
                return j == -1 || TokenAt(parms, j + 1).Equals("/Identity");
            }
        }
        return false;
    }

    // Returns the elements of the array that is the value of the key, or the
    // value itself when it is not an array.
    private List<String> GetValues(String key) {
        List<String> values = new List<String>();
        int i = dict.IndexOf(key) + 1;
        if (i == 0 || i >= dict.Count) {
            return values;
        }
        if (!dict[i].Equals("[")) {
            values.Add(dict[i]);
            return values;
        }
        for (i += 1; i < dict.Count && !dict[i].Equals("]"); i++) {
            values.Add(dict[i]);
        }
        return values;
    }

    // Undoes the predictor in the parameters of the filter at the index, of
    // an image too: the data of an image is its samples, which a copy of the
    // image does not use, as it copies the stream.
    private byte[] ApplyDecodeParms(byte[] decoded, int index) {
        List<String> parms = GetDecodeParms(index);
        return Decompressor.ApplyPredictor(
                decoded,
                GetDecodeParm(parms, "/Predictor", 1),
                GetDecodeParm(parms, "/Colors", 1),
                GetDecodeParm(parms, "/BitsPerComponent", 8),
                GetDecodeParm(parms, "/Columns", 1));
    }

    // Returns the tokens of the parameters dictionary of the filter at the
    // index. /DecodeParms is a dictionary for a single filter, or an array
    // with a dictionary or null for each filter.
    private List<String> GetDecodeParms(int index) {
        List<String> parms = new List<String>();
        int i = dict.IndexOf("/DecodeParms") + 1;
        if (i == 0 || i >= dict.Count) {
            return parms;
        }
        bool array = dict[i].Equals("[");
        if (array) {
            i += 1;
        }
        for (int element = 0; i < dict.Count && !dict[i].Equals("]"); element++) {
            int start = i;
            if (dict[i].Equals("<<")) {
                int level = 0;
                do {
                    String token = dict[i++];
                    if (token.Equals("<<")) {
                        level++;
                    } else if (token.Equals(">>")) {
                        level--;
                    }
                } while (level > 0 && i < dict.Count);
            } else if (i + 2 < dict.Count && dict[i + 2].Equals("R")) {
                i += 3;     // A reference to a dictionary, which is not supported.
            } else {
                i += 1;     // null
            }
            if (element == index) {
                if (dict[start].Equals("<<")) {
                    parms.AddRange(dict.GetRange(start, i - start));
                }
                break;
            }
            if (!array) {
                break;
            }
        }
        return parms;
    }

    // Returns the integer value of the key in the parameters dictionary.
    private static int GetDecodeParm(List<String> parms, String key, int defaultValue) {
        int i = parms.IndexOf(key);
        if (i != -1 && i + 1 < parms.Count) {
            return Int32.TryParse(parms[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : defaultValue;
        }
        return defaultValue;
    }

    internal void SetStream(byte[] stream) {
        this.stream = stream;
    }

    internal void SetNumber(int number) {
        this.number = number;
    }

    /// <summary>
    /// Returns the parameter value given the specified key. The key is an
    /// entry of the dictionary of the object, and not of a dictionary inside
    /// it: the /Type of a /Group before the /Type of a page is not the type of
    /// the page.
    /// </summary>
    /// <param name="key">the specified key.</param>
    /// <returns>the value, or "" when the object has no such key.</returns>
    public String GetValue(String key) {
        int open = dict.IndexOf("<<");
        if (open == -1) {
            return "";
        }
        int i = PDF.EntryIndex(dict.GetRange(open, dict.Count - open), key);
        if (i == -1) {
            return "";
        }
        i += open;
        if (i + 1 >= dict.Count) {
            return "";
        }
        String token = dict[i + 1];
        if (token.Equals("<<")) {
            return ValueUpTo(i + 2, ">>", "<< ");
        } else if (token.Equals("[")) {
            return ValueUpTo(i + 2, "]", "[ ");
        }
        return token;
    }

    // Returns the tokens of the value of the key: one, a reference to an
    // object, as 5 0 R, or an array or a dictionary with the arrays and
    // dictionaries in it, from its opening token to its closing one.
    internal List<String> ValueTokens(String key) {
        int open = dict.IndexOf("<<");
        if (open == -1) {
            return new List<String>();
        }
        int i = PDF.EntryIndex(dict.GetRange(open, dict.Count - open), key);
        if (i == -1) {
            return new List<String>();
        }
        int start = open + i + 1;
        if (start + 2 < dict.Count && dict[start + 2].Equals("R")) {
            return dict.GetRange(start, 3);
        }
        int depth = 0;
        for (int j = start; j < dict.Count; j++) {
            String token = dict[j];
            if (token.Equals("[") || token.Equals("<<")) {
                depth++;
            } else if (token.Equals("]") || token.Equals(">>")) {
                depth--;
            }
            if (depth <= 0) {
                return dict.GetRange(start, j + 1 - start);
            }
        }
        return dict.GetRange(start, dict.Count - start);
    }

    // Returns the tokens from the index up to the closing one, with the
    // opening before them and the closing after them.
    private String ValueUpTo(int i, String closing, String opening) {
        StringBuilder buffer = new StringBuilder();
        buffer.Append(opening);
        while (i < dict.Count && !dict[i].Equals(closing)) {
            buffer.Append(dict[i]);
            buffer.Append(" ");
            i += 1;
        }
        buffer.Append(closing);
        return buffer.ToString();
    }

    // Returns the object numbers of the key, which is one reference or an
    // array of them. A dictionary that ends in the middle of them, or a token
    // that is not a number where one belongs, ends the list: a PDF that was
    // read can hold anything.
    // The index of the key among this dictionary's own entries, not those of
    // the dictionaries in it, or -1 when it has none
    internal int EntryIndexOf(String key) {
        int open = dict.IndexOf("<<");
        if (open == -1) {
            return dict.IndexOf(key);   // Tokens with no dictionary, as an object made by hand
        }
        int i = PDF.EntryIndex(dict.GetRange(open, dict.Count - open), key);
        return i == -1 ? -1 : i + open;
    }

    internal List<Int32> GetObjectNumbers(String key) {
        List<Int32> numbers = new List<Int32>();
        // The key of this dictionary, not one of a dictionary in it: the
        // /Contents of an annotation of /Annots before the page's own was
        // taken for the page's (review of 17 October 2026)
        for (int i = EntryIndexOf(key); i >= 0 && i < dict.Count; i++) {
            String token = dict[i];
            if (token.Equals(key)) {
                if (++i >= dict.Count) {
                    break;
                }
                String str = dict[i];
                int number;
                if (str.Equals("[")) {
                    while (true) {
                        if (++i >= dict.Count) {
                            break;
                        }
                        str = dict[i];
                        if (str.Equals("]")) {
                            break;
                        }
                        if (!Int32.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) {
                            break;
                        }
                        numbers.Add(number);
                        ++i;    // 0
                        ++i;    // R
                    }
                } else if (Int32.TryParse(str, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) {
                    numbers.Add(number);
                }
                break;
            }
        }
        return numbers;
    }

    // Returns the object with the number, or null when the PDF that was read
    // does not have one: a reference can name an object that is not in the file.
    internal static PDFobj ObjectNumbered(List<PDFobj> objects, int number) {
        if (number < 1 || number > objects.Count) {
            return null;
        }
        return objects[number - 1];
    }

    /// <summary>
    /// Returns the width and height of the page, which its /MediaBox gives as
    /// the two corners of a rectangle, in either order. A page with no
    /// /MediaBox of its own, one that is not four numbers, or an empty one is
    /// letter size, as MuPDF and pdf.js draw it. PDF.GetPageObjects gives a
    /// page the box it inherits from the page tree, which a page of another
    /// program's PDF often does not carry itself, and the numbers of a box
    /// that is an object of its own or refers to them.
    /// </summary>
    public PageSize GetPageSize() {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/MediaBox")) {
                if (!TokenAt(dict, i + 1).Equals("[")) {
                    break;
                }
                float[] box = new float[4];
                for (int j = 0; j < 4; j++) {
                    if (!float.TryParse(TokenAt(dict, i + 2 + j), NumberStyles.Float,
                            CultureInfo.InvariantCulture, out box[j])) {
                        return Letter.PORTRAIT;
                    }
                }
                float width = Math.Abs(box[2] - box[0]);
                float height = Math.Abs(box[3] - box[1]);
                // A box of a NaN or infinite size is no size, as one of 0 is
                // not: a page of NaN by 792 stopped Swift where it was
                // watermarked (review of 12 October 2026, K1).
                if (!(width > 0f && height > 0f) || float.IsInfinity(width) || float.IsInfinity(height)) {
                    return Letter.PORTRAIT;
                }
                return new PageSize(width, height);
            }
        }
        return Letter.PORTRAIT;
    }

    // Returns the length of the stream. A length that is an object of its own
    // is found by its number in numbered, which holds the newest version of
    // each object: an object of a PDF that was updated is in it once, as the
    // last one of its number.
    internal int GetLength(Dictionary<int, PDFobj> numbered) {
        // The entry of the dictionary, and not a /Length inside another value
        // or that is the value of another entry, "/Height/Length", as pdf.js
        // tests it in issue19611.
        int open = dict.IndexOf("<<");
        if (open == -1) {
            return 0;
        }
        int i = PDF.EntryIndex(dict.GetRange(open, dict.Count - open), "/Length");
        if (i == -1) {
            return 0;
        }
        i += open;
        int number = ToLength(TokenAt(dict, i + 1));
        if (i + 2 >= dict.Count) {
            throw new InvalidDataException("The dictionary ends after the /Length.");
        }
        if (dict[i + 2].Equals("0")) {
            if (i + 3 >= dict.Count) {
                throw new InvalidDataException("The dictionary ends after the /Length.");
            }
            if (dict[i + 3].Equals("R")) {
                return GetLength(numbered, number);
            }
        }
        return number;
    }

    // Returns the length stored in the object with the number, or 0 when the
    // PDF has no such object.
    internal static int GetLength(Dictionary<int, PDFobj> numbered, int number) {
        PDFobj obj;
        if (!numbered.TryGetValue(number, out obj)) {
            return 0;
        }
        return ToLength(TokenAt(obj.dict, 3));
    }

    // Returns the length the token holds, which a PDF that was read can write
    // as anything. A real number and null are other programs' ways to write it,
    // and were refused: the stream ends at its endstream when the length is
    // wrong, as with null, for which it is 0 (review of 30 October 2026, B11).
    private static int ToLength(String token) {
        if (token == "null") {
            return 0;
        }
        int length;
        if (Int32.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out length)) {
            return length;
        }
        if (Util.IsDecimalNumber(token)) {
            double value = Double.Parse(token, CultureInfo.InvariantCulture);
            if (Math.Abs(value) <= Int32.MaxValue) {
                return (int) value;
            }
        }
        throw new InvalidDataException("The /Length of a stream is not a number.");
    }

    /// <summary>
    /// Returns the content object of this page, or null if it has no contents.
    /// The content of a page can be split into several streams, listed in an
    /// array that is either in the page dictionary or an object of its own.
    /// Together they are one content stream, so they are returned joined in
    /// a new object, which is not in the objects list.
    /// </summary>
    public PDFobj GetContentObject(List<PDFobj> objects) {
        List<Int32> numbers = GetObjectNumbers("/Contents");
        if (numbers.Count == 1) {
            PDFobj obj = ObjectNumbered(objects, numbers[0]);
            if (obj == null) {
                return null;
            }
            if (obj.stream != null) {
                return obj;
            }
            // "/Contents 39 0 R" where the object is "[ 41 0 R 43 0 R ]"
            numbers = new List<Int32>();
            int i = obj.dict.IndexOf("[");
            while (i != -1 && i + 3 < obj.dict.Count && obj.dict[i + 3].Equals("R")) {
                int number;
                if (!Int32.TryParse(obj.dict[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) {
                    break;
                }
                numbers.Add(number);
                i += 3;
            }
        }
        if (numbers.Count == 0) {
            return null;
        }
        if (numbers.Count == 1) {
            return ObjectNumbered(objects, numbers[0]);
        }
        MemoryStream buf = new MemoryStream();
        // Each stream copied into the content is charged to the budget, as the
        // copy is memory of its own, which decoding the stream does not pay
        // for: /Contents [4 0 R 4 0 R ...] of a stream of 16 MB made gigabytes
        // of a PDF of a few KB (review of 11 October 2026, #29), and 64 pages
        // of /Contents [4 0 R 5 0 R] held 1 GB against the budget of 256 MB
        // (review of 12 October 2026, R16). Past the budget, the page has no
        // content, as a stream past it has no data.
        foreach (int number in numbers) {
            PDFobj page = ObjectNumbered(objects, number);
            if (page == null) {
                continue;
            }
            byte[] bytes = page.GetData();
            if (bytes != null) {
                if (page.budget != null) {
                    page.budget.left -= bytes.Length + 1;
                    if (page.budget.left < 0) {
                        page.budget.left = 0;
                        return null;
                    }
                }
                buf.Write(bytes, 0, bytes.Length);
                buf.WriteByte((byte) '\n');     // A stream can end in the middle of a line.
            }
        }
        PDFobj content = new PDFobj();
        content.data = buf.ToArray();
        return content;
    }

    // Returns the token at the index, or an empty string when the dictionary
    // of an object that was read ends before it.
    private static String TokenAt(List<String> dict, int index) {
        return (index < 0 || index >= dict.Count) ? "" : dict[index];
    }

    // Returns the object that the token names, or null when the token is not
    // the number of an object the PDF has: a page of a file that was changed
    // can name an object that is not in it.
    internal static PDFobj ObjectAt(List<PDFobj> objects, String token) {
        int number;
        if (!Int32.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) {
            return null;
        }
        return ObjectNumbered(objects, number);
    }

    // Returns the index to insert at, which is the end of the dictionary when
    // the index is past it.
    private static int IndexAt(List<String> dict, int index) {
        return (index < 0) ? 0 : Math.Min(index, dict.Count);
    }

    /// <summary>Returns the resources object of this page.</summary>
    public PDFobj GetResourcesObject(List<PDFobj> objects) {
        if (inheritedResources != null) {
            // The dictionary the page shares with the others under its node,
            // as an object of its own, which is read and not changed
            PDFobj resources = new PDFobj();
            resources.dict = inheritedResources;
            return resources;
        }
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                if (i + 1 >= dict.Count) {
                    return null;
                }
                String token = dict[i + 1];
                if (token.Equals("<<")) {
                    return this;
                }
                int number;
                if (!Int32.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out number)) {
                    return null;
                }
                return ObjectNumbered(objects, number);
            }
        }
        return null;
    }

    // Gives the page a copy of the /Resources it shares with the others under
    // its node, before a resource is added to it, so that the resource is the
    // page's alone, as it was when every page had a copy. A page with no
    // /Resources gets an empty one: the fonts added to it were lost, with no
    // error (review of 30 October 2026, B5).
    internal void OwnResources() {
        List<String> entry = inheritedResources;
        if (entry == null) {
            if (EntryIndexOf("/Resources") != -1) {
                return;
            }
            entry = new List<String> {"<<", ">>"};
        }
        int open = dict.IndexOf("<<");
        if (open != -1) {
            dict.InsertRange(open + 1, entry);
            dict.Insert(open + 1, "/Resources");
        }
        inheritedResources = null;
    }

    /// <summary>Adds a core font to the resources of this page and returns the font.</summary>
    public Font AddResource(int coreFont, List<PDFobj> objects) {
        OwnResources();
        Font font = new Font(coreFont);
        font.fontID = font.name.Replace('-', '_').ToUpperInvariant();

        PDFobj obj = new PDFobj();
        obj.dict.Add("<<");
        obj.dict.Add("/Type");
        obj.dict.Add("/Font");
        obj.dict.Add("/Subtype");
        obj.dict.Add("/Type1");
        obj.dict.Add("/BaseFont");
        obj.dict.Add("/" + font.name);
        if (!font.name.Equals("Symbol") && !font.name.Equals("ZapfDingbats")) {
            obj.dict.Add("/Encoding");
            obj.dict.Add("/WinAnsiEncoding");
        }
        obj.dict.Add(">>");
        obj.number = objects.Count + 1;
        objects.Add(obj);

        // The first /Resources of the page is the one, and the font is added
        // to it once: adding it to the page again, after a resources object
        // that names the page itself has grown the page dictionary, never
        // ended.
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = TokenAt(dict, i + 1);
                if (token.Equals("<<")) {           // Direct resources object
                    AddFontResource(this, objects, font.fontID, obj.number);
                } else {                            // Indirect resources object
                    PDFobj resources = ObjectAt(objects, token);
                    if (resources != null) {
                        AddFontResource(resources, objects, font.fontID, obj.number);
                    }
                }
                break;
            }
        }

        return font;
    }

    private void AddFontResource(
            PDFobj obj, List<PDFobj> objects, String fontID, int number) {
        bool fonts = false;
        foreach (String token in obj.dict) {
            if (token.Equals("/Font")) {
                fonts = true;
                break;
            }
        }
        if (!fonts) {
            // Direct resources follow "/Resources <<" in the page dictionary;
            // an indirect resources object has no /Resources key, and its
            // entries follow its first "<<".
            int i = obj.dict.IndexOf("/Resources");
            i = (i == -1) ? obj.dict.IndexOf("<<") + 1 : i + 2;
            obj.dict.InsertRange(IndexAt(obj.dict, i), new String[] {"/Font", "<<", ">>"});
        }

        for (int i = 0; i < obj.dict.Count; i++) {
            if (obj.dict[i].Equals("/Font")) {
                String token = TokenAt(obj.dict, i + 1);
                if (token.Equals("<<")) {
                    obj.dict.Insert(i + 2, "/" + fontID);
                    obj.dict.Insert(i + 3, number.ToString(CultureInfo.InvariantCulture));
                    obj.dict.Insert(i + 4, "0");
                    obj.dict.Insert(i + 5, "R");
                    return;
                }
                PDFobj o2 = ObjectAt(objects, token);
                if (o2 != null) {
                    for (int j = 0; j < o2.dict.Count; j++) {
                        if (o2.dict[j].Equals("<<")) {
                            o2.dict.Insert(j + 1, "/" + fontID);
                            o2.dict.Insert(j + 2, number.ToString(CultureInfo.InvariantCulture));
                            o2.dict.Insert(j + 3, "0");
                            o2.dict.Insert(j + 4, "R");
                            return;
                        }
                    }
                }
            }
        }
    }

    private void InsertNewObject(
            List<String> dict, String[] list, String type) {
        if (dict.Contains(list[0])) {
            return;
        }
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals(type)) {
                dict.InsertRange(IndexAt(dict, i + 2), list);
                return;
            }
        }
        if (TokenAt(dict, 3).Equals("<<")) {
            dict.InsertRange(4, list);
            return;
        }
    }

    private void AddResource(
            String type, PDFobj obj, List<PDFobj> objects, Int32 objNumber) {
        String tag = type.Equals("/Font") ? "/F" : "/Im";
        String number = objNumber.ToString(CultureInfo.InvariantCulture);
        String[] list = {tag + number, number, "0", "R"};
        for (int i = 0; i < obj.dict.Count; i++) {
            String token = obj.dict[i];
            if (token.Equals(type)) {
                token = TokenAt(obj.dict, i + 1);
                if (token.Equals("<<")) {
                    InsertNewObject(obj.dict, list, type);
                } else {
                    PDFobj o2 = ObjectAt(objects, token);
                    if (o2 != null) {
                        InsertNewObject(o2.dict, list, type);
                    }
                }
                return;
            }
        }

        // Handle the case where the page originally does not have any font resources.
        String[] array = {type, "<<", tag + number, number, "0", "R", ">>"};
        for (int i = 0; i < obj.dict.Count; i++) {
            if (obj.dict[i].Equals("/Resources")) {
                obj.dict.InsertRange(IndexAt(obj.dict, i + 2), array);
                return;
            }
        }
        for (int i = 0; i < obj.dict.Count; i++) {
            if (obj.dict[i].Equals("<<")) {
                obj.dict.InsertRange(i + 1, array);
                return;
            }
        }
    }

    /// <summary>Adds an image to the resources of this page.</summary>
    public void AddResource(Image image, List<PDFobj> objects) {
        OwnResources();
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = TokenAt(dict, i + 1);
                if (token.Equals("<<")) {       // Direct resources object
                    AddResource("/XObject", this, objects, image.objNumber);
                } else {                        // Indirect resources object
                    PDFobj resources = ObjectAt(objects, token);
                    if (resources != null) {
                        AddResource("/XObject", resources, objects, image.objNumber);
                    }
                }
                return;
            }
        }
    }

    /// <summary>Adds a font to the resources of this page.</summary>
    public void AddResource(Font font, List<PDFobj> objects) {
        OwnResources();
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = TokenAt(dict, i + 1);
                if (token.Equals("<<")) {       // Direct resources object
                    AddResource("/Font", this, objects, font.objNumber);
                } else {                        // Indirect resources object
                    PDFobj resources = ObjectAt(objects, token);
                    if (resources != null) {
                        AddResource("/Font", resources, objects, font.objNumber);
                    }
                }
                return;
            }
        }
    }

    // Puts a content stream before the content streams of this page and
    // another after them, in a /Contents array of its own, or gives a page
    // that has none the second. Returns false when the /Contents is not a
    // stream or an array of them, and changes nothing.
    internal bool WrapContents(byte[] before, byte[] after, List<PDFobj> objects) {
        int i = EntryIndexOf("/Contents");
        if (i == -1) {
            // A page with no content: AddContent added nothing, and the
            // drawing was lost
            int last = dict.LastIndexOf(">>");
            if (last == -1) {
                return false;
            }
            List<String> added = new List<String> {"/Contents", "["};
            added.AddRange(AddStream(after, objects));
            added.Add("]");
            dict.InsertRange(last, added);
            return true;
        }
        int start = i + 1;
        List<String> refs;
        int end;
        if (TokenAt(dict, start) == "[") {
            end = ArrayEnd(dict, start + 1);
            if (end == -1) {
                return false;
            }
            refs = dict.GetRange(start + 1, end - 1 - (start + 1));
        } else {
            if (!PDF.IsReference(dict, start)) {
                return false;
            }
            end = start + 3;
            PDFobj contents = ObjectAt(objects, dict[start]);
            if (contents == null) {
                return false;
            }
            if (contents.data == null && contents.stream == null) {
                // An array of streams that is an object of its own
                int k = contents.dict.IndexOf("[");
                int arrayEnd = (k == -1) ? -1 : ArrayEnd(contents.dict, k + 1);
                if (arrayEnd == -1) {
                    return false;
                }
                refs = contents.dict.GetRange(k + 1, arrayEnd - 1 - (k + 1));
            } else {
                refs = dict.GetRange(start, 3);
            }
        }
        List<String> value = new List<String> {"["};
        value.AddRange(AddStream(before, objects));
        value.AddRange(refs);
        value.AddRange(AddStream(after, objects));
        value.Add("]");
        dict.RemoveRange(start, end - start);
        dict.InsertRange(start, value);
        return true;
    }

    // Returns the index after the "]" of the array of references that starts
    // at the index, or -1 when it is not one.
    private static int ArrayEnd(List<String> tokens, int from) {
        for (int j = from; j < tokens.Count; j += 3) {
            if (tokens[j] == "]") {
                return j + 1;
            }
            if (!PDF.IsReference(tokens, j)) {
                return -1;
            }
        }
        return -1;
    }

    // Adds a stream object of the content, and returns its reference.
    private static List<String> AddStream(byte[] content, List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.SetNumber(objects.Count + 1);
        obj.SetStream(content);
        objects.Add(obj);
        return new List<String> {obj.number.ToString(System.Globalization.CultureInfo.InvariantCulture), "0", "R"};
    }

    // Returns the lower left corner of the /MediaBox of this page, or 0, 0 when
    // it has none that is four numbers.
    internal float[] MediaBoxOrigin() {
        int i = dict.IndexOf("/MediaBox");
        if (i == -1 || TokenAt(dict, i + 1) != "[") {
            return new float[] {0f, 0f};
        }
        float[] box = new float[4];
        for (int j = 0; j < 4; j++) {
            if (!float.TryParse(TokenAt(dict, i + 2 + j), System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out box[j])
                    || float.IsNaN(box[j]) || float.IsInfinity(box[j])) {
                return new float[] {0f, 0f};
            }
        }
        return new float[] {Math.Min(box[0], box[2]), Math.Min(box[1], box[3])};
    }

    /// <summary>Adds a content stream to this page.</summary>
    public void AddContent(byte[] content, List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.SetNumber(objects.Count + 1);
        obj.SetStream(content);
        objects.Add(obj);

        String objNumber = obj.number.ToString(CultureInfo.InvariantCulture);
        // The page's own /Contents, not an annotation's in it
        for (int i = EntryIndexOf("/Contents"); i >= 0 && i < dict.Count; i++) {
            if (dict[i].Equals("/Contents")) {
                i += 1;
                String token = TokenAt(dict, i);
                if (token.Equals("[")) {
                    // Array of content objects, which can end before its "]".
                    for (i += 1; i < dict.Count; i += 3) {
                        if (dict[i].Equals("]")) {
                            dict.Insert(i, "R");
                            dict.Insert(i, "0");
                            dict.Insert(i, objNumber);
                            return;
                        }
                    }
                    return;
                } else {
                    // Single content object
                    PDFobj obj2 = ObjectAt(objects, token);
                    if (obj2 == null) {
                        return;
                    }
                    if (obj2.data == null && obj2.stream == null) {
                        // This is not a stream object!
                        for (int j = 0; j < obj2.dict.Count; j++) {
                            if (obj2.dict[j].Equals("]")) {
                                obj2.dict.Insert(j, "R");
                                obj2.dict.Insert(j, "0");
                                obj2.dict.Insert(j, objNumber);
                                return;
                            }
                        }
                    }
                    if (!TokenAt(dict, i + 1).Equals("0") || !TokenAt(dict, i + 2).Equals("R")) {
                        return;     // Not a whole "n 0 R" to put in an array.
                    }
                    dict.Insert(i, "[");
                    dict.Insert(i + 4, "]");
                    dict.Insert(i + 4, "R");
                    dict.Insert(i + 4, "0");
                    dict.Insert(i + 4, objNumber);
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Adds new content object before the existing content objects.
    /// The original code was provided by Stefan Ostermann author of ScribMaster and HandWrite Pro.
    /// Additional code to handle PDFs with indirect array of stream objects was written by EDragoev.
    /// </summary>
    public void AddPrefixContent(byte[] content, List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.SetNumber(objects.Count + 1);
        obj.SetStream(content);
        objects.Add(obj);

        String objNumber = obj.number.ToString(CultureInfo.InvariantCulture);
        // The page's own /Contents, not an annotation's in it
        for (int i = EntryIndexOf("/Contents"); i >= 0 && i < dict.Count; i++) {
            if (dict[i].Equals("/Contents")) {
                i += 1;
                String token = TokenAt(dict, i);
                if (token.Equals("[")) {
                    // Array of content object streams
                    i += 1;
                    dict.Insert(IndexAt(dict, i), "R");
                    dict.Insert(IndexAt(dict, i), "0");
                    dict.Insert(IndexAt(dict, i), objNumber);
                    return;
                } else {
                    // Single content object
                    PDFobj obj2 = ObjectAt(objects, token);
                    if (obj2 == null) {
                        return;
                    }
                    if (obj2.data == null && obj2.stream == null) {
                        // This is not a stream object!
                        for (int j = 0; j < obj2.dict.Count; j++) {
                            if (obj2.dict[j].Equals("[")) {
                                j += 1;
                                obj2.dict.Insert(j, "R");
                                obj2.dict.Insert(j, "0");
                                obj2.dict.Insert(j, objNumber);
                                return;
                            }
                        }
                    }
                    if (!TokenAt(dict, i + 1).Equals("0") || !TokenAt(dict, i + 2).Equals("R")) {
                        return;     // Not a whole "n 0 R" to put in an array.
                    }
                    dict.Insert(i, "[");
                    dict.Insert(i + 4, "]");
                    i += 1;
                    dict.Insert(i, "R");
                    dict.Insert(i, "0");
                    dict.Insert(i, objNumber);
                    return;
                }
            }
        }
    }

    // Returns the largest number of the /GS names in the dictionary, or 0. A
    // name with no number after /GS, like /GSa, is skipped.
    private int GetMaxGSNumber(PDFobj obj) {
        int maxGSNumber = 0;
        foreach (String token in obj.dict) {
            if (token.StartsWith("/GS", StringComparison.Ordinal) &&
                    Int32.TryParse(token.Substring(3), NumberStyles.Integer, CultureInfo.InvariantCulture, out int number)) {
                maxGSNumber = Math.Max(maxGSNumber, number);
            }
        }
        return maxGSNumber;
    }

    // Returns the largest number of the /GS, /Fm and /OC names in the resources
    // of this page, or 0, so that the names of what is drawn on it are not those
    // of what it holds. They are all read from the objects of the PDF, in the
    // resources or in an /ExtGState, /XObject or /Properties dictionary that is
    // an object of its own.
    internal int MaxResourceNumber() {
        if (readObjects == null) {
            return 0;
        }
        List<String> tokens = inheritedResources;
        if (tokens == null) {
            int i = EntryIndexOf("/Resources");
            if (i == -1) {
                return 0;
            }
            tokens = dict;
            if (PDF.IsReference(dict, i + 1)) {
                PDFobj resources;
                if (!readObjects.TryGetValue(Int32.Parse(dict[i + 1], CultureInfo.InvariantCulture), out resources)) {
                    return 0;
                }
                tokens = resources.dict;
            }
        }
        int maximum = MaxNameNumber(tokens);
        for (int i = 0; i < tokens.Count; i++) {
            String token = tokens[i];
            if ((token == "/ExtGState" || token == "/XObject" || token == "/Properties")
                    && PDF.IsReference(tokens, i + 1)) {
                PDFobj entries;
                if (readObjects.TryGetValue(Int32.Parse(tokens[i + 1], CultureInfo.InvariantCulture), out entries)) {
                    maximum = Math.Max(maximum, MaxNameNumber(entries.dict));
                }
            }
        }
        return maximum;
    }

    // Returns the largest number of the names in the tokens that are /GS, /Fm
    // or /OC and one to nine digits, or 0.
    private static int MaxNameNumber(List<String> tokens) {
        int maximum = 0;
        foreach (String token in tokens) {
            if (token.Length >= 4
                    && (token.StartsWith("/GS", StringComparison.Ordinal)
                            || token.StartsWith("/Fm", StringComparison.Ordinal)
                            || token.StartsWith("/OC", StringComparison.Ordinal))
                    && IsGSName("/GS" + token.Substring(3))) {
                maximum = Math.Max(maximum, Int32.Parse(token.Substring(3), CultureInfo.InvariantCulture));
            }
        }
        return maximum;
    }

    // Adds the name and its value to a dictionary of the resources of this
    // page, like /ExtGState, unless it has the name.
    internal void AddResourceEntry(String category, String name, List<String> value, List<PDFobj> objects) {
        PDFobj resources;
        int index = ResourceEntries(category, objects, out resources);
        if (resources == null) {
            return;
        }
        for (int i = index; i < resources.dict.Count && resources.dict[i] != ">>";
                i = PDF.ValueEnd(resources.dict, i + 1)) {
            if (resources.dict[i] == name) {
                return;
            }
        }
        resources.dict.InsertRange(index, value);
        resources.dict.Insert(index, name);
    }

    // Returns the index of the first entry of a dictionary of the resources of
    // this page, like /ExtGState, and the object that holds it, or null. The
    // resources and the dictionary can be objects of their own, and resources
    // without the dictionary get it.
    private int ResourceEntries(String category, List<PDFobj> objects, out PDFobj holder) {
        OwnResources();
        holder = null;
        PDFobj obj = null;
        int index = -1;
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = TokenAt(dict, i + 1);
                if (token.Equals("<<")) {
                    obj = this;
                    index = i + 2;
                } else if ((obj = ObjectAt(objects, token)) != null) {
                    for (int j = 0; j < obj.dict.Count; j++) {
                        if (obj.dict[j].Equals("<<")) {
                            index = j + 1;
                            break;
                        }
                    }
                }
                break;
            }
        }
        if (obj == null || index == -1) {
            return -1;
        }
        int k = index;
        while (k < obj.dict.Count && !obj.dict[k].Equals(category)) {
            k++;
        }
        if (k == obj.dict.Count) {
            obj.dict.InsertRange(index, new String[] {category, "<<", ">>"});
            holder = obj;
            return index + 2;
        }
        if (TokenAt(obj.dict, k + 1).Equals("<<")) {
            holder = obj;
            return k + 2;
        }
        holder = ObjectAt(objects, TokenAt(obj.dict, k + 1));   // "/ExtGState 12 0 R"
        return (holder == null) ? -1 : holder.dict.IndexOf("<<") + 1;
    }

    /// <summary>Adds the graphics state to the resources of this page.</summary>
    public PDFobj SetGraphicsState(GraphicsState gs, List<PDFobj> objects) {
        PDFobj obj;
        int index = ResourceEntries("/ExtGState", objects, out obj);
        if (obj == null) {
            return this;
        }
        List<String> entries = new List<String>(gs.Entries().Split(' '));
        String name = SameState(obj, index, entries);
        if (name != null) {
            gsNumber = Int32.Parse(name.Substring(3), CultureInfo.InvariantCulture) - 1;
        } else {
            gsNumber = GetMaxGSNumber(obj);
            name = "/GS" + (gsNumber + 1).ToString(CultureInfo.InvariantCulture);
            List<String> state = new List<String>();
            state.Add(name);
            state.Add("<<");
            state.AddRange(entries);
            state.Add(">>");
            obj.dict.InsertRange(index, state);
        }
        AddPrefixContent(Encoding.ASCII.GetBytes("q\n" + name + " gs\n"), objects);
        return this;
    }

    // Returns the name of a /GS state of the /ExtGState dictionary whose
    // entries at index are these entries and no others, or null: setting the
    // same state again reuses it rather than adding another (Stefan
    // Ostermann's fork of PDFjet, 19 October 2026). Only our own /GS names
    // with a number are reused, and only for exactly the same entries, so a
    // state with a blend mode is not taken for one without.
    private static String SameState(PDFobj obj, int index, List<String> entries) {
        int i = index;
        while (i < obj.dict.Count && obj.dict[i] != ">>") {
            String key = obj.dict[i];
            int end = PDF.ValueEnd(obj.dict, i + 1);
            if (IsGSName(key)
                    && end - i == entries.Count + 3
                    && obj.dict[i + 1] == "<<"
                    && SameTokens(obj.dict, i + 2, entries)) {
                return key;
            }
            i = end;
        }
        return null;
    }

    // Returns true for /GS and one to nine digits, as Java's matches of
    // "/GS[0-9]{1,9}".
    private static bool IsGSName(String key) {
        if (!key.StartsWith("/GS", StringComparison.Ordinal) || key.Length < 4 || key.Length > 12) {
            return false;
        }
        for (int i = 3; i < key.Length; i++) {
            if (key[i] < '0' || key[i] > '9') {
                return false;
            }
        }
        return true;
    }

    private static bool SameTokens(List<String> tokens, int start, List<String> entries) {
        for (int i = 0; i < entries.Count; i++) {
            if (tokens[start + i] != entries[i]) {
                return false;
            }
        }
        return true;
    }
}
}   // End of namespace PDFjet.NET
