import { useEffect, useState } from 'react'
import { ApiError, request, mutate } from './api'
import EncabezadoPagina from './components/EncabezadoPagina'
import { AlertaError, Aviso } from './components/Alerta'

type Widget = { id: number; codigo: string; descripcion: string; tamano: string; habilitado: boolean; orden: number; version: string | null }
type DashboardData = { id: number; descripcion: string; puedeConfigurar: boolean; widgets: Widget[] }
type WidgetData = { estado: string; valor: number | null; detalle: string; elementos: string[]; unidad?: string }
type Props = { onExpired: () => void }
const icons: Record<string, string> = { pacientes_activos: '◎', cantidad_dispensada: '↗', stock_disponible: '▤', sucursales: '⌂', alertas: '!', ultimas_dispensaciones: '≡' }

function WidgetCard({ widget, onExpired }: { widget: Widget } & Props) {
  const [data, setData] = useState<WidgetData | null>(null)
  const [error, setError] = useState('')
  const [attempt, setAttempt] = useState(0)
  useEffect(() => {
    const controller = new AbortController()
    request(`/api/dashboard/widgets/${encodeURIComponent(widget.codigo)}`, { signal: controller.signal })
      .then(response => response.json()).then((value: WidgetData) => { if (!controller.signal.aborted) setData(value) })
      .catch(reason => {
        if (controller.signal.aborted) return
        if (reason instanceof ApiError && reason.status === 401) onExpired()
        else setError(reason instanceof Error ? reason.message : 'No se pudo cargar este indicador.')
      })
    return () => controller.abort()
  }, [widget.codigo, attempt, onExpired])
  return <article className={`widget ${widget.tamano === 'ancho' ? 'widget-wide' : ''}`}>
    <div className="widget-heading"><span className="widget-icon" aria-hidden="true">{icons[widget.codigo] ?? '◈'}</span><h2>{widget.descripcion}</h2></div>
    <div aria-live="polite">{error ? <><AlertaError>{error}</AlertaError>
      <button className="secondary" onClick={() => { setError(''); setData(null); setAttempt(value => value + 1) }}>Reintentar</button></>
      : !data ? <p className="widget-loading">Cargando indicador…</p> : <>
        <p className={`metric ${data.valor === null ? 'metric-empty' : ''}`}>{data.valor === null ? 'Sin datos' : <>{data.valor.toLocaleString('es-AR')}{data.unidad && <span className="metric-unit">{data.unidad}</span>}</>}</p>
        <p className="widget-detail">{data.detalle}</p>
        {data.elementos.length > 0 && <ul className="branch-list">{data.elementos.map((item, index) => <li key={`${index}-${item}`}>{item}</li>)}</ul>}
        <span className={`widget-status ${data.estado === 'disponible' ? 'connected' : ''}`}>{data.estado === 'disponible' ? 'Datos actuales' : 'Módulo pendiente'}</span>
      </>}</div>
  </article>
}

