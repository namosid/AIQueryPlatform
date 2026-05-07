import React, { useState } from 'react';
import type { AIInsight } from '../types';

interface InsightCardProps {
  insight: AIInsight;
  theme: 'light' | 'dark';
}

const InsightCard: React.FC<InsightCardProps> = ({ insight, theme }) => {
  const [isExpanded, setIsExpanded] = useState(false);

  const getIcon = () => {
    switch (insight.type) {
      case 'risk':
        return (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              d="M12 9v4m0 4h.01M5.07 19c-1.42 0-2.28-1.53-1.55-2.77l6.93-12A2 2 0 0 1 12 3c.8 0 1.54.39 2 1.03l6.93 12c.73 1.24-.13 2.77-1.55 2.77H5.07z"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        );
      case 'opportunity':
        return (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              d="M12 2v4m0 12v4M4.22 4.22l2.83 2.83m9.9 9.9l2.83 2.83m0-14.14l-2.83 2.83M7.05 16.95l-2.83 2.83M22 12h-4M6 12H2"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        );
      case 'insight':
        return (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              d="M13 2L3 14h9l-1 8 10-12h-9l1-8z"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        );
    }
  };

  const getTypeClass = () => {
    switch (insight.type) {
      case 'risk':
        return 'ai-insight-risk';
      case 'opportunity':
        return 'ai-insight-opportunity';
      case 'insight':
        return 'ai-insight-info';
    }
  };

  return (
    <div className={`ai-insight-card ${getTypeClass()} theme-${theme}`}>
      <div className="ai-insight-header">
        <div className="ai-insight-icon">{getIcon()}</div>
        <div className="ai-insight-title-group">
          <h4 className="ai-insight-title">{insight.title}</h4>
          <p className="ai-insight-summary">{insight.summary}</p>
        </div>
        <div className="ai-insight-confidence">
          <span className="ai-insight-confidence-value">{insight.confidence}%</span>
        </div>
      </div>

      {isExpanded && (
        <div className="ai-insight-details">
          <div className="ai-insight-section">
            <strong>Why:</strong>
            <p>{insight.why}</p>
          </div>
          <div className="ai-insight-section">
            <strong>Recommended Action:</strong>
            <p>{insight.action}</p>
          </div>
        </div>
      )}

      <button
        className="ai-insight-toggle"
        onClick={() => setIsExpanded(!isExpanded)}
      >
        {isExpanded ? 'Show less' : 'Show more'}
        <svg
          width="16"
          height="16"
          viewBox="0 0 24 24"
          fill="none"
          xmlns="http://www.w3.org/2000/svg"
          style={{
            transform: isExpanded ? 'rotate(180deg)' : 'rotate(0deg)',
            transition: 'transform 0.2s',
          }}
        >
          <path
            d="M6 9l6 6 6-6"
            stroke="currentColor"
            strokeWidth="2"
            strokeLinecap="round"
            strokeLinejoin="round"
          />
        </svg>
      </button>
    </div>
  );
};

export default InsightCard;