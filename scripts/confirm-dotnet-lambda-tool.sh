#!/bin/bash
# confirm dotnet Amazon.Lambda.Tools is installed
set -o errexit

tools_installed=$(dotnet tool list -g) ;\
if [[ ! $tools_installed =~ "amazon.lambda.tools" ]]; then
    echo "dotnet lambda tools not installed | install via 'dotnet tool install -g Amazon.Lambda.Tools'"
    exit 1
fi
