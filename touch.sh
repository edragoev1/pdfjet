#!/bin/bash

export JAVA_HOME=/opt/jdk1.5.0_22
PATH=$JAVA_HOME/bin:$PATH

javac util/Touch.java
java util.Touch
