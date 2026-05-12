/**
 * Chart Renderer Component
 * Renders charts (bar, line, pie) using HTML/CSS
 * For production, replace with Chart.js or similar
 */

import React from 'react';
import type { ChartData } from '../../../types';

interface ChartRendererProps {
  data: ChartData;
}

const ChartRenderer: React.FC<ChartRendererProps> = ({ data }) => {
  const maxValue = Math.max(...data.datasets.flatMap((ds) => ds.data));

  if (data.chartType === 'bar') {
    return (
      <div className="chart-renderer chart-bar">
        <div className="chart-legend">
          {data.datasets.map((dataset, i) => (
            <div key={i} className="legend-item">
              <span className={`legend-color color-${i}`}></span>
              {dataset.label}
            </div>
          ))}
        </div>

        <div className="chart-container">
          <div className="chart-bars">
            {data.labels.map((label, labelIndex) => (
              <div key={labelIndex} className="bar-group">
                <div className="bars">
                  {data.datasets.map((dataset, datasetIndex) => {
                    const value = dataset.data[labelIndex];
                    const percentage = (value / maxValue) * 100;

                    return (
                      <div
                        key={datasetIndex}
                        className={`bar color-${datasetIndex}`}
                        style={{ height: `${percentage}%` }}
                        title={`${dataset.label}: ${value}`}
                      >
                        <span className="bar-value">{value}</span>
                      </div>
                    );
                  })}
                </div>
                <div className="bar-label">{label}</div>
              </div>
            ))}
          </div>
        </div>
      </div>
    );
  }

  if (data.chartType === 'line') {
    return (
      <div className="chart-renderer chart-line">
        <div className="chart-legend">
          {data.datasets.map((dataset, i) => (
            <div key={i} className="legend-item">
              <span className={`legend-color color-${i}`}></span>
              {dataset.label}
            </div>
          ))}
        </div>

        <div className="chart-placeholder">
          <svg width="100%" height="200" viewBox="0 0 400 200">
            {data.datasets.map((dataset, datasetIndex) => {
              const points = dataset.data.map((value, index) => {
                const x = (index / (data.labels.length - 1)) * 380 + 10;
                const y = 180 - (value / maxValue) * 160;
                return `${x},${y}`;
              }).join(' ');

              return (
                <polyline
                  key={datasetIndex}
                  points={points}
                  fill="none"
                  stroke={`var(--color-${datasetIndex})`}
                  strokeWidth="2"
                />
              );
            })}
          </svg>

          <div className="chart-x-labels">
            {data.labels.map((label, i) => (
              <span key={i}>{label}</span>
            ))}
          </div>
        </div>
      </div>
    );
  }

  // Fallback for pie or unsupported types
  return (
    <div className="chart-renderer chart-placeholder">
      <p className="text-muted">Chart visualization: {data.chartType}</p>
      <pre className="text-small">{JSON.stringify(data, null, 2)}</pre>
    </div>
  );
};

export default ChartRenderer;
