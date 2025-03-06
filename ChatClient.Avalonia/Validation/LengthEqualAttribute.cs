using System.ComponentModel.DataAnnotations;

namespace ChatClient.Avalonia.Validation;

public class LengthEqualAttribute:ValidationAttribute
{
    public int Length { get; set; }
    
    public override bool IsValid(object? value)
    {
        if (value == null)
            return true;

        if (value is not string str || str.Length != Length)
            return false;

        return true;
    }
}