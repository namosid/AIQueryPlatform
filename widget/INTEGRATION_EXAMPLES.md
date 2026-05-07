# Integration Examples

## 🚀 Universal Integration (Works Everywhere)

### Static HTML
```html
<!DOCTYPE html>
<html>
<head>
    <title>My Website</title>
</head>
<body>
    <h1>Welcome to my site</h1>
    
    <!-- Add widget at end of body -->
    <script src="https://cdn.yourdomain.com/ai-widget.js"
            data-api-base-url="https://api.yourdomain.com"
            data-api-key="your_api_key"
            data-tenant-id="your_tenant_id">
    </script>
</body>
</html>
```

---

## ⚛️ React Integration

### Option 1: Script in index.html (Recommended)
```html
<!-- public/index.html -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <title>React App</title>
</head>
<body>
    <div id="root"></div>
    
    <!-- AI Widget -->
    <script src="https://cdn.yourdomain.com/ai-widget.js"
            data-api-base-url="https://api.yourdomain.com"
            data-api-key="your_api_key"
            data-tenant-id="your_tenant_id"
            data-theme="light">
    </script>
</body>
</html>
```

### Option 2: Component with useEffect
```tsx
// components/AIWidget.tsx
import { useEffect } from 'react';

export const AIWidget = () => {
  useEffect(() => {
    const script = document.createElement('script');
    script.src = 'https://cdn.yourdomain.com/ai-widget.js';
    script.dataset.apiBaseUrl = 'https://api.yourdomain.com';
    script.dataset.apiKey = process.env.REACT_APP_AI_API_KEY!;
    script.dataset.tenantId = process.env.REACT_APP_TENANT_ID!;
    script.dataset.theme = 'light';
    script.async = true;

    document.body.appendChild(script);

    return () => {
      document.body.removeChild(script);
    };
  }, []);

  return null; // Widget renders itself
};

// App.tsx
import { AIWidget } from './components/AIWidget';

function App() {
  return (
    <div>
      <h1>My React App</h1>
      <AIWidget />
    </div>
  );
}
```

---

## 🅰️ Angular Integration

### Add to index.html
```html
<!-- src/index.html -->
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <title>Angular App</title>
  <base href="/">
</head>
<body>
  <app-root></app-root>
  
  <!-- AI Widget -->
  <script src="https://cdn.yourdomain.com/ai-widget.js"
          data-api-base-url="https://api.yourdomain.com"
          data-api-key="your_api_key"
          data-tenant-id="your_tenant_id">
  </script>
</body>
</html>
```

### Or create a service
```typescript
// ai-widget.service.ts
import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AIWidgetService {
  loadWidget(config: {
    apiBaseUrl: string;
    apiKey: string;
    tenantId: string;
    theme?: 'light' | 'dark';
  }): void {
    const script = document.createElement('script');
    script.src = 'https://cdn.yourdomain.com/ai-widget.js';
    script.dataset.apiBaseUrl = config.apiBaseUrl;
    script.dataset.apiKey = config.apiKey;
    script.dataset.tenantId = config.tenantId;
    script.dataset.theme = config.theme || 'light';
    script.async = true;

    document.body.appendChild(script);
  }
}

// app.component.ts
import { Component, OnInit } from '@angular/core';
import { AIWidgetService } from './services/ai-widget.service';

@Component({
  selector: 'app-root',
  template: '<h1>My Angular App</h1>'
})
export class AppComponent implements OnInit {
  constructor(private aiWidget: AIWidgetService) {}

  ngOnInit() {
    this.aiWidget.loadWidget({
      apiBaseUrl: 'https://api.yourdomain.com',
      apiKey: 'your_api_key',
      tenantId: 'your_tenant_id',
      theme: 'light'
    });
  }
}
```

---

## 🖖 Vue Integration

