// operator.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

package pathoperator

// Constants used to specify the 'Stroke', 'CloseAndStroke', 'Fill' and more operators.
const (
	Stroke                        = "S"  // Stroke the path
	CloseAndStroke                = "s"  // Close and then stroke the path
	Fill                          = "f"  // Close and fill the path
	FillAndStroke                 = "b"  // Close, fill and then stroke the path
	FillUsingEvenOddRule          = "f*" // Like 'f' but using even odd rule
	FillUsingEvenOddRuleAndStroke = "b*" // Like 'b' but using even odd rule
)
