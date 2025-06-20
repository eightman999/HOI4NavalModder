using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using HOI4NavalModder.Core;
using HOI4NavalModder.Core.Models;
using HOI4NavalModder.Core.Services;
using HOI4NavalModder.Mapper;
using HOI4NavalModder.Window;

namespace HOI4NavalModder.View;

public partial class EquipmentDesignView : UserControl
{
    private readonly Dictionary<string, NavalCategory> _categories = new();

    // カテゴリと国家のリスト（フィルタ用）
    private readonly List<string> _countryList = new();
    private readonly DatabaseManager _dbManager;
    private readonly ObservableCollection<NavalEquipment> _equipmentList = new();


    // フィルタリングされたリスト（表示用）
    private readonly ObservableCollection<NavalEquipment> _filteredEquipmentList = new();
    private readonly Dictionary<int, string> _tierYears = new();
    private ComboBox _categoryFilterComboBox;
    private Button _clearFilterButton;
    private ComboBox _countryFilterComboBox;
    private ListBox _equipmentListBox;
    private TextBox _searchTextBox;
    private CheckBox _showAllCheckBox;

    public EquipmentDesignView()
    {
        // EditCommandの初期化
        EditCommand = new RelayCommand<NavalEquipment>(equipment => OpenCategorySpecificEditor(equipment));

        InitializeComponent();

        // コントロールの取得
        _equipmentListBox = this.FindControl<ListBox>("EquipmentListBox");

        // DataContextを自分自身に設定（コマンドバインディングのため）
        DataContext = this;

        if (_equipmentListBox == null)
        {
            Console.WriteLine("ListBoxが見つかりません。コード上で作成します。");
            CreateEquipmentListBoxProgrammatically();
        }

        // フィルタコントロールの初期化
        InitializeFilterControls();

        // データベースマネージャーの初期化
        _dbManager = new DatabaseManager();
        _dbManager.InitializeDatabase();

        // カテゴリの初期化
        InitializeCategories();

        // 開発年(ティア)の初期化
        InitializeTierYears();

        // データの読み込み
        LoadEquipmentData();

        // リストボックスの設定
        if (_equipmentListBox != null)
        {
            // フィルタリングされたリストをソースとして使用
            _equipmentListBox.ItemsSource = _filteredEquipmentList;
            _equipmentListBox.DoubleTapped += OnEquipmentDoubleTapped;
            Console.WriteLine("ListBoxのItemsSourceを設定しました");
        }
    }

    // 編集コマンドの追加
    public ICommand EditCommand { get; }

    // フィルタコントロールの初期化メソッド
    private void InitializeFilterControls()
    {
        // 各フィルタコントロールの取得
        _categoryFilterComboBox = this.FindControl<ComboBox>("CategoryFilterComboBox");
        _countryFilterComboBox = this.FindControl<ComboBox>("CountryFilterComboBox");
        _searchTextBox = this.FindControl<TextBox>("SearchTextBox");
        _clearFilterButton = this.FindControl<Button>("ClearFilterButton");
        _showAllCheckBox = this.FindControl<CheckBox>("ShowAllCheckBox");

        // イベントハンドラの設定
        if (_categoryFilterComboBox != null)
            _categoryFilterComboBox.SelectionChanged += FilterSelectionChanged;

        if (_countryFilterComboBox != null)
            _countryFilterComboBox.SelectionChanged += FilterSelectionChanged;

        if (_searchTextBox != null)
            _searchTextBox.TextChanged += SearchTextChanged;

        if (_clearFilterButton != null)
            _clearFilterButton.Click += ClearFilterClick;

        if (_showAllCheckBox != null)
            _showAllCheckBox.IsCheckedChanged += ShowAllCheckedChanged;

        // カテゴリフィルタの初期化
        if (_categoryFilterComboBox != null)
        {
            _categoryFilterComboBox.Items.Add(new CategoryFilterItem { Id = "", DisplayName = "すべてのカテゴリ" });

            foreach (var category in _categories)
                _categoryFilterComboBox.Items.Add(new CategoryFilterItem
                {
                    Id = category.Key,
                    DisplayName = $"{category.Value.Name} ({category.Key})"
                });

            // 最初の項目（すべて）を選択
            _categoryFilterComboBox.SelectedIndex = 0;
        }

        // 国家フィルタは装備データの読み込み後に初期化
    }

