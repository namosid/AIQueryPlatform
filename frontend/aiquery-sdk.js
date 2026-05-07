/**
 * AI Query UI SDK
 * Frontend SDK for interacting with the AI Query Platform API
 */

class AIQueryUI {
    constructor(config) {
        this.apiUrl = config.apiUrl;
        this.apiKey = config.apiKey;
        this.resultContainerId = config.resultContainerId || 'result';
        this.logContainerId = config.logContainerId || 'logs';
        this.enableLogs = config.enableLogs !== false;
    }

    /**
     * Initialize the SDK
     */
    init() {
        console.log('AI Query UI SDK initialized');
        this.clearResult();
        this.clearLogs();
    }

    /**
     * Send query with streaming response
     */
    async sendQuery(query) {
        this.clearResult();
        this.clearLogs();

        try {
            const response = await fetch(`${this.apiUrl}/api/query/execute-stream`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/x-ndjson',
                    'x-api-key': this.apiKey
                },
                credentials: 'include',
                body: JSON.stringify({ query })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                
                if (done) break;

                buffer += decoder.decode(value, { stream: true });
                const lines = buffer.split('\n');
                buffer = lines.pop() || ''; // Keep incomplete line in buffer

                for (const line of lines) {
                    if (line.trim()) {
                        try {
                            const event = JSON.parse(line);
                            this.handleStreamEvent(event);
                        } catch (e) {
                            console.error('Failed to parse stream event:', e);
                        }
                    }
                }
            }
        } catch (error) {
            this.showError(`Error: ${error.message}`);
            console.error('Query execution error:', error);
        }
    }

    /**
     * Send query without streaming
     */
    async sendQuerySync(query) {
        this.clearResult();
        
        try {
            const response = await fetch(`${this.apiUrl}/api/query/execute`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json',
                    'x-api-key': this.apiKey
                },
                credentials: 'include',
                body: JSON.stringify({ query })
            });

            const result = await response.json();

            if (!result.success) {
                this.showError(result.errorMessage || 'Query failed');
                return;
            }

            this.renderResult(result.result, result.visualizationType, result.chartData);
        } catch (error) {
            this.showError(`Error: ${error.message}`);
            console.error('Query execution error:', error);
        }
    }

    /**
     * Generate PDF report
     */
    async generateReport(query, reportTitle = 'Report') {
        try {
            const response = await fetch(`${this.apiUrl}/api/query/generate-report`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/pdf',
                    'x-api-key': this.apiKey
                },
                credentials: 'include',
                body: JSON.stringify({ query, reportTitle })
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const blob = await response.blob();
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `${reportTitle.replace(/\s+/g, '_')}.pdf`;
            document.body.appendChild(a);
            a.click();
            document.body.removeChild(a);
            window.URL.revokeObjectURL(url);

            this.log('PDF report downloaded successfully', 'success');
        } catch (error) {
            this.showError(`Error generating report: ${error.message}`);
            console.error('Report generation error:', error);
        }
    }

    /**
     * Handle streaming events
     */
    handleStreamEvent(event) {
        console.log('Stream event received:', event);
        
        switch (event.type) {
            case 'log':
                this.log(event.message);
                break;
            case 'sql_generated':
                this.log(`SQL: ${event.sql}`, 'sql');
                break;
            case 'execution_progress':
                this.log(event.message, 'progress');
                break;
            case 'final_result':
                console.log('Final result event:', {
                    visualizationType: event.visualizationType,
                    hasData: !!event.data,
                    hasChartData: !!event.chartData,
                    chartData: event.chartData
                });
                this.renderResult(event.data, event.visualizationType, event.chartData);
                this.log(event.message, 'success');
                break;
            case 'error':
                this.showError(event.message);
                break;
        }
    }

    /**
     * Render query result
     */
    renderResult(data, visualizationType, chartData) {
        console.log('Rendering result:', {
            visualizationType: visualizationType,
            hasChartData: !!chartData,
            chartData: chartData,
            dataColumns: data?.columns,
            rowCount: data?.rows?.length
        });

        const container = document.getElementById(this.resultContainerId);
        if (!container) {
            console.error('Result container not found:', this.resultContainerId);
            return;
        }

        container.innerHTML = '';

        // Normalize visualization type to lowercase for comparison
        const vizType = typeof visualizationType === 'string' ? visualizationType.toLowerCase() : visualizationType;

        if (vizType === 'chart' || visualizationType === 2) { // 2 is VisualizationType.Chart enum value
            this.renderChart(container, data, chartData);
        } else {
            this.renderTable(container, data);
        }
    }

    /**
     * Render table
     */
    renderTable(container, data) {
        if (!data || !data.columns || data.columns.length === 0) {
            container.innerHTML = '<p class="no-data">No data available</p>';
            return;
        }

        const table = document.createElement('table');
        table.className = 'data-table';

        // Header
        const thead = document.createElement('thead');
        const headerRow = document.createElement('tr');
        data.columns.forEach(col => {
            const th = document.createElement('th');
            th.textContent = col;
            headerRow.appendChild(th);
        });
        thead.appendChild(headerRow);
        table.appendChild(thead);

        // Body
        const tbody = document.createElement('tbody');
        data.rows.forEach((row, idx) => {
            const tr = document.createElement('tr');
            tr.className = idx % 2 === 0 ? 'even' : 'odd';
            
            data.columns.forEach(col => {
                const td = document.createElement('td');
                td.textContent = row[col] !== null && row[col] !== undefined ? row[col] : '';
                tr.appendChild(td);
            });
            
            tbody.appendChild(tr);
        });
        table.appendChild(tbody);

        container.appendChild(table);

        // Row count
        const rowCount = document.createElement('p');
        rowCount.className = 'row-count';
        rowCount.textContent = `Total rows: ${data.rowCount || data.rows.length}`;
        container.appendChild(rowCount);
    }

    /**
     * Render chart using Chart.js
     */
    renderChart(container, data, chartData) {
        // Use chartData if provided, otherwise try to convert from result data
        if (chartData && chartData.datasets && chartData.datasets.length > 0) {
            this.renderChartFromChartData(container, data, chartData);
        } else if (data && data.columns && data.columns.length >= 2) {
            this.renderChartFromResult(container, data);
        } else {
            this.renderTable(container, data);
        }
    }

    /**
     * Render chart from pre-generated chart data
     */
    renderChartFromChartData(container, data, chartData) {
        console.log('Rendering chart from chart data:', chartData);
        
        // Check if Chart.js is loaded
        if (typeof Chart === 'undefined') {
            console.error('Chart.js is not loaded!');
            this.showError('Chart.js library is not loaded. Please refresh the page.');
            this.renderTable(container, data);
            return;
        }

        // Validate chart data
        if (!chartData || !chartData.labels || chartData.labels.length === 0) {
            console.error('Invalid chart data:', chartData);
            this.renderTable(container, data);
            return;
        }

        if (!chartData.datasets || chartData.datasets.length === 0) {
            console.error('No datasets in chart data');
            this.renderTable(container, data);
            return;
        }

        // Create canvas wrapper
        const chartWrapper = document.createElement('div');
        chartWrapper.style.position = 'relative';
        chartWrapper.style.height = '400px';
        chartWrapper.style.marginBottom = '20px';
        
        const canvas = document.createElement('canvas');
        canvas.id = 'resultChart';
        chartWrapper.appendChild(canvas);
        container.appendChild(chartWrapper);

        try {
            // Destroy existing chart if it exists
            const existingChart = Chart.getChart('resultChart');
            if (existingChart) {
                existingChart.destroy();
            }

            // Create chart
            new Chart(canvas, {
                type: chartData.chartType || 'bar',
                data: {
                    labels: chartData.labels,
                    datasets: chartData.datasets.map(ds => ({
                        label: ds.label,
                        data: ds.data,
                        backgroundColor: ds.backgroundColor || 'rgba(54, 162, 235, 0.6)',
                        borderColor: ds.borderColor || 'rgba(54, 162, 235, 1)',
                        borderWidth: 1
                    }))
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    },
                    plugins: {
                        legend: {
                            display: chartData.datasets.length > 1,
                            position: 'top'
                        },
                        title: {
                            display: true,
                            text: 'Data Visualization'
                        }
                    }
                }
            });

            console.log('Chart created successfully');
        } catch (error) {
            console.error('Error creating chart:', error);
            this.showError('Failed to create chart: ' + error.message);
        }

        // Also show table below chart
        const separator = document.createElement('hr');
        separator.style.margin = '20px 0';
        container.appendChild(separator);

        const tableTitle = document.createElement('h4');
        tableTitle.textContent = 'Detailed Data';
        tableTitle.style.marginTop = '20px';
        container.appendChild(tableTitle);

        this.renderTable(container, data);
    }

    /**
     * Render chart from result data (legacy support)
     */
    renderChartFromResult(container, data) {
        console.log('Rendering chart from result data');
        
        if (!data || !data.columns || data.columns.length < 2) {
            console.warn('Data not suitable for chart, falling back to table');
            this.renderTable(container, data);
            return;
        }

        // Check if Chart.js is loaded
        if (typeof Chart === 'undefined') {
            console.error('Chart.js is not loaded!');
            this.showError('Chart.js library is not loaded. Please refresh the page.');
            this.renderTable(container, data);
            return;
        }

        // Create canvas wrapper
        const chartWrapper = document.createElement('div');
        chartWrapper.style.position = 'relative';
        chartWrapper.style.height = '400px';
        chartWrapper.style.marginBottom = '20px';
        
        const canvas = document.createElement('canvas');
        canvas.id = 'resultChart';
        chartWrapper.appendChild(canvas);
        container.appendChild(chartWrapper);

        try {
            // Prepare data - first column as labels, remaining numeric columns as datasets
            const labels = data.rows.map(row => row[data.columns[0]]);
            const datasets = [];

            const colors = [
                { bg: 'rgba(54, 162, 235, 0.6)', border: 'rgba(54, 162, 235, 1)' },
                { bg: 'rgba(255, 99, 132, 0.6)', border: 'rgba(255, 99, 132, 1)' },
                { bg: 'rgba(75, 192, 192, 0.6)', border: 'rgba(75, 192, 192, 1)' },
                { bg: 'rgba(255, 206, 86, 0.6)', border: 'rgba(255, 206, 86, 1)' },
                { bg: 'rgba(153, 102, 255, 0.6)', border: 'rgba(153, 102, 255, 1)' }
            ];

            // Create dataset for each numeric column
            for (let i = 1; i < data.columns.length; i++) {
                const column = data.columns[i];
                const values = data.rows.map(row => {
                    const val = row[column];
                    return typeof val === 'number' ? val : parseFloat(val) || 0;
                });

                // Check if column is numeric
                if (values.some(v => !isNaN(v) && v !== 0)) {
                    const colorIndex = (i - 1) % colors.length;
                    datasets.push({
                        label: column,
                        data: values,
                        backgroundColor: colors[colorIndex].bg,
                        borderColor: colors[colorIndex].border,
                        borderWidth: 1
                    });
                }
            }

            if (datasets.length === 0) {
                console.warn('No numeric data found for chart');
                this.renderTable(container, data);
                return;
            }

            // Destroy existing chart if it exists
            const existingChart = Chart.getChart('resultChart');
            if (existingChart) {
                existingChart.destroy();
            }

            // Create chart
            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: datasets
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    scales: {
                        y: {
                            beginAtZero: true
                        }
                    },
                    plugins: {
                        legend: {
                            display: datasets.length > 1,
                            position: 'top'
                        },
                        title: {
                            display: true,
                            text: 'Data Visualization'
                        }
                    }
                }
            });

            console.log('Chart created successfully with', datasets.length, 'datasets');
        } catch (error) {
            console.error('Error creating chart:', error);
            this.showError('Failed to create chart: ' + error.message);
        }

        // Also show table below chart
        const tableContainer = document.createElement('div');
        tableContainer.style.marginTop = '20px';
        container.appendChild(tableContainer);
        this.renderTable(tableContainer, data);
    }

    /**
     * Log message
     */
    log(message, type = 'info') {
        if (!this.enableLogs) return;

        const logContainer = document.getElementById(this.logContainerId);
        if (!logContainer) return;

        const logEntry = document.createElement('div');
        logEntry.className = `log-entry log-${type}`;
        
        const timestamp = new Date().toLocaleTimeString();
        logEntry.innerHTML = `<span class="log-time">[${timestamp}]</span> <span class="log-message">${message}</span>`;
        
        logContainer.appendChild(logEntry);
        logContainer.scrollTop = logContainer.scrollHeight;
    }

    /**
     * Show error
     */
    showError(message) {
        this.log(`ERROR: ${message}`, 'error');
        
        const container = document.getElementById(this.resultContainerId);
        if (!container) return;

        const errorDiv = document.createElement('div');
        errorDiv.className = 'error-message';
        errorDiv.textContent = message;
        container.innerHTML = '';
        container.appendChild(errorDiv);
    }

    /**
     * Clear result container
     */
    clearResult() {
        const container = document.getElementById(this.resultContainerId);
        if (container) {
            container.innerHTML = '';
        }
    }

    /**
     * Clear logs
     */
    clearLogs() {
        const logContainer = document.getElementById(this.logContainerId);
        if (logContainer) {
            logContainer.innerHTML = '';
        }
    }
}

// Export for module systems
if (typeof module !== 'undefined' && module.exports) {
    module.exports = AIQueryUI;
}
