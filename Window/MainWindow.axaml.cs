using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using Avalonia.Styling;

namespace HOI4NavalModder;

public partial class MainWindow : Window
{
    private readonly Panel _contentPanel;
    private readonly Dictionary<string, UserControl> _pages = new();
    private Button _activeButton;

    public MainWindow()
    {
        Console.OutputEncoding = Encoding.UTF8;
        InitializeComponent();

        // XAMLで定義されたコントロールを取得
        _contentPanel = this.FindControl<Panel>("ContentPanel");

        // 各ページを初期化
        _pages.Add("EquipmentDesign", new EquipmentDesignView());
        _pages.Add("EquipmentIcon", new EquipmentIconView());
        _pages.Add("ShipType", new ShipTypeView());
        _pages.Add("ShipDesign", new ShipDesignView());
        _pages.Add("ShipIcon", new ShipIconView());
        _pages.Add("FleetDeployment", new FleetDeploymentView());
        _pages.Add("ShipName", new ShipNameView());
        _pages.Add("EquipmentName", new EquipmentNameView());
        _pages.Add("IDESettings", new IdeSettingsView());
        _pages.Add("ModSettings", new ModSettingsView());

        // デフォルトのページを設定
        NavigateTo("EquipmentDesign", this.FindControl<Button>("EquipmentDesignButton"));
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    // 各ボタンのクリックイベントハンドラ
    public void OnEquipmentDesignButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("EquipmentDesign", (Button)sender);
    }

    public void OnEquipmentIconButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("EquipmentIcon", (Button)sender);
    }

    public void OnShipTypeButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("ShipType", (Button)sender);
    }

    public void OnShipDesignButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("ShipDesign", (Button)sender);
    }

    public void OnShipIconButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("ShipIcon", (Button)sender);
    }

    public void OnFleetDeploymentButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("FleetDeployment", (Button)sender);
    }

    public void OnShipNameButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("ShipName", (Button)sender);
    }

    public void OnEquipmentNameButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("EquipmentName", (Button)sender);
    }

    public void OnIDESettingsButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("IDESettings", (Button)sender);
    }

    public void OnModSettingsButtonClick(object sender, RoutedEventArgs e)
    {
        NavigateTo("ModSettings", (Button)sender);
    }

    private void NavigateTo(string page, Button sourceButton)
    {
        // ボタンの状態を更新
        if (_activeButton != null) _activeButton.Background = new SolidColorBrush(Color.Parse("#252526"));

        sourceButton.Background = new SolidColorBrush(Color.Parse("#3E3E42"));
        _activeButton = sourceButton;

        // コンテンツを更新
        _contentPanel.Children.Clear();
        if (_pages.TryGetValue(page, out var control)) _contentPanel.Children.Add(control);
    }
}

// 共通のユーティリティクラスを作成
public static class ModuleHelper
{
    // モジュールのコンテンツ作成ヘルパーメソッド
    public static Panel CreateModuleContent(string description)
    {
        var panel = new StackPanel
        {
            Margin = new Thickness(20)
        };

        var descriptionText = new TextBlock
        {
            Text = description,
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 20)
        };

        var toolbarPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#333337")),
            Height = 40,
            Margin = new Thickness(0, 0, 0, 1)
        };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 5, 0, 0)
        };

        var newButton = new Button
        {
            Content = "新規",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 5, 0),
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };

        var saveButton = new Button
        {
            Content = "保存",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 5, 0),
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };

        toolbar.Children.Add(newButton);
        toolbar.Children.Add(saveButton);
        toolbarPanel.Children.Add(toolbar);

        var contentArea = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Height = 500,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46"))
        };

        panel.Children.Add(descriptionText);
        panel.Children.Add(toolbarPanel);
        panel.Children.Add(contentArea);

        return panel;
    }
}

// 各モジュールのViewクラス実装

public class EquipmentIconView : UserControl
{
    public EquipmentIconView()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        var headerPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Height = 40
        };

        var headerText = new TextBlock
        {
            Text = "装備アイコン",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0),
            FontSize = 16
        };

        headerPanel.Children.Add(headerText);
        Grid.SetRow(headerPanel, 0);

        var contentPanel = ModuleHelper.CreateModuleContent("装備のアイコンを作成・編集できます");
        Grid.SetRow(contentPanel, 1);

        grid.Children.Add(headerPanel);
        grid.Children.Add(contentPanel);

        Content = grid;
    }
}

