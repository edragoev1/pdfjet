/*
 * XMLParser.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.HashSet;
import java.util.List;
import java.util.Map;
import java.util.Set;

/**
 * Reads XML into elements: their names, their namespaces, their attributes in
 * the order they are written, and their text. It is the one XML parser of
 * PDFjet, the same in the four ports, which reads the SVG images of the library
 * and the electronic invoices of PDFjet Pro, so that every port reads them
 * alike. It reads what such a document is made of and no more, and what it
 * leaves out is what makes XML dangerous to read:
 * <ul>
 * <li>A document type declaration, a DOCTYPE, is refused by {@link #parse},
 * and skipped by {@link #parseSkippingDoctype}, as the files of the drawing
 * programs have one; its entities are never read: an entity that names a file
 * or a URL reads what it should not, and the ones that stand for each other
 * grow a short document into gigabytes.</li>
 * <li>The entities are the five of XML, &amp;lt;, &amp;gt;, &amp;amp;,
 * &amp;quot; and &amp;apos;, and the numeric ones, such as &amp;#160; and
 * &amp;#xA0;. Any other is an error, the ones a DOCTYPE declares among
 * them.</li>
 * <li>The elements nest MAX_DEPTH levels at most, and the reading does not
 * recurse, so no document overflows the stack.</li>
 * <li>The document is 20 MB at most, and has MAX_ELEMENTS elements at most.</li>
 * <li>The namespaces are looked up in the elements that declare them, from
 * the innermost out, and not copied into each element, so that no number of
 * declarations makes the reading slower than the length of the document
 * times its depth.</li>
 * <li>Nothing is fetched, and no schema is read.</li>
 * </ul>
 * What is read is XML, and what is not is an error: a character that XML
 * does not have, such as U+0000 or U+FFFF, written or as an entity; an
 * attribute written twice, or not after a space; and a prefix that no element
 * declares. A line break is a line feed, as XML reads a carriage return and
 * the line feed after it, and the tabs and line breaks in the value of an
 * attribute are spaces, as XML normalizes them.
 * <p>
 * The document is UTF-8, or UTF-16 when it starts with a byte order mark;
 * another encoding in the declaration is an error. The bytes that are not
 * valid UTF-8 are the character U+FFFD, one for each part of a sequence that
 * is as much of a character as it is valid for, which is what the Unicode
 * Standard recommends and the decoders of the other ports do. A code unit of
 * UTF-16 that is a surrogate without the other one of its pair is U+FFFD as
 * well.
 */
public final class XMLParser {
    /** How deep the elements of a document may nest. */
    public static final int MAX_DEPTH = 256;

    /**
     * How many elements a document may have, a million: an invoice of the 20 MB
     * has fewer than half as many, and an SVG image has its points in the
     * attributes of its paths, not in elements. It keeps a document of tiny
     * elements from taking hundreds of megabytes (the review of 9 October 2026).
     */
    public static final int MAX_ELEMENTS = 1000000;

    /** How many bytes a document may be, 20 MB. */
    public static final int MAX_SIZE = 20 << 20;

    /** The message of the XMLException of a document of more than 20 MB. */
    public static final String TOO_LARGE = "The document is more than 20 MB.";

    // The namespace of the prefix xml, which every document has without
    // declaring it.
    private static final String NAMESPACE_XML = "http://www.w3.org/XML/1998/namespace";

    // How many names are kept once each, so that a document of names all
    // different does not fill the map.
    private static final int MAX_NAMES = 4096;

    // Past how many attributes of an element they are compared by a set, and
    // before by one another.
    private static final int MAX_COMPARED = 16;

    private final String xml;
    // True to skip a DOCTYPE, as SVG images have one, and false to refuse it,
    // and to read a prefix that no element declares as no namespace, as SVG
    // files copied from web pages have xlink:href without xmlns:xlink.
    private final boolean skipDoctype;
    // The names read, each kept once, as a document repeats a few names
    // millions of times.
    private final Map<String, String> names = new HashMap<String, String>();
    // The elements read, MAX_ELEMENTS at most
    private int elements;
    private int index;
    private int line = 1;
    private int column = 1;

    private XMLParser(String xml, boolean skipDoctype) {
        this.xml = xml;
        this.skipDoctype = skipDoctype;
    }

