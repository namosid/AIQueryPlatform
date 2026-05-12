/**
 * Conversation Thread Component
 * Displays message history with visualizations
 */

import React from 'react';
import type { Message } from '../../../types/modal';
import MessageBubble from './MessageBubble';

interface ConversationThreadProps {
  messages: Message[];
  isLoading: boolean;
  onExportPDF?: (message: Message) => Promise<void>;
}

const ConversationThread: React.FC<ConversationThreadProps> = ({ messages, isLoading, onExportPDF }) => {
  return (
    <div className="conversation-thread">
      {messages.map((message) => (
        <MessageBubble key={message.id} message={message} onExportPDF={onExportPDF} />
      ))}

      {isLoading && (
        <div className="message-bubble assistant loading">
          <div className="message-avatar">
            <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
              <path
                d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z"
                fill="currentColor"
              />
            </svg>
          </div>
          <div className="message-content">
            <div className="typing-indicator">
              <span></span>
              <span></span>
              <span></span>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default ConversationThread;
