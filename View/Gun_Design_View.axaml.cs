using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using HOI4NavalModder.Calculators;
using HOI4NavalModder.Core.Models;
using HOI4NavalModder.Core.Services;
using HOI4NavalModder.Core.Utilities;
using HOI4NavalModder.Mapper;
using HOI4NavalModder.Window;

namespace HOI4NavalModder.View;

public partial class GunDesignView : Avalonia.Controls.Window
{
    private readonly CheckBox _autoGenerateIdCheckBox;

    private readonly ComboBox _barrelCountComboBox;

    // クラスのフィールド宣言部分に追加
    private readonly CheckBox _isAswCheckBox;
    // 追加されたコントロール
    private readonly NumericUpDown _barrelLengthNumeric;
    private readonly TextBlock _calculatedArmorPiercingText;
    private readonly TextBlock _calculatedAttackText;
    private readonly TextBlock _calculatedBuildCostText;
    private readonly TextBlock _calculatedRangeText;
    private readonly TextBlock _calculatedLgAttackText;
    private readonly TextBlock _calculatedHgAttackText;
    private readonly TextBlock _calculatedAntiAirText;
    private readonly NumericUpDown _calibreNumeric;
    private readonly ComboBox _calibreTypeComboBox;
    private readonly Dictionary<string, NavalCategory> _categories;
    private readonly ComboBox _categoryComboBox;
    private readonly NumericUpDown _chromiumNumeric;
    private readonly ComboBox _countryComboBox;

    private readonly TextBox _descriptionTextBox;
    private readonly NumericUpDown _elevationAngleNumeric;
    private readonly TextBox _idTextBox;
    private readonly NumericUpDown _manpowerNumeric;
    private readonly NumericUpDown _muzzleVelocityNumeric;
    private readonly TextBox _nameTextBox;
    private readonly NavalEquipment _originalEquipment;
    private readonly NumericUpDown _rpmNumeric;
    private readonly NumericUpDown _shellWeightNumeric;
    private readonly NumericUpDown _steelNumeric;
    private readonly NumericUpDown _yearNumeric;
    private readonly ComboBox _subCategoryComboBox;
    private readonly Dictionary<int, string> _tierYears;
    private readonly NumericUpDown _turretWeightNumeric;
    private string _activeMod;
    private string? _configFilePath;

    private List<CountryListManager.CountryInfo> _countryInfoList;

    // CountryListManagerのインスタンスを追加
    private CountryListManager _countryListManager;
    private string _vanillaPath;

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
        _yearNumeric = this.FindControl<NumericUpDown>("YearNumeric");
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
        _calculatedLgAttackText = this.FindControl<TextBlock>("CalculatedLgAttackText");
        _calculatedHgAttackText = this.FindControl<TextBlock>("CalculatedHgAttackText");
        _calculatedAntiAirText = this.FindControl<TextBlock>("CalculatedAntiAirText");
        // 追加：記述テキストボックスの取得
        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
        // 追加されたコントロールの取得
        _barrelLengthNumeric = this.FindControl<NumericUpDown>("BarrelLengthNumeric");
        _autoGenerateIdCheckBox = this.FindControl<CheckBox>("AutoGenerateIdCheckBox");
        // コンストラクタ内のUI取得部分に追加 (両方のコンストラクタに追加する)
        _isAswCheckBox = this.FindControl<CheckBox>("IsAswCheckBox");
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

        InitializeCountryList();
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


        // 自動ID生成の初期設定とイベントハンドラ
        if (_originalEquipment != null && !string.IsNullOrEmpty(_originalEquipment.Id))
        {
            // 既存装備を編集する場合
            // 装備データを読み込み
            LoadEquipmentData();

            // 既存IDから砲身長を抽出して設定
            string countrytag;
            if (NavalGunIdGenerator.TryParseGunId(_originalEquipment.Id, out _, out countrytag ,out _, out _, out _, out var barrelLength))
                _barrelLengthNumeric.Value = barrelLength;
                
            else
                // IDがパターンに合わない場合、デフォルト値を使用
                _barrelLengthNumeric.Value = 45; // デフォルト値

            // 編集モードでは自動生成をオフに
            _autoGenerateIdCheckBox.IsChecked = false;
            _idTextBox.IsEnabled = true;
        }
        else
        {
            // 新規作成の場合は自動生成をオン
            _autoGenerateIdCheckBox.IsChecked = true;
            _idTextBox.IsEnabled = false;

            // デフォルト値設定
            if (_categoryComboBox.Items.Count > 0)
                _categoryComboBox.SelectedIndex = 0;

            if (_subCategoryComboBox.Items.Count > 0)
                _subCategoryComboBox.SelectedIndex = 0;

            // デフォルトの開発年を設定
            _yearNumeric.Value = 1936;

            if (_countryComboBox.Items.Count > 0)
                _countryComboBox.SelectedIndex = 0;

            if (_barrelCountComboBox.Items.Count > 0)
                _barrelCountComboBox.SelectedIndex = 0;

            // 初期値を設定
            _barrelLengthNumeric.Value = 45;
        }

        // イベントハンドラの設定
        _categoryComboBox.SelectionChanged += OnCategoryChanged;
        _subCategoryComboBox.SelectionChanged += OnSubCategoryChanged;

        // 自動ID生成のためのイベントハンドラ
        _autoGenerateIdCheckBox.IsCheckedChanged += OnAutoGenerateIdChanged;
        _categoryComboBox.SelectionChanged += UpdateAutoGeneratedId;
        _yearNumeric.ValueChanged += UpdateAutoGeneratedId;
        _calibreNumeric.ValueChanged += UpdateAutoGeneratedId;
        _calibreTypeComboBox.SelectionChanged += UpdateAutoGeneratedId;
        _barrelLengthNumeric.ValueChanged += UpdateAutoGeneratedId;