    /**
     * Reads the document and returns its root element. A document type
     * declaration, a DOCTYPE, is refused.
     *
     * @param bytes the document.
     * @return the root element.
     * @throws XMLException if the document is not the XML this reads, has a
     *     DOCTYPE, or is more than 20 MB.
     */
    public static XMLNode parse(byte[] bytes) throws XMLException {
        return parse(bytes, false);
    }

    /**
     * Reads the document and returns its root element, as {@link #parse} does,
     * but skips a document type declaration, a DOCTYPE, as SVG images have one.
     * The entities it declares are not read: a reference to one is an error. A
     * prefix that no element declares is read as no namespace, as SVG files
     * copied from web pages have xlink:href without xmlns:xlink, rather than an
     * error.
     *
     * @param bytes the document.
     * @return the root element.
     * @throws XMLException if the document is not the XML this reads, or is
     *     more than 20 MB.
     */
    public static XMLNode parseSkippingDoctype(byte[] bytes) throws XMLException {
        return parse(bytes, true);
    }

    private static XMLNode parse(byte[] bytes, boolean skipDoctype) throws XMLException {
        if (bytes.length > MAX_SIZE) {
            throw new XMLException(TOO_LARGE);
        }
        StringBuilder normalized = new StringBuilder();
        int bad = normalize(decode(bytes), normalized);
        XMLParser parser = new XMLParser(normalized.toString(), skipDoctype);
        if (bad != -1) {
            parser.skip(bad);
            throw parser.error(String.format("The character U+%04X is not a character of XML",
                    (int) parser.xml.charAt(bad)));
        }
        return parser.document();
    }

    /**
     * Reads the document of the stream and returns its root element. The
     * stream is read to its end, or to the 20 MB a document may be, and is not
     * closed. A DOCTYPE is refused.
     *
     * @param in the stream.
     * @return the root element.
     * @throws IOException if the stream cannot be read.
     * @throws XMLException if the document is not the XML this reads.
     */
    public static XMLNode parse(InputStream in) throws IOException, XMLException {
        return parse(readAll(in), false);
    }

    /**
     * Reads the document of the stream as {@link #parse(InputStream)} does, but
     * skips a DOCTYPE, as {@link #parseSkippingDoctype(byte[])} does.
     *
     * @param in the stream.
     * @return the root element.
     * @throws IOException if the stream cannot be read.
     * @throws XMLException if the document is not the XML this reads.
     */
    public static XMLNode parseSkippingDoctype(InputStream in) throws IOException, XMLException {
        return parse(readAll(in), true);
    }

    // The bytes of the stream, to its end or past the 20 MB a document may be.
    private static byte[] readAll(InputStream in) throws IOException, XMLException {
        ByteArrayOutputStream bytes = new ByteArrayOutputStream();
        byte[] buffer = new byte[8192];
        int count;
        while ((count = in.read(buffer)) > 0) {
            bytes.write(buffer, 0, count);
            if (bytes.size() > MAX_SIZE) {
                throw new XMLException(TOO_LARGE);
            }
        }
        return bytes.toByteArray();
    }

    // The characters of the document: UTF-8, or the UTF-16 of a byte order
    // mark, without the mark. The bytes that are not valid UTF-8 are the
    // character U+FFFD, one for each maximal part of a sequence.
    private static String decode(byte[] bytes) {
        if (bytes.length >= 2 && (bytes[0] & 0xFF) == 0xFE && (bytes[1] & 0xFF) == 0xFF) {
            return decodeUTF16(bytes, 2, true);
        }
        if (bytes.length >= 2 && (bytes[0] & 0xFF) == 0xFF && (bytes[1] & 0xFF) == 0xFE) {
            return decodeUTF16(bytes, 2, false);
        }
        if (bytes.length >= 3 && (bytes[0] & 0xFF) == 0xEF
                && (bytes[1] & 0xFF) == 0xBB && (bytes[2] & 0xFF) == 0xBF) {
            return decodeUTF8(bytes, 3);
        }
        return decodeUTF8(bytes, 0);
    }

