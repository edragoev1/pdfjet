// main.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Goapi prints the exported declarations of the Go package in the directory it
// is given, one to a line and without their comments, for check-api.sh to
// hold against those of a tag: the functions and methods with their
// signatures, the types, with the exported fields of a struct and the methods
// of an interface, and the constants and variables, with their types.
//
//	go run ./util/goapi src/compliance
package main

import (
	"bytes"
	"fmt"
	"go/ast"
	"go/parser"
	"go/printer"
	"go/token"
	"os"
	"regexp"
	"sort"
	"strings"
)

var spaces = regexp.MustCompile(`\s+`)

// text returns the source of the node on one line.
func text(fset *token.FileSet, node any) string {
	var b bytes.Buffer
	printer.Fprint(&b, fset, node)
	return strings.TrimSpace(spaces.ReplaceAllString(b.String(), " "))
}

func main() {
	if len(os.Args) != 2 {
		fmt.Fprintln(os.Stderr, "usage: goapi <package directory>")
		os.Exit(2)
	}
	fset := token.NewFileSet()
	pkgs, err := parser.ParseDir(fset, os.Args[1], func(info os.FileInfo) bool {
		return !strings.HasSuffix(info.Name(), "_test.go")
	}, 0)
	if err != nil {
		fmt.Fprintln(os.Stderr, err)
		os.Exit(1)
	}
	var lines []string
	for name, pkg := range pkgs {
		if name == "main" {
			continue
		}
		for _, file := range pkg.Files {
			for _, decl := range file.Decls {
				switch d := decl.(type) {
				case *ast.FuncDecl:
					lines = append(lines, function(fset, name, d)...)
				case *ast.GenDecl:
					lines = append(lines, general(fset, name, d)...)
				}
			}
		}
	}
	sort.Strings(lines)
	for _, line := range lines {
		fmt.Println(line)
	}
}

// function returns the line of an exported function, or of an exported
// method of an exported type.
func function(fset *token.FileSet, pkg string, d *ast.FuncDecl) []string {
	if !d.Name.IsExported() {
		return nil
	}
	if d.Recv != nil {
		receiver := strings.TrimLeft(text(fset, d.Recv.List[0].Type), "*")
		if i := strings.Index(receiver, "["); i >= 0 {
			receiver = receiver[:i]
		}
		if !ast.IsExported(receiver) {
			return nil
		}
	}
	d.Body = nil
	d.Doc = nil
	return []string{pkg + " " + text(fset, d)}
}

// general returns the lines of the exported types, constants and variables
// of a declaration.
func general(fset *token.FileSet, pkg string, d *ast.GenDecl) []string {
	var lines []string
	for _, spec := range d.Specs {
		switch s := spec.(type) {
		case *ast.TypeSpec:
			if !s.Name.IsExported() {
				continue
			}
			switch t := s.Type.(type) {
			case *ast.StructType:
				lines = append(lines, pkg+" type "+s.Name.Name+" struct")
				for _, field := range t.Fields.List {
					for _, n := range field.Names {
						if n.IsExported() {
							lines = append(lines, pkg+" "+s.Name.Name+"."+n.Name+" "+text(fset, field.Type))
						}
					}
				}
			case *ast.InterfaceType:
				lines = append(lines, pkg+" type "+s.Name.Name+" interface")
				for _, method := range t.Methods.List {
					for _, n := range method.Names {
						lines = append(lines, pkg+" "+s.Name.Name+"."+n.Name+" "+text(fset, method.Type))
					}
				}
			default:
				lines = append(lines, pkg+" type "+s.Name.Name+" "+text(fset, s.Type))
			}
		case *ast.ValueSpec:
			kind := strings.ToLower(d.Tok.String())
			for _, n := range s.Names {
				if !n.IsExported() {
					continue
				}
				line := pkg + " " + kind + " " + n.Name
				if s.Type != nil {
					line += " " + text(fset, s.Type)
				}
				lines = append(lines, line)
			}
		}
	}
	return lines
}
