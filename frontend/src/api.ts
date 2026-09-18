export class ApiError extends Error {
  status: number
  constructor(status: number, message: string) { super(message); this.status = status }
}
export async function request(path: string, options: RequestInit = {}) {
  const response = await fetch(path, { ...options, credentials: 'same-origin', cache: 'no-store' })
  if (!response.ok) {
    let message = response.status === 401 ? 'Email o contraseña incorrectos, cuenta bloqueada o sesión vencida.'
      : response.status === 403 ? 'No tenés permisos para esta acción.'
      : response.status === 409 ? 'La configuración cambió. Recargá antes de guardar.'
      : response.status === 404 ? 'El elemento solicitado no está disponible.'
      : response.status === 429 ? 'Demasiados intentos. Esperá un minuto y volvé a intentar.'
      : response.status === 400 ? 'No se pudo validar el formulario. Volvé a intentar.'
      : 'No pudimos completar la solicitud. Verificá que la API y SQL Server estén disponibles.'
    if (response.status === 400 || response.status === 409) {
      const problem = await response.json().catch(() => null) as { detail?: unknown } | null
      if (typeof problem?.detail === 'string') message = problem.detail
    }
    throw new ApiError(response.status, message)
  }
  return response
}
export async function mutate(path: string, body?: unknown, method = 'POST', headers: Record<string, string> = {}) {
  const { token } = await (await request('/api/auth/csrf')).json() as { token: string }
  return request(path, {
    method, headers: { ...headers, 'Content-Type': 'application/json', 'X-CSRF-TOKEN': token },
    body: body === undefined ? undefined : JSON.stringify(body),
  })
}

// Subida multipart: el navegador fija el Content-Type con el límite. El token CSRF
// viaja en la misma cabecera que en el resto de las escrituras.
export async function subirArchivo(path: string, archivo: File, campo = 'archivo') {
  const { token } = await (await request('/api/auth/csrf')).json() as { token: string }
  const forma = new FormData()
  forma.append(campo, archivo)
  return request(path, { method: 'POST', headers: { 'X-CSRF-TOKEN': token }, body: forma })
}