    // The characters of the UTF-8, with one U+FFFD in the place of each
    // maximal subpart of a sequence that is not valid: the bytes that begin a
    // character and go on as it may go on, up to the byte that it may not, as
    // the Unicode Standard recommends and the decoders of the other ports do.
    // The decoder of the JDK makes one U+FFFD of the three bytes of a
    // surrogate, where this makes three, as Go, .NET and Swift do.
    static String decodeUTF8(byte[] bytes, int from) {
        StringBuilder sb = new StringBuilder(bytes.length - from);
        int i = from;
        while (i < bytes.length) {
            int lead = bytes[i] & 0xFF;
            if (lead < 0x80) {
                sb.append((char) lead);
                i++;
                continue;
            }
            int follow = 0;
            int low = 0x80;
            int high = 0xBF;
            if (lead >= 0xC2 && lead <= 0xDF) {
                follow = 1;
            } else if (lead == 0xE0) {
                follow = 2;
                low = 0xA0;
            } else if (lead == 0xED) {
                follow = 2;
                high = 0x9F;
            } else if (lead >= 0xE1 && lead <= 0xEF) {
                follow = 2;
            } else if (lead == 0xF0) {
                follow = 3;
                low = 0x90;
            } else if (lead == 0xF4) {
                follow = 3;
                high = 0x8F;
            } else if (lead >= 0xF1 && lead <= 0xF3) {
                follow = 3;
            }
            int size = 1;
            int code = lead & (0x3F >> follow);
            for (; size <= follow && i + size < bytes.length; size++) {
                int next = bytes[i + size] & 0xFF;
                if (next < low || next > high) {
                    break;
                }
                code = (code << 6) | (next & 0x3F);
                low = 0x80;
                high = 0xBF;
            }
            if (follow > 0 && size == follow + 1) {
                sb.appendCodePoint(code);
            } else {
                sb.append('\uFFFD');
            }
            i += size;
        }
        return sb.toString();
    }

    // Makes each line break of the document a line feed, as XML reads a
    // carriage return and the line feed after it, and a carriage return on its
    // own, into out, and returns where the first character is that is not a
    // character of XML, or -1.
    private static int normalize(String xml, StringBuilder out) {
        int bad = -1;
        for (int i = 0; i < xml.length(); i++) {
            char ch = xml.charAt(i);
            if (ch == '\r') {
                ch = '\n';
                if (i + 1 < xml.length() && xml.charAt(i + 1) == '\n') {
                    i++;
                }
            } else if (bad == -1 && !isXMLCharacter(ch)) {
                bad = out.length();
            }
            out.append(ch);
        }
        return bad;
    }

    // Whether the code unit is of a character XML has: all but the control
    // characters other than the tab and the line breaks, and U+FFFE and
    // U+FFFF. A surrogate of the document is one of a pair, as a surrogate
    // without the other half of its pair is read as U+FFFD.
    private static boolean isXMLCharacter(char ch) {
        return ch == '\t' || ch == '\n' || ch == '\r' || (ch >= 0x20 && ch != 0xFFFE && ch != 0xFFFF);
    }

    /**
     * The characters of UTF-16, after the byte order mark. A half of a
     * character that has no other half, and a last byte with no pair, is the
     * replacement character, and what follows is read as it stands. The
     * decoder of Java swallows the code unit after a lone half instead, which
     * the decoders of the other three ports do not, and a document is read the
     * same way by all four.
     *
     * @param bytes the bytes of UTF-16.
     * @param from where the characters start, after the byte order mark.
     * @param bigEndian true for UTF-16BE, false for UTF-16LE.
     * @return the characters.
     */
    public static String decodeUTF16(byte[] bytes, int from, boolean bigEndian) {
        StringBuilder sb = new StringBuilder();
        int i = from;
        while (i + 1 < bytes.length) {
            char unit = unitAt(bytes, i, bigEndian);
            i += 2;
            if (Character.isHighSurrogate(unit) && i + 1 < bytes.length) {
                char next = unitAt(bytes, i, bigEndian);
                if (Character.isLowSurrogate(next)) {
                    sb.append(unit).append(next);
                    i += 2;
                    continue;
                }
            }
            sb.append(Character.isSurrogate(unit) ? '�' : unit);
        }
        if (i < bytes.length) {
            sb.append('�');
        }
        return sb.toString();
    }

    private static char unitAt(byte[] bytes, int i, boolean bigEndian) {
        int first = bytes[bigEndian ? i : i + 1] & 0xFF;
        int second = bytes[bigEndian ? i + 1 : i] & 0xFF;
        return (char) ((first << 8) | second);
    }

    // --- The document -------------------------------------------------------

