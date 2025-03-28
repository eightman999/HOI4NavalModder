using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using HOI4NavalModder.Core.Models;

namespace HOI4NavalModder.View;

public partial class Torpedo_Design_View(NavalEquipment equipment, Dictionary<string, NavalCategory> categories, Dictionary<int, string> tierYears) : Avalonia.Controls.Window
{
    private readonly CheckBox _AutoGenerateIdCheckBox;
    private readonly TextBox _idTextBox;
    private readonly NumericUpDown _manpowerNumeric;
    
    public void On_Cancel_Click(object sender, RoutedEventArgs e)
    {
        // キャンセル
        Close();
    }

    private void On_Save_Click(object? sender, RoutedEventArgs e)
    {
        throw new System.NotImplementedException();
    }
}