    // 国家フィルタの初期化（装備データの読み込み後に呼び出す）
    private void InitializeCountryFilter()
    {
        if (_countryFilterComboBox == null) return;

        // 現在の選択を保持
        var currentSelection = _countryFilterComboBox.SelectedItem;

        // リストをクリア
        _countryFilterComboBox.Items.Clear();
        _countryList.Clear();

        // 「すべての国家」オプションを追加
        _countryFilterComboBox.Items.Add(new CountryFilterItem { Id = "", DisplayName = "すべての国家" });

        // 装備リストから国家を収集
        foreach (var equipment in _equipmentList)
            if (!string.IsNullOrEmpty(equipment.Country) && !_countryList.Contains(equipment.Country))
                _countryList.Add(equipment.Country);

        // 国家リストをソート
        _countryList.Sort();

        // コンボボックスに追加
        foreach (var country in _countryList)
            _countryFilterComboBox.Items.Add(new CountryFilterItem { Id = country, DisplayName = country });

        // 以前の選択を復元するか、最初の項目を選択
        if (currentSelection != null && _countryFilterComboBox.Items.Contains(currentSelection))
            _countryFilterComboBox.SelectedItem = currentSelection;
        else
            _countryFilterComboBox.SelectedIndex = 0;
    }

    // フィルタ適用メソッド
    private void ApplyFilter()
    {
        // リストクリア
        _filteredEquipmentList.Clear();

        // フィルタ条件の取得
        var categoryFilter = "";
        var countryFilter = "";
        var searchText = "";
        var showAll = true;

        if (_categoryFilterComboBox?.SelectedItem is CategoryFilterItem categoryItem)
            categoryFilter = categoryItem.Id;

        if (_countryFilterComboBox?.SelectedItem is CountryFilterItem countryItem)
            countryFilter = countryItem.Id;

        if (_searchTextBox != null)
            searchText = _searchTextBox.Text?.ToLower() ?? "";

        if (_showAllCheckBox != null)
            showAll = _showAllCheckBox.IsChecked ?? true;

        // フィルタ適用
        var filteredList = _equipmentList.Where(equipment =>
        {
            // カテゴリフィルタ
            if (!string.IsNullOrEmpty(categoryFilter) && equipment.Category != categoryFilter)
                return false;

            // 国家フィルタ
            if (!string.IsNullOrEmpty(countryFilter) && equipment.Country != countryFilter)
                return false;

            // テキスト検索
            if (!string.IsNullOrEmpty(searchText))
            {
                var matchFound = equipment.Name?.ToLower().Contains(searchText) == true ||
                                 equipment.Id?.ToLower().Contains(searchText) == true;

                if (!matchFound)
                    return false;
            }

            return true;
        });

        // 結果をリストに追加
        foreach (var equipment in filteredList) _filteredEquipmentList.Add(equipment);

        // リストボックスのアイテムソースを更新
        if (_equipmentListBox != null) _equipmentListBox.ItemsSource = _filteredEquipmentList;

        Console.WriteLine($"フィルタ適用: {_filteredEquipmentList.Count}件の装備が表示されています");
    }

    // イベントハンドラ
    private void FilterSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void SearchTextChanged(object sender, TextChangedEventArgs e)
    {
        ApplyFilter();
    }

    private void ClearFilterClick(object sender, RoutedEventArgs e)
    {
        // フィルタをリセット
        if (_categoryFilterComboBox != null)
            _categoryFilterComboBox.SelectedIndex = 0;

        if (_countryFilterComboBox != null)
            _countryFilterComboBox.SelectedIndex = 0;

        if (_searchTextBox != null)
            _searchTextBox.Text = "";

        if (_showAllCheckBox != null)
            _showAllCheckBox.IsChecked = true;

        ApplyFilter();
    }

    private void ShowAllCheckedChanged(object sender, RoutedEventArgs e)
    {
        ApplyFilter();
    }

    private void CreateEquipmentListBoxProgrammatically()
    {
        // UIを取得
        var contentGrid = this.FindControl<Grid>("ContentGrid");
        if (contentGrid == null)
        {
            Console.WriteLine("ContentGridが見つかりません");
            return;
        }

        // リストボックスを作成
        _equipmentListBox = new ListBox
        {
            Name = "EquipmentListBox",
            Background = new SolidColorBrush(Color.Parse("#2D2D30")),
            Foreground = Brushes.White,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(5),
            SelectionMode = SelectionMode.Multiple
        };

        // リストボックスのテンプレートを設定
        _equipmentListBox.ItemTemplate = new FuncDataTemplate<NavalEquipment>((item, scope) =>
        {
            if (item == null) return new TextBlock { Text = "データがありません", Foreground = Brushes.White };

            var panel = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                Margin = new Thickness(5)
            };

            // 装備情報パネル
            var contentPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Margin = new Thickness(0, 0, 10, 0)
            };

            // 装備名と開発年を1行目に表示
            var nameYearPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5
            };

