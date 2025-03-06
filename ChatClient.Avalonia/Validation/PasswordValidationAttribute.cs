using System.ComponentModel.DataAnnotations;

namespace ChatClient.Avalonia.Validation;

public class PasswordValidationAttribute:ValidationAttribute
{
    public int MinLength { get; set; }
    public int MinClass { get; set; }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if(value == null)
            return ValidationResult.Success;
        
        if (value is not string password || string.IsNullOrWhiteSpace(password))
            return new ValidationResult("密码不能为空。");

        if (password.Length < MinLength)
            return new ValidationResult("密码长度不能小于" + MinLength + "个字符。");
        
        bool hasUpper = password.Any(char.IsUpper);
        bool hasLower = password.Any(char.IsLower);
        bool hasDigit = password.Any(char.IsDigit);
        bool hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));

        int classCount = new[] { hasUpper, hasLower, hasDigit, hasSpecial }.Count(b => b);
        if (classCount < MinClass)
            return new ValidationResult("密码必须包含至少" + MinClass + "种字符。");
        
        return ValidationResult.Success;
    }
}