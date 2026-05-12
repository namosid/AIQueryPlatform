# AI Analytics Modal Workspace - Integration Guide

## 🚀 Quick Start (Plug-and-Play)

Add **ONE** script tag to your website:

```html
<script 
  src="https://cdn.yourdomain.com/ai-widget.js"
  data-api-base-url="https://api.yourdomain.com"
  data-api-key="YOUR_API_KEY"
  data-tenant-id="YOUR_TENANT_ID"
  data-theme="light"
  data-position="bottom-right">
</script>
```

That's it! No npm install, no build process, no framework dependencies.

---

## 📋 Configuration Options

| Attribute | Required | Default | Description |
|-----------|----------|---------|-------------|
| `data-api-base-url` | ✅ Yes | - | Your API base URL |
| `data-api-key` | ✅ Yes | - | API authentication key |
| `data-tenant-id` | ✅ Yes | - | Tenant identifier |
| `data-theme` | No | `light` | UI theme: `light` or `dark` |
| `data-position` | No | `bottom-right` | Widget position: `bottom-right`, `bottom-left`, `top-right`, `top-left` |
| `data-user-role` | No | `User` | User role for permissions |
| `data-auto-open` | No | `false` | Auto-open widget on page load |

---

## 🎯 Integration Examples

### Example 1: Basic Integration

```html
<!DOCTYPE html>
<html>
<head>
  <title>My App</title>
</head>
<body>
  <h1>Welcome to My Application</h1>
  
  <!-- AI Widget - Single Line -->
  <script 
    src="https://cdn.yourdomain.com/ai-widget.js"
    data-api-base-url="https://api.yourdomain.com"
    data-api-key="sk_live_abc123"
    data-tenant-id="company_xyz">
  </script>
</body>
</html>
```

### Example 2: Dark Theme with Custom Position

```html
<script 
  src="https://cdn.yourdomain.com/ai-widget.js"
  data-api-base-url="https://api.yourdomain.com"
  data-api-key="sk_live_abc123"
  data-tenant-id="company_xyz"
  data-theme="dark"
  data-position="bottom-left">
</script>
```

### Example 3: React Application

```jsx
// App.jsx
import React, { useEffect } from 'react';

function App() {
  useEffect(() => {
    // Dynamically load AI widget script
    const script = document.createElement('script');
    script.src = 'https://cdn.yourdomain.com/ai-widget.js';
    script.dataset.apiBaseUrl = 'https://api.yourdomain.com';
    script.dataset.apiKey = process.env.REACT_APP_AI_API_KEY;
    script.dataset.tenantId = 'company_xyz';
    script.dataset.theme = 'light';
    
    document.body.appendChild(script);

    return () => {
      // Cleanup on unmount
      document.body.removeChild(script);
      // Remove widget containers
      document.getElementById('ai-widget-root')?.remove();
      document.getElementById('ai-modal-root')?.remove();
    };
  }, []);

  return (
    <div>
      <h1>My React App</h1>
      {/* Widget will appear automatically */}
    </div>
  );
}

export default App;
```

### Example 4: Vue.js Application

```vue
<!-- App.vue -->
<template>
  <div id="app">
    <h1>My Vue App</h1>
  </div>
</template>

<script>
export default {
  name: 'App',
  mounted() {
    this.loadAIWidget();
  },
  beforeUnmount() {
    this.removeAIWidget();
  },
  methods: {
    loadAIWidget() {
      const script = document.createElement('script');
      script.src = 'https://cdn.yourdomain.com/ai-widget.js';
      script.dataset.apiBaseUrl = 'https://api.yourdomain.com';
      script.dataset.apiKey = process.env.VUE_APP_AI_API_KEY;
      script.dataset.tenantId = 'company_xyz';
      script.dataset.theme = 'light';
      
      document.body.appendChild(script);
    },
    removeAIWidget() {
      const script = document.querySelector('script[src*="ai-widget.js"]');
      if (script) document.body.removeChild(script);
      
      document.getElementById('ai-widget-root')?.remove();
      document.getElementById('ai-modal-root')?.remove();
    }
  }
}
</script>
```

### Example 5: Angular Application

```typescript
// app.component.ts
import { Component, OnInit, OnDestroy } from '@angular/core';
import { environment } from '../environments/environment';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  private widgetScript?: HTMLScriptElement;

  ngOnInit() {
    this.loadAIWidget();
  }

  ngOnDestroy() {
    this.removeAIWidget();
  }

  private loadAIWidget() {
    this.widgetScript = document.createElement('script');
    this.widgetScript.src = 'https://cdn.yourdomain.com/ai-widget.js';
    this.widgetScript.dataset.apiBaseUrl = 'https://api.yourdomain.com';
    this.widgetScript.dataset.apiKey = environment.aiApiKey;
    this.widgetScript.dataset.tenantId = 'company_xyz';
    this.widgetScript.dataset.theme = 'light';
    
    document.body.appendChild(this.widgetScript);
  }

  private removeAIWidget() {
    if (this.widgetScript) {
      document.body.removeChild(this.widgetScript);
    }
    
    document.getElementById('ai-widget-root')?.remove();
    document.getElementById('ai-modal-root')?.remove();
  }
}
```

### Example 6: WordPress/PHP Integration

