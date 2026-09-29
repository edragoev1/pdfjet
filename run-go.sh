bash "$(dirname "$0")/get-fonts-and-data.sh" || exit 1
# The folder of this script, which has fonts and data, from wherever it is run.
cd "$(dirname "$0")" || exit 1
if [ $# -eq 0 ]; then
    echo "Please provide an example number:"
    echo "./run-go.sh 33"
    exit 1
fi

# Very important!!
./clean.sh

cd src
go build -o ../Example_$1.exe examples/example$1/main.go
cd ..

./Example_$1.exe

mupdf Example_$1.pdf
# evince Example_$1.pdf
