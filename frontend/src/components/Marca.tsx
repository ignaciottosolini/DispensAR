// Marca de la asociación: logo principal, logo compacto en pantallas angostas y,
// si no hay logo, una alternativa con las iniciales y el nombre de la organización.
export default function Marca({ descripcion, logoPrincipalUrl, logoCompactoUrl }:
  { descripcion: string; logoPrincipalUrl?: string | null; logoCompactoUrl?: string | null }) {
  const iniciales = descripcion.trim().split(/\s+/u).slice(0, 2).map(palabra => palabra.charAt(0).toUpperCase()).join('')
  const logoCompleto = logoPrincipalUrl
    ? <img className={logoCompactoUrl ? 'brand-logo con-compacto' : 'brand-logo'} src={logoPrincipalUrl} alt={`Logo de ${descripcion}`} />
    : null
  return <a className="brand" href="/" aria-label={`DispensAR — ${descripcion}`}>
    {logoCompleto}
    {logoCompactoUrl && <img className="brand-logo brand-logo-compacto" src={logoCompactoUrl} alt="" aria-hidden="true" />}
    {!logoPrincipalUrl && <span className="brand-mark" aria-hidden="true">{iniciales}</span>}
    <span className="brand-nombre">{descripcion}<span>Gestión de asociaciones</span></span>
  </a>
}