public class ShipTypeView : UserControl
{
    public ShipTypeView()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        var headerPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Height = 40
        };

        var headerText = new TextBlock
        {
            Text = "艦種",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0),
            FontSize = 16
        };

        headerPanel.Children.Add(headerText);
        Grid.SetRow(headerPanel, 0);

        var contentPanel = ModuleHelper.CreateModuleContent("艦種の定義と設定を行います");
        Grid.SetRow(contentPanel, 1);

        grid.Children.Add(headerPanel);
        grid.Children.Add(contentPanel);

        Content = grid;
    }
}

public class ShipDesignView : UserControl
{
    public ShipDesignView()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        var headerPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Height = 40
        };

        var headerText = new TextBlock
        {
            Text = "艦船設計",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0),
            FontSize = 16
        };

        headerPanel.Children.Add(headerText);
        Grid.SetRow(headerPanel, 0);

        var contentPanel = ModuleHelper.CreateModuleContent("艦船の設計や性能調整ができます");
        Grid.SetRow(contentPanel, 1);

        grid.Children.Add(headerPanel);
        grid.Children.Add(contentPanel);

        Content = grid;
    }
}

public class ShipIconView : UserControl
{
    public ShipIconView()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        var headerPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Height = 40
        };

        var headerText = new TextBlock
        {
            Text = "艦船アイコン",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0),
            FontSize = 16
        };

        headerPanel.Children.Add(headerText);
        Grid.SetRow(headerPanel, 0);

        var contentPanel = ModuleHelper.CreateModuleContent("艦船のアイコンを作成・編集できます");
        Grid.SetRow(contentPanel, 1);

        grid.Children.Add(headerPanel);
        grid.Children.Add(contentPanel);

        Content = grid;
    }
}

public class ShipNameView : UserControl
{
    public ShipNameView()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        var headerPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Height = 40
        };

        var headerText = new TextBlock
        {
            Text = "艦名",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0),
            FontSize = 16
        };

        headerPanel.Children.Add(headerText);
        Grid.SetRow(headerPanel, 0);

        var contentPanel = ModuleHelper.CreateModuleContent("艦船の名前リストを管理します");
        Grid.SetRow(contentPanel, 1);

        grid.Children.Add(headerPanel);
        grid.Children.Add(contentPanel);

        Content = grid;
    }
}

public class EquipmentNameView : UserControl
{
    public EquipmentNameView()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        var headerPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Height = 40
        };

        var headerText = new TextBlock
        {
            Text = "装備名",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0),
            FontSize = 16
        };

        headerPanel.Children.Add(headerText);
        Grid.SetRow(headerPanel, 0);

        var contentPanel = ModuleHelper.CreateModuleContent("装備の名前リストを管理します");
        Grid.SetRow(contentPanel, 1);

        grid.Children.Add(headerPanel);
        grid.Children.Add(contentPanel);

        Content = grid;
    }
}

