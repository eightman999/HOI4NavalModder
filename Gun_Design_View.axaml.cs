using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace HOI4NavalModder;

public class GunDesignView : Window
{
    private readonly ComboBox _barrelCountComboBox;
    private readonly TextBlock _calculatedArmorPiercingText;

    // 表示用の性能値テキスト
    private readonly TextBlock _calculatedAttackText;
    private readonly TextBlock _calculatedBuildCostText;
    private readonly TextBlock _calculatedRangeText;
    private readonly NumericUpDown _calibreNumeric;
    private readonly ComboBox _calibreTypeComboBox;
    private readonly Dictionary<string, NavalCategory> _categories;
    private readonly ComboBox _categoryComboBox;
    private readonly NumericUpDown _chromiumNumeric;
    private readonly ComboBox _countryComboBox;
    private readonly NumericUpDown _elevationAngleNumeric;
    private readonly TextBox _idTextBox;
    private readonly NumericUpDown _manpowerNumeric;
    private readonly NumericUpDown _muzzleVelocityNumeric;
    private readonly TextBox _nameTextBox;

    private readonly NavalEquipment _originalEquipment;
    private readonly NumericUpDown _rpmNumeric;

    // 砲特有のフィールド
    private readonly NumericUpDown _shellWeightNumeric;

    // リソース
    private readonly NumericUpDown _steelNumeric;
    private readonly ComboBox _subCategoryComboBox;
    private readonly Dictionary<int, string> _tierYears;
    private readonly NumericUpDown _turretWeightNumeric;
    private readonly ComboBox _yearComboBox;

    public GunDesignView()
    {
        InitializeComponent();
    }

