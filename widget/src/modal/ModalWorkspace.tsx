/**
 * Modal Workspace - Main Container
 * Fullscreen AI analytics workspace that opens from the widget
 */

import React, { useState, useEffect, useRef } from 'react';
import type { WidgetConfig } from '../types';
import type { ModalContext, ModalState, Conversation } from '../types/modal';
import StateManager from '../state/StateManager';
import ModalAPIService from '../services/modalApi';

// Components
import ModalHeader from './ModalHeader';
import Sidebar from './components/Sidebar/Sidebar';
import MainWorkspace from './components/MainWorkspace/MainWorkspace';
import RightPanel from './components/RightPanel/RightPanel';
import { useConversation } from './hooks/useConversation';
import { useKeyboardShortcuts } from './hooks/useKeyboardShortcuts';

interface ModalWorkspaceProps {
  config: WidgetConfig;
  context?: ModalContext;
}

const ModalWorkspace: React.FC<ModalWorkspaceProps> = ({ config, context: initialContext }) => {
  console.log('[ModalWorkspace] Component initializing with config:', config);
  console.log('[ModalWorkspace] Initial context:', initialContext);
  
  const [state, setState] = useState<ModalState>({
    isOpen: !!initialContext,
    isLoading: false,
    context: initialContext || null,
    currentConversation: null,
    sidebarCollapsed: false,
    rightPanelCollapsed: false,
    error: null,
  });

  // Token usage refresh trigger - increment to force refresh
  const [tokenRefreshTrigger, setTokenRefreshTrigger] = useState(0);
  
  // Server-side insights setting - fetched from tenant configuration
  const [insightsEnabled, setInsightsEnabled] = useState(true);

  const modalRef = useRef<HTMLDivElement>(null);
  const [apiService] = useState(() => new ModalAPIService(config));
  
  // Fetch tenant configuration on mount
  useEffect(() => {
    const fetchTenantConfig = async () => {
      try {
        const tenantInfo = await apiService.getTenantInfo();
        setInsightsEnabled(tenantInfo.enableInsights);
        console.log('[ModalWorkspace] Tenant insights setting:', tenantInfo.enableInsights);
        
        // If insights disabled, collapse panel immediately
        if (!tenantInfo.enableInsights) {
          setState(prev => ({ ...prev, rightPanelCollapsed: true }));
        }
      } catch (error) {
        console.error('[ModalWorkspace] Failed to fetch tenant config:', error);
        // Default to enabled on error
      }
    };
    
    fetchTenantConfig();
  }, [apiService]);
  
  // Log when component mounts
  useEffect(() => {
    console.log('[ModalWorkspace] Component mounted');
    console.log('[ModalWorkspace] Modal ref:', modalRef.current);
    
    // Check if styles are in shadow DOM
    const modalRoot = document.getElementById('ai-modal-root');
    if (modalRoot && modalRoot.shadowRoot) {
      console.log('[ModalWorkspace] Shadow root found');
      console.log('[ModalWorkspace] Shadow root children:', modalRoot.shadowRoot.children.length);
      console.log('[ModalWorkspace] Style tags:', modalRoot.shadowRoot.querySelectorAll('style').length);
      
      const styleTag = modalRoot.shadowRoot.querySelector('style');
      if (styleTag) {
        console.log('[ModalWorkspace] Style content length:', styleTag.textContent?.length);
        console.log('[ModalWorkspace] First 300 chars:', styleTag.textContent?.substring(0, 300));
      } else {
        console.error('[ModalWorkspace] No style tag found in shadow root!');
      }
    }
    
    return () => {
      console.log('[ModalWorkspace] Component unmounting');
    };
  }, []);

  // Conversation management
  const {
    conversation,
    messages,
    isLoading: isConversationLoading,
    error: conversationError,
    sendMessage,
    loadConversation,
    createConversation,
  } = useConversation(apiService);

  // Listen for modal open events
  useEffect(() => {
    console.log('[ModalWorkspace] ========== SETTING UP LISTENER ==========');
    console.log('[ModalWorkspace] Setting up modal:open listener');
    console.log('[ModalWorkspace] Current listener count before setup:', StateManager.getListenerCount('modal:open'));
    
    const unsubscribe = StateManager.on('modal:open', (event) => {
      const context = event.payload as ModalContext;
      console.log('[ModalWorkspace] ========== MODAL OPEN EVENT ==========');
      console.log('[ModalWorkspace] Event received:', event);
      console.log('[ModalWorkspace] Event type:', event.type);
      console.log('[ModalWorkspace] Event payload:', event.payload);
      console.log('[ModalWorkspace] Context:', context);
      console.log('[ModalWorkspace] Context.query:', context?.query);
      console.log('[ModalWorkspace] Context.config:', context?.config);
      console.log('[ModalWorkspace] Context.insights:', context?.insights);
      console.log('[ModalWorkspace] Context.queryResult:', context?.queryResult);
      console.log('[ModalWorkspace] =======================================');

      setState((prev) => {
        console.log('[ModalWorkspace] Previous state:', prev);
        const newState = {
          ...prev,
          isOpen: true,
          context,
        };
        console.log('[ModalWorkspace] New state:', newState);
        return newState;
      });

      // If context has a query, create conversation and send it
      if (context && context.query) {
        console.log('[ModalWorkspace] Context has query, handling initial query');
        handleInitialQuery(context);
      } else {
        console.log('[ModalWorkspace] No query in context');
      }
    });

    console.log('[ModalWorkspace] Listener registered successfully');
    console.log('[ModalWorkspace] Current listener count after setup:', StateManager.getListenerCount('modal:open'));
    console.log('[ModalWorkspace] ================================================');

    return () => {
      console.log('[ModalWorkspace] Cleaning up modal:open listener');
      unsubscribe();
    };
  }, []);

  // Handle initial query from widget
  const handleInitialQuery = async (context: ModalContext) => {
    console.log('[ModalWorkspace] handleInitialQuery called with context:', context);
    try {
      // Create new conversation
      console.log('[ModalWorkspace] Creating new conversation...');
      const newConversation = await createConversation();
      console.log('[ModalWorkspace] Conversation created:', newConversation);
      
      // Send the query if available - pass the conversation directly to avoid state timing issues
      if (context.query) {
        console.log('[ModalWorkspace] Sending initial query:', context.query);
        console.log('[ModalWorkspace] Using conversation:', newConversation.id);
        await sendMessage(context.query, newConversation);
        console.log('[ModalWorkspace] Query sent successfully');
        
        // Refresh token usage after query completes
        setTokenRefreshTrigger(prev => prev + 1);
      } else if (context.queryResult?.data?.result) {
        // If we have query result from widget, display it
        console.log('[ModalWorkspace] Displaying widget query result');
        // The query result is already in context, will be displayed
      }
    } catch (error) {
      console.error('[ModalWorkspace] Failed to handle initial query:', error);
      console.error('[ModalWorkspace] Error stack:', error instanceof Error ? error.stack : 'No stack');
      setState((prev) => ({
        ...prev,
        error: 'Failed to start conversation',
      }));
    }
  };

  // ==================== HANDLERS ====================

  const handleClose = () => {
    setState((prev) => ({ ...prev, isOpen: false }));
    StateManager.closeModal();

    // Notify widget of conversation update
    if (conversation) {
      StateManager.updateWidgetFromModal(
        conversation.id,
        messages[messages.length - 1]?.content || ''
      );
    }
  };

  const handleMinimize = () => {
    setState((prev) => ({ ...prev, isOpen: false }));
    StateManager.minimizeModal();
  };

  const handleFocusQuery = () => {
    const queryInput = modalRef.current?.querySelector<HTMLInputElement>('#modal-query-input');
    queryInput?.focus();
  };

  const handleToggleSidebar = () => {
    setState((prev) => ({
      ...prev,
      sidebarCollapsed: !prev.sidebarCollapsed,
    }));
  };

  const handleToggleRightPanel = () => {
    setState((prev) => ({
      ...prev,
      rightPanelCollapsed: !prev.rightPanelCollapsed,
    }));
  };

  // Wrapper for sendMessage that refreshes token usage after completion
  const handleSendMessageWithRefresh = async (query: string) => {
    try {
      await sendMessage(query);
      // Refresh token usage after query completes
      console.log('[ModalWorkspace] Query completed, refreshing token usage');
      setTokenRefreshTrigger(prev => prev + 1);
    } catch (error) {
      console.error('[ModalWorkspace] Error sending message:', error);
      // Don't refresh on error
      throw error;
    }
  };

  const handleBackdropClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      handleClose();
    }
  };

  const handleConversationSelect = async (conversationId: string) => {
    await loadConversation(conversationId);
  };

  const handleNewConversation = async () => {
    await createConversation();
  };

  // Keyboard shortcuts
  useKeyboardShortcuts({
    'Escape': handleClose,
    'k': handleFocusQuery,
    '/': handleToggleSidebar,
  });

  // Focus trap
  useEffect(() => {
    if (state.isOpen && modalRef.current) {
      const firstFocusable = modalRef.current.querySelector<HTMLElement>(
        'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
      );
      firstFocusable?.focus();
    }
  }, [state.isOpen]);

  // Lock body scroll when modal is open
  useEffect(() => {
    if (state.isOpen) {
      document.body.style.overflow = 'hidden';
      
      // Show modal container and enable pointer events
      const modalRoot = document.getElementById('ai-modal-root');
      if (modalRoot) {
        modalRoot.style.display = 'block';
        modalRoot.style.visibility = 'visible';
        modalRoot.style.pointerEvents = 'auto';
        console.log('[ModalWorkspace] Modal container shown');
      }
    } else {
      document.body.style.overflow = '';
      
      // Hide modal container and disable pointer events
      const modalRoot = document.getElementById('ai-modal-root');
      if (modalRoot) {
        modalRoot.style.display = 'none';
        modalRoot.style.visibility = 'hidden';
        modalRoot.style.pointerEvents = 'none';
        console.log('[ModalWorkspace] Modal container hidden');
      }
    }

    return () => {
      document.body.style.overflow = '';
    };
  }, [state.isOpen]);

  // ==================== RENDER ====================

  if (!state.isOpen) {
    console.log('[ModalWorkspace] Modal is closed, not rendering');
    return null;
  }

  console.log('[ModalWorkspace] Rendering modal, isOpen:', state.isOpen);

  return (
    <div
      className={`modal-backdrop theme-${config.theme}`}
      onClick={handleBackdropClick}
      role="dialog"
      aria-modal="true"
      aria-labelledby="modal-title"
    >
      <div
        ref={modalRef}
        className={`modal-workspace ${state.sidebarCollapsed ? 'sidebar-collapsed' : ''} ${
          state.rightPanelCollapsed ? 'right-panel-collapsed' : ''
        }`}
        onClick={(e) => e.stopPropagation()}
      >
        {/* Header */}
        <ModalHeader
          onClose={handleClose}
          onMinimize={handleMinimize}
          conversationTitle={conversation?.title}
        />

        {/* Main Layout */}
        <div className="modal-content">
          {/* Left Sidebar */}
          {!state.sidebarCollapsed && (
            <Sidebar
              apiService={apiService}
              currentConversationId={conversation?.id}
              onConversationSelect={handleConversationSelect}
              onNewConversation={handleNewConversation}
              onToggle={handleToggleSidebar}
              refreshTrigger={tokenRefreshTrigger}
            />
          )}

          {/* Main Workspace */}
          <MainWorkspace
            conversation={conversation}
            messages={messages}
            isLoading={isConversationLoading}
            error={conversationError}
            onSendMessage={handleSendMessageWithRefresh}
            sidebarCollapsed={state.sidebarCollapsed}
            onToggleSidebar={handleToggleSidebar}
            apiService={apiService}
          />

          {/* Right Panel - Only show if insights are enabled in tenant settings */}
          {insightsEnabled && !state.rightPanelCollapsed && conversation && (
            <RightPanel
              conversation={conversation}
              messages={messages}
              onToggle={handleToggleRightPanel}
            />
          )}
        </div>
      </div>
    </div>
  );
};

export default ModalWorkspace;
