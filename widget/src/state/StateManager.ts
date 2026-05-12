/**
 * State Manager - Event Bus for Widget ↔ Modal Communication
 * Manages state transfer and synchronization between widget and modal workspace
 */

import type { ModalContext, ModalEvent, ModalEventType, Conversation } from '../types/modal';
import type { WidgetConfig, QueryResponse, AIInsight } from '../types';

type EventCallback = (event: ModalEvent) => void;

class StateManager {
  private static instance: StateManager;
  private listeners: Map<ModalEventType, Set<EventCallback>> = new Map();
  private modalContext: ModalContext | null = null;
  private config: WidgetConfig | null = null;

  private constructor() {
    console.log('[StateManager] Initialized');
  }

  static getInstance(): StateManager {
    if (!StateManager.instance) {
      StateManager.instance = new StateManager();
    }
    return StateManager.instance;
  }

  // ==================== CONFIGURATION ====================

  setConfig(config: WidgetConfig): void {
    this.config = config;
    console.log('[StateManager] Config set:', config);
  }

  getConfig(): WidgetConfig | null {
    return this.config;
  }

  // ==================== EVENT BUS ====================

  on(eventType: ModalEventType, callback: EventCallback): () => void {
    if (!this.listeners.has(eventType)) {
      this.listeners.set(eventType, new Set());
    }
    this.listeners.get(eventType)!.add(callback);

    console.log(`[StateManager] Listener added for: ${eventType}`);

    // Return unsubscribe function
    return () => {
      this.listeners.get(eventType)?.delete(callback);
    };
  }

  emit<T = any>(eventType: ModalEventType, payload: T): void {
    const event: ModalEvent<T> = {
      type: eventType,
      payload,
      timestamp: new Date().toISOString(),
    };

    console.log(`[StateManager] Emitting event: ${eventType}`, payload);

    const callbacks = this.listeners.get(eventType);
    if (callbacks) {
      callbacks.forEach((callback) => {
        try {
          callback(event);
        } catch (error) {
          console.error(`[StateManager] Error in listener for ${eventType}:`, error);
        }
      });
    }
  }

  // ==================== MODAL CONTROL ====================

  openModal(context: Partial<ModalContext>): void {
    console.log('[StateManager] ========== OPEN MODAL ==========');
    console.log('[StateManager] Input context:', context);
    console.log('[StateManager] context.query:', context.query);
    console.log('[StateManager] context.insights:', context.insights);
    console.log('[StateManager] context.queryResult:', context.queryResult);
    
    if (!this.config) {
      console.error('[StateManager] Cannot open modal: config not set');
      return;
    }

    this.modalContext = {
      ...context,
      config: this.config,
      openedAt: new Date().toISOString(),
      source: context.conversationId ? 'widget' : 'direct',
    } as ModalContext;

    console.log('[StateManager] Built modalContext:', this.modalContext);
    console.log('[StateManager] modalContext.query:', this.modalContext.query);
    console.log('[StateManager] modalContext.config:', this.modalContext.config);
    console.log('[StateManager] Current listener count:', this.getListenerCount('modal:open'));
    console.log('[StateManager] Emitting modal:open event...');
    console.log('[StateManager] ===================================');

    this.emit('modal:open', this.modalContext);
  }

  closeModal(): void {
    console.log('[StateManager] Closing modal');
    this.emit('modal:close', {});
    this.modalContext = null;
  }

  minimizeModal(): void {
    console.log('[StateManager] Minimizing modal');
    this.emit('modal:minimize', {});
  }

  getModalContext(): ModalContext | null {
    return this.modalContext;
  }

  // ==================== WIDGET ↔ MODAL SYNC ====================

  /**
   * Transfer query from widget to modal
   */
  transferQueryToModal(query: string, result?: QueryResponse, insights?: AIInsight[]): void {
    console.log('[StateManager] Transferring query to modal:', query);

    const conversationId = this.generateId();

    this.openModal({
      conversationId,
      query,
      queryResult: result,
      insights,
    });
  }

  /**
   * Update widget with conversation changes from modal
   */
  updateWidgetFromModal(conversationId: string, lastQuery: string): void {
    console.log('[StateManager] Updating widget from modal:', { conversationId, lastQuery });

    this.emit('widget:update', {
      conversationId,
      lastQuery,
      timestamp: new Date().toISOString(),
    });
  }

  /**
   * Notify about conversation updates
   */
  notifyConversationUpdate(conversation: Conversation): void {
    console.log('[StateManager] Conversation updated:', conversation.id);

    this.emit('conversation:update', conversation);
  }

  /**
   * Notify about new conversation
   */
  notifyNewConversation(conversation: Conversation): void {
    console.log('[StateManager] New conversation created:', conversation.id);

    this.emit('conversation:new', conversation);
  }

  // ==================== QUERY EXECUTION ====================

  /**
   * Notify query execution started
   */
  notifyQueryExecute(query: string, conversationId: string): void {
    console.log('[StateManager] Query execution started:', query);

    this.emit('query:execute', {
      query,
      conversationId,
      timestamp: new Date().toISOString(),
    });
  }

  /**
   * Notify query execution complete
   */
  notifyQueryComplete(query: string, result: QueryResponse): void {
    console.log('[StateManager] Query execution complete:', query);

    this.emit('query:complete', {
      query,
      result,
      timestamp: new Date().toISOString(),
    });
  }

  // ==================== UTILITIES ====================

  private generateId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  /**
   * Clear all listeners (useful for cleanup)
   */
  clearAllListeners(): void {
    this.listeners.clear();
    console.log('[StateManager] All listeners cleared');
  }

  /**
   * Get listener count for debugging
   */
  getListenerCount(eventType?: ModalEventType): number {
    if (eventType) {
      return this.listeners.get(eventType)?.size || 0;
    }
    let total = 0;
    this.listeners.forEach((callbacks) => {
      total += callbacks.size;
    });
    return total;
  }
}

// Export singleton instance
export default StateManager.getInstance();
