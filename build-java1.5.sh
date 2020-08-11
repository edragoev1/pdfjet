#!/bin/sh

/opt/jdk1.5.0_22/bin/javac -O -encoding utf-8 -Xlint com/pdfjet/*.java
/opt/jdk1.5.0_22/bin/jar cf PDFjet.jar com/pdfjet/*.class

/opt/jdk1.5.0_22/bin/javac -O -encoding utf-8 -Xlint util/StreamlinePNG.java
/opt/jdk1.5.0_22/bin/jar cf StreamlinePNG.jar util/StreamlinePNG.class

/opt/jdk1.5.0_22/bin/javac -O -encoding utf-8 -Xlint util/StreamlineOTF.java
/opt/jdk1.5.0_22/bin/jar cf StreamlineOTF.jar util/StreamlineOTF.class

/opt/jdk1.5.0_22/bin/javac -O -encoding utf-8 -Xlint -cp .:PDFjet.jar examples/*.java

/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_01
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_02
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_03
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_04
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_05
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_06
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_07
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_08
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_09
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_10
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_11
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_12
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_13
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_14
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_15
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_16
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_17
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_18
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_19
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_20
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_21
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_22
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_23
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_24
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_25
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_26
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_27
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_28
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_29
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_30
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_31
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_32
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_33
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_34
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_35
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_36
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_37
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_38
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_39
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_40
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_41
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_42
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_43
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_44
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_45
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_46
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_47
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_48
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_49
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_50

/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_71
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_72
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_73
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_74
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_75
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_76
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_77
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_78
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_79
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_80
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_81
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar examples.Example_99
