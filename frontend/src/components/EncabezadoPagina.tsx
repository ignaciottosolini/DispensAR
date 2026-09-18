import type { ReactNode } from 'react'

// Encabezado común de las pantallas: etiqueta, título, subtítulo y acciones.
export default function EncabezadoPagina({ eyebrow, titulo, subtitulo, children }:
  { eyebrow: string; titulo: ReactNode; subtitulo?: string; children?: ReactNode }) {
  return <div className="dashboard-heading"><div><p className="eyebrow">{eyebrow}</p><h1>{titulo}</h1>
    {subtitulo && <p className="subtitle">{subtitulo}</p>}</div>
    {children && <div className="dashboard-actions">{children}</div>}</div>
}
