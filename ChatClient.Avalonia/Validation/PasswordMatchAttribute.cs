using System.ComponentModel.DataAnnotations;

namespace ChatClient.Avalonia.Validation;

public class PasswordMatchAttribute : ValidationAttribute
{
    private readonly string _otherPropertyName;

    public PasswordMatchAttribute(string otherPropertyName)
    {
        _otherPropertyName = otherPropertyName;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var otherProperty = validationContext.ObjectType.GetProperty(_otherPropertyName);
        if (otherProperty == null)
            return ValidationResult.Success;

        var otherValue = otherProperty.GetValue(validationContext.ObjectInstance);

        if (value == null && otherValue == null)
            return ValidationResult.Success;

        if (value != null && value.Equals(otherValue))
            return ValidationResult.Success;

        return new ValidationResult(ErrorMessage ?? "两次输入的密码不一致");
    }
}