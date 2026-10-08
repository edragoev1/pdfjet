/*
 * XMLParser.cs
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
/// Reads the XML of an SVG image into elements: their names, their
/// namespaces, their attributes in the order they are written, and their
/// text. It is the one parser of the four ports, so that they read an SVG
/// alike, and it came from the parser of the electronic invoices of PDFjet
/// Pro. It reads what such a document is made of and no more, and what it
/// leaves out is what makes XML dangerous to read:
/// <list type="bullet">
/// <item><description>A document type declaration, a DOCTYPE, is skipped, as the files of
/// the drawing programs have one, and its entities are not read: an entity
/// that names a file or a URL reads what it should not, and the ones that
/// stand for each other grow a short document into gigabytes.</description></item>
/// <item><description>The entities are the five of XML, &amp;lt;, &amp;gt;, &amp;amp;,
/// &amp;quot; and &amp;apos;, and the numeric ones, such as &amp;#160; and
/// &amp;#xA0;. Any other is an error, the ones a DOCTYPE declares among
/// them.</description></item>
/// <item><description>The elements nest MAX_DEPTH levels at most, and the reading does not
/// recurse, so no document overflows the stack.</description></item>
/// <item><description>The document is 20 MB at most.</description></item>
/// <item><description>The namespaces are looked up in the elements that declare them, from
/// the innermost out, and not copied into each element, so that no number of
/// declarations makes the reading slower than the length of the document
/// times its depth.</description></item>
/// <item><description>Nothing is fetched, and no schema is read.</description></item>
/// </list>
/// What is read is XML, and what is not is an error: a character that XML
/// does not have, such as U+0000 or U+FFFF, written or as an entity; an
/// attribute written twice, or not after a space; and a prefix that no
/// element declares. A line break is a line feed, as XML reads a carriage
/// return and the line feed after it, and the tabs and line breaks in the
/// value of an attribute are spaces, as XML normalizes them.
/// <para>
/// The document is UTF-8, or UTF-16 when it starts with a byte order mark;
/// another encoding in the declaration is an error. The bytes that are not
/// valid UTF-8 are the character U+FFFD, one for each part of a sequence that
/// is as much of a character as it is valid for, which is what the Unicode
/// Standard recommends and the decoder of .NET does.
/// </para>
/// </summary>
internal sealed class XMLParser {
    /// <summary>How deep the elements of a document may nest.</summary>
    public const int MAX_DEPTH = 256;
    // How many bytes a document may be, 20 MB.
    internal const int MAX_SIZE = 20 << 20;
    // The message of a document of more than 20 MB.
    internal const string TOO_LARGE = "The document is more than 20 MB.";
    // The namespace of the prefix xml, which every document has without
    // declaring it.
    private const string NAMESPACE_XML = "http://www.w3.org/XML/1998/namespace";

    private readonly string xml;
    private int index;
    private int line = 1;
    private int column = 1;
    // The namespaces each open element declares, the innermost last, and null
    // where it declares none.
    private readonly List<Dictionary<string, string>> scopes = new List<Dictionary<string, string>>();

    private XMLParser(string xml) {
        this.xml = xml;
    }

    /// <summary>Reads the document and returns its root element.</summary>
    /// <param name="bytes">the document.</param>
    /// <returns>the root element.</returns>
    /// <exception cref="XMLException">The document is not the XML this reads, or is more
    ///     than 20 MB.</exception>
    public static XMLNode Parse(byte[] bytes) {
        if (bytes.Length > MAX_SIZE) {
            throw new XMLException(TOO_LARGE);
        }
        int bad;
        XMLParser parser = new XMLParser(Normalize(Decode(bytes), out bad));
        if (bad != -1) {
            parser.Skip(bad);
            throw parser.Error("The character U+" + ((int) parser.xml[bad]).ToString("X4",
                    CultureInfo.InvariantCulture) + " is not a character of XML");
        }
        return parser.Document();
    }

    /// <summary>
    /// Reads the document of the stream and returns its root element. The
    /// stream is read to its end, or to the 20 MB a document may be, and is
    /// not closed.
    /// </summary>
    /// <param name="stream">the stream.</param>
    /// <returns>the root element.</returns>
    /// <exception cref="System.IO.IOException">The stream cannot be read.</exception>
    /// <exception cref="XMLException">The document is not the XML this reads, or is more
    ///     than 20 MB.</exception>
    public static XMLNode Parse(Stream stream) {
        MemoryStream bytes = new MemoryStream();
        byte[] buffer = new byte[8192];
        int count;
        while (bytes.Length <= MAX_SIZE && (count = stream.Read(buffer, 0, buffer.Length)) > 0) {
            bytes.Write(buffer, 0, count);
        }
        if (bytes.Length > MAX_SIZE) {
            throw new XMLException(TOO_LARGE);
        }
        return Parse(bytes.ToArray());
    }

    // The characters of the document: UTF-8, or the UTF-16 of a byte order
    // mark, without the mark. A half of a character that has no other half,
    // and a last byte with no pair, is the replacement character, and what
    // follows is read as it stands, which is what the decoder of .NET does
    // and what the four ports agree on. The Java decodes UTF-16 itself, as
    // its decoder swallows the code unit after a lone half.
    private static string Decode(byte[] bytes) {
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF) {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE) {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        return Encoding.UTF8.GetString(bytes);
    }

    // Makes each line break of the document a line feed, as XML reads a
    // carriage return and the line feed after it, and a carriage return on
    // its own, and returns the characters and, in bad, where the first of them
    // is that is not a character of XML, or -1: the control characters other
    // than the tab and the line breaks, and U+FFFE and U+FFFF. A half of a
    // character outside the basic plane is always one of a pair here, as the
    // decoder makes the one without the other U+FFFD.
    private static string Normalize(string xml, out int bad) {
        bad = -1;
        StringBuilder buf = null;
        for (int i = 0; i < xml.Length; i++) {
            char ch = xml[i];
            if (ch == '\r') {
                if (buf == null) {
                    buf = new StringBuilder(xml.Length);
                    buf.Append(xml, 0, i);
                }
                buf.Append('\n');
                if (i + 1 < xml.Length && xml[i + 1] == '\n') {
                    i++;
                }
                continue;
            }
            if (bad == -1 && !IsXMLCharacter(ch)) {
                bad = (buf == null) ? i : buf.Length;
            }
            if (buf != null) {
                buf.Append(ch);
            }
        }
        return (buf == null) ? xml : buf.ToString();
    }

    // Whether the code unit is of a character XML has: all but the control
    // characters other than the tab and the line breaks, and U+FFFE and U+FFFF.
    private static bool IsXMLCharacter(char ch) {
        return ch == '\t' || ch == '\n' || ch == '\r'
                || (ch >= 0x20 && ch != '\uFFFE' && ch != '\uFFFF');
    }

    // --- The document -------------------------------------------------------

    private XMLNode Document() {
        Prolog();
        XMLNode root = Element();
        while (index < xml.Length) {
            if (IsWhitespace(Peek())) {
                Next();
            } else if (Ahead("<!--")) {
                Comment();
            } else if (Ahead("<?")) {
                ProcessingInstruction();
            } else {
                throw Error("There is more than the one element of the document");
            }
        }
        return root;
    }

    // The declaration, the comments, the processing instructions and the
    // DOCTYPE before the root element.
    private void Prolog() {
        while (index < xml.Length) {
            if (IsWhitespace(Peek())) {
                Next();
            } else if (Ahead("<?xml") && index == 0) {
                Declaration();
            } else if (Ahead("<!--")) {
                Comment();
            } else if (Ahead("<?")) {
                ProcessingInstruction();
            } else if (Ahead("<!DOCTYPE")) {
                Doctype();
            } else if (Peek() == '<') {
                return;
            } else {
                throw Error("The document starts with text");
            }
        }
        throw Error("The document has no element");
    }

    // <!DOCTYPE svg PUBLIC "..." "..." [ ... ]>: skipped to its end, its
    // internal subset too, past the > of its declarations, of its comments and
    // of its quoted strings. Its entities are not read.
    private void Doctype() {
        Skip("<!DOCTYPE".Length);
        int depth = 0;
        while (index < xml.Length) {
            char ch = Peek();
            if (Ahead("<!--")) {
                int end = xml.IndexOf("-->", index + 4, StringComparison.Ordinal);
                if (end == -1) {
                    throw Error("A comment of the document type declaration does not end");
                }
                Skip(end + 3 - index);
                continue;
            } else if (ch == '"' || ch == '\'') {
                char quote = Next();
                while (index < xml.Length && Peek() != quote) {
                    Next();
                }
                if (index >= xml.Length) {
                    throw Error("A string of the document type declaration does not end");
                }
            } else if (ch == '[') {
                depth++;
            } else if (ch == ']') {
                depth--;
            } else if (ch == '>' && depth <= 0) {
                Next();
                return;
            }
            Next();
        }
        throw Error("The document type declaration does not end");
    }

    // <?xml version="1.0" encoding="UTF-8"?>: the encoding is UTF-8 or the
    // UTF-16 of the byte order mark.
    private void Declaration() {
        int end = xml.IndexOf("?>", index, StringComparison.Ordinal);
        if (end == -1) {
            throw Error("The declaration does not end");
        }
        string declaration = xml.Substring(index, end - index);
        int encoding = declaration.IndexOf("encoding", StringComparison.Ordinal);
        if (encoding != -1) {
            int quote = -1;
            for (int i = encoding + "encoding".Length; i < declaration.Length; i++) {
                char ch = declaration[i];
                if (ch == '"' || ch == '\'') {
                    quote = i;
                    break;
                }
            }
            int close = (quote == -1) ? -1 : declaration.IndexOf(declaration[quote], quote + 1);
            string name = (close == -1) ? "" : declaration.Substring(quote + 1, close - quote - 1);
            if (!string.Equals(name, "UTF-8", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(name, "UTF-16", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(name, "UTF-16BE", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(name, "UTF-16LE", StringComparison.OrdinalIgnoreCase)) {
                throw Error("The encoding " + name + " is not read, only UTF-8 and UTF-16");
            }
        }
        Skip(end + 2 - index);
    }

    private void Comment() {
        int end = xml.IndexOf("-->", index, StringComparison.Ordinal);
        if (end == -1) {
            throw Error("The comment does not end");
        }
        Skip(end + 3 - index);
    }

    private void ProcessingInstruction() {
        int end = xml.IndexOf("?>", index, StringComparison.Ordinal);
        if (end == -1) {
            throw Error("The processing instruction does not end");
        }
        Skip(end + 2 - index);
    }

    // --- The elements -------------------------------------------------------

    // The root element and the elements in it, which are read one after
    // another with a stack and not by recursion, so that a document of any
    // depth is read in the stack of this call alone.
    private XMLNode Element() {
        XMLNode root = null;
        List<XMLNode> open = new List<XMLNode>();
        while (true) {
            if (index >= xml.Length) {
                throw Error("The element " + open[open.Count - 1].GetName() + " does not end");
            }
            if (Ahead("<!--")) {
                Comment();
            } else if (Ahead("<![CDATA[")) {
                CharacterData(open);
            } else if (Ahead("<?")) {
                ProcessingInstruction();
            } else if (Ahead("</")) {
                CloseTag(open);
                if (open.Count == 0) {
                    return root;
                }
            } else if (Peek() == '<') {
                if (open.Count >= MAX_DEPTH) {
                    throw Error("The elements nest more than "
                            + MAX_DEPTH.ToString(CultureInfo.InvariantCulture) + " deep");
                }
                if (root != null && open.Count == 0) {
                    throw Error("There is more than the one element of the document");
                }
                XMLNode node = OpenTag(open);
                if (root == null) {
                    root = node;
                }
                if (open.Count == 0) {
                    return root;     // An empty root element, <svg/>
                }
            } else {
                Text(open);
            }
        }
    }

    // <name attribute="value"> or <name/>: the element, added to the one it is
    // in, and open until its closing tag unless it closes itself.
    private XMLNode OpenTag(List<XMLNode> open) {
        Next();     // The <
        string name = Name();
        if (name.Length == 0) {
            throw Error("A tag has no name");
        }
        Dictionary<string, string> declared = null;
        List<string[]> attributes = new List<string[]>();
        HashSet<string> written = null;
        for (bool first = true; ; first = false) {
            bool spaced = index < xml.Length && IsWhitespace(Peek());
            SkipWhitespace();
            char ch = Peek();
            if (ch == '>' || ch == '/') {
                break;
            }
            // An attribute is after a space: XML has no name="1"other="2".
            if (!spaced) {
                if (first) {
                    throw Error("The name of the tag " + name + " is not followed by a space");
                }
                throw Error("The attributes of " + name + " are not separated by a space");
            }
            string attributeName = Name();
            if (attributeName.Length == 0) {
                throw Error("The attributes of " + name + " are not a name and a value");
            }
            SkipWhitespace();
            if (Peek() != '=') {
                throw Error("The attribute " + attributeName + " of " + name + " has no value");
            }
            Next();
            SkipWhitespace();
            string value = AttributeValue();
            if (written == null) {
                written = new HashSet<string>(StringComparer.Ordinal);
            }
            if (!written.Add(attributeName)) {
                throw Error("The attribute " + attributeName + " of " + name + " is written twice");
            }
            if (attributeName == "xmlns" || attributeName.StartsWith("xmlns:", StringComparison.Ordinal)) {
                if (declared == null) {
                    declared = new Dictionary<string, string>(StringComparer.Ordinal);
                }
                string prefix = "";
                if (attributeName != "xmlns") {
                    prefix = attributeName.Substring(6);
                    // A prefix stands for a namespace, and the default
                    // namespace is the only one that may be none.
                    if (value.Length == 0) {
                        throw Error("The prefix " + prefix + " of " + name
                                + " is declared as no namespace");
                    }
                }
                declared[prefix] = value;
            }
            attributes.Add(new string[] {attributeName, value});
        }
        // The namespace of the element is the one its prefix stands for in the
        // element itself or in the nearest element it is in that declares it.
        string ns = NamespaceOf(PrefixOf(name), declared);
        if (ns == null) {
            throw Error("The prefix " + PrefixOf(name) + " of " + name + " is not declared");
        }
        XMLNode node = new XMLNode(name, ns);
        foreach (string[] attribute in attributes) {
            string prefix = PrefixOf(attribute[0]);
            if (prefix.Length != 0 && prefix != "xmlns" && NamespaceOf(prefix, declared) == null) {
                throw Error("The prefix " + prefix + " of the attribute " + attribute[0]
                        + " of " + name + " is not declared");
            }
            node.AddAttribute(attribute[0], attribute[1]);
        }
        if (open.Count != 0) {
            open[open.Count - 1].AddChild(node);
        }
        bool empty = Peek() == '/';
        if (empty) {
            Next();
        }
        if (Peek() != '>') {
            throw Error("The tag " + name + " does not end with >");
        }
        Next();
        if (!empty) {
            open.Add(node);
            scopes.Add(declared);
        }
        return node;
    }

    // The prefix of the name, the part before its colon, or an empty string
    // when it has none.
    private static string PrefixOf(string name) {
        int colon = name.IndexOf(':');
        return (colon == -1) ? "" : name.Substring(0, colon);
    }

    // The namespace the prefix stands for, in the namespaces the element
    // declares and then in the ones of the elements it is in, from the
    // innermost out, or null when it stands for none. No prefix is the default
    // namespace, which is none where no element declares one, and xml is the
    // namespace of XML itself.
    private string NamespaceOf(string prefix, Dictionary<string, string> declared) {
        string ns;
        if (declared != null && declared.TryGetValue(prefix, out ns)) {
            return ns;
        }
        for (int i = scopes.Count - 1; i >= 0; i--) {
            if (scopes[i] != null && scopes[i].TryGetValue(prefix, out ns)) {
                return ns;
            }
        }
        if (prefix.Length == 0) {
            return "";
        }
        return (prefix == "xml") ? NAMESPACE_XML : null;
    }

    // </name>, which ends the element that is open.
    private void CloseTag(List<XMLNode> open) {
        Skip(2);    // The </
        string name = Name();
        SkipWhitespace();
        if (Peek() != '>') {
            throw Error("The closing tag of " + name + " does not end with >");
        }
        Next();
        if (open.Count == 0) {
            throw Error("The closing tag of " + name + " closes no element");
        }
        XMLNode node = open[open.Count - 1];
        open.RemoveAt(open.Count - 1);
        scopes.RemoveAt(scopes.Count - 1);
        if (node.GetName() != name) {
            throw Error("The element " + node.GetName() + " is closed by the tag of " + name);
        }
    }

    // <![CDATA[ the text as it is ]]>
    private void CharacterData(List<XMLNode> open) {
        int end = xml.IndexOf("]]>", index, StringComparison.Ordinal);
        if (end == -1) {
            throw Error("The character data does not end");
        }
        if (open.Count == 0) {
            throw Error("There is text outside the element of the document");
        }
        int start = index + "<![CDATA[".Length;
        open[open.Count - 1].AddText(xml.Substring(start, end - start));
        Skip(end + 3 - index);
    }

    // The text up to the next <, with its entities read.
    private void Text(List<XMLNode> open) {
        StringBuilder buf = new StringBuilder();
        while (index < xml.Length && Peek() != '<') {
            char ch = Next();
            if (ch == '&') {
                buf.Append(Entity());
            } else {
                buf.Append(ch);
            }
        }
        if (open.Count == 0) {
            if (Trim(buf.ToString()).Length == 0) {
                return;
            }
            throw Error("There is text outside the element of the document");
        }
        open[open.Count - 1].AddText(buf.ToString());
    }

    // The &lt;, &gt;, &amp;, &quot; and &apos; of XML, and the numeric ones,
    // such as &#160; and &#xA0;. The & is read.
    private string Entity() {
        int end = xml.IndexOf(';', index);
        if (end == -1 || end - index > 16) {
            throw Error("An & that is not an entity, such as &amp;");
        }
        string name = xml.Substring(index, end - index);
        Skip(end + 1 - index);
        if (name == "lt") {
            return "<";
        } else if (name == "gt") {
            return ">";
        } else if (name == "amp") {
            return "&";
        } else if (name == "quot") {
            return "\"";
        } else if (name == "apos") {
            return "'";
        } else if (name.StartsWith("#", StringComparison.Ordinal)) {
            bool hex = name.Length > 1 && (name[1] == 'x' || name[1] == 'X');
            string digits = name.Substring(hex ? 2 : 1);
            // The digits of XML and no others: int.Parse takes a sign, so it
            // reads &#+65;, which is not a number of XML. The number is read as
            // a long, since a hexadecimal one of eight digits is more than an
            // int holds, and such a number is no number of XML either.
            long code = -1;
            long number;
            if (IsNumber(digits, hex) && long.TryParse(digits,
                    hex ? NumberStyles.AllowHexSpecifier : NumberStyles.None,
                    CultureInfo.InvariantCulture, out number)
                    && number >= 0 && number <= int.MaxValue) {
                code = number;
            }
            if (code == -1) {
                throw Error("The character &" + name + "; is not a number");
            }
            if (code > 0x10FFFF || (code >= 0xD800 && code <= 0xDFFF)
                    || (code <= 0xFFFF && !IsXMLCharacter((char) code))) {
                throw Error("The character &" + name + "; is not a character of XML");
            }
            return char.ConvertFromUtf32((int) code);
        }
        throw Error("The entity &" + name + "; is not one of XML, and this reads no others");
    }

    // --- The characters -----------------------------------------------------

    // A name of an element or of an attribute: what stands before the
    // whitespace, the =, the / or the > after it.
    private string Name() {
        int start = index;
        while (index < xml.Length) {
            char ch = Peek();
            if (IsWhitespace(ch) || ch == '=' || ch == '/' || ch == '>' || ch == '<') {
                break;
            }
            Next();
        }
        return xml.Substring(start, index - start);
    }

    // "value" or 'value', with its entities read.
    private string AttributeValue() {
        char quote = Peek();
        if (quote != '"' && quote != '\'') {
            throw Error("The value of an attribute is not in quotes");
        }
        Next();
        StringBuilder buf = new StringBuilder();
        while (true) {
            if (index >= xml.Length) {
                throw Error("The value of an attribute does not end");
            }
            char ch = Next();
            if (ch == quote) {
                return buf.ToString();
            } else if (ch == '<') {
                throw Error("The value of an attribute holds a <");
            } else if (ch == '&') {
                buf.Append(Entity());
            } else if (ch == '\t' || ch == '\n') {
                // XML makes each tab and line break of a value a space, and
                // keeps the ones written as entities, such as &#10;.
                buf.Append(' ');
            } else {
                buf.Append(ch);
            }
        }
    }

    // Whether the characters at the index are the text, compared ordinally.
    private bool Ahead(string text) {
        return index + text.Length <= xml.Length
                && string.CompareOrdinal(xml, index, text, 0, text.Length) == 0;
    }

    private char Peek() {
        return (index < xml.Length) ? xml[index] : '\0';
    }

    private char Next() {
        char ch = xml[index++];
        if (ch == '\n') {
            line++;
            column = 1;
        } else {
            column++;
        }
        return ch;
    }

    private void Skip(int count) {
        for (int i = 0; i < count && index < xml.Length; i++) {
            Next();
        }
    }

    private void SkipWhitespace() {
        while (index < xml.Length && IsWhitespace(Peek())) {
            Next();
        }
    }

    // A number of a numeric entity: the digits 0 to 9, and a to f as well when
    // the entity is hexadecimal, in ASCII and in no other script.
    private static bool IsNumber(string digits, bool hex) {
        if (digits.Length == 0) {
            return false;
        }
        for (int i = 0; i < digits.Length; i++) {
            char ch = digits[i];
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

    private static bool IsWhitespace(char ch) {
        return ch == ' ' || ch == '\t' || ch == '\n' || ch == '\r';
    }

    // The trim of the Java, which removes the characters up to and including
    // the space from both ends, and not the Trim of C#, which removes the
    // characters that Unicode calls whitespace.
    internal static string Trim(string text) {
        int start = 0;
        int end = text.Length;
        while (start < end && text[start] <= ' ') {
            start++;
        }
        while (start < end && text[end - 1] <= ' ') {
            end--;
        }
        return text.Substring(start, end - start);
    }

    private XMLException Error(string message) {
        return new XMLException(message
                + ", at line " + line.ToString(CultureInfo.InvariantCulture)
                + " column " + column.ToString(CultureInfo.InvariantCulture));
    }
}
}   // End of namespace PDFjet.NET
