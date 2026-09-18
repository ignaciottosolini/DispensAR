// Utilidades de color compartidas por el tema de tenant y la pantalla de identidad.
// Las reglas de contraste coinciden con backend/DispensAR.Api/Identidad/Colores.cs.

export type RGB = { r: number; g: number; b: number }

export function normalizarHex(valor: string): string | null {
  const texto = valor.trim().toLowerCase()
  const corto = /^#([0-9a-f])([0-9a-f])([0-9a-f])$/u.exec(texto)
  if (corto) return `#${corto[1]}${corto[1]}${corto[2]}${corto[2]}${corto[3]}${corto[3]}`
  return /^#[0-9a-f]{6}$/u.test(texto) ? texto : null
}

export function descomponer(hex: string): RGB {
  return { r: Number.parseInt(hex.slice(1, 3), 16), g: Number.parseInt(hex.slice(3, 5), 16), b: Number.parseInt(hex.slice(5, 7), 16) }
}

export function aHex({ r, g, b }: RGB): string {
  const canal = (v: number) => Math.max(0, Math.min(255, Math.round(v))).toString(16).padStart(2, '0')
  return `#${canal(r)}${canal(g)}${canal(b)}`
}

// t = 0 devuelve `base`, t = 1 devuelve `capa`.
export function mezclar(base: string, capa: string, t: number): string {
  const a = descomponer(base)
  const b = descomponer(capa)
  return aHex({ r: a.r + (b.r - a.r) * t, g: a.g + (b.g - a.g) * t, b: a.b + (b.b - a.b) * t })
}

function canalLineal(v: number): number {
  const s = v / 255
  return s <= 0.03928 ? s / 12.92 : Math.pow((s + 0.055) / 1.055, 2.4)
}

export function luminancia(hex: string): number {
  const { r, g, b } = descomponer(hex)
  return 0.2126 * canalLineal(r) + 0.7152 * canalLineal(g) + 0.0722 * canalLineal(b)
}

export function contraste(a: string, b: string): number {
  const primero = luminancia(a)
  const segundo = luminancia(b)
  return (Math.max(primero, segundo) + 0.05) / (Math.min(primero, segundo) + 0.05)
}

export function contrasteTexto(fondo: string): { texto: string; ratio: number } {
  const claro = contraste('#ffffff', fondo)
  const oscuro = contraste('#111111', fondo)
  return oscuro > claro ? { texto: '#111111', ratio: oscuro } : { texto: '#ffffff', ratio: claro }
}
