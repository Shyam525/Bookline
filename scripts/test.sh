#!/usr/bin/env bash
set -e
echo "Running Backend Unit & Integration Tests..."
dotnet test backend/Bookline.slnx -c Release

echo "Running Frontend Automated Tests..."
cd frontend
npm run test

echo "Running Frontend Type Checks & Build..."
npm run build
cd ..

echo "All Tests & Build Verification Passed Cleanly!"