    public GunDesignView(NavalEquipment equipment, Dictionary<string, NavalCategory> categories,
        Dictionary<int, string> tierYears)
    {
        InitializeComponent();

        _originalEquipment = equipment;
        _categories = categories;
        _tierYears = tierYears;

        // UI要素の取得
        _idTextBox = this.FindControl<TextBox>("IdTextBox");
        _nameTextBox = this.FindControl<TextBox>("NameTextBox");
        _categoryComboBox = this.FindControl<ComboBox>("CategoryComboBox");
        _subCategoryComboBox = this.FindControl<ComboBox>("SubCategoryComboBox");
        _yearComboBox = this.FindControl<ComboBox>("YearComboBox");
        _countryComboBox = this.FindControl<ComboBox>("CountryComboBox");

        _shellWeightNumeric = this.FindControl<NumericUpDown>("ShellWeightNumeric");
        _muzzleVelocityNumeric = this.FindControl<NumericUpDown>("MuzzleVelocityNumeric");
        _rpmNumeric = this.FindControl<NumericUpDown>("RPMNumeric");
        _calibreNumeric = this.FindControl<NumericUpDown>("CalibreNumeric");
        _calibreTypeComboBox = this.FindControl<ComboBox>("CalibreTypeComboBox");
        _barrelCountComboBox = this.FindControl<ComboBox>("BarrelCountComboBox");
        _elevationAngleNumeric = this.FindControl<NumericUpDown>("ElevationAngleNumeric");
        _turretWeightNumeric = this.FindControl<NumericUpDown>("TurretWeightNumeric");
        _manpowerNumeric = this.FindControl<NumericUpDown>("ManpowerNumeric");

        _steelNumeric = this.FindControl<NumericUpDown>("SteelNumeric");
        _chromiumNumeric = this.FindControl<NumericUpDown>("ChromiumNumeric");

        _calculatedAttackText = this.FindControl<TextBlock>("CalculatedAttackText");
        _calculatedRangeText = this.FindControl<TextBlock>("CalculatedRangeText");
        _calculatedArmorPiercingText = this.FindControl<TextBlock>("CalculatedArmorPiercingText");
        _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");

        // カテゴリの設定（砲関連のもののみフィルタリング）
        var filteredCategories = new Dictionary<string, NavalCategory>();
        if (_categories.ContainsKey("SMLG")) filteredCategories.Add("SMLG", _categories["SMLG"]);
        if (_categories.ContainsKey("SMMG")) filteredCategories.Add("SMMG", _categories["SMMG"]);
        if (_categories.ContainsKey("SMHG")) filteredCategories.Add("SMHG", _categories["SMHG"]);
        if (_categories.ContainsKey("SMSHG")) filteredCategories.Add("SMSHG", _categories["SMSHG"]);

        foreach (var category in filteredCategories)
            _categoryComboBox.Items.Add(new NavalCategoryItem
                { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

        // サブカテゴリの設定
        _subCategoryComboBox.Items.Add("単装砲");
        _subCategoryComboBox.Items.Add("連装砲");
        _subCategoryComboBox.Items.Add("三連装砲");
        _subCategoryComboBox.Items.Add("四連装砲");

        // 開発年の設定
        foreach (var year in _tierYears)
            _yearComboBox.Items.Add(new NavalYearItem { Tier = year.Key, Year = year.Value });

        // 開発国の設定
        string[] countries = { "日本", "アメリカ", "イギリス", "ドイツ", "ソ連", "イタリア", "フランス", "その他" };
        foreach (var country in countries) _countryComboBox.Items.Add(country);

        // 口径種類の設定
        _calibreTypeComboBox.Items.Add("cm");
        _calibreTypeComboBox.Items.Add("inch");
        _calibreTypeComboBox.Items.Add("mm");
        _calibreTypeComboBox.SelectedIndex = 0; // cmをデフォルトに

        // 砲身数の設定
        _barrelCountComboBox.Items.Add("1");
        _barrelCountComboBox.Items.Add("2");
        _barrelCountComboBox.Items.Add("3");
        _barrelCountComboBox.Items.Add("4");

        // 既存の装備を編集する場合
        if (_originalEquipment != null && !string.IsNullOrEmpty(_originalEquipment.Category))
        {
            // ウィンドウタイトルをカテゴリに合わせて設定
            var categoryName = GetCategoryDisplayName(_originalEquipment.Category);
            Title = $"{categoryName}の編集";

            // 基本値の設定
            _idTextBox.Text = _originalEquipment.Id;
            _nameTextBox.Text = _originalEquipment.Name;

            // カテゴリの選択
            for (var i = 0; i < _categoryComboBox.Items.Count; i++)
            {
                var item = _categoryComboBox.Items[i] as NavalCategoryItem;
                if (item != null && item.Id == _originalEquipment.Category)
                {
                    _categoryComboBox.SelectedIndex = i;
                    break;
                }
            }

            // サブカテゴリの選択
            if (!string.IsNullOrEmpty(_originalEquipment.SubCategory))
            {
                var subCategoryIndex = _subCategoryComboBox.Items.IndexOf(_originalEquipment.SubCategory);
                if (subCategoryIndex >= 0) _subCategoryComboBox.SelectedIndex = subCategoryIndex;
            }

            // 開発年の選択
            for (var i = 0; i < _yearComboBox.Items.Count; i++)
            {
                var item = _yearComboBox.Items[i] as NavalYearItem;
                if (item != null && item.Tier == _originalEquipment.Tier)
                {
                    _yearComboBox.SelectedIndex = i;
                    break;
                }
            }

            // 開発国の選択
            if (_originalEquipment.AdditionalProperties.ContainsKey("Country"))
            {
                var country = _originalEquipment.AdditionalProperties["Country"].ToString();
                var countryIndex = _countryComboBox.Items.IndexOf(country);
                if (countryIndex >= 0) _countryComboBox.SelectedIndex = countryIndex;
            }

            // 砲の詳細パラメータを設定
            if (_originalEquipment.AdditionalProperties.ContainsKey("ShellWeight"))
                _shellWeightNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["ShellWeight"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("MuzzleVelocity"))
                _muzzleVelocityNumeric.Value =
                    Convert.ToDecimal(_originalEquipment.AdditionalProperties["MuzzleVelocity"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("RPM"))
                _rpmNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["RPM"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("Calibre"))
                _calibreNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Calibre"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("CalibreType"))
            {
                var calibreType = _originalEquipment.AdditionalProperties["CalibreType"].ToString();
                var calibreTypeIndex = _calibreTypeComboBox.Items.IndexOf(calibreType);
                if (calibreTypeIndex >= 0) _calibreTypeComboBox.SelectedIndex = calibreTypeIndex;
            }

            if (_originalEquipment.AdditionalProperties.ContainsKey("BarrelCount"))
            {
                var barrelCount = _originalEquipment.AdditionalProperties["BarrelCount"].ToString();
                var barrelCountIndex = _barrelCountComboBox.Items.IndexOf(barrelCount);
                if (barrelCountIndex >= 0) _barrelCountComboBox.SelectedIndex = barrelCountIndex;
            }

            if (_originalEquipment.AdditionalProperties.ContainsKey("ElevationAngle"))
                _elevationAngleNumeric.Value =
                    Convert.ToDecimal(_originalEquipment.AdditionalProperties["ElevationAngle"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("TurretWeight"))
                _turretWeightNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["TurretWeight"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("Manpower"))
                _manpowerNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Manpower"]);

            // リソース
            if (_originalEquipment.AdditionalProperties.ContainsKey("Steel"))
                _steelNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Steel"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("Chromium"))
                _chromiumNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Chromium"]);

            // 既存の計算値がある場合は表示
            if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedAttack"))
                _calculatedAttackText.Text = _originalEquipment.AdditionalProperties["CalculatedAttack"].ToString();

            if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedRange"))
                _calculatedRangeText.Text = _originalEquipment.AdditionalProperties["CalculatedRange"] + " km";

            if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedArmorPiercing"))
                _calculatedArmorPiercingText.Text =
                    _originalEquipment.AdditionalProperties["CalculatedArmorPiercing"].ToString();

            if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedBuildCost"))
                _calculatedBuildCostText.Text =
                    _originalEquipment.AdditionalProperties["CalculatedBuildCost"].ToString();
        }
        else
        {
            // 新規作成の場合
            if (!string.IsNullOrEmpty(_originalEquipment.Category))
            {
                // カテゴリが既に選択されている場合
                var categoryName = GetCategoryDisplayName(_originalEquipment.Category);
                Title = $"新規{categoryName}の作成";

                // カテゴリの選択
                for (var i = 0; i < _categoryComboBox.Items.Count; i++)
                {
                    var item = _categoryComboBox.Items[i] as NavalCategoryItem;
                    if (item != null && item.Id == _originalEquipment.Category)
                    {
                        _categoryComboBox.SelectedIndex = i;
                        break;
                    }
                }

                // カテゴリに応じたデフォルトのIDプレフィックスを設定
                _idTextBox.Text = _originalEquipment.Category.ToLower() + "_";
            }
            else
            {
                // カテゴリが選択されていない場合
                Title = "新規砲の作成";
                if (_categoryComboBox.Items.Count > 0)
                {
                    _categoryComboBox.SelectedIndex = 0;
                    var selectedCategory = _categoryComboBox.SelectedItem as NavalCategoryItem;
                    if (selectedCategory != null) _idTextBox.Text = selectedCategory.Id.ToLower() + "_";
                }
            }

            // デフォルト値の設定
            _subCategoryComboBox.SelectedIndex = 0; // 単装砲をデフォルトに
            _barrelCountComboBox.SelectedIndex = 0; // 1門をデフォルトに

            // カテゴリに応じた合理的なデフォルト値を設定
            SetDefaultValuesByCategory();

            // 最初のティアと国を選択
            if (_yearComboBox.Items.Count > 0)
                _yearComboBox.SelectedIndex = 0;

            if (_countryComboBox.Items.Count > 0)
                _countryComboBox.SelectedIndex = 0;

            // 初期値として計算結果表示エリアをクリア
            _calculatedAttackText.Text = "---";
            _calculatedRangeText.Text = "--- km";
            _calculatedArmorPiercingText.Text = "---";
            _calculatedBuildCostText.Text = "---";
        }

        // カテゴリ変更時の処理
        _categoryComboBox.SelectionChanged += OnCategoryChanged;

        // サブカテゴリ変更時の処理
        _subCategoryComboBox.SelectionChanged += OnSubCategoryChanged;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    private string GetCategoryDisplayName(string categoryId)
    {
        if (_categories.ContainsKey(categoryId)) return _categories[categoryId].Name;
        return "砲";
    }

    private void SetDefaultValuesByCategory()
    {
        if (_categoryComboBox.SelectedItem is NavalCategoryItem category)
        {
            switch (category.Id)
            {
                case "SMLG": // 小口径砲
                    _shellWeightNumeric.Value = 15; // 15kg
                    _muzzleVelocityNumeric.Value = 700; // 700m/s
                    _rpmNumeric.Value = 15; // 15発/分
                    _calibreNumeric.Value = 7.5M; // 7.5cm
                    _elevationAngleNumeric.Value = 45; // 45度
                    _turretWeightNumeric.Value = 10; // 10トン
                    _manpowerNumeric.Value = 8; // 8人
                    break;
                case "SMMG": // 中口径砲
                    _shellWeightNumeric.Value = 50; // 50kg
                    _muzzleVelocityNumeric.Value = 800; // 800m/s
                    _rpmNumeric.Value = 6; // 6発/分
                    _calibreNumeric.Value = 15; // 15cm
                    _elevationAngleNumeric.Value = 35; // 35度
                    _turretWeightNumeric.Value = 25; // 25トン
                    _manpowerNumeric.Value = 20; // 20人
                    break;
                case "SMHG": // 大口径砲
                    _shellWeightNumeric.Value = 120; // 120kg
                    _muzzleVelocityNumeric.Value = 850; // 850m/s
                    _rpmNumeric.Value = 3; // 3発/分
                    _calibreNumeric.Value = 25; // 25cm
                    _elevationAngleNumeric.Value = 30; // 30度
                    _turretWeightNumeric.Value = 100; // 100トン
                    _manpowerNumeric.Value = 40; // 40人
                    break;
                case "SMSHG": // 超大口径砲
                    _shellWeightNumeric.Value = 400; // 400kg
                    _muzzleVelocityNumeric.Value = 900; // 900m/s
                    _rpmNumeric.Value = 1.5M; // 1.5発/分
                    _calibreNumeric.Value = 40; // 40cm
                    _elevationAngleNumeric.Value = 25; // 25度
                    _turretWeightNumeric.Value = 400; // 400トン
                    _manpowerNumeric.Value = 80; // 80人
                    break;
            }

            // リソースのデフォルト値も設定
            SetDefaultResources(category.Id);
        }
    }

    private void SetDefaultResources(string categoryId)
    {
        switch (categoryId)
        {
            case "SMLG": // 小口径砲
                _steelNumeric.Value = 1; // 1単位
                _chromiumNumeric.Value = 0; // クロムは不要
                break;
            case "SMMG": // 中口径砲
                _steelNumeric.Value = 3; // 3単位
                _chromiumNumeric.Value = 1; // 1単位
                break;
            case "SMHG": // 大口径砲
                _steelNumeric.Value = 6; // 6単位
                _chromiumNumeric.Value = 2; // 2単位
                break;
            case "SMSHG": // 超大口径砲
                _steelNumeric.Value = 10; // 10単位
                _chromiumNumeric.Value = 4; // 4単位
                break;
        }
    }

    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_categoryComboBox.SelectedItem is NavalCategoryItem category)
        {
            // カテゴリに応じたデフォルト値を設定
            SetDefaultValuesByCategory();

            // IDのプレフィックスを更新
            if (string.IsNullOrEmpty(_idTextBox.Text) ||
                (e.RemovedItems.Count > 0 &&
                 _idTextBox.Text.StartsWith(((NavalCategoryItem)e.RemovedItems[0]).Id.ToLower() + "_")))
                _idTextBox.Text = category.Id.ToLower() + "_";
        }
    }

    private void OnSubCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        // サブカテゴリが変更されたら砲身数も更新
        if (_subCategoryComboBox.SelectedItem != null)
        {
            var subCategory = _subCategoryComboBox.SelectedItem.ToString();

            switch (subCategory)
            {
                case "単装砲":
                    _barrelCountComboBox.SelectedIndex = 0; // 1門
                    break;
                case "連装砲":
                    _barrelCountComboBox.SelectedIndex = 1; // 2門
                    break;
                case "三連装砲":
                    _barrelCountComboBox.SelectedIndex = 2; // 3門
                    break;
                case "四連装砲":
                    _barrelCountComboBox.SelectedIndex = 3; // 4門
                    break;
            }
        }
    }

    // Gun_Design_View.cs の中に以下のメソッドを追加
    private void On_Save_Click(object sender, RoutedEventArgs e)
    {
        // 入力バリデーション
        if (string.IsNullOrWhiteSpace(_idTextBox.Text))
        {
            ShowError("IDを入力してください");
            return;
        }

        if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            ShowError("名前を入力してください");
            return;
        }

        if (_categoryComboBox.SelectedItem == null)
        {
            ShowError("カテゴリを選択してください");
            return;
        }

        if (_subCategoryComboBox.SelectedItem == null)
        {
            ShowError("サブカテゴリを選択してください");
            return;
        }

        if (_yearComboBox.SelectedItem == null)
        {
            ShowError("開発年を選択してください");
            return;
        }

        if (_countryComboBox.SelectedItem == null)
        {
            ShowError("開発国を選択してください");
            return;
        }

        // 全てのパラメータをGun_Processing()に渡すためにデータを収集
        var gunData = new Dictionary<string, object>
        {
            { "Id", _idTextBox.Text },
            { "Name", _nameTextBox.Text },
            { "Category", ((NavalCategoryItem)_categoryComboBox.SelectedItem).Id },
            { "SubCategory", _subCategoryComboBox.SelectedItem.ToString() },
            { "Year", int.Parse(((NavalYearItem)_yearComboBox.SelectedItem).Year.TrimEnd('以', '前')) },
            { "Tier", ((NavalYearItem)_yearComboBox.SelectedItem).Tier },
            { "Country", _countryComboBox.SelectedItem.ToString() },
            { "ShellWeight", (double)_shellWeightNumeric.Value },
            { "MuzzleVelocity", (double)_muzzleVelocityNumeric.Value },
            { "RPM", (double)_rpmNumeric.Value },
            { "Calibre", (double)_calibreNumeric.Value },
            { "CalibreType", _calibreTypeComboBox.SelectedItem.ToString() },
            { "BarrelCount", int.Parse(_barrelCountComboBox.SelectedItem.ToString()) },
            { "ElevationAngle", (double)_elevationAngleNumeric.Value },
            { "TurretWeight", (double)_turretWeightNumeric.Value },
            { "Manpower", (int)_manpowerNumeric.Value },
            { "Steel", (int)_steelNumeric.Value },
            { "Chromium", (int)_chromiumNumeric.Value }
        };

        // Gun_Processingクラスに全てのデータを渡して処理を行う
        // 実際の処理は別クラスに実装
        NavalEquipment processedEquipment = Gun_Processing(gunData);

        // 処理結果を返して画面を閉じる
        Close(processedEquipment);
    }

    private void On_Cancel_Click(object sender, RoutedEventArgs e)
    {
        // キャンセル
        Close();
    }

    private void ShowError(string message)
    {
        // エラーメッセージを表示（実際の実装ではダイアログを表示する）
        Console.WriteLine($"エラー: {message}");
    }
}