using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using System.Collections.Generic;
using Avalonia.Interactivity;

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
            _pages.Add("EquipmentImageCreation", new EquipmentImageCreationView());
            _pages.Add("ShipDesign", new ShipDesignView());
            _pages.Add("HullImageCreation", new HullImageCreationView());
            _pages.Add("FleetDeployment", new FleetDeploymentView());
            _pages.Add("ShipNameEntry", new ShipNameEntryView());
            _pages.Add("TranslationFile", new TranslationFileView());

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

        public void OnEquipmentImageButtonClick(object sender, RoutedEventArgs e)
        {
            NavigateTo("EquipmentImageCreation", (Button)sender);
        }

        public void OnShipDesignButtonClick(object sender, RoutedEventArgs e)
        {
            NavigateTo("ShipDesign", (Button)sender);
        }

        public void OnHullImageButtonClick(object sender, RoutedEventArgs e)
        {
            NavigateTo("HullImageCreation", (Button)sender);
        }

        public void OnFleetDeploymentButtonClick(object sender, RoutedEventArgs e)
        {
            NavigateTo("FleetDeployment", (Button)sender);
        }

        public void OnShipNameButtonClick(object sender, RoutedEventArgs e)
        {
            NavigateTo("ShipNameEntry", (Button)sender);
        }

        public void OnTranslationButtonClick(object sender, RoutedEventArgs e)
        {
            NavigateTo("TranslationFile", (Button)sender);
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

    public class EquipmentImageCreationView : UserControl
    {
        public EquipmentImageCreationView()
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
                Text = "装備画像作成",
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0),
                FontSize = 16
            };

            headerPanel.Children.Add(headerText);
            Grid.SetRow(headerPanel, 0);
            
            var contentPanel = ModuleHelper.CreateModuleContent("装備のアイコンやグラフィックを作成できます");
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

    public class HullImageCreationView : UserControl
    {
        public HullImageCreationView()
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
                Text = "船体画像作成",
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0),
                FontSize = 16
            };

            headerPanel.Children.Add(headerText);
            Grid.SetRow(headerPanel, 0);

            var contentPanel = ModuleHelper.CreateModuleContent("船体のグラフィックを作成できます");
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

    public class ShipNameEntryView : UserControl
    {
        public ShipNameEntryView()
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
                Text = "艦名表入力",
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

    public class TranslationFileView : UserControl
    {
        public TranslationFileView()
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
                Text = "翻訳ファイル",
                Foreground = Brushes.White,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0),
                FontSize = 16
            };

            headerPanel.Children.Add(headerText);
            Grid.SetRow(headerPanel, 0);

            var contentPanel = ModuleHelper.CreateModuleContent("翻訳ファイルの編集を行います");
            Grid.SetRow(contentPanel, 1);

            grid.Children.Add(headerPanel);
            grid.Children.Add(contentPanel);

            Content = grid;
        }
    }
}