# Sourced by the build, run and test scripts. The fonts and the data files the
# examples and the tests read are the git submodules fonts and data, the
# repositories pdfjet-fonts and pdfjet-data. A clone made without
# --recurse-submodules has them as empty directories, so stop with the command
# that checks them out, rather than fail later on a file that is not there.
for _dir in fonts data; do
    if [ ! -f "${PDFJET_ROOT:-$(dirname "$0")}/$_dir/README.md" ]; then
        echo "The $_dir directory is empty: it is the git submodule pdfjet-$_dir." >&2
        echo "Check out the fonts and the data files with:" >&2
        echo "" >&2
        echo "    git submodule update --init" >&2
        echo "" >&2
        echo "or clone with: git clone --recurse-submodules https://github.com/edragoev1/pdfjet.git" >&2
        exit 1
    fi
done
# A git pull that moves a submodule to another commit leaves its files as they
# were, unless submodule.recurse is set: say so, and go on with them.
if git -C "${PDFJET_ROOT:-$(dirname "$0")}" submodule status fonts data 2> /dev/null | grep -q '^+'; then
    echo "Warning: fonts or data is not at the commit this checkout records, as after" >&2
    echo "a git pull that moved it. Update them with: git submodule update" >&2
fi
