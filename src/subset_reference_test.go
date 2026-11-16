// subset_reference_test.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
	"os"
	"path/filepath"
	"strconv"
	"strings"
	"testing"
)

// TestTrueTypeSubsetReference writes the TrueType subsets of the cases that
// TTF_SUBSET_CASES names, for check-ttf.sh of PDFjet Forms, which compares
// its TypeScript engine's subsets with these, byte for byte. A case is a line
// "path|glyph numbers|what it is", the path of a .ttf and the numbers of the
// glyphs used, separated by spaces, or "all"; lines starting with # are
// comments. For each case, in TTF_SUBSET_OUT: case-N.subset, the font;
// case-N.kept, a byte for each glyph number up to the last kept, 1 if kept;
// case-N.map, the CIDToGIDMap; or case-N.err when the font cannot be subset.
// Skipped unless TTF_SUBSET_CASES is set.
func TestTrueTypeSubsetReference(t *testing.T) {
	cases := os.Getenv("TTF_SUBSET_CASES")
	if cases == "" {
		t.Skip("TTF_SUBSET_CASES is not set")
	}
	out := os.Getenv("TTF_SUBSET_OUT")
	data, err := os.ReadFile(cases)
	if err != nil {
		t.Fatal(err)
	}
	n := 0
	for _, line := range strings.Split(string(data), "\n") {
		line = strings.TrimSpace(line)
		if line == "" || strings.HasPrefix(line, "#") {
			continue
		}
		fields := strings.SplitN(line, "|", 3)
		ttf, err := os.ReadFile(fields[0])
		if err != nil {
			t.Fatal(err)
		}
		used := make([]bool, 0x10000)
		if strings.TrimSpace(fields[1]) == "all" {
			for gid := range used {
				used[gid] = true
			}
		} else {
			for _, g := range strings.Fields(fields[1]) {
				gid, err := strconv.Atoi(g)
				if err != nil || gid < 0 || gid >= len(used) {
					t.Fatalf("case %d: the glyph %q", n, g)
				}
				used[gid] = true
			}
		}
		base := filepath.Join(out, fmt.Sprintf("case-%d", n))
		n++
		subset, kept, err := subsetTrueType(ttf, used)
		if err != nil {
			if err := os.WriteFile(base+".err", []byte(err.Error()), 0o644); err != nil {
				t.Fatal(err)
			}
			continue
		}
		keptBytes := make([]byte, len(kept))
		for gid, keep := range kept {
			if keep {
				keptBytes[gid] = 1
			}
		}
		for ext, b := range map[string][]byte{".subset": subset, ".kept": keptBytes, ".map": cidToGIDMap(kept)} {
			if err := os.WriteFile(base+ext, b, 0o644); err != nil {
				t.Fatal(err)
			}
		}
	}
}
