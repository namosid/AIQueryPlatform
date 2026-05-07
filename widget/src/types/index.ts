export interface WidgetConfig {
  apiBaseUrl: string;
  apiKey: string;
  tenantId: string;
  userRole: string;
  theme: 'light' | 'dark';
  position: 'bottom-right' | 'bottom-left' | 'top-right' | 'top-left';
  expandUrl?: string;
  autoOpen: boolean;
}

export interface AIInsight {
  id: string;
  type: 'risk' | 'opportunity' | 'insight';
  title: string;
  confidence: number; // 0-100
  summary: string;
  why: string;
  action: string;
  timestamp: string;
  priority: 'high' | 'medium' | 'low';
}

export interface QueryRequest {
  query: string;
  context: string;
  tenantId: string;
  userRole: string;
}

export interface QueryResponse {
  success: boolean;
  data?: {
    result: any;
    visualization?: 'table' | 'chart';
    chartData?: ChartData;
  };
  error?: string;
  sql?: string;
}

export interface ChartData {
  labels: string[];
  datasets: Array<{
    label: string;
    data: number[];
  }>;
  chartType: 'bar' | 'line' | 'pie';
}

export interface InsightsResponse {
  insights: AIInsight[];
  lastUpdated: string;
  nextUpdate?: string;
}

export interface WidgetState {
  isOpen: boolean;
  isLoading: boolean;
  insights: AIInsight[];
  error: string | null;
  lastUpdated: string | null;
}

export interface SuggestedQuery {
  id: string;
  text: string;
  icon: string;
}
