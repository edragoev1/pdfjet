// textutils.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pdfjet

import (
	"fmt"
)

// PrintDuration prints how long an example took: the difference between the
// two times, in milliseconds.
func PrintDuration(example string, time0, time1 int64) {
	duration := fmt.Sprintf("%d", time1-time0)
	if len(duration) == 1 {
		duration = "    " + duration
	} else if len(duration) == 2 {
		duration = "   " + duration
	} else if len(duration) == 3 {
		duration = "  " + duration
	} else if len(duration) == 4 {
		duration = " " + duration
	}
	duration += ".0"
	fmt.Println(example + " => " + duration)
}
