import React from 'react';
import ReactDOM from 'react-dom/client';
import App from './widget/App';
import type { WidgetConfig } from './types';
import widgetStyles from './styles/widget.css';

/**
 * Widget Entry Point
 * Listens for aiWidgetReady event from loader and mounts React app
 */

// Inject CSS into Shadow DOM
function injectStyles(shadowRoot: ShadowRoot): void {
  const styleElement = document.createElement('style');
  styleElement.textContent = widgetStyles.toString();
  shadowRoot.appendChild(styleElement);
}

// Wait for widget ready event from loader
window.addEventListener('aiWidgetReady', (event: any) => {
  console.log('[AIWidget] Received aiWidgetReady event', event.detail);
  const { shadowRoot, config } = event.detail;

  if (!shadowRoot || !config) {
    console.error('[AIWidget] Invalid initialization data');
    return;
  }

  try {
    // Inject CSS styles into Shadow DOM
    injectStyles(shadowRoot);
    console.log('[AIWidget] Styles injected into Shadow DOM');

    // Mount React app inside Shadow DOM
    const mountPoint = shadowRoot.querySelector('#widget-mount');
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
  } catch (error) {
    console.error('[AIWidget] Failed to mount React app:', error);
  }
});

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
