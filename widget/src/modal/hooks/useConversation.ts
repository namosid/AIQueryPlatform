/**
 * useConversation Hook
 * Manages conversation state and message flow
 */

import { useState, useCallback } from 'react';
import type { Conversation, Message, UseConversationResult } from '../../types/modal';
import type { ModalAPIService } from '../../services/modalApi';

export const useConversation = (apiService: ModalAPIService): UseConversationResult => {
  const [conversation, setConversation] = useState<Conversation | null>(null);
  const [messages, setMessages] = useState<Message[]>([]);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  /**
   * Create a new conversation
   */
  const createConversation = useCallback(async (): Promise<Conversation> => {
    setIsLoading(true);
    setError(null);

    try {
      const newConversation = await apiService.createConversation();
      setConversation(newConversation);
      setMessages([]);
      
      console.log('[useConversation] Created conversation:', newConversation.id);
      return newConversation;
    } catch (err) {
      const errorMsg = 'Failed to create conversation';
      setError(errorMsg);
      console.error('[useConversation]', err);
      throw err;
    } finally {
      setIsLoading(false);
    }
  }, [apiService]);

  /**
   * Load an existing conversation
   */
  const loadConversation = useCallback(async (id: string): Promise<void> => {
    setIsLoading(true);
    setError(null);

    try {
      const loadedConversation = await apiService.loadConversation(id);
      
      if (loadedConversation) {
        setConversation(loadedConversation);
        
        // Transform messages to include visualization field
        const transformedMessages = loadedConversation.messages.map((msg: any) => {
          // If it's an assistant message with data, build the visualization
          if (msg.role === 'assistant' && msg.data) {
            const visualization = buildVisualizationFromData(msg.data, msg.chartData);
            console.log('[useConversation] Built visualization for message:', msg.id, visualization);
            return {
              ...msg,
              visualization,
            };
          }
          return msg;
        });
        
        setMessages(transformedMessages);
        console.log('[useConversation] Loaded conversation with', transformedMessages.length, 'messages');
      } else {
        throw new Error('Conversation not found');
      }
    } catch (err) {
      const errorMsg = 'Failed to load conversation';
      setError(errorMsg);
      console.error('[useConversation]', err);
    } finally {
      setIsLoading(false);
    }
  }, [apiService]);

  /**
   * Send a message in the current conversation
   * Uses the unified endpoint that executes query via LLM and saves messages
   */
  const sendMessage = useCallback(async (query: string, conversationOverride?: Conversation) => {
    const activeConversation = conversationOverride || conversation;
    
    if (!activeConversation) {
      console.error('[useConversation] No active conversation');
      return;
    }
    
    console.log('[useConversation] Sending message to conversation:', activeConversation.id);

    setIsLoading(true);
    setError(null);

    // Optimistically add user message to UI
    const optimisticUserMessage: Message = {
      id: `temp-${generateId()}`,
      role: 'user',
      content: query,
      query,
      timestamp: new Date().toISOString(),
    };

    setMessages((prev) => [...prev, optimisticUserMessage]);

    try {
      // Call unified endpoint that handles everything
      const response = await apiService.executeQueryInConversation(activeConversation.id, query);

      console.log('[useConversation] Query response:', response);

      // Replace optimistic user message with actual saved message
      setMessages((prev) => {
        const withoutOptimistic = prev.filter(m => m.id !== optimisticUserMessage.id);
        
        // Convert API response to UI message format
        const userMsg: Message = {
          ...response.userMessage,
          query,
        };

        // Build visualization using helper function
        const visualization = response.queryResult.result 
          ? buildVisualizationFromData(response.queryResult.result, response.queryResult.chartData)
          : undefined;
        
        console.log('[useConversation] Created visualization:', visualization);

        const assistantMsg: Message = {
          ...response.assistantMessage,
          sql: response.queryResult.generatedSql,
          visualization,
          metadata: {
            executionTime: response.queryResult.executionTimeMs,
            rowCount: response.queryResult.result?.rowCount || 0,
            confidence: 90,
          },
        };

        console.log('[useConversation] Assistant message with visualization:', assistantMsg);

        return [...withoutOptimistic, userMsg, assistantMsg];
      });

      console.log('[useConversation] Message sent and response received successfully');
    } catch (err) {
      const errorMsg = 'Failed to send message';
      setError(errorMsg);
      console.error('[useConversation]', err);

      // Remove optimistic message and add error message
      setMessages((prev) => {
        const withoutOptimistic = prev.filter(m => m.id !== optimisticUserMessage.id);
        
        const errorMessage: Message = {
          id: generateId(),
          role: 'assistant',
          content: 'Sorry, I encountered an error processing your request. Please try again.',
          timestamp: new Date().toISOString(),
        };
        
        return [...withoutOptimistic, errorMessage];
      });
    } finally {
      setIsLoading(false);
    }
  }, [conversation, apiService]);

  /**
   * Clear current conversation
   */
  const clearConversation = useCallback(() => {
    setConversation(null);
    setMessages([]);
    setError(null);
    console.log('[useConversation] Conversation cleared');
  }, []);

  return {
    conversation,
    messages,
    isLoading,
    error,
    sendMessage,
    loadConversation,
    createConversation,
    clearConversation,
  };
};

// Utility functions

/**
 * Build visualization object from API data
 */
function buildVisualizationFromData(data: any, chartData: any): any {
  if (!data) return undefined;

  const hasValidChart = chartData && 
                        chartData.labels && 
                        chartData.labels.length > 0 &&
                        chartData.datasets &&
                        chartData.datasets.length > 0;

  console.log('[buildVisualizationFromData] hasValidChart:', hasValidChart, {
    hasChartData: !!chartData,
    hasLabels: chartData?.labels?.length,
    hasDatasets: chartData?.datasets?.length
  });

  if (hasValidChart) {
    // Show chart with table as fallback
    return {
      type: 'mixed',
      components: [
        {
          id: 'chart',
          title: 'Chart Visualization',
          type: 'chart' as const,
          data: chartData,
        },
        {
          id: 'table',
          title: 'Data Table',
          type: 'table' as const,
          data: {
            columns: data.columns.map((col: string) => ({
              key: col,
              label: col,
              type: 'string' as const,
              sortable: true,
            })),
            rows: data.rows || [],
          },
        },
      ],
    };
  } else {
    // Show table only
    return {
      type: 'table',
      table: {
        columns: data.columns.map((col: string) => ({
          key: col,
          label: col,
          type: 'string' as const,
          sortable: true,
        })),
        rows: data.rows || [],
      },
    };
  }
}

function generateId(): string {
  return `${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
}
