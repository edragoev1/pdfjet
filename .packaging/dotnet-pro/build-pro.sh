#!/bin/bash
# Builds the example projects of PDFjet Pro against PDFjet.dll, which
# has PDFjet Pro in it, and runs them.

for example in SignDocument WriteInvoice DrawSwissQRBill DrawEPCQRCode HyphenateText; do
    dotnet run --project examples/pro/$example -c release || exit 1
done

# SignLongTerm signs with the test PKI of examples/pro/ltv, whose OCSP
# responder, CRLs and time stamp authority ltv-server.py answers for on
# 127.0.0.1:19180, with python3 and openssl. The server stops when this
# script ends and the pipe to it closes.
if ! command -v python3 > /dev/null || ! command -v openssl > /dev/null; then
    echo "SignLongTerm needs python3 and openssl, for the test server of examples/pro/ltv; it did not run."
    exit 0
fi
coproc SERVER { exec python3 examples/pro/ltv/ltv-server.py examples/pro/ltv 19180 --until-stdin-closes 2>&1; }
if ! read -r -t 10 PORT <&"${SERVER[0]}" || [ "$PORT" != 19180 ]; then
    echo "examples/pro/ltv/ltv-server.py did not start on port 19180, which may be taken: $PORT"
    exit 1
fi
dotnet run --project examples/pro/SignLongTerm -c release || exit 1
