/*
 * XMLNode.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import java.util.ArrayList;
import java.util.LinkedHashMap;
import java.util.List;
import java.util.Map;

/**
 * An element of an XML document that XMLParser read: its name, its
 * attributes, its text and the elements in it.
 * <p>
 * The elements are found by a path of the names without their prefix, such as
 * "SupplyChainTradeTransaction/ApplicableHeaderTradeAgreement/BuyerReference",
 * because the prefixes of an invoice are its own: what one writes as ram:ID
 * another writes as a:ID. A * matches an element of any name.
 */
public final class XMLNode {
    private final String name;
    private final String localName;
    private final String namespace;
    // The names and the values of the attributes, one after the other, null
    // when there are none: written once each, as the parser refuses a name
    // written twice; with no map of them, and the children and the text made
    // when there are some, as a map and empty lists made an element of an SVG
    // of millions several times larger (the review of 9 October 2026).
    private String[] attributes;
    private List<XMLNode> children;
    private StringBuilder text;

    XMLNode(String name, String namespace) {
        this(name, localNameOf(name), namespace);
    }

    // The element of the name and of its name without its prefix, which the
    // parser keeps once for every element of that name.
    XMLNode(String name, String localName, String namespace) {
        this.name = name;
        this.localName = localName;
        this.namespace = namespace;
    }

    // The name without its prefix, the part after its colon.
    static String localNameOf(String name) {
        int colon = name.indexOf(':');
        return (colon == -1) ? name : name.substring(colon + 1);
    }

    // The names and the values of the attributes, one after the other.
    void setAttributes(String[] namesAndValues) {
        attributes = (namesAndValues.length == 0) ? null : namesAndValues;
    }

    void addChild(XMLNode child) {
        if (children == null) {
            children = new ArrayList<XMLNode>(4);
        }
        children.add(child);
    }

    void addText(String characters) {
        if (text == null) {
            text = new StringBuilder(characters.length());
        }
        text.append(characters);
    }

    /**
     * Returns the name of the element as the document writes it, with its
     * prefix when it has one, such as ram:ID.
     *
     * @return the name.
     */
    public String getName() {
        return name;
    }

    /**
     * Returns the name of the element without its prefix, such as ID.
     *
     * @return the name without its prefix.
     */
    public String getLocalName() {
        return localName;
    }

    /**
     * Returns the namespace of the element, the URI its prefix stands for, or
     * an empty string when it is in no namespace.
     *
     * @return the namespace.
     */
    public String getNamespace() {
        return namespace;
    }

    /**
     * Returns the text of the element, the characters in it that are not in
     * the elements it holds, as they are written.
     *
     * @return the text.
     */
    public String getText() {
        return (text == null) ? "" : text.toString();
    }

    /**
     * Returns the elements in this one, in their order.
     *
     * @return the elements.
     */
    public List<XMLNode> getChildren() {
        return (children == null) ? new ArrayList<XMLNode>() : new ArrayList<XMLNode>(children);
    }

    /**
     * Returns the value of the attribute of that name, by its name with its
     * prefix, such as unitCode or ram:unitCode, or null when it has none.
     *
     * @param attributeName the name of the attribute.
     * @return the value, or null.
     */
    public String getAttribute(String attributeName) {
        if (attributes == null) {
            return null;
        }
        for (int i = 0; i < attributes.length; i += 2) {
            if (attributes[i].equals(attributeName)) {
                return attributes[i + 1];
            }
        }
        // By its name without its prefix; a namespace declaration, xmlns:name,
        // is not an attribute of that name
        for (int i = 0; i < attributes.length; i += 2) {
            String key = attributes[i];
            int colon = key.indexOf(':');
            if (colon != -1 && !key.startsWith("xmlns:")
                    && key.length() - colon - 1 == attributeName.length()
                    && key.startsWith(attributeName, colon + 1)) {
                return attributes[i + 1];
            }
        }
        return null;
    }

    /**
     * Returns the attributes of the element, by their names as the document
     * writes them.
     *
     * @return the attributes.
     */
    public Map<String, String> getAttributes() {
        Map<String, String> map = new LinkedHashMap<String, String>();
        if (attributes != null) {
            for (int i = 0; i < attributes.length; i += 2) {
                map.put(attributes[i], attributes[i + 1]);
            }
        }
        return map;
    }

    /**
     * Returns the elements at the path under this one, none when there are
     * none: the names of the path are the names without their prefix, and a *
     * matches an element of any name.
     *
     * @param path the path, such as "IncludedSupplyChainTradeLineItem/SpecifiedTradeProduct/Name".
     * @return the elements.
     */
    public List<XMLNode> findAll(String path) {
        List<XMLNode> found = new ArrayList<XMLNode>();
        found.add(this);
        for (String step : path.split("/")) {
            if (step.isEmpty()) {
                continue;
            }
            List<XMLNode> next = new ArrayList<XMLNode>();
            for (XMLNode node : found) {
                if (node.children == null) {
                    continue;
                }
                for (XMLNode child : node.children) {
                    if (step.equals("*") || step.equals(child.localName)) {
                        next.add(child);
                    }
                }
            }
            found = next;
        }
        return found;
    }

    /**
     * Returns the first element at the path under this one, or null.
     *
     * @param path the path.
     * @return the element, or null.
     */
    public XMLNode find(String path) {
        List<XMLNode> found = findAll(path);
        return found.isEmpty() ? null : found.get(0);
    }

    /**
     * Returns the text of the first element at the path under this one,
     * without the spaces and line breaks at its ends, or null when the path
     * has no element.
     *
     * @param path the path.
     * @return the text, or null.
     */
    public String getValue(String path) {
        XMLNode node = find(path);
        return (node == null) ? null : node.getText().trim();
    }
}   // End of XMLNode.java
