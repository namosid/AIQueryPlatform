/**
 * Main Workspace Component
 * Central area with conversation thread and query input
 */

import React, { useRef, useEffect } from 'react';
import type { Conversation, Message } from '../../../types/modal';
import type { ModalAPIService } from '../../../services/modalApi';
import ConversationThread from './ConversationThread';
import QueryInput from './QueryInput';

interface MainWorkspaceProps {
  conversation: Conversation | null;
  messages: Message[];
  isLoading: boolean;
  error: string | null;
  onSendMessage: (query: string) => Promise<void>;
  sidebarCollapsed: boolean;
  onToggleSidebar: () => void;
  apiService: ModalAPIService;
}

const MainWorkspace: React.FC<MainWorkspaceProps> = ({
  conversation,
  messages,
  isLoading,
  error,
  onSendMessage,
  sidebarCollapsed,
  onToggleSidebar,
  apiService,
}) => {
  const messagesEndRef = useRef<HTMLDivElement>(null);

  // Auto-scroll to bottom when new messages arrive
  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [messages]);

  // Save Analysis Handler
  const handleSaveAnalysis = async () => {
    if (!conversation) {
      alert('No conversation to save');
      return;
    }

    const title = prompt('Enter a title for this analysis:', conversation.title);
    if (!title) return;

    const description = prompt('Add a description (optional):');

    try {
      console.log('[MainWorkspace] Saving analysis for conversation:', conversation.id);
      const result = await apiService.saveAnalysis(conversation.id, title, description || undefined);
      console.log('[MainWorkspace] Save result:', result);
      alert('✓ Analysis saved successfully!');
    } catch (error) {
      console.error('[MainWorkspace] Failed to save analysis:', error);
      const errorMessage = error instanceof Error ? error.message : 'Unknown error';
      alert(`Failed to save analysis: ${errorMessage}\n\nCheck browser console for details.`);
    }
  };

  // PDF Export Handler
  const handleExportPDF = async (message: Message) => {
    // Extract table and chart data from visualization (handles both table and mixed types)
    let tableData: any = null;
    let chartData: any = null;
    
    if (message.visualization?.type === 'table' && message.visualization.table) {
      tableData = message.visualization.table;
    } else if (message.visualization?.type === 'mixed' && message.visualization.components) {
      // Find table and chart components in mixed visualization
      const tableComponent = message.visualization.components.find((c: any) => c.type === 'table');
      if (tableComponent && tableComponent.data) {
        tableData = tableComponent.data;
      }
      
      const chartComponent = message.visualization.components.find((c: any) => c.type === 'chart');
      if (chartComponent && chartComponent.data) {
        chartData = chartComponent.data;
      }
    } else if (message.visualization?.type === 'chart' && message.visualization.chart) {
      chartData = message.visualization.chart;
    }
    
    // Check if we have data to export
    if (!tableData || !tableData.rows || tableData.rows.length === 0) {
      console.error('[MainWorkspace] Cannot export PDF: no data to export');
      console.error('[MainWorkspace] Visualization:', message.visualization);
      alert('No data available to export');
      return;
    }

    // Get query from message content if it's a user message, or from the previous user message
    const query = message.query || message.content;
    const sql = message.sql || 'N/A';
    
    // Build result structure for PDF
    const resultData = {
      columns: tableData.columns.map((c: any) => c.label || c.key),
      rows: tableData.rows,
      rowCount: tableData.rows.length,
    };

    console.log('[MainWorkspace] Exporting PDF with chartData:', chartData);

    try {
      const blob = await apiService.generatePDF(query, sql, resultData, chartData, conversation?.id);
      
      // Download the PDF
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      a.download = `query-result-${new Date().toISOString().slice(0, 10)}.pdf`;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);

      console.log('[MainWorkspace] PDF downloaded successfully');
    } catch (error) {
      console.error('[MainWorkspace] PDF export failed:', error);
      alert('Failed to export PDF. Please try again.');
    }
  };

  return (
    <div className="modal-main-workspace">
      {/* Header */}
      <div className="workspace-header">
        {sidebarCollapsed && (
          <button
            className="btn-icon"
            onClick={onToggleSidebar}
            title="Show sidebar"
            aria-label="Show sidebar"
          >
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M3 12h18M3 6h18M3 18h18"
                stroke="currentColor"
                strokeWidth="2"
                strokeLinecap="round"
              />
            </svg>
          </button>
        )}

        <div className="workspace-title">
          {conversation ? (
            <>
              <h2>{conversation.title}</h2>
              <span className="text-muted">{messages.length} messages</span>
            </>
          ) : (
            <h2>Start a new conversation</h2>
          )}
        </div>

        {/* Action Buttons */}
        {conversation && messages.length > 0 && (
          <div className="workspace-actions">
            <button
              className="btn-icon btn-save"
              onClick={handleSaveAnalysis}
              title="Save this analysis"
              aria-label="Save analysis"
            >
              <svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path
                  d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"
                  stroke="currentColor"
                  strokeWidth="2"
                  strokeLinecap="round"
                  strokeLinejoin="round"
                />
              </svg>
            </button>
          </div>
        )}
      </div>

      {/* Conversation Thread */}
      <div className="workspace-content">
        {messages.length === 0 ? (
          <div className="empty-workspace">
            <div className="empty-workspace-icon">
              <svg width="64" height="64" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path
                  d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z"
                  fill="currentColor"
                  opacity="0.2"
                />
              </svg>
            </div>
            <h3>Ask me anything about your data</h3>
            <p className="text-muted">
              I can help you analyze trends, find insights, and answer questions about your business.
            </p>

            {/* Suggested Queries */}
            <div className="suggested-queries">
              <button
                className="suggested-query"
                onClick={() => onSendMessage('Show me sales trends for the last quarter')}
              >
                📊 Sales trends for last quarter
              </button>
              <button
                className="suggested-query"
                onClick={() => onSendMessage('What are our top performing products?')}
              >
                🏆 Top performing products
              </button>
              <button
                className="suggested-query"
                onClick={() => onSendMessage('Identify any anomalies in recent data')}
              >
                🔍 Detect anomalies
              </button>
              <button
                className="suggested-query"
                onClick={() => onSendMessage('Compare this year vs last year')}
              >
                ⚖️ Year-over-year comparison
              </button>
            </div>
          </div>
        ) : (
          <>
            <ConversationThread 
              messages={messages} 
              isLoading={isLoading} 
              onExportPDF={handleExportPDF}
            />
            <div ref={messagesEndRef} />
          </>
        )}

        {error && (
          <div className="error-banner">
            <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <circle cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="2" />
              <path d="M12 8v4M12 16h.01" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
            </svg>
            {error}
          </div>
        )}
      </div>

      {/* Query Input */}
      <div className="workspace-footer">
        <QueryInput onSend={onSendMessage} isLoading={isLoading} />
      </div>
    </div>
  );
};

export default MainWorkspace;
