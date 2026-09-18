import { useEffect, useState } from 'react'
import type { ChangeEvent, CSSProperties } from 'react'
import { ApiError, mutate, request, subirArchivo } from './api'
import { normalizarHex } from './theme/colores'
import { TEMA_DETERMINADO, coloresBase, erroresContraste, variablesTema } from './theme/tema'
import type { Identidad, IdentidadRespuesta } from './theme/tema'
import Confirmacion from './components/Confirmacion'
import { AlertaError, Aviso } from './components/Alerta'
import EncabezadoPagina from './components/EncabezadoPagina'
import Marca from './components/Marca'
import EtiquetaEstado from './components/EtiquetaEstado'

type Props = { onExpired: () => void; tenantId: number; onActualizada: () => void }

type Borrador = {
  logoPrincipalRuta: string | null
  logoPrincipalUrl: string | null
  logoCompactoRuta: string | null
  logoCompactoUrl: string | null
  primario: string
  secundario: string
  sidebarFondo: string
  sidebarTexto: string
  version: string | null
}

type Colores = 'primario' | 'secundario' | 'sidebarFondo' | 'sidebarTexto'

const camposColor: { clave: Colores; etiqueta: string; descripcion: string }[] = [
  { clave: 'primario', etiqueta: 'Color primario', descripcion: 'Botones, marca y navegación seleccionada.' },
  { clave: 'secundario', etiqueta: 'Color secundario', descripcion: 'Acentos e íconos sobre las tarjetas.' },
  { clave: 'sidebarFondo', etiqueta: 'Fondo de la barra lateral', descripcion: 'Color de fondo del menú izquierdo.' },
  { clave: 'sidebarTexto', etiqueta: 'Texto de la barra lateral', descripcion: 'Color de los ítems del menú izquierdo.' },
]

const MAXIMO_LOGO = 1024 * 1024
const tiposPermitidos = ['image/png', 'image/jpeg', 'image/webp']

function borradorDesde(identidad: Identidad | null): Borrador {
  return {
    logoPrincipalRuta: identidad?.logoPrincipalUrl ? rutaDeUrl(identidad.logoPrincipalUrl) : null,
    logoPrincipalUrl: identidad?.logoPrincipalUrl ?? null,
    logoCompactoRuta: identidad?.logoCompactoUrl ? rutaDeUrl(identidad.logoCompactoUrl) : null,
    logoCompactoUrl: identidad?.logoCompactoUrl ?? null,
    primario: identidad?.colorPrimario ?? '',
    secundario: identidad?.colorSecundario ?? '',
    sidebarFondo: identidad?.colorSidebarFondo ?? '',
    sidebarTexto: identidad?.colorSidebarTexto ?? '',
    version: identidad?.version ?? null,
  }
}

// El borrador viaja como identidad "cruda": cadena vacía = sin configurar (null).
function identidadDelBorrador(b: Borrador): Identidad {
  return {
    logoPrincipalUrl: b.logoPrincipalUrl,
    logoCompactoUrl: b.logoCompactoUrl,
    colorPrimario: b.primario || null,
    colorSecundario: b.secundario || null,
    colorSidebarFondo: b.sidebarFondo || null,
    colorSidebarTexto: b.sidebarTexto || null,
    version: b.version ?? '',
  }
}

function rutaDeUrl(url: string): string | null {
  const prefijo = '/api/identidad/logos/'
  return url.startsWith(prefijo) ? decodeURIComponent(url.slice(prefijo.length)) : null
}