### Add to index.html
```html
<!-- public/index.html -->
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <title>Vue App</title>
</head>
<body>
    <div id="app"></div>
    
    <!-- AI Widget -->
    <script src="https://cdn.yourdomain.com/ai-widget.js"
            data-api-base-url="https://api.yourdomain.com"
            data-api-key="your_api_key"
            data-tenant-id="your_tenant_id">
    </script>
</body>
</html>
```

### Or use composition API
```vue
<!-- components/AIWidget.vue -->
<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue';

interface WidgetConfig {
  apiBaseUrl: string;
  apiKey: string;
  tenantId: string;
  theme?: 'light' | 'dark';
}

const props = defineProps<{ config: WidgetConfig }>();

let scriptElement: HTMLScriptElement | null = null;

onMounted(() => {
  scriptElement = document.createElement('script');
  scriptElement.src = 'https://cdn.yourdomain.com/ai-widget.js';
  scriptElement.dataset.apiBaseUrl = props.config.apiBaseUrl;
  scriptElement.dataset.apiKey = props.config.apiKey;
  scriptElement.dataset.tenantId = props.config.tenantId;
  scriptElement.dataset.theme = props.config.theme || 'light';
  scriptElement.async = true;

  document.body.appendChild(scriptElement);
});

onUnmounted(() => {
  if (scriptElement) {
    document.body.removeChild(scriptElement);
  }
});
</script>

<template>
  <!-- Widget renders itself -->
</template>

<!-- App.vue -->
<script setup lang="ts">
import AIWidget from './components/AIWidget.vue';

const widgetConfig = {
  apiBaseUrl: 'https://api.yourdomain.com',
  apiKey: import.meta.env.VITE_AI_API_KEY,
  tenantId: import.meta.env.VITE_TENANT_ID,
  theme: 'light' as const
};
</script>

<template>
  <div id="app">
    <h1>My Vue App</h1>
    <AIWidget :config="widgetConfig" />
  </div>
</template>
```

---

## 🎨 WordPress Integration

### Method 1: Theme Functions (Recommended)
```php
// functions.php
function add_ai_widget() {
    $api_key = get_option('ai_widget_api_key');
    $tenant_id = get_option('ai_widget_tenant_id');
    
    if (!$api_key || !$tenant_id) {
        return;
    }
    ?>
    <script src="https://cdn.yourdomain.com/ai-widget.js"
            data-api-base-url="https://api.yourdomain.com"
            data-api-key="<?php echo esc_attr($api_key); ?>"
            data-tenant-id="<?php echo esc_attr($tenant_id); ?>"
            data-theme="light">
    </script>
    <?php
}
add_action('wp_footer', 'add_ai_widget');
```

### Method 2: Plugin
```php
<?php
/**
 * Plugin Name: AI Query Widget
 * Description: Adds AI-powered insights widget to your site
 * Version: 1.0.0
 */

function ai_widget_settings() {
    add_menu_page(
        'AI Widget Settings',
        'AI Widget',
        'manage_options',
        'ai-widget-settings',
        'ai_widget_settings_page'
    );
}
add_action('admin_menu', 'ai_widget_settings');

function ai_widget_settings_page() {
    if (isset($_POST['ai_widget_submit'])) {
        update_option('ai_widget_api_key', sanitize_text_field($_POST['api_key']));
        update_option('ai_widget_tenant_id', sanitize_text_field($_POST['tenant_id']));
    }
    
    $api_key = get_option('ai_widget_api_key');
    $tenant_id = get_option('ai_widget_tenant_id');
    ?>
    <div class="wrap">
        <h1>AI Widget Settings</h1>
        <form method="post">
            <table class="form-table">
                <tr>
                    <th>API Key</th>
                    <td><input type="text" name="api_key" value="<?php echo esc_attr($api_key); ?>" class="regular-text"></td>
                </tr>
                <tr>
                    <th>Tenant ID</th>
                    <td><input type="text" name="tenant_id" value="<?php echo esc_attr($tenant_id); ?>" class="regular-text"></td>
                </tr>
            </table>
            <input type="submit" name="ai_widget_submit" class="button button-primary" value="Save">
        </form>
    </div>
    <?php
}

function ai_widget_enqueue() {
    $api_key = get_option('ai_widget_api_key');
    $tenant_id = get_option('ai_widget_tenant_id');
    
    if (!$api_key || !$tenant_id) return;
    
    wp_enqueue_script(
        'ai-widget',
        'https://cdn.yourdomain.com/ai-widget.js',
        [],
        '1.0.0',
        true
    );
    
    wp_script_add_data('ai-widget', 'data-api-base-url', 'https://api.yourdomain.com');
    wp_script_add_data('ai-widget', 'data-api-key', $api_key);
    wp_script_add_data('ai-widget', 'data-tenant-id', $tenant_id);
}
add_action('wp_enqueue_scripts', 'ai_widget_enqueue');
?>
```

