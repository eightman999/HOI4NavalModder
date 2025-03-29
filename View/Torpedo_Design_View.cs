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

public partial class Torpedo_Design_View : Avalonia.Controls.Window
{
    private readonly CheckBox _autoGenerateIdCheckBox;
    private readonly TextBox _idTextBox;
    private readonly TextBox _nameTextBox;
    private readonly ComboBox _categoryComboBox;
    private readonly ComboBox _subCategoryComboBox;
    private readonly NumericUpDown _yearNumeric;
    private readonly ComboBox _countryComboBox;

    private readonly NumericUpDown _weightNumeric;
    private readonly NumericUpDown _torpedoSpeedNumeric;
    private readonly NumericUpDown _explosionWeightNumeric;
    private readonly NumericUpDown _lengthNumeric;
    private readonly ComboBox _lengthTypeComboBox;
    private readonly NumericUpDown _rangeNumeric;
    private readonly CheckBox _isAswCheckBox;
    private readonly CheckBox _isAipCheckBox;
    private readonly CheckBox _isOxiCheckBox;
    private readonly CheckBox _isWalCheckBox;
    private readonly CheckBox _isLineCheckBox;
    private readonly CheckBox _isHomingCheckBox;

    private readonly TextBlock _calculatedTorpAttackText;
    private readonly TextBlock _calculatedRangeText;
    private readonly TextBlock _calculatedArmorPiercingText;
    private readonly TextBlock _calculatedBuildCostText;
    private readonly TextBox _descriptionTextBox;

    private readonly Dictionary<string, NavalCategory> _categories;
    private readonly Dictionary<int, string> _tierYears;
    private readonly NavalEquipment _originalEquipment;

    private List<CountryListManager.CountryInfo> _countryInfoList;
    private CountryListManager _countryListManager;

    public Torpedo_Design_View(NavalEquipment equipment, Dictionary<string, NavalCategory> categories, Dictionary<int, string> tierYears)
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

        _weightNumeric = this.FindControl<NumericUpDown>("WeightNumeric");
        _torpedoSpeedNumeric = this.FindControl<NumericUpDown>("TorpedoSpeedNumeric");
        _explosionWeightNumeric = this.FindControl<NumericUpDown>("ExplosionWeightNumeric");
        _lengthNumeric = this.FindControl<NumericUpDown>("LengthNumeric");
        _lengthTypeComboBox = this.FindControl<ComboBox>("LengthTypeComboBox");
        _rangeNumeric = this.FindControl<NumericUpDown>("RangeNumeric");

        _isAswCheckBox = this.FindControl<CheckBox>("IsAswCheckBox");
        _isAipCheckBox = this.FindControl<CheckBox>("IsAipCheckBox");
        _isOxiCheckBox = this.FindControl<CheckBox>("IsOxiCheckBox");
        _isWalCheckBox = this.FindControl<CheckBox>("IsWalCheckBox");
        _isLineCheckBox = this.FindControl<CheckBox>("IsLineCheckBox");
        _isHomingCheckBox = this.FindControl<CheckBox>("IsHomingCheckBox");

        _calculatedTorpAttackText = this.FindControl<TextBlock>("CalculatedTorpAttackText");
        _calculatedRangeText = this.FindControl<TextBlock>("CalculatedRangeText");
        _calculatedArmorPiercingText = this.FindControl<TextBlock>("CalculatedArmorPiercingText");
        _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
        _autoGenerateIdCheckBox = this.FindControl<CheckBox>("AutoGenerateIdCheckBox");

        // カテゴリの設定（魚雷関連のもののみフィルタリング）
        var filteredCategories = new Dictionary<string, NavalCategory>();
        if (_categories.ContainsKey("SMTP")) filteredCategories.Add("SMTP", _categories["SMTP"]);
        if (_categories.ContainsKey("SMSTP")) filteredCategories.Add("SMSTP", _categories["SMSTP"]);

