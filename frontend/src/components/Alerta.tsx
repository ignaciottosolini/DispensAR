import type { ReactNode } from 'react'

// Mensajes de estado: error con role=alert, aviso exitoso con role=status.
export function AlertaError({ children }: { children: ReactNode }) {
  return <p className="error" role="alert">{children}</p>
}

export function Aviso({ children }: { children: ReactNode }) {
  return <p className="notice" role="status">{children}</p>
}