public class IdeSettingsView : UserControl
{
    // 設定ファイルパス
    private readonly string _configFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HOI4NavalModder",
        "idesettings.json");

    private readonly RadioButton _equipmentFileIntegratedRadio;
    private readonly RadioButton _equipmentFileSplitRadio;
    private readonly ComboBox _fontFamilyComboBox;
    private readonly NumericUpDown _fontSizeNumeric;
    private readonly RadioButton _languageEnglishRadio;
    private readonly RadioButton _languageJapaneseRadio;
    private readonly ComboBox _themeComboBox;

    // 設定を保持するクラス
    private IdeSettings _settings;

    public IdeSettingsView()
    {
        // 設定の読み込み
        LoadSettings();

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        var headerPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Height = 40
        };

        var headerText = new TextBlock
        {
            Text = "IDE設定",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0),
            FontSize = 16
        };

        headerPanel.Children.Add(headerText);
        Grid.SetRow(headerPanel, 0);

        // メインコンテンツ
        var mainPanel = new StackPanel
        {
            Margin = new Thickness(20)
        };

        var descriptionText = new TextBlock
        {
            Text = "開発環境の設定ができます",
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 20)
        };

        // 外観設定セクション
        var appearanceSection = CreateSectionHeader("外観設定");
        mainPanel.Children.Add(appearanceSection);

        // テーマ設定
        var themePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 10, 0, 10)
        };

        var themeLabel = new TextBlock
        {
            Text = "テーマ:",
            Width = 150,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White
        };

        _themeComboBox = new ComboBox
        {
            Width = 200,
            SelectedIndex = _settings.IsDarkTheme ? 0 : 1
        };
        _themeComboBox.Items.Add("ダークモード");
        _themeComboBox.Items.Add("ライトモード");
        _themeComboBox.SelectionChanged += OnThemeSelectionChanged;

        themePanel.Children.Add(themeLabel);
        themePanel.Children.Add(_themeComboBox);
        mainPanel.Children.Add(themePanel);

        // フォント設定
        var fontPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 10, 0, 10)
        };

        var fontLabel = new TextBlock
        {
            Text = "フォント:",
            Width = 150,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White
        };

        _fontFamilyComboBox = new ComboBox
        {
            Width = 200
        };

        // システムから利用可能なフォントを取得する試み
        var availableFonts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        try
        {
            // 方法1: TextDecorationCollection からフォント情報を取得
            var fontNames = new List<string>();
            var defaultTypeface = Typeface.Default;
            if (defaultTypeface.FontFamily != null)
            {
                var fontFamily = defaultTypeface.FontFamily.Name;
                if (!string.IsNullOrEmpty(fontFamily)) availableFonts.Add(fontFamily);
            }

            // 方法2: SystemFonts を通じてフォント取得を試みる
            try
            {
                var systemFonts = FontManager.Current.SystemFonts;
                if (systemFonts != null)
                    foreach (var fontFamily in systemFonts)
                        if (!string.IsNullOrEmpty(fontFamily.Name))
                            availableFonts.Add(fontFamily.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"SystemFonts でのフォント取得エラー: {ex.Message}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"フォント一覧取得エラー: {ex.Message}");
        }

        // 方法3: 一般的なフォントを追加（上記方法が失敗した場合のバックアップ）
        if (availableFonts.Count == 0)
        {
            string[] commonFonts =
            {
                "Yu Gothic UI", "Meiryo UI", "MS Gothic", "MS PGothic", "MS UI Gothic",
                "Yu Mincho", "MS Mincho", "Segoe UI", "Arial", "Times New Roman",
                "Courier New", "Verdana", "Tahoma", "Consolas", "Calibri", "Yu Gothic",
                "Meiryo", "MS Mincho", "HGS明朝E", "HGP創英角ゴシックUB", "HGS教科書体",
                "HGP行書体", "メイリオ", "游ゴシック", "游明朝", "ＭＳ ゴシック", "ＭＳ 明朝",
                "ＭＳ Ｐゴシック", "ＭＳ Ｐ明朝", "Arial Unicode MS", "Microsoft Sans Serif"
            };

            foreach (var font in commonFonts)
                try
                {
                    // フォントが存在するか確認
                    var fontFamily = new FontFamily(font);
                    availableFonts.Add(font);
                }
                catch
                {
                    // フォントが見つからない場合は無視
                }
        }

        // フォントを追加
        foreach (var font in availableFonts.OrderBy(f => f)) _fontFamilyComboBox.Items.Add(font);

        // 設定されたフォントが存在しない場合はデフォルトを選択
        if (availableFonts.Contains(_settings.FontFamily))
        {
            _fontFamilyComboBox.SelectedItem = _settings.FontFamily;
        }
        else
        {
            // 代替フォントとして日本語対応フォントを探す
            string[] preferredFonts = { "Yu Gothic UI", "Meiryo UI", "MS Gothic", "Segoe UI" };
            var selectedFont = preferredFonts.FirstOrDefault(f => availableFonts.Contains(f)) ??
                               availableFonts.FirstOrDefault() ?? "Segoe UI";

            if (_fontFamilyComboBox.Items.Contains(selectedFont))
            {
                _fontFamilyComboBox.SelectedItem = selectedFont;
            }
            else if (_fontFamilyComboBox.Items.Count > 0)
            {
                _fontFamilyComboBox.SelectedIndex = 0;
                selectedFont = _fontFamilyComboBox.SelectedItem?.ToString() ?? "Segoe UI";
            }

            _settings.FontFamily = selectedFont;
        }

        _fontFamilyComboBox.SelectionChanged += OnFontFamilyChanged;

        fontPanel.Children.Add(fontLabel);
        fontPanel.Children.Add(_fontFamilyComboBox);
        mainPanel.Children.Add(fontPanel);

        // フォントサイズ設定
        var fontSizePanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 10, 0, 20)
        };

        var fontSizeLabel = new TextBlock
        {
            Text = "フォントサイズ:",
            Width = 150,
            VerticalAlignment = VerticalAlignment.Center,
            Foreground = Brushes.White
        };

        _fontSizeNumeric = new NumericUpDown
        {
            Width = 200,
            Minimum = 8,
            Maximum = 24,
            Increment = 1,
            Value = (decimal?)_settings.FontSize,
            FormatString = "0"
        };
        _fontSizeNumeric.ValueChanged += OnFontSizeChanged;

        fontSizePanel.Children.Add(fontSizeLabel);
        fontSizePanel.Children.Add(_fontSizeNumeric);
        mainPanel.Children.Add(fontSizePanel);

        // 書き出し設定セクション
        var exportSection = CreateSectionHeader("書き出し設定");
        mainPanel.Children.Add(exportSection);

        // 装備ファイル設定
        var equipmentFilePanel = new StackPanel
        {
            Margin = new Thickness(10, 10, 0, 10)
        };

        var equipmentFileLabel = new TextBlock
        {
            Text = "装備ファイル:",
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var equipmentFileOptions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(20, 5, 0, 0)
        };

        _equipmentFileIntegratedRadio = new RadioButton
        {
            Content = "統合",
            GroupName = "EquipmentFile",
            Margin = new Thickness(0, 0, 20, 0),
            Foreground = Brushes.White,
            IsChecked = _settings.IsEquipmentFileIntegrated
        };
        _equipmentFileIntegratedRadio.Checked += OnEquipmentFileOptionChanged;

        _equipmentFileSplitRadio = new RadioButton
        {
            Content = "分割",
            GroupName = "EquipmentFile",
            Foreground = Brushes.White,
            IsChecked = !_settings.IsEquipmentFileIntegrated
        };
        _equipmentFileSplitRadio.Checked += OnEquipmentFileOptionChanged;

        equipmentFileOptions.Children.Add(_equipmentFileIntegratedRadio);
        equipmentFileOptions.Children.Add(_equipmentFileSplitRadio);

        equipmentFilePanel.Children.Add(equipmentFileLabel);
        equipmentFilePanel.Children.Add(equipmentFileOptions);
        mainPanel.Children.Add(equipmentFilePanel);

        // 言語設定
        var languagePanel = new StackPanel
        {
            Margin = new Thickness(10, 10, 0, 10)
        };

        var languageLabel = new TextBlock
        {
            Text = "言語:",
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 5)
        };

        var languageOptions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(20, 5, 0, 0)
        };

        _languageJapaneseRadio = new RadioButton
        {
            Content = "日本語",
            GroupName = "Language",
            Margin = new Thickness(0, 0, 20, 0),
            Foreground = Brushes.White,
            IsChecked = _settings.IsJapanese
        };
        _languageJapaneseRadio.Checked += OnLanguageOptionChanged;

        _languageEnglishRadio = new RadioButton
        {
            Content = "英語",
            GroupName = "Language",
            Foreground = Brushes.White,
            IsChecked = !_settings.IsJapanese
        };
        _languageEnglishRadio.Checked += OnLanguageOptionChanged;

        languageOptions.Children.Add(_languageJapaneseRadio);
        languageOptions.Children.Add(_languageEnglishRadio);

        languagePanel.Children.Add(languageLabel);
        languagePanel.Children.Add(languageOptions);
        mainPanel.Children.Add(languagePanel);

        // 保存ボタン
        var saveButton = new Button
        {
            Content = "設定を保存",
            HorizontalAlignment = HorizontalAlignment.Left,
            Margin = new Thickness(10, 20, 0, 0),
            Padding = new Thickness(15, 8, 15, 8),
            Background = new SolidColorBrush(Color.Parse("#0078D7")),
            Foreground = Brushes.White
        };
        saveButton.Click += OnSaveButtonClick;

        mainPanel.Children.Add(saveButton);

        Grid.SetRow(mainPanel, 1);
        grid.Children.Add(headerPanel);
        grid.Children.Add(mainPanel);

        Content = grid;
    }

    private Panel CreateSectionHeader(string title)
    {
        var panel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Height = 30,
            Margin = new Thickness(0, 10, 0, 10)
        };

        var titleText = new TextBlock
        {
            Text = title,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };

        panel.Children.Add(titleText);
        return panel;
    }

    private void LoadSettings()
    {
        try
        {
            var configDir = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var jsonData = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

                if (jsonData != null)
                    _settings = new IdeSettings
                    {
                        IsDarkTheme = GetBooleanValue(jsonData, "IsDarkTheme"),
                        FontFamily = jsonData.ContainsKey("FontFamily")
                            ? jsonData["FontFamily"].ToString()
                            : "Yu Gothic UI",
                        FontSize = GetDecimalValue(jsonData, "FontSize"),
                        IsEquipmentFileIntegrated = GetBooleanValue(jsonData, "IsEquipmentFileIntegrated"),
                        IsJapanese = GetBooleanValue(jsonData, "IsJapanese")
                    };
            }
            else
            {
                // Default settings
                _settings = new IdeSettings
                {
                    IsDarkTheme = true,
                    FontFamily = "Yu Gothic UI",
                    FontSize = 12,
                    IsEquipmentFileIntegrated = true,
                    IsJapanese = true
                };
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading settings file: {ex.Message}");

            // Default settings in case of error
            _settings = new IdeSettings
            {
                IsDarkTheme = true,
                FontFamily = "Yu Gothic UI",
                FontSize = 12,
                IsEquipmentFileIntegrated = true,
                IsJapanese = true
            };
        }
    }

    private static bool GetBooleanValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return false;

        try
        {
            if (data[key] is JsonElement jsonElement)
                if (jsonElement.ValueKind == JsonValueKind.True ||
                    jsonElement.ValueKind == JsonValueKind.False)
                    return jsonElement.GetBoolean();

            return Convert.ToBoolean(data[key]);
        }
        catch
        {
            return false;
        }
    }

    private static decimal GetDecimalValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return 0;

        try
        {
            if (data[key] is JsonElement jsonElement)
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return jsonElement.GetDecimal();

            return Convert.ToDecimal(data[key]);
        }
        catch
        {
            return 0;
        }
    }

    private void SaveSettings()
    {
        try
        {
            var configDir = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(_settings, options);
            File.WriteAllText(_configFilePath, json);

            ApplySettings();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定ファイルの保存中にエラーが発生しました: {ex.Message}");
        }
    }

    private void ApplySettings()
    {
        // アプリケーション全体に設定を反映する
        var app = Application.Current;
        if (app != null)
        {
            // テーマの適用
            app.RequestedThemeVariant = _settings.IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;

            // フォント設定の適用（実装は環境によって異なる場合があります）
            if (app.Resources is ResourceDictionary resourceDictionary)
            {
                resourceDictionary["DefaultFontFamily"] = new FontFamily(_settings.FontFamily);
                resourceDictionary["DefaultFontSize"] = _settings.FontSize;
            }
        }
    }

    private void OnThemeSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_themeComboBox.SelectedIndex == 0)
            _settings.IsDarkTheme = true;
        else
            _settings.IsDarkTheme = false;
    }

    private void OnFontFamilyChanged(object sender, SelectionChangedEventArgs e)
    {
        _settings.FontFamily = _fontFamilyComboBox.SelectedItem?.ToString() ?? "Yu Gothic UI";
    }

    private void OnFontSizeChanged(object sender, NumericUpDownValueChangedEventArgs e)
    {
        _settings.FontSize = (double)_fontSizeNumeric.Value;
    }

    private void OnEquipmentFileOptionChanged(object sender, RoutedEventArgs e)
    {
        _settings.IsEquipmentFileIntegrated = _equipmentFileIntegratedRadio.IsChecked ?? true;
    }

    private void OnLanguageOptionChanged(object sender, RoutedEventArgs e)
    {
        _settings.IsJapanese = _languageJapaneseRadio.IsChecked ?? true;
    }

    private void OnSaveButtonClick(object sender, RoutedEventArgs e)
    {
        SaveSettings();
    }
}

