using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.Interactivity;

namespace HOI4NavalModder
{
    public class FleetDeploymentMapView : Window
    {
        // モデルクラス
        public class FleetInfo
        {
            public string Name { get; set; }
            public string BaseLocation { get; set; }
            public int ProvinceId { get; set; }
            public ObservableCollection<ShipInfo> Ships { get; set; } = new ObservableCollection<ShipInfo>();
        }

        public class ShipInfo
        {
            public string Name { get; set; }
            public string ShipType { get; set; }
            public int Count { get; set; }
        }

        private TextBlock _statusTextBlock;
        private Button _closeButton;
        private ProgressBar _loadingProgressBar;
        
        // タブビュー
        private TabControl _viewTabControl;
        private Panel _fleetListPanel;
        private Panel _mapViewPanel;
        private MapViewer _mapViewer;
        private ListBox _fleetListBox;
        private ObservableCollection<FleetInfo> _fleetsList = new ObservableCollection<FleetInfo>();
        
        // 国家情報
        private string _countryTag;
        private string _countryName;
        private Bitmap _countryFlag;
        
        // パス情報
        private string _activeMod;
        private string _vanillaPath;
        
        public FleetDeploymentMapView(string countryTag, string countryName, Bitmap countryFlag, string activeMod, string vanillaPath)
        {
            // ウィンドウ設定
            Title = $"{countryName} 艦隊配備";
            Width = 1200;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            // パラメータを保存
            _countryTag = countryTag;
            _countryName = countryName;
            _countryFlag = countryFlag;
            _activeMod = activeMod;
            _vanillaPath = vanillaPath;
            
            // UIを初期化
            InitializeComponent();
            
            // 艦隊データの初期読み込み
            LoadFleetData();
            
            // マップビューアの初期化
            InitializeMapViewer();
        }
        
        private void InitializeComponent()
        {
            var mainGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto")
            };

            // ヘッダーパネル
            var headerPanel = new Panel
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D30")),
                Height = 50
            };

            var headerContent = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(20, 0, 20, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            
            // 国旗表示
            var flagBorder = new Border
            {
                Width = 40,
                Height = 24,
                Margin = new Thickness(0, 0, 10, 0),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
                ClipToBounds = true
            };

            if (_countryFlag != null)
            {
                var flagImage = new Image
                {
                    Source = _countryFlag,
                    Width = 40,
                    Height = 24,
                    Stretch = Stretch.Uniform
                };
                flagBorder.Child = flagImage;
            }
            else
            {
                var noFlagText = new TextBlock
                {
                    Text = "?",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.White
                };
                flagBorder.Child = noFlagText;
            }
            
            var headerText = new TextBlock
            {
                Text = $"{_countryName} ({_countryTag}) 艦隊配備",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 16,
                FontWeight = FontWeight.Bold
            };

            headerContent.Children.Add(flagBorder);
            headerContent.Children.Add(headerText);
            headerPanel.Children.Add(headerContent);
            Grid.SetRow(headerPanel, 0);

            // コンテンツエリア
            _viewTabControl = new TabControl
            {
                Margin = new Thickness(20),
                TabStripPlacement = Dock.Top
            };
            
            // 艦隊リストタブ
            var fleetListTab = new TabItem
            {
                Header = "艦隊リスト",
                Padding = new Thickness(10)
            };
            
            _fleetListPanel = CreateFleetListPanel();
            fleetListTab.Content = _fleetListPanel;
            
            // マップビュータブ
            var mapViewTab = new TabItem
            {
                Header = "マップビュー",
                Padding = new Thickness(10)
            };
            
            _mapViewPanel = new Panel();
            _mapViewer = new MapViewer();
            _mapViewPanel.Children.Add(_mapViewer);
            mapViewTab.Content = _mapViewPanel;
            
            _viewTabControl.Items.Add(fleetListTab);
            _viewTabControl.Items.Add(mapViewTab);
            
            Grid.SetRow(_viewTabControl, 1);
            
            // フッターパネル
            var footerPanel = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Height = 40,
                Background = new SolidColorBrush(Color.Parse("#2D2D30"))
            };
            