export default function IdentidadVisual({ onExpired, tenantId, onActualizada }: Props) {
  const [datos, setDatos] = useState<IdentidadRespuesta | null>(null)
  const [base, setBase] = useState<Borrador>(borradorDesde(null))
  const [borrador, setBorrador] = useState<Borrador>(borradorDesde(null))
  const [revision, setRevision] = useState(0)
  const [error, setError] = useState('')
  const [aviso, setAviso] = useState('')
  const [confirmando, setConfirmando] = useState(false)
  const [ocupado, setOcupado] = useState(false)
  const [bloqueado, setBloqueado] = useState(false)

  useEffect(() => {
    const controller = new AbortController()
    request('/api/identidad', { signal: controller.signal }).then(response => response.json())
      .then((value: IdentidadRespuesta) => {
        if (controller.signal.aborted) return
        setDatos(value)
        setBase(borradorDesde(value.identidad))
        setBorrador(borradorDesde(value.identidad))
      })
      .catch(reason => {
        if (controller.signal.aborted) return
        if (reason instanceof ApiError && reason.status === 401) onExpired()
        else setError(reason instanceof Error ? reason.message : 'No se pudo cargar la identidad visual.')
      })
    return () => controller.abort()
  }, [revision, onExpired])

  function recargar() {
    setBorrador(base); setBloqueado(false); setConfirmando(false); setError(''); setAviso(''); setRevision(value => value + 1)
  }
  function handleError(reason: unknown) {
    if (reason instanceof ApiError && reason.status === 401) onExpired()
    else {
      if (reason instanceof ApiError && (reason.status === 403 || reason.status === 409)) setBloqueado(true)
      setError(reason instanceof Error ? reason.message : 'No se pudo guardar el cambio.')
    }
  }
  async function subir(event: ChangeEvent<HTMLInputElement>, clave: 'principal' | 'compacto') {
    const archivo = event.target.files?.[0]
    event.target.value = ''
    if (!archivo) return
    if (!tiposPermitidos.includes(archivo.type)) { setError('El logo debe ser una imagen PNG, JPEG o WebP.'); return }
    if (archivo.size > MAXIMO_LOGO) { setError(`El logo no puede superar ${MAXIMO_LOGO / 1024} KB.`); return }
    setOcupado(true); setError('')
    try {
      const respuesta = await (await subirArchivo(`/api/identidad/logos/${clave}`, archivo)).json() as { ruta: string; url: string }
      setBorrador(borrador => clave === 'principal'
        ? { ...borrador, logoPrincipalRuta: respuesta.ruta, logoPrincipalUrl: respuesta.url }
        : { ...borrador, logoCompactoRuta: respuesta.ruta, logoCompactoUrl: respuesta.url })
    } catch (reason) { handleError(reason) }
    finally { setOcupado(false) }
  }
  function quitar(clave: 'principal' | 'compacto') {
    setBorrador(borrador => clave === 'principal'
      ? { ...borrador, logoPrincipalRuta: null, logoPrincipalUrl: null }
      : { ...borrador, logoCompactoRuta: null, logoCompactoUrl: null })
  }
  function cambiarColor(clave: Colores, valor: string) {
    setBorrador(borrador => ({ ...borrador, [clave]: valor }))
  }
  const hexInvalido = camposColor.some(({ clave }) => borrador[clave] !== '' && normalizarHex(borrador[clave]) === null)
  const identidadBorrador = identidadDelBorrador(borrador)
  const advertencias = erroresContraste(coloresBase(identidadBorrador))
  const sucio = JSON.stringify(borrador) !== JSON.stringify(base)
  async function guardar() {
    setOcupado(true); setError('')
    try {
      await mutate('/api/identidad/', {
        logoPrincipalRuta: borrador.logoPrincipalRuta, logoCompactoRuta: borrador.logoCompactoRuta,
        colorPrimario: borrador.primario || null, colorSecundario: borrador.secundario || null,
        colorSidebarFondo: borrador.sidebarFondo || null, colorSidebarTexto: borrador.sidebarTexto || null,
        version: borrador.version,
      }, 'PUT')
      setRevision(value => value + 1)
      setAviso('La identidad visual se guardó para toda la asociación.')
      onActualizada()
    } catch (reason) { handleError(reason) }
    finally { setOcupado(false) }
  }
  async function restaurar() {
    setOcupado(true); setError(''); setConfirmando(false)
    try {
      await mutate('/api/identidad/', undefined, 'DELETE',
        base.version ? { 'If-Match': `"${base.version}"` } : {})
      setRevision(value => value + 1)
      setAviso('Se restauró el tema predeterminado de DispensAR.')
      onActualizada()
    } catch (reason) { handleError(reason) }
    finally { setOcupado(false) }
  }
  const preview = variablesTema(identidadBorrador)
  return <main className="identidad">
    <EncabezadoPagina eyebrow="CONFIGURACIÓN DE LA ASOCIACIÓN" titulo="Identidad visual"
      subtitulo={`Estás configurando la apariencia de “${datos?.descripcion ?? 'tu asociación'}” (Asociación #${tenantId}).`} />
    {aviso && <Aviso>{aviso}</Aviso>}
    {error && <AlertaError>{error}{bloqueado && ' Usá “Cancelar edición” para obtener el estado actualizado.'}</AlertaError>}
    {datos && !datos.identidad && <p className="identidad-aviso">
      Esta asociación todavía no configuró su identidad visual: está usando el tema predeterminado de DispensAR.
    </p>}
    <div className="identidad-layout">
      <div className="identidad-formulario">
        <section className="identidad-seccion" aria-labelledby="identidad-logos-title">
          <h2 id="identidad-logos-title">Logos</h2>
          <p>PNG, JPEG o WebP de hasta 1 MB y 4096 × 4096 píxeles. Se muestran conservando su proporción.</p>
          <label htmlFor="logo-principal">Logo principal</label>
          <div className="logo-casilla">
            <div className="logo-marco">
              {borrador.logoPrincipalUrl
                ? <img src={borrador.logoPrincipalUrl} alt={`Logo principal de ${datos?.descripcion ?? 'la asociación'}`} />
                : <span>Sin logo</span>}
            </div>
            <div className="logo-acciones">
              <input id="logo-principal" type="file" accept="image/png,image/jpeg,image/webp" disabled={ocupado}
                onChange={event => void subir(event, 'principal')} />
              {borrador.logoPrincipalUrl && <button type="button" className="secondary danger-text" disabled={ocupado} onClick={() => quitar('principal')}>Quitar logo principal</button>}
              <span className="logo-datos">Aparece en el encabezado en pantallas amplias. Sin logo se muestra el nombre de la asociación.</span>
            </div>
          </div>
          <label htmlFor="logo-compacto">Logo compacto (opcional)</label>
          <div className="logo-casilla">
            <div className="logo-marco">
              {borrador.logoCompactoUrl
                ? <img src={borrador.logoCompactoUrl} alt={`Logo compacto de ${datos?.descripcion ?? 'la asociación'}`} />
                : <span>Sin logo</span>}
            </div>
            <div className="logo-acciones">
              <input id="logo-compacto" type="file" accept="image/png,image/jpeg,image/webp" disabled={ocupado}
                onChange={event => void subir(event, 'compacto')} />
              {borrador.logoCompactoUrl && <button type="button" className="secondary danger-text" disabled={ocupado} onClick={() => quitar('compacto')}>Quitar logo compacto</button>}
              <span className="logo-datos">Para espacios reducidos, como el encabezado en el celular.</span>
            </div>
          </div>
        </section>
        <section className="identidad-seccion" aria-labelledby="identidad-colores-title">
          <h2 id="identidad-colores-title">Colores</h2>
          <p>Escribí el valor hexadecimal o elegilo con el selector. Dejá el campo vacío para volver al predeterminado de DispensAR.</p>
          {camposColor.map(({ clave, etiqueta, descripcion }) => {
            const efectivo = coloresBase(identidadBorrador)[clave]
            return <div key={clave}>
              <label htmlFor={`color-${clave}`}>{etiqueta}</label>
              <div className="color-casilla">
                <input id={`selector-${clave}`} type="color" value={efectivo} aria-label={`Selector de ${etiqueta}`} disabled={ocupado}
                  onChange={event => cambiarColor(clave, event.target.value)} />
                <div>
                  <input id={`color-${clave}`} type="text" value={borrador[clave]} placeholder={`${TEMA_DETERMINADO[clave]} (predeterminado)`}
                    maxLength={7} autoComplete="off" spellCheck={false} disabled={ocupado}
                    aria-invalid={borrador[clave] !== '' && normalizarHex(borrador[clave]) === null}
                    aria-describedby={`ayuda-${clave}`}
                    onChange={event => cambiarColor(clave, event.target.value)} />
                  <small id={`ayuda-${clave}`}>{descripcion} Predeterminado de DispensAR: {TEMA_DETERMINADO[clave]}.</small>
                </div>
              </div>
            </div>
          })}
          {hexInvalido && <AlertaError>Usá un color hexadecimal válido con el formato #rgb o #rrggbb.</AlertaError>}
          {!hexInvalido && advertencias.map(mensaje => <p className="identidad-aviso" key={mensaje} role="status">{mensaje}</p>)}
        </section>
      </div>
      <div className="identidad-vista">
        <section className="identidad-preview" style={preview as CSSProperties} aria-label="Previsualización de la identidad visual">
          <div className="preview-topbar topbar"><Marca descripcion={datos?.descripcion ?? 'Asociación'}
            logoPrincipalUrl={borrador.logoPrincipalUrl} logoCompactoUrl={borrador.logoCompactoUrl} />
            <div className="session"><div><strong>operaria@asociacion.ar</strong><small>Administrativo</small></div>
              <button className="secondary" type="button" tabIndex={-1}>Cerrar sesión</button></div></div>
          <div className="preview-body">
            <div className="preview-sidebar sidebar"><p className="eyebrow">PRINCIPAL</p>
              <button type="button" className="nav-item selected" tabIndex={-1}><span aria-hidden="true">▦</span> Dashboard</button>
              <button type="button" className="nav-item" tabIndex={-1}><span aria-hidden="true">◎</span> Pacientes</button>
              <button type="button" className="nav-item" tabIndex={-1}><span aria-hidden="true">▤</span> Productos</button></div>
            <div className="preview-contenido">
              <div className="preview-botones">
                <button type="button" tabIndex={-1}>Guardar</button>
                <button type="button" className="secondary" tabIndex={-1}>Cancelar</button>
                <button type="button" className="danger" tabIndex={-1}>Confirmar baja</button>
              </div>
              <div className="widget-grid preview-tarjetas">
                <article className="widget"><div className="widget-heading"><span className="widget-icon" aria-hidden="true">◎</span><h2>Pacientes activos</h2></div>
                  <p className="metric">128</p><p className="widget-detail">Pacientes habilitados en la asociación.</p>
                  <span className="widget-status connected">Datos actuales</span></article>
                <article className="widget"><div className="widget-heading"><span className="widget-icon" aria-hidden="true">⌂</span><h2>Sucursales</h2></div>
                  <p className="metric">3</p><ul className="branch-list"><li>Sucursal Centro</li><li>Sucursal Norte</li></ul></article>
              </div>
              <div className="preview-badges"><EtiquetaEstado>En baja</EtiquetaEstado><EtiquetaEstado activo>Activo</EtiquetaEstado></div>
            </div>
          </div>
          <p className="preview-caption">Los cambios sin guardar afectan solo a esta previsualización.</p>
        </section>
      </div>
    </div>
    {confirmando && <Confirmacion id="identidad-reset" titulo="Restaurar el tema predeterminado de DispensAR"
      descripcion="Se quitarán los logos y los colores de tu asociación. Esta acción no afecta los datos de operación."
      confirmar="Restaurar" ocupado={ocupado} bloqueado={bloqueado}
      onCancelar={() => setConfirmando(false)} onConfirmar={() => void restaurar()} />}
    <div className="identidad-pie"><small>Los cambios afectan a todos los usuarios de la asociación al recargar.</small>
      <div className="editor-actions">
        <button type="button" className="secondary" disabled={ocupado || !base.version} onClick={() => setConfirmando(true)}>Restaurar predeterminados</button>
        <button type="button" className="secondary" disabled={ocupado || !sucio} onClick={recargar}>Cancelar edición</button>
        <button type="button" disabled={ocupado || bloqueado || !sucio || hexInvalido || advertencias.length > 0}
          onClick={() => void guardar()}>{ocupado ? 'Guardando…' : 'Guardar cambios'}</button>
      </div></div>
  </main>
}
