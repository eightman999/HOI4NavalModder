using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Layout;
using Avalonia.Interactivity;

namespace HOI4NavalModder
{
    public class FleetDeploymentWindow : Window
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

        // 艦隊リスト
        private ListBox _fleetListBox;
        private ObservableCollection<FleetInfo> _fleetsList = new ObservableCollection<FleetInfo>();

        // 国家情報
        private string _countryTag;
        private string _countryName;
        private Bitmap _countryFlag;

        // パス情報
        private string _activeMod;
        private string _vanillaPath;

        public FleetDeploymentWindow(string countryTag, string countryName, Bitmap countryFlag, string activeMod,
            string vanillaPath)
        {
            // ウィンドウ設定
            Title = $"{countryName} 艦隊配備";
            Width = 800;
            Height = 600;
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

            // 艦隊リストパネル
            var fleetListPanel = CreateFleetListPanel();
            Grid.SetRow(fleetListPanel, 1);

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
            mainGrid.Children.Add(fleetListPanel);
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

                var detailsButton = new Button
                {
                    Content = "詳細",
                    Padding = new Thickness(8, 4, 8, 4),
                    Background = new SolidColorBrush(Color.Parse("#1E90FF")),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                detailsButton.Click += (s, e) => ShowFleetDetails(item);
                Grid.SetColumn(detailsButton, 1);

                panel.Children.Add(leftPanel);
                panel.Children.Add(detailsButton);

                return panel;
            });

            listContainer.Child = _fleetListBox;
            Grid.SetRow(listContainer, 1);

            panel.Children.Add(toolbarPanel);
            panel.Children.Add(listContainer);

