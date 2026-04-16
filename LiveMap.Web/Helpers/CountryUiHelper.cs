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



    public static string GetFlagPaletteStyle(Country country)
    {
        return country switch
        {
            Country.China => "background: linear-gradient(135deg, rgba(222,41,16,0.96) 0%, rgba(222,41,16,0.92) 72%, rgba(255,222,0,0.88) 100%);",
            Country.Italy => "background: linear-gradient(90deg, rgba(0,146,70,0.72) 0%, rgba(255,255,255,0.78) 50%, rgba(206,43,55,0.72) 100%);",
            Country.France => "background: linear-gradient(90deg, rgba(0,85,164,0.74) 0%, rgba(255,255,255,0.8) 50%, rgba(239,65,53,0.72) 100%);",
            Country.Germany => "background: linear-gradient(180deg, rgba(0,0,0,0.82) 0%, rgba(221,0,0,0.66) 54%, rgba(255,206,0,0.78) 100%);",
            Country.Bulgaria => "background: linear-gradient(180deg, rgba(255,255,255,0.92) 0%, rgba(0,150,110,0.72) 54%, rgba(214,38,18,0.72) 100%);",
            Country.Argentina => "background: linear-gradient(180deg, rgba(116,172,223,0.8) 0%, rgba(255,255,255,0.86) 50%, rgba(116,172,223,0.8) 100%);",
            Country.Belarus => "background: linear-gradient(180deg, rgba(200,33,46,0.78) 0%, rgba(200,33,46,0.7) 58%, rgba(0,146,70,0.62) 58%, rgba(0,146,70,0.66) 100%);",
            Country.Afghanistan => "background: linear-gradient(90deg, rgba(0,0,0,0.74) 0%, rgba(190,0,0,0.68) 50%, rgba(18,123,61,0.7) 100%);",
            Country.Romania => "background: linear-gradient(90deg, rgba(0,43,127,0.72) 0%, rgba(252,209,22,0.76) 50%, rgba(206,17,38,0.72) 100%);",
            Country.Belgium => "background: linear-gradient(90deg, rgba(0,0,0,0.74) 0%, rgba(253,218,36,0.78) 50%, rgba(239,51,64,0.72) 100%);",
            Country.Netherlands => "background: linear-gradient(180deg, rgba(174,28,40,0.72) 0%, rgba(255,255,255,0.84) 50%, rgba(33,70,139,0.72) 100%);",
            Country.Ireland => "background: linear-gradient(90deg, rgba(22,155,98,0.72) 0%, rgba(255,255,255,0.84) 50%, rgba(255,136,62,0.72) 100%);",
            Country.Hungary => "background: linear-gradient(180deg, rgba(205,42,62,0.72) 0%, rgba(255,255,255,0.84) 50%, rgba(67,111,77,0.72) 100%);",
            Country.Poland => "background: linear-gradient(180deg, rgba(255,255,255,0.9) 0%, rgba(220,20,60,0.64) 100%);",
            Country.Ukraine => "background: linear-gradient(180deg, rgba(0,91,187,0.72) 0%, rgba(255,213,0,0.74) 100%);",
            Country.Russia => "background: linear-gradient(180deg, rgba(255,255,255,0.9) 0%, rgba(0,57,166,0.68) 50%, rgba(213,43,30,0.68) 100%);",
            Country.Austria => "background: linear-gradient(180deg, rgba(237,41,57,0.7) 0%, rgba(255,255,255,0.84) 50%, rgba(237,41,57,0.7) 100%);",
            Country.Japan => "background: radial-gradient(circle at 50% 50%, rgba(188,0,45,0.45) 0 16%, rgba(255,255,255,0.88) 17% 100%);",
            Country.Brazil => "background: linear-gradient(135deg, rgba(0,156,59,0.8) 0%, rgba(255,223,0,0.64) 50%, rgba(0,39,118,0.72) 100%);",
            Country.Greece => "background: linear-gradient(180deg, rgba(13,94,175,0.74) 0%, rgba(255,255,255,0.86) 50%, rgba(13,94,175,0.74) 100%);",
            Country.Spain => "background: linear-gradient(180deg, rgba(170,21,27,0.72) 0%, rgba(241,191,0,0.78) 50%, rgba(170,21,27,0.72) 100%);",
            Country.Portugal => "background: linear-gradient(90deg, rgba(0,102,0,0.76) 0%, rgba(0,102,0,0.7) 42%, rgba(255,0,0,0.66) 42%, rgba(255,0,0,0.7) 100%);",
            _ => "background: linear-gradient(135deg, rgba(99,103,255,0.72) 0%, rgba(255,255,255,0.8) 50%, rgba(255,215,253,0.68) 100%);"
        };
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
