using System.Globalization;

namespace DispensAR.Api.Identidad;

// The only color parser the API accepts: no CSS keywords, functions or arbitrary text.
public static class Colores
{
    public static bool EsHexadecimal(string? valor, out string normalizado)
    {
        normalizado = "";
        if (string.IsNullOrWhiteSpace(valor)) return false;
        var texto = valor.Trim();
        Span<char> digitos = stackalloc char[7];
        digitos[0] = '#';
        if (texto.Length == 4)
        {
            if (texto[0] != '#') return false;
            for (var i = 0; i < 3; i++)
            {
                if (!TryHex(texto[i + 1], out var digito)) return false;
                digitos[1 + i * 2] = digito;
                digitos[2 + i * 2] = digito;
            }
            normalizado = new string(digitos[..7]);
            return true;
        }
        if (texto.Length == 7)
        {
            if (texto[0] != '#') return false;
            for (var i = 0; i < 6; i++)
            {
                if (!TryHex(texto[i + 1], out var digito)) return false;
                digitos[i + 1] = digito;
            }
            normalizado = new string(digitos[..7]);
            return true;
        }
        return false;
    }

    private static bool TryHex(char caracter, out char normalizado)
    {
        if (caracter is >= '0' and <= '9') { normalizado = caracter; return true; }
        if (caracter is >= 'a' and <= 'f') { normalizado = caracter; return true; }
        if (caracter is >= 'A' and <= 'F') { normalizado = char.ToLowerInvariant(caracter); return true; }
        normalizado = '\0';
        return false;
    }

    public static (byte R, byte G, byte B) Descomponer(string hex) => (
        (byte)int.Parse(hex.AsSpan(1, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        (byte)int.Parse(hex.AsSpan(3, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture),
        (byte)int.Parse(hex.AsSpan(5, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture));

    // WCAG 2.x relative luminance.
    public static double Luminancia(string hex)
    {
        var (r, g, b) = Descomponer(hex);
        return 0.2126 * Canal(r) + 0.7152 * Canal(g) + 0.0722 * Canal(b);
    }

    public static double Contraste(string a, string b)
    {
        var primero = Luminancia(a);
        var segundo = Luminancia(b);
        return (Math.Max(primero, segundo) + 0.05) / (Math.Min(primero, segundo) + 0.05);
    }

    private static double Canal(byte valor)
    {
        var s = valor / 255d;
        return s <= 0.03928 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
    }
}
