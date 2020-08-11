#!/bin/sh

javac -O -encoding utf-8 -Xlint com/pdfjet/*.java
jar cf PDFjet.jar com/pdfjet/*.class

javac -O -encoding utf-8 -Xlint util/StreamlinePNG.java
jar cf StreamlinePNG.jar util/StreamlinePNG.class

javac -O -encoding utf-8 -Xlint util/StreamlineOTF.java
jar cf StreamlineOTF.jar util/StreamlineOTF.class

javac -O -encoding utf-8 -Xlint -cp .:PDFjet.jar examples/*.java

java -cp .:PDFjet.jar examples.Example_01
java -cp .:PDFjet.jar examples.Example_02
java -cp .:PDFjet.jar examples.Example_03
java -cp .:PDFjet.jar examples.Example_04
java -cp .:PDFjet.jar examples.Example_05
java -cp .:PDFjet.jar examples.Example_06
java -cp .:PDFjet.jar examples.Example_07
java -cp .:PDFjet.jar examples.Example_08
java -cp .:PDFjet.jar examples.Example_09
java -cp .:PDFjet.jar examples.Example_10
java -cp .:PDFjet.jar examples.Example_11
java -cp .:PDFjet.jar examples.Example_12
java -cp .:PDFjet.jar examples.Example_13
java -cp .:PDFjet.jar examples.Example_14
java -cp .:PDFjet.jar examples.Example_15
java -cp .:PDFjet.jar examples.Example_16
java -cp .:PDFjet.jar examples.Example_17
java -cp .:PDFjet.jar examples.Example_18
java -cp .:PDFjet.jar examples.Example_19
java -cp .:PDFjet.jar examples.Example_20
java -cp .:PDFjet.jar examples.Example_21
java -cp .:PDFjet.jar examples.Example_22
java -cp .:PDFjet.jar examples.Example_23
java -cp .:PDFjet.jar examples.Example_24
java -cp .:PDFjet.jar examples.Example_25
java -cp .:PDFjet.jar examples.Example_26
java -cp .:PDFjet.jar examples.Example_27
java -cp .:PDFjet.jar examples.Example_28
java -cp .:PDFjet.jar examples.Example_29
java -cp .:PDFjet.jar examples.Example_30
java -cp .:PDFjet.jar examples.Example_31
java -cp .:PDFjet.jar examples.Example_32
java -cp .:PDFjet.jar examples.Example_33
java -cp .:PDFjet.jar examples.Example_34
java -cp .:PDFjet.jar examples.Example_35
java -cp .:PDFjet.jar examples.Example_36
java -cp .:PDFjet.jar examples.Example_37
java -cp .:PDFjet.jar examples.Example_38
java -cp .:PDFjet.jar examples.Example_39
java -cp .:PDFjet.jar examples.Example_40
java -cp .:PDFjet.jar examples.Example_41
java -cp .:PDFjet.jar examples.Example_42
java -cp .:PDFjet.jar examples.Example_43
java -cp .:PDFjet.jar examples.Example_44
java -cp .:PDFjet.jar examples.Example_45
java -cp .:PDFjet.jar examples.Example_46
java -cp .:PDFjet.jar examples.Example_47
java -cp .:PDFjet.jar examples.Example_48
java -cp .:PDFjet.jar examples.Example_49
java -cp .:PDFjet.jar examples.Example_50

java -cp .:PDFjet.jar examples.Example_71
java -cp .:PDFjet.jar examples.Example_72
java -cp .:PDFjet.jar examples.Example_73
java -cp .:PDFjet.jar examples.Example_74
java -cp .:PDFjet.jar examples.Example_75
java -cp .:PDFjet.jar examples.Example_76
java -cp .:PDFjet.jar examples.Example_77
java -cp .:PDFjet.jar examples.Example_78
java -cp .:PDFjet.jar examples.Example_79
java -cp .:PDFjet.jar examples.Example_80
java -cp .:PDFjet.jar examples.Example_81
java -cp .:PDFjet.jar examples.Example_99
