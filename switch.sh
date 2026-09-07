#!/bin/bash

# Check if the script is run as root
if [ "$(id -u)" -ne 0 ]; then
    echo "This script needs to be run as root or with sudo."
    exit 1
fi

# Prompt the user to select a Java version
echo "Select a Java version to switch to:"
echo "1) Java 8"
echo "2) Java 11"
echo "3) Java 17"
echo "4) Java 21"
echo "5) Java 25"

read -p "Enter your choice (1/2/3/4/5): " choice

# Set the Java version based on the user's input
case $choice in
    1)
        echo "Switching to Java 8..."
        sudo update-alternatives --set java /opt/jdk8u504-b01/bin/java
        sudo update-alternatives --set javac /opt/jdk8u504-b01/bin/javac
        sudo update-alternatives --set jar /opt/jdk8u504-b01/bin/jar
        sudo update-alternatives --set javadoc /opt/jdk8u504-b01/bin/javadoc
        ;;
    2)
        echo "Switching to Java 11..."
        sudo update-alternatives --set java /opt/jdk-11.0.32.1+1/bin/java
        sudo update-alternatives --set javac /opt/jdk-11.0.32.1+1/bin/javac
        sudo update-alternatives --set jar /opt/jdk-11.0.32.1+1/bin/jar
        sudo update-alternatives --set javadoc /opt/jdk-11.0.32.1+1/bin/javadoc
        ;;
    3)
        echo "Switching to Java 17..."
        sudo update-alternatives --set java /opt/jdk-17.0.20.1+1/bin/java
        sudo update-alternatives --set javac /opt/jdk-17.0.20.1+1/bin/javac
        sudo update-alternatives --set jar /opt/jdk-17.0.20.1+1/bin/jar
        sudo update-alternatives --set javadoc /opt/jdk-17.0.20.1+1/bin/javadoc
        ;;
    4)
        echo "Switching to Java 21..."
        sudo update-alternatives --set java /opt/jdk-21.0.12.1+1/bin/java
        sudo update-alternatives --set javac /opt/jdk-21.0.12.1+1/bin/javac
        sudo update-alternatives --set jar /opt/jdk-21.0.12.1+1/bin/jar
        sudo update-alternatives --set javadoc /opt/jdk-21.0.12.1+1/bin/javadoc
        ;;
    5)
        echo "Switching to Java 25..."
        sudo update-alternatives --set java /opt/jdk-25.0.4.1+1/bin/java
        sudo update-alternatives --set javac /opt/jdk-25.0.4.1+1/bin/javac
        sudo update-alternatives --set jar /opt/jdk-25.0.4.1+1/bin/jar
        sudo update-alternatives --set javadoc /opt/jdk-25.0.4.1+1/bin/javadoc
        ;;
    *)
        echo "Invalid choice. Please run the script again and choose a valid option."
        exit 1
        ;;
esac

# Show the currently active Java version
echo "Java version after switch:"
java -version