    private XMLNode document() throws XMLException {
        prolog();
        XMLNode root = element();
        while (index < xml.length()) {
            if (isWhitespace(peek())) {
                next();
            } else if (xml.startsWith("<!--", index)) {
                comment();
            } else if (xml.startsWith("<?", index)) {
                processingInstruction();
            } else {
                throw error("There is more than the one element of the document");
            }
        }
        return root;
    }

    // The declaration, the comments, the processing instructions and the
    // DOCTYPE before the root element; the DOCTYPE skipped or refused.
    private void prolog() throws XMLException {
        while (index < xml.length()) {
            if (isWhitespace(peek())) {
                next();
            } else if (index == 0 && xml.startsWith("<?xml", index)
                    && index + 5 < xml.length() && isWhitespace(xml.charAt(index + 5))) {
                // The declaration, and not a processing instruction whose name
                // starts with xml, as xml-stylesheet (the review of 9 October 2026)
                declaration();
            } else if (xml.startsWith("<!--", index)) {
                comment();
            } else if (xml.startsWith("<?", index)) {
                processingInstruction();
            } else if (xml.startsWith("<!DOCTYPE", index)) {
                if (!skipDoctype) {
                    throw error("A document type declaration is not read, as its entities are not");
                }
                doctype();
            } else if (peek() == '<') {
                return;
            } else {
                throw error("The document starts with text");
            }
        }
        throw error("The document has no element");
    }

    // <!DOCTYPE svg PUBLIC "..." "..." [ ... ]>: skipped to its end, its
    // internal subset too, past the > of its declarations, of its comments and
    // of its quoted strings. Its entities are not read.
    private void doctype() throws XMLException {
        skip("<!DOCTYPE".length());
        int depth = 0;
        while (index < xml.length()) {
            if (xml.startsWith("<!--", index)) {
                int end = xml.indexOf("-->", index + 4);
                if (end == -1) {
                    throw error("A comment of the document type declaration does not end");
                }
                skip(end + 3 - index);
                continue;
            }
            if (xml.startsWith("<?", index)) {
                // A processing instruction, whose text can hold a quote, as
                // <?pi don't ?> (the review of 9 October 2026)
                int end = xml.indexOf("?>", index + 2);
                if (end == -1) {
                    throw error("A processing instruction of the document type declaration does not end");
                }
                skip(end + 2 - index);
                continue;
            }
            char ch = peek();
            if (ch == '"' || ch == '\'') {
                char quote = next();
                while (index < xml.length() && peek() != quote) {
                    next();
                }
                if (index >= xml.length()) {
                    throw error("A string of the document type declaration does not end");
                }
            } else if (ch == '[') {
                depth++;
            } else if (ch == ']') {
                depth--;
            } else if (ch == '>' && depth <= 0) {
                next();
                return;
            }
            next();
        }
        throw error("The document type declaration does not end");
    }

    // <?xml version="1.0" encoding="UTF-8"?>: the encoding is UTF-8 or the
    // UTF-16 of the byte order mark.
    private void declaration() throws XMLException {
        int end = xml.indexOf("?>", index);
        if (end == -1) {
            throw error("The declaration does not end");
        }
        String declaration = xml.substring(index, end);
        int encoding = declaration.indexOf("encoding");
        if (encoding != -1) {
            int quote = -1;
            for (int i = encoding + "encoding".length(); i < declaration.length(); i++) {
                char ch = declaration.charAt(i);
                if (ch == '"' || ch == '\'') {
                    quote = i;
                    break;
                }
            }
            int close = (quote == -1) ? -1 : declaration.indexOf(declaration.charAt(quote), quote + 1);
            String name = (close == -1) ? "" : declaration.substring(quote + 1, close);
            if (!name.equalsIgnoreCase("UTF-8") && !name.equalsIgnoreCase("UTF-16")
                    && !name.equalsIgnoreCase("UTF-16BE") && !name.equalsIgnoreCase("UTF-16LE")) {
                throw error("The encoding " + name + " is not read, only UTF-8 and UTF-16");
            }
        }
        skip(end + 2 - index);
    }

    private void comment() throws XMLException {
        int end = xml.indexOf("-->", index);
        if (end == -1) {
            throw error("The comment does not end");
        }
        skip(end + 3 - index);
    }

    private void processingInstruction() throws XMLException {
        int end = xml.indexOf("?>", index);
        if (end == -1) {
            throw error("The processing instruction does not end");
        }
        skip(end + 2 - index);
    }

