import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, request, mutate } from './api'
import EncabezadoPagina from './components/EncabezadoPagina'
import { AlertaError, Aviso } from './components/Alerta'
import Confirmacion from './components/Confirmacion'
import EtiquetaEstado from './components/EtiquetaEstado'
import { Campo, EtiquetaCheck, Selector } from './components/Campo'

type Element = { id: number; codigo: string; descripcion: string; tamano: string; tipoIndicador: string; activo: boolean; version: string }
type Source = { codigo: string; descripcion: string }
type Catalog = { elementos: Element[]; indicadores: Source[] }
type Draft = { codigo: string; descripcion: string; tamano: string; tipoIndicador: string; activo: boolean }
const emptyDraft: Draft = { codigo: '', descripcion: '', tamano: 'normal', tipoIndicador: '', activo: true }

export default function ElementCatalog({ onExpired }: { onExpired: () => void }) {
  const [catalog, setCatalog] = useState<Catalog | null>(null)
  const [revision, setRevision] = useState(0)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('todos')
  const [editing, setEditing] = useState<Element | 'new' | null>(null)
  const [draft, setDraft] = useState<Draft>(emptyDraft)
  const [removing, setRemoving] = useState<Element | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [blocked, setBlocked] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    request('/api/plataforma/elementos/', { signal: controller.signal })
      .then(response => response.json()).then((value: Catalog) => { if (!controller.signal.aborted) setCatalog(value) })
      .catch(reason => {
        if (controller.signal.aborted) return
        if (reason instanceof ApiError && reason.status === 401) onExpired()
        else setError(reason instanceof Error ? reason.message : 'No se pudo cargar el catálogo.')
      })
    return () => controller.abort()
  }, [revision, onExpired])
  function reload() {
    setEditing(null); setRemoving(null); setBlocked(false); setError(''); setCatalog(null); setRevision(value => value + 1)
  }
  function edit(item: Element | 'new') {
    setEditing(item); setRemoving(null); setError(''); setNotice(''); setBlocked(false)
    setDraft(item === 'new' ? { ...emptyDraft, tipoIndicador: catalog?.indicadores[0]?.codigo ?? '' } : {
      codigo: item.codigo, descripcion: item.descripcion, tamano: item.tamano, tipoIndicador: item.tipoIndicador, activo: item.activo,
    })
  }
  function handleError(reason: unknown) {
    if (reason instanceof ApiError && reason.status === 401) onExpired()
    else {
      if (reason instanceof ApiError && (reason.status === 403 || reason.status === 404 || (reason.status === 409 && editing !== 'new'))) setBlocked(true)
      setError(reason instanceof Error ? reason.message : 'No se pudo guardar el cambio.')
    }
  }
  async function save(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!editing) return
    setBusy(true); setError('')
    try {
      const isNew = editing === 'new'
      await mutate(isNew ? '/api/plataforma/elementos/' : `/api/plataforma/elementos/${editing.id}`,
        { ...draft, version: isNew ? null : editing.version }, isNew ? 'POST' : 'PUT')
      reload(); setNotice(isNew ? 'Elemento creado. Cada asociación puede activarlo en su configuración.' : 'Elemento actualizado en el catálogo global.')
    } catch (reason) { handleError(reason) }
    finally { setBusy(false) }
  }
  async function deactivate() {
    if (!removing) return
    setBusy(true); setError('')
    try {
      await mutate(`/api/plataforma/elementos/${removing.id}`, undefined, 'DELETE', { 'If-Match': `"${removing.version}"` })
      reload(); setNotice('Elemento dado de baja. Las preferencias de las asociaciones se conservaron.')
    } catch (reason) { handleError(reason) }
    finally { setBusy(false) }
  }
  const filtered = catalog?.elementos.filter(item =>
    (status === 'todos' || item.activo === (status === 'activos')) &&
    `${item.codigo} ${item.descripcion}`.toLocaleLowerCase('es').includes(query.toLocaleLowerCase('es')))
  return <main className="catalog-page">
    <EncabezadoPagina eyebrow="ADMINISTRACIÓN DE PLATAFORMA" titulo="Elementos del dashboard"
      subtitulo="Catálogo compartido por todas las asociaciones.">
      <button className="secondary" disabled={busy} onClick={reload}>Recargar</button>
      <button disabled={busy || !catalog || !!editing || !!removing} onClick={() => edit('new')}>+ Nuevo elemento</button>
    </EncabezadoPagina>
    {notice && <Aviso>{notice}</Aviso>}
    {error && <AlertaError>{error}{blocked && ' Usá Recargar para obtener el estado actualizado.'}</AlertaError>}
    {editing && <section className="catalog-editor" aria-labelledby="element-form-title">
      <h2 id="element-form-title">{editing === 'new' ? 'Nuevo elemento' : `Editar: ${editing.descripcion}`}</h2>
      <form onSubmit={event => void save(event)}>
        <fieldset disabled={busy || blocked}><div className="catalog-form-grid">
          <Campo id="element-code" etiqueta="Código único" value={draft.codigo} disabled={editing !== 'new'} required maxLength={80} pattern="[a-z][a-z0-9_]*"
            ayuda="Minúsculas, números y guion bajo. No cambia después del alta."
            onChange={event => setDraft({ ...draft, codigo: event.target.value })} />
          <Campo id="element-description" etiqueta="Descripción" value={draft.descripcion} required maxLength={160}
            onChange={event => setDraft({ ...draft, descripcion: event.target.value })} />
          <Selector id="element-size" etiqueta="Tamaño" value={draft.tamano} onChange={event => setDraft({ ...draft, tamano: event.target.value })}>
            <option value="normal">Normal · Una columna</option><option value="ancho">Ancho · Dos columnas</option></Selector>
          <Selector id="element-source" etiqueta="Indicador" value={draft.tipoIndicador} required
            ayuda="Sucursales y pacientes activos tienen datos reales. Los demás módulos todavía están pendientes."
            onChange={event => setDraft({ ...draft, tipoIndicador: event.target.value })}>
            <option value="" disabled>Seleccioná un indicador</option>{catalog?.indicadores.map(source => <option key={source.codigo} value={source.codigo}>{source.descripcion}</option>)}</Selector>
        </div>
        <EtiquetaCheck><input type="checkbox" checked={draft.activo} onChange={event => setDraft({ ...draft, activo: event.target.checked })} />Disponible para las asociaciones</EtiquetaCheck>
        <p className="catalog-impact">Los cambios se aplican a todas las asociaciones que usan este elemento. Reactivarlo recupera sus preferencias anteriores.</p>
        </fieldset>
        <div className="editor-actions"><button type="button" className="secondary" disabled={busy} onClick={() => { setEditing(null); setError(''); setBlocked(false) }}>Cancelar</button>
          <button type="submit" disabled={busy || blocked}>{busy ? 'Guardando…' : 'Guardar elemento'}</button></div>
      </form></section>}
    {removing && <Confirmacion id="delete" titulo={`Dar de baja “${removing.descripcion}”`}
      descripcion="Dejará de estar disponible para todas las asociaciones. Sus configuraciones se conservan y podés reactivarlo desde Editar."
      ocupado={busy} bloqueado={blocked}
      onCancelar={() => { setRemoving(null); setError(''); setBlocked(false) }} onConfirmar={() => void deactivate()} />}
    {!catalog && !error && <p role="status">Cargando catálogo…</p>}
    {catalog && <section className="catalog-list" aria-label="Listado de elementos">
      <div className="catalog-filters"><Campo id="catalog-search" etiqueta="Buscar elemento" type="search" placeholder="Descripción o código" value={query}
        onChange={event => setQuery(event.target.value)} />
        <Selector id="catalog-status" etiqueta="Estado" value={status} onChange={event => setStatus(event.target.value)}>
          <option value="todos">Todos</option><option value="activos">Activos</option><option value="inactivos">Dados de baja</option></Selector></div>
      <div className="table-scroll"><table><caption>{filtered?.length ?? 0} elementos</caption><thead><tr><th scope="col">Elemento</th><th scope="col">Indicador</th><th scope="col">Tamaño</th><th scope="col">Estado</th><th scope="col">Acciones</th></tr></thead>
        <tbody>{filtered?.map(item => <tr key={item.id}><td data-etiqueta="Elemento"><strong>{item.descripcion}</strong><small>{item.codigo}</small></td>
          <td data-etiqueta="Indicador">{catalog.indicadores.find(source => source.codigo === item.tipoIndicador)?.descripcion ?? 'Sin indicador'}</td>
          <td data-etiqueta="Tamaño">{item.tamano === 'ancho' ? 'Ancho' : 'Normal'}</td><td data-etiqueta="Estado"><EtiquetaEstado activo={item.activo}>{item.activo ? 'Activo' : 'Baja'}</EtiquetaEstado></td>
          <td data-etiqueta="Acciones"><div className="row-actions"><button className="secondary" disabled={busy || !!editing || !!removing} aria-label={`Editar ${item.descripcion}`} onClick={() => edit(item)}>Editar</button>
            {item.activo && <button className="secondary danger-text" disabled={busy || !!editing || !!removing} aria-label={`Dar de baja ${item.descripcion}`} onClick={() => { setRemoving(item); setError(''); setNotice(''); setBlocked(false) }}>Dar de baja</button>}</div></td></tr>)}</tbody></table></div>
      {filtered?.length === 0 && <p className="catalog-empty">No hay elementos que coincidan con la búsqueda.</p>}
    </section>}
  </main>
}
