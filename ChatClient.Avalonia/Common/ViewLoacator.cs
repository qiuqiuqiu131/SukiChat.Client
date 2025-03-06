using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace ChatClient.Tool.Common;

public class ViewLocator(SukiChatViews views) : IDataTemplate
{
    private readonly Dictionary<object, Control> _controlCache = [];

    public Control Build(object? param)
    {
        if (param is null)
        {
            return CreateText("Data is null.");
        }

        if (_controlCache.TryGetValue(param, out var control))
        {
            return control;
        }

        if (views.TryCreateView(param, out var view))
        {
            _controlCache.Add(param, view);

            return view;
        }

        return CreateText($"No View For {param.GetType().Name}.");
    }

    public bool Match(object? data) => data is BindableBase;

    private static TextBlock CreateText(string text) => new TextBlock { Text = text };
}