import React, { useState } from 'react';

interface QueryBarProps {
  onQuery: (query: string) => void;
  isQuerying: boolean;
  theme: 'light' | 'dark';
}

const QueryBar: React.FC<QueryBarProps> = ({ onQuery, isQuerying, theme }) => {
  const [query, setQuery] = useState('');

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (query.trim() && !isQuerying) {
      onQuery(query.trim());
      setQuery('');
    }
  };

  return (
    <form className={`ai-query-bar theme-${theme}`} onSubmit={handleSubmit}>
      <input
        type="text"
        className="ai-query-input"
        placeholder="Ask about your business..."
        value={query}
        onChange={(e) => setQuery(e.target.value)}
        disabled={isQuerying}
        aria-label="Enter your query"
      />
      <button
        type="submit"
        className="ai-query-submit"
        disabled={!query.trim() || isQuerying}
        aria-label="Submit query"
      >
        {isQuerying ? (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
            className="spinning"
          >
            <circle
              cx="12"
              cy="12"
              r="10"
              stroke="currentColor"
              strokeWidth="4"
              strokeOpacity="0.25"
            />
            <path
              d="M12 2a10 10 0 0 1 10 10"
              stroke="currentColor"
              strokeWidth="4"
              strokeLinecap="round"
            />
          </svg>
        ) : (
          <svg
            width="20"
            height="20"
            viewBox="0 0 24 24"
            fill="none"
            xmlns="http://www.w3.org/2000/svg"
          >
            <path
              d="M22 2L11 13M22 2l-7 20-4-9-9-4 20-7z"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        )}
      </button>
    </form>
  );
};

export default QueryBar;
