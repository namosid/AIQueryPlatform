/**
 * Response Renderer Component
 * Dynamically renders visualizations (tables, charts, KPIs)
 */

import React from 'react';
import type { VisualizationData } from '../../../types/modal';
import TableRenderer from './TableRenderer';
import ChartRenderer from './ChartRenderer';

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
      return visualization.components ? (
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
      ) : null;

    default:
      return null;
  }
};

export default ResponseRenderer;
