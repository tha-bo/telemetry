import { useEffect, useMemo, useState } from 'react'
import './App.css'

const API_BASE_URL = 'http://localhost:5274'

type TelemetryEvent = {
  eventId: string
  customerId: string
  deviceId: string
  recordedAt: number
  value: number
}

type DateRangeKey = '24h' | '1m' | '1y'

function App() {
  const [tenants, setTenants] = useState<string[]>([])
  const [selectedTenant, setSelectedTenant] = useState<string>('')
  const [telemetry, setTelemetry] = useState<TelemetryEvent[]>([])
  const [loadingTenants, setLoadingTenants] = useState(false)
  const [loadingTelemetry, setLoadingTelemetry] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [selectedDevice, setSelectedDevice] = useState<string>('')
  const [range, setRange] = useState<DateRangeKey>('24h')

  // Load tenants on first render
  useEffect(() => {
    const loadTenants = async () => {
      try {
        setLoadingTenants(true)
        setError(null)

        const res = await fetch(`${API_BASE_URL}/customers`)
        if (!res.ok) {
          throw new Error(`Failed to load customers (${res.status})`)
        }

        const data = (await res.json()) as string[]
        setTenants(data)
        if (data.length) {
          setSelectedTenant(data[0])
        }
      } catch (err) {
        setError(err instanceof Error ? err.message : 'Failed to load customers')
      } finally {
        setLoadingTenants(false)
      }
    }

    loadTenants()
  }, [])

  // Load telemetry for the selected tenant and date range
  useEffect(() => {
    if (!selectedTenant) return

    const loadTelemetry = async () => {
      try {
        setLoadingTelemetry(true)
        setError(null)

        const now = Date.now()
        let from: number

        switch (range) {
          case '1m':
            from = now - 30 * 24 * 60 * 60 * 1000
            break
          case '1y':
            from = now - 365 * 24 * 60 * 60 * 1000
            break
          case '24h':
          default:
            from = now - 24 * 60 * 60 * 1000
            break
        }

        const to = now

        const url = new URL(
          `${API_BASE_URL}/customers/${encodeURIComponent(selectedTenant)}/telemetry`
        )
        url.searchParams.set('from', String(from))
        url.searchParams.set('to', String(to))

        const res = await fetch(url)
        if (!res.ok) {
          throw new Error(`Failed to load telemetry (${res.status})`)
        }

        const data = (await res.json()) as TelemetryEvent[]
        setTelemetry(data)
      } catch (err) {
        setError(
          err instanceof Error ? err.message : 'Failed to load telemetry data'
        )
        setTelemetry([])
      } finally {
        setLoadingTelemetry(false)
      }
    }

    loadTelemetry()
  }, [selectedTenant, range])

  useEffect(() => {
    // Reset device selection when tenant changes
    setSelectedDevice('')
  }, [selectedTenant])

  const devices = useMemo(
    () =>
      Array.from(new Set(telemetry.map((t) => t.deviceId))).sort((a, b) =>
        a.localeCompare(b)
      ),
    [telemetry]
  )

  const filteredTelemetry = useMemo(
    () =>
      selectedDevice
        ? telemetry.filter((t) => t.deviceId === selectedDevice)
        : telemetry,
    [telemetry, selectedDevice]
  )

  const summaryStats = useMemo(() => {
    if (!filteredTelemetry.length) return null

    const values = filteredTelemetry.map((t) => t.value)
    const min = Math.min(...values)
    const max = Math.max(...values)
    const avg = values.reduce((sum, v) => sum + v, 0) / values.length

    return {
      min,
      max,
      avg,
      count: values.length,
    }
  }, [filteredTelemetry])

  const telemetryRows = useMemo(
    () =>
      filteredTelemetry.map((t) => ({
        ...t,
        recordedAtLocal: new Date(t.recordedAt).toLocaleString(),
      })),
    [filteredTelemetry]
  )

  const chartData = useMemo(() => {
    if (!filteredTelemetry.length) return null

    const sorted = [...filteredTelemetry].sort(
      (a, b) => a.recordedAt - b.recordedAt
    )
    const minTime = sorted[0].recordedAt
    const maxTime = sorted[sorted.length - 1].recordedAt

    const values = sorted.map((t) => t.value)
    const minValue = Math.min(...values)
    const maxValue = Math.max(...values)

    const width = 800
    const height = 240
    const paddingLeft = 48
    const paddingRight = 16
    const paddingTop = 16
    const paddingBottom = 32
    const innerWidth = width - paddingLeft - paddingRight
    const innerHeight = height - paddingTop - paddingBottom

    const timeRange = maxTime - minTime || 1
    const valueRange = maxValue - minValue || 1

    const points = sorted.map((t) => {
      const xNorm = (t.recordedAt - minTime) / timeRange
      const yNorm = (t.value - minValue) / valueRange
      const x = paddingLeft + xNorm * innerWidth
      const y = paddingTop + (1 - yNorm) * innerHeight

      return {
        x,
        y,
        value: t.value,
        timestamp: new Date(t.recordedAt),
      }
    })

    const pathD = points
      .map((p, idx) => `${idx === 0 ? 'M' : 'L'} ${p.x} ${p.y}`)
      .join(' ')

    const firstTimeLabel = points[0]?.timestamp.toLocaleTimeString()
    const lastTimeLabel =
      points[points.length - 1]?.timestamp.toLocaleTimeString()

    return {
      width,
      height,
      paddingLeft,
      paddingTop,
      paddingBottom,
      innerWidth,
      innerHeight,
      minValue,
      maxValue,
      pathD,
      points,
      firstTimeLabel,
      lastTimeLabel,
    }
  }, [filteredTelemetry])

  const rangeLabel = useMemo(() => {
    switch (range) {
      case '1m':
        return 'Last 1 month'
      case '1y':
        return 'Last 1 year'
      case '24h':
      default:
        return 'Last 24 hours'
    }
  }, [range])

  return (
    <div className="app">
      <header className="app-header">
        <h1>Telemetry Viewer</h1>
        <p>
          {rangeLabel} of telemetry per tenant / device
        </p>
      </header>

      <main className="app-main">
        <section className="controls">
          <label className="field">
            <span className="field-label">Tenant</span>
            <select
              value={selectedTenant}
              onChange={(e) => setSelectedTenant(e.target.value)}
              disabled={loadingTenants || tenants.length === 0}
            >
              {tenants.length === 0 && <option>Loading...</option>}
              {tenants.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span className="field-label">Device</span>
            <select
              value={selectedDevice}
              onChange={(e) => setSelectedDevice(e.target.value)}
              disabled={devices.length === 0}
            >
              <option value="">All devices</option>
              {devices.map((d) => (
                <option key={d} value={d}>
                  {d}
                </option>
              ))}
            </select>
          </label>
          <label className="field">
            <span className="field-label">Date range</span>
            <select
              value={range}
              onChange={(e) => setRange(e.target.value as DateRangeKey)}
            >
              <option value="24h">Last 24 hours</option>
              <option value="1m">Last 1 month</option>
              <option value="1y">Last 1 year</option>
            </select>
          </label>
        </section>

        {error && <div className="error-banner">{error}</div>}

        <section className="table-section">
          <div className="table-header">
            <h2>Telemetry events</h2>
            {loadingTelemetry && <span className="badge">Loading…</span>}
            {!loadingTelemetry && filteredTelemetry.length === 0 && (
              <span className="badge badge-muted">
                No data for this selection
              </span>
            )}
          </div>

          <div className="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th>Time</th>
                  <th>Device</th>
                  <th>Value</th>
                  <th>Event Id</th>
                </tr>
              </thead>
              <tbody>
                {telemetryRows.map((t) => (
                  <tr key={t.eventId}>
                    <td>{t.recordedAtLocal}</td>
                    <td>{t.deviceId}</td>
                    <td>{t.value}</td>
                    <td>{t.eventId}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        <section className="chart-section">
          <div className="chart-header">
            <h2>Value over time</h2>
            {!loadingTelemetry && filteredTelemetry.length > 0 && (
              <span className="badge badge-muted">
                {filteredTelemetry.length} points
              </span>
            )}
          </div>

          {summaryStats && (
            <div className="stats-row">
              <div className="stat">
                <span className="stat-label">Min</span>
                <span className="stat-value">
                  {summaryStats.min.toFixed(2)}
                </span>
              </div>
              <div className="stat">
                <span className="stat-label">Max</span>
                <span className="stat-value">
                  {summaryStats.max.toFixed(2)}
                </span>
              </div>
              <div className="stat">
                <span className="stat-label">Average</span>
                <span className="stat-value">
                  {summaryStats.avg.toFixed(2)}
                </span>
              </div>
            </div>
          )}

          {loadingTelemetry && telemetry.length === 0 ? (
            <p className="chart-placeholder">Loading chart…</p>
          ) : filteredTelemetry.length === 0 || !chartData ? (
            <p className="chart-placeholder">No data to display.</p>
          ) : (
            <svg
              className="chart"
              viewBox={`0 0 ${chartData.width} ${chartData.height}`}
              role="img"
              aria-label="Telemetry values over time"
            >
              <line
                className="chart-axis"
                x1={chartData.paddingLeft}
                x2={chartData.paddingLeft + chartData.innerWidth}
                y1={chartData.height - chartData.paddingBottom}
                y2={chartData.height - chartData.paddingBottom}
              />
              <line
                className="chart-axis"
                x1={chartData.paddingLeft}
                x2={chartData.paddingLeft}
                y1={chartData.paddingTop}
                y2={chartData.height - chartData.paddingBottom}
              />

              <path className="chart-line" d={chartData.pathD} />

              {chartData.points.map((p, idx) => (
                <circle
                  key={idx}
                  className="chart-point"
                  cx={p.x}
                  cy={p.y}
                  r={3}
                />
              ))}

              {chartData.firstTimeLabel && (
                <text
                  className="chart-axis-label"
                  x={chartData.paddingLeft}
                  y={chartData.height - 8}
                  textAnchor="start"
                >
                  {chartData.firstTimeLabel}
                </text>
              )}
              {chartData.lastTimeLabel && (
                <text
                  className="chart-axis-label"
                  x={chartData.paddingLeft + chartData.innerWidth}
                  y={chartData.height - 8}
                  textAnchor="end"
                >
                  {chartData.lastTimeLabel}
                </text>
              )}

              <text
                className="chart-axis-label"
                x={chartData.paddingLeft - 8}
                y={chartData.paddingTop + 8}
                textAnchor="end"
              >
                {chartData.maxValue.toFixed(1)}
              </text>
              <text
                className="chart-axis-label"
                x={chartData.paddingLeft - 8}
                y={chartData.height - chartData.paddingBottom}
                textAnchor="end"
              >
                {chartData.minValue.toFixed(1)}
              </text>
            </svg>
          )}
        </section>
      </main>
    </div>
  )
}

export default App
