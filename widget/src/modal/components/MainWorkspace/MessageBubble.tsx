/**
 * Message Bubble Component
 * Individual message with role-based styling and visualization rendering
 */

import React, { useState } from 'react';
import type { Message } from '../../../types/modal';
import ResponseRenderer from './ResponseRenderer';

interface MessageBubbleProps {
  message: Message;
  onExportPDF?: (message: Message) => Promise<void>;
}

const MessageBubble: React.FC<MessageBubbleProps> = ({ message, onExportPDF }) => {
  const [isExporting, setIsExporting] = useState(false);

  console.log('[MessageBubble] Rendering message:', {
    id: message.id,
    role: message.role,
    hasVisualization: !!message.visualization,
    visualizationType: message.visualization?.type,
    visualization: message.visualization
  });

  const formatTimestamp = (timestamp: string): string => {
    const date = new Date(timestamp);
    return date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
  };

  const handleExportPDF = async () => {
    if (!onExportPDF) return;
    
    setIsExporting(true);
    try {
      await onExportPDF(message);
    } catch (error) {
      console.error('[MessageBubble] PDF export failed:', error);
    } finally {
      setIsExporting(false);
    }
  };

  const canExportPDF = message.role === 'assistant' && (message.visualization || message.data);

  return (
    <div className={`message-bubble ${message.role}`}>
      <div className="message-avatar">
        {message.role === 'user' ? (
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <circle cx="12" cy="8" r="4" fill="currentColor" />
            <path
              d="M4 20c0-3.314 2.686-6 6-6h4c3.314 0 6 2.686 6 6"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
            />
          </svg>
        ) : (
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z"
              fill="currentColor"
            />
          </svg>
        )}
      </div>

      <div className="message-content">
        <div className="message-header">
          <span className="message-role">
            {message.role === 'user' ? 'You' : 'AI Assistant'}
          </span>
          <div className="message-actions">
            {canExportPDF && (
              <button 
                className="btn-export-pdf" 
                onClick={handleExportPDF}
                disabled={isExporting}
                title="Export as PDF"
              >
                {isExporting ? (
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                    <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="2" opacity="0.3" />
                    <path d="M12 2a10 10 0 0 1 10 10" stroke="currentColor" strokeWidth="2" strokeLinecap="round">
                      <animateTransform
                        attributeName="transform"
                        type="rotate"
                        from="0 12 12"
                        to="360 12 12"
                        dur="1s"
                        repeatCount="indefinite"
                      />
                    </path>
                  </svg>
                ) : (
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                    <path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z" stroke="currentColor" strokeWidth="2" />
                    <path d="M14 2v6h6M12 18v-6M9 15l3 3 3-3" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                  </svg>
                )}
                {!isExporting && <span>PDF</span>}
              </button>
            )}
            <span className="message-timestamp">{formatTimestamp(message.timestamp)}</span>
          </div>
        </div>

        <div className="message-body">
          <p>{message.content}</p>

          {/* Render query SQL if available */}
          {message.sql && (
            <div className="message-sql">
              <div className="sql-header">
                <span>SQL Query</span>
                <button className="btn-copy" title="Copy SQL">
                  <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                    <rect x="9" y="9" width="13" height="13" rx="2" stroke="currentColor" strokeWidth="2" />
                    <path d="M5 15H4a2 2 0 01-2-2V4a2 2 0 012-2h9a2 2 0 012 2v1" stroke="currentColor" strokeWidth="2" />
                  </svg>
                </button>
              </div>
              <pre><code>{message.sql}</code></pre>
            </div>
          )}

          {/* Render visualization */}
          {message.visualization && (
            <ResponseRenderer visualization={message.visualization} />
          )}

          {/* Render insights */}
          {message.insights && message.insights.length > 0 && (
            <div className="message-insights">
              <h4>Key Insights</h4>
              {message.insights.map((insight) => (
                <div key={insight.id} className={`insight-card insight-${insight.type}`}>
                  <div className="insight-header">
                    <span className="insight-type">{insight.type}</span>
                    <span className="insight-confidence">{insight.confidence}% confidence</span>
                  </div>
                  <h5>{insight.title}</h5>
                  <p>{insight.summary}</p>
                </div>
              ))}
            </div>
          )}

          {/* Metadata */}
          {message.metadata && (
            <div className="message-metadata">
              {message.metadata.executionTime && (
                <span>⏱️ {message.metadata.executionTime}ms</span>
              )}
              {message.metadata.rowCount && (
                <span>📊 {message.metadata.rowCount} rows</span>
              )}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

export default MessageBubble;