public class ModSettingsView : UserControl
{
    private readonly Button _addButton;

    private readonly string _configFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HOI4NavalModder",
        "modpaths.json");

    private readonly ObservableCollection<ModInfo> _modList = new();
    private readonly ListBox _modListBox;
    private readonly Button _removeButton;
    private readonly Button _saveButton;
    private readonly TextBox _vanillaGamePathTextBox;
    private readonly TextBox _vanillaLogPathTextBox;
    private string _vanillaGamePath;
    private string _vanillaLogPath;

    public ModSettingsView()
    {
        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("Auto,*")
        };

        var headerPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Height = 40
        };

        var headerText = new TextBlock
        {
            Text = "MOD設定",
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(20, 0, 0, 0),
            FontSize = 16
        };

        headerPanel.Children.Add(headerText);
        Grid.SetRow(headerPanel, 0);

        var mainPanel = new StackPanel
        {
            Margin = new Thickness(20)
        };

        var descriptionText = new TextBlock
        {
            Text = "バニラゲームとMODの設定をします",
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 20)
        };

        // ==================== バニラゲームパス設定セクション ====================
        var vanillaGameSection = CreateSectionHeader("バニラ環境設定");
        mainPanel.Children.Add(vanillaGameSection);

        // Game Path
        var gamePathLabel = new TextBlock
        {
            Text = "ゲームパス:",
            Foreground = Brushes.White,
            Margin = new Thickness(5, 10, 0, 5)
        };
        mainPanel.Children.Add(gamePathLabel);

        var vanillaGamePathPanel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, Auto"),
            Margin = new Thickness(5, 0, 0, 10)
        };

        _vanillaGamePathTextBox = new TextBox
        {
            Watermark = "Hearts of Iron IV のインストールパスを設定してください",
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.Parse("#333337")),
            BorderBrush = new SolidColorBrush(Color.Parse("#555555")),
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetColumn(_vanillaGamePathTextBox, 0);

        var browseGamePathButton = new Button
        {
            Content = "参照...",
            Padding = new Thickness(10, 5, 10, 5),
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#555555"))
        };
        browseGamePathButton.Click += OnBrowseGamePathButtonClick;
        Grid.SetColumn(browseGamePathButton, 1);

        vanillaGamePathPanel.Children.Add(_vanillaGamePathTextBox);
        vanillaGamePathPanel.Children.Add(browseGamePathButton);

        mainPanel.Children.Add(vanillaGamePathPanel);

        // Log Path
        var logPathLabel = new TextBlock
        {
            Text = "データログパス:",
            Foreground = Brushes.White,
            Margin = new Thickness(5, 10, 0, 5)
        };
        mainPanel.Children.Add(logPathLabel);

        var vanillaLogPathPanel = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*, Auto"),
            Margin = new Thickness(5, 0, 0, 20)
        };

        _vanillaLogPathTextBox = new TextBox
        {
            Watermark = "ゲームのデータログパスを設定してください（通常は Documents\\Paradox Interactive\\Hearts of Iron IV）",
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.Parse("#333337")),
            BorderBrush = new SolidColorBrush(Color.Parse("#555555")),
            Margin = new Thickness(0, 0, 10, 0)
        };
        Grid.SetColumn(_vanillaLogPathTextBox, 0);

        var browseLogPathButton = new Button
        {
            Content = "参照...",
            Padding = new Thickness(10, 5, 10, 5),
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#555555"))
        };
        browseLogPathButton.Click += OnBrowseLogPathButtonClick;
        Grid.SetColumn(browseLogPathButton, 1);

        vanillaLogPathPanel.Children.Add(_vanillaLogPathTextBox);
        vanillaLogPathPanel.Children.Add(browseLogPathButton);

        mainPanel.Children.Add(vanillaLogPathPanel);

        // ==================== MODリスト設定セクション ====================
        var modListSection = CreateSectionHeader("MODリスト");
        mainPanel.Children.Add(modListSection);

        var toolbarPanel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#333337")),
            Height = 40,
            Margin = new Thickness(0, 0, 0, 1)
        };

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(10, 5, 0, 0)
        };

        _addButton = new Button
        {
            Content = "+",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 5, 0),
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        _addButton.Click += OnAddButtonClick;

        _removeButton = new Button
        {
            Content = "-",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 5, 0),
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            IsEnabled = false
        };
        _removeButton.Click += OnRemoveButtonClick;

        toolbar.Children.Add(_addButton);
        toolbar.Children.Add(_removeButton);
        toolbarPanel.Children.Add(toolbar);

        var listContainer = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
            Padding = new Thickness(5),
            Height = 400
        };

        _modListBox = new ListBox
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            ItemsSource = _modList,
            SelectionMode = SelectionMode.Multiple
        };
        _modListBox.SelectionChanged += OnModSelectionChanged;

        _modListBox.ItemTemplate = new FuncDataTemplate<ModInfo>((item, scope) =>
        {
            if (item == null) return new TextBlock { Text = "No data available", Foreground = Brushes.White };

            var panel = new DockPanel
            {
                LastChildFill = true,
                Margin = new Thickness(5)
            };

            var radioButton = new RadioButton
            {
                GroupName = "ModSelection",
                IsChecked = item.IsActive,
                Margin = new Thickness(0, 0, 5, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            radioButton.Checked += (s, e) =>
            {
                // このMODをアクティブにし、他のすべてのMODを非アクティブにする
                foreach (var mod in _modList) mod.IsActive = mod == item;
                SaveConfigData();
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            Image thumbnail = null;
            if (item.ThumbnailPath != null)
                try
                {
                    var bitmap = new Bitmap(item.ThumbnailPath);
                    thumbnail = new Image
                    {
                        Source = bitmap,
                        Width = 40,
                        Height = 40,
                        Stretch = Stretch.Uniform
                    };
                }
                catch
                {
                    thumbnail = new Image
                    {
                        Width = 40,
                        Height = 40
                    };
                }
            else
                thumbnail = new Image
                {
                    Width = 40,
                    Height = 40
                };

            var textStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            var modNameText = new TextBlock
            {
                Text = item.Name,
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            var modVersionText = new TextBlock
            {
                Text = item.Version,
                Foreground = Brushes.White,
                FontSize = 12
            };

            var modPathText = new TextBlock
            {
                Text = item.Path,
                Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                FontSize = 12
            };

            textStack.Children.Add(modNameText);
            textStack.Children.Add(modVersionText);
            textStack.Children.Add(modPathText);

            stackPanel.Children.Add(thumbnail);
            stackPanel.Children.Add(textStack);

            panel.Children.Add(radioButton);
            panel.Children.Add(stackPanel);
            return panel;
        });

        listContainer.Child = _modListBox;

        mainPanel.Children.Add(toolbarPanel);
        mainPanel.Children.Add(listContainer);

        // ボタンパネル
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(0, 10, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        _saveButton = new Button
        {
            Content = "設定を保存",
            Padding = new Thickness(15, 8, 15, 8),
            Background = new SolidColorBrush(Color.Parse("#0078D7")),
            Foreground = Brushes.White,
            Margin = new Thickness(10, 0, 0, 0)
        };
        _saveButton.Click += OnSaveButtonClick;

        buttonPanel.Children.Add(_saveButton);
        mainPanel.Children.Add(buttonPanel);

        Grid.SetRow(mainPanel, 1);
        grid.Children.Add(headerPanel);
        grid.Children.Add(mainPanel);

        Content = grid;

        LoadConfigData();
    }

    private Panel CreateSectionHeader(string title)
    {
        var panel = new Panel
        {
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Height = 30,
            Margin = new Thickness(0, 10, 0, 10)
        };

        var titleText = new TextBlock
        {
            Text = title,
            Foreground = Brushes.White,
            FontWeight = FontWeight.Bold,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(10, 0, 0, 0)
        };

        panel.Children.Add(titleText);
        return panel;
    }

    private void LoadConfigData()
    {
        try
        {
            var configDir = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

            if (!File.Exists(_configFilePath))
            {
                // デフォルトのログパスを設定
                var defaultLogPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "Paradox Interactive",
                    "Hearts of Iron IV");

                if (Directory.Exists(defaultLogPath))
                {
                    _vanillaLogPath = defaultLogPath;
                    _vanillaLogPathTextBox.Text = defaultLogPath;
                }

                return;
            }

            var json = File.ReadAllText(_configFilePath);
            var config = JsonSerializer.Deserialize<ModConfig>(json);

            if (config != null)
            {
                // バニラゲームパスの設定
                _vanillaGamePath = config.VanillaGamePath;
                _vanillaGamePathTextBox.Text = _vanillaGamePath;

                // バニラログパスの設定
                _vanillaLogPath = config.VanillaLogPath;
                _vanillaLogPathTextBox.Text = _vanillaLogPath;

                // MODリストの設定
                _modList.Clear();
                if (config.Mods != null)
                {
                    foreach (var modInfo in config.Mods)
                    {
                        if (!string.IsNullOrEmpty(modInfo.ThumbnailPath) && !File.Exists(modInfo.ThumbnailPath))
                            modInfo.ThumbnailPath = null;
                        _modList.Add(modInfo);
                    }

                    // アクティブなMODが複数ある場合は最初の1つだけをアクティブにする
                    var foundActive = false;
                    foreach (var mod in _modList)
                        if (mod.IsActive)
                        {
                            if (foundActive)
                                mod.IsActive = false;
                            else
                                foundActive = true;
                        }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定ファイルの読み込み中にエラーが発生しました: {ex.Message}");
        }
    }

    private async void OnBrowseGamePathButtonClick(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folderDialog = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Hearts of Iron IV のインストールフォルダを選択",
            AllowMultiple = false
        });

        if (folderDialog != null && folderDialog.Count > 0)
        {
            var folderPath = folderDialog[0].Path.LocalPath;

            // ゲームディレクトリの検証（executable または特定のディレクトリの存在確認）
            var isValidGameDir = Directory.Exists(Path.Combine(folderPath, "common")) &&
                                 Directory.Exists(Path.Combine(folderPath, "history")) &&
                                 File.Exists(Path.Combine(folderPath, "hoi4.exe"));

            if (isValidGameDir)
            {
                _vanillaGamePath = folderPath;
                _vanillaGamePathTextBox.Text = _vanillaGamePath;
                SaveConfigData();
            }
            else
            {
                // エラーメッセージ表示ロジックをここに追加（ダイアログ表示など）
                Console.WriteLine("選択されたディレクトリは有効なHOI4インストールディレクトリではありません。");

                // あとでダイアログに変更する例
                /*
                var messageBox = new Window
                {
                    Title = "エラー",
                    Width = 300,
                    Height = 150,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                var messagePanel = new StackPanel
                {
                    Margin = new Thickness(20)
                };

                messagePanel.Children.Add(new TextBlock
                {
                    Text = "選択されたディレクトリは有効なHOI4インストールディレクトリではありません。",
                    TextWrapping = TextWrapping.Wrap
                });

                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 10, 0, 0)
                };
                okButton.Click += (s, e) => messageBox.Close();

                messagePanel.Children.Add(okButton);
                messageBox.Content = messagePanel;

                await messageBox.ShowDialog(TopLevel.GetTopLevel(this));
                */
            }
        }
    }

    private async void OnBrowseLogPathButtonClick(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folderDialog = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Hearts of Iron IV のデータログフォルダを選択",
            AllowMultiple = false
        });

        if (folderDialog != null && folderDialog.Count > 0)
        {
            var folderPath = folderDialog[0].Path.LocalPath;

            // ログディレクトリの検証（通常は Paradox Interactive/Hearts of Iron IV フォルダ）
            var isValidLogDir = Directory.Exists(folderPath);

            if (isValidLogDir)
            {
                _vanillaLogPath = folderPath;
                _vanillaLogPathTextBox.Text = _vanillaLogPath;
                SaveConfigData();
            }
            else
            {
                Console.WriteLine("選択されたディレクトリが見つかりません。");
            }
        }
    }

    private async void OnAddButtonClick(object sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folderDialog = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "MODフォルダを選択",
            AllowMultiple = false
        });

        if (folderDialog != null && folderDialog.Count > 0)
        {
            var folderPath = folderDialog[0].Path.LocalPath;
            var descriptorPath = Path.Combine(folderPath, "descriptor.mod");

            var modName = Path.GetFileName(folderPath);
            var modVersion = "Unknown";
            var thumbnailPath = Path.Combine(folderPath, "thumbnail.png");

            if (File.Exists(descriptorPath))
            {
                var lines = File.ReadAllLines(descriptorPath);
                foreach (var line in lines)
                    if (line.StartsWith("name="))
                        modName = line.Split('=')[1].Trim('"');
                    else if (line.StartsWith("version="))
                        modVersion = line.Split('=')[1].Trim('"');
                    else if (line.StartsWith("picture="))
                        thumbnailPath = Path.Combine(folderPath, line.Split('=')[1].Trim('"'));
            }

            // 1つもアクティブなMODがなければ、このMODをアクティブにする
            var setActive = !_modList.Any(m => m.IsActive);

            var modInfo = new ModInfo
            {
                Name = modName,
                Version = modVersion,
                Path = folderPath,
                ThumbnailPath = File.Exists(thumbnailPath) ? thumbnailPath : null,
                IsActive = setActive // 既存のMODがない場合のみアクティブに
            };

            _modList.Add(modInfo);
            SaveConfigData();
        }
    }

    private void OnRemoveButtonClick(object sender, RoutedEventArgs e)
    {
        var selectedItems = _modListBox.SelectedItems.Cast<ModInfo>().ToList();
        if (selectedItems.Any())
        {
            foreach (var item in selectedItems) _modList.Remove(item);
            SaveConfigData();
        }

        _removeButton.IsEnabled = _modListBox.SelectedItems.Count > 0;
    }

    private void OnModSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _removeButton.IsEnabled = _modListBox.SelectedItems.Count > 0;
    }

    private void OnSaveButtonClick(object sender, RoutedEventArgs e)
    {
        SaveConfigData();
    }

    private void SaveConfigData()
    {
        try
        {
            var configDir = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var config = new ModConfig
            {
                VanillaGamePath = _vanillaGamePath ?? _vanillaGamePathTextBox.Text,
                VanillaLogPath = _vanillaLogPath ?? _vanillaLogPathTextBox.Text,
                Mods = _modList.ToList()
            };

            var json = JsonSerializer.Serialize(config, options);

            File.WriteAllText(_configFilePath, json);
            Console.WriteLine("設定を保存しました");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定ファイルの保存中にエラーが発生しました: {ex.Message}");
        }
    }
}