using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace HOI4NavalModder
{
    public partial class Equipment_Design_View : UserControl
    {
        public Equipment_Design_View()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}