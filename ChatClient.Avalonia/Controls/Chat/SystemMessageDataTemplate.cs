using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using ChatClient.Tool.Data.ChatMessage;

namespace ChatClient.Avalonia.Controls.Chat;

public static class SystemMessageDataTemplate
{
    public static FuncDataTemplate<SystemMessDto> GenderDataTemplate { get; }
        = new(mess => mess is not null,
            BuildGenderPresenter);

    private static Control BuildGenderPresenter(SystemMessDto mess)
    {
        TextBlock textBlock = new TextBlock();
        textBlock.TextAlignment = TextAlignment.Center;
        textBlock.Opacity = 0.8;
        textBlock.FontSize = 12;
        textBlock.Classes.Add("Small");
        textBlock.Padding = new Thickness(0, 0, 5, 0);
        foreach (var block in mess.Blocks)
        {
            Run run = new Run();
            run.Text = block.Text + " ";
            if (block.Bold)
            {
                Application.Current.TryFindResource("SukiPrimaryColor", out var color);
                if (color is IBrush brush)
                    run.Foreground = brush;
                else if (color is Color c)
                    run.Foreground = new SolidColorBrush(c);
            }
            else
            {
                Application.Current.TryFindResource("SukiText", out var color);
                if (color is IBrush brush)
                    run.Foreground = brush;
                else if (color is Color c)
                    run.Foreground = new SolidColorBrush(c);
            }

            textBlock.Inlines.Add(run);
        }

        return textBlock;
    }
}