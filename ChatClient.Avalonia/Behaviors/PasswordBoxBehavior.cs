using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace ChatClient.Avalonia.Behaviors
{
    public class PasswordBoxBehavior : Behavior<TextBox>
    {
        private const string revealButtonClassName = "revealPasswordButton";
        public char PasswordChar { get; set; } = '*';
        public bool ShowRevealButton { get; set; } = true;

        protected override void OnAttached()
        {
            base.OnAttached();
            if (AssociatedObject != null)
                AssociatedObject.Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var realEditor = AssociatedObject;
            if (realEditor == null)
                return;
            realEditor.PasswordChar = PasswordChar;
            if (ShowRevealButton)
                realEditor.Classes.Add(revealButtonClassName);
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            if (AssociatedObject != null)
                AssociatedObject.Loaded -= OnLoaded;
        }
    }
}