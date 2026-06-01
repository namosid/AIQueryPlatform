import React, { useState } from 'react';
import type { WidgetConfig, WidgetState, ChartData } from '../types';
import InsightCard from './InsightCard';
import QueryBar from './QueryBar';
import SuggestedQueries from './SuggestedQueries';
import StateManager from '../state/StateManager';

// Simple chart renderer for widget
const SimpleChartRenderer: React.FC<{ data: ChartData }> = ({ data }) => {
  console.log('[SimpleChartRenderer] ========== CHART RENDER ==========');
  console.log('[SimpleChartRenderer] Full data:', JSON.stringify(data, null, 2));
  console.log('[SimpleChartRenderer] Labels:', data?.labels);
  console.log('[SimpleChartRenderer] Datasets:', data?.datasets);
  console.log('[SimpleChartRenderer] ============================================');
  
  // Validate data
  if (!data || !data.labels || !data.datasets || data.labels.length === 0 || data.datasets.length === 0) {
    console.error('[SimpleChartRenderer] Invalid chart data:', data);
    return (
      <div className="widget-chart">
        <div className="chart-error">Chart data is incomplete</div>
      </div>
    );
  }
  
  // Validate dataset has data array
  if (!data.datasets[0]?.data || data.datasets[0].data.length === 0) {
    console.error('[SimpleChartRenderer] No data in dataset');
    return (
      <div className="widget-chart">
        <div className="chart-error">No data points available</div>
      </div>
    );
  }
  
  const maxValue = Math.max(...data.datasets.flatMap(ds => ds.data || []));
  console.log('[SimpleChartRenderer] Max value:', maxValue);
  
  if (maxValue === 0 || !isFinite(maxValue)) {
    console.warn('[SimpleChartRenderer] No valid numeric data');
    return (
      <div className="widget-chart">
        <div className="chart-error">No numeric data to display</div>
      </div>
    );
  }
  
  return (
    <div className="widget-chart">
      <div className="chart-bars">
        {data.labels.map((label, idx) => {
          const value = data.datasets[0]?.data[idx] || 0;
          const height = Math.max((value / maxValue) * 100, 5); // Minimum 5% height for visibility
          console.log(`[SimpleChartRenderer] Bar ${idx}: label=${label}, value=${value}, height=${height}%`);
          return (
            <div key={idx} className="chart-bar-group">
              <div className="chart-bar" style={{ height: `${height}%` }} title={`${label}: ${value}`}>
                <span className="bar-value">{value}</span>
              </div>
              <span className="bar-label">{label}</span>
            </div>
          );
        })}
      </div>
    </div>
  );
};

interface WidgetPanelProps {
  config: WidgetConfig;
  state: WidgetState;
  onClose: () => void;
  onRefresh: () => void;
  onQuery: (query: string) => Promise<any>;
  getConversationId: () => string | null;
}

