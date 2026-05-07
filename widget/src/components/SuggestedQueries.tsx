import React from 'react';
import type { SuggestedQuery } from '../types';

interface SuggestedQueriesProps {
  onSelect: (query: string) => void;
  theme: 'light' | 'dark';
}

const suggestedQueries: SuggestedQuery[] = [
  { id: '1', text: 'Why are sales down?', icon: '📉' },
  { id: '2', text: 'Top opportunities this week', icon: '🎯' },
  { id: '3', text: 'Customer churn risks', icon: '⚠️' },
  { id: '4', text: 'Inventory alerts', icon: '📦' },
  { id: '5', text: 'Revenue by category', icon: '💰' },
  { id: '6', text: 'Best selling products', icon: '🏆' },
];

const SuggestedQueries: React.FC<SuggestedQueriesProps> = ({ onSelect, theme }) => {
  return (
    <div className={`ai-suggested-queries theme-${theme}`}>
      <label className="ai-suggested-label">Suggested queries:</label>
      <div className="ai-suggested-chips">
        {suggestedQueries.map((suggestion) => (
          <button
            key={suggestion.id}
            className="ai-suggested-chip"
            onClick={() => onSelect(suggestion.text)}
            aria-label={`Query: ${suggestion.text}`}
          >
            <span className="ai-suggested-icon">{suggestion.icon}</span>
            <span className="ai-suggested-text">{suggestion.text}</span>
          </button>
        ))}
      </div>
    </div>
  );
};

export default SuggestedQueries;
