import type { ReactNode } from 'react'

type Props = {
  id: string
  titulo: ReactNode
  descripcion: ReactNode
  confirmar?: string
  ocupado?: boolean
  bloqueado?: boolean
  onCancelar: () => void
  onConfirmar: () => void
}

// Diálogo de baja lógica, igual para todas las listas de la aplicación.
export default function Confirmacion({ id, titulo, descripcion, confirmar = 'Confirmar baja',
  ocupado = false, bloqueado = false, onCancelar, onConfirmar }: Props) {
  return <section className="delete-confirmation" role="alertdialog" aria-labelledby={`${id}-titulo`} aria-describedby={`${id}-descripcion`}>
    <h2 id={`${id}-titulo`}>{titulo}</h2>
    <p id={`${id}-descripcion`}>{descripcion}</p>
    <div className="editor-actions"><button className="secondary" disabled={ocupado} onClick={onCancelar}>Cancelar</button>
      <button className="danger" disabled={ocupado || bloqueado} onClick={() => void onConfirmar()}>{ocupado ? 'Procesando…' : confirmar}</button></div></section>
}
