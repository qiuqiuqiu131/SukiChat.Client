using System.Globalization;

namespace ChatClient.Tool.Tools;

public static class DateTimeExtensions
{
    private static readonly string[] SupportedFormats = new[]
    {
        "yyyy-MM-dd HH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-M-d H:mm:ss.fff",
        "yyyy-M-d H:mm:ss",
        "yyyy/MM/dd HH:mm:ss.fff",
        "yyyy/M/d HH:mm:ss.fff",
        "yyyy/MM/dd HH:mm:ss",
        "yyyy/M/d HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff", // ISO-like
        "yyyy-MM-ddTHH:mm:ss"
    };

    private static readonly string[] SupportedDateOnlyFormats = new[]
    {
        "yyyy-MM-dd",
        "yyyy-M-d",
        "yyyy/MM/dd",
        "yyyy/M/d"
    };

    /// <summary>
    /// 将 DateTime 格式化为 "yyyy-MM-dd HH:mm:ss.fff"（InvariantCulture）。
    /// </summary>
    public static string ToInvariantString(this DateTime dateTime) =>
        dateTime.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);

    public static string ToInvariantString(this DateOnly? dateOnly) =>
        dateOnly?.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture) ?? String.Empty;

    public static DateOnly toDateOnlyInvariant(this string? date)
    {
        bool result = DateOnly.TryParseExact(date, SupportedDateOnlyFormats, CultureInfo.InstalledUICulture,
            DateTimeStyles.None, out var dateOnly);
        if (!result) throw new FormatException($"无法解析为 DateOnly: {date}");
        return dateOnly;
    }

    /// <summary>
    /// 尝试解析字符串为 DateTime，支持多种常见格式（有/无毫秒、/ 或 -、单/双位月日），失败时返回 false。
    /// </summary>
    public static bool TryParseInvariant(this string? input, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // 先用 TryParseExact 支持的列表
        if (DateTime.TryParseExact(input,
                SupportedFormats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out result))
        {
            return true;
        }

        // 回退到宽松的 TryParse（仍使用 InvariantCulture）
        if (DateTime.TryParse(input, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
            return true;

        return false;
    }

    /// <summary>
    /// 解析字符串为 DateTime，解析失败时抛出 FormatException。
    /// </summary>
    public static DateTime ParseInvariant(this string? input)
    {
        if (input.TryParseInvariant(out var dt))
            return dt;
        throw new FormatException($"无法解析为 DateTime: {input}");
    }

    /// <summary>
    /// 解析字符串为 DateTime，解析失败则返回指定的默认值（默认为 DateTime.Now）。
    /// </summary>
    public static DateTime ParseInvariantOrDefault(this string? input, DateTime? defaultValue = null)
    {
        if (input.TryParseInvariant(out var dt))
            return dt;
        return defaultValue ?? DateTime.Now;
    }
}