    // --- The elements -------------------------------------------------------

    // The root element and the elements in it, which are read one after
    // another with a stack and not by recursion, so that a document of any
    // depth is read in the stack of this call alone.
    private XMLNode element() throws XMLException {
        XMLNode root = null;
        List<XMLNode> open = new ArrayList<XMLNode>();
        // The namespaces each open element declares, null where it declares
        // none.
        List<Map<String, String>> scopes = new ArrayList<Map<String, String>>();
        while (true) {
            if (index >= xml.length()) {
                throw error("The element " + open.get(open.size() - 1).getName() + " does not end");
            }
            if (xml.startsWith("<!--", index)) {
                comment();
            } else if (xml.startsWith("<![CDATA[", index)) {
                characterData(open);
            } else if (xml.startsWith("<?", index)) {
                processingInstruction();
            } else if (xml.startsWith("</", index)) {
                closeTag(open, scopes);
                if (open.isEmpty()) {
                    return root;
                }
            } else if (peek() == '<') {
                if (open.size() >= MAX_DEPTH) {
                    throw error("The elements nest more than " + MAX_DEPTH + " deep");
                }
                if (root != null && open.isEmpty()) {
                    throw error("There is more than the one element of the document");
                }
                if (elements == MAX_ELEMENTS) {
                    throw error("The document has more than " + MAX_ELEMENTS + " elements");
                }
                elements++;
                XMLNode node = openTag(open, scopes);
                if (root == null) {
                    root = node;
                }
                if (open.isEmpty()) {
                    return root;     // An empty root element, <svg/>
                }
            } else {
                text(open);
            }
        }
    }

    // <name attribute="value"> or <name/>: the element, added to the one it is
    // in, and open until its closing tag unless it closes itself.
    private XMLNode openTag(List<XMLNode> open, List<Map<String, String>> scopes)
            throws XMLException {
        next();     // The <
        String name = name();
        if (name.isEmpty()) {
            throw error("A tag has no name");
        }
        Map<String, String> declared = null;
        List<String> attributes = new ArrayList<String>();   // Names and values
        Set<String> written = null;     // Past a few attributes, which are compared one by one
        for (boolean first = true; ; first = false) {
            boolean spaced = index < xml.length() && isWhitespace(peek());
            skipWhitespace();
            char ch = peek();
            if (ch == '>' || ch == '/') {
                break;
            }
            // An attribute is after a space: XML has no name="1"other="2".
            if (!spaced) {
                if (first) {
                    throw error("The name of the tag " + name + " is not followed by a space");
                }
                throw error("The attributes of " + name + " are not separated by a space");
            }
            String attributeName = name();
            if (attributeName.isEmpty()) {
                throw error("The attributes of " + name + " are not a name and a value");
            }
            skipWhitespace();
            if (peek() != '=') {
                throw error("The attribute " + attributeName + " of " + name + " has no value");
            }
            next();
            skipWhitespace();
            String value = attributeValue();
            boolean twice = false;
            if (written != null) {
                twice = written.contains(attributeName);
            } else {
                for (int i = 0; i < attributes.size() && !twice; i += 2) {
                    twice = attributes.get(i).equals(attributeName);
                }
                if (attributes.size() / 2 >= MAX_COMPARED) {
                    written = new HashSet<String>(2 * attributes.size());
                    for (int i = 0; i < attributes.size(); i += 2) {
                        written.add(attributes.get(i));
                    }
                }
            }
            if (twice) {
                throw error("The attribute " + attributeName + " of " + name + " is written twice");
            }
            if (written != null) {
                written.add(attributeName);
            }
            if (attributeName.equals("xmlns") || attributeName.startsWith("xmlns:")) {
                if (declared == null) {
                    declared = new HashMap<String, String>();
                }
                String prefix = "";
                if (!attributeName.equals("xmlns")) {
                    prefix = attributeName.substring("xmlns:".length());
                    if (prefix.isEmpty()) {
                        throw error("A namespace of " + name + " is declared for an empty prefix");
                    }
                    // A prefix stands for a namespace, and the default
                    // namespace is the only one that may be none.
                    if (value.isEmpty()) {
                        throw error("The prefix " + prefix + " of " + name
                                + " is declared as no namespace");
                    }
                }
                declared.put(prefix, value);
            }
            attributes.add(attributeName);
            attributes.add(value);
        }
        // The namespace of the element is the one its prefix stands for in the
        // element itself or in the nearest element it is in that declares it.
        String namespace = namespaceOf(prefixOf(name), declared, scopes);
        if (namespace == null) {
            if (!skipDoctype) {
                throw error("The prefix " + prefixOf(name) + " of " + name + " is not declared");
            }
            namespace = "";
        }
        XMLNode node = new XMLNode(name, keep(XMLNode.localNameOf(name)), namespace);
        for (int i = 0; i < attributes.size() && !skipDoctype; i += 2) {
            String prefix = prefixOf(attributes.get(i));
            if (!prefix.isEmpty() && !prefix.equals("xmlns")
                    && namespaceOf(prefix, declared, scopes) == null) {
                throw error("The prefix " + prefix + " of the attribute " + attributes.get(i)
                        + " of " + name + " is not declared");
            }
        }
        node.setAttributes(attributes.toArray(new String[attributes.size()]));
        if (!open.isEmpty()) {
            open.get(open.size() - 1).addChild(node);
        }
        boolean empty = peek() == '/';
        if (empty) {
            next();
        }
        if (peek() != '>') {
            throw error("The tag " + name + " does not end with >");
        }
        next();
        if (!empty) {
            open.add(node);
            scopes.add(declared);
        }
        return node;
    }

