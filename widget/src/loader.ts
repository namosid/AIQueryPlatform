/**
 * AI Query Widget Loader
 * Lightweight script that bootstraps the widget from script tag attributes
 * Size target: < 5KB minified
 */

// Immediate execution check
(() => {
  console.log('[AIWidget] Loader module loaded - TOP LEVEL');
})();

console.log('[AIWidget] Loader module loaded');

interface WidgetConfig {
  apiBaseUrl: string;
  apiKey: string;
  tenantId: string;
  userRole: string;
  theme: 'light' | 'dark';
  position: 'bottom-right' | 'bottom-left' | 'top-right' | 'top-left';
  expandUrl?: string;
  autoOpen: boolean;
}

class AIWidgetLoader {
  private config: WidgetConfig | null = null;
  private shadowRoot: ShadowRoot | null = null;
  private widgetContainer: HTMLElement | null = null;

  constructor() {
    if (document.readyState === 'loading') {
      document.addEventListener('DOMContentLoaded', () => this.init());
    } else {
      this.init();
    }
  }

  private init(): void {
    try {
      console.log('[AIWidget] Initializing...');
      this.config = this.readConfig();
      console.log('[AIWidget] Config loaded:', this.config);
      this.validateConfig();
      console.log('[AIWidget] Config validated');
      this.createContainer();
      console.log('[AIWidget] Container created');
      this.loadWidget();
    } catch (error) {
      console.error('[AIWidget] Initialization failed:', error);
    }
  }

  private readConfig(): WidgetConfig {
    const script = document.currentScript as HTMLScriptElement || 
                   document.querySelector('script[data-api-key]') as HTMLScriptElement;

    if (!script) {
      throw new Error('Widget script not found');
    }

    return {
      apiBaseUrl: script.dataset.apiBaseUrl || '',
      apiKey: script.dataset.apiKey || '',
      tenantId: script.dataset.tenantId || '',
      userRole: script.dataset.userRole || 'User',
      theme: (script.dataset.theme as 'light' | 'dark') || 'light',
      position: (script.dataset.position as any) || 'bottom-right',
      expandUrl: script.dataset.expandUrl,
      autoOpen: script.dataset.autoOpen === 'true',
    };
  }

  private validateConfig(): void {
    if (!this.config) return;

    const required = ['apiBaseUrl', 'apiKey', 'tenantId'];
    for (const field of required) {
      if (!this.config[field as keyof WidgetConfig]) {
        throw new Error(`Missing required config: ${field}`);
      }
    }

    // Validate URL format
    try {
      new URL(this.config.apiBaseUrl);
    } catch {
      throw new Error('Invalid apiBaseUrl format');
    }
  }

  private createContainer(): void {
    // Check if user provided a root element
    let container = document.getElementById('ai-widget-root');

    if (!container) {
      // Create our own container
      container = document.createElement('div');
      container.id = 'ai-widget-root';
      container.style.cssText = `
        position: fixed;
        z-index: 999999;
        ${this.getPositionStyles()}
      `;
      document.body.appendChild(container);
    }

    // Create Shadow DOM for isolation
    this.shadowRoot = container.attachShadow({ mode: 'open' });
    
    // Create widget mount point inside shadow DOM
    this.widgetContainer = document.createElement('div');
    this.widgetContainer.id = 'widget-mount';
    this.shadowRoot.appendChild(this.widgetContainer);

    // Store config in container for widget access
    (container as any).__widgetConfig = this.config;
  }

  private getPositionStyles(): string {
    const position = this.config?.position || 'bottom-right';
    const spacing = '20px';

    switch (position) {
      case 'bottom-right':
        return `bottom: ${spacing}; right: ${spacing};`;
      case 'bottom-left':
        return `bottom: ${spacing}; left: ${spacing};`;
      case 'top-right':
        return `top: ${spacing}; right: ${spacing};`;
      case 'top-left':
        return `top: ${spacing}; left: ${spacing};`;
      default:
        return `bottom: ${spacing}; right: ${spacing};`;
    }
  }

  private loadWidget(): void {
    // Load vendors bundle first (React dependencies)
    const vendorsScript = document.createElement('script');
    const basePath = this.getBasePath();
    vendorsScript.src = `${basePath}vendors.js`;
    vendorsScript.async = true;
    vendorsScript.onload = () => {
      console.log('[AIWidget] Vendors loaded successfully');
      // Then load widget app bundle
      const widgetScript = document.createElement('script');
      widgetScript.src = `${basePath}widget-app.js`;
      widgetScript.async = true;
      widgetScript.onload = () => {
        console.log('[AIWidget] Widget app loaded successfully');
        this.mountWidget();
      };
      widgetScript.onerror = () => {
        console.error('[AIWidget] Failed to load widget bundle');
        this.showErrorFallback();
      };
      document.head.appendChild(widgetScript);
    };
    vendorsScript.onerror = () => {
      console.error('[AIWidget] Failed to load vendors bundle');
      this.showErrorFallback();
    };
    
    document.head.appendChild(vendorsScript);
  }

  private getBasePath(): string {
    const scriptSrc = (document.currentScript as HTMLScriptElement)?.src || '';
    return scriptSrc.substring(0, scriptSrc.lastIndexOf('/') + 1);
  }

  private getWidgetBundleUrl(): string {
    return `${this.getBasePath()}widget-app.js`;
  }

  private mountWidget(): void {
    if (!this.shadowRoot || !this.config) return;

    // Widget app will be loaded and will look for __widgetConfig
    const event = new CustomEvent('aiWidgetReady', {
      detail: {
        shadowRoot: this.shadowRoot,
        config: this.config,
      },
    });
    window.dispatchEvent(event);
  }

  private showErrorFallback(): void {
    if (!this.widgetContainer) return;

    this.widgetContainer.innerHTML = `
      <div style="
        background: #fff;
        border: 1px solid #e5e7eb;
        border-radius: 8px;
        padding: 16px;
        box-shadow: 0 4px 6px rgba(0,0,0,0.1);
        font-family: system-ui, -apple-system, sans-serif;
        font-size: 14px;
        color: #374151;
        max-width: 300px;
      ">
        <strong style="color: #ef4444;">AI Widget Error</strong>
        <p style="margin: 8px 0 0 0;">
          Failed to load widget. Please check your configuration.
        </p>
      </div>
    `;
  }
}

// Auto-initialize when script loads
if (typeof window !== 'undefined') {
  console.log('[AIWidget] Loader script executing...');
  try {
    const loader = new AIWidgetLoader();
    console.log('[AIWidget] Loader instance created:', loader);
    // Store on window for debugging
    (window as any).__AIWidgetLoader = loader;
  } catch (error) {
    console.error('[AIWidget] Failed to create loader instance:', error);
  }
} else {
  console.warn('[AIWidget] Window is undefined, cannot initialize');
}
