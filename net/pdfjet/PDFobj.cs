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
            this.stream = new byte[length];
            Array.Copy(buf, streamOffset, stream, 0, length);
            if (decryptor != null) {
                this.stream = decryptor.DecryptStream(this, stream);
                SetLength(stream.Length);
            }
            this.data = Decode(stream);
        }
    }

    // Sets the /Length of the stream, replacing a reference to the length.
    private void SetLength(int length) {
        int i = dict.IndexOf("/Length");
        if (i == -1 || i + 1 >= dict.Count) {
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
                String token = dict[i + 1];
                if (token.Equals("<<")) {
                    StringBuilder buffer = new StringBuilder();
                    buffer.Append("<< ");
                    i += 2;
                    while (!dict[i].Equals(">>")) {
                        buffer.Append(dict[i]);
                        buffer.Append(" ");
                        i += 1;
                    }
                    buffer.Append(">>");
                    return buffer.ToString();
                } else if (token.Equals("[")) {
                    StringBuilder buffer = new StringBuilder();
                    buffer.Append("[ ");
                    i += 2;
                    while (!dict[i].Equals("]")) {
                        buffer.Append(dict[i]);
                        buffer.Append(" ");
                        i += 1;
                    }
                    buffer.Append("]");
                    return buffer.ToString();
                } else {
                    return token;
                }
            }
        }
        return "";
    }

    internal List<Int32> GetObjectNumbers(String key) {
        List<Int32> numbers = new List<Int32>();
        for (int i = 0; i < dict.Count; i++) {
            String token = dict[i];
            if (token.Equals(key)) {
                String str = dict[++i];
                if (str.Equals("[")) {
                    while (true) {
                        str = dict[++i];
                        if (str.Equals("]")) {
                            break;
                        }
                        numbers.Add(Int32.Parse(str));
                        ++i;    // 0
                        ++i;    // R
                    }
                } else {
                    numbers.Add(Int32.Parse(str));
                }
                break;
            }
        }
        return numbers;
    }

    /// <summary>Returns the width and height from the /MediaBox of this page.</summary>
    public float[] GetPageSize() {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/MediaBox")) {
                return new float[] {
                        float.Parse(dict[i + 4], CultureInfo.InvariantCulture),
                        float.Parse(dict[i + 5], CultureInfo.InvariantCulture) };
            }
        }
        return Letter.PORTRAIT;
    }

    internal int GetLength(List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            String token = dict[i];
            if (token.Equals("/Length")) {
                int number = Int32.Parse(dict[i + 1]);
                if (dict[i + 2].Equals("0") &&
                        dict[i + 3].Equals("R")) {
                    return GetLength(objects, number);
                } else {
                    return number;
                }
            }
        }
        return 0;
    }

    internal int GetLength(List<PDFobj> objects, int number) {
        foreach (PDFobj obj in objects) {
            if (obj.number == number) {
                return Int32.Parse(obj.dict[3]);
            }
        }
        return 0;
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
            PDFobj obj = objects[numbers[0] - 1];
            if (obj.stream != null) {
                return obj;
            }
            // "/Contents 39 0 R" where the object is "[ 41 0 R 43 0 R ]"
            numbers = new List<Int32>();
            int i = obj.dict.IndexOf("[");
            while (i != -1 && i + 3 < obj.dict.Count && obj.dict[i + 3].Equals("R")) {
                numbers.Add(Int32.Parse(obj.dict[i + 1]));
                i += 3;
            }
        }
        if (numbers.Count == 0) {
            return null;
        }
        if (numbers.Count == 1) {
            return objects[numbers[0] - 1];
        }
        MemoryStream buf = new MemoryStream();
        foreach (int number in numbers) {
            byte[] bytes = objects[number - 1].data;
            if (bytes != null) {
                buf.Write(bytes, 0, bytes.Length);
                buf.WriteByte((byte) '\n');     // A stream can end in the middle of a line.
            }
        }
        PDFobj content = new PDFobj();
        content.data = buf.ToArray();
        return content;
    }

    /// <summary>Returns the resources object of this page.</summary>
    public PDFobj GetResourcesObject(List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {
                    return this;
                }
                return objects[Int32.Parse(token) - 1];
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

        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[++i];
                if (token.Equals("<<")) {                   // Direct resources object
                    AddFontResource(this, objects, font.fontID, obj.number);
                } else if (token[0] >= '0' && token[0] <= '9') {        // Indirect resources object
                    AddFontResource(objects[Int32.Parse(token) - 1], objects, font.fontID, obj.number);
                }
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
            obj.dict.InsertRange(i, new String[] {"/Font", "<<", ">>"});
        }

        for (int i = 0; i < obj.dict.Count; i++) {
            if (obj.dict[i].Equals("/Font")) {
                String token = obj.dict[i + 1];
                if (token.Equals("<<")) {
                    obj.dict.Insert(i + 2, "/" + fontID);
                    obj.dict.Insert(i + 3, number.ToString());
                    obj.dict.Insert(i + 4, "0");
                    obj.dict.Insert(i + 5, "R");
                    return;
                } else if (token[0] >= '0' && token[0] <= '9') {
                    PDFobj o2 = objects[Int32.Parse(token) - 1];
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
                dict.InsertRange(i + 2, list);
                return;
            }
        }
        if (dict[3].Equals("<<")) {
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
                token = obj.dict[i + 1];
                if (token.Equals("<<")) {
                    InsertNewObject(obj.dict, list, type);
                } else {
                    InsertNewObject(objects[Int32.Parse(token) - 1].dict, list, type);
                }
                return;
            }
        }

        // Handle the case where the page originally does not have any font resources.
        String[] array = {type, "<<", tag + number, number, "0", "R", ">>"};
        for (int i = 0; i < obj.dict.Count; i++) {
            if (obj.dict[i].Equals("/Resources")) {
                obj.dict.InsertRange(i + 2, array);
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
                String token = dict[i + 1];
                if (token.Equals("<<")) {       // Direct resources object
                    AddResource("/XObject", this, objects, image.objNumber);
                } else {                        // Indirect resources object
                    AddResource("/XObject", objects[Int32.Parse(token) - 1], objects, image.objNumber);
                }
                return;
            }
        }
    }

    /// <summary>Adds a font to the resources of this page.</summary>
    public void AddResource(Font font, List<PDFobj> objects) {
        for (int i = 0; i < dict.Count; i++) {
            if (dict[i].Equals("/Resources")) {
                String token = dict[i + 1];
                if (token.Equals("<<")) {       // Direct resources object
                    AddResource("/Font", this, objects, font.objNumber);
                } else {                        // Indirect resources object
                    AddResource("/Font", objects[Int32.Parse(token) - 1], objects, font.objNumber);
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
                String token = dict[i];
                if (token.Equals("[")) {
                    // Array of content objects
                    while (true) {
                        i += 1;
                        token = dict[i];
                        if (token.Equals("]")) {
                            dict.Insert(i, "R");
                            dict.Insert(i, "0");
                            dict.Insert(i, objNumber);
                            return;
                        }
                        i += 2;     // Skip the 0 and R
                    }
                } else {
                    // Single content object
                    PDFobj obj2 = objects[Int32.Parse(token) - 1];
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
                String token = dict[i];
                if (token.Equals("[")) {
                    // Array of content object streams
                    i += 1;
                    dict.Insert(i, "R");
                    dict.Insert(i, "0");
                    dict.Insert(i, objNumber);
                    return;
                } else {
                    // Single content object
                    PDFobj obj2 = objects[Int32.Parse(token) - 1];
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
                String token = dict[i + 1];
                if (token.Equals("<<")) {
                    obj = this;
                    index = i + 2;
                } else {
                    obj = objects[Int32.Parse(token) - 1];
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
        } else if (obj.dict[k + 1].Equals("<<")) {
            index = k + 2;
        } else {                                    // "/ExtGState 12 0 R"
            obj = objects[Int32.Parse(obj.dict[k + 1]) - 1];
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
