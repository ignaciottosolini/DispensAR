import { contraste, mezclar } from './colores'

export const SUPERFICIE = '#ffffff'
export const FONDO_APLICACION = '#eff5f2'

// Tema predeterminado de DispensAR. Debe coincidir con styles/tokens.css y con
// IdentidadEndpoints.cs, en el backend.
export const TEMA_DETERMINADO = {
  primario: '#195d4b',
  secundario: '#2e6b52',
  sidebarFondo: '#ffffff',
  sidebarTexto: '#526d60',
} as const

export type ColoresBase = {
  primario: string
  secundario: string
  sidebarFondo: string
  sidebarTexto: string
}

// Respuesta de GET /api/identidad. Un campo de color nulo significa "usa el
// predeterminado de DispensAR"; solo se pisa la variable CSS correspondiente
// cuando el asociación configuró ese valor.
export type Identidad = {
  logoPrincipalUrl: string | null
  logoCompactoUrl: string | null
  colorPrimario: string | null
  colorSecundario: string | null
  colorSidebarFondo: string | null
  colorSidebarTexto: string | null
  version: string
}

export type IdentidadRespuesta = { descripcion: string; identidad: Identidad | null }

// Combinación efectiva de los cuatro colores, aplicando el predeterminado donde falta.
export function coloresBase(identidad: Identidad | null): ColoresBase {
  return {
    primario: identidad?.colorPrimario ?? TEMA_DETERMINADO.primario,
    secundario: identidad?.colorSecundario ?? TEMA_DETERMINADO.secundario,
    sidebarFondo: identidad?.colorSidebarFondo ?? TEMA_DETERMINADO.sidebarFondo,
    sidebarTexto: identidad?.colorSidebarTexto ?? TEMA_DETERMINADO.sidebarTexto,
  }
}

const esClaro = (hex: string) => {
  const [r, g, b] = [1, 3, 5].map(i => Number.parseInt(hex.slice(i, i + 2), 16))
  return (0.299 * r + 0.587 * g + 0.114 * b) / 255 > 0.6
}

// Resolución central del tema: un único lugar deriva los estados de interacción
// (hover, foco, seleccionado) y los ribetes de la barra lateral.
export function variablesTema(identidad: Identidad | null): Record<string, string> {
  if (!identidad) return {}
  const base = coloresBase(identidad)
  const variables: Record<string, string> = {}
  if (identidad.colorPrimario) {
    variables['--color-primario'] = base.primario
    variables['--color-foco'] = mezclar(base.primario, SUPERFICIE, 0.5)
    variables['--color-marca-suave'] = mezclar(base.primario, SUPERFICIE, 0.9)
    variables['--color-marca-superficie'] = mezclar(base.primario, SUPERFICIE, 0.88)
  }
  if (identidad.colorSecundario) variables['--color-secundario'] = base.secundario
  if (identidad.colorSidebarFondo || identidad.colorSidebarTexto) {
    const claro = esClaro(base.sidebarFondo)
    variables['--color-sidebar-fondo'] = base.sidebarFondo
    variables['--color-sidebar-texto'] = base.sidebarTexto
    variables['--color-sidebar-linea'] = mezclar(base.sidebarTexto, base.sidebarFondo, 0.8)
    variables['--color-sidebar-suave'] = mezclar(base.sidebarTexto, base.sidebarFondo, 0.13)
    variables['--color-sidebar-seleccionado'] = mezclar(base.primario, base.sidebarFondo, claro ? 0.1 : 0.18)
    variables['--color-sidebar-seleccionado-texto'] =
      contraste(base.primario, base.sidebarFondo) >= 4.5 ? base.primario : base.sidebarTexto
  }
  return variables
}

// Validación local idéntica a la del backend para advertir antes de guardar.
export function erroresContraste(base: ColoresBase): string[] {
  const errores: string[] = []
  if (contraste(base.primario, SUPERFICIE) < 4.5)
    errores.push('El color primario necesita un contraste de al menos 4.5:1 sobre blanco para que el texto de los botones sea legible.')
  if (contraste(base.secundario, SUPERFICIE) < 3 || contraste(base.secundario, FONDO_APLICACION) < 3)
    errores.push('El color secundario necesita un contraste de al menos 3:1 sobre el fondo de la aplicación.')
  if (contraste(base.sidebarTexto, base.sidebarFondo) < 4.5)
    errores.push('El texto de la barra lateral necesita un contraste de al menos 4.5:1 sobre su fondo.')
  return errores
}

const VARIABLES_TEMA = ['--color-primario', '--color-secundario', '--color-foco', '--color-marca-suave',
  '--color-marca-superficie', '--color-sidebar-fondo', '--color-sidebar-texto', '--color-sidebar-linea',
  '--color-sidebar-suave', '--color-sidebar-seleccionado', '--color-sidebar-seleccionado-texto']

let limpiarActual: (() => void) | null = null

// Se llama al autenticar (login o /me) y al guardar la identidad. Al cerrar sesión
// o expirar la sesión se llama a limpiarTema para no heredar la identidad anterior.
export function aplicarTema(identidad: Identidad | null): void {
  limpiarTema()
  const raiz = document.documentElement
  const meta = document.querySelector('meta[name="theme-color"]')
  const colorMetaAnterior = meta?.getAttribute('content') ?? null
  const variables = variablesTema(identidad)
  for (const [clave, valor] of Object.entries(variables)) raiz.style.setProperty(clave, valor)
  if (meta && variables['--color-primario']) meta.setAttribute('content', variables['--color-primario'])
  limpiarActual = () => {
    for (const clave of VARIABLES_TEMA) raiz.style.removeProperty(clave)
    if (meta && colorMetaAnterior) meta.setAttribute('content', colorMetaAnterior)
  }
}

export function limpiarTema(): void {
  limpiarActual?.()
  limpiarActual = null
}
