/**
 * Table Renderer Component
 * Renders tabular data with sorting and formatting
 */

import React, { useState } from 'react';
import type { TableData, TableColumn } from '../../../types/modal';

interface TableRendererProps {
  data: TableData;
}

const TableRenderer: React.FC<TableRendererProps> = ({ data }) => {
  const [sortColumn, setSortColumn] = useState<string | null>(null);
  const [sortDirection, setSortDirection] = useState<'asc' | 'desc'>('asc');

  const handleSort = (columnKey: string) => {
    if (sortColumn === columnKey) {
      setSortDirection(sortDirection === 'asc' ? 'desc' : 'asc');
    } else {
      setSortColumn(columnKey);
      setSortDirection('asc');
    }
  };

  const formatCellValue = (value: any, column: TableColumn): string => {
    if (value === null || value === undefined) return '-';

    switch (column.type) {
      case 'currency':
        return new Intl.NumberFormat('en-US', {
          style: 'currency',
          currency: 'USD',
        }).format(value);

      case 'percentage':
        return `${(value * 100).toFixed(2)}%`;

      case 'number':
        return new Intl.NumberFormat('en-US').format(value);

      case 'date':
        return new Date(value).toLocaleDateString();

      default:
        return String(value);
    }
  };

  const sortedRows = sortColumn
    ? [...data.rows].sort((a, b) => {
        const aVal = a[sortColumn];
        const bVal = b[sortColumn];

        if (aVal === bVal) return 0;
        const comparison = aVal > bVal ? 1 : -1;
        return sortDirection === 'asc' ? comparison : -comparison;
      })
    : data.rows;

  return (
    <div className="table-renderer">
      <div className="table-wrapper">
        <table className="data-table">
          <thead>
            <tr>
              {data.columns.map((column) => (
                <th
                  key={column.key}
                  className={column.sortable ? 'sortable' : ''}
                  onClick={() => column.sortable && handleSort(column.key)}
                  style={column.width ? { width: `${column.width}px` } : undefined}
                >
                  {column.label}
                  {column.sortable && sortColumn === column.key && (
                    <span className="sort-indicator">
                      {sortDirection === 'asc' ? ' ↑' : ' ↓'}
                    </span>
                  )}
                </th>
              ))}
            </tr>
          </thead>
          <tbody>
            {sortedRows.map((row, rowIndex) => (
              <tr key={rowIndex}>
                {data.columns.map((column) => (
                  <td key={column.key} className={`cell-${column.type}`}>
                    {formatCellValue(row[column.key], column)}
                  </td>
                ))}
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {data.totalRows && data.totalRows > data.rows.length && (
        <div className="table-footer">
          <span className="text-muted">
            Showing {data.rows.length} of {data.totalRows} rows
          </span>
        </div>
      )}
    </div>
  );
};

export default TableRenderer;
