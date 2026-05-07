# AI Query Widget SDK

Universal, plug-and-play AI insights widget for any website.

## 🚀 Quick Start

Add ONE line to your website:

```html
<script src="https://cdn.yourdomain.com/ai-widget.js"
        data-api-base-url="http://localhost:5000"
        data-api-key="demo_api_key_12345"
        data-tenant-id="11111111-1111-1111-1111-111111111111"
        data-position="bottom-right"
        data-theme="light">
</script>
```

That's it! The widget will automatically appear.

## 📦 Project Structure

```
widget/
├── src/
│   ├── loader.ts              # Lightweight loader script
│   ├── widget/
│   │   ├── App.tsx           # Main React widget
│   │   ├── components/       # UI components
│   │   ├── services/         # API service layer
│   │   ├── styles/           # CSS modules
│   │   └── types/            # TypeScript types
│   └── index.tsx             # Widget entry point
├── public/
│   └── widget.html           # Test page
├── dist/                     # Build output
├── package.json
├── tsconfig.json
├── webpack.config.js
└── README.md
```

## 🛠️ Build & Deploy

```bash
# Install dependencies
npm install

# Development mode
npm run dev

# Production build
npm run build

# Deploy to CDN
npm run deploy
```

## 🎨 Configuration Options

| Attribute | Required | Default | Description |
|-----------|----------|---------|-------------|
| `data-api-base-url` | ✅ | - | Your API endpoint |
| `data-api-key` | ✅ | - | Tenant API key |
| `data-tenant-id` | ✅ | - | Tenant UUID |
| `data-user-role` | ❌ | "User" | User role |
| `data-theme` | ❌ | "light" | light/dark theme |
| `data-position` | ❌ | "bottom-right" | Widget position |
| `data-expand-url` | ❌ | null | Deep analysis link |
| `data-auto-open` | ❌ | "false" | Auto-open on load |

## 🔒 Security

- API keys passed via secure headers
- CORS validation on backend
- No sensitive data in localStorage
- Domain validation supported

## 📱 Browser Support

- Chrome 90+
- Firefox 88+
- Safari 14+
- Edge 90+

## 🎯 Features

- ✅ Zero dependencies on client framework
- ✅ Isolated Shadow DOM (no CSS conflicts)
- ✅ Lazy loading (< 50KB initial)
- ✅ Mobile responsive
- ✅ Accessible (WCAG 2.1 AA)
- ✅ Auto-refresh insights
- ✅ Natural language queries
- ✅ Suggested queries
- ✅ Error handling & fallbacks