        foreach (var category in filteredCategories)
            _categoryComboBox.Items.Add(new NavalCategoryItem
                { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

        // サブカテゴリの設定（魚雷直径）
        string[] torpedoDiameters = {
            "250mm", "320mm", "323.7mm", "324mm", "350mm", "360mm", "400mm", "438mm", "449mm", "450mm",
            "460mm", "480mm", "500mm", "533mm", "550mm", "600mm", "610mm", "622mm", "650mm", "1000mm"
        };
        foreach (var diameter in torpedoDiameters)
            _subCategoryComboBox.Items.Add(diameter);

        InitializeCountryList();

        // 長さタイプの設定
        _lengthTypeComboBox.Items.Add("m");
        _lengthTypeComboBox.Items.Add("inch");
        _lengthTypeComboBox.Items.Add("cm");
        _lengthTypeComboBox.SelectedIndex = 0; // mをデフォルトに

        // 自動ID生成の初期設定とイベントハンドラ
        if (_originalEquipment != null && !string.IsNullOrEmpty(_originalEquipment.Id))
        {
            // 既存装備を編集する場合
            LoadEquipmentData();
            
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
                _subCategoryComboBox.SelectedIndex = _subCategoryComboBox.Items.IndexOf("533mm"); // 533mmをデフォルトに

            // デフォルトの開発年を設定
            _yearNumeric.Value = 1936;

            if (_countryComboBox.Items.Count > 0)
                _countryComboBox.SelectedIndex = 0;

            // 初期値を設定
            _weightNumeric.Value = 1500; // 1.5トン
            _torpedoSpeedNumeric.Value = 45; // 45ノット
            _explosionWeightNumeric.Value = 300; // 300kg
            _lengthNumeric.Value = 7; // 7m
            _rangeNumeric.Value = 10000; // 10km
        }

        // イベントハンドラの設定
        _categoryComboBox.SelectionChanged += OnCategoryChanged;
        _subCategoryComboBox.SelectionChanged += OnSubCategoryChanged;

        // 自動ID生成のためのイベントハンドラ
        _autoGenerateIdCheckBox.IsCheckedChanged += OnAutoGenerateIdChanged;
        _categoryComboBox.SelectionChanged += UpdateAutoGeneratedId;
        _yearNumeric.ValueChanged += UpdateAutoGeneratedId;
        _subCategoryComboBox.SelectionChanged += UpdateAutoGeneratedId;

        // 性能値計算のためのイベントハンドラ
        _weightNumeric.ValueChanged += UpdateCalculatedValues;
        _torpedoSpeedNumeric.ValueChanged += UpdateCalculatedValues;
        _explosionWeightNumeric.ValueChanged += UpdateCalculatedValues;
        _rangeNumeric.ValueChanged += UpdateCalculatedValues;
        _isAswCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isAipCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isOxiCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isWalCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isLineCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isHomingCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        
        // 初期ID生成（自動生成がオンの場合）
        if (_autoGenerateIdCheckBox.IsChecked == true)
            UpdateAutoGeneratedId(null, EventArgs.Empty);
            
        // 初期計算値更新
        UpdateCalculatedValues(null, EventArgs.Empty);
    }
    
    public Torpedo_Design_View(Dictionary<string, object> rawTorpedoData, Dictionary<string, NavalCategory> categories, Dictionary<int, string> tierYears)
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

        _weightNumeric = this.FindControl<NumericUpDown>("WeightNumeric");
        _torpedoSpeedNumeric = this.FindControl<NumericUpDown>("TorpedoSpeedNumeric");
        _explosionWeightNumeric = this.FindControl<NumericUpDown>("ExplosionWeightNumeric");
        _lengthNumeric = this.FindControl<NumericUpDown>("LengthNumeric");
        _lengthTypeComboBox = this.FindControl<ComboBox>("LengthTypeComboBox");
        _rangeNumeric = this.FindControl<NumericUpDown>("RangeNumeric");

        _isAswCheckBox = this.FindControl<CheckBox>("IsAswCheckBox");
        _isAipCheckBox = this.FindControl<CheckBox>("IsAipCheckBox");
        _isOxiCheckBox = this.FindControl<CheckBox>("IsOxiCheckBox");
        _isWalCheckBox = this.FindControl<CheckBox>("IsWalCheckBox");
        _isLineCheckBox = this.FindControl<CheckBox>("IsLineCheckBox");
        _isHomingCheckBox = this.FindControl<CheckBox>("IsHomingCheckBox");

        _calculatedTorpAttackText = this.FindControl<TextBlock>("CalculatedTorpAttackText");
        _calculatedRangeText = this.FindControl<TextBlock>("CalculatedRangeText");
        _calculatedArmorPiercingText = this.FindControl<TextBlock>("CalculatedArmorPiercingText");
        _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
        _autoGenerateIdCheckBox = this.FindControl<CheckBox>("AutoGenerateIdCheckBox");

        // UI項目の選択肢を初期化
        InitializeUiOptions();
        
        // 生データから値を設定
        if (rawTorpedoData != null)
        {
            PopulateFromRawData(rawTorpedoData);
            
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
        _subCategoryComboBox.SelectionChanged += UpdateAutoGeneratedId;

        // 性能値計算のためのイベントハンドラ
        _weightNumeric.ValueChanged += UpdateCalculatedValues;
        _torpedoSpeedNumeric.ValueChanged += UpdateCalculatedValues;
        _explosionWeightNumeric.ValueChanged += UpdateCalculatedValues;
        _rangeNumeric.ValueChanged += UpdateCalculatedValues;
        _isAswCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isAipCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isOxiCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isWalCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isLineCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isHomingCheckBox.IsCheckedChanged += UpdateCalculatedValues;
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
        // カテゴリの設定（魚雷関連のもののみフィルタリング）
        var filteredCategories = new Dictionary<string, NavalCategory>();
        if (_categories.ContainsKey("SMTP")) filteredCategories.Add("SMTP", _categories["SMTP"]);
        if (_categories.ContainsKey("SMSTP")) filteredCategories.Add("SMSTP", _categories["SMSTP"]);

        foreach (var category in filteredCategories)
            _categoryComboBox.Items.Add(new NavalCategoryItem
                { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

        // サブカテゴリの設定（魚雷直径）
        string[] torpedoDiameters = {
            "250mm", "320mm", "323.7mm", "324mm", "350mm", "360mm", "400mm", "438mm", "449mm", "450mm",
            "460mm", "480mm", "500mm", "533mm", "550mm", "600mm", "610mm", "622mm", "650mm", "1000mm"
        };
        foreach (var diameter in torpedoDiameters)
            _subCategoryComboBox.Items.Add(diameter);

        // 長さタイプの設定
        _lengthTypeComboBox.Items.Add("m");
        _lengthTypeComboBox.Items.Add("inch");
        _lengthTypeComboBox.Items.Add("cm");
        _lengthTypeComboBox.SelectedIndex = 0; // mをデフォルトに
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

        // 魚雷の詳細パラメータを設定
        if (_originalEquipment.AdditionalProperties.ContainsKey("Weight"))
            _weightNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Weight"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("TorpedoSpeed"))
            _torpedoSpeedNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["TorpedoSpeed"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("ExplosionWeight"))
            _explosionWeightNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["ExplosionWeight"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Length"))
            _lengthNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Length"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("LengthType"))
        {
            var lengthType = _originalEquipment.AdditionalProperties["LengthType"].ToString();
            var lengthTypeIndex = _lengthTypeComboBox.Items.IndexOf(lengthType);
            if (lengthTypeIndex >= 0) _lengthTypeComboBox.SelectedIndex = lengthTypeIndex;
        }

        if (_originalEquipment.AdditionalProperties.ContainsKey("Range"))
            _rangeNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Range"]);

        // 特殊フラグの設定
        if (_originalEquipment.AdditionalProperties.ContainsKey("IsAsw"))
            _isAswCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsAsw"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsAip"))
            _isAipCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsAip"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsOxi"))
            _isOxiCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsOxi"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsWal"))
            _isWalCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsWal"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsLine"))
            _isLineCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsLine"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsHoming"))
            _isHomingCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsHoming"]);

        // 既存の計算値がある場合は表示
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedTorpedoAttack"))
            _calculatedTorpAttackText.Text = _originalEquipment.AdditionalProperties["CalculatedTorpedoAttack"].ToString();
        
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedRange"))
            _calculatedRangeText.Text = _originalEquipment.AdditionalProperties["CalculatedRange"] + " km";
        
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedArmorPiercing"))
            _calculatedArmorPiercingText.Text = _originalEquipment.AdditionalProperties["CalculatedArmorPiercing"].ToString();
        
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedBuildCost"))
            _calculatedBuildCostText.Text = _originalEquipment.AdditionalProperties["CalculatedBuildCost"].ToString();
        
        // 備考
        if (_originalEquipment.AdditionalProperties.ContainsKey("Description"))
            _descriptionTextBox.Text = _originalEquipment.AdditionalProperties["Description"].ToString();
        // 既存IDからパラメータを抽出して設定
        if (!string.IsNullOrEmpty(_originalEquipment.Id))
        {
            string category, countryTag, diameter;
            int year;

            if (NavalTorpedoIdGenerator.TryParseTorpedoId(_originalEquipment.Id, 
                    out category, 
                    out countryTag, 
                    out year, 
                    out diameter))
            {
                // カテゴリの選択（既に上で処理済み）

                // 国家タグが取得できた場合は選択
                if (!string.IsNullOrEmpty(countryTag))
                    SetCountrySelection(countryTag);

                // 年の設定
                if (year > 0)
                    _yearNumeric.Value = year;
            
                // 直径の選択
                var diameterIndex = _subCategoryComboBox.Items.IndexOf(diameter);
                if (diameterIndex >= 0)
                    _subCategoryComboBox.SelectedIndex = diameterIndex;
            }
        }
    }

    private void PopulateFromRawData(Dictionary<string, object> rawTorpedoData)
    {
        // ウィンドウタイトルをカテゴリに合わせて設定
        var categoryId = rawTorpedoData["Category"].ToString();
        var categoryName = GetCategoryDisplayName(categoryId);
        Title = $"{categoryName}の編集";

        // 基本情報の設定
        _idTextBox.Text = rawTorpedoData["Id"].ToString();
        _nameTextBox.Text = rawTorpedoData["Name"].ToString();

        // ComboBoxの選択
        UiHelper.SelectComboBoxItem(_categoryComboBox, "Id", categoryId);
        UiHelper.SelectComboBoxItem(_subCategoryComboBox, null, rawTorpedoData["SubCategory"].ToString());
        
        // 開発年を設定
        if (rawTorpedoData.ContainsKey("Year"))
            _yearNumeric.Value = NavalUtility.GetDecimalValue(rawTorpedoData, "Year");
        else
            _yearNumeric.Value = 1936; // デフォルト値
            
        if (rawTorpedoData.ContainsKey("Country") && rawTorpedoData["Country"] != null)
        {
            var countryValue = rawTorpedoData["Country"].ToString();
            SetCountrySelection(countryValue);
        }

        // 数値の設定
        UiHelper.SetNumericValue(_weightNumeric, NavalUtility.GetDecimalValue(rawTorpedoData, "Weight"));
        UiHelper.SetNumericValue(_torpedoSpeedNumeric, NavalUtility.GetDecimalValue(rawTorpedoData, "TorpedoSpeed"));
        UiHelper.SetNumericValue(_explosionWeightNumeric, NavalUtility.GetDecimalValue(rawTorpedoData, "ExplosionWeight"));
        UiHelper.SetNumericValue(_lengthNumeric, NavalUtility.GetDecimalValue(rawTorpedoData, "Length"));
        UiHelper.SelectComboBoxItem(_lengthTypeComboBox, null, rawTorpedoData.ContainsKey("LengthType") ? rawTorpedoData["LengthType"].ToString() : "m");
        UiHelper.SetNumericValue(_rangeNumeric, NavalUtility.GetDecimalValue(rawTorpedoData, "Range"));

        // 特殊フラグの設定
        _isAswCheckBox.IsChecked = rawTorpedoData.ContainsKey("IsAsw") && NavalUtility.GetBooleanValue(rawTorpedoData, "IsAsw");
        _isAipCheckBox.IsChecked = rawTorpedoData.ContainsKey("IsAip") && NavalUtility.GetBooleanValue(rawTorpedoData, "IsAip");
        _isOxiCheckBox.IsChecked = rawTorpedoData.ContainsKey("IsOxi") && NavalUtility.GetBooleanValue(rawTorpedoData, "IsOxi");
        _isWalCheckBox.IsChecked = rawTorpedoData.ContainsKey("IsWal") && NavalUtility.GetBooleanValue(rawTorpedoData, "IsWal");
        _isLineCheckBox.IsChecked = rawTorpedoData.ContainsKey("IsLine") && NavalUtility.GetBooleanValue(rawTorpedoData, "IsLine");
        _isHomingCheckBox.IsChecked = rawTorpedoData.ContainsKey("IsHoming") && NavalUtility.GetBooleanValue(rawTorpedoData, "IsHoming");

        // 計算された性能値
        if (rawTorpedoData.ContainsKey("CalculatedTorpedoAttack"))
            _calculatedTorpAttackText.Text = rawTorpedoData["CalculatedTorpedoAttack"].ToString();

        if (rawTorpedoData.ContainsKey("CalculatedRange"))
            _calculatedRangeText.Text = rawTorpedoData["CalculatedRange"] + " km";

        if (rawTorpedoData.ContainsKey("CalculatedArmorPiercing"))
            _calculatedArmorPiercingText.Text = rawTorpedoData["CalculatedArmorPiercing"].ToString();

        if (rawTorpedoData.ContainsKey("CalculatedBuildCost"))
            _calculatedBuildCostText.Text = rawTorpedoData["CalculatedBuildCost"].ToString();
            
        // 備考欄の設定
        if (rawTorpedoData.ContainsKey("Description"))
            _descriptionTextBox.Text = NavalUtility.GetStringValue(rawTorpedoData, "Description");
    }

    private string GetCategoryDisplayName(string categoryId)
    {
        if (_categories.ContainsKey(categoryId)) return _categories[categoryId].Name;
        return categoryId == "SMTP" ? "魚雷" : "潜水艦魚雷";
    }

    private void SetCountrySelection(string countryValue)
    {
        if (string.IsNullOrEmpty(countryValue))
        {
            // 値が空の場合は「未設定」を選択
            _countryComboBox.SelectedIndex = 0;
            return;
        }

        // CountryListManagerのヘルパーを使用
        UiHelper.SetCountrySelection(_countryComboBox, countryValue, _countryInfoList);
    }

    private void OnCategoryChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_categoryComboBox.SelectedItem is NavalCategoryItem category)
        {
            // カテゴリに応じたデフォルト値を設定
            SetDefaultValuesByCategory(category.Id);

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
        // サブカテゴリが変更されたらIDを更新（自動生成の場合）
        if (_autoGenerateIdCheckBox.IsChecked == true)
            UpdateAutoGeneratedId(null, null);
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

        if (_categoryComboBox.SelectedItem is NavalCategoryItem categoryItem &&
            _yearNumeric.Value.HasValue &&
            _subCategoryComboBox.SelectedItem != null)
        {
            var category = categoryItem.Id;
            var year = (int)_yearNumeric.Value.Value;
            var diameter = _subCategoryComboBox.SelectedItem.ToString();
    
            // 国家タグを取得
            var countryTag = GetSelectedCountryTag();
    
            // 直径から"mm"を取り除く
            if (diameter.EndsWith("mm"))
                diameter = diameter.Substring(0, diameter.Length - 2);
    
            // NavalTorpedoIdGeneratorを使用してIDを生成
            var generatedId = NavalTorpedoIdGenerator.GenerateTorpedoId(
                category, countryTag, year, diameter);

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
    // カテゴリに応じたデフォルト値を設定
    private void SetDefaultValuesByCategory(string categoryId)
    {
        switch (categoryId)
        {
            case "SMTP": // 魚雷
                _weightNumeric.Value = 1500; // 1.5トン
                _torpedoSpeedNumeric.Value = 45; // 45ノット
                _explosionWeightNumeric.Value = 300; // 300kg
                _rangeNumeric.Value = 10000; // 10km
                break;
                
            case "SMSTP": // 潜水艦魚雷
                _weightNumeric.Value = 1700; // 1.7トン
                _torpedoSpeedNumeric.Value = 35; // 35ノット
                _explosionWeightNumeric.Value = 350; // 350kg
                _rangeNumeric.Value = 15000; // 15km
                break;
        }
    }

    // 性能値の計算と表示を更新するメソッド
    private void UpdateCalculatedValues(object sender, EventArgs e)
    {
        try
        {
            if (_categoryComboBox.SelectedItem is not NavalCategoryItem categoryItem)
                return;

            // パラメータの取得
            var torpedoWeight = (double)(_weightNumeric.Value ?? 0);
            var torpedoSpeed = (double)(_torpedoSpeedNumeric.Value ?? 0);
            var explosionWeight = (double)(_explosionWeightNumeric.Value ?? 0);
            var range = (double)(_rangeNumeric.Value ?? 0);
            
            var isAsw = _isAswCheckBox.IsChecked ?? false;
            var isAip = _isAipCheckBox.IsChecked ?? false;
            var isOxi = _isOxiCheckBox.IsChecked ?? false;
            var isWal = _isWalCheckBox.IsChecked ?? false;
            var isLine = _isLineCheckBox.IsChecked ?? false;
            var isHoming = _isHomingCheckBox.IsChecked ?? false;
            
            var category = categoryItem.Id;
            var calibre = 0.0;
            
            // サブカテゴリから口径を取得
            if (_subCategoryComboBox.SelectedItem != null)
            {
                var diameterStr = _subCategoryComboBox.SelectedItem.ToString();
                if (diameterStr.EndsWith("mm"))
                {
                    diameterStr = diameterStr.Substring(0, diameterStr.Length - 2);
                    if (double.TryParse(diameterStr, out var diameter))
                        calibre = diameter;
                }
            }
            
            // 魚雷攻撃力計算（弾頭重量×爆発効率×速度補正）
            var speedModifier = torpedoSpeed / 40.0; // 40ktを基準とした補正
            var explosionEfficiency = 0.075; // 基本効率
            
            // 種類別補正
            if (isOxi) explosionEfficiency *= 1.25; // 酸素魚雷の場合、効率25%増加
            if (isWal) explosionEfficiency *= 1.3; // ヴァルターエンジンの場合、効率30%増加
            if (isAip) explosionEfficiency *= 1.15; // 閉サイクルの場合、効率15%増加
            
            // 誘導方式による補正
            if (isLine) speedModifier *= 0.9; // 有線誘導の場合、速度補正10%減少
            if (isHoming) explosionEfficiency *= 1.2; // 音響ホーミングの場合、効率20%増加
            
            // 最終的な攻撃力計算
            var torpedoAttack = explosionWeight * explosionEfficiency * speedModifier;
            
            // 対潜攻撃力ボーナス
            if (isAsw) torpedoAttack *= 1.5; // 対潜用の場合、攻撃力50%増加
            
            // 装甲貫通力計算（速度×弾頭重量/口径）
            var armorPiercing = torpedoSpeed * explosionWeight / calibre * 0.05;
            
            // 射程を km 単位に変換
            var rangeKm = range / 1000.0;
            
            // 建造コスト計算（重量+弾頭重量×0.3+誘導方式追加）
            var buildCost = torpedoWeight * 0.001 + explosionWeight * 0.0003;
            
            // 誘導方式によるコスト上昇
            if (isLine) buildCost += 1.0;
            if (isHoming) buildCost += 1.5;
            if (isWal) buildCost += 2.0;
            
            // 計算結果をUIに表示（小数点第10位まで表示するフォーマット）
            _calculatedTorpAttackText.Text = torpedoAttack.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedArmorPiercingText.Text = armorPiercing.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedRangeText.Text = rangeKm.ToString("F10").TrimEnd('0').TrimEnd('.') + " km";
            _calculatedBuildCostText.Text = buildCost.ToString("F10").TrimEnd('0').TrimEnd('.');
        }
        catch (Exception ex)
        {
            Console.WriteLine($"計算エラー: {ex.Message}");
        }
    }

    // 保存ボタンのイベントハンドラ
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
                            
        // 国家タグを正しく取得
        var countryTag = GetSelectedCountryTag();
        var countryValue = string.IsNullOrEmpty(countryTag) ? "" : countryTag;

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

        // Tier（開発世代）を年度から計算
        int tier = NavalUtility.GetTierFromYear((int)_yearNumeric.Value);
    
        // 魚雷データを収集
        var torpedoData = new Dictionary<string, object>
        {
            { "Id", equipmentId },
            { "Name", _nameTextBox.Text },
            { "Category", ((NavalCategoryItem)_categoryComboBox.SelectedItem).Id },
            { "SubCategory", _subCategoryComboBox.SelectedItem.ToString() },
            { "Year", (int)_yearNumeric.Value },
            { "Tier", tier },
            { "Country", countryValue }, // 国家タグを正しく設定
            { "Weight", (double)_weightNumeric.Value },
            { "TorpedoSpeed", (double)_torpedoSpeedNumeric.Value },
            { "ExplosionWeight", (double)_explosionWeightNumeric.Value },
            { "Length", (double)_lengthNumeric.Value },
            { "LengthType", _lengthTypeComboBox.SelectedItem.ToString() },
            { "Range", (double)_rangeNumeric.Value },
            { "IsAsw", _isAswCheckBox.IsChecked ?? false },
            { "IsAip", _isAipCheckBox.IsChecked ?? false },
            { "IsOxi", _isOxiCheckBox.IsChecked ?? false },
            { "IsWal", _isWalCheckBox.IsChecked ?? false },
            { "IsLine", _isLineCheckBox.IsChecked ?? false },
            { "IsHoming", _isHomingCheckBox.IsChecked ?? false },
            { "Description", _descriptionTextBox?.Text ?? "" }
        };
    
        // 計算された性能値も追加
        try
        {
            // パラメータの取得
            var torpedoWeight = (double)_weightNumeric.Value;
            var torpedoSpeed = (double)_torpedoSpeedNumeric.Value;
            var explosionWeight = (double)_explosionWeightNumeric.Value;
            var range = (double)_rangeNumeric.Value;
        
            var isAsw = _isAswCheckBox.IsChecked ?? false;
            var isAip = _isAipCheckBox.IsChecked ?? false;
            var isOxi = _isOxiCheckBox.IsChecked ?? false;
            var isWal = _isWalCheckBox.IsChecked ?? false;
            var isLine = _isLineCheckBox.IsChecked ?? false;
            var isHoming = _isHomingCheckBox.IsChecked ?? false;
        
            // サブカテゴリから口径を取得
            var calibre = 0.0;
            if (_subCategoryComboBox.SelectedItem != null)
            {
                var diameterStr = _subCategoryComboBox.SelectedItem.ToString();
                if (diameterStr.EndsWith("mm"))
                {
                    diameterStr = diameterStr.Substring(0, diameterStr.Length - 2);
                    if (double.TryParse(diameterStr, out var diameter))
                        calibre = diameter;
                }
            }
        
            // 魚雷攻撃力計算
            var speedModifier = torpedoSpeed / 40.0;
            var explosionEfficiency = 0.075;
        
            if (isOxi) explosionEfficiency *= 1.25;
            if (isWal) explosionEfficiency *= 1.3;
            if (isAip) explosionEfficiency *= 1.15;
        
            if (isLine) speedModifier *= 0.9;
            if (isHoming) explosionEfficiency *= 1.2;
        
            var torpedoAttack = explosionWeight * explosionEfficiency * speedModifier;
        
            if (isAsw) torpedoAttack *= 1.5;
        
            // 装甲貫通力計算
            var armorPiercing = torpedoSpeed * explosionWeight / calibre * 0.05;
        
            // 射程をkm単位に変換
            var rangeKm = range / 1000.0;
        
            // 建造コスト計算
            var buildCost = torpedoWeight * 0.001 + explosionWeight * 0.0003;
        
            if (isLine) buildCost += 1.0;
            if (isHoming) buildCost += 1.5;
            if (isWal) buildCost += 2.0;
        
            // 計算結果をデータに追加
            torpedoData["CalculatedTorpedoAttack"] = torpedoAttack;
            torpedoData["CalculatedArmorPiercing"] = armorPiercing;
            torpedoData["CalculatedRange"] = rangeKm;
            torpedoData["CalculatedBuildCost"] = buildCost;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"計算値の設定中にエラーが発生しました: {ex.Message}");
        }
    
        // NavalEquipmentオブジェクトを作成
        var equipment = TorpedoCalculator.Torpedo_Processing(torpedoData);

        // 魚雷の生データも保存
        dbManager.SaveRawGunData(equipmentId, torpedoData);
    
        // 処理結果を返して画面を閉じる
        Close(equipment);
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

    // キャンセルボタンのイベントハンドラ
    public void On_Cancel_Click(object sender, RoutedEventArgs e)
    {
        // キャンセル
        Close();
    }
}