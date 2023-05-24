cd src

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
