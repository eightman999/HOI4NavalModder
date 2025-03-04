using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace HOI4NavalModder
{
    public partial class MainWindow : Window
    {
        private Panel _contentPanel;
        private readonly Dictionary<string, UserControl> _pages = new Dictionary<string, UserControl>();
        private Button _activeButton;
        
        public MainWindow()
        {
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
            _pages.Add("IDESettings", new IDESettingsView());
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
            if (_activeButton != null)
            {
                _activeButton.Background = new SolidColorBrush(Color.Parse("#252526"));
            }

            sourceButton.Background = new SolidColorBrush(Color.Parse("#3E3E42"));
            _activeButton = sourceButton;

            // コンテンツを更新
            _contentPanel.Children.Clear();
            if (_pages.TryGetValue(page, out var control))
            {
                _contentPanel.Children.Add(control);
            }
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
                Orientation = Avalonia.Layout.Orientation.Horizontal,
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
    public partial class EquipmentDesignView : UserControl
    {
        public EquipmentDesignView()
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
                Text = "装備設計",
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0),
                FontSize = 16
            };

            headerPanel.Children.Add(headerText);
            Grid.SetRow(headerPanel, 0);

            var contentPanel = ModuleHelper.CreateModuleContent("装備の設計と性能調整ができます");
            Grid.SetRow(contentPanel, 1);

            grid.Children.Add(headerPanel);
            grid.Children.Add(contentPanel);

            Content = grid;
        }
    }

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
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
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
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
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
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
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
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
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

    public class FleetDeploymentView : UserControl
    {
        public FleetDeploymentView()
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
                Text = "艦隊配備",
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0),
                FontSize = 16
            };

            headerPanel.Children.Add(headerText);
            Grid.SetRow(headerPanel, 0);

            var contentPanel = ModuleHelper.CreateModuleContent("艦隊の編成と配備を行います");
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
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
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
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
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

    public class IDESettingsView : UserControl
    {
        public IDESettingsView()
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
                Text = "IDE設定",
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0),
                FontSize = 16
            };

            headerPanel.Children.Add(headerText);
            Grid.SetRow(headerPanel, 0);

            var contentPanel = ModuleHelper.CreateModuleContent("開発環境の設定ができます");
            Grid.SetRow(contentPanel, 1);

            grid.Children.Add(headerPanel);
            grid.Children.Add(contentPanel);

            Content = grid;
        }
    }


 public class ModSettingsView : UserControl
{
    private ObservableCollection<ModInfo> _modList = new ObservableCollection<ModInfo>();
    private ListBox _modListBox;
    private Button _addButton;
    private Button _removeButton;
    private Button _saveButton;

    private readonly string _configFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HOI4NavalModder",
        "modpaths.json");

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
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
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
            Text = "MODのパスを設定します",
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
            Padding = new Thickness(5)
        };

        _modListBox = new ListBox
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            ItemsSource = _modList
        };
        _modListBox.SelectionChanged += OnModSelectionChanged;

        _modListBox.ItemTemplate = new FuncDataTemplate<ModInfo>((item, scope) =>
        {
            var panel = new DockPanel
            {
                LastChildFill = true,
                Margin = new Thickness(5)
            };

            var stackPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10
            };

            Image thumbnail = null;
            if (item.ThumbnailPath != null)
            {
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
            }
            else
            {
                thumbnail = new Image
                {
                    Width = 40,
                    Height = 40
                };
            }

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

            panel.Children.Add(stackPanel);
            return panel;
        });

        listContainer.Child = _modListBox;

        mainPanel.Children.Add(descriptionText);
        mainPanel.Children.Add(toolbarPanel);
        mainPanel.Children.Add(listContainer);

        Grid.SetRow(mainPanel, 1);
        grid.Children.Add(headerPanel);
        grid.Children.Add(mainPanel);

        Content = grid;

        LoadConfigData();
    }

    private void LoadConfigData()
    {
        try
        {
            var configDir = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            if (!File.Exists(_configFilePath))
            {
                return;
            }

            var json = File.ReadAllText(_configFilePath);
            var modInfoList = JsonSerializer.Deserialize<List<ModInfo>>(json);

            if (modInfoList != null)
            {
                _modList.Clear();
                foreach (var modInfo in modInfoList)
                {
                    if (!string.IsNullOrEmpty(modInfo.ThumbnailPath) && !File.Exists(modInfo.ThumbnailPath))
                    {
                        modInfo.ThumbnailPath = null;
                    }
                    _modList.Add(modInfo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定ファイルの読み込み中にエラーが発生しました: {ex.Message}");
        }
    }

    private async void OnAddButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
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

            string modName = Path.GetFileName(folderPath);
            string modVersion = "Unknown";
            string thumbnailPath = Path.Combine(folderPath, "thumbnail.png");

            if (File.Exists(descriptorPath))
            {
                var lines = File.ReadAllLines(descriptorPath);
                foreach (var line in lines)
                {
                    if (line.StartsWith("name="))
                    {
                        modName = line.Split('=')[1].Trim('"');
                    }
                    else if (line.StartsWith("version="))
                    {
                        modVersion = line.Split('=')[1].Trim('"');
                    }
                    else if (line.StartsWith("picture="))
                    {
                        thumbnailPath = Path.Combine(folderPath, line.Split('=')[1].Trim('"'));
                    }
                }
            }

            var modInfo = new ModInfo
            {
                Name = modName,
                Version = modVersion,
                Path = folderPath,
                ThumbnailPath = File.Exists(thumbnailPath) ? thumbnailPath : null
            };

            _modList.Add(modInfo);
            SaveConfigData();
        }
    }

    private void OnRemoveButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_modListBox.SelectedItem is ModInfo selectedMod)
        {
            _modList.Remove(selectedMod);
            SaveConfigData();
        }

        _removeButton.IsEnabled = _modListBox.SelectedItem != null;
    }

    private void OnModSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _removeButton.IsEnabled = _modListBox.SelectedItem != null;
    }

    private void SaveConfigData()
    {
        try
        {
            var configDir = Path.GetDirectoryName(_configFilePath);
            if (!Directory.Exists(configDir))
            {
                Directory.CreateDirectory(configDir);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var modInfoList = _modList.ToList();
            var json = JsonSerializer.Serialize(modInfoList, options);

            File.WriteAllText(_configFilePath, json);
            Console.WriteLine("設定を保存しました");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定ファイルの保存中にエラーが発生しました: {ex.Message}");
        }
    }
}

    // MOD情報を保持するクラス
    public class ModInfo
    {
        public string Name { get; set; }
        public string Version { get; set; }
        public string Path { get; set; }
        public string ThumbnailPath { get; set; }

        public ModInfo() { }
    }
}