/*
 * PDFobj.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;

/**
 * An object of a PDF that was read with PDF.read, which holds the tokens of
 * its dictionary and its stream. See Example_20, Example_37 and Example_50.
 */
public class PDFobj {
    /** The object number. */
    protected int number;           // The object number
    /** The byte offset of the object. */
    protected int offset;           // The object offset
    /** The tokens of the object dictionary. */
    protected List<String> dict;
    /** The byte offset of the stream. */
    protected int streamOffset;
    /** The compressed stream. */
    protected byte[] stream;        // The compressed stream
    /** The decompressed data. */
    protected byte[] data;          // The decompressed data
    /** The number of the graphics state resource, or -1. */
    protected int gsNumber = -1;

    /**
     * Creates an object with an empty dictionary.
     */
    protected PDFobj() {
        this.dict = new ArrayList<String>();
    }

    /**
     * Returns the PDFobj number.
     *
     * @return the PDFobj number.
     */
    public int getNumber() {
        return this.number;
    }

    /**
     * Returns the object dictionary.
     *
     * @return the object dictionary.
     */
    public List<String> getDict() {
        return this.dict;
    }

    /**
     * Returns the uncompressed stream data.
     *
     * @return the uncompressed stream data.
     */
    public byte[] getData() {
        return this.data;
    }

    /**
     * Copies the stream from the buffer, and decodes it with the filters of
     * its /Filter entry.
     *
     * @param buf the PDF bytes.
     * @param length the length of the stream.
     * @throws Exception if the stream cannot be decoded.
     */
    protected void setStreamAndData(byte[] buf, int length) throws Exception {
        setStreamAndData(buf, length, null);
    }

    // Copies the stream from the buffer, decrypts it when the PDF is encrypted,
    // and decodes it with the filters of its /Filter entry. The decrypted
    // stream replaces the encrypted one, so that it can be copied.
    void setStreamAndData(byte[] buf, int length, Decryptor decryptor) throws Exception {
        if (this.stream == null) {
            this.stream = new byte[length];
            System.arraycopy(buf, streamOffset, stream, 0, length);
            if (decryptor != null) {
                this.stream = decryptor.decryptStream(this, stream);
                setLength(stream.length);
            }
            this.data = decode(stream);
        }
    }

    // Sets the /Length of the stream, replacing a reference to the length.
    private void setLength(int length) {
        int i = dict.indexOf("/Length");
        if (i == -1 || i + 1 >= dict.size()) {
            return;
        }
        if (i + 3 < dict.size() && dict.get(i + 3).equals("R")) {
            dict.remove(i + 3);
            dict.remove(i + 2);
        }
        dict.set(i + 1, String.valueOf(length));
    }

    // Decodes the stream with each filter of its /Filter entry in turn. A
    // filter that is not supported, like DCTDecode, ends the decoding, and
    // the data is what the filters before it decoded.
    private byte[] decode(byte[] stream) throws Exception {
        List<String> filters = getValues("/Filter");
        byte[] decoded = stream;
        for (int i = 0; i < filters.size(); i++) {
            String filter = filters.get(i);
            if (filter.equals("/FlateDecode") || filter.equals("/Fl")) {
                decoded = applyDecodeParms(Decompressor.inflate(decoded), i);
            } else if (filter.equals("/LZWDecode") || filter.equals("/LZW")) {
                decoded = applyDecodeParms(Decompressor.lzwDecode(decoded), i);
            } else if (filter.equals("/ASCIIHexDecode") || filter.equals("/AHx")) {
                decoded = Decompressor.asciiHexDecode(decoded);
            } else if (filter.equals("/ASCII85Decode") || filter.equals("/A85")) {
                decoded = Decompressor.ascii85Decode(decoded);
            } else if (filter.equals("/RunLengthDecode") || filter.equals("/RL")) {
                decoded = Decompressor.runLengthDecode(decoded);
            } else {
                break;
            }
        }
        return decoded;
    }