```php
<?php
// In your theme's footer.php or functions.php

function add_ai_widget() {
  $api_key = get_option('ai_widget_api_key');
  $tenant_id = get_option('ai_widget_tenant_id');
  ?>
  <script 
    src="https://cdn.yourdomain.com/ai-widget.js"
    data-api-base-url="https://api.yourdomain.com"
    data-api-key="<?php echo esc_attr($api_key); ?>"
    data-tenant-id="<?php echo esc_attr($tenant_id); ?>"
    data-theme="light">
  </script>
  <?php
}

add_action('wp_footer', 'add_ai_widget');
?>
```

---

## 🎨 User Flow

### 1. Initial State

```
┌─────────────────────────┐
│   Client Website        │
│                         │
│                         │
│                  [💫]   │ ← Floating AI button
└─────────────────────────┘
```

### 2. Widget Expanded

```
┌─────────────────────────┐
│   Client Website        │
│                         │
│          ┌──────────────┤
│          │ AI Insights  │
│          │              │
│          │ 📊 Sales ↓   │
│          │ 💡 Insight   │
│          │              │
│          │ [Ask AI...]  │
│          │              │
│          │ [Explore ↗]  │ ← "Explore Deeper" button
│          └──────────────┤
└─────────────────────────┘
```

### 3. Modal Workspace (Fullscreen)

```
┌─────────────────────────────────────────────────────┐
│ AI Analytics Workspace                         [×]  │
├───────┬────────────────────────────────┬────────────┤
│       │                                │            │
│ 💬    │  Ask me anything...            │  ⚠️ Risks │
│ Chats │                                │            │
│       │  ┌──────────────────────────┐  │  💡 Ideas │
│ 📌    │  │ User: Sales trends?      │  │            │
│ Saved │  │                          │  │  ✓ Actions│
│       │  │ AI: Here's a chart...    │  │            │
│       │  │                          │  │            │
│ New   │  │ [Chart visualization]    │  │            │
│       │  └──────────────────────────┘  │            │
│       │                                │            │
└───────┴────────────────────────────────┴────────────┘
```

---

## 🔌 No Backend Changes Required

The widget works with your **existing APIs**:

```javascript
// Your existing API endpoints (no changes needed)
POST   /api/query/execute
GET    /api/conversations
GET    /api/conversations/{id}
POST   /api/conversations/{id}/messages
GET    /api/chart/{conversationId}
```

---

## 🎯 Features Unlocked

### Widget Features:
- ✅ Quick AI insights
- ✅ Mini query interface
- ✅ Suggested questions
- ✅ Compact result previews

### Modal Workspace Features:
- ✅ Full conversation history
- ✅ Saved analyses
- ✅ Rich visualizations (charts, tables, KPIs)
- ✅ Risks & opportunities panel
- ✅ Keyboard shortcuts
- ✅ Responsive design
- ✅ Dark mode support

---

## 🚫 Zero Conflicts

The widget uses **Shadow DOM** for complete isolation:

- ✅ Your CSS won't affect the widget
- ✅ Widget CSS won't affect your app
- ✅ No JavaScript namespace collisions
- ✅ No routing conflicts
- ✅ Works with ANY framework

---

## 📦 Bundle Sizes

| Bundle | Size (gzipped) | Loads When |
|--------|----------------|------------|
| Loader | ~5 KB | Page load (always) |
| Widget | ~50 KB | Page load (always) |
| Modal | ~200 KB | First "Explore" click (lazy) |

**Total initial load: ~55 KB**

---

## 🔧 Advanced Configuration

### Programmatic Control

```javascript
// Access widget API (after loaded)
window.AIWidget = {
  open: () => {
    // Open widget programmatically
    const event = new CustomEvent('aiWidgetOpen');
    window.dispatchEvent(event);
  },
  
  openModal: (query) => {
    // Open modal directly with a query
    const event = new CustomEvent('aiModalOpen', {
      detail: { query }
    });
    window.dispatchEvent(event);
  },
  
  close: () => {
    // Close widget
    const event = new CustomEvent('aiWidgetClose');
    window.dispatchEvent(event);
  }
};

// Usage:
// Button click opens AI modal directly
document.getElementById('ask-ai-btn').addEventListener('click', () => {
  window.AIWidget.openModal('Show me sales trends');
});
```

### Custom Styling

While the widget is isolated, you can customize positioning:

```html
<style>
  #ai-widget-root {
    /* Override position if needed */
    bottom: 20px !important;
    right: 20px !important;
  }
</style>
```

---

## 🐛 Troubleshooting

### Widget Not Appearing?

1. Check browser console for errors
2. Verify API URL is accessible
3. Ensure API key is valid
4. Check CORS settings on your API

### Modal Not Loading?

1. Check network tab for modal bundle loading
2. Verify no Content Security Policy (CSP) blocking scripts
3. Ensure browser supports Shadow DOM (Chrome 90+, Firefox 88+, Safari 14+)

### Styling Issues?

1. Shadow DOM provides complete isolation
2. If you see conflicts, check for `!important` rules in global CSS
3. Widget positioning can be adjusted via `data-position` attribute

---

## 📚 Next Steps

1. ✅ Add script tag to your website
2. ✅ Configure API credentials
3. ✅ Test widget functionality
4. ✅ Test modal workspace
5. ✅ Deploy to production

---

## 🆘 Support

- **Documentation**: https://docs.yourdomain.com/ai-widget
- **API Reference**: https://docs.yourdomain.com/api
- **Issues**: https://github.com/yourorg/ai-widget/issues
- **Email**: support@yourdomain.com

---

## 🎉 That's It!

**One script tag. Zero configuration. Complete AI analytics.**

Your clients get enterprise-grade AI insights without any development work.
