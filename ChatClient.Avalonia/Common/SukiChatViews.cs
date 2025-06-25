using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;

namespace ChatClient.Avalonia.Common;

public class SukiChatViews
{
    private readonly Dictionary<Type, Type> _vmToViewMap = [];

    public SukiChatViews AddView<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TView,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        TViewModel>(IContainerRegistry services)
        where TView : ContentControl
        where TViewModel : BindableBase
    {
        var viewType = typeof(TView);
        var viewModelType = typeof(TViewModel);

        _vmToViewMap.Add(viewModelType, viewType);

        if (viewModelType.IsAssignableTo(typeof(ChatPageBase)))
        {
            services.Register(typeof(ChatPageBase), viewModelType);
        }
        else
        {
            services.Register(viewModelType);
        }

        return this;
    }

    public bool TryCreateView(IContainerProvider provider, Type viewModelType, [NotNullWhen(true)] out Control? view)
    {
        var viewModel = provider.Resolve(viewModelType);

        return TryCreateView(viewModel, out view);
    }

    public bool TryCreateView(object? viewModel, [NotNullWhen(true)] out Control? view)
    {
        view = null;

        if (viewModel == null)
        {
            return false;
        }

        var viewModelType = viewModel.GetType();

        if (_vmToViewMap.TryGetValue(viewModelType, out var viewType))
        {
            view = Activator.CreateInstance(viewType) as Control;

            if (view != null)
            {
                view.DataContext = viewModel;
            }
        }

        return view != null;
    }

    public Control CreateView<TViewModel>(IContainerProvider provider) where TViewModel : BindableBase
    {
        var viewModelType = typeof(TViewModel);

        if (TryCreateView(provider, viewModelType, out var view))
        {
            return view;
        }

        throw new InvalidOperationException();
    }
}