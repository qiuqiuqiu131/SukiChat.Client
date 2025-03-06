using System.ComponentModel.DataAnnotations;

namespace ChatClient.Avalonia.Validation;

public class InputRequiredAttribute:ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value == null)
            return true;

        if (value is not string str || string.IsNullOrWhiteSpace(str))
            return false;

        return true;
    }
}