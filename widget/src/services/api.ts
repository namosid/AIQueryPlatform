import type {
  WidgetConfig,
  QueryRequest,
  QueryResponse,
  InsightsResponse,
  AIInsight,
} from '../types';

class APIService {
  private config: WidgetConfig;

  constructor(config: WidgetConfig) {
    this.config = config;
  }

  /**
   * Fetch AI-generated insights (auto-loaded on widget open)
   */
  async fetchInsights(): Promise<AIInsight[]> {
    try {
      const response = await fetch(`${this.config.apiBaseUrl}/api/ai/insights`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
          'x-api-key': this.config.apiKey,
          'x-tenant-id': this.config.tenantId,
        },
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || `HTTP ${response.status}`);
      }

      const data = await response.json();
      
      // Transform API response to AIInsight format if needed
      const insights: AIInsight[] = Array.isArray(data) ? data : data.insights || [];
      
      return insights;
    } catch (error) {
      console.error('[APIService] Failed to fetch insights:', error);
      
      // Return empty array on error so widget doesn't break
      // In production, you might want to show an error message instead
      return [];
    }
  }

  /**
   * Execute a natural language query
   */
  async executeQuery(query: string): Promise<QueryResponse> {
    try {
      const payload: QueryRequest = {
        query,
        context: 'widget',
        tenantId: this.config.tenantId,
        userRole: this.config.userRole,
      };

      const response = await fetch(`${this.config.apiBaseUrl}/api/query/execute`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'x-api-key': this.config.apiKey,
          'x-tenant-id': this.config.tenantId,
        },
        body: JSON.stringify(payload),
      });

      if (!response.ok) {
        const errorData = await response.json().catch(() => ({}));
        throw new Error(errorData.error || `HTTP ${response.status}`);
      }

      const data = await response.json();
      return {
        success: true,
        data: data,
      };
    } catch (error) {
      console.error('[APIService] Query execution failed:', error);
      return {
        success: false,
        error: error instanceof Error ? error.message : 'Query failed',
      };
    }
  }

  /**
   * Execute a streaming query (for large result sets)
   */
  async executeStreamingQuery(
    query: string,
    onProgress: (data: any) => void
  ): Promise<void> {
    try {
      const payload: QueryRequest = {
        query,
        context: 'widget',
        tenantId: this.config.tenantId,
        userRole: this.config.userRole,
      };

      const response = await fetch(
        `${this.config.apiBaseUrl}/api/query/execute-stream`,
        {
          method: 'POST',
          headers: {
            'Content-Type': 'application/json',
            'x-api-key': this.config.apiKey,
            'x-tenant-id': this.config.tenantId,
          },
          body: JSON.stringify(payload),
        }
      );

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      const reader = response.body?.getReader();
      if (!reader) {
        throw new Error('Stream not available');
      }

      const decoder = new TextDecoder();
      let buffer = '';

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;

        buffer += decoder.decode(value, { stream: true });
        const lines = buffer.split('\n');
        buffer = lines.pop() || '';

        for (const line of lines) {
          if (line.trim()) {
            try {
              const data = JSON.parse(line);
              onProgress(data);
            } catch (e) {
              console.warn('[APIService] Failed to parse stream data:', line);
            }
          }
        }
      }
    } catch (error) {
      console.error('[APIService] Streaming query failed:', error);
      throw error;
    }
  }

  /**
   * Health check
   */
  async checkHealth(): Promise<boolean> {
    try {
      const response = await fetch(`${this.config.apiBaseUrl}/health`, {
        method: 'GET',
      });
      return response.ok;
    } catch {
      return false;
    }
  }

  private delay(ms: number): Promise<void> {
    return new Promise((resolve) => setTimeout(resolve, ms));
  }
}

export default APIService;
