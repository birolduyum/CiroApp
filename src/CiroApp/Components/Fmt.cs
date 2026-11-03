using System.Globalization;

namespace CiroApp.Components;

public static class Fmt
{
    private static readonly CultureInfo Tr = CultureInfo.GetCultureInfo("tr-TR");

    public static string Tutar(decimal tutar) => tutar.ToString("N2", Tr);
}
