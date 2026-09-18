import type { InputHTMLAttributes, LabelHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react'

type Props = {
  id: string
  etiqueta: string
  ayuda?: ReactNode
  children?: ReactNode
} & InputHTMLAttributes<HTMLInputElement>

// Campo con etiqueta y ayuda accesibles, usado por todos los formularios.
export function Campo({ id, etiqueta, ayuda, children, ...input }: Props) {
  const ayudaId = ayuda ? `${id}-ayuda` : undefined
  return <div><label htmlFor={id}>{etiqueta}</label>
    <input id={id} aria-describedby={ayudaId} {...input} />
    {ayuda && <small id={ayudaId}>{ayuda}</small>}{children}</div>
}

type SeleccionProps = {
  id: string
  etiqueta: string
  ayuda?: ReactNode
  children: ReactNode
} & SelectHTMLAttributes<HTMLSelectElement>

export function Selector({ id, etiqueta, ayuda, children, ...select }: SeleccionProps) {
  const ayudaId = ayuda ? `${id}-ayuda` : undefined
  return <div><label htmlFor={id}>{etiqueta}</label>
    <select id={id} aria-describedby={ayudaId} {...select}>{children}</select>
    {ayuda && <small id={ayudaId}>{ayuda}</small>}</div>
}

type EtiquetaProps = {
  className?: string
} & LabelHTMLAttributes<HTMLLabelElement>

export function EtiquetaCheck({ className = 'checkbox-label', children, ...label }: EtiquetaProps) {
  return <label className={className} {...label}>{children}</label>
}

type AreaProps = {
  id: string
  etiqueta: string
  ayuda?: ReactNode
} & TextareaHTMLAttributes<HTMLTextAreaElement>

export function AreaTexto({ id, etiqueta, ayuda, ...area }: AreaProps) {
  const ayudaId = ayuda ? `${id}-ayuda` : undefined
  return <><label htmlFor={id}>{etiqueta}</label>
    <textarea id={id} aria-describedby={ayudaId} {...area} />
    {ayuda && <small id={ayudaId}>{ayuda}</small>}</>
}