            _statusTextBlock = new TextBlock
            {
                Text = "準備完了",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0)
            };
            Grid.SetColumn(_statusTextBlock, 0);
            
            _closeButton = new Button
            {
                Content = "閉じる",
                Padding = new Thickness(15, 5, 15, 5),
                Margin = new Thickness(0, 0, 20, 0),
                Background = new SolidColorBrush(Color.Parse("#3E3E42")),
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            _closeButton.Click += (s, e) => Close();
            Grid.SetColumn(_closeButton, 1);
            
            _loadingProgressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 4,
                Margin = new Thickness(0),
                IsVisible = false
            };
            
            var footerStackPanel = new StackPanel
            {
                Spacing = 1
            };
            
            footerStackPanel.Children.Add(_loadingProgressBar);
            
            var footerContentPanel = new Panel();
            footerContentPanel.Children.Add(_statusTextBlock);
            footerContentPanel.Children.Add(_closeButton);
            
            footerStackPanel.Children.Add(footerContentPanel);
            footerPanel.Children.Add(footerStackPanel);
            
            Grid.SetRow(footerPanel, 2);

            mainGrid.Children.Add(headerPanel);
            mainGrid.Children.Add(_viewTabControl);
            mainGrid.Children.Add(footerPanel);

            Content = mainGrid;
        }
        
        private Panel CreateFleetListPanel()
        {
            var panel = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*")
            };
            
            // ツールバー
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

            var addFleetButton = new Button
            {
                Content = "艦隊追加",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 5, 0),
                Background = new SolidColorBrush(Color.Parse("#3E3E42")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            addFleetButton.Click += OnAddFleetButtonClick;

            var editFleetButton = new Button
            {
                Content = "艦隊編集",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 5, 0),
                Background = new SolidColorBrush(Color.Parse("#3E3E42")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsEnabled = false
            };
            editFleetButton.Click += OnEditFleetButtonClick;
            
            var removeFleetButton = new Button
            {
                Content = "艦隊削除",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 5, 0),
                Background = new SolidColorBrush(Color.Parse("#3E3E42")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsEnabled = false
            };
            removeFleetButton.Click += OnRemoveFleetButtonClick;

            toolbar.Children.Add(addFleetButton);
            toolbar.Children.Add(editFleetButton);
            toolbar.Children.Add(removeFleetButton);
            toolbarPanel.Children.Add(toolbar);
            
            Grid.SetRow(toolbarPanel, 0);
            
            // 艦隊リスト
            var listContainer = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D30")),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
                Padding = new Thickness(5)
            };
            
            _fleetListBox = new ListBox
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D30")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ItemsSource = _fleetsList
            };
            _fleetListBox.SelectionChanged += (s, e) => 
            {
                editFleetButton.IsEnabled = _fleetListBox.SelectedItem != null;
                removeFleetButton.IsEnabled = _fleetListBox.SelectedItem != null;
            };
            
            _fleetListBox.ItemTemplate = new FuncDataTemplate<FleetInfo>((item, scope) =>
            {
                if (item == null)
                {
                    return new TextBlock { Text = "データがありません", Foreground = Brushes.White };
                }

                var panel = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Margin = new Thickness(5)
                };

                var leftPanel = new StackPanel
                {
                    Orientation = Orientation.Vertical,
                    Spacing = 5
                };
                
                var fleetNameText = new TextBlock
                {
                    Text = item.Name,
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold
                };
                
                var baseLocationText = new TextBlock
                {
                    Text = $"基地: {item.BaseLocation} (ID: {item.ProvinceId})",
                    Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                    FontSize = 12
                };
                
                var shipsCountText = new TextBlock
                {
                    Text = $"艦船数: {item.Ships.Count}",
                    Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                    FontSize = 12
                };
                
                leftPanel.Children.Add(fleetNameText);
                leftPanel.Children.Add(baseLocationText);
                leftPanel.Children.Add(shipsCountText);
                
                Grid.SetColumn(leftPanel, 0);
                
                var showOnMapButton = new Button
                {
                    Content = "マップで表示",
                    Padding = new Thickness(8, 4, 8, 4),
                    Background = new SolidColorBrush(Color.Parse("#1E90FF")),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                showOnMapButton.Click += (s, e) => 
                {
                    // マップタブに切り替えてこの艦隊の位置を表示
                    _viewTabControl.SelectedIndex = 1;
                    
                    // マップの該当プロヴィンスにフォーカス
                    // 実装を追加
                };
                Grid.SetColumn(showOnMapButton, 1);
                
                panel.Children.Add(leftPanel);
                panel.Children.Add(showOnMapButton);
                
                return panel;
            });
            
            listContainer.Child = _fleetListBox;
            Grid.SetRow(listContainer, 1);
            
            panel.Children.Add(toolbarPanel);
            panel.Children.Add(listContainer);
            
            return panel;
        }
        
        private async void InitializeMapViewer()
        {
            try
            {
                _loadingProgressBar.IsVisible = true;
                _statusTextBlock.Text = "マップデータを読み込み中...";
                
                await _mapViewer.Initialize(_vanillaPath, _activeMod);
                
                _statusTextBlock.Text = "マップデータの読み込みが完了しました";
            }
            catch (Exception ex)
            {
                _statusTextBlock.Text = $"マップ初期化エラー: {ex.Message}";
            }
            finally
            {
                _loadingProgressBar.IsVisible = false;
            }
        }
        
        private void LoadFleetData()
        {
            try
            {
                _loadingProgressBar.IsVisible = true;
                _statusTextBlock.Text = "艦隊データを読み込み中...";
                
                // ここでは例としてサンプルデータを追加
                // 実際の実装ではMODまたはバニラから艦隊データを読み込む
                _fleetsList.Clear();
                
                // サンプルデータを追加
                var fleet1 = new FleetInfo
                {
                    Name = "第1艦隊",
                    BaseLocation = "東京湾",
                    ProvinceId = 1234
                };
                fleet1.Ships.Add(new ShipInfo { Name = "大和", ShipType = "戦艦", Count = 1 });
                fleet1.Ships.Add(new ShipInfo { Name = "赤城", ShipType = "空母", Count = 1 });
                
                var fleet2 = new FleetInfo
                {
                    Name = "第2艦隊",
                    BaseLocation = "神戸港",
                    ProvinceId = 2345
                };
                fleet2.Ships.Add(new ShipInfo { Name = "巡洋艦", ShipType = "巡洋艦", Count = 3 });
                fleet2.Ships.Add(new ShipInfo { Name = "駆逐艦", ShipType = "駆逐艦", Count = 5 });
                
                _fleetsList.Add(fleet1);
                _fleetsList.Add(fleet2);
                
                _statusTextBlock.Text = $"{_fleetsList.Count} 艦隊を読み込みました";
            }
            catch (Exception ex)
            {
                _statusTextBlock.Text = $"艦隊データ読み込みエラー: {ex.Message}";
            }
            finally
            {
                _loadingProgressBar.IsVisible = false;
            }
        }
        
        // イベントハンドラ
        private void OnAddFleetButtonClick(object sender, RoutedEventArgs e)
        {
            // 艦隊追加ダイアログを表示
            _statusTextBlock.Text = "新しい艦隊を追加します";
            
            // 実際の実装ではダイアログを表示
            var newFleet = new FleetInfo
            {
                Name = $"新規艦隊 {_fleetsList.Count + 1}",
                BaseLocation = "未設定",
                ProvinceId = 0
            };
            
            _fleetsList.Add(newFleet);
        }
        
        private void OnEditFleetButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedFleet = _fleetListBox.SelectedItem as FleetInfo;
            if (selectedFleet != null)
            {
                _statusTextBlock.Text = $"{selectedFleet.Name} を編集します";
                
                // 実際の実装ではダイアログを表示
            }
        }
        
        private void OnRemoveFleetButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedFleet = _fleetListBox.SelectedItem as FleetInfo;
            if (selectedFleet != null)
            {
                _fleetsList.Remove(selectedFleet);
                _statusTextBlock.Text = $"{selectedFleet.Name} を削除しました";
            }
        }
    }
}