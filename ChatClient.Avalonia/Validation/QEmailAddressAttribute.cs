using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ChatClient.Avalonia.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class QEmailAddressAttribute : ValidationAttribute
{
    public bool AllowEmpty { get; set; } = false;

    private static readonly Regex EmailRegex = new Regex(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public override bool IsValid(object? value)
    {
        // 空值或空字符串直接视为有效
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) && AllowEmpty)
            return true;

        // 否则验证邮箱格式
        string? email = value.ToString();
        return email != null && EmailRegex.IsMatch(email);
    }
}