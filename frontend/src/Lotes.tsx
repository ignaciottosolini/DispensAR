import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, request, mutate } from './api'
import EncabezadoPagina from './components/EncabezadoPagina'
import { AlertaError, Aviso } from './components/Alerta'
import Confirmacion from './components/Confirmacion'
import EtiquetaEstado from './components/EtiquetaEstado'
import { Campo, EtiquetaCheck, Selector } from './components/Campo'
import type { WriteProps } from './Pacientes'

type Lote = { id: string; productoId: string; productoCodigo: string; productoDescripcion: string; productoUnidad: string;
  codigo: string; fechaVencimiento: string | null; activo: boolean; version: string }
type ProductoOption = { id: string; codigo: string; descripcion: string; activo: boolean }
type Draft = { productoId: string; codigo: string; fechaVencimiento: string; activo: boolean }
const emptyDraft: Draft = { productoId: '', codigo: '', fechaVencimiento: '', activo: true }

export default function Lotes({ onExpired, canWrite }: WriteProps & { onExpired: () => void }) {
  const [items, setItems] = useState<Lote[] | null>(null)
  const [productos, setProductos] = useState<ProductoOption[]>([])
  const [revision, setRevision] = useState(0)
  const [query, setQuery] = useState('')
  const [productFilter, setProductFilter] = useState('todos')
  const [status, setStatus] = useState('todos')
  const [editing, setEditing] = useState<Lote | 'new' | null>(null)
  const [draft, setDraft] = useState<Draft>(emptyDraft)
  const [removing, setRemoving] = useState<Lote | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [blocked, setBlocked] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    Promise.all([request('/api/lotes', { signal: controller.signal }).then(r => r.json()),
      request('/api/productos', { signal: controller.signal }).then(r => r.json())])
      .then(([lotes, productos]: [Lote[], ProductoOption[]]) => {
        if (controller.signal.aborted) return
        setItems(lotes); setProductos(productos)
      })
      .catch(reason => {
        if (controller.signal.aborted) return
        if (reason instanceof ApiError && reason.status === 401) onExpired()
        else setError(reason instanceof Error ? reason.message : 'No se pudo cargar la lista de lotes.')
      })
    return () => controller.abort()
  }, [revision, onExpired])
  function reload() {
    setEditing(null); setRemoving(null); setBlocked(false); setError(''); setItems(null); setRevision(value => value + 1)
  }
  function edit(item: Lote | 'new') {
    setEditing(item); setRemoving(null); setError(''); setNotice(''); setBlocked(false)
    setDraft(item === 'new'
      ? { ...emptyDraft, productoId: productos.filter(p => p.activo).sort((a, b) => a.descripcion.localeCompare(b.descripcion, 'es'))[0]?.id ?? '' }
      : { productoId: item.productoId, codigo: item.codigo, fechaVencimiento: item.fechaVencimiento ?? '', activo: item.activo })
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
      await mutate(isNew ? '/api/lotes' : `/api/lotes/${editing.id}`,
        { productoId: draft.productoId, codigo: draft.codigo, fechaVencimiento: draft.fechaVencimiento || null,
          activo: draft.activo, version: isNew ? null : editing.version }, isNew ? 'POST' : 'PUT')
      reload(); setNotice(isNew ? 'Lote registrado. El stock por sucursal se cargará con el módulo de movimientos.' : 'Lote actualizado.')
    } catch (reason) { handleError(reason) }
    finally { setBusy(false) }
  }
  async function deactivate() {
    if (!removing) return
    setBusy(true); setError('')
    try {
      await mutate(`/api/lotes/${removing.id}`, undefined, 'DELETE', { 'If-Match': `"${removing.version}"` })
      reload(); setNotice('Lote dado de baja. Podés reactivarlo desde Editar.')
    } catch (reason) { handleError(reason) }
    finally { setBusy(false) }
  }
  const hoy = new Date().toISOString().slice(0, 10)
  const filtered = items?.filter(item =>
    (status === 'todos' || item.activo === (status === 'activos')) &&
    (productFilter === 'todos' || item.productoId === productFilter) &&
    `${item.codigo} ${item.productoDescripcion}`.toLocaleLowerCase('es').includes(query.toLocaleLowerCase('es')))
  return <main className="catalog-page">
    <EncabezadoPagina eyebrow="ASOCIACIÓN" titulo="Lotes" subtitulo="Lotes por producto de tu asociación.">
      <button className="secondary" disabled={busy} onClick={reload}>Recargar</button>
      {canWrite && <button disabled={busy || !items || !!editing || !!removing || !productos.some(p => p.activo)} onClick={() => edit('new')}>+ Nuevo lote</button>}
    </EncabezadoPagina>
    {notice && <Aviso>{notice}</Aviso>}
    {error && <AlertaError>{error}{blocked && ' Usá Recargar para obtener el estado actualizado.'}</AlertaError>}
    {editing && <section className="catalog-editor" aria-labelledby="lote-form-title">
      <h2 id="lote-form-title">{editing === 'new' ? 'Nuevo lote' : `Editar: lote ${editing.codigo}`}</h2>
      <form onSubmit={event => void save(event)}>
        <fieldset disabled={busy || blocked}><div className="catalog-form-grid">
          <Selector id="lote-producto" etiqueta="Producto" value={draft.productoId} required disabled={editing !== 'new'}
            ayuda="Un lote no cambia de producto después del alta."
            onChange={event => setDraft({ ...draft, productoId: event.target.value })}>
            <option value="" disabled>Seleccioná un producto</option>
            {productos.filter(p => p.activo || p.id === draft.productoId).sort((a, b) => a.descripcion.localeCompare(b.descripcion, 'es'))
              .map(p => <option key={p.id} value={p.id}>{p.descripcion}{p.activo ? '' : ' (dado de baja)'}</option>)}</Selector>
          <Campo id="lote-codigo" etiqueta="Código de lote" value={draft.codigo} disabled={editing !== 'new'} required maxLength={80}
            pattern="[0-9A-Za-z][0-9A-Za-z._/-]{0,79}" ayuda="Letras, números, punto, guion, guion bajo o barra. No cambia después del alta."
            onChange={event => setDraft({ ...draft, codigo: event.target.value })} />
          <Campo id="lote-vencimiento" etiqueta="Fecha de vencimiento" type="date" value={draft.fechaVencimiento}
            onChange={event => setDraft({ ...draft, fechaVencimiento: event.target.value })} />
          <EtiquetaCheck><input type="checkbox" checked={draft.activo} onChange={event => setDraft({ ...draft, activo: event.target.checked })} />Lote activo</EtiquetaCheck>
        </div></fieldset>
        <div className="editor-actions"><button type="button" className="secondary" disabled={busy} onClick={() => { setEditing(null); setError(''); setBlocked(false) }}>Cancelar</button>
          <button type="submit" disabled={busy || blocked}>{busy ? 'Guardando…' : 'Guardar lote'}</button></div>
      </form></section>}
    {removing && <Confirmacion id="lote-delete" titulo={`Dar de baja el lote “${removing.codigo}”`}
      descripcion="Dejará de estar disponible. El lote se conserva y podés reactivarlo desde Editar."
      ocupado={busy} bloqueado={blocked}
      onCancelar={() => { setRemoving(null); setError(''); setBlocked(false) }} onConfirmar={() => void deactivate()} />}
    {!items && !error && <p role="status">Cargando lotes…</p>}
    {items && <section className="catalog-list" aria-label="Listado de lotes">
      <div className="catalog-filters lote-filters"><Campo id="lote-search" etiqueta="Buscar lote" type="search" placeholder="Código o producto" value={query}
        onChange={event => setQuery(event.target.value)} />
        <Selector id="lote-product-filter" etiqueta="Producto" value={productFilter} onChange={event => setProductFilter(event.target.value)}>
          <option value="todos">Todos</option>{productos.map(p => <option key={p.id} value={p.id}>{p.descripcion}</option>)}</Selector>
        <Selector id="lote-status" etiqueta="Estado" value={status} onChange={event => setStatus(event.target.value)}>
          <option value="todos">Todos</option><option value="activos">Activos</option><option value="inactivos">Dados de baja</option></Selector></div>
      <div className="table-scroll"><table><caption>{filtered?.length ?? 0} lotes</caption>
        <thead><tr><th scope="col">Lote</th><th scope="col">Producto</th><th scope="col">Vencimiento</th><th scope="col">Estado</th>{canWrite && <th scope="col">Acciones</th>}</tr></thead>
        <tbody>{filtered?.map(item => <tr key={item.id}><td data-etiqueta="Lote"><strong>{item.codigo}</strong></td>
          <td data-etiqueta="Producto"><strong>{item.productoDescripcion}</strong><small>{item.productoCodigo} · {item.productoUnidad}</small></td>
          <td data-etiqueta="Vencimiento">{item.fechaVencimiento ?? 'Sin fecha'}{item.fechaVencimiento && item.fechaVencimiento < hoy && <EtiquetaEstado inline>Vencido</EtiquetaEstado>}</td>
          <td data-etiqueta="Estado"><EtiquetaEstado activo={item.activo}>{item.activo ? 'Activo' : 'Baja'}</EtiquetaEstado></td>
          {canWrite && <td data-etiqueta="Acciones"><div className="row-actions"><button className="secondary" disabled={busy || !!editing || !!removing} aria-label={`Editar lote ${item.codigo}`} onClick={() => edit(item)}>Editar</button>
            {item.activo && <button className="secondary danger-text" disabled={busy || !!editing || !!removing} aria-label={`Dar de baja lote ${item.codigo}`}
              onClick={() => { setRemoving(item); setError(''); setNotice(''); setBlocked(false) }}>Dar de baja</button>}</div></td>}</tr>)}</tbody></table></div>
      {filtered?.length === 0 && <p className="catalog-empty">No hay lotes que coincidan con la búsqueda.</p>}
    </section>}
  </main>
}
