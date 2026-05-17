/**
 * Modal API Service
 * Extended API service for modal workspace features
 * Builds on top of the base APIService
 */

import type { WidgetConfig } from '../types';
import type {
  Conversation,
  Message,
  SavedAnalysis,
  ConversationListResponse,
  ConversationQueryResponse,
  SavedAnalysisListResponse,
  SuggestedQueryCategory,
} from '../types/modal';
import APIService from './api';

export class ModalAPIService extends APIService {
  constructor(config: WidgetConfig) {
    super(config);
  }

  // ==================== CONVERSATIONS ====================

  /**
   * List all conversations for the tenant
   */
  async listConversations(page: number = 1, pageSize: number = 20): Promise<ConversationListResponse> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/conversations?page=${page}&pageSize=${pageSize}`,
        {
          method: 'GET',
          headers: this.getHeaders(),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error('[ModalAPIService] Failed to list conversations:', error);
      // Return empty list on error
      return {
        conversations: [],
        total: 0,
        page: 1,
        pageSize: 20,
      };
    }
  }

  /**
   * Load a specific conversation by ID
   */
  async loadConversation(id: string): Promise<Conversation | null> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/conversations/${id}`,
        {
          method: 'GET',
          headers: this.getHeaders(),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const conversation = await response.json();
      return conversation;
    } catch (error) {
      console.error('[ModalAPIService] Failed to load conversation:', error);
      return null;
    }
  }

  /**
   * Create a new conversation
   */
  async createConversation(title?: string): Promise<Conversation> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/conversations`,
        {
          method: 'POST',
          headers: this.getHeaders(),
          body: JSON.stringify({
            title: title || 'New Conversation',
            tenantId: this.config.tenantId,
          }),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const conversation = await response.json();
      return conversation;
    } catch (error) {
      console.error('[ModalAPIService] Failed to create conversation:', error);
      // Return a client-side conversation as fallback
      return {
        id: this.generateId(),
        title: title || 'New Conversation',
        messages: [],
        createdAt: new Date().toISOString(),
        updatedAt: new Date().toISOString(),
      };
    }
  }

  /**
   * Add a message to a conversation
   */
  async addMessage(conversationId: string, message: Omit<Message, 'id' | 'timestamp'>): Promise<Message> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/conversations/${conversationId}/messages`,
        {
          method: 'POST',
          headers: this.getHeaders(),
          body: JSON.stringify(message),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const savedMessage = await response.json();
      return savedMessage;
    } catch (error) {
      console.error('[ModalAPIService] Failed to add message:', error);
      // Return client-side message as fallback
      return {
        ...message,
        id: this.generateId(),
        timestamp: new Date().toISOString(),
      } as Message;
    }
  }

  /**
   * Execute query within a conversation (unified endpoint)
   * This handles: user message save, LLM execution, AI response save
   * Returns: both messages and complete query results
   */
  async executeQueryInConversation(conversationId: string, query: string): Promise<ConversationQueryResponse> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/conversations/${conversationId}/query`,
        {
          method: 'POST',
          headers: this.getHeaders(),
          body: JSON.stringify({ query }),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const result = await response.json();
      console.log('[ModalAPIService] Query executed in conversation:', result);
      return result;
    } catch (error) {
      console.error('[ModalAPIService] Failed to execute query in conversation:', error);
      throw error;
    }
  }

  /**
   * Generate PDF report from query results
   */
  async generatePDF(query: string, sql: string, result: any, chartData?: any): Promise<Blob> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/query/generate-report-from-data`,
        {
          method: 'POST',
          headers: this.getHeaders(),
          body: JSON.stringify({
            query,
            generatedSql: sql,
            result,
            chartData: chartData || null,
            reportTitle: 'Query Results',
          }),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const blob = await response.blob();
      console.log('[ModalAPIService] PDF generated successfully');
      return blob;
    } catch (error) {
      console.error('[ModalAPIService] Failed to generate PDF:', error);
      throw error;
    }
  }

  /**
   * Delete a conversation
   */
  async deleteConversation(id: string): Promise<boolean> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/conversations/${id}`,
        {
          method: 'DELETE',
          headers: this.getHeaders(),
        }
      );

      return response.ok;
    } catch (error) {
      console.error('[ModalAPIService] Failed to delete conversation:', error);
      return false;
    }
  }

  /**
   * Pin/unpin a conversation
   */
  async pinConversation(id: string, pinned: boolean): Promise<boolean> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/conversations/${id}/pin`,
        {
          method: 'PATCH',
          headers: this.getHeaders(),
          body: JSON.stringify({ pinned }),
        }
      );

      return response.ok;
    } catch (error) {
      console.error('[ModalAPIService] Failed to pin conversation:', error);
      return false;
    }
  }

  // ==================== SAVED ANALYSES ====================

  /**
   * List saved analyses
   */
  async listSavedAnalyses(): Promise<SavedAnalysisListResponse> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/analyses`,
        {
          method: 'GET',
          headers: this.getHeaders(),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const data = await response.json();
      return data;
    } catch (error) {
      console.error('[ModalAPIService] Failed to list analyses:', error);
      return {
        analyses: [],
        total: 0,
      };
    }
  }

  /**
   * Save an analysis
   */
  async saveAnalysis(conversationId: string, title: string, description?: string): Promise<SavedAnalysis> {
    try {
      console.log('[ModalAPIService] Saving analysis:', { conversationId, title, description });
      // .NET expects PascalCase property names
      const requestBody = {
        ConversationId: conversationId,
        Title: title,
        Description: description || null,
      };
      console.log('[ModalAPIService] Request body:', requestBody);

      const response = await fetch(
        `${this.config.apiBaseUrl}/api/analyses`,
        {
          method: 'POST',
          headers: this.getHeaders(),
          body: JSON.stringify(requestBody),
        }
      );

      console.log('[ModalAPIService] Save response status:', response.status, response.statusText);

      if (!response.ok) {
        const errorText = await response.text();
        console.error('[ModalAPIService] Save failed with error:', errorText);
        throw new Error(`HTTP ${response.status}: ${errorText}`);
      }

      const analysis = await response.json();
      console.log('[ModalAPIService] Analysis saved successfully:', analysis);
      return analysis;
    } catch (error) {
      console.error('[ModalAPIService] Failed to save analysis:', error);
      throw error;
    }
  }

  /**
   * Delete an analysis
   */
  async deleteAnalysis(id: string): Promise<boolean> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/analyses/${id}`,
        {
          method: 'DELETE',
          headers: this.getHeaders(),
        }
      );

      return response.ok;
    } catch (error) {
      console.error('[ModalAPIService] Failed to delete analysis:', error);
      return false;
    }
  }

  // ==================== SUGGESTED QUERIES ====================

  /**
   * Get suggested queries
   */
  async getSuggestedQueries(): Promise<SuggestedQueryCategory[]> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/queries/suggested`,
        {
          method: 'GET',
          headers: this.getHeaders(),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const queries = await response.json();
      return queries;
    } catch (error) {
      console.error('[ModalAPIService] Failed to get suggested queries:', error);
      // Return default suggestions
      return this.getDefaultSuggestedQueries();
    }
  }

  // ==================== TOKEN USAGE ====================

  /**
   * Get token usage for the current tenant
   */
  async getTokenUsage(): Promise<any> {
    try {
      const response = await fetch(
        `${this.config.apiBaseUrl}/api/tokenusage`,
        {
          method: 'GET',
          headers: this.getHeaders(),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const usage = await response.json();
      return usage;
    } catch (error) {
      console.error('[ModalAPIService] Failed to get token usage:', error);
      // Return default/mock data on error
      return {
        monthlyLimit: 100000,
        usedTokens: 0,
        remainingTokens: 100000,
        usagePercentage: 0,
        status: 'normal',
        planName: 'Free',
        totalRequests: 0,
        daysUntilReset: 30,
        estimatedDailyUsage: 0,
        lastUpdated: new Date().toISOString(),
      };
    }
  }

  // ==================== UTILITIES ====================

  private getHeaders(): Record<string, string> {
    return {
      'Content-Type': 'application/json',
      'x-api-key': this.config.apiKey,
      'x-tenant-id': this.config.tenantId,
    };
  }

  private generateId(): string {
    return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
  }

  private getDefaultSuggestedQueries(): SuggestedQueryCategory[] {
    return [
      {
        id: 'performance',
        title: 'Performance',
        icon: '📊',
        queries: [
          {
            id: 'sales-trend',
            text: 'Show me sales trends for the last quarter',
            category: 'performance',
          },
          {
            id: 'top-products',
            text: 'What are our top 10 products by revenue?',
            category: 'performance',
          },
          {
            id: 'customer-growth',
            text: 'How is our customer base growing?',
            category: 'performance',
          },
        ],
      },
      {
        id: 'insights',
        title: 'Insights',
        icon: '💡',
        queries: [
          {
            id: 'sales-drop',
            text: 'Why are sales down this month?',
            category: 'insights',
          },
          {
            id: 'anomalies',
            text: 'Detect any anomalies in recent data',
            category: 'insights',
          },
          {
            id: 'opportunities',
            text: 'What opportunities should we focus on?',
            category: 'insights',
          },
        ],
      },
      {
        id: 'comparison',
        title: 'Comparisons',
        icon: '⚖️',
        queries: [
          {
            id: 'yoy',
            text: 'Compare this year vs last year',
            category: 'comparison',
          },
          {
            id: 'region',
            text: 'Compare performance across regions',
            category: 'comparison',
          },
          {
            id: 'segment',
            text: 'Compare different customer segments',
            category: 'comparison',
          },
        ],
      },
    ];
  }
}

export default ModalAPIService;
