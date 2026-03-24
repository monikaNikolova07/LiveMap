using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using LiveMap.Data.Models;

namespace LiveMap.Web.Helpers;

public static class CountryUiHelper
{
    private static readonly IReadOnlyDictionary<string, Country> AliasMap = new Dictionary<string, Country>(StringComparer.OrdinalIgnoreCase)
    {
        ["antigua"] = Country.AntiguaAndBarbuda,
        ["bahamas"] = Country.Bahamas,
        ["bolivia (plurinational state of)"] = Country.Bolivia,
        ["bosnia and herzegovina"] = Country.BosniaAndHerzegovina,
        ["brunei darussalam"] = Country.Brunei,
        ["cape verde"] = Country.CapeVerde,
        ["cabo verde"] = Country.CapeVerde,
        ["central african republic"] = Country.CentralAfricanRepublic,
        ["comoros"] = Country.Comoros,
        ["congo"] = Country.Congo,
        ["republic of the congo"] = Country.Congo,
        ["democratic republic of the congo"] = Country.Congo,
        ["czechia"] = Country.CzechRepublic,
        ["dominican republic"] = Country.DominicanRepublic,
        ["east timor"] = Country.TimorLeste,
        ["equatorial guinea"] = Country.EquatorialGuinea,
        ["gambia"] = Country.Gambia,
        ["holy see"] = Country.VaticanCity,
        ["iran, islamic republic of"] = Country.Iran,
        ["korea, democratic people's republic of"] = Country.NorthKorea,
        ["korea, republic of"] = Country.SouthKorea,
        ["lao people's democratic republic"] = Country.Laos,
        ["marshall islands"] = Country.MarshallIslands,
        ["micronesia (federated states of)"] = Country.Micronesia,
        ["moldova, republic of"] = Country.Moldova,
        ["myanmar"] = Country.Myanmar,
        ["north macedonia"] = Country.NorthMacedonia,
        ["russian federation"] = Country.Russia,
        ["saint kitts and nevis"] = Country.SaintKittsAndNevis,
        ["saint lucia"] = Country.SaintLucia,
        ["saint vincent and the grenadines"] = Country.SaintVincentAndTheGrenadines,
        ["sao tome and principe"] = Country.SaoTomeAndPrincipe,
        ["solomon islands"] = Country.SolomonIslands,
        ["south korea"] = Country.SouthKorea,
        ["north korea"] = Country.NorthKorea,
        ["syrian arab republic"] = Country.Syria,
        ["tanzania, united republic of"] = Country.Tanzania,
        ["the bahamas"] = Country.Bahamas,
        ["the gambia"] = Country.Gambia,
        ["trinidad and tobago"] = Country.TrinidadAndTobago,
        ["turkiye"] = Country.Turkey,
        ["türkiye"] = Country.Turkey,
        ["united kingdom"] = Country.UnitedKingdom,
        ["uk"] = Country.UnitedKingdom,
        ["united states"] = Country.UnitedStates,
        ["united states of america"] = Country.UnitedStates,
        ["usa"] = Country.UnitedStates,
        ["venezuela, bolivarian republic of"] = Country.Venezuela,
        ["viet nam"] = Country.Vietnam
    };

    private static readonly Lazy<Dictionary<Country, string>> CountryCodeMap = new(BuildCountryCodeMap);

    public static string GetDisplayName(Country country)
    {
        var raw = country.ToString();
        var withSpaces = Regex.Replace(raw, "([a-z])([A-Z])", "$1 $2");
        return withSpaces
            .Replace("And", " and ")
            .Replace(" Of ", " of ")
            .Replace(" The ", " the ")
            .Trim();
    }

    public static string GetFlagEmoji(Country country)
    {
        if (!CountryCodeMap.Value.TryGetValue(country, out var isoCode) || isoCode.Length != 2)
        {
            return "🌍";
        }

        isoCode = isoCode.ToUpperInvariant();
        var first = char.ConvertFromUtf32(0x1F1E6 + (isoCode[0] - 'A'));
        var second = char.ConvertFromUtf32(0x1F1E6 + (isoCode[1] - 'A'));
        return first + second;
    }