    // The prefix of the name, the part before its colon, or an empty string
    // when it has none.
    private static String prefixOf(String name) {
        int colon = name.indexOf(':');
        return (colon == -1) ? "" : name.substring(0, colon);
    }

    // The namespace the prefix stands for, in the namespaces the element
    // declares and then in the ones of the elements it is in, from the
    // innermost out, or null when it stands for none. No prefix is the default
    // namespace, which is none where no element declares one, and xml is the
    // namespace of XML itself.
    private static String namespaceOf(String prefix, Map<String, String> declared,
            List<Map<String, String>> scopes) {
        if (declared != null && declared.containsKey(prefix)) {
            return declared.get(prefix);
        }
        for (int i = scopes.size() - 1; i >= 0; i--) {
            Map<String, String> scope = scopes.get(i);
            if (scope != null && scope.containsKey(prefix)) {
                return scope.get(prefix);
            }
        }
        if (prefix.isEmpty()) {
            return "";
        }
        return prefix.equals("xml") ? NAMESPACE_XML : null;
    }

    // </name>, which ends the element that is open.
    private void closeTag(List<XMLNode> open, List<Map<String, String>> scopes)
            throws XMLException {
        skip(2);    // The </
        String name = name();
        skipWhitespace();
        if (peek() != '>') {
            throw error("The closing tag of " + name + " does not end with >");
        }
        next();
        if (open.isEmpty()) {
            throw error("The closing tag of " + name + " closes no element");
        }
        XMLNode node = open.remove(open.size() - 1);
        scopes.remove(scopes.size() - 1);
        if (!node.getName().equals(name)) {
            throw error("The element " + node.getName() + " is closed by the tag of " + name);
        }
    }

    // <![CDATA[ the text as it is ]]>
    private void characterData(List<XMLNode> open) throws XMLException {
        int end = xml.indexOf("]]>", index);
        if (end == -1) {
            throw error("The character data does not end");
        }
        if (open.isEmpty()) {
            throw error("There is text outside the element of the document");
        }
        open.get(open.size() - 1).addText(xml.substring(index + "<![CDATA[".length(), end));
        skip(end + 3 - index);
    }

    // The text up to the next <, with its entities read.
    private void text(List<XMLNode> open) throws XMLException {
        StringBuilder buf = new StringBuilder();
        while (index < xml.length() && peek() != '<') {
            char ch = next();
            if (ch == '&') {
                buf.append(entity());
            } else {
                buf.append(ch);
            }
        }
        if (open.isEmpty()) {
            if (buf.toString().trim().isEmpty()) {
                return;
            }
            throw error("There is text outside the element of the document");
        }
        open.get(open.size() - 1).addText(buf.toString());
    }

