import React from 'react';

interface FloatingButtonProps {
  onClick: () => void;
  theme: 'light' | 'dark';
  hasNotification?: boolean;
}

const FloatingButton: React.FC<FloatingButtonProps> = ({
  onClick,
  theme,
  hasNotification = false,
}) => {
  return (
    <button
      className={`ai-widget-fab theme-${theme}`}
      onClick={onClick}
      aria-label="Open AI Insights"
      title="Open AI Insights"
    >
      {/* AI Sparkle Icon */}
      <svg
        width="24"
        height="24"
        viewBox="0 0 24 24"
        fill="none"
        xmlns="http://www.w3.org/2000/svg"
      >
        <path
          d="M12 2L14.5 9.5L22 12L14.5 14.5L12 22L9.5 14.5L2 12L9.5 9.5L12 2Z"
          fill="currentColor"
        />
        <circle cx="18" cy="6" r="2" fill="currentColor" opacity="0.6" />
        <circle cx="6" cy="18" r="2" fill="currentColor" opacity="0.6" />
      </svg>

      {/* Notification Badge */}
      {hasNotification && <span className="ai-widget-fab-badge" />}
    </button>
  );
};

export default FloatingButton;