    public static bool TryParseCountry(string? value, out Country country)
    {
        country = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = NormalizeKey(value);

        foreach (var enumValue in Enum.GetValues<Country>())
        {
            if (NormalizeKey(enumValue.ToString()) == normalized || NormalizeKey(GetDisplayName(enumValue)) == normalized)
            {
                country = enumValue;
                return true;
            }
        }

        foreach (var alias in AliasMap)
        {
            if (NormalizeKey(alias.Key) == normalized)
            {
                country = alias.Value;
                return true;
            }
        }

        return false;
    }

    public static Dictionary<string, string> GetClientAliasMap()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var country in Enum.GetValues<Country>())
        {
            map[NormalizeKey(country.ToString())] = country.ToString();
            map[NormalizeKey(GetDisplayName(country))] = country.ToString();
        }

        foreach (var alias in AliasMap)
        {
            map[NormalizeKey(alias.Key)] = alias.Value.ToString();
        }

        return map;
    }

    public static string NormalizeKey(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var formD = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var character in formD)
        {
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(character);
            if (unicodeCategory == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
            }
        }

        return builder.ToString();
    }

    private static Dictionary<Country, string> BuildCountryCodeMap()
    {
        var regionLookup = CultureInfo.GetCultures(CultureTypes.SpecificCultures)
            .Select(culture =>
            {
                try
                {
                    return new RegionInfo(culture.Name);
                }
                catch
                {
                    return null;
                }
            })
            .Where(region => region is not null)
            .GroupBy(region => NormalizeKey(region!.EnglishName))
            .ToDictionary(group => group.Key, group => group.First()!.TwoLetterISORegionName, StringComparer.OrdinalIgnoreCase);

        var manualCodes = new Dictionary<Country, string>
        {
            [Country.AntiguaAndBarbuda] = "AG",
            [Country.Bahamas] = "BS",
            [Country.BosniaAndHerzegovina] = "BA",
            [Country.Brunei] = "BN",
            [Country.CapeVerde] = "CV",
            [Country.CentralAfricanRepublic] = "CF",
            [Country.Comoros] = "KM",
            [Country.Congo] = "CG",
            [Country.CzechRepublic] = "CZ",
            [Country.DominicanRepublic] = "DO",
            [Country.EquatorialGuinea] = "GQ",
            [Country.Eswatini] = "SZ",
            [Country.Iran] = "IR",
            [Country.Laos] = "LA",
            [Country.MarshallIslands] = "MH",
            [Country.Micronesia] = "FM",
            [Country.Moldova] = "MD",
            [Country.NewZealand] = "NZ",
            [Country.NorthKorea] = "KP",
            [Country.NorthMacedonia] = "MK",
            [Country.PapuaNewGuinea] = "PG",
            [Country.SaintKittsAndNevis] = "KN",
            [Country.SaintLucia] = "LC",
            [Country.SaintVincentAndTheGrenadines] = "VC",
            [Country.SaoTomeAndPrincipe] = "ST",
            [Country.SolomonIslands] = "SB",
            [Country.SouthKorea] = "KR",
            [Country.SouthSudan] = "SS",
            [Country.SriLanka] = "LK",
            [Country.TimorLeste] = "TL",
            [Country.TrinidadAndTobago] = "TT",
            [Country.UnitedArabEmirates] = "AE",
            [Country.UnitedKingdom] = "GB",
            [Country.UnitedStates] = "US",
            [Country.VaticanCity] = "VA"
        };

        var result = new Dictionary<Country, string>();
        foreach (var country in Enum.GetValues<Country>())
        {
            if (manualCodes.TryGetValue(country, out var manualCode))
            {
                result[country] = manualCode;
                continue;
            }

            var displayNameKey = NormalizeKey(GetDisplayName(country));
            var enumKey = NormalizeKey(country.ToString());

            if (regionLookup.TryGetValue(displayNameKey, out var code) || regionLookup.TryGetValue(enumKey, out code))
            {
                result[country] = code;
            }
        }

        return result;
    }
}
