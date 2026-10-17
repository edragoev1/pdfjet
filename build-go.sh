bash "$(dirname "$0")/get-fonts-and-data.sh" || exit 1
# Stop at the first failure: a library that does not build, or go vet, was
# passed over, and the script gave the exit code of the last example (review
# of 17 October 2026).
set -e
# The folder of this script, which has fonts and data, from wherever it is run.
cd "$(dirname "$0")" || exit 1
cd src

# The Go compiler has no warnings; go vet reports the suspicious code instead.
go vet ./...

for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        go build -o ../Example_0$i.exe examples/example0$i/main.go
    else
        go build -o ../Example_$i.exe examples/example$i/main.go
    fi
done

cd ..

for i in $(seq 1 50);
do
    if [ $i -lt 10 ]; then
        ./Example_0$i.exe
    else
        ./Example_$i.exe
    fi
done
