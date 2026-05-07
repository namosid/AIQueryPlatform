# 🚀 Deployment Guide

## Build Process

### 1. Install Dependencies
```bash
cd widget
npm install
```

### 2. Build for Production
```bash
npm run build
```

This generates:
```
dist/
├── ai-widget.js       # Loader script (~5KB minified)
├── widget-app.js      # React widget bundle
├── vendors.js         # React + dependencies
└── index.html         # Test page
```

---

## CDN Deployment

### Option 1: AWS CloudFront + S3

#### Upload to S3
```bash
# Install AWS CLI
pip install awscli

# Configure credentials
aws configure

# Upload files
aws s3 sync dist/ s3://your-cdn-bucket/ai-widget/ \
  --cache-control "public, max-age=31536000" \
  --exclude "*.html"

# Upload with versioning
aws s3 sync dist/ s3://your-cdn-bucket/ai-widget/v1.0.0/ \
  --cache-control "public, max-age=31536000"
```

#### CloudFront Distribution
1. Create distribution pointing to S3 bucket
2. Enable CORS:
```json
{
  "AllowedHeaders": ["*"],
  "AllowedMethods": ["GET", "HEAD"],
  "AllowedOrigins": ["*"],
  "ExposeHeaders": []
}
```
3. Set cache behaviors:
   - `*.js` files: cache for 1 year
   - Update `latest/` path when deploying new versions

#### Usage
```html
<script src="https://d123456.cloudfront.net/ai-widget/ai-widget.js"
        data-api-base-url="https://api.yourdomain.com"
        data-api-key="your_key">
</script>
```

---

### Option 2: Azure CDN + Blob Storage

#### Upload to Blob Storage
```bash
# Install Azure CLI
curl -sL https://aka.ms/InstallAzureCLIDeb | sudo bash

# Login
az login

# Create storage account (if needed)
az storage account create \
  --name yourcdnstorage \
  --resource-group your-rg \
  --location eastus

# Enable static website
az storage blob service-properties update \
  --account-name yourcdnstorage \
  --static-website \
  --index-document index.html

# Upload files
az storage blob upload-batch \
  --account-name yourcdnstorage \
  --destination '$web/ai-widget' \
  --source dist/ \
  --content-cache-control "public, max-age=31536000"
```

#### Create CDN Profile
```bash
az cdn profile create \
  --name ai-widget-cdn \
  --resource-group your-rg \
  --sku Standard_Microsoft

az cdn endpoint create \
  --name ai-widget \
  --profile-name ai-widget-cdn \
  --resource-group your-rg \
  --origin yourcdnstorage.z13.web.core.windows.net
```

---

### Option 3: Cloudflare CDN + Pages

#### Deploy via CLI
```bash
# Install Wrangler
npm install -g wrangler

# Login
wrangler login

# Deploy
wrangler pages publish dist --project-name=ai-widget
```

#### Usage
```html
<script src="https://ai-widget.pages.dev/ai-widget.js"
        data-api-base-url="https://api.yourdomain.com"
        data-api-key="your_key">
</script>
```

---

### Option 4: Netlify

#### Deploy
```bash
# Install Netlify CLI
npm install -g netlify-cli

# Login
netlify login

# Deploy
netlify deploy --prod --dir=dist
```

#### netlify.toml
```toml
[build]
  publish = "dist"
  command = "npm run build"

[[headers]]
  for = "/*.js"
  [headers.values]
    Cache-Control = "public, max-age=31536000"
    Access-Control-Allow-Origin = "*"

[[redirects]]
  from = "/latest/*"
  to = "/v1.0.0/:splat"
  status = 301
```

---

## Version Management

### Semantic Versioning
```bash
# Update version in package.json
npm version patch  # 1.0.0 -> 1.0.1
npm version minor  # 1.0.1 -> 1.1.0
npm version major  # 1.1.0 -> 2.0.0

# Build with version
npm run build

# Deploy to versioned path
aws s3 sync dist/ s3://cdn-bucket/ai-widget/v$(node -p "require('./package.json').version")/
```

### Symlink Latest
```bash
# Point 'latest' to current version
aws s3 sync s3://cdn-bucket/ai-widget/v1.0.0/ \
            s3://cdn-bucket/ai-widget/latest/ \
            --delete
```

---

## CI/CD Pipeline

### GitHub Actions

#### .github/workflows/deploy.yml
```yaml
name: Deploy Widget

on:
  push:
    branches: [main]
    tags: ['v*']

jobs:
  build-and-deploy:
    runs-on: ubuntu-latest
    
    steps:
      - uses: actions/checkout@v3
      
      - name: Setup Node
        uses: actions/setup-node@v3
        with:
          node-version: '18'
          
      - name: Install dependencies
        run: |
          cd widget
          npm ci
          
      - name: Build
        run: |
          cd widget
          npm run build
          
      - name: Deploy to S3
        env:
          AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
          AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        run: |
          VERSION=$(node -p "require('./widget/package.json').version")
          aws s3 sync widget/dist/ s3://your-cdn-bucket/ai-widget/v$VERSION/ \
            --cache-control "public, max-age=31536000" \
            --exclude "*.html"
          
      - name: Invalidate CloudFront
        env:
          AWS_ACCESS_KEY_ID: ${{ secrets.AWS_ACCESS_KEY_ID }}
          AWS_SECRET_ACCESS_KEY: ${{ secrets.AWS_SECRET_ACCESS_KEY }}
        run: |
          aws cloudfront create-invalidation \
            --distribution-id ${{ secrets.CLOUDFRONT_DISTRIBUTION_ID }} \
            --paths "/ai-widget/*"
```

