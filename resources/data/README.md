# resources/data

The example data of [PDFjet](https://github.com/edragoev1/pdfjet): the text,
CSV, XML, Markdown, image and PDF files its examples and tests read, by paths
relative to the root of the repository, such as
`resources/data/the-idiot.txt`.

The files are in the PDFjet repository itself, so nothing is fetched to build,
test or run the examples. Up to PDFjet 9.1.5 they were the repository
pdfjet-data, which the scripts fetched into the folder `data`. `go.mod` keeps
this folder out of the Go module of PDFjet, so `go get` does not download it.

## Where the data comes from

- `the-idiot.txt` is "The Idiot" by Fyodor Dostoyevsky, in the English
  translation by Eva Martin (1915), from Project Gutenberg, eBook #2638, and
  in the public domain. `the-idiot-source.txt` says where it comes from and
  how the text was prepared. `dostoevsky.txt` is from the same translation.
- `languages` holds sample text in the languages and scripts PDFjet draws.
- `testPDFs/PDFjetLogo.pdf`, `markdown` and `invoice` are PDFjet's own
  (`invoice/logo.svg` is the logo of a fictional print shop);
  `testPDFs/scanned-linearized.pdf` is written by `util/make-scanned-pdf.py`
  of PDFjet.
- `life-expectancy-and-fertility-2023.txt` is the life expectancy at birth,
  in years, and the children per woman of 2023 of the 217 countries and
  economies that have both, separated by `|`: the World Bank's World
  Development Indicators SP.DYN.LE00.IN and SP.DYN.TFRT.IN, as its API gave
  them on 26 October 2026 (last updated 8 October 2026), rounded to 1 and 2
  decimals, without the aggregates of regions and income groups; then the
  World Bank region of each, as its API gave it on 29 October 2026. Source:
  World Bank, World Development Indicators, licensed under
  [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/).
- `Electric_Vehicle_Population_10_Pages.csv` is the header and the first
  549 rows of the Washington State Electric Vehicle Population Data, the
  state's open data on data.wa.gov, 124,716 rows in the copy the examples
  were made with; Example_24 draws it as a table, and the full file when it
  is given its path. The full file, 30 MB, is not in the repository.
  `Electric_Vehicles_by_Model_Year.csv` is its vehicles counted by model
  year and by kind, battery electric and plug-in hybrid, which Example_25
  draws, written from the full file by `util/make-ev-by-model-year.py`.
  Source: Washington State Department of Licensing, [Electric Vehicle
  Population Data](https://data.wa.gov/d/f6w7-q2d2), licensed under the
  [Open Data Commons Open Database License (ODbL)
  1.0](https://opendatacommons.org/licenses/odbl/1-0/); the two files, a
  part of it and counts made from it, are under the same license.
- The other files are sample data for the examples: the tables they draw, an
  invoice in the Factur-X format, and a form they read pages from.