    // Returns the elements of the array that is the value of the key, or the
    // value itself when it is not an array.
    private List<String> getValues(String key) {
        List<String> values = new ArrayList<String>();
        int i = dict.indexOf(key) + 1;
        if (i == 0 || i >= dict.size()) {
            return values;
        }
        if (!dict.get(i).equals("[")) {
            values.add(dict.get(i));
            return values;
        }
        for (i += 1; i < dict.size() && !dict.get(i).equals("]"); i++) {
            values.add(dict.get(i));
        }
        return values;
    }

    // Undoes the predictor in the parameters of the filter at the index.
    // Images keep it, as they are copied with their stream, and their data
    // is not used.
    private byte[] applyDecodeParms(byte[] decoded, int index) {
        if (getValue("/Subtype").equals("/Image")) {
            return decoded;
        }
        List<String> parms = getDecodeParms(index);
        return Decompressor.applyPredictor(
                decoded,
                getDecodeParm(parms, "/Predictor", 1),
                getDecodeParm(parms, "/Colors", 1),
                getDecodeParm(parms, "/BitsPerComponent", 8),
                getDecodeParm(parms, "/Columns", 1));
    }

    // Returns the tokens of the parameters dictionary of the filter at the
    // index. /DecodeParms is a dictionary for a single filter, or an array
    // with a dictionary or null for each filter.
    private List<String> getDecodeParms(int index) {
        List<String> parms = new ArrayList<String>();
        int i = dict.indexOf("/DecodeParms") + 1;
        if (i == 0 || i >= dict.size()) {
            return parms;
        }
        boolean array = dict.get(i).equals("[");
        if (array) {
            i += 1;
        }
        for (int element = 0; i < dict.size() && !dict.get(i).equals("]"); element++) {
            int start = i;
            if (dict.get(i).equals("<<")) {
                int level = 0;
                do {
                    String token = dict.get(i++);
                    if (token.equals("<<")) {
                        level++;
                    } else if (token.equals(">>")) {
                        level--;
                    }
                } while (level > 0 && i < dict.size());
            } else if (i + 2 < dict.size() && dict.get(i + 2).equals("R")) {
                i += 3;     // A reference to a dictionary, which is not supported.
            } else {
                i += 1;     // null
            }
            if (element == index) {
                if (dict.get(start).equals("<<")) {
                    parms.addAll(dict.subList(start, i));
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
    private static int getDecodeParm(List<String> parms, String key, int defaultValue) {
        int i = parms.indexOf(key);
        if (i != -1 && i + 1 < parms.size()) {
            try {
                return Integer.parseInt(parms.get(i + 1));
            } catch (NumberFormatException e) {
                return defaultValue;
            }
        }
        return defaultValue;
    }

    /**
     * Sets the stream.
     *
     * @param stream the stream.
     */
    protected void setStream(byte[] stream) {
        this.stream = stream;
    }

    /**
     * Sets the object number.
     *
     * @param number the object number.
     */
    protected void setNumber(int number) {
        this.number = number;
    }

    /**
     * Returns the dictionary value for the specified key.
     *
     * @param key the specified key.
     * @return the value.
     */
    public String getValue(String key) {
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals(key)) {
                String token = dict.get(i + 1);
                if (token.equals("<<")) {
                    StringBuilder buffer = new StringBuilder();
                    buffer.append("<< ");
                    i += 2;
                    while (!dict.get(i).equals(">>")) {
                        buffer.append(dict.get(i));
                        buffer.append(" ");
                        i += 1;
                    }
                    buffer.append(">>");
                    return buffer.toString();
                } else if (token.equals("[")) {
                    StringBuilder buffer = new StringBuilder();
                    buffer.append("[ ");
                    i += 2;
                    while (!dict.get(i).equals("]")) {
                        buffer.append(dict.get(i));
                        buffer.append(" ");
                        i += 1;
                    }
                    buffer.append("]");
                    return buffer.toString();
                } else {
                    return token;
                }
            }
        }
        return "";
    }

    /**
     * Returns the object numbers referenced by the specified dictionary key.
     *
     * @param key the key, for example "/Contents".
     * @return the object numbers.
     */
    protected List<Integer> getObjectNumbers(String key) {
        List<Integer> numbers = new ArrayList<Integer>();
        for (int i = 0; i < dict.size(); i++) {
            String token = dict.get(i);
            if (token.equals(key)) {
                String str = dict.get(++i);
                if (str.equals("[")) {
                    while (true) {
                        str = dict.get(++i);
                        if (str.equals("]")) {
                            break;
                        }
                        numbers.add(Integer.valueOf(str));
                        ++i;    // 0
                        ++i;    // R
                    }
                } else {
                    numbers.add(Integer.valueOf(str));
                }
                break;
            }
        }
        return numbers;
    }

    /**
     * Returns the PDF page size.
     *
     * @return the PDF page size.
     */
    public float[] getPageSize() {
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals("/MediaBox")) {
                return new float[] {
                        Float.parseFloat(dict.get(i + 4)),
                        Float.parseFloat(dict.get(i + 5)) };
            }
        }
        return Letter.PORTRAIT;
    }