        // 性能値計算のためのイベントハンドラ
        _shellWeightNumeric.ValueChanged += UpdateCalculatedValues;
        _muzzleVelocityNumeric.ValueChanged += UpdateCalculatedValues;
        _rpmNumeric.ValueChanged += UpdateCalculatedValues;
        _calibreNumeric.ValueChanged += UpdateCalculatedValues;
        _calibreTypeComboBox.SelectionChanged += UpdateCalculatedValues;
        _barrelCountComboBox.SelectionChanged += UpdateCalculatedValues;
        _barrelLengthNumeric.ValueChanged += UpdateCalculatedValues;
        _elevationAngleNumeric.ValueChanged += UpdateCalculatedValues;
        _turretWeightNumeric.ValueChanged += UpdateCalculatedValues;
        _yearNumeric.ValueChanged += UpdateCalculatedValues;
        _isAswCheckBox.IsCheckedChanged += UpdateCalculatedValues;

        // 初期ID生成（自動生成がオンの場合）
        if (_autoGenerateIdCheckBox.IsChecked == true) UpdateAutoGeneratedId(null, EventArgs.Empty);
    }


    public GunDesignView(Dictionary<string, object> rawGunData, Dictionary<string, NavalCategory> categories,
        Dictionary<int, string> tierYears)
    {
        InitializeComponent();

        _categories = categories;
        _tierYears = tierYears;

        // UI要素の取得
        _idTextBox = this.FindControl<TextBox>("IdTextBox");
        _nameTextBox = this.FindControl<TextBox>("NameTextBox");
        _categoryComboBox = this.FindControl<ComboBox>("CategoryComboBox");
        _subCategoryComboBox = this.FindControl<ComboBox>("SubCategoryComboBox");
        _yearNumeric = this.FindControl<NumericUpDown>("YearNumeric");
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
        _calculatedLgAttackText = this.FindControl<TextBlock>("CalculatedLgAttackText");
        _calculatedHgAttackText = this.FindControl<TextBlock>("CalculatedHgAttackText");
        _calculatedAntiAirText = this.FindControl<TextBlock>("CalculatedAntiAirText");
        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
        // 追加されたコントロールの取得
        _barrelLengthNumeric = this.FindControl<NumericUpDown>("BarrelLengthNumeric");
        _autoGenerateIdCheckBox = this.FindControl<CheckBox>("AutoGenerateIdCheckBox");
        // コンストラクタ内のUI取得部分に追加 (両方のコンストラクタに追加する)
        _isAswCheckBox = this.FindControl<CheckBox>("IsAswCheckBox");
        // UI項目の選択肢を初期化
        InitializeUiOptions();
        // 2番目のコンストラクタの InitializeUIOptions() の後に以下を追加
        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
        // 生データから値を設定
        if (rawGunData != null)
        {
            PopulateFromRawData(rawGunData);

            // 既存IDから砲身長を抽出して設定
            var id = rawGunData["Id"].ToString();
            string countrytag;
            if (NavalGunIdGenerator.TryParseGunId(id, out _, out countrytag ,out _, out _, out _, out var barrelLength))
                _barrelLengthNumeric.Value = barrelLength;
            else if (rawGunData.ContainsKey("BarrelLength"))
                // 生データに砲身長がある場合
                _barrelLengthNumeric.Value = GetDecimalValue(rawGunData, "BarrelLength");
            else
                // デフォルト値
                _barrelLengthNumeric.Value = 45;

            // 編集モードでは自動生成をオフに
            _autoGenerateIdCheckBox.IsChecked = false;
            _idTextBox.IsEnabled = true;
        }

        InitializeCountryList();
        // イベントハンドラを設定
        _categoryComboBox.SelectionChanged += OnCategoryChanged;
        _subCategoryComboBox.SelectionChanged += OnSubCategoryChanged;

        // 自動ID生成のためのイベントハンドラ
        _autoGenerateIdCheckBox.IsCheckedChanged += OnAutoGenerateIdChanged;
        _categoryComboBox.SelectionChanged += UpdateAutoGeneratedId;
        _yearNumeric.ValueChanged += UpdateAutoGeneratedId;
        _calibreNumeric.ValueChanged += UpdateAutoGeneratedId;
        _calibreTypeComboBox.SelectionChanged += UpdateAutoGeneratedId;
        _barrelLengthNumeric.ValueChanged += UpdateAutoGeneratedId;

        // 性能値計算のためのイベントハンドラ
        _shellWeightNumeric.ValueChanged += UpdateCalculatedValues;
        _muzzleVelocityNumeric.ValueChanged += UpdateCalculatedValues;
        _rpmNumeric.ValueChanged += UpdateCalculatedValues;
        _calibreNumeric.ValueChanged += UpdateCalculatedValues;
        _calibreTypeComboBox.SelectionChanged += UpdateCalculatedValues;
        _barrelCountComboBox.SelectionChanged += UpdateCalculatedValues;
        _barrelLengthNumeric.ValueChanged += UpdateCalculatedValues;
        _elevationAngleNumeric.ValueChanged += UpdateCalculatedValues;
        _turretWeightNumeric.ValueChanged += UpdateCalculatedValues;
        _yearNumeric.ValueChanged += UpdateCalculatedValues;
        _isAswCheckBox.IsCheckedChanged += UpdateCalculatedValues;
    }

    private void LoadEquipmentData()
    {
        if (_originalEquipment == null) return;

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

        // 開発年を設定
        if (_originalEquipment.Year > 0)
            _yearNumeric.Value = _originalEquipment.Year;
        else
            _yearNumeric.Value = 1936; // デフォルト値

        // 開発国の選択
        if (!string.IsNullOrEmpty(_originalEquipment.Country)) SetCountrySelection(_originalEquipment.Country);

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
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedLgAttack"))
            _calculatedLgAttackText.Text = _originalEquipment.AdditionalProperties["CalculatedLgAttack"].ToString();
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedHgAttack"))
            _calculatedHgAttackText.Text = _originalEquipment.AdditionalProperties["CalculatedHgAttack"].ToString();
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedAntiAirAttack"))
            _calculatedAntiAirText.Text = _originalEquipment.AdditionalProperties["CalculatedAntiAirAttack"].ToString();
        if (_originalEquipment.AdditionalProperties.ContainsKey("IsAsw"))
            _isAswCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsAsw"]);
        
        if (!string.IsNullOrEmpty(_originalEquipment.Id))
        {
            string category, countryTag;
            int year, barrelLength;
            double calibre;
            string calibreType;

            if (NavalGunIdGenerator.TryParseGunId(_originalEquipment.Id, 
                    out category, 
                    out countryTag, 
                    out year, 
                    out calibre, 
                    out calibreType, 
                    out barrelLength))
            {
                // カテゴリの選択（既に上で処理済み）

                // 国家タグが取得できた場合は選択
                if (!string.IsNullOrEmpty(countryTag))
                    SetCountrySelection(countryTag);

                // 年、口径、口径タイプの設定
                if (year > 0)
                    _yearNumeric.Value = year;
            
                _calibreNumeric.Value = Convert.ToDecimal(calibre);
        
                // 口径タイプの選択
                var calibreTypeIndex = _calibreTypeComboBox.Items.IndexOf(calibreType);
                if (calibreTypeIndex >= 0)
                    _calibreTypeComboBox.SelectedIndex = calibreTypeIndex;
            
                // 砲身長の設定
                _barrelLengthNumeric.Value = barrelLength;
            }
            else
            {
                // IDがパターンに合わない場合、デフォルト値を使用
                _barrelLengthNumeric.Value = 45; // デフォルト値
            }
        }
    }

    private async void InitializeCountryList()
    {
        try
        {
            // 設定からパスを取得（空文字を渡しても内部で自動的に探索する）
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HOI4NavalModder");

            var settingsPath = Path.Combine(appDataPath, "modpaths.json");

            // 設定ファイルからパスを直接読み込む（CountryListManagerでも行われるが明示的に行う）
            var modPath = string.Empty;
            var gamePath = string.Empty;

            if (File.Exists(settingsPath))
                try
                {
                    var settingsJson = File.ReadAllText(settingsPath);
                    var settings = JsonSerializer.Deserialize<ModConfig>(settingsJson);
                    if (settings != null)
                    {
                        var activeMod = settings.Mods?.FirstOrDefault(m => m.IsActive);
                        if (activeMod != null)
                        {
                            Console.WriteLine($"アクティブMOD: {activeMod.Name}");
                            modPath = activeMod.Path;
                        }
                        else
                        {
                            Console.WriteLine("設定のMODパスが存在しません");
                        }

                        gamePath = settings.VanillaGamePath ?? string.Empty;
                        Console.WriteLine($"設定から読み込んだパス - MOD: {modPath}, ゲーム: {gamePath}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"設定ファイル読み込みエラー: {ex.Message}");
                }

            // CountryListManagerは明示的にパスを指定
            _countryListManager = new CountryListManager(modPath, gamePath);

            // 国家リストを非同期で取得（showAllCountries=trueは全国家表示）
            _countryInfoList = await _countryListManager.GetCountriesAsync(true);

            // ComboBoxをクリア
            _countryComboBox.Items.Clear();

            // まず「未設定」を追加
            _countryComboBox.Items.Add("未設定");

            // 国家リストをComboBoxに追加
            foreach (var country in _countryInfoList)
            {
                // 国名とタグを組み合わせた表示名を作成
                var displayName = $"{country.Name} ({country.Tag})";
                _countryComboBox.Items.Add(displayName);
            }

            // デフォルトで「未設定」を選択
            _countryComboBox.SelectedIndex = 0;

            // 装備データがある場合は適切な国家を選択
            if (_originalEquipment != null && !string.IsNullOrEmpty(_originalEquipment.Country))
                SetCountrySelection(_originalEquipment.Country);

            Console.WriteLine($"国家リスト初期化完了: {_countryComboBox.Items.Count - 1}件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"国家リスト初期化エラー: {ex.Message}");
            Console.WriteLine($"スタックトレース: {ex.StackTrace}");

            // エラー時は既存の国家リストを使用
            _countryComboBox.Items.Clear();
            string[] defaultCountries =
            {
                "未設定", "日本 (JAP)", "アメリカ (USA)", "イギリス (ENG)",
                "ドイツ (GER)", "ソ連 (SOV)", "イタリア (ITA)", "フランス (FRA)", "その他 (OTH)"
            };
            foreach (var country in defaultCountries) _countryComboBox.Items.Add(country);
            _countryComboBox.SelectedIndex = 0;

            // 空のリストを初期化しておく
            _countryInfoList = new List<CountryListManager.CountryInfo>();
        }
    }


    // 国家タグに基づいてコンボボックスの選択を設定
    // 国家タグまたは国名に基づいてコンボボックスの選択を設定
    private void SetCountrySelection(string countryValue)
    {
        if (string.IsNullOrEmpty(countryValue))
        {
            // 値が空の場合は「未設定」を選択
            _countryComboBox.SelectedIndex = 0;
            return;
        }

        Console.WriteLine($"国家選択: {countryValue}");

        // 1. 完全一致（タグが括弧内にある場合）
        for (var i = 0; i < _countryComboBox.Items.Count; i++)
        {
            var item = _countryComboBox.Items[i].ToString();
            if (item.EndsWith($"({countryValue})"))
            {
                _countryComboBox.SelectedIndex = i;
                Console.WriteLine($"タグの完全一致で選択: {item}");
                return;
            }
        }

        // 2. 国家タグが直接マッチする場合
        foreach (var country in _countryInfoList)
            if (country.Tag.Equals(countryValue, StringComparison.OrdinalIgnoreCase))
                for (var i = 0; i < _countryComboBox.Items.Count; i++)
                {
                    var item = _countryComboBox.Items[i].ToString();
                    if (item.Contains($"({country.Tag})"))
                    {
                        _countryComboBox.SelectedIndex = i;
                        Console.WriteLine($"タグの直接マッチで選択: {item}");
                        return;
                    }
                }

        // 3. 国名が直接マッチする場合
        foreach (var country in _countryInfoList)
            if (country.Name.Equals(countryValue, StringComparison.OrdinalIgnoreCase))
                for (var i = 0; i < _countryComboBox.Items.Count; i++)
                {
                    var item = _countryComboBox.Items[i].ToString();
                    if (item.StartsWith(country.Name))
                    {
                        _countryComboBox.SelectedIndex = i;
                        Console.WriteLine($"国名の直接マッチで選択: {item}");
                        return;
                    }
                }

        // 4. 部分一致を試みる
        for (var i = 0; i < _countryComboBox.Items.Count; i++)
        {
            var item = _countryComboBox.Items[i].ToString();
            if (item.Contains(countryValue, StringComparison.OrdinalIgnoreCase))
            {
                _countryComboBox.SelectedIndex = i;
                Console.WriteLine($"部分一致で選択: {item}");
                return;
            }
        }

        // 5. 一致するものがなかった場合、mainとか一般的な接頭辞か判定
        var lowerValue = countryValue.ToLower();
        if (lowerValue == "main" || lowerValue == "generic" || lowerValue == "default")
        {
            // 未設定（デフォルト）を選択
            _countryComboBox.SelectedIndex = 0;
            Console.WriteLine("一般的な値のため「未設定」を選択");
            return;
        }

        // それでも見つからない場合は「その他」を探す
        for (var i = 0; i < _countryComboBox.Items.Count; i++)
        {
            var item = _countryComboBox.Items[i].ToString();
            if (item.Contains("その他") || item.Contains("(OTH)"))
            {
                _countryComboBox.SelectedIndex = i;
                Console.WriteLine("一致しないため「その他」を選択");
                return;
            }
        }

        // 最後の手段として最初の項目を選択
        Console.WriteLine($"一致する国家が見つからないため「未設定」を選択: {countryValue}");
        _countryComboBox.SelectedIndex = 0;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void InitializeUiOptions()
    {
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
    }

    // 生データからフォームに値を設定するメソッド
    private void PopulateFromRawData(Dictionary<string, object> rawGunData)
    {
        // ウィンドウタイトルをカテゴリに合わせて設定
        var categoryId = rawGunData["Category"].ToString();
        var categoryName = GetCategoryDisplayName(categoryId);
        Title = $"{categoryName}の編集";

        // 基本情報の設定
        _idTextBox.Text = rawGunData["Id"].ToString();
        _nameTextBox.Text = rawGunData["Name"].ToString();

        // ComboBoxの選択
        SelectComboBoxItem(_categoryComboBox, "Id", categoryId);
        SelectComboBoxItem(_subCategoryComboBox, null, rawGunData["SubCategory"].ToString());
        
        // 開発年を設定
        if (rawGunData.ContainsKey("Year"))
            _yearNumeric.Value = GetDecimalValue(rawGunData, "Year");
        else
            _yearNumeric.Value = 1936; // デフォルト値
            
        SelectComboBoxItem(_countryComboBox, null, rawGunData["Country"].ToString());

        // 数値の設定
        SetNumericValue(_shellWeightNumeric, GetDoubleValue(rawGunData, "ShellWeight"));
        SetNumericValue(_muzzleVelocityNumeric, GetDoubleValue(rawGunData, "MuzzleVelocity"));
        SetNumericValue(_rpmNumeric, GetDoubleValue(rawGunData, "RPM"));
        SetNumericValue(_calibreNumeric, GetDoubleValue(rawGunData, "Calibre"));
        SelectComboBoxItem(_calibreTypeComboBox, null, rawGunData["CalibreType"].ToString());
        SelectComboBoxItem(_barrelCountComboBox, null, rawGunData["BarrelCount"].ToString());
        SetNumericValue(_elevationAngleNumeric, GetDoubleValue(rawGunData, "ElevationAngle"));
        SetNumericValue(_turretWeightNumeric, GetDoubleValue(rawGunData, "TurretWeight"));
        SetNumericValue(_manpowerNumeric, GetIntValue(rawGunData, "Manpower"));

        // リソース
        SetNumericValue(_steelNumeric, GetIntValue(rawGunData, "Steel"));
        SetNumericValue(_chromiumNumeric, GetIntValue(rawGunData, "Chromium"));
        if (rawGunData.ContainsKey("Country") && rawGunData["Country"] != null)
        {
            var countryValue = rawGunData["Country"].ToString();
            SetCountrySelection(countryValue);
        }

        // 計算された性能値
        if (rawGunData.ContainsKey("CalculatedAttack"))
            _calculatedAttackText.Text = rawGunData["CalculatedAttack"].ToString();

        if (rawGunData.ContainsKey("CalculatedRange"))
            _calculatedRangeText.Text = rawGunData["CalculatedRange"] + " km";

        if (rawGunData.ContainsKey("CalculatedArmorPiercing"))
            _calculatedArmorPiercingText.Text = rawGunData["CalculatedArmorPiercing"].ToString();

        if (rawGunData.ContainsKey("CalculatedBuildCost"))
            _calculatedBuildCostText.Text = rawGunData["CalculatedBuildCost"].ToString();
            
        if (rawGunData.ContainsKey("CalculatedLgAttack"))
            _calculatedLgAttackText.Text = rawGunData["CalculatedLgAttack"].ToString();
            
        if (rawGunData.ContainsKey("CalculatedHgAttack"))
            _calculatedHgAttackText.Text = rawGunData["CalculatedHgAttack"].ToString();
            
        if (rawGunData.ContainsKey("CalculatedAntiAirAttack"))
            _calculatedAntiAirText.Text = rawGunData["CalculatedAntiAirAttack"].ToString();
            
        if (rawGunData.ContainsKey("IsAsw"))
            _isAswCheckBox.IsChecked = GetBooleanValue(rawGunData, "IsAsw");
            
        // 備考欄の設定
        if (rawGunData.ContainsKey("Description"))
            _descriptionTextBox.Text = GetStringValue(rawGunData, "Description");
    }
    
    private static bool GetBooleanValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return false;

        try
        {
            if (data[key] is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.True)
                    return true;
                if (jsonElement.ValueKind == JsonValueKind.False)
                    return false;
                if (jsonElement.ValueKind == JsonValueKind.String &&
                    bool.TryParse(jsonElement.GetString(), out var result))
                    return result;
            }

            return Convert.ToBoolean(data[key]);
        }
        catch
        {
            return false;
        }
    }
    // ヘルパーメソッド: ComboBoxの項目を選択
    private void SelectComboBoxItem(ComboBox comboBox, string propertyName, object value)
    {
        if (comboBox.Items.Count == 0) return;

        if (propertyName == null)
        {
            // 単純な値比較
            for (var i = 0; i < comboBox.Items.Count; i++)
                if (comboBox.Items[i].ToString() == value.ToString())
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
        }
        else
        {
            // プロパティによる比較
            for (var i = 0; i < comboBox.Items.Count; i++)
            {
                var item = comboBox.Items[i];
                var prop = item.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var propValue = prop.GetValue(item);
                    if (propValue.ToString() == value.ToString())
                    {
                        comboBox.SelectedIndex = i;
                        return;
                    }
                }
            }
        }

        // 一致するものがなければ最初の項目を選択
        comboBox.SelectedIndex = 0;
    }

    // ヘルパーメソッド: NumericUpDownの値を設定
    private static void SetNumericValue(NumericUpDown numericUpDown, decimal value)
    {
        if (value >= numericUpDown.Minimum && value <= numericUpDown.Maximum) numericUpDown.Value = value;
    }

    // ヘルパーメソッド: Dictionaryから安全にdouble値を取得
    private static decimal GetDoubleValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return 0;

        try
        {
            if (data[key] is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number) return (decimal)jsonElement.GetDouble();

                if (jsonElement.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(jsonElement.GetString(), out var result))
                    return result;
            }

            return Convert.ToDecimal(data[key]);
        }
        catch
        {
            return 0;
        }
    }

    // ヘルパーメソッド: Dictionaryから安全にint値を取得
    private static decimal GetIntValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return 0;

        try
        {
            if (data[key] is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number) return jsonElement.GetInt32();

                if (jsonElement.ValueKind == JsonValueKind.String &&
                    int.TryParse(jsonElement.GetString(), out var result))
                    return result;
            }

            return Convert.ToInt32(data[key]);
        }
        catch
        {
            return 0;
        }
    }

    // ヘルパーメソッド: Dictionaryから安全にdecimal値を取得
    private static decimal GetDecimalValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return 0;

        try
        {
            if (data[key] is JsonElement jsonElement)
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return (decimal)jsonElement.GetDouble();

            return Convert.ToDecimal(data[key]);
        }
        catch
        {
            return 0;
        }
    }

    // ヘルパーメソッド: Dictionaryから安全にString値を取得
    private static string GetStringValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return string.Empty;

        try
        {
            if (data[key] is JsonElement jsonElement)
                if (jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString();

            return data[key].ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private string GetSelectedCountryValue()
    {
        if (_countryComboBox.SelectedIndex <= 0 || _countryComboBox.SelectedItem == null)
            // 未選択または「未設定」の場合
            return "";

        var selectedCountry = _countryComboBox.SelectedItem.ToString();

        // 表示名から国家タグを抽出（例: "日本 (JAP)" → "JAP"）
        var startIndex = selectedCountry.LastIndexOf('(');
        var endIndex = selectedCountry.LastIndexOf(')');

        if (startIndex > 0 && endIndex > startIndex)
        {
            var tag = selectedCountry.Substring(startIndex + 1, endIndex - startIndex - 1);
            Console.WriteLine($"選択された国家タグ: {tag}");
            return tag;
        }

        // タグが見つからない場合は表示名をそのまま返す
        Console.WriteLine($"選択された国家名: {selectedCountry}");
        return selectedCountry;
    }

    private string GetCategoryDisplayName(string categoryId)
    {
        if (_categories.ContainsKey(categoryId)) return _categories[categoryId].Name;
        return "砲";
    }

    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_categoryComboBox.SelectedItem is NavalCategoryItem category)
        {
            // カテゴリに応じたデフォルト値を設定
            SetDefaultValuesByCategory();

            // IDのプレフィックスを更新（自動生成がオフの場合のみ）
            if (!(_autoGenerateIdCheckBox.IsChecked ?? false))
            {
                if (string.IsNullOrEmpty(_idTextBox.Text) ||
                    (e.RemovedItems.Count > 0 &&
                     _idTextBox.Text.StartsWith(((NavalCategoryItem)e.RemovedItems[0]).Id.ToLower() + "_")))
                    _idTextBox.Text = category.Id.ToLower() + "_";
            }
            else
            {
                // 自動生成がオンならIDを更新
                UpdateAutoGeneratedId(null, null);
            }
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

    // 自動生成チェックボックスの状態変更時の処理
    private void OnAutoGenerateIdChanged(object sender, RoutedEventArgs e)
    {
        var autoGenerate = _autoGenerateIdCheckBox.IsChecked ?? false;

        // 自動生成がオンならテキストボックスを無効化
        _idTextBox.IsEnabled = !autoGenerate;

        if (autoGenerate)
            // チェックされたら即座にID生成
            UpdateAutoGeneratedId(null, null);
    }


// ID自動生成メソッド
    private void UpdateAutoGeneratedId(object sender, EventArgs e)
    {
        // 自動生成がオフなら何もしない
        if (!(_autoGenerateIdCheckBox.IsChecked ?? false))
            return;

        // 必要なパラメータを取得
        if (_categoryComboBox.SelectedItem is NavalCategoryItem categoryItem &&
            _yearNumeric.Value.HasValue &&
            _calibreTypeComboBox.SelectedItem != null)
        {
            var category = categoryItem.Id;
            var year = (int)_yearNumeric.Value.Value;
            var calibre = (double)(_calibreNumeric.Value ?? 0);
            var calibreType = _calibreTypeComboBox.SelectedItem.ToString();
            var barrelLength = (double)(_barrelLengthNumeric.Value ?? 45);
        
            // 国家タグを取得
            var countryTag = GetSelectedCountryTag();

            // IDを生成
            var generatedId = NavalGunIdGenerator.GenerateGunId(
                category, countryTag, year, calibre, calibreType, barrelLength);

            // テキストボックスに設定
            _idTextBox.Text = generatedId;
        }
    }

// 選択された国家タグを取得するメソッド
    private string GetSelectedCountryTag()
    {
        if (_countryComboBox.SelectedIndex <= 0 || _countryComboBox.SelectedItem == null)
            // 未選択または「未設定」の場合
            return "";

        var selectedCountry = _countryComboBox.SelectedItem.ToString();

        // 表示名から国家タグを抽出（例: "日本 (JAP)" → "JAP"）
        var startIndex = selectedCountry.LastIndexOf('(');
        var endIndex = selectedCountry.LastIndexOf(')');

        if (startIndex > 0 && endIndex > startIndex)
        {
            var tag = selectedCountry.Substring(startIndex + 1, endIndex - startIndex - 1);
            return tag;
        }

        // タグが見つからない場合は空文字を返す
        return "";
    }

    // 性能値の計算と表示を更新するメソッド
    private void UpdateCalculatedValues(object sender, EventArgs e)
    {
        try
        {
            if (_categoryComboBox.SelectedItem is not NavalCategoryItem categoryItem ||
                _calibreTypeComboBox.SelectedItem == null ||
                _barrelCountComboBox.SelectedItem == null) return;

            // パラメータの取得
            var shellWeight = (double)(_shellWeightNumeric.Value ?? 0);
            var muzzleVelocity = (double)(_muzzleVelocityNumeric.Value ?? 0);
            var rpm = (double)(_rpmNumeric.Value ?? 0);
            var calibre = (double)(_calibreNumeric.Value ?? 0);
            var calibreType = _calibreTypeComboBox.SelectedItem.ToString();
            var barrelCount = int.Parse(_barrelCountComboBox.SelectedItem.ToString());
            var elevationAngle = (double)(_elevationAngleNumeric.Value ?? 0);
            var barrelLength = (double)(_barrelLengthNumeric.Value ?? 45);
            var turretWeight = (double)(_turretWeightNumeric.Value ?? 0);
            var year = (int)(_yearNumeric.Value ?? 1936);
            var isAsw = _isAswCheckBox.IsChecked ?? false;

            // 口径をmmに変換
            var calibreInMm = ConvertCalibreToMm(calibre, calibreType);

            // 攻撃力計算: m*v^2/4000000*RoF*barrelCount
            var attackBase = shellWeight * Math.Pow(muzzleVelocity, 2) / 4000000 * rpm;
            var totalAttack = attackBase * barrelCount;

            // 口径範囲によって攻撃タイプを決定 (14cm-24cm間はグラデーション)
            double lgAttackValue = 0;
            double hgAttackValue = 0;

            if (calibreInMm < 140)
            {
                // 完全に軽砲攻撃
                lgAttackValue = attackBase * barrelCount;
            }
            else if (calibreInMm > 240)
            {
                // 完全に重砲攻撃
                hgAttackValue = attackBase * barrelCount;
            }
            else
            {
                // 14cm～24cmの間は線形補間でグラデーション
                var heavyRatio = (calibreInMm - 140) / 100.0; // 14cmで0%、24cmで100%
                lgAttackValue = attackBase * (1 - heavyRatio) * barrelCount;
                hgAttackValue = attackBase * heavyRatio * barrelCount;
            }

            // 装甲貫通値計算
            var lgArmorPiercing = lgAttackValue > 0 ? lgAttackValue / Math.Pow(calibreInMm / 10, 2) : 0;
            var hgArmorPiercing = hgAttackValue > 0 ? hgAttackValue / Math.Pow(calibreInMm / 10, 2) : 0;
            var armorPiercingValue = lgArmorPiercing + hgArmorPiercing;

            // 射程計算（新しい計算式: 口径(mm) * 初速 * 砲身長 * 仰角 / 80000）
            var rangeValue = calibreInMm * muzzleVelocity * barrelLength * elevationAngle / 80000;

            // 対空攻撃力計算
            var antiAirAttack = (rpm / 8.5) * (muzzleVelocity / 750) * (elevationAngle / 75) * (100 / calibreInMm) * barrelCount;

            // 建造コスト計算
            var buildCost = Math.Pow(calibreInMm / 10, 1.5) + turretWeight / (10 * calibreInMm / 10) + rpm * 0.75 + (year - 1920) / 10;

            // 対潜攻撃力計算
            var subAttack = isAsw ? (shellWeight / 35) * (870 / muzzleVelocity) * (rpm / 5) * 1.25 * barrelCount : 0;

            // 計算結果をUIに表示（小数点第10位まで表示するフォーマット）
            _calculatedAttackText.Text = totalAttack.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedLgAttackText.Text = lgAttackValue.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedHgAttackText.Text = hgAttackValue.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedArmorPiercingText.Text = armorPiercingValue.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedRangeText.Text = rangeValue.ToString("F10").TrimEnd('0').TrimEnd('.') + " km";
            _calculatedAntiAirText.Text = antiAirAttack.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedBuildCostText.Text = buildCost.ToString("F10").TrimEnd('0').TrimEnd('.');
        }
        catch (Exception ex)
        {
            Console.WriteLine($"計算エラー: {ex.Message}");
        }
    }

    // 各種口径単位をmmに変換
    private static double ConvertCalibreToMm(double calibre, string calibreType)
    {
        switch (calibreType)
        {
            case "cm":
                return calibre * 10;
            case "inch":
                return calibre * 25.4;
            case "mm":
                return calibre;
            default:
                return calibre * 10; // デフォルトはcmとして扱う
        }
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

    // 保存ボタンのイベントハンドラを修正
    public async void On_Save_Click(object sender, RoutedEventArgs e)
    {
        // 入力バリデーション
        if (string.IsNullOrWhiteSpace(_idTextBox.Text))
        {
            UiHelper.ShowError("IDを入力してください");
            return;
        }

        if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            UiHelper.ShowError("名前を入力してください");
            return;
        }

        if (_categoryComboBox.SelectedItem == null)
        {
            UiHelper.ShowError("カテゴリを選択してください");
            return;
        }

        if (_subCategoryComboBox.SelectedItem == null)
        {
            UiHelper.ShowError("サブカテゴリを選択してください");
            return;
        }

        var equipmentId = _idTextBox.Text;

        // IDが既存のものと衝突するかチェック
        var dbManager = new DatabaseManager();
        var idExists = dbManager.IdExists(equipmentId);

        // オリジナルの装備を編集している場合は衝突としない
        var isEditingOriginal = _originalEquipment != null &&
                                _originalEquipment.Id == equipmentId;
        var countryValue = GetSelectedCountryValue();
        if (_countryComboBox.SelectedItem != null && _countryComboBox.SelectedIndex > 0)
        {
            var selectedCountry = _countryComboBox.SelectedItem.ToString();

            // 表示名から国家タグを抽出（例: "日本 (JAP)" → "JAP"）
            var startIndex = selectedCountry.LastIndexOf('(');
            var endIndex = selectedCountry.LastIndexOf(')');

            if (startIndex > 0 && endIndex > startIndex)
                countryValue = selectedCountry.Substring(startIndex + 1, endIndex - startIndex - 1);
            else
                countryValue = selectedCountry;
        }

        if (idExists && !isEditingOriginal)
        {
            // ID衝突ダイアログを表示
            var conflictDialog = new Window.IdConflictWindow(equipmentId);
            var result = await conflictDialog.ShowDialog<Window.IdConflictWindow.ConflictResolution>(this);

            switch (result)
            {
                case Window.IdConflictWindow.ConflictResolution.Cancel:
                    // キャンセル - 何もせずに戻る
                    return;

                case Window.IdConflictWindow.ConflictResolution.Overwrite:
                    // 上書き保存 - そのまま続行
                    break;

                case Window.IdConflictWindow.ConflictResolution.SaveAsNew:
                    // 別物として保存 - 一意のIDを生成
                    var allIds = dbManager.GetAllEquipmentIds();
                    equipmentId = UniqueIdGenerator.GenerateUniqueId(equipmentId, allIds);
                    break;
            }
        }

        // 全てのパラメータをGun_Processing()に渡すためにデータを収集
        Console.WriteLine("ID: " + equipmentId +
                          "\nName: " + _nameTextBox.Text +
                          "\nCategory: " + ((NavalCategoryItem)_categoryComboBox.SelectedItem).Id +
                          "\nSubCategory: " + _subCategoryComboBox.SelectedItem +
                          "\nYear: " + _yearNumeric.Value +
                          "\nCountry: " + _countryComboBox.SelectedItem +
                          "\nShellWeight: " + _shellWeightNumeric.Value +
                          "\nMuzzleVelocity: " + _muzzleVelocityNumeric.Value +
                          "\nRPM: " + _rpmNumeric.Value +
                          "\nCalibre: " + _calibreNumeric.Value +
                          "\nCalibreType: " + _calibreTypeComboBox.SelectedItem +
                          "\nBarrelCount: " + _barrelCountComboBox.SelectedItem +
                          "\nBarrelLength: " + _barrelLengthNumeric.Value +
                          "\nElevationAngle: " + _elevationAngleNumeric.Value);
                          
        // Tier（開発世代）を年度から計算
        int tier = NavalUtility.GetTierFromYear((int)_yearNumeric.Value);
        
        var gunData = new Dictionary<string, object>
        {
            { "Id", equipmentId }, // 更新されたIDを使用
            { "Name", _nameTextBox.Text },
            { "Category", ((NavalCategoryItem)_categoryComboBox.SelectedItem).Id },
            { "SubCategory", _subCategoryComboBox.SelectedItem.ToString() },
            { "Year", (int)_yearNumeric.Value },
            { "Tier", tier },
            { "Country", countryValue },
            { "ShellWeight", (double)_shellWeightNumeric.Value },
            { "MuzzleVelocity", (double)_muzzleVelocityNumeric.Value },
            { "RPM", (double)_rpmNumeric.Value },
            { "Calibre", (double)_calibreNumeric.Value },
            { "CalibreType", _calibreTypeComboBox.SelectedItem.ToString() },
            { "BarrelCount", int.Parse(_barrelCountComboBox.SelectedItem.ToString()) },
            { "BarrelLength", (double)_barrelLengthNumeric.Value },
            { "ElevationAngle", (double)_elevationAngleNumeric.Value },
            { "TurretWeight", (double)_turretWeightNumeric.Value },
            { "Manpower", (int)_manpowerNumeric.Value },
            { "Steel", (double)_steelNumeric.Value },
            { "Chromium", (double)_chromiumNumeric.Value },
            { "Description", _descriptionTextBox?.Text ?? "" },
            { "IsAsw", _isAswCheckBox.IsChecked ?? false }
        };
        
        // 計算済みの値も追加
        try
        {
            // 口径をmmに変換
            var calibreInMm = NavalUtility.ConvertCalibreToMm((double)_calibreNumeric.Value, _calibreTypeComboBox.SelectedItem.ToString());
            
            // 攻撃力計算
            var shellWeight = (double)_shellWeightNumeric.Value;
            var muzzleVelocity = (double)_muzzleVelocityNumeric.Value;
            var rpm = (double)_rpmNumeric.Value;
            var barrelCount = int.Parse(_barrelCountComboBox.SelectedItem.ToString());
            var attackBase = shellWeight * Math.Pow(muzzleVelocity, 2) / 4000000 * rpm;
            var totalAttack = attackBase * barrelCount;
            
            // 軽/重攻撃力の分配
            double lgAttackValue = 0;
            double hgAttackValue = 0;
            
            if (calibreInMm < 140) {
                lgAttackValue = totalAttack;
            } else if (calibreInMm > 240) {
                hgAttackValue = totalAttack;
            } else {
                var heavyRatio = (calibreInMm - 140) / 100.0;
                lgAttackValue = totalAttack * (1 - heavyRatio);
                hgAttackValue = totalAttack * heavyRatio;
            }
            
            // 装甲貫通計算
            var lgArmorPiercing = lgAttackValue > 0 ? lgAttackValue / Math.Pow(calibreInMm / 10, 2) : 0;
            var hgArmorPiercing = hgAttackValue > 0 ? hgAttackValue / Math.Pow(calibreInMm / 10, 2) : 0;
            var armorPiercingValue = lgArmorPiercing + hgArmorPiercing;
            
            // 射程計算
            var elevationAngle = (double)_elevationAngleNumeric.Value;
            var barrelLength = (double)_barrelLengthNumeric.Value;
            var rangeValue = calibreInMm * muzzleVelocity * barrelLength * elevationAngle / 80000;
            
            // 対空攻撃力計算
            var antiAirAttack = (rpm / 8.5) * (muzzleVelocity / 750) * (elevationAngle / 75) * (100 / calibreInMm) * barrelCount;
            
            // 建造コスト計算
            var turretWeight = (double)_turretWeightNumeric.Value;
            var year = (int)_yearNumeric.Value;
            var buildCost = Math.Pow(calibreInMm / 10, 1.5) + turretWeight / (10 * calibreInMm / 10) + rpm * 0.75 + (year - 1920) / 10;
            
            // 対潜攻撃力計算
            var isAsw = _isAswCheckBox.IsChecked ?? false;
            var subAttack = isAsw ? (shellWeight / 35) * (870 / muzzleVelocity) * (rpm / 5) * 1.25 * barrelCount : 0;
            
            // 計算結果をデータに追加
            gunData["CalculatedAttack"] = totalAttack;
            gunData["CalculatedLgAttack"] = lgAttackValue;
            gunData["CalculatedHgAttack"] = hgAttackValue;
            gunData["CalculatedLgArmorPiercing"] = lgArmorPiercing;
            gunData["CalculatedHgArmorPiercing"] = hgArmorPiercing;
            gunData["CalculatedArmorPiercing"] = armorPiercingValue;
            gunData["CalculatedRange"] = rangeValue;
            gunData["CalculatedAntiAirAttack"] = antiAirAttack;
            gunData["CalculatedBuildCost"] = buildCost;
            gunData["CalculatedSubAttack"] = subAttack;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"計算値の設定中にエラーが発生しました: {ex.Message}");
        }

        // Gun_Processingクラスに全てのデータを渡して処理を行う
        var processedEquipment = GunCalculator.Gun_Processing(gunData);

        // 砲の生データも保存
        GunDataToDb.SaveGunData(processedEquipment, gunData);

        // 処理結果を返して画面を閉じる
        Close(processedEquipment);
    }

    // キャンセルボタンのイベントハンドラ
    public void On_Cancel_Click(object sender, RoutedEventArgs e)
    {
        // キャンセル
        Close();
    }

    
}