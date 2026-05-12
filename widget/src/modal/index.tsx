/**
 * Modal Entry Point
 * Bootstraps the modal workspace inside Shadow DOM
 */

import React from 'react';
import ReactDOM from 'react-dom/client';
import ModalWorkspace from './ModalWorkspace';
import type { WidgetConfig } from '../types';
import type { ModalContext } from '../types/modal';
import StateManager from '../state/StateManager';
// @ts-ignore - raw-loader import for CSS as string
import modalStyles from '!!raw-loader!../styles/modal.css';

/**
 * Inject CSS into Shadow DOM
 */
function injectStyles(shadowRoot: ShadowRoot): void {
  console.log('[ModalWorkspace] injectStyles called');
  console.log('[ModalWorkspace] modalStyles:', modalStyles);
  console.log('[ModalWorkspace] modalStyles type:', typeof modalStyles);
  
  const styleElement = document.createElement('style');
  
  // raw-loader returns an object with default property
  let styles: string;
  if (typeof modalStyles === 'string') {
    styles = modalStyles;
  } else if (modalStyles && typeof modalStyles === 'object' && 'default' in modalStyles) {
    styles = (modalStyles as any).default;
  } else {
    console.error('[ModalWorkspace] Invalid styles format! modalStyles:', modalStyles);
    return;
  }
  
  if (!styles || styles.length === 0) {
    console.error('[ModalWorkspace] Empty styles! styles:', styles);
    return;
  }
  
  console.log('[ModalWorkspace] Injecting styles, length:', styles.length);
  console.log('[ModalWorkspace] First 200 chars:', styles.substring(0, 200));
  
  styleElement.textContent = styles;
  shadowRoot.appendChild(styleElement);
  console.log('[ModalWorkspace] Styles injected successfully');
}

/**
 * Mount modal workspace
 */
export function mountModal(shadowRoot: ShadowRoot, config: WidgetConfig, context?: ModalContext): void {
  console.log('[ModalWorkspace] Mounting modal...');
  console.log('[ModalWorkspace] shadowRoot:', shadowRoot);
  console.log('[ModalWorkspace] config:', config);

  try {
    // First, inject CSS styles before mounting React
    injectStyles(shadowRoot);

    // Find or create mount point
    let mountPoint = shadowRoot.querySelector('#modal-mount') as HTMLElement;
    if (!mountPoint) {
      console.log('[ModalWorkspace] Creating new modal-mount element');
      mountPoint = document.createElement('div');
      mountPoint.id = 'modal-mount';
      mountPoint.style.width = '100%';
      mountPoint.style.height = '100%';
      shadowRoot.appendChild(mountPoint);
    } else {
      console.log('[ModalWorkspace] Using existing modal-mount element');
    }

    console.log('[ModalWorkspace] Mount point:', mountPoint);
    console.log('[ModalWorkspace] Shadow root children:', shadowRoot.children.length);

    // Mount React app
    const root = ReactDOM.createRoot(mountPoint);
    root.render(
      <React.StrictMode>
        <ModalWorkspace config={config} context={context} />
      </React.StrictMode>
    );

    console.log('[ModalWorkspace] Modal mounted successfully');
    console.log('[ModalWorkspace] Shadow root HTML:', shadowRoot.innerHTML.substring(0, 500));
  } catch (error) {
    console.error('[ModalWorkspace] Failed to mount modal:', error);
    console.error('[ModalWorkspace] Error stack:', error instanceof Error ? error.stack : 'No stack');
  }
}

/**
 * Initialize modal listener
 * This sets up the event listener for modal open events
 */
export function initializeModal(shadowRoot: ShadowRoot, config: WidgetConfig): void {
  console.log('[ModalWorkspace] Initializing modal listener');

  // Mount modal immediately (it will be hidden by default)
  mountModal(shadowRoot, config);

  // Listen for modal open events
  StateManager.on('modal:open', (event) => {
    console.log('[ModalWorkspace] Modal open event received:', event);
    // Modal will handle its own visibility through state
  });
}

// Export for module bundler
export default {
  mountModal,
  initializeModal,
};
