# Optimize TTF or OTF font by converting it to .ttf.stream or .otf.stream
# These stream fonts can be enbedded in PDFs much faster
java --class-path .:PDFjet.jar com.pdfjet.OptimizeOTF $1
