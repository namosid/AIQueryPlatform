/**
 * Query Input Component
 * Enterprise-grade input with keyboard shortcuts and suggestions
 */

import React, { useState, useRef, useEffect } from 'react';

interface QueryInputProps {
  onSend: (query: string) => Promise<void>;
  isLoading: boolean;
}

const QueryInput: React.FC<QueryInputProps> = ({ onSend, isLoading }) => {
  const [query, setQuery] = useState('');
  const [isFocused, setIsFocused] = useState(false);
  const textareaRef = useRef<HTMLTextAreaElement>(null);

  // Auto-resize textarea
  useEffect(() => {
    if (textareaRef.current) {
      textareaRef.current.style.height = 'auto';
      textareaRef.current.style.height = `${textareaRef.current.scrollHeight}px`;
    }
  }, [query]);

  const handleSubmit = async (e?: React.FormEvent) => {
    e?.preventDefault();

    if (!query.trim() || isLoading) return;

    const trimmedQuery = query.trim();
    setQuery('');

    try {
      await onSend(trimmedQuery);
    } catch (error) {
      console.error('[QueryInput] Failed to send query:', error);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    // Submit on Ctrl/Cmd + Enter
    if ((e.ctrlKey || e.metaKey) && e.key === 'Enter') {
      e.preventDefault();
      handleSubmit();
    }
  };

  return (
    <form className="query-input-container" onSubmit={handleSubmit}>
      <div className={`query-input-wrapper ${isFocused ? 'focused' : ''}`}>
        <textarea
          id="modal-query-input"
          ref={textareaRef}
          className="query-input"
          placeholder="Ask anything about your data... (Ctrl+Enter to send)"
          value={query}
          onChange={(e) => setQuery(e.target.value)}
          onFocus={() => setIsFocused(true)}
          onBlur={() => setIsFocused(false)}
          onKeyDown={handleKeyDown}
          disabled={isLoading}
          rows={1}
          maxLength={2000}
          aria-label="Query input"
        />

        <div className="query-input-actions">
          <button
            type="submit"
            className="btn btn-primary btn-send"
            disabled={!query.trim() || isLoading}
            title="Send query (Ctrl+Enter)"
            aria-label="Send query"
          >
            {isLoading ? (
              <div className="spinner-small" />
            ) : (
              <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
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
        </div>
      </div>

      <div className="query-input-footer">
        <span className="text-muted">
          {query.length}/2000 characters
        </span>
      </div>
    </form>
  );
};

export default QueryInput;