---

## 🏗️ ASP.NET MVC Integration

### Add to _Layout.cshtml
```cshtml
<!DOCTYPE html>
<html>
<head>
    <meta charset="utf-8" />
    <title>@ViewData["Title"] - My ASP.NET App</title>
</head>
<body>
    @RenderBody()
    
    <!-- AI Widget -->
    <script src="https://cdn.yourdomain.com/ai-widget.js"
            data-api-base-url="https://api.yourdomain.com"
            data-api-key="@Configuration["AIWidget:ApiKey"]"
            data-tenant-id="@Configuration["AIWidget:TenantId"]"
            data-theme="light">
    </script>
</body>
</html>
```

### appsettings.json
```json
{
  "AIWidget": {
    "ApiKey": "your_api_key",
    "TenantId": "your_tenant_id"
  }
}
```

---

## 🔧 Advanced: Dynamic Configuration

### Load config from your backend API
```javascript
// Fetch widget config from your backend
fetch('/api/widget-config')
  .then(res => res.json())
  .then(config => {
    const script = document.createElement('script');
    script.src = 'https://cdn.yourdomain.com/ai-widget.js';
    script.dataset.apiBaseUrl = config.apiBaseUrl;
    script.dataset.apiKey = config.apiKey;
    script.dataset.tenantId = config.tenantId;
    script.dataset.userRole = config.userRole;
    script.dataset.theme = config.theme;
    script.async = true;
    
    document.body.appendChild(script);
  });
```

---

## 🎯 User-Specific Configuration

### Pass user context
```html
<script src="https://cdn.yourdomain.com/ai-widget.js"
        data-api-base-url="https://api.yourdomain.com"
        data-api-key="your_api_key"
        data-tenant-id="your_tenant_id"
        data-user-role="<%= current_user.role %>"
        data-theme="<%= current_user.theme_preference %>">
</script>
```

---

## 🚀 CDN Hosting

### Upload to your CDN
1. Build the widget: `npm run build`
2. Upload files to CDN:
   - `dist/ai-widget.js` → https://cdn.yourdomain.com/ai-widget.js
   - `dist/widget-app.js` → https://cdn.yourdomain.com/widget-app.js
   - `dist/vendors.js` → https://cdn.yourdomain.com/vendors.js

3. Update integration URL:
```html
<script src="https://cdn.yourdomain.com/ai-widget.js" ...></script>
```

### Version Management
```html
<!-- Versioned URL -->
<script src="https://cdn.yourdomain.com/v1.0.0/ai-widget.js" ...></script>

<!-- Latest version -->
<script src="https://cdn.yourdomain.com/latest/ai-widget.js" ...></script>
```

---

## 📊 Analytics Integration

### Track widget interactions
```javascript
// Listen for widget events
window.addEventListener('aiWidgetQuery', (event) => {
  // Track query in your analytics
  gtag('event', 'ai_widget_query', {
    query: event.detail.query
  });
});
```
