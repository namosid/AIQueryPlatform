import React, { useState } from 'react';
import type { WidgetConfig, WidgetState } from '../types';
import InsightCard from './InsightCard';
import QueryBar from './QueryBar';
import SuggestedQueries from './SuggestedQueries';

interface WidgetPanelProps {
  config: WidgetConfig;
  state: WidgetState;
  onClose: () => void;
  onRefresh: () => void;
  onQuery: (query: string) => Promise<any>;
}

const WidgetPanel: React.FC<WidgetPanelProps> = ({
  config,
  state,
  onClose,
  onRefresh,
  onQuery,
}) => {
  const [queryResult, setQueryResult] = useState<any>(null);
  const [isQuerying, setIsQuerying] = useState(false);

  const handleQuery = async (query: string) => {
    setIsQuerying(true);
    setQueryResult(null);

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
                
                {/* Display chart info if available */}
                {queryResult.data?.visualizationType === 'Chart' && (
                  <div className="result-meta">
                    <span className="result-badge">📊 {queryResult.data.chartData?.chartType || 'Chart'}</span>
                    <span className="result-badge">⏱️ {queryResult.data.executionTimeMs}ms</span>
                  </div>
                )}

                {/* Display table data if available */}
                {queryResult.data?.result?.rows && queryResult.data.result.rows.length > 0 ? (
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
                        <div className="result-more">
                          + {queryResult.data.result.rowCount - 5} more row{queryResult.data.result.rowCount - 5 !== 1 ? 's' : ''}
                        </div>
                      )}
                    </div>
                  </div>
                ) : (
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

        {/* Explore CTA */}
        {config.expandUrl && (
          <div className="ai-widget-cta">
            <a
              href={config.expandUrl}
              target="_blank"
              rel="noopener noreferrer"
              className="ai-widget-cta-link"
            >
              Explore deeper insights →
            </a>
          </div>
        )}
      </div>
    </div>
  );
};

export default WidgetPanel;
