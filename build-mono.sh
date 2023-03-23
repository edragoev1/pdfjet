mcs -warn:2 -debug -sdk:4.0 net/pdfjet/*.cs -reference:System.Drawing.dll -target:library -out:PDFjet.dll

mcs -warn:2 -debug examples/Example_01.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_02.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_03.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_04.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_05.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_06.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_07.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_08.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_09.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_10.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_11.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_12.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_13.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_14.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_15.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_16.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_17.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_18.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_19.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_20.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_21.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_22.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_23.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_24.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_25.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_26.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_27.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_28.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_29.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_30.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_31.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_32.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_33.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_34.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_35.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_36.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_37.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_38.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_39.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_40.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_41.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_42.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_43.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_44.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_45.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_46.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_47.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_48.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_49.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_50.cs -reference:PDFjet.dll
mcs -warn:2 -debug examples/Example_51.cs -reference:PDFjet.dll

chmod 777 examples/Example_??.exe
mv examples/Example_??.exe .

mono --debug Example_01.exe
mono --debug Example_02.exe
mono --debug Example_03.exe
mono --debug Example_04.exe
mono --debug Example_05.exe
mono --debug Example_06.exe
mono --debug Example_07.exe
mono --debug Example_08.exe
mono --debug Example_09.exe
mono --debug Example_10.exe
mono --debug Example_11.exe
mono --debug Example_12.exe
mono --debug Example_13.exe
mono --debug Example_14.exe
mono --debug Example_15.exe
mono --debug Example_16.exe
mono --debug Example_17.exe
mono --debug Example_18.exe
mono --debug Example_19.exe
mono --debug Example_20.exe
mono --debug Example_21.exe
mono --debug Example_22.exe
mono --debug Example_23.exe
mono --debug Example_24.exe
mono --debug Example_25.exe
mono --debug Example_26.exe
mono --debug Example_27.exe
mono --debug Example_28.exe
mono --debug Example_29.exe
mono --debug Example_30.exe
mono --debug Example_31.exe
mono --debug Example_32.exe
mono --debug Example_33.exe
mono --debug Example_34.exe
mono --debug Example_35.exe
mono --debug Example_36.exe
mono --debug Example_37.exe
mono --debug Example_38.exe
mono --debug Example_39.exe
mono --debug Example_40.exe
mono --debug Example_41.exe
mono --debug Example_42.exe
mono --debug Example_43.exe
mono --debug Example_44.exe
mono --debug Example_45.exe
mono --debug Example_46.exe
mono --debug Example_47.exe
mono --debug Example_48.exe
mono --debug Example_49.exe
mono --debug Example_50.exe
mono --debug Example_51.exe
