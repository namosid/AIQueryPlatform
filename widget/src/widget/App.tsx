import React, { useState, useEffect } from 'react';
import type { WidgetConfig, AIInsight, WidgetState } from '../types';
import APIService from '../services/api';
import FloatingButton from '../components/FloatingButton';
import WidgetPanel from '../components/WidgetPanel';

interface AppProps {
  config: WidgetConfig;
}

const App: React.FC<AppProps> = ({ config }) => {
  const [state, setState] = useState<WidgetState>({
    isOpen: config.autoOpen,
    isLoading: false,
    insights: [],
    error: null,
    lastUpdated: null,
  });

  const [apiService] = useState(() => new APIService(config));

  // Load insights on mount or when opened
  useEffect(() => {
    if (state.isOpen && state.insights.length === 0 && !state.isLoading) {
      //loadInsights();
    }
  }, [state.isOpen]);

  const loadInsights = async () => {
    setState((prev: WidgetState) => ({ ...prev, isLoading: true, error: null }));

    try {
      const insights = await apiService.fetchInsights();
      setState((prev: WidgetState) => ({
        ...prev,
        insights,
        isLoading: false,
        lastUpdated: new Date().toISOString(),
        error: null,
      }));
    } catch (error) {
      setState((prev: WidgetState) => ({
        ...prev,
        isLoading: false,
        error: 'Failed to load insights. Please try again.',
      }));
    }
  };

  const toggleWidget = () => {
    setState((prev: WidgetState) => ({ ...prev, isOpen: !prev.isOpen }));
  };

  const handleRefresh = () => {
    //loadInsights();
  };

  const handleQuery = async (query: string): Promise<any> => {
    try {
      const response = await apiService.executeQuery(query);
      return response;
    } catch (error) {
      throw error;
    }
  };

  return (
    <div className={`ai-widget-container theme-${config.theme}`}>
      {/* Floating Button */}
      {!state.isOpen && (
        <FloatingButton
          onClick={toggleWidget}
          theme={config.theme}
          hasNotification={state.insights.some((i: AIInsight) => i.priority === 'high')}
        />
      )}

      {/* Widget Panel */}
      {state.isOpen && (
        <WidgetPanel
          config={config}
          state={state}
          onClose={toggleWidget}
          onRefresh={handleRefresh}
          onQuery={handleQuery}
        />
      )}
    </div>
  );
};

export default App;