    /**
     * Returns the length of the stream, resolving an indirect reference.
     *
     * @param objects the objects in the PDF.
     * @return the length of the stream.
     */
    protected int getLength(List<PDFobj> objects) {
        for (int i = 0; i < dict.size(); i++) {
            String token = dict.get(i);
            if (token.equals("/Length")) {
                int number = Integer.parseInt(dict.get(i + 1));
                if (dict.get(i + 2).equals("0") &&
                        dict.get(i + 3).equals("R")) {
                    return getLength(objects, number);
                } else {
                    return number;
                }
            }
        }
        return 0;
    }

    /**
     * Returns the length stored in the object with the specified number.
     *
     * @param objects the objects in the PDF.
     * @param number the object number.
     * @return the length.
     */
    protected int getLength(List<PDFobj> objects, int number) {
        for (PDFobj obj : objects) {
            if (obj.number == number) {
                return Integer.parseInt(obj.dict.get(3));
            }
        }
        return 0;
    }

    /**
     * Returns the PDF contents object.
     * The content of a page can be split into several streams, listed in an
     * array that is either in the page dictionary or an object of its own.
     * Together they are one content stream, so they are returned joined in
     * a new object, which is not in the objects list.
     *
     * @param objects the objects list.
     * @return the contents object, or null if the page has no contents.
     */
    public PDFobj getContentObject(List<PDFobj> objects) {
        List<Integer> numbers = getObjectNumbers("/Contents");
        if (numbers.size() == 1) {
            PDFobj obj = objects.get(numbers.get(0) - 1);
            if (obj.stream != null) {
                return obj;
            }
            // "/Contents 39 0 R" where the object is "[ 41 0 R 43 0 R ]"
            numbers = new ArrayList<Integer>();
            int i = obj.dict.indexOf("[");
            while (i != -1 && i + 3 < obj.dict.size() && obj.dict.get(i + 3).equals("R")) {
                numbers.add(Integer.valueOf(obj.dict.get(i + 1)));
                i += 3;
            }
        }
        if (numbers.isEmpty()) {
            return null;
        }
        if (numbers.size() == 1) {
            return objects.get(numbers.get(0) - 1);
        }
        ByteArrayOutputStream buf = new ByteArrayOutputStream();
        for (int number : numbers) {
            byte[] data = objects.get(number - 1).data;
            if (data != null) {
                buf.write(data, 0, data.length);
                buf.write('\n');    // A stream can end in the middle of a line.
            }
        }
        PDFobj content = new PDFobj();
        content.data = buf.toByteArray();
        return content;
    }

