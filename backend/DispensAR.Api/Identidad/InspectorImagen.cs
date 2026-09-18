namespace DispensAR.Api.Identidad;

public sealed record ContenidoImagen(string Extension, string ContentType, int Ancho, int Alto);

// Validates the real content of an uploaded image: signature and dimensions read from
// the file bytes, never from the client-provided name or content type.
public static class InspectorImagen
{
    public const int MaximoBytes = 1_048_576;
    public const int MaximoLado = 4096;
    public const long MaximoPixeles = 12_000_000;

    public static ContenidoImagen? Inspeccionar(ReadOnlySpan<byte> datos)
    {
        if (datos.Length == 0 || datos.Length > MaximoBytes) return null;
        return ExaminarPng(datos) ?? ExaminarJpeg(datos) ?? ExaminarWebp(datos);
    }

    public static bool DentroDeLimites(ContenidoImagen imagen) =>
        imagen.Ancho is > 0 and <= MaximoLado && imagen.Alto is > 0 and <= MaximoLado &&
        (long)imagen.Ancho * imagen.Alto <= MaximoPixeles;

    private static ContenidoImagen? ExaminarPng(ReadOnlySpan<byte> datos)
    {
        ReadOnlySpan<byte> firma = [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];
        if (!datos.StartsWith(firma)) return null;
        ReadOnlySpan<byte> ihdr = [(byte)'I', (byte)'H', (byte)'D', (byte)'R'];
        if (datos.Length < 24 || !datos[12..16].SequenceEqual(ihdr)) return null;
        return Aceptar(".png", "image/png", EnteroGrande(datos, 16), EnteroGrande(datos, 20));
    }

    private static ContenidoImagen? ExaminarJpeg(ReadOnlySpan<byte> datos)
    {
        if (datos.Length < 4 || datos[0] != 0xFF || datos[1] != 0xD8) return null;
        var posicion = 2;
        while (posicion + 9 < datos.Length)
        {
            if (datos[posicion] != 0xFF) { posicion++; continue; }
            var marcador = datos[posicion + 1];
            // SOF0..SOF3, SOF5..SOF7, SOF9..SOF11 and SOF13..SOF15 carry the frame size.
            if (marcador is >= 0xC0 and <= 0xC3 or >= 0xC5 and <= 0xC7 or >= 0xC9 and <= 0xCB or >= 0xCD and <= 0xCF)
            {
                var alto = EnteroGrande(datos, posicion + 5);
                var ancho = EnteroGrande(datos, posicion + 7);
                return Aceptar(".jpg", "image/jpeg", ancho, alto);
            }
            if (marcador is 0xD8 or 0x01 or >= 0xD0 and <= 0xD7) { posicion += 2; continue; }
            var longitud = EnteroGrande(datos, posicion + 2);
            if (longitud < 2) return null;
            posicion += 2 + longitud;
        }
        return null;
    }

    private static ContenidoImagen? ExaminarWebp(ReadOnlySpan<byte> datos)
    {
        ReadOnlySpan<byte> riff = [(byte)'R', (byte)'I', (byte)'F', (byte)'F'];
        ReadOnlySpan<byte> webp = [(byte)'W', (byte)'E', (byte)'B', (byte)'P'];
        if (datos.Length < 30 || !datos[..4].SequenceEqual(riff) || !datos[8..12].SequenceEqual(webp)) return null;
        if (datos[12..16].SequenceEqual("VP8 "u8))
        {
            var ancho = (datos[26] | datos[27] << 8) & 0x3FFF;
            var alto = (datos[28] | datos[29] << 8) & 0x3FFF;
            return Aceptar(".webp", "image/webp", ancho, alto);
        }
        if (datos[12..16].SequenceEqual("VP8L"u8))
        {
            var bits = datos[21] | datos[22] << 8 | datos[23] << 16 | datos[24] << 24;
            return Aceptar(".webp", "image/webp", (bits & 0x3FFF) + 1, ((bits >> 14) & 0x3FFF) + 1);
        }
        if (datos[12..16].SequenceEqual("VP8X"u8))
        {
            var ancho = (datos[24] | datos[25] << 8 | datos[26] << 16) + 1;
            var alto = (datos[27] | datos[28] << 8 | datos[29] << 16) + 1;
            return Aceptar(".webp", "image/webp", ancho, alto);
        }
        return null;
    }

    private static ContenidoImagen? Aceptar(string extension, string contentType, int ancho, int alto) =>
        ancho <= 0 || alto <= 0 ? null : new ContenidoImagen(extension, contentType, ancho, alto);

    private static int EnteroGrande(ReadOnlySpan<byte> datos, int offset) =>
        datos[offset] << 24 | datos[offset + 1] << 16 | datos[offset + 2] << 8 | datos[offset + 3];
}
