// operator.go
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.

// Package pathoperator defines the path painting operators, such as stroke and fill.
package pathoperator

// PathOperator specifies the operator that paints a path.
type PathOperator string

// Constants used to specify the 'Stroke', 'CloseAndStroke', 'Fill' and more operators.
const (
	Stroke                        PathOperator = "S"  // Stroke the path
	CloseAndStroke                PathOperator = "s"  // Close and then stroke the path
	Fill                          PathOperator = "f"  // Close and fill the path
	FillAndStroke                 PathOperator = "b"  // Close, fill and then stroke the path
	FillUsingEvenOddRule          PathOperator = "f*" // Like 'f' but using even odd rule
	FillUsingEvenOddRuleAndStroke PathOperator = "b*" // Like 'b' but using even odd rule
)
