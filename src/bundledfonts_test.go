// bundledfonts_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"errors"
	"go/ast"
	"go/parser"
	"go/token"
	"io/fs"
	"os"
	"path/filepath"
	"slices"
	"strconv"
	"strings"
	"testing"

	"github.com/edragoev1/pdfjet/v9/resources/fonts"
	// The CJK fonts, built in for the tests that draw in them, as a program
	// builds them in
	_ "github.com/edragoev1/pdfjet/v9/resources/fonts/cjk"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSans"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansJP"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansKR"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansSC"
	"github.com/edragoev1/pdfjet/v9/src/IBMPlexSansTC"
	"github.com/edragoev1/pdfjet/v9/src/letter"
)

// The fonts PDFjet carries, in resources/fonts: each has a constant in the
// packages of the fonts, and each constant that is not deprecated names one
// of them, which the library has.

var testFontPackages = []string{
	"IBMPlexMath", "IBMPlexMono", "IBMPlexSans", "IBMPlexSansArabic", "IBMPlexSansHebrew",
	"IBMPlexSansJP", "IBMPlexSansKR", "IBMPlexSansSC", "IBMPlexSansTC", "IBMPlexSansThai",
	"IBMPlexSerif", "JetBrainsMono", "CascadiaMono", "SourceSerif4", "NotoSans",
}

// testIsDeprecated tells if the doc comment has a paragraph that starts
// with "Deprecated:", as go doc and the tools read it.
func testIsDeprecated(doc *ast.CommentGroup) bool {
	if doc == nil {
		return false
	}
	for _, paragraph := range strings.Split(doc.Text(), "\n\n") {
		if strings.HasPrefix(paragraph, "Deprecated:") {
			return true
		}
	}
	return false
}

func TestBundledFontsEveryFontCarriedHasAConstantAndEveryConstantNotDeprecatedIsCarried(t *testing.T) {
	var carried []string
	root := testRepoPath(t, "resources/fonts")
	families, err := os.ReadDir(root)
	if err != nil {
		t.Fatal(err)
	}
	for _, family := range families {
		if !family.IsDir() {
			continue
		}
		files, err := os.ReadDir(filepath.Join(root, family.Name()))
		if err != nil {
			t.Fatal(err)
		}
		for _, file := range files {
			if strings.HasSuffix(file.Name(), ".ttf") || strings.HasSuffix(file.Name(), ".otf") {
				carried = append(carried, "fonts/"+family.Name()+"/"+file.Name())
			}
		}
	}
	slices.Sort(carried)
	if len(carried) != 72 {
		t.Fatalf("%d fonts carried, not 72", len(carried))
	}
	var constants []string
	deprecated := 0
	for _, name := range testFontPackages {
		dir := filepath.Join(testRepoPath(t, "src"), name)
		sources, err := filepath.Glob(filepath.Join(dir, "*.go"))
		if err != nil || len(sources) == 0 {
			t.Fatalf("no package %s: %v", name, err)
		}
		count := 0
		for _, source := range sources {
			file, err := parser.ParseFile(token.NewFileSet(), source, nil, parser.ParseComments)
			if err != nil {
				t.Fatal(err)
			}
			deprecatedPackage := testIsDeprecated(file.Doc)
			for _, decl := range file.Decls {
				gen, ok := decl.(*ast.GenDecl)
				if !ok || gen.Tok != token.CONST {
					continue
				}
				for _, spec := range gen.Specs {
					value := spec.(*ast.ValueSpec)
					path, err := strconv.Unquote(value.Values[0].(*ast.BasicLit).Value)
					if err != nil {
						t.Fatal(err)
					}
					constant := name + "." + value.Names[0].Name
					count++
					_, isCarried := slices.BinarySearch(carried, path)
					if deprecatedPackage || testIsDeprecated(value.Doc) {
						deprecated++
						if isCarried {
							t.Errorf("%s is deprecated and carried", constant)
						}
						continue
					}
					if !isCarried {
						t.Errorf("%s is not carried", constant)
						continue
					}
					// In the library, as the module has it, not only in the folder:
					// the CJK fonts registered by the package cjk, which this
					// test imports, and the others built in always
					if buf, err := fonts.ReadFile(bundledFontName(path)); err != nil || len(buf) == 0 {
						t.Errorf("%s is not in the library: %v", constant, err)
					}
					family, _, _ := strings.Cut(bundledFontName(path), "/")
					_, err = fonts.FS.ReadFile(bundledFontName(path))
					if builtIn := err == nil; builtIn == slices.Contains(cjkFamilies, family) {
						t.Errorf("%s built in always: %v", constant, builtIn)
					}
					constants = append(constants, path)
				}
			}
		}
		if count == 0 {
			t.Errorf("no constants in %s", name)
		}
	}
	// IBM Plex Mono, JetBrains Mono, Noto Sans and Source Serif 4, and the
	// weights not carried of the other families
	if deprecated != 16+12+18+14+25 {
		t.Errorf("%d constants deprecated", deprecated)
	}
	slices.Sort(constants)
	if !slices.Equal(carried, constants) {
		t.Errorf("the fonts carried\n%v\nand the constants\n%v", carried, constants)
	}
}

