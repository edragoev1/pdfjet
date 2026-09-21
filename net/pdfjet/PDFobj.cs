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
/// its dictionary and its stream. See Example_20, Example_37 and Example_50.
/// </summary>
public class PDFobj {
    internal int number;           // The object number
    internal int offset;           // The object offset
    internal List<String> dict;
    internal int streamOffset;
    internal byte[] stream;        // The compressed stream
    internal byte[] data;          // The decompressed data
    internal int gsNumber = -1;

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

    /// <summary>Returns the decompressed stream data.</summary>
    public byte[] GetData() {
        return this.data;
    }

    // Copies the stream from the buffer, and decodes it with the filters of
    // its /Filter entry.
    internal void SetStreamAndData(byte[] buf, int length) {
        SetStreamAndData(buf, length, null);
    }

    // Copies the stream from the buffer, decrypts it when the PDF is encrypted,
    // and decodes it with the filters of its /Filter entry. The decrypted
    // stream replaces the encrypted one, so that it can be copied.
    internal void SetStreamAndData(byte[] buf, int length, Decryptor decryptor) {
        if (this.stream == null) {
            int actual = StreamLength(buf, streamOffset, length);
            if (actual != length) {
                length = actual;
                SetLength(length);
            }
            // A /Length the PDF does not have is checked before the stream is
            // made, so that a file of a few bytes that says its stream is a
            // gigabyte takes no memory.
            if (length < 0 || streamOffset < 0 || length > buf.Length - streamOffset) {
                throw new Exception("The stream of an object is not in the PDF.");
            }
            this.stream = new byte[length];
            Array.Copy(buf, streamOffset, stream, 0, length);
            if (decryptor != null) {
                this.stream = decryptor.DecryptStream(this, stream);
                SetLength(stream.Length);
            }
            this.data = DecodeStream();
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
            return Decode(stream);
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
    private static int StreamLength(byte[] buf, int offset, int length) {
        if (offset < 0 || offset > buf.Length) {
            return length;
        }
        if (length >= 0 && length <= buf.Length - offset) {
            int i = offset + length;
            while (i < buf.Length && PDF.IsWhiteSpace(buf[i])) {
                i++;
            }
            if (PDF.StartsWith(buf, i, "endstream")) {
                return length;
            }
        }
        int end = PDF.IndexOf(buf, "endstream", offset);
        if (end == -1) {
            return length;
        }
        if (end > offset && buf[end - 1] == '\n') {
            end--;
        }
        if (end > offset && buf[end - 1] == '\r') {
            end--;
        }
        return end - offset;
    }

    // Sets the /Length of the stream, replacing a reference to the length,
    // and adds it to a stream dictionary that has none.
    private void SetLength(int length) {
        int i = dict.IndexOf("/Length");
        if (i == -1) {
            int open = dict.IndexOf("<<");
            if (open != -1) {
                dict.InsertRange(open + 1, new String[] {"/Length", length.ToString()});
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
        dict[i + 1] = length.ToString();
    }

    // Decodes the stream with each filter of its /Filter entry in turn. A
    // filter that is not supported, like DCTDecode, ends the decoding, and
    // the data is what the filters before it decoded.
    private byte[] Decode(byte[] stream) {
        List<String> filters = GetValues("/Filter");
        byte[] decoded = stream;
        for (int i = 0; i < filters.Count; i++) {
            String filter = filters[i];
            if (filter.Equals("/FlateDecode") || filter.Equals("/Fl")) {
                decoded = ApplyDecodeParms(Decompressor.Inflate(decoded), i);
            } else if (filter.Equals("/LZWDecode") || filter.Equals("/LZW")) {
                decoded = ApplyDecodeParms(Decompressor.LZWDecode(decoded), i);
            } else if (filter.Equals("/ASCIIHexDecode") || filter.Equals("/AHx")) {
                decoded = Decompressor.ASCIIHexDecode(decoded);
            } else if (filter.Equals("/ASCII85Decode") || filter.Equals("/A85")) {
                decoded = Decompressor.ASCII85Decode(decoded);
            } else if (filter.Equals("/RunLengthDecode") || filter.Equals("/RL")) {
                decoded = Decompressor.RunLengthDecode(decoded);
            } else {
                break;
            }
        }
        return decoded;
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

    // Undoes the predictor in the parameters of the filter at the index.
    // Images keep it, as they are copied with their stream, and their data
    // is not used.
    private byte[] ApplyDecodeParms(byte[] decoded, int index) {
        if (GetValue("/Subtype").Equals("/Image")) {
            return decoded;
        }
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
            return Int32.TryParse(parms[i + 1], out int value) ? value : defaultValue;
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
    /// Returns the parameter value given the specified key.
    /// </summary>
    /// <param name="key">the specified key.</param>
    /// <returns>the value.</returns>
    public String GetValue(String key) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals(key)) {
                if (i + 1 >= dict.Count) {
                    return "";
                }
                String token = dict[i + 1];
                if (token.Equals("<<")) {
                    return ValueUpTo(i + 2, ">>", "<< ");
                } else if (token.Equals("[")) {
                    return ValueUpTo(i + 2, "]", "[ ");
                } else {
                    return token;
                }
            }
        }
        return "";
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
    internal List<Int32> GetObjectNumbers(String key) {
        List<Int32> numbers = new List<Int32>();
        for (int i = 0; i < dict.Count; i++) {
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
                        if (!Int32.TryParse(str, out number)) {
                            break;
                        }
                        numbers.Add(number);
                        ++i;    // 0
                        ++i;    // R
                    }
                } else if (Int32.TryParse(str, out number)) {
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
                if (width == 0f || height == 0f) {
                    return Letter.PORTRAIT;
                }
                return new PageSize(width, height);
            }
        }
        return Letter.PORTRAIT;
    }

    internal int GetLength(List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            String token = dict[i];
            if (token.Equals("/Length")) {
                int number = ToLength(TokenAt(dict, i + 1));
                if (i + 2 >= dict.Count) {
                    throw new Exception("The dictionary ends after the /Length.");
                }
                if (dict[i + 2].Equals("0")) {
                    if (i + 3 >= dict.Count) {
                        throw new Exception("The dictionary ends after the /Length.");
                    }
                    if (dict[i + 3].Equals("R")) {
                        return GetLength(objects, number);
                    }
                }
                return number;
            }
        }
        return 0;
    }

    internal int GetLength(List<PDFobj> objects, int number) {
        foreach (PDFobj obj in objects) {
            if (obj.number == number) {
                return ToLength(TokenAt(obj.dict, 3));
            }
        }
        return 0;
    }

    // Returns the length the token holds, which a PDF that was read can write
    // as anything.
    private static int ToLength(String token) {
        int length;
        if (!Int32.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out length)) {
            throw new Exception("The /Length of a stream is not a number.");
        }
        return length;
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
                if (!Int32.TryParse(obj.dict[i + 1], out number)) {
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
        foreach (int number in numbers) {
            PDFobj page = ObjectNumbered(objects, number);
            if (page == null) {
                continue;
            }
            byte[] bytes = page.data;
            if (bytes != null) {
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
    private static PDFobj ObjectAt(List<PDFobj> objects, String token) {
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
                if (!Int32.TryParse(token, out number)) {
                    return null;
                }
                return ObjectNumbered(objects, number);
            }
        }
        return null;
    }

    /// <summary>Adds a core font to the resources of this page and returns the font.</summary>
    public Font AddResource(int coreFont, List<PDFobj> objects) {
        Font font = new Font(coreFont);
        font.fontID = font.name.Replace('-', '_').ToUpper();

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
                    obj.dict.Insert(i + 3, number.ToString());
                    obj.dict.Insert(i + 4, "0");
                    obj.dict.Insert(i + 5, "R");
                    return;
                }
                PDFobj o2 = ObjectAt(objects, token);
                if (o2 != null) {
                    for (int j = 0; j < o2.dict.Count; j++) {
                        if (o2.dict[j].Equals("<<")) {
                            o2.dict.Insert(j + 1, "/" + fontID);
                            o2.dict.Insert(j + 2, number.ToString());
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
        String number = objNumber.ToString();
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

    /// <summary>Adds a content stream to this page.</summary>
    public void AddContent(byte[] content, List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.SetNumber(objects.Count + 1);
        obj.SetStream(content);
        objects.Add(obj);

        String objNumber = obj.number.ToString();
        for (int i = 0; i < dict.Count; i++) {
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

        String objNumber = obj.number.ToString();
        for (int i = 0; i < dict.Count; i++) {
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

    /// <summary>Adds the graphics state to the resources of this page.</summary>
    public PDFobj SetGraphicsState(GraphicsState gs, List<PDFobj> objects) {
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
            return this;
        }
        // The graphics states go in the /ExtGState dictionary of the resources,
        // which can be an object of its own. Resources without one get it.
        int k = index;
        while (k < obj.dict.Count && !obj.dict[k].Equals("/ExtGState")) {
            k++;
        }
        if (k == obj.dict.Count) {
            obj.dict.InsertRange(index, new String[] {"/ExtGState", "<<", ">>"});
            index += 2;
        } else if (TokenAt(obj.dict, k + 1).Equals("<<")) {
            index = k + 2;
        } else {                                    // "/ExtGState 12 0 R"
            PDFobj extGState = ObjectAt(objects, TokenAt(obj.dict, k + 1));
            if (extGState == null) {
                return this;
            }
            obj = extGState;
            index = obj.dict.IndexOf("<<") + 1;
        }
        gsNumber = GetMaxGSNumber(obj);
        String name = "/GS" + (gsNumber + 1).ToString();
        obj.dict.InsertRange(index, new String[] {
                name, "<<",
                "/CA", Encoding.ASCII.GetString(FastFloat.ToByteArray(gs.GetAlphaStroking())),
                "/ca", Encoding.ASCII.GetString(FastFloat.ToByteArray(gs.GetAlphaNonStroking())),
                ">>"});
        AddPrefixContent(Encoding.ASCII.GetBytes("q\n" + name + " gs\n"), objects);
        return this;
    }
}
}   // End of namespace PDFjet.NET
