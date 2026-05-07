#!/bin/bash

# AI Widget Quick Start Script

echo "🚀 AI Query Widget - Quick Start"
echo "================================"
echo ""

# Check if Node.js is installed
if ! command -v node &> /dev/null; then
    echo "❌ Error: Node.js is not installed"
    echo "   Please install Node.js from https://nodejs.org/"
    exit 1
fi

echo "✅ Node.js $(node --version) detected"

# Check if npm is installed
if ! command -v npm &> /dev/null; then
    echo "❌ Error: npm is not installed"
    exit 1
fi

echo "✅ npm $(npm --version) detected"
echo ""

# Install dependencies
echo "📦 Installing dependencies..."
npm install

if [ $? -ne 0 ]; then
    echo "❌ Failed to install dependencies"
    exit 1
fi

echo "✅ Dependencies installed"
echo ""

# Build the widget
echo "🔨 Building widget..."
npm run build

if [ $? -ne 0 ]; then
    echo "❌ Build failed"
    exit 1
fi

echo "✅ Build successful"
echo ""

# Display output files
echo "📦 Output files:"
echo "   dist/ai-widget.js    (~5KB - loader script)"
echo "   dist/widget-app.js   (~50KB - React widget)"
echo "   dist/vendors.js      (~130KB - dependencies)"
echo ""

# Display next steps
echo "🎉 Widget is ready!"
echo ""
echo "Next steps:"
echo "1. Open dist/index.html in your browser to test"
echo "2. Deploy to CDN: aws s3 sync dist/ s3://your-bucket/"
echo "3. Integrate: <script src='https://cdn.yourdomain.com/ai-widget.js' data-api-key='...'></script>"
echo ""
echo "For more info:"
echo "  - Integration examples: See INTEGRATION_EXAMPLES.md"
echo "  - Deployment guide: See DEPLOYMENT.md"
echo "  - Architecture: See ARCHITECTURE.md"
echo ""
echo "To start dev server: npm run dev"
