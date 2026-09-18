import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, request, mutate } from './api'
import EncabezadoPagina from './components/EncabezadoPagina'
import { AlertaError, Aviso } from './components/Alerta'
import Confirmacion from './components/Confirmacion'
import EtiquetaEstado from './components/EtiquetaEstado'
import { Campo, EtiquetaCheck, Selector } from './components/Campo'
import type { WriteProps } from './Pacientes'

type Producto = { id: string; codigo: string; descripcion: string; unidad: string; activo: boolean; version: string }
type Draft = { codigo: string; descripcion: string; unidad: string; activo: boolean }
const emptyDraft: Draft = { codigo: '', descripcion: '', unidad: 'gramos', activo: true }
const unidades: Record<string, string> = { gramos: 'Gramos', mililitros: 'Mililitros', unidades: 'Unidades' }

export default function Productos({ onExpired, canWrite }: WriteProps & { onExpired: () => void }) {
  const [items, setItems] = useState<Producto[] | null>(null)
  const [revision, setRevision] = useState(0)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('todos')
  const [editing, setEditing] = useState<Producto | 'new' | null>(null)
  const [draft, setDraft] = useState<Draft>(emptyDraft)
  const [removing, setRemoving] = useState<Producto | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [blocked, setBlocked] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    request('/api/productos', { signal: controller.signal }).then(response => response.json())
      .then((value: Producto[]) => { if (!controller.signal.aborted) setItems(value) })
      .catch(reason => {
        if (controller.signal.aborted) return
        if (reason instanceof ApiError && reason.status === 401) onExpired()
        else setError(reason instanceof Error ? reason.message : 'No se pudo cargar la lista de productos.')
      })
    return () => controller.abort()
  }, [revision, onExpired])
  function reload() {
    setEditing(null); setRemoving(null); setBlocked(false); setError(''); setItems(null); setRevision(value => value + 1)
  }
  function edit(item: Producto | 'new') {
    setEditing(item); setRemoving(null); setError(''); setNotice(''); setBlocked(false)
    setDraft(item === 'new' ? { ...emptyDraft } : { codigo: item.codigo, descripcion: item.descripcion, unidad: item.unidad, activo: item.activo })
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
      await mutate(isNew ? '/api/productos' : `/api/productos/${editing.id}`,
        { ...draft, version: isNew ? null : editing.version }, isNew ? 'POST' : 'PUT')
      reload(); setNotice(isNew ? 'Producto creado. Ahora podés registrarle lotes.' : 'Producto actualizado.')
    } catch (reason) { handleError(reason) }
    finally { setBusy(false) }
  }
  async function deactivate() {
    if (!removing) return
    setBusy(true); setError('')
    try {
      await mutate(`/api/productos/${removing.id}`, undefined, 'DELETE', { 'If-Match': `"${removing.version}"` })
      reload(); setNotice('Producto dado de baja. Sus lotes se conservan y podés reactivarlo desde Editar.')
    } catch (reason) { handleError(reason) }
    finally { setBusy(false) }
  }
  const filtered = items?.filter(item =>
    (status === 'todos' || item.activo === (status === 'activos')) &&
    `${item.codigo} ${item.descripcion}`.toLocaleLowerCase('es').includes(query.toLocaleLowerCase('es')))
  return <main className="catalog-page">
    <EncabezadoPagina eyebrow="ASOCIACIÓN" titulo="Productos" subtitulo="Catálogo de productos de tu asociación, con su unidad de medida.">
      <button className="secondary" disabled={busy} onClick={reload}>Recargar</button>
      {canWrite && <button disabled={busy || !items || !!editing || !!removing} onClick={() => edit('new')}>+ Nuevo producto</button>}
    </EncabezadoPagina>
    {notice && <Aviso>{notice}</Aviso>}
    {error && <AlertaError>{error}{blocked && ' Usá Recargar para obtener el estado actualizado.'}</AlertaError>}
    {editing && <section className="catalog-editor" aria-labelledby="producto-form-title">
      <h2 id="producto-form-title">{editing === 'new' ? 'Nuevo producto' : `Editar: ${editing.descripcion}`}</h2>
      <form onSubmit={event => void save(event)}>
        <fieldset disabled={busy || blocked}><div className="catalog-form-grid">
          <Campo id="producto-codigo" etiqueta="Código único" value={draft.codigo} disabled={editing !== 'new'} required maxLength={80} pattern="[a-z][a-z0-9_]*"
            ayuda="Minúsculas, números y guion bajo. No cambia después del alta."
            onChange={event => setDraft({ ...draft, codigo: event.target.value })} />
          <Campo id="producto-descripcion" etiqueta="Descripción" value={draft.descripcion} required maxLength={160}
            onChange={event => setDraft({ ...draft, descripcion: event.target.value })} />
          <Selector id="producto-unidad" etiqueta="Unidad de medida" value={draft.unidad}
            ayuda="Las cantidades de stock y dispensación se expresan siempre en esta unidad."
            onChange={event => setDraft({ ...draft, unidad: event.target.value })}>
            <option value="gramos">Gramos</option><option value="mililitros">Mililitros</option><option value="unidades">Unidades</option></Selector>
          <EtiquetaCheck><input type="checkbox" checked={draft.activo} onChange={event => setDraft({ ...draft, activo: event.target.checked })} />Producto activo</EtiquetaCheck>
        </div></fieldset>
        <div className="editor-actions"><button type="button" className="secondary" disabled={busy} onClick={() => { setEditing(null); setError(''); setBlocked(false) }}>Cancelar</button>
          <button type="submit" disabled={busy || blocked}>{busy ? 'Guardando…' : 'Guardar producto'}</button></div>
      </form></section>}
    {removing && <Confirmacion id="producto-delete" titulo={`Dar de baja “${removing.descripcion}”`}
      descripcion="Dejará de estar disponible para nuevos lotes. Sus lotes existentes se conservan y podés reactivarlo desde Editar."
      ocupado={busy} bloqueado={blocked}
      onCancelar={() => { setRemoving(null); setError(''); setBlocked(false) }} onConfirmar={() => void deactivate()} />}
    {!items && !error && <p role="status">Cargando productos…</p>}
    {items && <section className="catalog-list" aria-label="Listado de productos">
      <div className="catalog-filters"><Campo id="producto-search" etiqueta="Buscar producto" type="search" placeholder="Descripción o código" value={query}
        onChange={event => setQuery(event.target.value)} />
        <Selector id="producto-status" etiqueta="Estado" value={status} onChange={event => setStatus(event.target.value)}>
          <option value="todos">Todos</option><option value="activos">Activos</option><option value="inactivos">Dados de baja</option></Selector></div>
      <div className="table-scroll"><table><caption>{filtered?.length ?? 0} productos</caption>
        <thead><tr><th scope="col">Producto</th><th scope="col">Unidad</th><th scope="col">Estado</th>{canWrite && <th scope="col">Acciones</th>}</tr></thead>
        <tbody>{filtered?.map(item => <tr key={item.id}><td data-etiqueta="Producto"><strong>{item.descripcion}</strong><small>{item.codigo}</small></td>
          <td data-etiqueta="Unidad">{unidades[item.unidad] ?? item.unidad}</td>
          <td data-etiqueta="Estado"><EtiquetaEstado activo={item.activo}>{item.activo ? 'Activo' : 'Baja'}</EtiquetaEstado></td>
          {canWrite && <td data-etiqueta="Acciones"><div className="row-actions"><button className="secondary" disabled={busy || !!editing || !!removing} aria-label={`Editar ${item.descripcion}`} onClick={() => edit(item)}>Editar</button>
            {item.activo && <button className="secondary danger-text" disabled={busy || !!editing || !!removing} aria-label={`Dar de baja ${item.descripcion}`}
              onClick={() => { setRemoving(item); setError(''); setNotice(''); setBlocked(false) }}>Dar de baja</button>}</div></td>}</tr>)}</tbody></table></div>
      {filtered?.length === 0 && <p className="catalog-empty">No hay productos que coincidan con la búsqueda.</p>}
    </section>}
  </main>
}
