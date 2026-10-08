/*
 * XMLNode.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// An element of an XML document that XMLParser read: its name, its
/// attributes, its text and the elements in it.
/// <para>
/// The elements are found by a path of the names without their prefix, such as
/// "SupplyChainTradeTransaction/ApplicableHeaderTradeAgreement/BuyerReference",
/// because the prefixes of an invoice are its own: what one writes as ram:ID
/// another writes as a:ID. A * matches an element of any name.
/// </para>
/// </summary>
internal sealed class XMLNode {
    private readonly string name;
    private readonly string localName;
    private readonly string ns;
    // The attributes in the order they are written: a Dictionary keeps the
    // order of the keys that are added to it and never removed, as the
    // LinkedHashMap of the Java does.
    private readonly Dictionary<string, string> attributes =
            new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly List<XMLNode> children = new List<XMLNode>();
    private readonly StringBuilder text = new StringBuilder();

    internal XMLNode(string name, string ns) {
        this.name = name;
        int colon = name.IndexOf(':');
        this.localName = (colon == -1) ? name : name.Substring(colon + 1);
        this.ns = ns;
    }

    internal void AddAttribute(string attributeName, string value) {
        attributes[attributeName] = value;
    }

    internal void AddChild(XMLNode child) {
        children.Add(child);
    }

    internal void AddText(string characters) {
        text.Append(characters);
    }

    /// <summary>
    /// Returns the name of the element as the document writes it, with its
    /// prefix when it has one, such as svg:path.
    /// </summary>
    /// <returns>the name.</returns>
    public string GetName() {
        return name;
    }

    /// <summary>Returns the name of the element without its prefix, such as ID.</summary>
    /// <returns>the name without its prefix.</returns>
    public string GetLocalName() {
        return localName;
    }

    /// <summary>
    /// Returns the namespace of the element, the URI its prefix stands for, or
    /// an empty string when it is in no namespace.
    /// </summary>
    /// <returns>the namespace.</returns>
    public string GetNamespace() {
        return ns;
    }

    /// <summary>
    /// Returns the text of the element, the characters in it that are not in
    /// the elements it holds, as they are written.
    /// </summary>
    /// <returns>the text.</returns>
    public string GetText() {
        return text.ToString();
    }

    /// <summary>Returns the elements in this one, in their order.</summary>
    /// <returns>the elements.</returns>
    public List<XMLNode> GetChildren() {
        return new List<XMLNode>(children);
    }

    /// <summary>
    /// Returns the value of the attribute of that name, by its name with its
    /// prefix, such as unitCode or ram:unitCode, or null when it has none.
    /// </summary>
    /// <param name="attributeName">the name of the attribute.</param>
    /// <returns>the value, or null.</returns>
    public string GetAttribute(string attributeName) {
        string value;
        if (attributes.TryGetValue(attributeName, out value)) {
            return value;
        }
        foreach (KeyValuePair<string, string> attribute in attributes) {
            string key = attribute.Key;
            int colon = key.IndexOf(':');
            if (colon != -1 && key.Substring(colon + 1) == attributeName) {
                return attribute.Value;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the attributes of the element, by their names as the document
    /// writes them.
    /// </summary>
    /// <returns>the attributes.</returns>
    public Dictionary<string, string> GetAttributes() {
        return new Dictionary<string, string>(attributes, StringComparer.Ordinal);
    }

    /// <summary>
    /// Returns the elements at the path under this one, none when there are
    /// none: the names of the path are the names without their prefix, and a *
    /// matches an element of any name.
    /// </summary>
    /// <param name="path">the path, such as "IncludedSupplyChainTradeLineItem/SpecifiedTradeProduct/Name".</param>
    /// <returns>the elements.</returns>
    public List<XMLNode> FindAll(string path) {
        List<XMLNode> found = new List<XMLNode>();
        found.Add(this);
        foreach (string step in path.Split('/')) {
            if (step.Length == 0) {
                continue;
            }
            List<XMLNode> next = new List<XMLNode>();
            foreach (XMLNode node in found) {
                foreach (XMLNode child in node.children) {
                    if (step == "*" || step == child.localName) {
                        next.Add(child);
                    }
                }
            }
            found = next;
        }
        return found;
    }

    /// <summary>Returns the first element at the path under this one, or null.</summary>
    /// <param name="path">the path.</param>
    /// <returns>the element, or null.</returns>
    public XMLNode Find(string path) {
        List<XMLNode> found = FindAll(path);
        return (found.Count == 0) ? null : found[0];
    }

    /// <summary>
    /// Returns the text of the first element at the path under this one,
    /// without the spaces and line breaks at its ends, or null when the path
    /// has no element.
    /// </summary>
    /// <param name="path">the path.</param>
    /// <returns>the text, or null.</returns>
    public string GetValue(string path) {
        XMLNode node = Find(path);
        return (node == null) ? null : XMLParser.Trim(node.GetText());
    }
}
}   // End of namespace PDFjet.NET