    /**
     * Returns the PDF resources object.
     *
     * @param objects the objects list.
     * @return the resources object.
     */
    public PDFobj getResourcesObject(List<PDFobj> objects) {
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals("/Resources")) {
                String token = dict.get(i + 1);
                if (token.equals("<<")) {
                    return this;
                }
                return objects.get(Integer.parseInt(token) - 1);
            }
        }
        return null;
    }

    /**
     * Adds core font resource object to the PDF.
     *
     * @param coreFont the core font.
     * @param objects the objects list.
     * @return the font object.
     */
    public Font addResource(int coreFont, List<PDFobj> objects) {
        Font font = new Font(coreFont);
        font.fontID = font.name.replace('-', '_').toUpperCase();

        PDFobj obj = new PDFobj();
        obj.dict.add("<<");
        obj.dict.add("/Type");
        obj.dict.add("/Font");
        obj.dict.add("/Subtype");
        obj.dict.add("/Type1");
        obj.dict.add("/BaseFont");
        obj.dict.add("/" + font.name);
        if (!font.name.equals("Symbol") && !font.name.equals("ZapfDingbats")) {
            obj.dict.add("/Encoding");
            obj.dict.add("/WinAnsiEncoding");
        }
        obj.dict.add(">>");
        obj.number = objects.size() + 1;
        objects.add(obj);

        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals("/Resources")) {
                String token = dict.get(++i);
                if (token.equals("<<")) {                       // Direct resources object
                    addFontResource(this, objects, font.fontID, obj.number);
                } else if (token.charAt(0) >= '0' && token.charAt(0) <= '9') {  // Indirect resources object
                    addFontResource(objects.get(Integer.parseInt(token) - 1), objects, font.fontID, obj.number);
                }
            }
        }

        return font;
    }

    private void addFontResource(
            PDFobj obj, List<PDFobj> objects, String fontID, int number) {
        boolean fonts = false;
        for (String token : obj.dict) {
            if (token.equals("/Font")) {
                fonts = true;
                break;
            }
        }
        if (!fonts) {
            // Direct resources follow "/Resources <<" in the page dictionary;
            // an indirect resources object has no /Resources key, and its
            // entries follow its first "<<".
            int i = obj.dict.indexOf("/Resources");
            i = (i == -1) ? obj.dict.indexOf("<<") + 1 : i + 2;
            obj.dict.addAll(i, Arrays.asList("/Font", "<<", ">>"));
        }

        for (int i = 0; i < obj.dict.size(); i++) {
            if (obj.dict.get(i).equals("/Font")) {
                String token = obj.dict.get(i + 1);
                if (token.equals("<<")) {
                    obj.dict.add(i + 2, "/" + fontID);
                    obj.dict.add(i + 3, String.valueOf(number));
                    obj.dict.add(i + 4, "0");
                    obj.dict.add(i + 5, "R");
                    return;
                } else if (token.charAt(0) >= '0' && token.charAt(0) <= '9') {
                    PDFobj o2 = objects.get(Integer.parseInt(token) - 1);
                    for (int j = 0; j < o2.dict.size(); j++) {
                        if (o2.dict.get(j).equals("<<")) {
                            o2.dict.add(j + 1, "/" + fontID);
                            o2.dict.add(j + 2, String.valueOf(number));
                            o2.dict.add(j + 3, "0");
                            o2.dict.add(j + 4, "R");
                            return;
                        }
                    }
                }
            }
        }
    }

    private void insertNewObject(
            List<String> dict, List<String> list, String type) {
        for (String token : dict) {
            if (token.equals(list.get(0))) {
                return;
            }
        }
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals(type)) {
                dict.addAll(i + 2, list);
                return;
            }
        }
        if (dict.get(3).equals("<<")) {
            dict.addAll(4, list);
            return;
        }
    }

    private void addResource(
            String type, PDFobj obj, List<PDFobj> objects, int objNumber) {
        String tag = type.equals("/Font") ? "/F" : "/Im";
        String number = String.valueOf(objNumber);
        List<String> list = Arrays.asList(tag + number, number, "0", "R");
        for (int i = 0; i < obj.dict.size(); i++) {
            String token = obj.dict.get(i);
            if (token.equals(type)) {
                token = obj.dict.get(i + 1);
                if (token.equals("<<")) {
                    insertNewObject(obj.dict, list, type);
                } else {
                    insertNewObject(objects.get(Integer.parseInt(token) - 1).dict, list, type);
                }
                return;
            }
        }

        // Handle the case where the page originally does not have any font resources.
        list = Arrays.asList(type, "<<", tag + number, number, "0", "R", ">>");
        for (int i = 0; i < obj.dict.size(); i++) {
            if (obj.dict.get(i).equals("/Resources")) {
                obj.dict.addAll(i + 2, list);
                return;
            }
        }
        for (int i = 0; i < obj.dict.size(); i++) {
            if (obj.dict.get(i).equals("<<")) {
                obj.dict.addAll(i + 1, list);
                return;
            }
        }
    }

    /**
     * Adds image resource object to the PDF.
     *
     * @param image the image resource object.
     * @param objects the objects list.
     */
    public void addResource(Image image, List<PDFobj> objects) {
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals("/Resources")) {
                String token = dict.get(i + 1);
                if (token.equals("<<")) {   // Direct resources object
                    addResource("/XObject", this, objects, image.objNumber);
                } else {                    // Indirect resources object
                    addResource("/XObject", objects.get(Integer.parseInt(token) - 1), objects, image.objNumber);
                }
                return;
            }
        }
    }

    /**
     * Adds font resource to the PDF.
     *
     * @param font the font.
     * @param objects the objects list.
     */
    public void addResource(Font font, List<PDFobj> objects) {
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals("/Resources")) {
                String token = dict.get(i + 1);
                if (token.equals("<<")) {   // Direct resources object
                    addResource("/Font", this, objects, font.objNumber);
                } else {                    // Indirect resources object
                    addResource("/Font", objects.get(Integer.parseInt(token) - 1), objects, font.objNumber);
                }
                return;
            }
        }
    }

    /**
     * Adds content to the PDF.
     *
     * @param content the content.
     * @param objects the objects list.
     */
    public void addContent(byte[] content, List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.setNumber(objects.size() + 1);
        obj.setStream(content);
        objects.add(obj);

        String objNumber = String.valueOf(obj.number);
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals("/Contents")) {
                i += 1;
                String token = dict.get(i);
                if (token.equals("[")) {
                    // Array of content objects
                    while (true) {
                        i += 1;
                        token = dict.get(i);
                        if (token.equals("]")) {
                            dict.add(i, "R");
                            dict.add(i, "0");
                            dict.add(i, objNumber);
                            return;
                        }
                        i += 2;     // Skip the 0 and R
                    }
                } else {
                    // Single content object
                    PDFobj obj2 = objects.get(Integer.parseInt(token) - 1);
                    if (obj2.data == null && obj2.stream == null) {
                        // This is not a stream object!
                        for (int j = 0; j < obj2.dict.size(); j++) {
                            if (obj2.dict.get(j).equals("]")) {
                                obj2.dict.add(j, "R");
                                obj2.dict.add(j, "0");
                                obj2.dict.add(j, objNumber);
                                return;
                            }
                        }
                    }
                    dict.add(i, "[");
                    dict.add(i + 4, "]");
                    dict.add(i + 4, "R");
                    dict.add(i + 4, "0");
                    dict.add(i + 4, objNumber);
                    return;
                }
            }
        }
    }

    /**
     * Adds new content object before the existing content objects.
     * The original code was provided by Stefan Ostermann author of ScribMaster and HandWrite Pro.
     * Additional code to handle PDFs with indirect array of stream objects was written by EDragoev.
     *
     * @param content the new content object to be added
     * @param objects the existing list of content objects
     */
    public void addPrefixContent(byte[] content, List<PDFobj> objects) {
        PDFobj obj = new PDFobj();
        obj.setNumber(objects.size() + 1);
        obj.setStream(content);
        objects.add(obj);

        String objNumber = String.valueOf(obj.number);
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals("/Contents")) {
                i += 1;
                String token = dict.get(i);
                if (token.equals("[")) {
                    // Array of content object streams
                    i += 1;
                    dict.add(i, "R");
                    dict.add(i, "0");
                    dict.add(i, objNumber);
                    return;
                } else {
                    // Single content object
                    PDFobj obj2 = objects.get(Integer.parseInt(token) - 1);
                    if (obj2.data == null && obj2.stream == null) {
                        // This is not a stream object!
                        for (int j = 0; j < obj2.dict.size(); j++) {
                            if (obj2.dict.get(j).equals("[")) {
                                j += 1;
                                obj2.dict.add(j, "R");
                                obj2.dict.add(j, "0");
                                obj2.dict.add(j, objNumber);
                                return;
                            }
                        }
                    }
                    dict.add(i, "[");
                    dict.add(i + 4, "]");
                    i += 1;
                    dict.add(i, "R");
                    dict.add(i, "0");
                    dict.add(i, objNumber);
                    return;
                }
            }
        }
    }

    // Returns the largest number of the /GS names in the dictionary, or 0. A
    // name with no number after /GS, like /GSa, is skipped.
    private int getMaxGSNumber(PDFobj obj) {
        int maxGSNumber = 0;
        for (String token : obj.dict) {
            if (token.startsWith("/GS")) {
                try {
                    maxGSNumber = Math.max(maxGSNumber, Integer.parseInt(token.substring(3)));
                } catch (NumberFormatException e) {
                    // Not a /GS name with a number.
                }
            }
        }
        return maxGSNumber;
    }

    /**
     * Sets the graphics state of the PDF.
     *
     * @param gs the graphics state.
     * @param objects the objects list.
     * @return this PDFobj object.
     */
    public PDFobj setGraphicsState(GraphicsState gs, List<PDFobj> objects) {
        PDFobj obj = null;
        int index = -1;
        for (int i = 0; i < dict.size(); i++) {
            if (dict.get(i).equals("/Resources")) {
                String token = dict.get(i + 1);
                if (token.equals("<<")) {
                    obj = this;
                    index = i + 2;
                } else {
                    obj = objects.get(Integer.parseInt(token) - 1);
                    for (int j = 0; j < obj.dict.size(); j++) {
                        if (obj.dict.get(j).equals("<<")) {
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
        int i = index;
        while (i < obj.dict.size() && !obj.dict.get(i).equals("/ExtGState")) {
            i++;
        }
        if (i == obj.dict.size()) {
            obj.dict.addAll(index, Arrays.asList("/ExtGState", "<<", ">>"));
            index += 2;
        } else if (obj.dict.get(i + 1).equals("<<")) {
            index = i + 2;
        } else {                                // "/ExtGState 12 0 R"
            obj = objects.get(Integer.parseInt(obj.dict.get(i + 1)) - 1);
            index = obj.dict.indexOf("<<") + 1;
        }
        gsNumber = getMaxGSNumber(obj);
        String name = "/GS" + (gsNumber + 1);
        obj.dict.addAll(index, Arrays.asList(
                name, "<<",
                "/CA", String.valueOf(gs.getAlphaStroking()),
                "/ca", String.valueOf(gs.getAlphaNonStroking()), ">>"));
        addPrefixContent(("q\n" + name + " gs\n").getBytes(StandardCharsets.UTF_8), objects);
        return this;
    }
}