            var equipmentNameText = new TextBlock
            {
                Text = item.Name ?? "不明な装備",
                Foreground = Brushes.White,
                FontWeight = FontWeight.Bold,
                FontSize = 14
            };

            var yearText = new TextBlock
            {
                Text = $"：{item.Year}年",
                Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                FontSize = 14,
                VerticalAlignment = VerticalAlignment.Center
            };

            nameYearPanel.Children.Add(equipmentNameText);
            nameYearPanel.Children.Add(yearText);

            // IDと開発国を2行目に表示
            var idCountryPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 5,
                Margin = new Thickness(0, 5, 0, 0)
            };

            var idText = new TextBlock
            {
                Text = $"{item.Id}",
                Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                FontSize = 12
            };

            var countryText = new TextBlock
            {
                Text = $"：{item.Country}",
                Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                FontSize = 12
            };

            idCountryPanel.Children.Add(idText);
            idCountryPanel.Children.Add(countryText);

            contentPanel.Children.Add(nameYearPanel);
            contentPanel.Children.Add(idCountryPanel);

            // 編集ボタン
            var editButton = new Button
            {
                Content = "編集",
                Padding = new Thickness(8, 4, 8, 4),
                Background = new SolidColorBrush(Color.Parse("#1E90FF")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(editButton, 1);

            panel.Children.Add(contentPanel);
            Grid.SetColumn(contentPanel, 0);
            panel.Children.Add(editButton);

            return panel;
        });

        // リストボックスをグリッドに追加
        var mainContentGrid = contentGrid.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetColumn(g) == 1);
        if (mainContentGrid != null)
        {
            var row1Grid = mainContentGrid.Children.OfType<Grid>().FirstOrDefault(g => Grid.GetRow(g) == 1);
            if (row1Grid != null)
            {
                row1Grid.Children.Add(_equipmentListBox);
                Grid.SetRow(_equipmentListBox, 0);
                Console.WriteLine("ListBoxをUIに追加しました");
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void InitializeCategories()
    {
        // カテゴリの定義
        _categories.Add("SMLG", new NavalCategory { Id = "SMLG", Name = "小口径砲" });
        _categories.Add("SMMG", new NavalCategory { Id = "SMMG", Name = "中口径砲" });
        _categories.Add("SMHG", new NavalCategory { Id = "SMHG", Name = "大口径砲" });
        _categories.Add("SMSHG", new NavalCategory { Id = "SMSHG", Name = "超大口径砲" });
        _categories.Add("SMTP", new NavalCategory { Id = "SMTP", Name = "魚雷" });
        _categories.Add("SMSTP", new NavalCategory { Id = "SMSTP", Name = "潜水艦魚雷" });
        _categories.Add("SMSP", new NavalCategory { Id = "SMSP", Name = "水上機" });
        _categories.Add("SMCR", new NavalCategory { Id = "SMCR", Name = "艦上偵察機" });
        _categories.Add("SMHC", new NavalCategory { Id = "SMHC", Name = "回転翼機" });
        _categories.Add("SMASP", new NavalCategory { Id = "SMASP", Name = "対潜哨戒機" });
        _categories.Add("SMLSP", new NavalCategory { Id = "SMLSP", Name = "大型飛行艇" });
        _categories.Add("SMDCL", new NavalCategory { Id = "SMDCL", Name = "爆雷投射機" });
        _categories.Add("SMSO", new NavalCategory { Id = "SMSO", Name = "ソナー" });
        _categories.Add("SMLSO", new NavalCategory { Id = "SMLSO", Name = "大型ソナー" });
        _categories.Add("SMDC", new NavalCategory { Id = "SMDC", Name = "爆雷" });
        _categories.Add("SMLR", new NavalCategory { Id = "SMLR", Name = "小型電探" });
        _categories.Add("SMHR", new NavalCategory { Id = "SMHR", Name = "大型電探" });
        _categories.Add("SMAA", new NavalCategory { Id = "SMAA", Name = "対空砲" });
        _categories.Add("SMTR", new NavalCategory { Id = "SMTR", Name = "機関" });
        _categories.Add("SMMBL", new NavalCategory { Id = "SMMBL", Name = "増設バルジ(中型艦)" });
        _categories.Add("SMHBL", new NavalCategory { Id = "SMHBL", Name = "増設バルジ(大型艦)" });
        _categories.Add("SMHAA", new NavalCategory { Id = "SMHAA", Name = "高射装置" });
        _categories.Add("SMOT", new NavalCategory { Id = "SMOT", Name = "その他" });
        _categories.Add("SMASM", new NavalCategory { Id = "SMASM", Name = "対艦ミサイル" });
        _categories.Add("SMSAM", new NavalCategory { Id = "SMSAM", Name = "対空ミサイル" });
        _categories.Add("SMHNG", new NavalCategory { Id = "SMHNG", Name = "格納庫" });
    }

    private void InitializeTierYears()
    {
        // ティア(開発年)の定義
        _tierYears.Add(0, "1890以前");
        _tierYears.Add(1, "1890");
        _tierYears.Add(2, "1895");
        _tierYears.Add(3, "1900");
        _tierYears.Add(4, "1905");
        _tierYears.Add(5, "1910");
        _tierYears.Add(6, "1915");
        _tierYears.Add(7, "1920");
        _tierYears.Add(8, "1925");
        _tierYears.Add(9, "1930");
        _tierYears.Add(10, "1935");
        _tierYears.Add(11, "1940");
        _tierYears.Add(12, "1945");
        _tierYears.Add(13, "1950");
        _tierYears.Add(14, "1955");
        _tierYears.Add(15, "1960");
        _tierYears.Add(16, "1965");
        _tierYears.Add(17, "1970");
        _tierYears.Add(18, "1975");
        _tierYears.Add(19, "1980");
        _tierYears.Add(20, "1985");
        _tierYears.Add(21, "1990");
        _tierYears.Add(22, "1995");
        _tierYears.Add(23, "2000");
    }


    private void LoadEquipmentData()
    {
        try
        {
            // 既存のリストをクリア
            _equipmentList.Clear();
            _filteredEquipmentList.Clear();

            // データベースから基本情報のみ取得（JSONファイル含む）
            var equipmentList = _dbManager.GetBasicEquipmentInfo();

            Console.WriteLine($"{equipmentList.Count}件の装備データを読み込みました");

            // 取得したデータをリストに追加
            foreach (var equipment in equipmentList)
            {
                Console.WriteLine(
                    $"装備追加: ID={equipment.Id}, Name={equipment.Name}, Year={equipment.Year}, Country={equipment.Country}");
                _equipmentList.Add(equipment);
                _filteredEquipmentList.Add(equipment); // フィルタリスト初期化
            }

            Console.WriteLine($"リスト内の装備数: {_equipmentList.Count}件");

            // 国家フィルタの初期化
            InitializeCountryFilter();

            // リストボックスのアイテムソースを更新（フィルタリングされたリストを使用）
            if (_equipmentListBox != null) _equipmentListBox.ItemsSource = _filteredEquipmentList;

            // 初期フィルタ適用
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"装備データの読み込み中にエラーが発生しました: {ex.Message}");
            Console.WriteLine($"スタックトレース: {ex.StackTrace}");
            // エラー時は空のリストを使用
            _equipmentList.Clear();
            _filteredEquipmentList.Clear();
        }
    }

    public void OnEditButtonClick(object sender, RoutedEventArgs e)
    {
        // Button の DataContext から NavalEquipment を取得
        var button = sender as Button;
        if (button?.DataContext is NavalEquipment selectedEquipment)
            OpenCategorySpecificEditor(selectedEquipment);
        // DataContext が取得できない場合はリストボックスの選択アイテムを使用
        else if (_equipmentListBox.SelectedItem is NavalEquipment equipment) OpenCategorySpecificEditor(equipment);
    }

    // データベースに装備データを保存するメソッド
    private void SaveEquipmentData(NavalEquipment equipment)
    {
        try
        {
            // NavalEquipmentをModuleDataに変換
            var moduleData = ConvertToModuleData(equipment);

            // データベースに保存
            _dbManager.SaveModuleData(
                moduleData.Info,
                moduleData.AddStats,
                moduleData.MultiplyStats,
                moduleData.AddAverageStats,
                moduleData.Resources,
                moduleData.ConvertModules
            );

            Console.WriteLine($"装備 {equipment.Id} をデータベースに保存しました");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"装備データの保存中にエラーが発生しました: {ex.Message}");
        }
    }

    // NavalEquipmentをModuleDataに変換するヘルパーメソッド
    private ModuleData ConvertToModuleData(NavalEquipment equipment)
    {
        var moduleData = new ModuleData();

        // 基本情報
        moduleData.Info = new ModuleInfo
        {
            Id = equipment.Id,
            Name = equipment.Name,
            Gfx = equipment.AdditionalProperties.ContainsKey("Gfx")
                ? equipment.AdditionalProperties["Gfx"].ToString()
                : $"gfx_{equipment.Category.ToLower()}_{equipment.Id.ToLower()}",
            Sfx = equipment.AdditionalProperties.ContainsKey("Sfx")
                ? equipment.AdditionalProperties["Sfx"].ToString()
                : "",
            Year = equipment.Year,
            Manpower = equipment.AdditionalProperties.ContainsKey("Manpower")
                ? Convert.ToInt32(equipment.AdditionalProperties["Manpower"])
                : 0,
            Country = equipment.Country,
            CriticalParts = equipment.SpecialAbility
        };

        // 加算ステータス
        moduleData.AddStats = new ModuleStats();
        if (equipment.AdditionalProperties.ContainsKey("BuildCostIc"))
            moduleData.AddStats.BuildCostIc = Convert.ToDouble(equipment.AdditionalProperties["BuildCostIc"]);
        if (equipment.AdditionalProperties.ContainsKey("NavalSpeed"))
            moduleData.AddStats.NavalSpeed = Convert.ToDouble(equipment.AdditionalProperties["NavalSpeed"]);
        if (equipment.AdditionalProperties.ContainsKey("FireRange"))
            moduleData.AddStats.FireRange = Convert.ToDouble(equipment.AdditionalProperties["FireRange"]);
        if (equipment.AdditionalProperties.ContainsKey("LgArmorPiercing"))
            moduleData.AddStats.LgArmorPiercing =
                Convert.ToDouble(equipment.AdditionalProperties["LgArmorPiercing"]);
        if (equipment.AdditionalProperties.ContainsKey("LgAttack"))
            moduleData.AddStats.LgAttack = Convert.ToDouble(equipment.AdditionalProperties["LgAttack"]);
        if (equipment.AdditionalProperties.ContainsKey("HgArmorPiercing"))
            moduleData.AddStats.HgArmorPiercing =
                Convert.ToDouble(equipment.AdditionalProperties["HgArmorPiercing"]);
        if (equipment.AdditionalProperties.ContainsKey("HgAttack"))
            moduleData.AddStats.HgAttack = Convert.ToDouble(equipment.AdditionalProperties["HgAttack"]);
        if (equipment.AdditionalProperties.ContainsKey("TorpedoAttack"))
            moduleData.AddStats.TorpedoAttack = Convert.ToDouble(equipment.AdditionalProperties["TorpedoAttack"]);
        if (equipment.AdditionalProperties.ContainsKey("AntiAirAttack"))
            moduleData.AddStats.AntiAirAttack = Convert.ToDouble(equipment.AdditionalProperties["AntiAirAttack"]);
        if (equipment.AdditionalProperties.ContainsKey("ShoreBombardment"))
            moduleData.AddStats.ShoreBombardment =
                Convert.ToDouble(equipment.AdditionalProperties["ShoreBombardment"]);

        // 乗算ステータスと平均加算ステータスは新規作成時は空のオブジェクトを使用
        moduleData.MultiplyStats = new ModuleStats();
        moduleData.AddAverageStats = new ModuleStats();

        // リソース
        moduleData.Resources = new ModuleResources();
        if (equipment.AdditionalProperties.ContainsKey("Steel"))
            moduleData.Resources.Steel = Convert.ToInt32(equipment.AdditionalProperties["Steel"]);
        if (equipment.AdditionalProperties.ContainsKey("Chromium"))
            moduleData.Resources.Chromium = Convert.ToInt32(equipment.AdditionalProperties["Chromium"]);
        if (equipment.AdditionalProperties.ContainsKey("Tungsten"))
            moduleData.Resources.Tungsten = Convert.ToInt32(equipment.AdditionalProperties["Tungsten"]);
        if (equipment.AdditionalProperties.ContainsKey("Oil"))
            moduleData.Resources.Oil = Convert.ToInt32(equipment.AdditionalProperties["Oil"]);
        if (equipment.AdditionalProperties.ContainsKey("Aluminium"))
            moduleData.Resources.Aluminium = Convert.ToInt32(equipment.AdditionalProperties["Aluminium"]);
        if (equipment.AdditionalProperties.ContainsKey("Rubber"))
            moduleData.Resources.Rubber = Convert.ToInt32(equipment.AdditionalProperties["Rubber"]);

        // 変換モジュール情報は新規作成時は空のリストを使用
        moduleData.ConvertModules = new List<ModuleConvert>();

        return moduleData;
    }

    // ボタンイベントハンドラ
    public void OnNewEquipmentClick(object sender, RoutedEventArgs e)
    {
        ShowCategorySelectionWindow();
    }

    public void OnEditEquipmentClick(object sender, RoutedEventArgs e)
    {
        if (_equipmentListBox.SelectedItem is NavalEquipment selectedEquipment)
            OpenCategorySpecificEditor(selectedEquipment);
    }

    public void OnDeleteEquipmentClick(object sender, RoutedEventArgs e)
    {
        if (_equipmentListBox.SelectedItem is NavalEquipment selectedEquipment)
        {
            // ToDo: 確認ダイアログを表示

            // データベースから削除
            var success = _dbManager.DeleteModule(selectedEquipment.Id);

            if (success)
            {
                // UI上のリストからも削除
                _equipmentList.Remove(selectedEquipment);
                Console.WriteLine($"装備 {selectedEquipment.Id} を削除しました");
            }
        }
    }

    public void OnDuplicateEquipmentClick(object sender, RoutedEventArgs e)
    {
        if (_equipmentListBox.SelectedItem is NavalEquipment selectedEquipment)
        {
            // 装備を複製
            var newEquipment = new NavalEquipment
            {
                Id = selectedEquipment.Id + "_copy",
                Name = selectedEquipment.Name + " (コピー)",
                Category = selectedEquipment.Category,
                SubCategory = selectedEquipment.SubCategory,
                Year = selectedEquipment.Year,
                Tier = selectedEquipment.Tier,
                Country = selectedEquipment.Country,
                Attack = selectedEquipment.Attack,
                Defense = selectedEquipment.Defense,
                SpecialAbility = selectedEquipment.SpecialAbility,
                AdditionalProperties = new Dictionary<string, object>(selectedEquipment.AdditionalProperties)
            };

            // データベースに保存
            SaveEquipmentData(newEquipment);

            // UIのリストに追加
            _equipmentList.Add(newEquipment);

            // 新しい装備を選択
            _equipmentListBox.SelectedItem = newEquipment;
        }
    }

    public void OnExportClick(object sender, RoutedEventArgs e)
    {
        // ToDo: エクスポート機能の実装
        Console.WriteLine("エクスポート機能は未実装です");
    }

    public void OnImportClick(object sender, RoutedEventArgs e)
    {
        // ToDo: インポート機能の実装
        Console.WriteLine("インポート機能は未実装です");
    }

    private void OnEquipmentDoubleTapped(object sender, RoutedEventArgs e)
    {
        if (_equipmentListBox.SelectedItem is NavalEquipment selectedEquipment)
            OpenCategorySpecificEditor(selectedEquipment);
    }

    private async void ShowCategorySelectionWindow()
    {
        // カテゴリ選択ウィンドウを表示
        var categoryWindow = new CategorySelectionWindow(_categories, _tierYears);

        // 結果の処理
        var result =
            await categoryWindow.ShowDialog<CategorySelectionResult>(this.GetVisualRoot() as Avalonia.Controls.Window);
        if (result != null)
        {
            // 選択されたカテゴリとティアに基づいて、適切なエディタを開く
            var newEquipment = new NavalEquipment
            {
                Category = result.CategoryId,
                Tier = result.TierId,
                Year = GetYearFromTierId(result.TierId),
                AdditionalProperties = new Dictionary<string, object>()
            };

            OpenCategorySpecificEditor(newEquipment);
        }
    }

    private int GetYearFromTierId(int tierId)
    {
        if (_tierYears.TryGetValue(tierId, out var yearStr))
        {
            if (yearStr.EndsWith("以前")) return 1889; // 1890以前の場合

            if (int.TryParse(yearStr, out var year)) return year;
        }

        return 1900; // デフォルト値
    }

    private async void OpenCategorySpecificEditor(NavalEquipment equipment)
    {
        Avalonia.Controls.Window editorWindow = null;

        // カテゴリに応じた適切なエディタを選択
        switch (equipment.Category)
        {
            case "SMLG": // 小口径砲
            case "SMMG": // 中口径砲
            case "SMHG": // 大口径砲
            case "SMSHG": // 超大口径砲
                Dictionary<string, object> rawGunData = null;

                // 既存の装備の場合は生データを取得
                if (!string.IsNullOrEmpty(equipment.Id))
                {
                    // 装備がJSONファイルからのデータかチェック
                    if (equipment.AdditionalProperties.ContainsKey("FilePath") &&
                        File.Exists(equipment.AdditionalProperties["FilePath"].ToString()))
                    {
                        // JSONファイルから直接データを読み込む
                        var jsonFilePath = equipment.AdditionalProperties["FilePath"].ToString();
                        var jsonContent = File.ReadAllText(jsonFilePath);
                        rawGunData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            jsonContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    else
                    {
                        // データベースから生データを取得
                        rawGunData = GunDataToDb.GetRawGunData(equipment.Id);
                    }
                }

                // GunDesignViewを開く（生データがある場合はそれを使用）
                if (rawGunData != null)
                    editorWindow = new GunDesignView(rawGunData, _categories, _tierYears);
                else
                    editorWindow = new GunDesignView(equipment, _categories, _tierYears);
                break;

            case "SMTP": // 魚雷
            case "SMSTP": // 潜水艦魚雷
                Dictionary<string, object> rawTorpedoData = null;

                // 既存の装備の場合は生データを取得
                if (!string.IsNullOrEmpty(equipment.Id))
                {
                    // 装備がJSONファイルからのデータかチェック
                    if (equipment.AdditionalProperties.ContainsKey("FilePath") &&
                        File.Exists(equipment.AdditionalProperties["FilePath"].ToString()))
                    {
                        // JSONファイルから直接データを読み込む
                        var jsonFilePath = equipment.AdditionalProperties["FilePath"].ToString();
                        var jsonContent = File.ReadAllText(jsonFilePath);
                        rawTorpedoData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            jsonContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    else
                    {
                        // データベースから生データを取得
                        rawTorpedoData = GunDataToDb.GetRawGunData(equipment.Id);
                    }
                }

                // Torpedo_Design_Viewを開く（生データがある場合はそれを使用）
                if (rawTorpedoData != null)
                    editorWindow = new Torpedo_Design_View(rawTorpedoData, _categories, _tierYears);
                else
                    editorWindow = new Torpedo_Design_View(equipment, _categories, _tierYears);
                break;
            case "SMSP": // 水上機
            case "SMCR": // 艦上偵察機
            case "SMHC": // 回転翼機
            case "SMASP": // 対潜哨戒機
            case "SMLSP": // 大型飛行艇
                //editorWindow = new SMSP_Design_View(equipment, _categories, _tierYears);
                break;

            case "SMSO": // ソナー
            case "SMLSO": // 大型ソナー
                Dictionary<string, object> rawSonarData = null;

                // 既存の装備の場合は生データを取得
                if (!string.IsNullOrEmpty(equipment.Id))
                {
                    // 装備がJSONファイルからのデータかチェック
                    if (equipment.AdditionalProperties.ContainsKey("FilePath") &&
                        File.Exists(equipment.AdditionalProperties["FilePath"].ToString()))
                    {
                        // JSONファイルから直接データを読み込む
                        var jsonFilePath = equipment.AdditionalProperties["FilePath"].ToString();
                        var jsonContent = File.ReadAllText(jsonFilePath);
                        rawSonarData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            jsonContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    else
                    {
                        // データベースから生データを取得
                        rawSonarData = SonarDataToDb.GetRawSonarData(equipment.Id);
                    }
                }

                // Sonar_Design_Viewを開く（生データがある場合はそれを使用）
                if (rawSonarData != null)
                    editorWindow = new Sonar_Design_View(rawSonarData, _categories, _tierYears);
                else
                    editorWindow = new Sonar_Design_View(equipment, _categories, _tierYears);
                break;

            case "SMDC": // 爆雷
            case "SMDCL": // 爆雷投射機
                Dictionary<string, object> rawDCData = null;

                // 既存の装備の場合は生データを取得
                if (!string.IsNullOrEmpty(equipment.Id))
                {
                    // 装備がJSONファイルからのデータかチェック
                    if (equipment.AdditionalProperties.ContainsKey("FilePath") &&
                        File.Exists(equipment.AdditionalProperties["FilePath"].ToString()))
                    {
                        // JSONファイルから直接データを読み込む
                        var jsonFilePath = equipment.AdditionalProperties["FilePath"].ToString();
                        var jsonContent = File.ReadAllText(jsonFilePath);
                        rawDCData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            jsonContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    else
                    {
                        // データベースから生データを取得
                        rawDCData = DCDataToDb.GetRawDCData(equipment.Id);
                    }
                }

                // DC_Design_Viewを開く（生データがある場合はそれを使用）
                if (rawDCData != null)
                    editorWindow = new DC_Design_View(rawDCData, _categories, _tierYears);
                else
                    editorWindow = new DC_Design_View(equipment, _categories, _tierYears);
                break;
            case "SMLR": // 小型電探
            case "SMHR": // 大型電探
                Dictionary<string, object> rawRadarData = null;

                // 既存の装備の場合は生データを取得
                if (!string.IsNullOrEmpty(equipment.Id))
                {
                    // 装備がJSONファイルからのデータかチェック
                    if (equipment.AdditionalProperties.ContainsKey("FilePath") &&
                        File.Exists(equipment.AdditionalProperties["FilePath"].ToString()))
                    {
                        // JSONファイルから直接データを読み込む
                        var jsonFilePath = equipment.AdditionalProperties["FilePath"].ToString();
                        var jsonContent = File.ReadAllText(jsonFilePath);
                        rawRadarData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            jsonContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    else
                    {
                        // データベースから生データを取得
                        rawRadarData = RadarDataToDb.GetRawRadarData(equipment.Id);
                    }
                }

                // Radar_Design_Viewを開く（生データがある場合はそれを使用）
                if (rawRadarData != null)
                    editorWindow = new Radar_Design_View(rawRadarData, _categories, _tierYears);
                else
                    editorWindow = new Radar_Design_View(equipment, _categories, _tierYears);
                break;
            // 追加: 対空砲と高射装置
            case "SMAA": // 対空砲
            case "SMHAA": // 高射装置
                Dictionary<string, object> rawAAData = null;

                // 既存の装備の場合は生データを取得
                if (!string.IsNullOrEmpty(equipment.Id))
                {
                    // 装備がJSONファイルからのデータかチェック
                    if (equipment.AdditionalProperties.ContainsKey("FilePath") &&
                        File.Exists(equipment.AdditionalProperties["FilePath"].ToString()))
                    {
                        // JSONファイルから直接データを読み込む
                        var jsonFilePath = equipment.AdditionalProperties["FilePath"].ToString();
                        var jsonContent = File.ReadAllText(jsonFilePath);
                        rawAAData = JsonSerializer.Deserialize<Dictionary<string, object>>(
                            jsonContent,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                        );
                    }
                    else
                    {
                        // データベースから生データを取得
                        rawAAData = AAGunDataToDb.GetRawAAGunData(equipment.Id);
                    }
                }

                // AADesignViewを開く（生データがある場合はそれを使用）
                if (rawAAData != null)
                    editorWindow = new AADesignView(rawAAData, _categories, _tierYears);
                else
                    editorWindow = new AADesignView(equipment, _categories, _tierYears);
                break;
            case "SMASM": // 対艦ミサイル
            case "SMSAM": // 対空ミサイル
                //editorWindow = new SMASM_Design_View(equipment, _categories, _tierYears);
                break;
            case "SMHNG": // 格納庫
                //editorWindow = new SMHNG_Design_View(equipment, _categories, _tierYears);
                break;
        }

        // エディタウィンドウを表示
        if (editorWindow != null)
        {
            var result =
                await editorWindow.ShowDialog<NavalEquipment>(this.GetVisualRoot() as Avalonia.Controls.Window);
            if (result != null)
            {
                // 既存の装備を編集した場合
                if (_equipmentList.Any(e => e.Id == result.Id))
                {
                    // リストの中の該当する装備を更新
                    var index = _equipmentList.IndexOf(_equipmentList.First(e => e.Id == result.Id));
                    if (index >= 0) _equipmentList[index] = result;
                }
                else // 新規装備の場合
                {
                    _equipmentList.Add(result);
                }
            }
        }
    }

    public class CategoryFilterItem
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }

    public class CountryFilterItem
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}