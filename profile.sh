#!/bin/sh
/opt/jdk1.5.0_22/bin/java -cp .:PDFjet.jar -agentlib:hprof=cpu=times Example_06 stream
