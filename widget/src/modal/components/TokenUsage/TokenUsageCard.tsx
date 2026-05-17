/**
 * TokenUsageCard Component
 * Displays current token usage with progress bar and status
 */

import React, { useEffect, useState } from 'react';
import type { ModalAPIService } from '../../../services/modalApi';

interface TokenUsageData {
  monthlyLimit: number;
  usedTokens: number;
  remainingTokens: number;
  usagePercentage: number;
  status: 'normal' | 'warning' | 'critical' | 'exceeded';
  planName: string;
  daysUntilReset: number;
  estimatedDailyUsage: number;
}

interface TokenUsageCardProps {
  apiService: ModalAPIService;
  compact?: boolean;
  onQuotaExceeded?: () => void;
  refreshTrigger?: number; // Increment this to trigger immediate refresh
}

const TokenUsageCard: React.FC<TokenUsageCardProps> = ({
  apiService,
  compact = false,
  onQuotaExceeded,
  refreshTrigger = 0,
}) => {
  const [usage, setUsage] = useState<TokenUsageData | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadUsage();
    
    // Refresh every 30 seconds
    const interval = setInterval(loadUsage, 30000);
    return () => clearInterval(interval);
  }, []);

  // Refresh immediately when refreshTrigger changes
  useEffect(() => {
    if (refreshTrigger > 0) {
      console.log('[TokenUsageCard] Refresh triggered by parent:', refreshTrigger);
      loadUsage();
    }
  }, [refreshTrigger]);

  const loadUsage = async () => {
    try {
      const data = await apiService.getTokenUsage();
      setUsage(data);
      setError(null);
      
      if (data.status === 'exceeded' && onQuotaExceeded) {
        onQuotaExceeded();
      }
    } catch (err) {
      console.error('[TokenUsageCard] Failed to load usage:', err);
      setError('Unable to load quota information');
    } finally {
      setLoading(false);
    }
  };

  const formatTokens = (tokens: number): string => {
    if (tokens >= 1000000) {
      return `${(tokens / 1000000).toFixed(1)}M`;
    }
    if (tokens >= 1000) {
      return `${(tokens / 1000).toFixed(1)}K`;
    }
    return tokens.toString();
  };

  const getStatusColor = (status: string): string => {
    switch (status) {
      case 'exceeded':
        return '#ef4444'; // red
      case 'critical':
        return '#f59e0b'; // orange
      case 'warning':
        return '#eab308'; // yellow
      default:
        return '#10b981'; // green
    }
  };

  const getStatusIcon = (status: string): string => {
    switch (status) {
      case 'exceeded':
        return '🚫';
      case 'critical':
        return '⚠️';
      case 'warning':
        return '⚡';
      default:
        return '✓';
    }
  };

  const getStatusMessage = (status: string): string => {
    switch (status) {
      case 'exceeded':
        return 'Quota exceeded - AI requests blocked';
      case 'critical':
        return 'Critical - Approaching limit';
      case 'warning':
        return 'Warning - High usage';
      default:
        return 'All systems normal';
    }
  };

  if (loading) {
    return (
      <div className={`token-usage-card ${compact ? 'compact' : ''}`}>
        <div className="token-usage-loading">
          <div className="spinner-small" />
          Loading quota...
        </div>
      </div>
    );
  }

  if (error || !usage) {
    return (
      <div className={`token-usage-card ${compact ? 'compact' : ''} error`}>
        <span className="error-icon">⚠️</span>
        <span className="error-text">{error || 'No data'}</span>
      </div>
    );
  }

  if (compact) {
    return (
      <div className={`token-usage-card compact status-${usage.status}`}>
        <div className="usage-header-compact">
          <span className="usage-icon">{getStatusIcon(usage.status)}</span>
          <div className="usage-text">
            <span className="usage-label">AI Tokens</span>
            <span className="usage-value">
              {formatTokens(usage.usedTokens)} / {formatTokens(usage.monthlyLimit)}
            </span>
          </div>
        </div>
        <div className="progress-bar-container">
          <div 
            className="progress-bar-fill" 
            style={{ 
              width: `${Math.min(usage.usagePercentage, 100)}%`,
              backgroundColor: getStatusColor(usage.status)
            }}
          />
        </div>
      </div>
    );
  }

  return (
    <div className={`token-usage-card detailed status-${usage.status}`}>
      <div className="usage-header">
        <h4>
          <span className="usage-icon">{getStatusIcon(usage.status)}</span>
          AI Token Usage
        </h4>
        <span className="usage-plan">{usage.planName}</span>
      </div>

      <div className="usage-stats">
        <div className="stat-item">
          <span className="stat-label">Used</span>
          <span className="stat-value">{formatTokens(usage.usedTokens)}</span>
        </div>
        <div className="stat-divider">/</div>
        <div className="stat-item">
          <span className="stat-label">Limit</span>
          <span className="stat-value">{formatTokens(usage.monthlyLimit)}</span>
        </div>
        <div className="stat-item remaining">
          <span className="stat-label">Remaining</span>
          <span className="stat-value remaining">{formatTokens(usage.remainingTokens)}</span>
        </div>
      </div>

      <div className="progress-bar-container">
        <div 
          className="progress-bar-fill" 
          style={{ 
            width: `${Math.min(usage.usagePercentage, 100)}%`,
            backgroundColor: getStatusColor(usage.status)
          }}
        />
        <span className="progress-percentage">{usage.usagePercentage.toFixed(1)}%</span>
      </div>

      <div className="usage-status">
        <span 
          className="status-badge" 
          style={{ backgroundColor: getStatusColor(usage.status) }}
        >
          {usage.status.toUpperCase()}
        </span>
        <span className="status-message">{getStatusMessage(usage.status)}</span>
      </div>

      <div className="usage-footer">
        <div className="footer-item">
          <span className="footer-icon">📅</span>
          <span className="footer-text">Resets in {usage.daysUntilReset} days</span>
        </div>
        <div className="footer-item">
          <span className="footer-icon">📊</span>
          <span className="footer-text">
            ~{formatTokens(usage.estimatedDailyUsage)}/day avg
          </span>
        </div>
      </div>

      {(usage.status === 'warning' || usage.status === 'critical' || usage.status === 'exceeded') && (
        <div className="usage-alert">
          {usage.status === 'exceeded' ? (
            <>
              <p><strong>Quota Exceeded!</strong></p>
              <p>AI requests are currently blocked. Upgrade your plan to continue.</p>
              <button className="btn btn-primary btn-upgrade">
                Upgrade Plan
              </button>
            </>
          ) : usage.status === 'critical' ? (
            <>
              <p><strong>Critical Usage Level</strong></p>
              <p>You're approaching your monthly limit. Consider upgrading soon.</p>
            </>
          ) : (
            <>
              <p><strong>High Usage Detected</strong></p>
              <p>You've used {usage.usagePercentage.toFixed(0)}% of your monthly quota.</p>
            </>
          )}
        </div>
      )}
    </div>
  );
};

export default TokenUsageCard;
