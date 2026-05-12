/**
 * Modal Header Component
 * Title bar with close/minimize controls
 */

import React from 'react';

interface ModalHeaderProps {
  onClose: () => void;
  onMinimize: () => void;
  conversationTitle?: string;
}

const ModalHeader: React.FC<ModalHeaderProps> = ({ onClose, onMinimize, conversationTitle }) => {
  return (
    <div className="modal-header">
      <div className="modal-header-left">
        <div className="modal-icon">
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z"
              fill="currentColor"
            />
          </svg>
        </div>
        <div className="modal-title-container">
          <h1 id="modal-title" className="modal-title">
            AI Analytics Workspace
          </h1>
          {conversationTitle && (
            <span className="modal-subtitle">{conversationTitle}</span>
          )}
        </div>
      </div>

      <div className="modal-header-right">
        <button
          className="modal-header-btn"
          onClick={onMinimize}
          title="Minimize to widget (ESC)"
          aria-label="Minimize"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M5 12H19" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
          </svg>
        </button>

        <button
          className="modal-header-btn"
          onClick={onClose}
          title="Close (ESC)"
          aria-label="Close"
        >
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path
              d="M18 6L6 18M6 6L18 18"
              stroke="currentColor"
              strokeWidth="2"
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          </svg>
        </button>
      </div>
    </div>
  );
};

export default ModalHeader;
