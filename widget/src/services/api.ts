import type {
  WidgetConfig,
  QueryRequest,
  QueryResponse,
  InsightsResponse,
  AIInsight,
} from '../types';

class APIService {
  protected config: WidgetConfig;

  constructor(config: WidgetConfig) {
    this.config = config;
  }

  /**
   * Get tenant information including feature flags
   */
  async getTenantInfo(): Promise<{ enableInsights: boolean; name: string; themeColor?: string }> {
    try {
      const response = await fetch(`${this.config.apiBaseUrl}/api/tenant/current`, {
        method: 'GET',
        headers: {
          'x-api-key': this.config.apiKey,
          'x-tenant-id': this.config.tenantId,
        },
      });

      if (!response.ok) {
        console.warn('[APIService] Failed to fetch tenant info, using defaults');
        return { enableInsights: true, name: 'Unknown' };
      }

      const data = await response.json();
      return {
        enableInsights: data.enableInsights ?? true,
        name: data.name,
        themeColor: data.themeColor,
      };
    } catch (error) {
      console.error('[APIService] Error fetching tenant info:', error);
      return { enableInsights: true, name: 'Unknown' }; // Default to enabled on error
    }
  }

  /**
   * Fetch AI-generated insights (auto-loaded on widget open)
   */
  async fetchInsights(): Promise<AIInsight[]> {
    // Return mock insights since /api/ai/insights endpoint doesn't exist yet
    // In production, implement this endpoint in the backend
    return [
      {
        id: '1',
        type: 'opportunity',
        title: 'Revenue Growth Opportunity',
        summary: 'Top 3 customers account for 45% of revenue. Expanding similar customer profiles could increase quarterly revenue by 23%.',
        priority: 'high',
        confidence: 87,
        why: 'Historical data shows strong correlation between customer profile characteristics and lifetime value.',
        action: 'Review customer acquisition strategy to target similar profiles. Potential 23% revenue increase over next quarter.',
        timestamp: new Date().toISOString(),
      },
      {
        id: '2',
        type: 'insight',
        title: 'Order Processing Efficiency',
        summary: 'Average order fulfillment time decreased by 12% this month compared to last month.',
        priority: 'medium',
        confidence: 92,
        why: 'Warehouse optimization and improved routing algorithms contributed to faster processing.',
        action: 'Document and replicate successful process changes across all facilities.',
        timestamp: new Date().toISOString(),
      },
      {
        id: '3',
        type: 'risk',
        title: 'Stock Level Alert',
        summary: '5 products are below reorder level. Potential stockout risk within 7 days.',
        priority: 'high',
        confidence: 95,
        why: 'Current inventory levels combined with average daily sales rate indicate imminent shortage.',
        action: 'Place urgent reorder for affected products: SKU-101, SKU-234, SKU-445, SKU-678, SKU-891.',
        timestamp: new Date().toISOString(),
      },
    ];
  }

  /**
   * Execute a natural language query
   */
  async executeQuery(query: string, conversationId?: string): Promise<QueryResponse> {
    try {
      const payload: QueryRequest = {
        query,
        context: 'widget',
        tenantId: this.config.tenantId,
        userRole: this.config.userRole,
        ...(conversationId && { conversationId }),
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
    onProgress: (data: any) => void,
    conversationId?: string
  ): Promise<void> {
    try {
      const payload: QueryRequest = {
        query,
        context: 'widget',
        tenantId: this.config.tenantId,
        userRole: this.config.userRole,
        ...(conversationId && { conversationId }),
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
