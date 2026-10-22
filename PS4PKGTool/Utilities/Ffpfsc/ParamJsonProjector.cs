#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using OrbisPkgTool.Sfo;

namespace PS4PKGTool.Utilities.Ffpfsc;

/// <summary>
/// Projects a PS4 <c>param.sfo</c> into the PS5 <c>param.json</c> shape expected
/// by ShadowMountPlus when launching PS4 FFPFSC images. Ported from the
/// SadykovIV <c>ps4ffpsc</c> reference — <c>build_param_json</c>,
/// <c>choose_title</c> and <c>validate_shadowmount_param_json</c> in
/// <c>tools/ps4ffpsc/ps4ffpsc/sfo.py</c>.
/// </summary>
public static class ParamJsonProjector
{
    /// <summary>
    /// The 30 SCE system-language locales, in the exact order of
    /// <c>TITLE_00</c>..<c>TITLE_29</c> in a PS4 param.sfo. The spellings follow
    /// the native FW 12.70 appmeta convention where it differs from the shorter
    /// system-language name (notably Chinese scripts, Latin American Spanish,
    /// and Arabic).
    /// </summary>
    public static readonly string[] TitleLocales =
    [
        "ja-JP",
        "en-US",
        "fr-FR",
        "es-ES",
        "de-DE",
        "it-IT",
        "nl-NL",
        "pt-PT",
        "ru-RU",
        "ko-KR",
        "zh-Hant",
        "zh-Hans",
        "fi-FI",
        "sv-SE",
        "da-DK",
        "no-NO",
        "pl-PL",
        "pt-BR",
        "en-GB",
        "tr-TR",
        "es-419",
        "ar-AE",
        "fr-CA",
        "cs-CZ",
        "hu-HU",
        "el-GR",
        "ro-RO",
        "th-TH",
        "vi-VN",
        "id-ID",
    ];

    /// <summary>
    /// Builds the PS5 <c>param.json</c> for a PS4 PKG. Mirrors the reference
    /// <c>build_param_json</c>: preserve existing JSON fields, project every
    /// non-empty <c>TITLE_NN</c> into <c>localizedParameters.{locale}.titleName</c>,
    /// always ensure an <c>en-US</c> fallback, set <c>defaultLanguage</c> when
    /// missing, and mirror non-zero <c>USER_DEFINED_PARAM_1..4</c>. Existing data
    /// that carries a UTF-8 BOM is discarded, matching the reference. Output is
    /// UTF-8 without BOM, keys sorted, 2-space indentation, trailing newline.
    /// </summary>
    public static byte[] Build(string titleId, string titleName, ParamSfo sfo, byte[]? existingJson = null)
    {
        ArgumentNullException.ThrowIfNull(titleId);
        ArgumentNullException.ThrowIfNull(titleName);
        ArgumentNullException.ThrowIfNull(sfo);

        JsonObject payload = ParseExisting(existingJson) ?? [];
        JsonObject localized = GetOrCreateObject(payload, "localizedParameters");

        for (int index = 0; index < TitleLocales.Length; index++)
        {
            string? value = GetSfoText(sfo, FormattedTitleKey(index));
            if (string.IsNullOrWhiteSpace(value))
                continue;
            GetOrCreateObject(localized, TitleLocales[index])["titleName"] = value.Trim();
        }

        // ShadowMountPlus requires an en-US title fallback. Prefer the exact
        // TITLE_01 text when available, otherwise expose the selected title
        // without discarding any other localized SFO entries.
        JsonObject english = GetOrCreateObject(localized, "en-US");
        string? englishTitle = GetSfoText(sfo, FormattedTitleKey(1));
        english["titleName"] = !string.IsNullOrWhiteSpace(englishTitle)
            ? englishTitle!.Trim()
            : titleName;

        string? existingDefault = TryGetString(localized, "defaultLanguage");
        if (existingDefault is null || localized[existingDefault] is not JsonObject)
            localized["defaultLanguage"] = "en-US";

        payload["titleId"] = titleId;
        payload["titleName"] = titleName;

        // Native PS4 appmeta JSON normally remains minimal and the game reads
        // USER_DEFINED_PARAM_* from param.sfo. Some image-launched games,
        // however, have been observed to lose their language/region selector
        // unless the non-zero value is also exposed through the camelCase JSON
        // projection. Mirror only explicit non-zero integers.
        for (int index = 1; index <= 4; index++)
        {
            int? value = GetSfoInt(sfo, FormattedUserDefinedParamKey(index));
            if (value.HasValue && value.Value != 0)
                payload[FormUserDefinedParamKey(index)] = value.Value;
        }

        return Serialize(payload);
    }

    /// <summary>
    /// Chooses the display title for the FFPFSC image, mirroring the reference
    /// <c>choose_title</c>: prefer <c>TITLE</c>, then <c>TITLE_NN</c> at
    /// <paramref name="preferredIndex"/>, then <c>TITLE_01</c>, then the first
    /// non-empty <c>TITLE_NN</c>, then <c>CONTENT_ID</c> / <c>TITLE_ID</c>, and
    /// finally <c>"Unknown Game"</c>.
    /// </summary>
    public static string ChooseTitle(ParamSfo sfo, int? preferredIndex = null)
    {
        ArgumentNullException.ThrowIfNull(sfo);

        string? title = GetSfoText(sfo, "TITLE");
        if (!string.IsNullOrWhiteSpace(title))
            return title.Trim();

        if (preferredIndex is int preferred)
        {
            string? preferredTitle = GetSfoText(sfo, FormattedTitleKey(preferred));
            if (!string.IsNullOrWhiteSpace(preferredTitle))
                return preferredTitle.Trim();
        }

        string? english = GetSfoText(sfo, FormattedTitleKey(1));
        if (!string.IsNullOrWhiteSpace(english))
            return english.Trim();

        for (int index = 0; index < 30; index++)
        {
            if (index == 1) continue;
            string? candidate = GetSfoText(sfo, FormattedTitleKey(index));
            if (!string.IsNullOrWhiteSpace(candidate))
                return candidate.Trim();
        }

        foreach (string fallbackKey in new[] { "CONTENT_ID", "TITLE_ID" })
        {
            string? fallback = GetSfoText(sfo, fallbackKey);
            if (!string.IsNullOrWhiteSpace(fallback))
                return fallback.Trim();
        }

        return "Unknown Game";
    }