func TestBundledFontsAFontAddedToAnExistingPDFIsReadFromTheLibraryByItsPath(t *testing.T) {
	// The objects of a PDF of one page, and the constant of a font PDFjet
	// carries, with no fonts folder: the font is read from the library, as
	// NewFontFromFile reads it
	doc := testNewDoc()
	NewPage(doc.pdf, letter.Portrait())
	objects := testRead(t, doc.complete())
	font := NewFontFromFileForObjects(&objects, IBMPlexSans.Bold)
	testWant(t, "IBMPlexSans-Bold", font.GetName())
	// A font it does not carry says to use a font file of one's own
	defer func() {
		err, ok := recover().(error)
		if !ok || !errors.Is(err, fs.ErrNotExist) || !strings.Contains(err.Error(), "use a font file of your own") {
			t.Errorf("%v", err)
		}
	}()
	NewFontFromFileForObjects(&objects, "fonts/NotoSans/NotoSans-Regular.ttf")
	t.Error("a font PDFjet does not carry was read")
}

func TestBundledFontsTheCJKFontsAreReadFromTheLibraryWhenTheProgramImportsThePackageCJK(t *testing.T) {
	// A working directory with no folder resources/fonts: the font is the
	// one the package cjk, which this test imports, builds in
	t.Chdir(t.TempDir())
	doc := testNewDoc()
	for _, path := range []string{IBMPlexSansJP.Regular, IBMPlexSansKR.SemiBold, IBMPlexSansSC.Bold, IBMPlexSansTC.Regular} {
		font := NewFontFromFile(doc.pdf, path)
		if family := filepath.Base(filepath.Dir(path)); !strings.HasPrefix(font.GetName(), family) {
			t.Errorf("%s: the font %s", path, font.GetName())
		}
	}
	// A weight PDFjet does not carry is not one of them
	if bundledCJKMissing(IBMPlexSansJP.Regular) || bundledCJKMissing("fonts/IBMPlexSansJP/IBMPlexSansJP-Thin.ttf") {
		t.Error("a CJK font is missing")
	}
}

func TestBundledFontsCSSWeightsMatchFacesThatLookAlikeAcrossTheFamilies(t *testing.T) {
	// Cascadia Mono SemiLight is as thick as IBM Plex Sans Regular: it stands
	// for 400, and Regular for 500
	for _, c := range []struct {
		family, weight int
		want           string
	}{
		{bundledSans, 400, "IBMPlexSans/IBMPlexSans-Regular.ttf"},
		{bundledSans, 200, "IBMPlexSans/IBMPlexSans-Thin.ttf"},
		{bundledSans, 800, "IBMPlexSans/IBMPlexSans-Bold.ttf"},
		{bundledSerif, 100, "IBMPlexSerif/IBMPlexSerif-Thin.ttf"},
		{bundledSerif, 900, "IBMPlexSerif/IBMPlexSerif-Bold.ttf"},
		{bundledMono, 400, "CascadiaMono/CascadiaMono-SemiLight.ttf"},
		{bundledMono, 500, "CascadiaMono/CascadiaMono-Regular.ttf"},
		{bundledMono, 600, "CascadiaMono/CascadiaMono-SemiBold.ttf"},
		{bundledMono, 700, "CascadiaMono/CascadiaMono-Bold.ttf"},
	} {
		testWant(t, c.want, bundledFontFile(c.family, c.weight, false))
	}
	testWant(t, "CascadiaMono/CascadiaMono-SemiLightItalic.ttf", bundledFontFile(bundledMono, 400, true))
	testWant(t, "CascadiaMono/CascadiaMono-Italic.ttf", bundledFontFile(bundledMono, 500, true))
	testWant(t, "IBMPlexSans/IBMPlexSans-Italic.ttf", bundledFontFile(bundledSans, 400, true))
}