    // The &lt;, &gt;, &amp;, &quot; and &apos; of XML, and the numeric ones,
    // such as &#160; and &#xA0;. The & is read.
    private String entity() throws XMLException {
        int end = xml.indexOf(';', index);
        if (end == -1 || end - index > 16) {
            throw error("An & that is not an entity, such as &amp;");
        }
        String name = xml.substring(index, end);
        skip(end + 1 - index);
        if (name.equals("lt")) {
            return "<";
        } else if (name.equals("gt")) {
            return ">";
        } else if (name.equals("amp")) {
            return "&";
        } else if (name.equals("quot")) {
            return "\"";
        } else if (name.equals("apos")) {
            return "'";
        } else if (name.startsWith("#")) {
            boolean hex = name.length() > 1 && (name.charAt(1) == 'x' || name.charAt(1) == 'X');
            String digits = name.substring(hex ? 2 : 1);
            int code;
            try {
                // The digits of XML and no others: Integer.parseInt takes a
                // sign, and the digits of every script, so it reads &#١٢; and
                // &#x４１;, which are not numbers of XML.
                code = isNumber(digits, hex) ? Integer.parseInt(digits, hex ? 16 : 10) : -1;
            } catch (NumberFormatException e) {     // More digits than a number holds.
                code = -1;
            }
            if (code == -1) {
                throw error("The character &" + name + "; is not a number");
            }
            if (code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF)
                    || (code <= 0xFFFF && !isXMLCharacter((char) code))) {
                throw error("The character &" + name + "; is not a character of XML");
            }
            return new String(Character.toChars(code));
        }
        throw error("The entity &" + name + "; is not one of XML, and this reads no others");
    }

    // --- The characters -----------------------------------------------------

    // A name of an element or of an attribute: what stands before the
    // whitespace, the =, the / or the > after it.
    private String name() {
        int start = index;
        while (index < xml.length()) {
            char ch = peek();
            if (isWhitespace(ch) || ch == '=' || ch == '/' || ch == '>' || ch == '<') {
                break;
            }
            next();
        }
        return keep(xml.substring(start, index));
    }

    // The name, kept once for the document while there are no more than
    // MAX_NAMES of them.
    private String keep(String name) {
        String kept = names.get(name);
        if (kept != null) {
            return kept;
        }
        if (names.size() < MAX_NAMES) {
            names.put(name, name);
        }
        return name;
    }

    // "value" or 'value', with its entities read.
    private String attributeValue() throws XMLException {
        char quote = peek();
        if (quote != '"' && quote != '\'') {
            throw error("The value of an attribute is not in quotes");
        }
        next();
        StringBuilder buf = new StringBuilder();
        while (true) {
            if (index >= xml.length()) {
                throw error("The value of an attribute does not end");
            }
            char ch = next();
            if (ch == quote) {
                return (buf.length() == 0) ? "" : buf.toString();
            } else if (ch == '<') {
                throw error("The value of an attribute holds a <");
            } else if (ch == '&') {
                buf.append(entity());
            } else if (ch == '\t' || ch == '\n') {
                // XML makes each tab and line break of a value a space, and
                // keeps the ones written as entities, such as &#10;.
                buf.append(' ');
            } else {
                buf.append(ch);
            }
        }
    }

    private char peek() {
        return (index < xml.length()) ? xml.charAt(index) : '\0';
    }

    private char next() {
        char ch = xml.charAt(index++);
        if (ch == '\n') {
            line++;
            column = 1;
        } else {
            column++;
        }
        return ch;
    }

    private void skip(int count) {
        for (int i = 0; i < count && index < xml.length(); i++) {
            next();
        }
    }

    private void skipWhitespace() {
        while (index < xml.length() && isWhitespace(peek())) {
            next();
        }
    }

    // A number of a numeric entity: the digits 0 to 9, and a to f as well when
    // the entity is hexadecimal, in ASCII and in no other script.
    private static boolean isNumber(String digits, boolean hex) {
        if (digits.isEmpty()) {
            return false;
        }
        for (int i = 0; i < digits.length(); i++) {
            char ch = digits.charAt(i);
            if (ch >= '0' && ch <= '9') {
                continue;
            }
            if (hex && ((ch >= 'a' && ch <= 'f') || (ch >= 'A' && ch <= 'F'))) {
                continue;
            }
            return false;
        }
        return true;
    }

    private static boolean isWhitespace(char ch) {
        return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
    }

    private XMLException error(String message) {
        return new XMLException(message + ", at line " + line + " column " + column);
    }
}   // End of XMLParser.java
