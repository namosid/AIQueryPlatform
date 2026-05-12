/**
 * Sidebar Component
 * Left sidebar with conversation history and saved analyses
 */

import React, { useState, useEffect } from 'react';
import type { ModalAPIService } from '../../../services/modalApi';
import type { Conversation, SavedAnalysis } from '../../../types/modal';

interface SidebarProps {
  apiService: ModalAPIService;
  currentConversationId?: string;
  onConversationSelect: (id: string) => void;
  onNewConversation: () => void;
  onToggle: () => void;
}

const Sidebar: React.FC<SidebarProps> = ({
  apiService,
  currentConversationId,
  onConversationSelect,
  onNewConversation,
  onToggle,
}) => {
  const [activeTab, setActiveTab] = useState<'conversations' | 'saved'>('conversations');
  const [conversations, setConversations] = useState<Conversation[]>([]);
  const [savedAnalyses, setSavedAnalyses] = useState<SavedAnalysis[]>([]);
  const [isLoading, setIsLoading] = useState(false);

  // Load conversations on mount
  useEffect(() => {
    if (activeTab === 'conversations') {
      loadConversations();
    } else {
      loadSavedAnalyses();
    }
  }, [activeTab]);

  const loadConversations = async () => {
    setIsLoading(true);
    try {
      const response = await apiService.listConversations();
      setConversations(response.conversations);
    } catch (error) {
      console.error('[Sidebar] Failed to load conversations:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const loadSavedAnalyses = async () => {
    setIsLoading(true);
    try {
      const response = await apiService.listSavedAnalyses();
      setSavedAnalyses(response.analyses);
    } catch (error) {
      console.error('[Sidebar] Failed to load analyses:', error);
    } finally {
      setIsLoading(false);
    }
  };

  const handleDeleteAnalysis = async (id: string) => {
    if (!confirm('Are you sure you want to delete this saved analysis?')) {
      return;
    }

    try {
      const success = await apiService.deleteAnalysis(id);
      if (success) {
        // Reload the list
        await loadSavedAnalyses();
      } else {
        alert('Failed to delete analysis');
      }
    } catch (error) {
      console.error('[Sidebar] Failed to delete analysis:', error);
      alert('Failed to delete analysis. Please try again.');
    }
  };

  const formatDate = (dateString: string): string => {
    const date = new Date(dateString);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / 60000);

    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffMins < 1440) return `${Math.floor(diffMins / 60)}h ago`;
    if (diffMins < 10080) return `${Math.floor(diffMins / 1440)}d ago`;
    return date.toLocaleDateString();
  };

  return (
    <div className="modal-sidebar">
      {/* Sidebar Header */}
      <div className="sidebar-header">
        <button
          className="btn btn-primary btn-new-conversation"
          onClick={onNewConversation}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M12 5V19M5 12H19" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
          </svg>
          New Chat
        </button>

        <button
          className="btn-icon"
          onClick={onToggle}
          title="Collapse sidebar"
          aria-label="Collapse sidebar"
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M15 18L9 12L15 6"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </button>
      </div>

      {/* Tabs */}
      <div className="sidebar-tabs">
        <button
          className={`sidebar-tab ${activeTab === 'conversations' ? 'active' : ''}`}
          onClick={() => setActiveTab('conversations')}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M21 15a2 2 0 0 1-2 2H7l-4 4V5a2 2 0 0 1 2-2h14a2 2 0 0 1 2 2z"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
          Conversations
        </button>

        <button
          className={`sidebar-tab ${activeTab === 'saved' ? 'active' : ''}`}
          onClick={() => setActiveTab('saved')}
        >
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M19 21l-7-5-7 5V5a2 2 0 0 1 2-2h10a2 2 0 0 1 2 2z"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
          Saved
        </button>
      </div>

      {/* Content */}
      <div className="sidebar-content">
        {isLoading ? (
          <div className="sidebar-loading">
            <div className="spinner" />
            Loading...
          </div>
        ) : activeTab === 'conversations' ? (
          <div className="conversation-list">
            {conversations.length === 0 ? (
              <div className="empty-state">
                <p>No conversations yet</p>
                <p className="text-muted">Start a new conversation to get insights</p>
              </div>
            ) : (
              conversations.map((conv) => (
                <button
                  key={conv.id}
                  className={`conversation-item ${
                    conv.id === currentConversationId ? 'active' : ''
                  }`}
                  onClick={() => onConversationSelect(conv.id)}
                >
                  {conv.pinned && (
                    <svg
                      className="pin-icon"
                      width="12"
                      height="12"
                      viewBox="0 0 24 24"
                      fill="currentColor"
                    >
                      <path d="M16 12V4H17V2H7V4H8V12L6 14V16H11.2V22H12.8V16H18V14L16 12Z" />
                    </svg>
                  )}

                  <div className="conversation-item-content">
                    <div className="conversation-title">{conv.title}</div>
                    <div className="conversation-meta">
                      <span>{conv.messages?.length || 0} messages</span>
                      <span className="bullet">•</span>
                      <span>{formatDate(conv.updatedAt)}</span>
                    </div>
                  </div>
                </button>
              ))
            )}
          </div>
        ) : (
          <div className="saved-list">
            {savedAnalyses.length === 0 ? (
              <div className="empty-state">
                <p>No saved analyses</p>
                <p className="text-muted">Save important insights for later</p>
              </div>
            ) : (
              savedAnalyses.map((analysis) => (
                <div 
                  key={analysis.id} 
                  className="saved-item"
                  onClick={() => onConversationSelect(analysis.conversationId)}
                  style={{ cursor: 'pointer' }}
                >
                  <div className="saved-item-header">
                    <h4>{analysis.title}</h4>
                    <button 
                      className="btn-icon" 
                      title="Delete"
                      onClick={(e) => {
                        e.stopPropagation(); // Prevent triggering the parent onClick
                        handleDeleteAnalysis(analysis.id);
                      }}
                    >
                      <svg width="14" height="14" viewBox="0 0 24 24" fill="none">
                        <path
                          d="M3 6h18M19 6v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6m3 0V4a2 2 0 012-2h4a2 2 0 012 2v2"
                          stroke="currentColor"
                          strokeWidth="2"
                          strokeLinecap="round"
                        />
                      </svg>
                    </button>
                  </div>
                  {analysis.description && (
                    <p className="saved-item-description">{analysis.description}</p>
                  )}
                  <div className="saved-item-meta">
                    <span>{formatDate(analysis.savedAt)}</span>
                  </div>
                </div>
              ))
            )}
          </div>
        )}
      </div>
    </div>
  );
};

export default Sidebar;
