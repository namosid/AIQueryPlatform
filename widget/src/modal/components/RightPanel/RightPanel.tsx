/**
 * Right Panel Component
 * Displays risks, opportunities, and recommendations
 */

import React from 'react';
import type { Conversation, Message, Recommendation } from '../../../types/modal';

interface RightPanelProps {
  conversation: Conversation;
  messages: Message[];
  onToggle: () => void;
}

const RightPanel: React.FC<RightPanelProps> = ({ conversation, messages, onToggle }) => {
  // Extract recommendations from messages
  const recommendations: Recommendation[] = messages
    .filter((m) => m.recommendations)
    .flatMap((m) => m.recommendations || []);

  // Group by type
  const risks = recommendations.filter((r) => r.type === 'risk');
  const opportunities = recommendations.filter((r) => r.type === 'opportunity');
  const actions = recommendations.filter((r) => r.type === 'action');

  const renderRecommendation = (rec: Recommendation) => (
    <div key={rec.id} className={`recommendation-card priority-${rec.priority}`}>
      <div className="recommendation-header">
        <span className={`recommendation-badge ${rec.type}`}>
          {rec.type === 'risk' ? '⚠️' : rec.type === 'opportunity' ? '💡' : '✓'}
          {rec.type}
        </span>
        <span className={`priority-badge priority-${rec.priority}`}>
          {rec.priority}
        </span>
      </div>

      <h4>{rec.title}</h4>
      <p>{rec.description}</p>

      {rec.impact && (
        <div className="recommendation-meta">
          <span className="meta-label">Impact:</span>
          <span>{rec.impact}</span>
        </div>
      )}

      {rec.effort && (
        <div className="recommendation-meta">
          <span className="meta-label">Effort:</span>
          <span>{rec.effort}</span>
        </div>
      )}

      {rec.actions && rec.actions.length > 0 && (
        <div className="recommendation-actions">
          {rec.actions.map((action) => (
            <button
              key={action.id}
              className="btn btn-small btn-secondary"
              onClick={() => {
                if (action.query) {
                  // Trigger query
                  console.log('Execute query:', action.query);
                } else if (action.url) {
                  window.open(action.url, '_blank');
                }
              }}
            >
              {action.label}
            </button>
          ))}
        </div>
      )}
    </div>
  );

  return (
    <div className="modal-right-panel">
      <div className="panel-header">
        <h3>Insights</h3>
        <button
          className="btn-icon"
          onClick={onToggle}
          title="Collapse panel"
          aria-label="Collapse panel"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M9 18l6-6-6-6"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </button>
      </div>

      <div className="panel-content">
        {/* Risks */}
        {risks.length > 0 && (
          <div className="panel-section">
            <h4 className="section-title">
              <span className="section-icon">⚠️</span>
              Risks ({risks.length})
            </h4>
            {risks.map(renderRecommendation)}
          </div>
        )}

        {/* Opportunities */}
        {opportunities.length > 0 && (
          <div className="panel-section">
            <h4 className="section-title">
              <span className="section-icon">💡</span>
              Opportunities ({opportunities.length})
            </h4>
            {opportunities.map(renderRecommendation)}
          </div>
        )}

        {/* Actions */}
        {actions.length > 0 && (
          <div className="panel-section">
            <h4 className="section-title">
              <span className="section-icon">✓</span>
              Recommended Actions ({actions.length})
            </h4>
            {actions.map(renderRecommendation)}
          </div>
        )}

        {/* Empty State */}
        {recommendations.length === 0 && (
          <div className="panel-empty-state">
            <p className="text-muted">
              Ask questions to receive personalized insights and recommendations.
            </p>
          </div>
        )}
      </div>
    </div>
  );
};

export default RightPanel;