            return panel;
        }

        private void LoadFleetData()
        {
            try
            {
                _loadingProgressBar.IsVisible = true;
                _statusTextBlock.Text = "艦隊データを読み込み中...";

                // MODディレクトリからの艦隊データ読み込みを試みる
                if (!string.IsNullOrEmpty(_activeMod))
                {
                    string fleetFilePath = Path.Combine(_activeMod, "common", "units", "fleets",
                        $"{_countryTag}_fleets.txt");
                    if (File.Exists(fleetFilePath))
                    {
                        // 実際の実装ではここでファイルからデータを読み込む
                        // 読み込み処理は省略
                    }
                }

                // データがなければサンプルを表示
                if (_fleetsList.Count == 0)
                {
                    // サンプルデータを追加
                    var fleet1 = new FleetInfo
                    {
                        Name = "第1艦隊",
                        BaseLocation = "主要港",
                        ProvinceId = 1234
                    };
                    fleet1.Ships.Add(new ShipInfo { Name = "戦艦A", ShipType = "戦艦", Count = 1 });
                    fleet1.Ships.Add(new ShipInfo { Name = "空母B", ShipType = "空母", Count = 1 });

                    var fleet2 = new FleetInfo
                    {
                        Name = "第2艦隊",
                        BaseLocation = "補助港",
                        ProvinceId = 2345
                    };
                    fleet2.Ships.Add(new ShipInfo { Name = "巡洋艦C", ShipType = "巡洋艦", Count = 3 });
                    fleet2.Ships.Add(new ShipInfo { Name = "駆逐艦D", ShipType = "駆逐艦", Count = 5 });

                    _fleetsList.Add(fleet1);
                    _fleetsList.Add(fleet2);
                }

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

            // 実際の実装ではダイアログを表示してユーザー入力を受け付ける
            ShowFleetEditDialog(null);
        }

        private void OnEditFleetButtonClick(object sender, RoutedEventArgs e)
        {
            var selectedFleet = _fleetListBox.SelectedItem as FleetInfo;
            if (selectedFleet != null)
            {
                _statusTextBlock.Text = $"{selectedFleet.Name} を編集します";

                // 編集ダイアログを表示
                ShowFleetEditDialog(selectedFleet);
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

        private async void ShowFleetEditDialog(FleetInfo fleet)
        {
            // 新規作成か編集か
            bool isNew = fleet == null;

            // 編集用ウィンドウの作成
            var dialog = new Window
            {
                Title = isNew ? "艦隊の追加" : "艦隊の編集",
                Width = 400,
                Height = 300,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Margin = new Thickness(10)
            };

            // 入力フォーム
            var formPanel = new StackPanel
            {
                Spacing = 10
            };

            // 艦隊名
            var namePanel = new StackPanel { Spacing = 5 };
            namePanel.Children.Add(new TextBlock { Text = "艦隊名:", Foreground = Brushes.White });
            var nameTextBox = new TextBox
            {
                Text = isNew ? "" : fleet.Name,
                Watermark = "艦隊名を入力"
            };
            namePanel.Children.Add(nameTextBox);

            // 基地位置
            var basePanel = new StackPanel { Spacing = 5 };
            basePanel.Children.Add(new TextBlock { Text = "基地位置:", Foreground = Brushes.White });
            var baseTextBox = new TextBox
            {
                Text = isNew ? "" : fleet.BaseLocation,
                Watermark = "基地名を入力"
            };
            basePanel.Children.Add(baseTextBox);

            // プロヴィンスID
            var provincePanel = new StackPanel { Spacing = 5 };
            provincePanel.Children.Add(new TextBlock { Text = "プロヴィンスID:", Foreground = Brushes.White });
            var provinceTextBox = new TextBox
            {
                Text = isNew ? "0" : fleet.ProvinceId.ToString(),
                Watermark = "プロヴィンスIDを入力"
            };
            provincePanel.Children.Add(provinceTextBox);

            formPanel.Children.Add(namePanel);
            formPanel.Children.Add(basePanel);
            formPanel.Children.Add(provincePanel);

            Grid.SetRow(formPanel, 0);

            // ボタンパネル
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var cancelButton = new Button
            {
                Content = "キャンセル",
                Padding = new Thickness(10, 5, 10, 5)
            };
            cancelButton.Click += (s, e) => dialog.Close();

            var saveButton = new Button
            {
                Content = "保存",
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.Parse("#0078D7")),
                Foreground = Brushes.White
            };
            saveButton.Click += (s, e) =>
            {
                try
                {
                    string name = nameTextBox.Text?.Trim() ?? "";
                    string baseLocation = baseTextBox.Text?.Trim() ?? "";

                    if (string.IsNullOrEmpty(name))
                    {
                        _statusTextBlock.Text = "艦隊名は必須です";
                        return;
                    }

                    if (!int.TryParse(provinceTextBox.Text, out int provinceId))
                    {
                        provinceId = 0;
                    }

                    if (isNew)
                    {
                        // 新規追加
                        var newFleet = new FleetInfo
                        {
                            Name = name,
                            BaseLocation = baseLocation,
                            ProvinceId = provinceId
                        };
                        _fleetsList.Add(newFleet);
                        _statusTextBlock.Text = $"艦隊「{name}」を追加しました";
                    }
                    else
                    {
                        // 既存の編集
                        fleet.Name = name;
                        fleet.BaseLocation = baseLocation;
                        fleet.ProvinceId = provinceId;

                        // ListBoxの表示を更新
                        int index = _fleetsList.IndexOf(fleet);
                        if (index >= 0)
                        {
                            _fleetsList.Remove(fleet);
                            _fleetsList.Insert(index, fleet);
                        }

                        _statusTextBlock.Text = $"艦隊「{name}」を更新しました";
                    }

                    dialog.Close();
                }
                catch (Exception ex)
                {
                    _statusTextBlock.Text = $"エラー: {ex.Message}";
                }
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(saveButton);

            Grid.SetRow(buttonPanel, 1);

            grid.Children.Add(formPanel);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            // ダイアログ表示
            await dialog.ShowDialog(this);
        }

        private async void ShowFleetDetails(FleetInfo fleet)
        {
            // 艦隊詳細ウィンドウの作成
            var dialog = new Window
            {
                Title = $"{fleet.Name} の詳細",
                Width = 500,
                Height = 400,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*,Auto"),
                Margin = new Thickness(10)
            };

            // ヘッダー情報
            var headerPanel = new StackPanel
            {
                Spacing = 5,
                Margin = new Thickness(0, 0, 0, 10)
            };

            headerPanel.Children.Add(new TextBlock
            {
                Text = fleet.Name,
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White
            });

            headerPanel.Children.Add(new TextBlock
            {
                Text = $"基地: {fleet.BaseLocation} (ID: {fleet.ProvinceId})",
                Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                FontSize = 14
            });

            Grid.SetRow(headerPanel, 0);

            // 艦船リスト
            var listContainer = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D30")),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
                Padding = new Thickness(5)
            };

            var shipListBox = new ListBox
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D30")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ItemsSource = fleet.Ships
            };

            shipListBox.ItemTemplate = new FuncDataTemplate<ShipInfo>((item, scope) =>
            {
                if (item == null)
                {
                    return new TextBlock { Text = "データがありません", Foreground = Brushes.White };
                }

                var panel = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
                    Margin = new Thickness(5)
                };

                var nameText = new TextBlock
                {
                    Text = item.Name,
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(nameText, 0);

                var typeText = new TextBlock
                {
                    Text = item.ShipType,
                    Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                    Margin = new Thickness(5, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(typeText, 1);

                var countText = new TextBlock
                {
                    Text = $"×{item.Count}",
                    Foreground = Brushes.White,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(countText, 2);

                panel.Children.Add(nameText);
                panel.Children.Add(typeText);
                panel.Children.Add(countText);

                return panel;
            });

            listContainer.Child = shipListBox;
            Grid.SetRow(listContainer, 1);

            // フッタボタン
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var addShipButton = new Button
            {
                Content = "艦船追加",
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.Parse("#0078D7")),
                Foreground = Brushes.White
            };
            addShipButton.Click += (s, e) => ShowAddShipDialog(fleet, shipListBox);

            var closeButton = new Button
            {
                Content = "閉じる",
                Padding = new Thickness(10, 5, 10, 5)
            };
            closeButton.Click += (s, e) => dialog.Close();

            buttonPanel.Children.Add(addShipButton);
            buttonPanel.Children.Add(closeButton);

            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(headerPanel);
            grid.Children.Add(listContainer);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

            // ダイアログ表示
            await dialog.ShowDialog(this);
        }

        private async void ShowAddShipDialog(FleetInfo fleet, ListBox shipListBox)
        {
            // 艦船追加ダイアログの作成
            var dialog = new Window
            {
                Title = "艦船の追加",
                Width = 350,
                Height = 250,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("*,Auto"),
                Margin = new Thickness(10)
            };

            // 入力フォーム
            var formPanel = new StackPanel
            {
                Spacing = 10
            };

            // 艦船名
            var namePanel = new StackPanel { Spacing = 5 };
            namePanel.Children.Add(new TextBlock { Text = "艦船名:", Foreground = Brushes.White });
            var nameTextBox = new TextBox
            {
                Watermark = "艦船名を入力"
            };
            namePanel.Children.Add(nameTextBox);

            // 艦船タイプ
            var typePanel = new StackPanel { Spacing = 5 };
            typePanel.Children.Add(new TextBlock { Text = "艦船タイプ:", Foreground = Brushes.White });
            var typeComboBox = new ComboBox
            {
                Width = 200,
                SelectedIndex = 0
            };
            typeComboBox.Items.Add("戦艦");
            typeComboBox.Items.Add("空母");
            typeComboBox.Items.Add("巡洋艦");
            typeComboBox.Items.Add("駆逐艦");
            typeComboBox.Items.Add("潜水艦");
            typePanel.Children.Add(typeComboBox);

            // 数量
            var countPanel = new StackPanel { Spacing = 5 };
            countPanel.Children.Add(new TextBlock { Text = "数量:", Foreground = Brushes.White });
            var countNumeric = new NumericUpDown
            {
                Value = 1,
                Minimum = 1,
                Maximum = 100,
                Increment = 1,
                Width = 100,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            countPanel.Children.Add(countNumeric);

            formPanel.Children.Add(namePanel);
            formPanel.Children.Add(typePanel);
            formPanel.Children.Add(countPanel);

            Grid.SetRow(formPanel, 0);

// ボタンパネル
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10,
                Margin = new Thickness(0, 10, 0, 0)
            };

            var cancelButton = new Button
            {
                Content = "キャンセル",
                Padding = new Thickness(10, 5, 10, 5)
            };
            cancelButton.Click += (s, e) => dialog.Close();

            var addButton = new Button
            {
                Content = "追加",
                Padding = new Thickness(10, 5, 10, 5),
                Background = new SolidColorBrush(Color.Parse("#0078D7")),
                Foreground = Brushes.White
            };
            addButton.Click += (s, e) =>
            {
                try
                {
                    string name = nameTextBox.Text?.Trim() ?? "";
                    string type = typeComboBox.SelectedItem?.ToString() ?? "不明";
                    int count = (int)countNumeric.Value;

                    if (string.IsNullOrEmpty(name))
                    {
                        _statusTextBlock.Text = "艦船名は必須です";
                        return;
                    }

                    // 艦船を追加
                    fleet.Ships.Add(new ShipInfo
                    {
                        Name = name,
                        ShipType = type,
                        Count = count
                    });

                    // リスト表示を更新
                    shipListBox.Items.Add(fleet.Ships.Last());

                    _statusTextBlock.Text = $"艦船「{name}」を追加しました";
                    dialog.Close();
                }
                catch (Exception ex)
                {
                    _statusTextBlock.Text = $"エラー: {ex.Message}";
                }
            };

            buttonPanel.Children.Add(cancelButton);
            buttonPanel.Children.Add(addButton);

            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(formPanel);
            grid.Children.Add(buttonPanel);

            dialog.Content = grid;

// ダイアログ表示
            await dialog.ShowDialog(this);
        }
    }
}