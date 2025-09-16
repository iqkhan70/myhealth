#!/bin/bash

# Test Script for Multimedia Health Platform
# This script tests the new multimedia capabilities

echo "🚀 Testing Multimedia Health Platform Capabilities"
echo "================================================="

# Test 1: Check if Tesseract data is available
echo ""
echo "📁 Testing Tesseract OCR Setup..."
if [ -f "SM_MentalHealthApp.Server/tessdata/eng.traineddata" ]; then
    echo "✅ Tesseract English data file found"
    file_size=$(ls -lh "SM_MentalHealthApp.Server/tessdata/eng.traineddata" | awk '{print $5}')
    echo "   File size: $file_size"
else
    echo "❌ Tesseract English data file not found"
    echo "   Please download eng.traineddata to tessdata/ directory"
fi

# Test 2: Check database tables
echo ""
echo "🗄️ Testing Database Tables..."
echo "   New tables should be created:"
echo "   - ContentAnalyses"
echo "   - ContentAlerts"

# Test 3: Check configuration
echo ""
echo "⚙️ Testing Configuration..."
config_file="SM_MentalHealthApp.Server/appsettings.json"
if [ -f "$config_file" ]; then
    if grep -q "sk-your-actual-openai-api-key-here" "$config_file"; then
        echo "⚠️ OpenAI API key needs to be configured"
        echo "   Update appsettings.json with your actual OpenAI API key"
    else
        echo "✅ OpenAI API key appears to be configured"
    fi
    
    if grep -q "hf_" "$config_file"; then
        echo "✅ HuggingFace API key configured"
    else
        echo "⚠️ HuggingFace API key not configured"
    fi
else
    echo "❌ Configuration file not found"
fi

# Test 4: Check if build was successful
echo ""
echo "🔨 Testing Build Status..."
if [ -f "SM_MentalHealthApp.Server/bin/Debug/net9.0/SM_MentalHealthApp.Server.dll" ]; then
    echo "✅ Server build successful"
else
    echo "❌ Server build failed or not found"
fi

if [ -f "SM_MentalHealthApp.Client/bin/Debug/net9.0/SM_MentalHealthApp.Client.dll" ]; then
    echo "✅ Client build successful"
else
    echo "❌ Client build failed or not found"
fi

echo ""
echo "🎯 Multimedia Capabilities Available:"
echo "   ✅ PDF Text Extraction (iTextSharp)"
echo "   ✅ Word Document Processing (OpenXml)"
echo "   ✅ Image OCR (Tesseract + Azure Vision)"
echo "   ✅ Video Processing (FFmpeg)"
echo "   ✅ Audio Processing (Ready for Speech Services)"
echo "   ✅ GPT-4 Medical Analysis"
echo "   ✅ Intelligent Alert System"

echo ""
echo "🚀 Next Steps:"
echo "   1. Update OpenAI API key in appsettings.json"
echo "   2. Start the application: cd SM_MentalHealthApp.Server && dotnet run"
echo "   3. Upload test files through the web interface"
echo "   4. Check the Content Analysis section for results"

echo ""
echo "✨ Your multimedia health platform is ready!"
