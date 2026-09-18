import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, request, mutate } from './api'
import EncabezadoPagina from './components/EncabezadoPagina'
import { AlertaError, Aviso } from './components/Alerta'
import Confirmacion from './components/Confirmacion'
import EtiquetaEstado from './components/EtiquetaEstado'
import { Campo, Selector } from './components/Campo'

export type Paciente = { id: string; nombres: string; apellidos: string; dni: string; fechaNacimiento: string | null
  estado: string; autorizacionHasta: string | null; observaciones: string | null; version: string }
type Draft = { nombres: string; apellidos: string; dni: string; fechaNacimiento: string; estado: string; autorizacionHasta: string; observaciones: string }
const emptyDraft: Draft = { nombres: '', apellidos: '', dni: '', fechaNacimiento: '', estado: 'Activo', autorizacionHasta: '', observaciones: '' }
export type WriteProps = { canWrite: boolean }

export default function Pacientes({ onExpired, canWrite }: WriteProps & { onExpired: () => void }) {
  const [items, setItems] = useState<Paciente[] | null>(null)
  const [revision, setRevision] = useState(0)
  const [query, setQuery] = useState('')
  const [status, setStatus] = useState('todos')
  const [editing, setEditing] = useState<Paciente | 'new' | null>(null)
  const [draft, setDraft] = useState<Draft>(emptyDraft)
  const [removing, setRemoving] = useState<Paciente | null>(null)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [notice, setNotice] = useState('')
  const [blocked, setBlocked] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    request('/api/pacientes', { signal: controller.signal }).then(response => response.json())
      .then((value: Paciente[]) => { if (!controller.signal.aborted) setItems(value) })
      .catch(reason => {
        if (controller.signal.aborted) return
        if (reason instanceof ApiError && reason.status === 401) onExpired()
        else setError(reason instanceof Error ? reason.message : 'No se pudo cargar la lista de pacientes.')
      })
    return () => controller.abort()
  }, [revision, onExpired])
  function reload() {
    setEditing(null); setRemoving(null); setBlocked(false); setError(''); setItems(null); setRevision(value => value + 1)
  }
  function edit(item: Paciente | 'new') {
    setEditing(item); setRemoving(null); setError(''); setNotice(''); setBlocked(false)
    setDraft(item === 'new' ? { ...emptyDraft } : {
      nombres: item.nombres, apellidos: item.apellidos, dni: item.dni, fechaNacimiento: item.fechaNacimiento ?? '',
      estado: item.estado, autorizacionHasta: item.autorizacionHasta ?? '', observaciones: item.observaciones ?? '',
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
      const body = { ...draft, dni: isNew ? draft.dni : undefined, fechaNacimiento: draft.fechaNacimiento || null,
        autorizacionHasta: draft.autorizacionHasta || null, observaciones: draft.observaciones || null,
        version: isNew ? null : editing.version }
      await mutate(isNew ? '/api/pacientes' : `/api/pacientes/${editing.id}`, body, isNew ? 'POST' : 'PUT')
      reload(); setNotice(isNew ? 'Paciente registrado.' : 'Paciente actualizado.')
    } catch (reason) { handleError(reason) }
    finally { setBusy(false) }
  }
  async function deactivate() {
    if (!removing) return
    setBusy(true); setError('')
    try {
      await mutate(`/api/pacientes/${removing.id}`, undefined, 'DELETE', { 'If-Match': `"${removing.version}"` })
      reload(); setNotice('Paciente dado de baja. Su DNI queda reservado y podés reactivarlo desde Editar.')
    } catch (reason) { handleError(reason) }
    finally { setBusy(false) }
  }
  const filtered = items?.filter(item =>
    (status === 'todos' || item.estado.toLowerCase() === status) &&
    `${item.apellidos} ${item.nombres} ${item.dni}`.toLocaleLowerCase('es').includes(query.toLocaleLowerCase('es')))
  return <main className="catalog-page">
    <EncabezadoPagina eyebrow="ASOCIACIÓN" titulo="Pacientes" subtitulo="Registro de pacientes de tu asociación.">
      <button className="secondary" disabled={busy} onClick={reload}>Recargar</button>
      {canWrite && <button disabled={busy || !items || !!editing || !!removing} onClick={() => edit('new')}>+ Nuevo paciente</button>}
    </EncabezadoPagina>
    {notice && <Aviso>{notice}</Aviso>}
    {error && <AlertaError>{error}{blocked && ' Usá Recargar para obtener el estado actualizado.'}</AlertaError>}
    {editing && <section className="catalog-editor" aria-labelledby="paciente-form-title">
      <h2 id="paciente-form-title">{editing === 'new' ? 'Nuevo paciente' : `Editar: ${editing.apellidos}, ${editing.nombres}`}</h2>
      <form onSubmit={event => void save(event)}>
        <fieldset disabled={busy || blocked}><div className="catalog-form-grid">
          <Campo id="paciente-apellidos" etiqueta="Apellidos" value={draft.apellidos} required maxLength={160}
            onChange={event => setDraft({ ...draft, apellidos: event.target.value })} />
          <Campo id="paciente-nombres" etiqueta="Nombres" value={draft.nombres} required maxLength={160}
            onChange={event => setDraft({ ...draft, nombres: event.target.value })} />
          <Campo id="paciente-dni" etiqueta="DNI" value={draft.dni} required={editing === 'new'} disabled={editing !== 'new'}
            inputMode="numeric" pattern="[0-9]{6,10}" ayuda="6 a 10 dígitos, sin puntos. No cambia después del alta."
            onChange={event => setDraft({ ...draft, dni: event.target.value })} />
          <Campo id="paciente-nacimiento" etiqueta="Fecha de nacimiento" type="date" value={draft.fechaNacimiento}
            onChange={event => setDraft({ ...draft, fechaNacimiento: event.target.value })} />
          <Campo id="paciente-autorizacion" etiqueta="Autorización hasta" type="date" value={draft.autorizacionHasta}
            ayuda="Fecha de vencimiento de la documentación habilitante."
            onChange={event => setDraft({ ...draft, autorizacionHasta: event.target.value })} />
          <Selector id="paciente-estado" etiqueta="Estado" value={draft.estado}
            onChange={event => setDraft({ ...draft, estado: event.target.value })}>
            <option value="Activo">Activo</option><option value="Inactivo">Inactivo</option></Selector>
        </div>
        <label htmlFor="paciente-observaciones">Observaciones</label>
        <textarea id="paciente-observaciones" value={draft.observaciones} maxLength={1000} rows={3}
          onChange={event => setDraft({ ...draft, observaciones: event.target.value })} />
        </fieldset>
        <div className="editor-actions"><button type="button" className="secondary" disabled={busy} onClick={() => { setEditing(null); setError(''); setBlocked(false) }}>Cancelar</button>
          <button type="submit" disabled={busy || blocked}>{busy ? 'Guardando…' : 'Guardar paciente'}</button></div>
      </form></section>}
    {removing && <Confirmacion id="paciente-delete" titulo={`Dar de baja a “${removing.apellidos}, ${removing.nombres}”`}
      descripcion="Quedará inactivo y seguirá contando su DNI como reservado. No se elimina ningún dato."
      ocupado={busy} bloqueado={blocked}
      onCancelar={() => { setRemoving(null); setError(''); setBlocked(false) }} onConfirmar={() => void deactivate()} />}
    {!items && !error && <p role="status">Cargando pacientes…</p>}
    {items && <section className="catalog-list" aria-label="Listado de pacientes">
      <div className="catalog-filters"><Campo id="paciente-search" etiqueta="Buscar paciente" type="search" placeholder="Apellido, nombre o DNI" value={query}
        onChange={event => setQuery(event.target.value)} />
        <Selector id="paciente-status" etiqueta="Estado" value={status} onChange={event => setStatus(event.target.value)}>
          <option value="todos">Todos</option><option value="activo">Activos</option><option value="inactivo">Inactivos</option></Selector></div>
      <div className="table-scroll"><table><caption>{filtered?.length ?? 0} pacientes</caption>
        <thead><tr><th scope="col">Paciente</th><th scope="col">Nacimiento</th><th scope="col">Autorización</th><th scope="col">Estado</th>{canWrite && <th scope="col">Acciones</th>}</tr></thead>
        <tbody>{filtered?.map(item => <tr key={item.id}><td data-etiqueta="Paciente"><strong>{item.apellidos}, {item.nombres}</strong><small>DNI {item.dni}</small></td>
          <td data-etiqueta="Nacimiento">{item.fechaNacimiento ?? '—'}</td>
          <td data-etiqueta="Autorización">{item.autorizacionHasta ?? 'Sin fecha'}{item.autorizacionHasta && item.autorizacionHasta < new Date().toISOString().slice(0, 10) && <EtiquetaEstado inline>Vencida</EtiquetaEstado>}</td>
          <td data-etiqueta="Estado"><EtiquetaEstado activo={item.estado === 'Activo'}>{item.estado}</EtiquetaEstado></td>
          {canWrite && <td data-etiqueta="Acciones"><div className="row-actions"><button className="secondary" disabled={busy || !!editing || !!removing} aria-label={`Editar a ${item.apellidos}, ${item.nombres}`} onClick={() => edit(item)}>Editar</button>
            {item.estado === 'Activo' && <button className="secondary danger-text" disabled={busy || !!editing || !!removing} aria-label={`Dar de baja a ${item.apellidos}, ${item.nombres}`}
              onClick={() => { setRemoving(item); setError(''); setNotice(''); setBlocked(false) }}>Dar de baja</button>}</div></td>}</tr>)}</tbody></table></div>
      {filtered?.length === 0 && <p className="catalog-empty">No hay pacientes que coincidan con la búsqueda.</p>}
    </section>}
  </main>
}
