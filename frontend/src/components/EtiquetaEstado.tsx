import type { ReactNode } from 'react'

// Etiqueta de estado con los colores semánticos compartidos.
export default function EtiquetaEstado({ children, activo = false, inline = false }:
  { children: ReactNode; activo?: boolean; inline?: boolean }) {
  const clases = ['state-badge', activo ? 'active' : '', inline ? 'state-inline' : ''].filter(Boolean).join(' ')
  return <span className={clases}>{children}</span>
}
