import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './widget/App';
import type { WidgetConfig } from './types';
// @ts-ignore - raw-loader import
import widgetStyles from '!!raw-loader!./styles/widget.css';
import StateManager from './state/StateManager';

/**
 * Widget Entry Point
 * Listens for aiWidgetReady event from loader and mounts React app
 * Also sets up modal lazy loading
 */

// Inject CSS into Shadow DOM
function injectStyles(shadowRoot: ShadowRoot): void {
  console.log('[AIWidget] injectStyles called');
  console.log('[AIWidget] widgetStyles type:', typeof widgetStyles);
  
  const styleElement = document.createElement('style');
  // raw-loader returns an object with default property
  const styles = typeof widgetStyles === 'string' ? widgetStyles : (widgetStyles as any).default;
  
  if (!styles) {
    console.error('[AIWidget] No styles found! widgetStyles:', widgetStyles);
    return;
  }
  
  styleElement.textContent = styles;
  shadowRoot.appendChild(styleElement);
  console.log('[AIWidget] Styles injected, length:', styles.length);
}

// Wait for widget ready event from loader
window.addEventListener('aiWidgetReady', (event: any) => {
  console.log('[AIWidget] Received aiWidgetReady event', event.detail);
  const { widgetShadowRoot, modalShadowRoot, shadowRoot, config } = event.detail;

  // Support both old and new loader format
  const actualWidgetShadow = widgetShadowRoot || shadowRoot;
  
  if (!actualWidgetShadow || !config) {
    console.error('[AIWidget] Invalid initialization data');
    return;
  }

  try {
    // Initialize StateManager with config
    StateManager.setConfig(config);

    // Inject CSS styles into widget Shadow DOM
    injectStyles(actualWidgetShadow);
    console.log('[AIWidget] Styles injected into Shadow DOM');

    // Mount React app inside widget Shadow DOM
    const mountPoint = actualWidgetShadow.querySelector('#widget-mount');
    if (!mountPoint) {
      console.error('[AIWidget] Mount point not found');
      return;
    }

    console.log('[AIWidget] Mounting React app...');
    const root = ReactDOM.createRoot(mountPoint);
    root.render(
      <React.StrictMode>
        <App config={config as WidgetConfig} />
      </React.StrictMode>
    );

    console.log('[AIWidget] React app mounted successfully');

    // Setup modal lazy loading
    setupModalLazyLoad(modalShadowRoot, config);
  } catch (error) {
    console.error('[AIWidget] Failed to mount React app:', error);
  }
});

/**
 * Setup modal lazy loading
 * Modal bundle only loads when "Explore Deeper" is clicked
 */
function setupModalLazyLoad(modalShadowRoot: ShadowRoot | undefined, config: WidgetConfig): void {
  if (!modalShadowRoot) {
    console.warn('[AIWidget] Modal shadow root not available, modal will not be available');
    return;
  }

  let modalLoaded = false;
  let pendingModalContext: any = null;

  // Listen for modal open requests
  StateManager.on('modal:open', async (event) => {
    console.log('[AIWidget] Modal open requested, modalLoaded:', modalLoaded);
    
    if (!modalLoaded) {
      console.log('[AIWidget] Loading modal workspace bundle...');
      
      // Store the context to open after load
      pendingModalContext = event.payload;
      
      try {
        // Dynamically import modal module (code-split by webpack)
        const modalModule = await import('./modal/index');
        
        // Initialize modal
        modalModule.initializeModal(modalShadowRoot, config);
        
        modalLoaded = true;
        console.log('[AIWidget] Modal workspace loaded successfully');
        
        // Now emit the modal open event again to actually open it
        if (pendingModalContext) {
          console.log('[AIWidget] Re-emitting modal:open after load with context:', pendingModalContext);
          console.log('[AIWidget] pendingModalContext.query:', pendingModalContext.query);
          console.log('[AIWidget] pendingModalContext.config:', pendingModalContext.config);
          console.log('[AIWidget] Waiting 300ms for modal to fully initialize...');
          setTimeout(() => {
            console.log('[AIWidget] Now emitting modal:open event');
            console.log('[AIWidget] Active listeners for modal:open:', StateManager.getListenerCount('modal:open'));
            StateManager.emit('modal:open', pendingModalContext);
            pendingModalContext = null;
          }, 300);
        }
      } catch (error) {
        console.error('[AIWidget] Failed to load modal:', error);
        pendingModalContext = null;
      }
    }
  });

  console.log('[AIWidget] Modal lazy loading configured');
}

// Also support direct mount for development
if (typeof window !== 'undefined' && (window as any).__AI_WIDGET_DEV_MODE) {
  const container = document.getElementById('ai-widget-root');
  if (container) {
    const config = (container as any).__widgetConfig;
    if (config) {
      const root = ReactDOM.createRoot(container);
      root.render(
        <React.StrictMode>
          <App config={config} />
        </React.StrictMode>
      );
    }
  }
}

export default App;
