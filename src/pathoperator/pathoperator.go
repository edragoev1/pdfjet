package pathoperator

// PathOperator contains constants for path drawing operations
type PathOperator struct{}

const (
	// Stroke the path
	Stroke = "S"

	// CloseAndStroke close and then stroke the path
	CloseAndStroke = "s"

	// Fill close and then fill the path
	Fill = "f"

	// FillAndStroke fill and then stroke the path
	FillAndStroke = "b"

	// FillUsingEvenOddRule like 'f' but using even odd rule
	FillUsingEvenOddRule = "f*"

	// FillUsingEvenOddRuleAndStroke like 'b' but using even odd rule
	FillUsingEvenOddRuleAndStroke = "b*"
)
