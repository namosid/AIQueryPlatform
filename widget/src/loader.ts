/**
 * AI Query Widget Loader
 * Lightweight script that bootstraps the widget and modal from script tag attributes
 * Size target: < 5KB minified
 * 
 * Architecture:
 * 1. Loader (this file) - reads config, creates containers
 * 2. Widget bundle - loads immediately
 * 3. Modal bundle - lazy loads on first "Explore" click
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
  private widgetShadowRoot: ShadowRoot | null = null;
  private modalShadowRoot: ShadowRoot | null = null;
  private widgetContainer: HTMLElement | null = null;
  private modalContainer: HTMLElement | null = null;
  private modalLoaded = false;

  constructor() {
    console.log('[AIWidget] Constructor called, document.readyState:', document.readyState);
    if (document.readyState === 'loading') {
      console.log('[AIWidget] Document still loading, waiting for DOMContentLoaded...');
      document.addEventListener('DOMContentLoaded', () => {
        console.log('[AIWidget] DOMContentLoaded fired');
        this.init();
      });
    } else {
      console.log('[AIWidget] Document already loaded, initializing immediately');
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
      console.log('[AIWidget] Container created, widgetShadowRoot:', this.widgetShadowRoot);
      console.log('[AIWidget] widgetContainer:', this.widgetContainer);
      this.loadWidget();
      console.log('[AIWidget] loadWidget called');
    } catch (error) {
      console.error('[AIWidget] Initialization failed:', error);
      console.error('[AIWidget] Error stack:', error instanceof Error ? error.stack : 'No stack trace');
    }
  }

  private readConfig(): WidgetConfig {
    console.log('[AIWidget] Reading config from script tag...');
    console.log('[AIWidget] document.currentScript:', document.currentScript);
    
    const script = document.currentScript as HTMLScriptElement || 
                   document.querySelector('script[data-api-key]') as HTMLScriptElement;

    console.log('[AIWidget] Found script element:', script);
    
    if (!script) {
      throw new Error('Widget script not found');
    }

    const config = {
      apiBaseUrl: script.dataset.apiBaseUrl || '',
      apiKey: script.dataset.apiKey || '',
      tenantId: script.dataset.tenantId || '',
      userRole: script.dataset.userRole || 'User',
      theme: (script.dataset.theme as 'light' | 'dark') || 'light',
      position: (script.dataset.position as any) || 'bottom-right',
      expandUrl: script.dataset.expandUrl,
      autoOpen: script.dataset.autoOpen === 'true',
    };
    
    console.log('[AIWidget] Parsed config:', config);
    return config;
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

    // Create Shadow DOM for widget
    this.widgetShadowRoot = container.attachShadow({ mode: 'open' });
    
    // Create widget mount point inside shadow DOM
    this.widgetContainer = document.createElement('div');
    this.widgetContainer.id = 'widget-mount';
    this.widgetShadowRoot.appendChild(this.widgetContainer);

    // Create modal container with higher z-index than widget
    const modalContainer = document.createElement('div');
    modalContainer.id = 'ai-modal-root';
    modalContainer.style.cssText = `
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      z-index: 9999999;
      pointer-events: none;
      display: none;
      visibility: hidden;
    `;
    this.modalContainer = modalContainer;
    this.modalShadowRoot = modalContainer.attachShadow({ mode: 'open' });
    
    const modalMount = document.createElement('div');
    modalMount.id = 'modal-mount';
    this.modalShadowRoot.appendChild(modalMount);
    
    document.body.appendChild(modalContainer);

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
    const basePath = this.getBasePath();

    // Load dependencies in order: runtime → vendors → widget-app
    // Note: In dev mode, these are served from memory by webpack-dev-server
    this.loadScript(`${basePath}runtime.js`)
      .then(() => {
        console.log('[AIWidget] Runtime loaded');
        return this.loadScript(`${basePath}vendors.js`);
      })
      .then(() => {
        console.log('[AIWidget] Vendors loaded');
        return this.loadScript(`${basePath}widget-app.js`);
      })
      .then(() => {
        console.log('[AIWidget] Widget app loaded, dispatching ready event');
        // Emit ready event with both shadow roots
        const event = new CustomEvent('aiWidgetReady', {
          detail: {
            widgetShadowRoot: this.widgetShadowRoot,
            modalShadowRoot: this.modalShadowRoot,
            config: this.config,
          },
        });
        window.dispatchEvent(event);
      })
      .catch((error) => {
        console.error('[AIWidget] Failed to load widget:', error);
        this.showErrorFallback();
      });
  }

  private loadScript(src: string): Promise<void> {
    return new Promise((resolve, reject) => {
      // Check if script already loaded
      const existing = document.querySelector(`script[src="${src}"]`);
      if (existing) {
        resolve();
        return;
      }

      const script = document.createElement('script');
      script.src = src;
      script.async = false; // Load in order
      script.onload = () => resolve();
      script.onerror = () => reject(new Error(`Failed to load ${src}`));
      document.head.appendChild(script);
    });
  }

  private getBasePath(): string {
    const scriptSrc = (document.currentScript as HTMLScriptElement)?.src || '';
    return scriptSrc.substring(0, scriptSrc.lastIndexOf('/') + 1);
  }

  private getWidgetBundleUrl(): string {
    return `${this.getBasePath()}widget-app.js`;
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
