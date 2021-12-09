using Avalonia;
using Avalonia.Controls;

namespace OpenBudget.Avalonia.Controls
{
    public class SelectableButton : Button
    {
        public bool IsSelected
        {
            get { return (bool)GetValue(IsSelectedProperty); }
            set { SetValue(IsSelectedProperty, value); }
        }

        public static readonly StyledProperty<bool> IsSelectedProperty =
            AvaloniaProperty.Register<SelectableButton, bool>(nameof(IsSelected), defaultValue: false);
    }
}
