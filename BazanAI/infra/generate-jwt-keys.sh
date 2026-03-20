#!/bin/bash

# Tạo thư mục chứa keys nếu chưa có
mkdir -p ./secrets

# Generate RSA 2048-bit private key
openssl genrsa -out ./secrets/jwt_private.pem 2048

# Generate RSA 2048-bit public key từ private key
openssl rsa -in ./secrets/jwt_private.pem -pubout -out ./secrets/jwt_public.pem

# Set permissions
chmod 400 ./secrets/jwt_private.pem
chmod 444 ./secrets/jwt_public.pem

echo "JWT RSA key pair generated successfully in ./infra/secrets/"