    /// <summary>
    /// Validates a <c>param.json</c> against ShadowMountPlus expectations,
    /// mirroring the reference <c>validate_shadowmount_param_json</c>. Throws
    /// <see cref="FormatException"/> on any mismatch and returns the parsed
    /// payload on success.
    /// </summary>
    public static JsonObject Validate(byte[] data, string expectedTitleId)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(expectedTitleId);

        if (HasUtf8Bom(data))
            throw new FormatException("param.json must not contain a UTF-8 BOM.");

        JsonObject payload = ParseExisting(data) ??
            throw new FormatException("param.json is not a valid JSON object.");

        string? titleId = TryGetString(payload, "titleId") ?? TryGetString(payload, "title_id");
        if (!string.Equals(titleId, expectedTitleId, StringComparison.Ordinal))
            throw new FormatException($"param.json titleId mismatch: {titleId ?? "<missing>"}");
        if (titleId.Length != 9)
            throw new FormatException("param.json titleId must contain exactly 9 characters.");

        string? titleName = TryGetString(payload, "titleName");
        if (string.IsNullOrEmpty(titleName) && payload["localizedParameters"] is JsonObject localized)
        {
            titleName = (localized["en-US"] as JsonObject) is JsonObject enUs
                ? TryGetString(enUs, "titleName")
                : null;
        }
        if (string.IsNullOrEmpty(titleName))
            throw new FormatException("param.json does not expose titleName to ShadowMountPlus.");

        return payload;
    }

    private static bool HasUtf8Bom(byte[] data) =>
        data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF;

    /// <summary>
    /// Parses existing param.json bytes. Returns null when absent, empty,
    /// BOM-marked (discarded, matching the reference), invalid UTF-8/JSON, or
    /// not a JSON object.
    /// </summary>
    private static JsonObject? ParseExisting(byte[]? data)
    {
        if (data is null || data.Length == 0 || HasUtf8Bom(data))
            return null;
        try
        {
            return JsonNode.Parse(data) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the existing object stored under <paramref name="key"/>, or
    /// attaches a new object and returns it. Non-object values are replaced,
    /// matching the reference's "if not isinstance(..., dict): new dict" pattern.
    /// </summary>
    private static JsonObject GetOrCreateObject(JsonObject parent, string key)
    {
        if (parent[key] is JsonObject existing)
            return existing;
        JsonObject created = [];
        parent[key] = created;
        return created;
    }

    private static string? TryGetString(JsonObject parent, string key)
    {
        if (!parent.TryGetPropertyValue(key, out JsonNode? node) || node is not JsonValue value)
            return null;
        return value.TryGetValue<string>(out string? text) ? text : null;
    }

    /// <summary>Text SFO value (format 0x0204) or null — matches Python's isinstance(value, str).</summary>
    private static string? GetSfoText(ParamSfo sfo, string key)
    {
        SfoValue? entry = sfo[key];
        if (entry is null || entry.Format != ParamSfo.FormatUtf8)
            return null;
        return entry.StringValue;
    }

    /// <summary>Integer SFO value (format 0x0404) or null — matches Python's isinstance(value, int).</summary>
    private static int? GetSfoInt(ParamSfo sfo, string key)
    {
        SfoValue? entry = sfo[key];
        if (entry is null || entry.Format != ParamSfo.FormatInt)
            return null;
        return entry.IntValue;
    }

    /// <summary>UTF-8 (no BOM), keys sorted at every level, 2-space indent, trailing newline.</summary>
    private static byte[] Serialize(JsonObject payload)
    {
        var options = new JsonWriterOptions
        {
            Indented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        };

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, options))
        {
            WriteSorted(payload, writer);
            writer.Flush();
        }
        stream.WriteByte((byte)'\n');
        return stream.ToArray();
    }

    /// <summary>
    /// Writes the node tree with object keys sorted by ordinal comparison at
    /// every level — the equivalent of Python's <c>json.dumps(sort_keys=True)</c>.
    /// Array element order is preserved (only dict keys are sorted).
    /// </summary>
    private static void WriteSorted(JsonNode? node, Utf8JsonWriter writer)
    {
        switch (node)
        {
            case null:
                writer.WriteNullValue();
                break;
            case JsonObject obj:
                writer.WriteStartObject();
                foreach (KeyValuePair<string, JsonNode?> child in obj.OrderBy(
                             property => property.Key, StringComparer.Ordinal))
                {
                    writer.WritePropertyName(child.Key);
                    WriteSorted(child.Value, writer);
                }
                writer.WriteEndObject();
                break;
            case JsonArray array:
                writer.WriteStartArray();
                foreach (JsonNode? item in array)
                    WriteSorted(item, writer);
                writer.WriteEndArray();
                break;
            default:
                node.WriteTo(writer);
                break;
        }
    }

    private static string FormattedTitleKey(int index) => $"TITLE_{index:D2}";
    private static string FormattedUserDefinedParamKey(int index) => $"USER_DEFINED_PARAM_{index}";
    private static string FormUserDefinedParamKey(int index) => $"userDefinedParam{index}";
}
