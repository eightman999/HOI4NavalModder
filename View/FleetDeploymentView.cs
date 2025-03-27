using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace HOI4NavalModder;

public class FleetDeploymentView : UserControl
{
    // 設定ファイルパス
    private readonly string _configFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HOI4NavalModder",
        "modpaths.json");

    private readonly ObservableCollection<CountryListManager.CountryInfo> _countriesList = new();
    private readonly ListBox _countriesListBox;
    private readonly Button _exportButton;
    private readonly ProgressBar _loadingProgressBar;
    private readonly Button _refreshButton;
    private readonly TextBox _searchBox;
    private readonly CheckBox _showAllCountriesCheckBox;
    private readonly TextBlock _statusTextBlock;

    private string _activeMod;
    private CountryListManager _countryListManager;
    private string _vanillaPath;

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

        // 説明テキスト
        var descriptionText = new TextBlock
        {
            Text = "国家別の艦隊配備設定ができます",
            Foreground = Brushes.White,
            Margin = new Thickness(0, 0, 0, 20)
        };
        mainPanel.Children.Add(descriptionText);

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

        _refreshButton = new Button
        {
            Content = "更新",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 5, 0),
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0)
        };
        _refreshButton.Click += OnRefreshButtonClick;

        _exportButton = new Button
        {
            Content = "書き出し",
            Padding = new Thickness(10, 5, 10, 5),
            Margin = new Thickness(0, 0, 5, 0),
            Background = new SolidColorBrush(Color.Parse("#3E3E42")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            IsEnabled = false // 初期状態では無効
        };
        _exportButton.Click += OnExportButtonClick;

        // 検索ボックス
        _searchBox = new TextBox
        {
            Watermark = "国家検索...",
            Width = 200,
            Margin = new Thickness(10, 0, 0, 0),
            Background = new SolidColorBrush(Color.Parse("#252526")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46"))
        };
        _searchBox.TextChanged += OnSearchTextChanged;

        // 全国家表示チェックボックス
        _showAllCountriesCheckBox = new CheckBox
        {
            Content = "全国家表示",
            Margin = new Thickness(10, 0, 0, 0),
            Foreground = Brushes.White,
            IsChecked = false
        };
        _showAllCountriesCheckBox.IsCheckedChanged += OnShowAllCountriesChanged;

        toolbar.Children.Add(_refreshButton);
        toolbar.Children.Add(_exportButton);
        toolbar.Children.Add(_searchBox);
        toolbar.Children.Add(_showAllCountriesCheckBox);
        toolbarPanel.Children.Add(toolbar);

        mainPanel.Children.Add(toolbarPanel);

        // ロード中表示用プログレスバー
        _loadingProgressBar = new ProgressBar
        {
            IsIndeterminate = true,
            Height = 4,
            Margin = new Thickness(0, 1, 0, 1),
            IsVisible = false
        };
        mainPanel.Children.Add(_loadingProgressBar);

        // 国家リスト
        var listContainer = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
            Padding = new Thickness(5),
            Height = 400
        };

        _countriesListBox = new ListBox
        {
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            ItemsSource = _countriesList,
            SelectionMode = SelectionMode.Multiple
        };
        _countriesListBox.SelectionChanged += OnCountrySelectionChanged;

        // リストアイテムテンプレート
        _countriesListBox.ItemTemplate = new FuncDataTemplate<CountryListManager.CountryInfo>((item, scope) =>
        {
            if (item == null) return new TextBlock { Text = "データがありません", Foreground = Brushes.White };

            var panel = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                Margin = new Thickness(5)
            };

            // チェックボックス
            var checkBox = new CheckBox
            {
                IsChecked = item.IsSelected,
                Margin = new Thickness(0, 0, 10, 0),
                VerticalAlignment = VerticalAlignment.Center
            };
            checkBox.IsCheckedChanged += (s, e) => { item.IsSelected = checkBox.IsChecked ?? false; };
            Grid.SetColumn(checkBox, 0);

            // 国旗と国名
            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 0, 10, 0)
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

            if (item.FlagImage != null)
            {
                var flagImage = new Image
                {
                    Source = item.FlagImage,
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

            // 国名とタグ
            var infoStack = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center
            };

            var countryNameText = new TextBlock
            {
                Text = item.Name ?? "不明な国家",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold
            };

            var countryTagText = new TextBlock
            {
                Text = item.Tag,
                Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                FontSize = 12
            };

            infoStack.Children.Add(countryNameText);
            infoStack.Children.Add(countryTagText);

            contentPanel.Children.Add(flagBorder);
            contentPanel.Children.Add(infoStack);

            Grid.SetColumn(contentPanel, 1);

            // 配備ボタン
            var configButton = new Button
            {
                Content = "配備設定",
                Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.Parse("#1E90FF")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            configButton.Click += (s, e) => OnConfigCountryClick(item);
            Grid.SetColumn(configButton, 2);

            panel.Children.Add(checkBox);
            panel.Children.Add(contentPanel);
            panel.Children.Add(configButton);

            return panel;
        });

        listContainer.Child = _countriesListBox;
        mainPanel.Children.Add(listContainer);

        // ステータステキスト
        _statusTextBlock = new TextBlock
        {
            Text = "アクティブなMODを検出中...",
            Foreground = Brushes.White,
            Margin = new Thickness(5, 10, 0, 0)
        };
        mainPanel.Children.Add(_statusTextBlock);

        Grid.SetRow(mainPanel, 1);
        grid.Children.Add(headerPanel);
        grid.Children.Add(mainPanel);

        Content = grid;

        // 初期ロード
        LoadModConfigAndInitialize();
    }

    private void LoadModConfigAndInitialize()
    {
        try
        {
            // MOD設定の読み込み
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath);
                var config = JsonSerializer.Deserialize<ModConfig>(json);

                if (config != null)
                {
                    _vanillaPath = config.VanillaGamePath;
                    var activeMod = config.Mods?.FirstOrDefault(m => m.IsActive);

                    if (activeMod != null)
                    {
                        _activeMod = activeMod.Path;
                        _statusTextBlock.Text = $"アクティブMOD: {activeMod.Name}";
                    }
                    else
                    {
                        _statusTextBlock.Text = "アクティブなMODが設定されていません。";
                    }

                    // CountryListManagerを初期化
                    _countryListManager = new CountryListManager(_activeMod, _vanillaPath);

                    // 国家データのロード
                    LoadCountryData();
                }
            }
            else
            {
                _statusTextBlock.Text = "MOD設定ファイルが見つかりません。設定メニューで設定してください。";
            }
        }
        catch (Exception ex)
        {
            _statusTextBlock.Text = $"設定読み込みエラー: {ex.Message}";
        }
    }

    private async void LoadCountryData()
    {
        _loadingProgressBar.IsVisible = true;
        _refreshButton.IsEnabled = false;
        _countriesList.Clear();

        // UIスレッドをブロックしないために非同期で実行
        await Dispatcher.UIThread.InvokeAsync(async () =>
        {
            try
            {
                // MODパスのチェック - バニラパスがあれば国家データ読み込みを続行
                if (string.IsNullOrEmpty(_activeMod) && string.IsNullOrEmpty(_vanillaPath))
                {
                    _statusTextBlock.Text = "MODパスまたはバニラパスが設定されていません。";
                    _loadingProgressBar.IsVisible = false;
                    _refreshButton.IsEnabled = true;
                    return;
                }

                if (_countryListManager == null) _countryListManager = new CountryListManager(_activeMod, _vanillaPath);

                // 国家データを取得
                var showAllCountries = _showAllCountriesCheckBox.IsChecked ?? false;
                var countries = await _countryListManager.GetCountriesAsync(showAllCountries);

                // フィルタリング（検索テキストがある場合）
                var searchText = _searchBox.Text?.ToLower() ?? "";
                if (!string.IsNullOrWhiteSpace(searchText))
                    countries = countries.Where(c =>
                        (c.Name?.ToLower().Contains(searchText) ?? false) ||
                        c.Tag.ToLower().Contains(searchText)).ToList();

                // ObservableCollectionに追加
                foreach (var country in countries) _countriesList.Add(country);

                _statusTextBlock.Text = $"合計 {countries.Count} か国を検出しました。表示: {_countriesList.Count} か国";
            }
            catch (Exception ex)
            {
                _statusTextBlock.Text = $"国家データ読み込みエラー: {ex.Message}";
            }
            finally
            {
                _loadingProgressBar.IsVisible = false;
                _refreshButton.IsEnabled = true;
            }
        });
    }

    private void OnRefreshButtonClick(object sender, RoutedEventArgs e)
    {
        LoadModConfigAndInitialize();
    }

    private void OnExportButtonClick(object sender, RoutedEventArgs e)
    {
        // 選択された国家の配備設定をエクスポート
        var selectedCountries = _countriesList.Where(c => c.IsSelected).ToList();
        if (selectedCountries.Count > 0)
            _statusTextBlock.Text = $"{selectedCountries.Count} か国の配備設定をエクスポート中...";
        // ここにエクスポートロジックを実装
        else
            _statusTextBlock.Text = "エクスポートする国家が選択されていません。";
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var searchText = _searchBox.Text?.ToLower() ?? "";

        // 検索テキストが空の場合は全表示/主要国のみの設定に従う
        if (string.IsNullOrWhiteSpace(searchText))
        {
            OnShowAllCountriesChanged(null, null);
            return;
        }

        // 検索条件に一致する国家のみ表示するため、国家リストを再読み込み
        LoadCountryData();
    }

    private void OnShowAllCountriesChanged(object sender, RoutedEventArgs e)
    {
        // 国家リストの再読み込み
        LoadCountryData();
    }

    private void OnCountrySelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 選択された国家の数に基づいてエクスポートボタンの有効/無効を切り替え
        var selectedCount = _countriesList.Count(c => c.IsSelected);
        _exportButton.IsEnabled = selectedCount > 0;

        if (selectedCount > 0) _statusTextBlock.Text = $"{selectedCount} か国が選択されています";
    }

    private void OnConfigCountryClick(CountryListManager.CountryInfo country)
    {
        // 国家配備設定ダイアログを表示
        _statusTextBlock.Text = $"{country.Name ?? country.Tag} の配備設定を編集中...";
        Console.WriteLine($"配備設定: {country.Tag}");

        // 新たな艦隊配備マップビューを表示
        var mapView = new FleetDeploymentWindow(
            country.Tag,
            country.Name ?? country.Tag,
            country.FlagImage,
            _activeMod,
            _vanillaPath);

        // ダイアログとして表示
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel != null) mapView.ShowDialog((Window)topLevel);
    }
}