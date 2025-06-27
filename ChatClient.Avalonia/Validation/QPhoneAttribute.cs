using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace ChatClient.Avalonia.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public class QPhoneAttribute : ValidationAttribute
{
    public bool AllowEmpty { get; set; } = false;

    // 支持多种电话号码格式：
    // - 中国手机号：1开头的11位数字
    // - 座机号码：区号(3-4位)-电话号码(7-8位)
    // - 国际格式：+国家代码-号码
    private static readonly Regex PhoneRegex = new Regex(
        @"^(\+\d{1,3}(-| )?)?((\d{3,4})|(\(\d{3,4}\)))(-| )?(\d{7,8})$|^1\d{10}$",
        RegexOptions.Compiled);

    public override bool IsValid(object? value)
    {
        // 空值或空字符串直接视为有效
        if (value == null || string.IsNullOrWhiteSpace(value.ToString()) && AllowEmpty)
            return true;

        // 否则验证电话号码格式
        string? phone = value.ToString();
        return phone != null && PhoneRegex.IsMatch(phone);
    }
}