function Configuration({ onClose, onSaved, onExpired }: { onClose: () => void; onSaved: () => void } & Props) {
  const [widgets, setWidgets] = useState<Widget[] | null>(null)
  const [error, setError] = useState('')
  const [saving, setSaving] = useState(false)
  const [retry, setRetry] = useState(0)
  const [conflict, setConflict] = useState(false)
  useEffect(() => {
    const controller = new AbortController()
    request('/api/dashboard/configuracion', { signal: controller.signal }).then(response => response.json())
      .then((items: Widget[]) => { if (!controller.signal.aborted) setWidgets(items) })
      .catch(reason => {
        if (controller.signal.aborted) return
        if (reason instanceof ApiError && reason.status === 401) onExpired()
        else setError(reason instanceof Error ? reason.message : 'No se pudo cargar la configuración.')
      })
    return () => controller.abort()
  }, [retry, onExpired])
  function move(index: number, delta: number) {
    if (!widgets) return
    const next = [...widgets]; [next[index], next[index + delta]] = [next[index + delta], next[index]]
    setWidgets(next)
  }
  async function save() {
    if (!widgets) return
    setSaving(true); setError('')
    try {
      await mutate('/api/dashboard/configuracion', { widgets: widgets.map((widget, orden) => ({ id: widget.id, habilitado: widget.habilitado, orden, version: widget.version })) }, 'PUT')
      onSaved()
    } catch (reason) {
      if (reason instanceof ApiError && reason.status === 401) onExpired()
      else {
        if (reason instanceof ApiError && (reason.status === 409 || reason.status === 403)) setConflict(true)
        setError(reason instanceof Error ? reason.message : 'No se pudieron guardar los cambios.')
      }
    } finally { setSaving(false) }
  }
  return <section className="configuration" aria-labelledby="configuration-title">
    <div className="section-heading"><div><p className="eyebrow">PERSONALIZACIÓN</p><h2 id="configuration-title">Widgets de la asociación</h2></div>
      <button className="secondary" disabled={saving} onClick={onClose}>Cancelar</button></div>
    <p>Elegí qué mostrar y en qué orden. Los cambios se comparten con todos los operadores de tu asociación.</p>
    {error && <div role="alert"><AlertaError>{error}</AlertaError><button className="secondary" disabled={saving} onClick={() => {
      setWidgets(null); setError(''); setConflict(false); setRetry(value => value + 1)
    }}>Recargar configuración</button></div>}
    {!widgets && !error && <p role="status">Cargando configuración…</p>}
    {widgets && <><div className="widget-options">{widgets.map((widget, index) => <div className="widget-option" key={widget.id}>
      <div className="option-title"><strong>{widget.descripcion}</strong><small>{widget.habilitado ? 'Visible en el dashboard' : 'Oculto'}</small></div>
      <div className="option-actions"><button className="order-button secondary" aria-label={`Subir ${widget.descripcion}`} disabled={saving || conflict || index === 0} onClick={() => move(index, -1)}>↑</button>
        <button className="order-button secondary" aria-label={`Bajar ${widget.descripcion}`} disabled={saving || conflict || index === widgets.length - 1} onClick={() => move(index, 1)}>↓</button>
        <button role="switch" aria-checked={widget.habilitado} aria-label={`Mostrar ${widget.descripcion}`} className={`switch ${widget.habilitado ? 'on' : ''}`}
          disabled={saving || conflict} onClick={() => setWidgets(widgets.map(item => item.id === widget.id ? { ...item, habilitado: !item.habilitado } : item))}><span /></button>
      </div></div>)}</div><div className="configuration-footer"><small>Apagar un widget no borra datos ni desactiva su módulo.</small>
      <button disabled={saving || conflict} onClick={() => void save()}>{saving ? 'Guardando…' : 'Guardar cambios'}</button></div></>}
  </section>
}

export default function Dashboard({ onExpired }: Props) {
  const [data, setData] = useState<DashboardData | null>(null)
  const [error, setError] = useState('')
  const [revision, setRevision] = useState(0)
  const [configuring, setConfiguring] = useState(false)
  const [notice, setNotice] = useState('')
  useEffect(() => {
    const controller = new AbortController()
    request('/api/dashboard', { signal: controller.signal }).then(response => response.json())
      .then((value: DashboardData) => { if (!controller.signal.aborted) setData(value) })
      .catch(reason => {
        if (controller.signal.aborted) return
        if (reason instanceof ApiError && reason.status === 401) onExpired()
        else setError(reason instanceof Error ? reason.message : 'No se pudo cargar el dashboard.')
      })
    return () => controller.abort()
  }, [revision, onExpired])
  function reload() { setData(null); setError(''); setRevision(value => value + 1) }
  return <main className="dashboard">
    <EncabezadoPagina eyebrow="RESUMEN DE LA ASOCIACIÓN" titulo={data?.descripcion ?? 'Dashboard'}
      subtitulo="La información de tu organización, en un solo lugar.">
      {!configuring && <><button className="secondary" onClick={reload}>Actualizar</button>
        {data?.puedeConfigurar && <button onClick={() => { setNotice(''); setConfiguring(true) }}>Configurar widgets</button>}</>}
    </EncabezadoPagina>
    {notice && <Aviso>{notice}</Aviso>}
    {error && <AlertaError>{error}</AlertaError>}
    {configuring ? <Configuration onExpired={onExpired} onClose={() => setConfiguring(false)} onSaved={() => {
      setConfiguring(false); setNotice('Configuración guardada para tu asociación.'); reload()
    }} /> : !data && !error ? <p role="status">Cargando tu dashboard…</p> : data && <>
      <div className="dashboard-meta"><span>Vista general · Todas las sucursales</span><span>{data.widgets.length} widgets habilitados</span></div>
      {data.widgets.length === 0 ? <section className="empty-dashboard"><h2>Tu dashboard está vacío</h2><p>{data.puedeConfigurar ? 'Activá los widgets que quieras ver desde Configurar widgets.' : 'Un administrador de tu asociación puede activar los widgets.'}</p></section>
        : <div className="widget-grid">{data.widgets.map(widget => <WidgetCard key={`${revision}-${widget.id}`} widget={widget} onExpired={onExpired} />)}</div>}
    </>}
  </main>
}
