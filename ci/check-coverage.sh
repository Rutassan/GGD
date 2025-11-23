#!/bin/bash
set -e

# Run tests and collect coverage in Cobertura format
dotnet test GGD.sln \
    --collect:"XPlat Code Coverage" \
    --settings Coverlet.runsettings \
    /p:CoverletOutputFormat=cobertura \
    /p:CoverletOutput=./coverage.cobertura.xml # This line might be redundant but keeping it for now

# Find the latest generated coverage.cobertura.xml file
COVERAGE_REPORT=$(find tests/GGD.Tests/TestResults -name "coverage.cobertura.xml" | sort -r | head -n 1)

if [ -z "$COVERAGE_REPORT" ]; then
    echo "Error: Could not find coverage.cobertura.xml report."
    exit 1
fi

echo "Found coverage report at: $COVERAGE_REPORT"

# Call the Python script to check coverage
python3 ci/check_coverage.py "$COVERAGE_REPORT" 100