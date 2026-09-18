import { useCallback, useEffect, useRef, useState } from 'react'
import type { FormEvent } from 'react'
import { ApiError, request, mutate } from './api'
import Dashboard from './Dashboard'
import ElementCatalog from './ElementCatalog'
import Pacientes from './Pacientes'
import Productos from './Productos'
import Lotes from './Lotes'
import IdentidadVisual from './IdentidadVisual'
import Marca from './components/Marca'
import { AlertaError } from './components/Alerta'
import { aplicarTema, limpiarTema } from './theme/tema'
import type { IdentidadRespuesta } from './theme/tema'

type Account = { id: string; email: string; tenantId: number; rol: string; esAdministradorPlataforma: boolean }
type View = 'dashboard' | 'pacientes' | 'productos' | 'lotes' | 'elements' | 'identidad'

export default function App() {
  const [account, setAccount] = useState<Account | null>(null)
  const [identidad, setIdentidad] = useState<IdentidadRespuesta | null>(null)
  const [view, setView] = useState<View>('dashboard')
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  // Al expirar o cerrar la sesión se limpia el tema del tenant: la identidad de una
  // asociación nunca debe mostrarse a otro usuario. La secuencia descarta respuestas
  // de /api/identidad que lleguen tarde (por ejemplo, después de un logout).
  const secuenciaIdentidad = useRef(0)
  const expired = useCallback(() => {
    secuenciaIdentidad.current += 1
    setAccount(null); setIdentidad(null); setView('dashboard')
    limpiarTema(); setError('Tu sesión venció. Volvé a ingresar.')
  }, [])
  // Si falla la configuración visual, la aplicación sigue con el tema predeterminado.
  const cargarIdentidad = useCallback(async () => {
    const secuencia = secuenciaIdentidad.current
    try {
      const respuesta = await (await request('/api/identidad')).json() as IdentidadRespuesta
      if (secuencia === secuenciaIdentidad.current) setIdentidad(respuesta)
    }
    catch (reason) { if (reason instanceof ApiError && reason.status === 401) expired() }
  }, [expired])
  useEffect(() => { aplicarTema(identidad?.identidad ?? null) }, [identidad])
  useEffect(() => {
    const controller = new AbortController()
    request('/api/auth/me', { signal: controller.signal }).then(response => response.json()).then((me: Account) => {
      if (controller.signal.aborted) return
      setAccount(me); void cargarIdentidad()
    }).catch(reason => {
      if (!controller.signal.aborted && !(reason instanceof ApiError && reason.status === 401))
        setError(reason instanceof Error ? reason.message : 'No se pudo recuperar la sesión.')
    }).finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [cargarIdentidad])
  async function login(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setBusy(true); setError('')
    try {
      await mutate('/api/auth/login', { email: email.trim(), password })
      setPassword('')
      const me = await (await request('/api/auth/me')).json() as Account
      setAccount(me)
      // El backend identifica el tenant desde la sesión; el frontend solo pide su tema.
      await cargarIdentidad()
    } catch (reason) { setError(reason instanceof Error ? reason.message : 'No se pudo iniciar sesión.') }
    finally { setBusy(false) }
  }
  async function logout() {
    setBusy(true); setError('')
    secuenciaIdentidad.current += 1
    try {
      await mutate('/api/auth/logout')
      setAccount(null); setIdentidad(null); setView('dashboard'); setPassword(''); limpiarTema()
    }
    catch (reason) {
      if (reason instanceof ApiError && reason.status === 401) { setAccount(null); setIdentidad(null); limpiarTema() }
      else setError(reason instanceof Error ? reason.message : 'No se pudo cerrar sesión.')
    } finally { setBusy(false) }
  }
  if (loading) return <main><p role="status">Recuperando sesión…</p></main>
  const canWrite = account !== null && (account.rol === 'Administrador' || account.rol === 'Administrativo')
  const puedeEditarIdentidad = account !== null && account.rol === 'Administrador'
  if (account) return <div className="app-shell">
    <header className="topbar"><Marca descripcion={identidad?.descripcion ?? 'DispensAR'}
      logoPrincipalUrl={identidad?.identidad?.logoPrincipalUrl ?? null} logoCompactoUrl={identidad?.identidad?.logoCompactoUrl ?? null} />
      <div className="session"><div><strong>{account.email}</strong><small>{account.rol}{account.esAdministradorPlataforma ? " · Plataforma" : ""}</small></div>
        <button className="secondary" disabled={busy} onClick={() => void logout()}>Cerrar sesión</button></div></header>
    {error && <AlertaError>{error}</AlertaError>}
    <div className="workspace-layout">
      <aside className="sidebar"><p className="eyebrow">PRINCIPAL</p><nav aria-label="Menú principal">
        <button className={view === 'dashboard' ? 'nav-item selected' : 'nav-item'} aria-current={view === 'dashboard' ? 'page' : undefined} onClick={() => setView('dashboard')}><span aria-hidden="true">▦</span> Dashboard</button>
        <p className="eyebrow sidebar-heading">OPERACIÓN</p>
        <button className={view === 'pacientes' ? 'nav-item selected' : 'nav-item'} aria-current={view === 'pacientes' ? 'page' : undefined} onClick={() => setView('pacientes')}><span aria-hidden="true">◎</span> Pacientes</button>
        <button className={view === 'productos' ? 'nav-item selected' : 'nav-item'} aria-current={view === 'productos' ? 'page' : undefined} onClick={() => setView('productos')}><span aria-hidden="true">▤</span> Productos</button>
        <button className={view === 'lotes' ? 'nav-item selected' : 'nav-item'} aria-current={view === 'lotes' ? 'page' : undefined} onClick={() => setView('lotes')}><span aria-hidden="true">≡</span> Lotes</button>
        {account.esAdministradorPlataforma && <><p className="eyebrow sidebar-heading">ADMINISTRACIÓN</p>
          <button className={view === 'elements' ? 'nav-item selected' : 'nav-item'} aria-current={view === 'elements' ? 'page' : undefined} onClick={() => setView('elements')}><span aria-hidden="true">◈</span> Elementos</button></>}
        {puedeEditarIdentidad && <><p className="eyebrow sidebar-heading">CONFIGURACIÓN</p>
          <button className={view === 'identidad' ? 'nav-item selected' : 'nav-item'} aria-current={view === 'identidad' ? 'page' : undefined} onClick={() => setView('identidad')}><span aria-hidden="true">✱</span> Identidad visual</button></>}
      </nav><p className="sidebar-note">Asociación #{account.tenantId}</p></aside>
      <div className="workspace-content">{view === 'elements' && account.esAdministradorPlataforma
        ? <ElementCatalog key={account.id} onExpired={expired} />
        : view === 'pacientes' ? <Pacientes key={account.id} onExpired={expired} canWrite={canWrite} />
        : view === 'productos' ? <Productos key={account.id} onExpired={expired} canWrite={canWrite} />
        : view === 'lotes' ? <Lotes key={account.id} onExpired={expired} canWrite={canWrite} />
        : view === 'identidad' && puedeEditarIdentidad
        ? <IdentidadVisual key={account.id} onExpired={expired} tenantId={account.tenantId} onActualizada={() => void cargarIdentidad()} />
        : <Dashboard key={account.id} onExpired={expired} />}</div>
    </div>
  </div>
  return <main className="login-page"><p className="eyebrow">TU ORGANIZACIÓN EN UN SOLO LUGAR</p><h1>DispensAR</h1>
    <section className="login"><h2>Iniciar sesión</h2><p>Accedé a tu asociación con tu cuenta.</p>
      <form onSubmit={event => void login(event)}>
        <label htmlFor="email">Email</label><input id="email" type="email" autoComplete="username" maxLength={256} required disabled={busy}
          value={email} onChange={event => setEmail(event.target.value)} />
        <label htmlFor="password">Contraseña</label><input id="password" type="password" autoComplete="current-password" maxLength={1024} required disabled={busy}
          value={password} onChange={event => setPassword(event.target.value)} />
        {error && <AlertaError>{error}</AlertaError>}
        <button type="submit" disabled={busy}>{busy ? 'Ingresando…' : 'Ingresar'}</button>
      </form></section></main>
}
