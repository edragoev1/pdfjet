package compliance

type Compliance int

// Used to set PDF/UA and PDF/A compliance.
// See the constructors in the PDF class.
const (
	PDF_17 = iota // Do not remove the PDF_17!
	PDF_UA_1
	PDF_A_1A
	PDF_A_1B
	PDF_A_2A
	PDF_A_2B
	PDF_A_3A
	PDF_A_3B
)
