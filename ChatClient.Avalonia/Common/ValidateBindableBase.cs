using System.Collections;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ChatClient.Avalonia.Common;

public class ValidateBindableBase : BindableBase, INotifyDataErrorInfo
{
    private readonly Dictionary<string, List<string>?> _errors = new Dictionary<string, List<string>?>();

    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;

    public bool HasErrors => _errors.Any();

    public IEnumerable GetErrors(string? propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _errors.Values.SelectMany(v => v);

        return _errors.GetValueOrDefault(propertyName);
    }

    protected void ValidateProperty<T>(string propertyName, T value)
    {
        // 获取属性的验证结果
        var context = new ValidationContext(this) { MemberName = propertyName };
        var validationResults = new List<ValidationResult>();
        Validator.TryValidateProperty(value, context, validationResults);

        HandleValidationResults(propertyName, validationResults);
    }

    private void HandleValidationResults(string propertyName, List<ValidationResult> validationResults)
    {
        var origionErrors = _errors.GetValueOrDefault(propertyName) ?? [];

        // 检查是否有变化
        bool hasChanged = origionErrors.Count != validationResults.Count
                          || !origionErrors!.SequenceEqual(validationResults.Select(vr => vr.ErrorMessage));

        if (!hasChanged) return;

        // 更新_errors
        if (validationResults.Any())
            _errors[propertyName] = validationResults.Select(vr => vr.ErrorMessage).ToList()!;
        else
            _errors.Remove(propertyName);

        // 触发ErrorsChanged事件
        ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(propertyName));
    }
}