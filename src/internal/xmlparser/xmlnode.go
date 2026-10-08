// xmlnode.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package xmlparser

import "strings"

// XMLAttribute is an attribute of an element: its name as the document writes
// it, with its prefix when it has one, such as xlink:href, and its value.
type XMLAttribute struct {
	Name  string
	Value string
}

// XMLNode is an element of an XML document that Parse read: its name, its
// attributes, its text and the elements in it.
//
// The elements are found by a path of the names without their prefix, such as
// "SupplyChainTradeTransaction/ApplicableHeaderTradeAgreement/BuyerReference",
// because the prefixes of an invoice are its own: what one writes as ram:ID
// another writes as a:ID. A * matches an element of any name.
type XMLNode struct {
	name       string
	localName  string
	namespace  string
	attributes []XMLAttribute
	// Where each attribute name is in attributes, which the parser writes
	// once each, as it refuses a name written twice.
	attributeAt map[string]int
	children    []*XMLNode
	text        strings.Builder
}

func newXMLNode(name, namespace string) *XMLNode {
	localName := name
	if colon := strings.IndexByte(name, ':'); colon != -1 {
		localName = name[colon+1:]
	}
	return &XMLNode{name: name, localName: localName, namespace: namespace}
}

func (node *XMLNode) addAttribute(attributeName, value string) {
	if at, ok := node.attributeAt[attributeName]; ok {
		node.attributes[at].Value = value
		return
	}
	if node.attributeAt == nil {
		node.attributeAt = make(map[string]int)
	}
	node.attributeAt[attributeName] = len(node.attributes)
	node.attributes = append(node.attributes, XMLAttribute{Name: attributeName, Value: value})
}

func (node *XMLNode) addChild(child *XMLNode) {
	node.children = append(node.children, child)
}

func (node *XMLNode) addText(characters string) {
	node.text.WriteString(characters)
}

// Name returns the name of the element as the document writes it, with its
// prefix when it has one, such as ram:ID.
func (node *XMLNode) Name() string {
	return node.name
}

// LocalName returns the name of the element without its prefix, such as ID.
func (node *XMLNode) LocalName() string {
	return node.localName
}

// Namespace returns the namespace of the element, the URI its prefix stands
// for, or an empty string when it is in no namespace.
func (node *XMLNode) Namespace() string {
	return node.namespace
}

// Text returns the text of the element, the characters in it that are not in
// the elements it holds, as they are written.
func (node *XMLNode) Text() string {
	return node.text.String()
}

// Children returns the elements in this one, in their order.
func (node *XMLNode) Children() []*XMLNode {
	return append([]*XMLNode(nil), node.children...)
}

// Attribute returns the value of the attribute of that name, by its name with
// its prefix, such as unitCode or ram:unitCode, and whether the element has
// such an attribute.
func (node *XMLNode) Attribute(attributeName string) (string, bool) {
	if at, ok := node.attributeAt[attributeName]; ok {
		return node.attributes[at].Value, true
	}
	for _, attribute := range node.attributes {
		colon := strings.IndexByte(attribute.Name, ':')
		if colon != -1 && attribute.Name[colon+1:] == attributeName {
			return attribute.Value, true
		}
	}
	return "", false
}

// Attributes returns the attributes of the element, by their names as the
// document writes them, in the order it writes them.
func (node *XMLNode) Attributes() []XMLAttribute {
	return append([]XMLAttribute(nil), node.attributes...)
}

// FindAll returns the elements at the path under this one, none when there are
// none: the names of the path are the names without their prefix, and a *
// matches an element of any name. The path is such as
// "IncludedSupplyChainTradeLineItem/SpecifiedTradeProduct/Name".
func (node *XMLNode) FindAll(path string) []*XMLNode {
	found := []*XMLNode{node}
	for _, step := range strings.Split(path, "/") {
		if step == "" {
			continue
		}
		next := make([]*XMLNode, 0)
		for _, parent := range found {
			for _, child := range parent.children {
				if step == "*" || step == child.localName {
					next = append(next, child)
				}
			}
		}
		found = next
	}
	return found
}

// Find returns the first element at the path under this one, or nil.
func (node *XMLNode) Find(path string) *XMLNode {
	found := node.FindAll(path)
	if len(found) == 0 {
		return nil
	}
	return found[0]
}

// Value returns the text of the first element at the path under this one,
// without the spaces and line breaks at its ends, and whether the path has an
// element.
func (node *XMLNode) Value(path string) (string, bool) {
	found := node.Find(path)
	if found == nil {
		return "", false
	}
	return trim(found.Text()), true
}

// trim returns the text without the characters up to and including the space
// at its ends, as Java's String.trim does: the space and the control
// characters of a text, and not the other whitespace of Unicode.
func trim(text string) string {
	return strings.TrimFunc(text, func(ch rune) bool { return ch <= ' ' })
}