---

## Backend CORS Configuration

### ASP.NET Core
Update your API's Program.cs:

```csharp
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
            "https://yourdomain.com",
            "https://www.yourdomain.com",
            "http://localhost:3001" // Development
        )
        .AllowAnyMethod()
        .AllowAnyHeader()
        .AllowCredentials();
    });
});

app.UseCors();
```

---

## Security Checklist

### ✅ Pre-Deployment

- [ ] API keys are NOT hardcoded in widget code
- [ ] HTTPS enabled on CDN
- [ ] CORS configured correctly
- [ ] Rate limiting enabled on API
- [ ] API key validation on backend
- [ ] Content Security Policy (CSP) headers set
- [ ] Subresource Integrity (SRI) hashes generated

### Generate SRI Hashes
```bash
# Generate hash for ai-widget.js
openssl dgst -sha384 -binary dist/ai-widget.js | openssl base64 -A

# Usage
<script src="https://cdn.yourdomain.com/ai-widget.js"
        integrity="sha384-HASH_HERE"
        crossorigin="anonymous"
        data-api-key="...">
</script>
```

---

## Monitoring

### CloudWatch Metrics (AWS)
```bash
# Monitor CDN requests
aws cloudwatch get-metric-statistics \
  --namespace AWS/CloudFront \
  --metric-name Requests \
  --dimensions Name=DistributionId,Value=YOUR_DIST_ID \
  --start-time 2024-01-01T00:00:00Z \
  --end-time 2024-01-02T00:00:00Z \
  --period 3600 \
  --statistics Sum
```

### Application Insights (Azure)
```javascript
// Add to widget for telemetry
window.appInsights = {
  config: {
    instrumentationKey: "YOUR_KEY"
  }
};
```

---

## Rollback Strategy

### Quick Rollback
```bash
# Revert to previous version
aws s3 sync s3://cdn-bucket/ai-widget/v1.0.0/ \
            s3://cdn-bucket/ai-widget/latest/ \
            --delete

# Invalidate cache
aws cloudfront create-invalidation \
  --distribution-id YOUR_DIST_ID \
  --paths "/ai-widget/*"
```

---

## Performance Optimization

### 1. Compression
Ensure gzip/brotli enabled on CDN:
```bash
# AWS S3
aws s3 cp dist/ai-widget.js s3://bucket/ai-widget.js \
  --content-encoding gzip \
  --content-type "application/javascript"
```

### 2. Minification
Already handled by webpack production build.

### 3. Cache Headers
```bash
# Long cache for versioned files
Cache-Control: public, max-age=31536000, immutable

# Short cache for 'latest'
Cache-Control: public, max-age=300
```

---

## Testing Deployment

### 1. Test Page
```html
<!DOCTYPE html>
<html>
<head><title>Widget Test</title></head>
<body>
    <h1>Widget Test Page</h1>
    <script src="https://YOUR_CDN_URL/ai-widget.js"
            data-api-base-url="https://YOUR_API_URL"
            data-api-key="test_key"
            data-tenant-id="test_tenant">
    </script>
</body>
</html>
```

### 2. Automated Tests
```bash
# Check if files are accessible
curl -I https://YOUR_CDN_URL/ai-widget.js
curl -I https://YOUR_CDN_URL/widget-app.js
curl -I https://YOUR_CDN_URL/vendors.js

# Verify CORS headers
curl -H "Origin: https://example.com" \
     -I https://YOUR_CDN_URL/ai-widget.js
```

---

## Cost Estimation

### AWS CloudFront + S3
- S3 Storage: ~$0.023/GB/month
- CloudFront Requests: ~$0.0075/10,000 requests
- Data Transfer: ~$0.085/GB (first 10 TB)

**Example:** 1M requests/month with 5KB widget:
- Requests: $0.75
- Transfer: 5GB × $0.085 = $0.43
- **Total: ~$1.20/month**

### Cloudflare (Free Tier)
- Unlimited bandwidth
- Unlimited requests
- **Total: $0/month**

---

## Support & Troubleshooting

### Common Issues

**Widget not loading:**
1. Check browser console for errors
2. Verify CDN URL is accessible
3. Check CORS headers
4. Verify API key is valid

**Slow loading:**
1. Check CDN cache hit rate
2. Enable compression
3. Verify cache headers
4. Consider CDN with closer edge locations

**Breaking changes:**
1. Always use versioned URLs in production
2. Test new versions before updating 'latest'
3. Keep old versions available for rollback
