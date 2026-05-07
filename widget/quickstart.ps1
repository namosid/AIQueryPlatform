# AI Widget Quick Start Script (PowerShell)

Write-Host "🚀 AI Query Widget - Quick Start" -ForegroundColor Cyan
Write-Host "================================" -ForegroundColor Cyan
Write-Host ""

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "✅ Node.js $nodeVersion detected" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: Node.js is not installed" -ForegroundColor Red
    Write-Host "   Please install Node.js from https://nodejs.org/" -ForegroundColor Yellow
    exit 1
}

# Check if npm is installed
try {
    $npmVersion = npm --version
    Write-Host "✅ npm $npmVersion detected" -ForegroundColor Green
} catch {
    Write-Host "❌ Error: npm is not installed" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Install dependencies
Write-Host "📦 Installing dependencies..." -ForegroundColor Yellow
npm install

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Failed to install dependencies" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Dependencies installed" -ForegroundColor Green
Write-Host ""

# Build the widget
Write-Host "🔨 Building widget..." -ForegroundColor Yellow
npm run build

if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit 1
}

Write-Host "✅ Build successful" -ForegroundColor Green
Write-Host ""

# Display output files
Write-Host "📦 Output files:" -ForegroundColor Cyan
Write-Host "   dist/ai-widget.js    (~5KB - loader script)"
Write-Host "   dist/widget-app.js   (~50KB - React widget)"
Write-Host "   dist/vendors.js      (~130KB - dependencies)"
Write-Host ""

# Display next steps
Write-Host "🎉 Widget is ready!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Open dist/index.html in your browser to test"
Write-Host "2. Deploy to CDN: aws s3 sync dist/ s3://your-bucket/"
Write-Host "3. Integrate: <script src='https://cdn.yourdomain.com/ai-widget.js' data-api-key='...'></script>"
Write-Host ""
Write-Host "For more info:" -ForegroundColor Cyan
Write-Host "  - Integration examples: See INTEGRATION_EXAMPLES.md"
Write-Host "  - Deployment guide: See DEPLOYMENT.md"
Write-Host "  - Architecture: See ARCHITECTURE.md"
Write-Host ""
Write-Host "To start dev server: npm run dev" -ForegroundColor Yellow
