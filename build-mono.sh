mcs -warn:2 -debug -sdk:4.0 net/pdfjet/*.cs -reference:System.Drawing.dll -target:library -out:PDFjet.dll

for i in $(seq 1 51);
do
    if [ $i -lt 10 ]; then
        mcs -warn:2 -debug examples/Example_0$i.cs -reference:PDFjet.dll
    else
        mcs -warn:2 -debug examples/Example_$i.cs -reference:PDFjet.dll
    fi
done

chmod 777 examples/Example_??.exe
mv examples/Example_??.exe .

for i in $(seq 1 51);
do
    if [ $i -lt 10 ]; then
        mono --debug Example_0$i.exe
    else
        mono --debug Example_$i.exe
    fi
done
