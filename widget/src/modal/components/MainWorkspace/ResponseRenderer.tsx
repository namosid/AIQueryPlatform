/**
 * Response Renderer Component
 * Dynamically renders visualizations (tables, charts, KPIs)
 */

import React from 'react';
import type { VisualizationData } from '../../../types/modal';
import TableRenderer from './TableRenderer';
import ChartRenderer from './ChartRenderer';
import TabView, { Tab } from '../../../components/TabView';

interface ResponseRendererProps {
  visualization: VisualizationData;
}

const ResponseRenderer: React.FC<ResponseRendererProps> = ({ visualization }) => {
  console.log('[ResponseRenderer] Rendering visualization:', {
    type: visualization.type,
    hasTable: !!visualization.table,
    hasChart: !!visualization.chart,
    hasComponents: !!visualization.components,
    visualization
  });

  switch (visualization.type) {
    case 'table':
      return visualization.table ? (
        <TableRenderer data={visualization.table} />
      ) : null;

    case 'chart':
      return visualization.chart ? (
        <ChartRenderer data={visualization.chart} />
      ) : null;

    case 'kpi':
      return visualization.kpis ? (
        <div className="kpi-grid">
          {visualization.kpis.map((kpi) => (
            <div key={kpi.id} className="kpi-card">
              <div className="kpi-title">{kpi.title}</div>
              <div className="kpi-value">
                {kpi.value}
                {kpi.unit && <span className="kpi-unit">{kpi.unit}</span>}
              </div>
              {kpi.change && (
                <div className={`kpi-change ${kpi.change.direction}`}>
                  {kpi.change.direction === 'up' ? '↑' : '↓'} {Math.abs(kpi.change.value)}%
                  <span className="kpi-period"> vs {kpi.change.period}</span>
                </div>
              )}
            </div>
          ))}
        </div>
      ) : null;

    case 'mixed':
      if (!visualization.components) return null;
      
      // Check if we have both table and chart components
      const tableComponent = visualization.components.find((c: any) => c.type === 'table');
      const chartComponent = visualization.components.find((c: any) => c.type === 'chart');
      
      // If we have both, render in tabs
      if (tableComponent && chartComponent) {
        const tabs: Tab[] = [
          {
            id: 'results',
            label: 'Results',
            icon: (
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <rect x="3" y="3" width="7" height="7" stroke="currentColor" strokeWidth="2" />
                <rect x="14" y="3" width="7" height="7" stroke="currentColor" strokeWidth="2" />
                <rect x="3" y="14" width="7" height="7" stroke="currentColor" strokeWidth="2" />
                <rect x="14" y="14" width="7" height="7" stroke="currentColor" strokeWidth="2" />
              </svg>
            ),
            content: (
              <div className="viz-component">
                <TableRenderer data={tableComponent.data as any} />
              </div>
            )
          },
          {
            id: 'chart',
            label: 'Chart',
            icon: (
              <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                <path d="M3 3v18h18" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
                <path d="M7 16V11M12 16V8M17 16V13" stroke="currentColor" strokeWidth="2" strokeLinecap="round" />
              </svg>
            ),
            content: (
              <div className="viz-component">
                <ChartRenderer data={chartComponent.data as any} />
              </div>
            )
          }
        ];
        
        return <TabView tabs={tabs} defaultTab="results" className="modal-result-tabs" />;
      }
      
      // Otherwise, render all components in a grid
      return (
        <div className="mixed-visualization">
          {visualization.components.map((component) => (
            <div key={component.id} className="viz-component">
              <h4>{component.title}</h4>
              <ResponseRenderer
                visualization={{
                  type: component.type,
                  table: component.type === 'table' ? component.data as any : undefined,
                  chart: component.type === 'chart' ? component.data as any : undefined,
                  kpis: component.type === 'kpi' ? component.data as any : undefined,
                }}
              />
            </div>
          ))}
        </div>
      );

    default:
      return null;
  }
};

export default ResponseRenderer;
