#!/bin/bash
set -e

REPORT_DIR="coverage-report"
COVERAGE_DIR="coverage-results"

# Clean previous results
rm -rf "$COVERAGE_DIR" "$REPORT_DIR"
mkdir -p "$COVERAGE_DIR"

echo "Running Domain tests with coverage..."
dotnet test source/MoriiCoffee.Domain.Tests/ \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE_DIR/domain" \
  --verbosity quiet

echo "Running Application tests with coverage..."
dotnet test source/MoriiCoffee.Application.Tests/ \
  --collect:"XPlat Code Coverage" \
  --results-directory "$COVERAGE_DIR/application" \
  --verbosity quiet

echo "Generating HTML report..."
export PATH="$PATH:/Users/zephyr.nguyen/.dotnet/tools"
reportgenerator \
  -reports:"$COVERAGE_DIR/**/coverage.cobertura.xml" \
  -targetdir:"$REPORT_DIR" \
  -reporttypes:Html \
  -assemblyfilters:"+MoriiCoffee.Domain;+MoriiCoffee.Application" \
  -verbosity:Warning

echo ""
echo "Coverage report generated: $REPORT_DIR/index.html"
open "$REPORT_DIR/index.html" 2>/dev/null || echo "Open $REPORT_DIR/index.html in your browser"
