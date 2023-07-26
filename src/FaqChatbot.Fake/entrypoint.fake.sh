#!/bin/bash
set -o errexit
set -o xtrace

# generate the key and cert files
openssl genrsa -out /etc/ssl/server.key 2048
openssl req -new \
            -x509 \
            -sha256 \
            -key /etc/ssl/server.key \
            -out /etc/ssl/server.crt \
            -days 3650 \
            -subj "/C=US/ST=New York/L=New York/O=ZocDoc Inc"

# generate the pfx file
openssl pkcs12 -export \
               -out /etc/ssl/server.pfx \
               -inkey /etc/ssl/server.key \
               -in /etc/ssl/server.crt \
               -password pass:

dotnet /app/FaqChatbot.Fake.dll
