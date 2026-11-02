#!/bin/sh
set -eu

awslocal s3api head-bucket --bucket kotshop-arquivos 2>/dev/null || \
    awslocal s3api create-bucket --bucket kotshop-arquivos

# Only product images are public; other files in the bucket remain private.
awslocal s3api put-bucket-policy --bucket kotshop-arquivos --policy '{"Version":"2012-10-17","Statement":[{"Effect":"Allow","Principal":"*","Action":"s3:GetObject","Resource":"arn:aws:s3:::kotshop-arquivos/produtos/*"}]}'
