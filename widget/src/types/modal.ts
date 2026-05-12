/**
 * Modal Workspace Type Definitions
 * Extended types for fullscreen AI analytics workspace
 */

import type { WidgetConfig, AIInsight, QueryResponse, ChartData } from './index';

// ==================== MODAL STATE ====================

export interface ModalContext {
  // Transfer from widget
  conversationId?: string;
  query?: string;
  insights?: AIInsight[];
  queryResult?: QueryResponse;
  
  // Configuration
  config: WidgetConfig;
  
  // Modal specific
  openedAt: string;
  source: 'widget' | 'direct';
}

export interface ModalState {
  isOpen: boolean;
  isLoading: boolean;
  context: ModalContext | null;
  
  // Current conversation
  currentConversation: Conversation | null;
  
  // UI state
  sidebarCollapsed: boolean;
  rightPanelCollapsed: boolean;
  error: string | null;
}

// ==================== CONVERSATION ====================

export interface Message {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  timestamp: string;
  
  // Query-specific
  query?: string;
  sql?: string;
  
  // Response-specific
  data?: any;
  visualization?: VisualizationData;
  insights?: AIInsight[];
  recommendations?: Recommendation[];
  
  // Metadata
  metadata?: {
    executionTime?: number;
    rowCount?: number;
    confidence?: number;
  };
}

export interface Conversation {
  id: string;
  title: string;
  messages: Message[];
  createdAt: string;
  updatedAt: string;
  
  // Metadata
  tags?: string[];
  pinned?: boolean;
  favorite?: boolean;
  
  // Summary
  summary?: string;
  keyInsights?: string[];
}

// ==================== VISUALIZATION ====================

export interface VisualizationData {
  type: 'table' | 'chart' | 'kpi' | 'timeline' | 'mixed';
  
  // Table
  table?: TableData;
  
  // Chart
  chart?: ChartData;
  
  // KPI Cards
  kpis?: KPICard[];
  
  // Timeline
  timeline?: TimelineData;
  
  // Multiple visualizations
  components?: VisualizationComponent[];
}

export interface TableData {
  columns: TableColumn[];
  rows: any[];
  totalRows?: number;
  page?: number;
  pageSize?: number;
}

export interface TableColumn {
  key: string;
  label: string;
  type: 'string' | 'number' | 'date' | 'currency' | 'percentage';
  sortable?: boolean;
  width?: number;
}

export interface KPICard {
  id: string;
  title: string;
  value: string | number;
  unit?: string;
  change?: {
    value: number;
    direction: 'up' | 'down' | 'neutral';
    period: string;
  };
  trend?: number[];
  color?: string;
}

export interface TimelineData {
  events: TimelineEvent[];
  startDate: string;
  endDate: string;
}

export interface TimelineEvent {
  id: string;
  date: string;
  title: string;
  description: string;
  type: 'milestone' | 'event' | 'alert';
  impact?: 'positive' | 'negative' | 'neutral';
}

export interface VisualizationComponent {
  id: string;
  type: 'table' | 'chart' | 'kpi';
  title: string;
  data: TableData | ChartData | KPICard[];
  layout?: {
    col: number;
    row: number;
    width: number;
    height: number;
  };
}

// ==================== RECOMMENDATIONS ====================

export interface Recommendation {
  id: string;
  type: 'risk' | 'opportunity' | 'action';
  title: string;
  description: string;
  priority: 'high' | 'medium' | 'low';
  impact?: string;
  effort?: 'low' | 'medium' | 'high';
  category?: string;
  actions?: RecommendedAction[];
}

export interface RecommendedAction {
  id: string;
  label: string;
  query?: string; // Follow-up query to execute
  url?: string; // External link
  type: 'query' | 'link' | 'export';
}

// ==================== SAVED ANALYSES ====================

export interface SavedAnalysis {
  id: string;
  title: string;
  description?: string;
  conversationId: string;
  savedAt: string;
  
  // Preview
  thumbnail?: string;
  preview?: {
    queryCount: number;
    keyInsights: string[];
    charts: number;
  };
  
  // Organization
  tags?: string[];
  folder?: string;
}

// ==================== SUGGESTED QUERIES ====================

export interface SuggestedQueryCategory {
  id: string;
  title: string;
  icon: string;
  queries: SuggestedQueryItem[];
}

export interface SuggestedQueryItem {
  id: string;
  text: string;
  description?: string;
  category: string;
}

// ==================== API RESPONSES ====================

export interface ConversationListResponse {
  conversations: Conversation[];
  total: number;
  page: number;
  pageSize: number;
}

export interface SavedAnalysisListResponse {
  analyses: SavedAnalysis[];
  total: number;
  folders?: string[];
}

// ==================== CONVERSATION QUERY RESPONSE ====================

export interface ConversationQueryResponse {
  userMessage: Message;
  assistantMessage: Message;
  queryResult: {
    success: boolean;
    generatedSql?: string;
    result?: {
      columns: string[];
      rows: any[];
      rowCount: number;
    };
    visualizationType?: 'Table' | 'Chart' | 'PDF';
    chartData?: ChartData;
    executionTimeMs?: number;
    errorMessage?: string;
  };
}

// ==================== MODAL EVENTS ====================

export type ModalEventType =
  | 'modal:open'
  | 'modal:close'
  | 'modal:minimize'
  | 'conversation:update'
  | 'conversation:new'
  | 'query:execute'
  | 'query:complete'
  | 'widget:update';

export interface ModalEvent<T = any> {
  type: ModalEventType;
  payload: T;
  timestamp: string;
}

// ==================== HOOKS ====================

export interface UseConversationResult {
  conversation: Conversation | null;
  messages: Message[];
  isLoading: boolean;
  error: string | null;
  
  sendMessage: (query: string, conversationOverride?: Conversation) => Promise<void>;
  loadConversation: (id: string) => Promise<void>;
  createConversation: () => Promise<Conversation>;
  clearConversation: () => void;
}

export interface UseConversationHistoryResult {
  conversations: Conversation[];
  isLoading: boolean;
  error: string | null;
  
  loadConversations: () => Promise<void>;
  deleteConversation: (id: string) => Promise<void>;
  pinConversation: (id: string) => Promise<void>;
}

export interface UseSavedAnalysesResult {
  analyses: SavedAnalysis[];
  isLoading: boolean;
  error: string | null;
  
  loadAnalyses: () => Promise<void>;
  saveAnalysis: (conversationId: string, title: string) => Promise<void>;
  deleteAnalysis: (id: string) => Promise<void>;
}

// ==================== KEYBOARD SHORTCUTS ====================

export interface KeyboardShortcut {
  key: string;
  ctrlKey?: boolean;
  shiftKey?: boolean;
  metaKey?: boolean;
  description: string;
  handler: () => void;
}