const WidgetPanel: React.FC<WidgetPanelProps> = ({
  config,
  state,
  onClose,
  onRefresh,
  onQuery,
  getConversationId,
}) => {
  const [queryResult, setQueryResult] = useState<any>(null);
  const [isQuerying, setIsQuerying] = useState(false);
  const [lastQuery, setLastQuery] = useState<string>('');

  const handleQuery = async (query: string) => {
    setIsQuerying(true);
    setQueryResult(null);
    setLastQuery(query);

    try {
      const result = await onQuery(query);
      setQueryResult(result);
    } catch (error) {
      setQueryResult({
        success: false,
        error: 'Query failed',
      });
    } finally {
      setIsQuerying(false);
    }
  };

  const handleExploreDeeperClick = (e: React.MouseEvent) => {
    e.stopPropagation();
    console.log('[WidgetPanel] ========== EXPLORE DEEPER CLICKED ==========');
    console.log('[WidgetPanel] lastQuery:', lastQuery);
    console.log('[WidgetPanel] queryResult:', queryResult);
    console.log('[WidgetPanel] insights:', state.insights);
    
    const conversationId = getConversationId();
    console.log('[WidgetPanel] Current conversation ID:', conversationId);
    
    // Build context object with conversation ID
    const context = {
      conversationId: conversationId || undefined,
      query: lastQuery || undefined,
      insights: state.insights && state.insights.length > 0 ? state.insights : undefined,
      queryResult: queryResult || undefined,
    };
    
    console.log('[WidgetPanel] Opening modal with context:', context);
    console.log('[WidgetPanel] Context.conversationId:', context.conversationId);
    console.log('[WidgetPanel] Context.query:', context.query);
    console.log('[WidgetPanel] Context.insights:', context.insights);
    console.log('[WidgetPanel] Context.queryResult:', context.queryResult);
    console.log('[WidgetPanel] =======================================');
    
    // Open modal with current context - ensure all required fields
    StateManager.openModal(context);
  };

  const formatTimestamp = (timestamp: string | null) => {
    if (!timestamp) return 'Never';
    const date = new Date(timestamp);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`;
    return date.toLocaleDateString();
  };

  return (
    <div className={`ai-widget-panel theme-${config.theme}`}>
      {/* Header */}
      <div className="ai-widget-header">
        <div className="ai-widget-title">
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z"
              fill="currentColor"
            />
          </svg>
          <h3>AI Insights</h3>
        </div>

        <div className="ai-widget-header-actions">
          <span className="ai-widget-timestamp">
            {formatTimestamp(state.lastUpdated)}
          </span>
          <button
            className="ai-widget-icon-btn"
            onClick={onRefresh}
            disabled={state.isLoading}
            title="Refresh insights"
            aria-label="Refresh insights"
          >
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
              className={state.isLoading ? 'spinning' : ''}
            >
              <path
                d="M4 12C4 7.58172 7.58172 4 12 4C14.5264 4 16.7792 5.17108 18.2454 7M20 12C20 16.4183 16.4183 20 12 20C9.47362 20 7.22082 18.8289 5.75463 17M20 7V11H16M4 17V13H8"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </button>
          <button
            className="ai-widget-icon-btn"
            onClick={onClose}
            title="Close"
            aria-label="Close widget"
          >
            <svg
              width="18"
              height="18"
              viewBox="0 0 24 24"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
            >
              <path
                d="M6 18L18 6M6 6l12 12"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
                strokeLinejoin="round"
              />
            </svg>
          </button>
        </div>
      </div>

      {/* Content */}
      <div className="ai-widget-content">
        {/* Error State */}
        {state.error && (
          <div className="ai-widget-error">
            <svg
              width="20"
              height="20"
              viewBox="0 0 24 24"
              fill="none"
              xmlns="http://www.w3.org/2000/svg"
            >
              <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="2" />
              <path d="M12 8v4m0 4h.01" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
            </svg>
            <p>{state.error}</p>
          </div>
        )}

        {/* Loading Skeleton */}
        {state.isLoading && state.insights.length === 0 && (
          <div className="ai-widget-loading">
            {[1, 2, 3].map((i) => (
              <div key={i} className="ai-widget-skeleton" />
            ))}
          </div>
        )}

        {/* Insights */}
        {!state.isLoading && !state.error && state.insights.length > 0 && (
          <div className="ai-widget-insights">
            {state.insights.map((insight) => (
              <InsightCard key={insight.id} insight={insight} theme={config.theme} />
            ))}
          </div>
        )}

        {/* Query Bar */}
        <QueryBar
          onQuery={handleQuery}
          isQuerying={isQuerying}
          theme={config.theme}
        />

        {/* Suggested Queries */}
        <SuggestedQueries onSelect={handleQuery} theme={config.theme} />

        {/* Query Result */}
        {queryResult && (
          <div className="ai-widget-query-result">
            {queryResult.success ? (
              <div className="ai-widget-result-success">
                <h4>Query Result:</h4>
                
                {/* Display chart if available and valid */}
                {(() => {
                  const shouldShowChart = queryResult.data?.visualizationType === 'Chart' && 
                                         queryResult.data?.chartData && 
                                         queryResult.data.chartData.labels && 
                                         queryResult.data.chartData.labels.length > 0 &&
                                         queryResult.data.chartData.datasets &&
                                         queryResult.data.chartData.datasets.length > 0;
                  
                  console.log('[WidgetPanel] Chart check:', {
                    visualizationType: queryResult.data?.visualizationType,
                    hasChartData: !!queryResult.data?.chartData,
                    hasLabels: !!queryResult.data?.chartData?.labels,
                    labelsLength: queryResult.data?.chartData?.labels?.length,
                    hasDatasets: !!queryResult.data?.chartData?.datasets,
                    datasetsLength: queryResult.data?.chartData?.datasets?.length,
                    shouldShowChart
                  });
                  
                  return shouldShowChart ? (
                    <div className="result-chart-section">
                      <div className="result-meta">
                        <span className="result-badge">📊 {queryResult.data.chartData.chartType || 'Chart'}</span>
                        <span className="result-badge">⏱️ {queryResult.data.executionTimeMs}ms</span>
                      </div>
                      <SimpleChartRenderer data={queryResult.data.chartData} />
                    </div>
                  ) : null;
                })()}

                {/* Explore Deeper Button - positioned between chart and table */}
                {queryResult.data?.result?.rows && queryResult.data.result.rows.length > 0 && (
                  <div className="ai-widget-cta-inline">
                    <button
                      className="ai-widget-explore-btn"
                      onClick={handleExploreDeeperClick}
                      title="Open fullscreen AI workspace"
                    >
                      <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path
                          d="M15 3h6v6M9 21H3v-6M21 3l-7 7M3 21l7-7"
                          stroke="currentColor"
                          strokeWidth="2"
                          strokeLinecap="round"
                          strokeLinejoin="round"
                        />
                      </svg>
                      Explore Deeper
                    </button>
                  </div>
                )}

                {/* Display table data if available */}
                {queryResult.data?.result?.rows && queryResult.data.result.rows.length > 0 && (
                  <div className="result-table-container">
                    <div className="result-summary">
                      {queryResult.data.result.rowCount} row{queryResult.data.result.rowCount !== 1 ? 's' : ''} returned
                    </div>
                    <div className="result-table-wrapper">
                      <table className="result-table">
                        <thead>
                          <tr>
                            {queryResult.data.result.columns.map((col: string, idx: number) => (
                              <th key={idx}>{col}</th>
                            ))}
                          </tr>
                        </thead>
                        <tbody>
                          {queryResult.data.result.rows.slice(0, 5).map((row: any, rowIdx: number) => (
                            <tr key={rowIdx}>
                              {queryResult.data.result.columns.map((col: string, colIdx: number) => (
                                <td key={colIdx}>
                                  {typeof row[col] === 'number' 
                                    ? row[col].toLocaleString(undefined, { maximumFractionDigits: 2 })
                                    : row[col]?.toString() || '-'}
                                </td>
                              ))}
                            </tr>
                          ))}
                        </tbody>
                      </table>
                      {queryResult.data.result.rowCount > 5 && (
                        <button className="result-more-link" onClick={handleExploreDeeperClick}>
                          View all {queryResult.data.result.rowCount} rows →
                        </button>
                      )}
                    </div>
                  </div>
                )}
                
                {/* Display JSON fallback if no structured data */}
                {!queryResult.data?.result?.rows && !queryResult.data?.chartData && (
                  <div className="result-json">
                    <pre>{JSON.stringify(queryResult.data, null, 2)}</pre>
                  </div>
                )}
              </div>
            ) : (
              <div className="ai-widget-result-error">
                <p>Error: {queryResult.error}</p>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default WidgetPanel;
