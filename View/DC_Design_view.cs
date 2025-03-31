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

public partial class DC_Design_View : Avalonia.Controls.Window
{
    private readonly CheckBox _autoGenerateIdCheckBox;
    private readonly TextBox _idTextBox;
    private readonly TextBox _nameTextBox;
    private readonly ComboBox _categoryComboBox;
    private readonly ComboBox _subCategoryComboBox;
    private readonly NumericUpDown _yearNumeric;
    private readonly ComboBox _countryComboBox;

    // 爆雷特有のパラメータコントロール
    private readonly NumericUpDown _explosiveWeightNumeric;
    private readonly NumericUpDown _explosiveEnergyDensityNumeric;
    private readonly NumericUpDown _detectionRangeNumeric;
    private readonly NumericUpDown _weightNumeric;
    private readonly NumericUpDown _manpowerNumeric;

    // リソース関連コントロール
    private readonly NumericUpDown _steelNumeric;
    private readonly NumericUpDown _explosivesNumeric;

    // 計算値表示用コントロール
    private readonly TextBlock _calculatedSubAttackText;
    private readonly TextBlock _calculatedDamageRadiusText;
    private readonly TextBlock _calculatedBuildCostText;
    private readonly TextBlock _calculatedReliabilityText;

    // 特殊機能チェックボックス
    private readonly CheckBox _isReactiveCheckBox;
    private readonly CheckBox _isMultiLayerCheckBox;
    private readonly CheckBox _isDirectionalCheckBox;
    private readonly CheckBox _isAdvancedFuseCheckBox;
    private readonly CheckBox _isDeepWaterCheckBox;

    private readonly TextBox _descriptionTextBox;

    private readonly Dictionary<string, NavalCategory> _categories;
    private readonly Dictionary<int, string> _tierYears;
    private readonly NavalEquipment _originalEquipment;

    private List<CountryListManager.CountryInfo> _countryInfoList;
    private CountryListManager _countryListManager;

    /// <summary>
    /// 既存装備の編集用コンストラクタ
    /// </summary>
    public DC_Design_View(NavalEquipment equipment, Dictionary<string, NavalCategory> categories, Dictionary<int, string> tierYears)
    {
        InitializeComponent();

        _originalEquipment = equipment;
        _categories = categories;
        _tierYears = tierYears;

        // 基本情報UI要素の取得
        _idTextBox = this.FindControl<TextBox>("IdTextBox");
        _nameTextBox = this.FindControl<TextBox>("NameTextBox");
        _categoryComboBox = this.FindControl<ComboBox>("CategoryComboBox");
        _subCategoryComboBox = this.FindControl<ComboBox>("SubCategoryComboBox");
        _yearNumeric = this.FindControl<NumericUpDown>("YearNumeric");
        _countryComboBox = this.FindControl<ComboBox>("CountryComboBox");
        _autoGenerateIdCheckBox = this.FindControl<CheckBox>("AutoGenerateIdCheckBox");

        // 爆雷特有のパラメータUI要素の取得
        _explosiveWeightNumeric = this.FindControl<NumericUpDown>("ExplosiveWeightNumeric");
        _explosiveEnergyDensityNumeric = this.FindControl<NumericUpDown>("ExplosiveEnergyDensityNumeric");
        _detectionRangeNumeric = this.FindControl<NumericUpDown>("DetectionRangeNumeric");
        _weightNumeric = this.FindControl<NumericUpDown>("WeightNumeric");
        _manpowerNumeric = this.FindControl<NumericUpDown>("ManpowerNumeric");

        // リソース関連UI要素の取得
        _steelNumeric = this.FindControl<NumericUpDown>("SteelNumeric");
        _explosivesNumeric = this.FindControl<NumericUpDown>("ExplosivesNumeric");

        // 計算値表示用UI要素の取得
        _calculatedSubAttackText = this.FindControl<TextBlock>("CalculatedSubAttackText");
        _calculatedDamageRadiusText = this.FindControl<TextBlock>("CalculatedDamageRadiusText");
        _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
        _calculatedReliabilityText = this.FindControl<TextBlock>("CalculatedReliabilityText");

        // 特殊機能チェックボックスの取得
        _isReactiveCheckBox = this.FindControl<CheckBox>("IsReactiveCheckBox");
        _isMultiLayerCheckBox = this.FindControl<CheckBox>("IsMultiLayerCheckBox");
        _isDirectionalCheckBox = this.FindControl<CheckBox>("IsDirectionalCheckBox");
        _isAdvancedFuseCheckBox = this.FindControl<CheckBox>("IsAdvancedFuseCheckBox");
        _isDeepWaterCheckBox = this.FindControl<CheckBox>("IsDeepWaterCheckBox");

        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");

        // カテゴリの設定（爆雷関連のもののみフィルタリング）
        var filteredCategories = new Dictionary<string, NavalCategory>();
        if (_categories.ContainsKey("SMDC")) filteredCategories.Add("SMDC", _categories["SMDC"]);
        if (_categories.ContainsKey("SMDCL")) filteredCategories.Add("SMDCL", _categories["SMDCL"]);

        foreach (var category in filteredCategories)
            _categoryComboBox.Items.Add(new NavalCategoryItem
                { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

        // サブカテゴリの設定
        _subCategoryComboBox.Items.Add("標準型");
        _subCategoryComboBox.Items.Add("特殊型");
        _subCategoryComboBox.Items.Add("対潜型");
        _subCategoryComboBox.Items.Add("深海型");
        _subCategoryComboBox.Items.Add("小型");

        InitializeCountryList();

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
                _subCategoryComboBox.SelectedIndex = 0;

            // デフォルトの開発年を設定
            _yearNumeric.Value = 1936;

            if (_countryComboBox.Items.Count > 0)
                _countryComboBox.SelectedIndex = 0;
                
            // 爆雷のデフォルト値
            _explosiveWeightNumeric.Value = 100; // 100kg
            _explosiveEnergyDensityNumeric.Value = 5; // 5 MJ/kg (TNT相当)
            _detectionRangeNumeric.Value = 50; // 50m
            _weightNumeric.Value = 150; // 150kg
            _manpowerNumeric.Value = 3; // 3人
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
        _explosiveWeightNumeric.ValueChanged += UpdateCalculatedValues;
        _explosiveEnergyDensityNumeric.ValueChanged += UpdateCalculatedValues;
        _detectionRangeNumeric.ValueChanged += UpdateCalculatedValues;
        _weightNumeric.ValueChanged += UpdateCalculatedValues;
        _yearNumeric.ValueChanged += UpdateCalculatedValues;
        
        // 特殊機能チェックボックスのイベントハンドラ
        _isReactiveCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isMultiLayerCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDirectionalCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isAdvancedFuseCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDeepWaterCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        
        // 初期ID生成（自動生成がオンの場合）
        if (_autoGenerateIdCheckBox.IsChecked == true)
            UpdateAutoGeneratedId(null, EventArgs.Empty);
            
        // 初期計算値更新
        UpdateCalculatedValues(null, EventArgs.Empty);
    }

    /// <summary>
    /// 生データから作成するコンストラクタ
    /// </summary>
    public DC_Design_View(Dictionary<string, object> rawDCData, Dictionary<string, NavalCategory> categories, Dictionary<int, string> tierYears)
    {
        InitializeComponent();

        _categories = categories;
        _tierYears = tierYears;

        // 基本情報UI要素の取得
        _idTextBox = this.FindControl<TextBox>("IdTextBox");
        _nameTextBox = this.FindControl<TextBox>("NameTextBox");
        _categoryComboBox = this.FindControl<ComboBox>("CategoryComboBox");
        _subCategoryComboBox = this.FindControl<ComboBox>("SubCategoryComboBox");
        _yearNumeric = this.FindControl<NumericUpDown>("YearNumeric");
        _countryComboBox = this.FindControl<ComboBox>("CountryComboBox");
        _autoGenerateIdCheckBox = this.FindControl<CheckBox>("AutoGenerateIdCheckBox");

        // 爆雷特有のパラメータUI要素の取得
        _explosiveWeightNumeric = this.FindControl<NumericUpDown>("ExplosiveWeightNumeric");
        _explosiveEnergyDensityNumeric = this.FindControl<NumericUpDown>("ExplosiveEnergyDensityNumeric");
        _detectionRangeNumeric = this.FindControl<NumericUpDown>("DetectionRangeNumeric");
        _weightNumeric = this.FindControl<NumericUpDown>("WeightNumeric");
        _manpowerNumeric = this.FindControl<NumericUpDown>("ManpowerNumeric");

        // リソース関連UI要素の取得
        _steelNumeric = this.FindControl<NumericUpDown>("SteelNumeric");
        _explosivesNumeric = this.FindControl<NumericUpDown>("ExplosivesNumeric");

        // 計算値表示用UI要素の取得
        _calculatedSubAttackText = this.FindControl<TextBlock>("CalculatedSubAttackText");
        _calculatedDamageRadiusText = this.FindControl<TextBlock>("CalculatedDamageRadiusText");
        _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
        _calculatedReliabilityText = this.FindControl<TextBlock>("CalculatedReliabilityText");

        // 特殊機能チェックボックスの取得
        _isReactiveCheckBox = this.FindControl<CheckBox>("IsReactiveCheckBox");
        _isMultiLayerCheckBox = this.FindControl<CheckBox>("IsMultiLayerCheckBox");
        _isDirectionalCheckBox = this.FindControl<CheckBox>("IsDirectionalCheckBox");
        _isAdvancedFuseCheckBox = this.FindControl<CheckBox>("IsAdvancedFuseCheckBox");
        _isDeepWaterCheckBox = this.FindControl<CheckBox>("IsDeepWaterCheckBox");

        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");

        // UI項目の選択肢を初期化
        InitializeUiOptions();
        
        // 生データから値を設定
        if (rawDCData != null)
        {
            PopulateFromRawData(rawDCData);
            
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
        _explosiveWeightNumeric.ValueChanged += UpdateCalculatedValues;
        _explosiveEnergyDensityNumeric.ValueChanged += UpdateCalculatedValues;
        _detectionRangeNumeric.ValueChanged += UpdateCalculatedValues;
        _weightNumeric.ValueChanged += UpdateCalculatedValues;
        _yearNumeric.ValueChanged += UpdateCalculatedValues;
        
        // 特殊機能チェックボックスのイベントハンドラ
        _isReactiveCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isMultiLayerCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDirectionalCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isAdvancedFuseCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDeepWaterCheckBox.IsCheckedChanged += UpdateCalculatedValues;
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
        // カテゴリの設定（爆雷関連のもののみフィルタリング）
        var filteredCategories = new Dictionary<string, NavalCategory>();
        if (_categories.ContainsKey("SMDC")) filteredCategories.Add("SMDC", _categories["SMDC"]);
        if (_categories.ContainsKey("SMDCL")) filteredCategories.Add("SMDCL", _categories["SMDCL"]);

        foreach (var category in filteredCategories)
            _categoryComboBox.Items.Add(new NavalCategoryItem
                { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

        // サブカテゴリの設定
        _subCategoryComboBox.Items.Add("標準型");
        _subCategoryComboBox.Items.Add("特殊型");
        _subCategoryComboBox.Items.Add("対潜型");
        _subCategoryComboBox.Items.Add("深海型");
        _subCategoryComboBox.Items.Add("小型");
    }

    private async void InitializeCountryList()
    {
        try
        {
            // 設定からパスを取得
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HOI4NavalModder");

            var settingsPath = Path.Combine(appDataPath, "modpaths.json");

            // 設定ファイルからパスを直接読み込む
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

        // 爆雷の詳細パラメータを設定
        if (_originalEquipment.AdditionalProperties.ContainsKey("ExplosiveWeight"))
            _explosiveWeightNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["ExplosiveWeight"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("ExplosiveEnergyDensity"))
            _explosiveEnergyDensityNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["ExplosiveEnergyDensity"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("DetectionRange"))
            _detectionRangeNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["DetectionRange"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Weight"))
            _weightNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Weight"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Manpower"))
            _manpowerNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Manpower"]);

        // リソース設定
        if (_originalEquipment.AdditionalProperties.ContainsKey("Steel"))
            _steelNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Steel"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Explosives"))
            _explosivesNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Explosives"]);

        // 特殊機能の設定
        if (_originalEquipment.AdditionalProperties.ContainsKey("IsReactive"))
            _isReactiveCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsReactive"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsMultiLayer"))
            _isMultiLayerCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsMultiLayer"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsDirectional"))
            _isDirectionalCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsDirectional"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsAdvancedFuse"))
            _isAdvancedFuseCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsAdvancedFuse"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsDeepWater"))
            _isDeepWaterCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsDeepWater"]);

        // 既存の計算値がある場合は表示
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedSubAttack"))
            _calculatedSubAttackText.Text = _originalEquipment.AdditionalProperties["CalculatedSubAttack"].ToString();
        
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedDamageRadius"))
            _calculatedDamageRadiusText.Text = _originalEquipment.AdditionalProperties["CalculatedDamageRadius"] + " m";
        
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedBuildCost"))
            _calculatedBuildCostText.Text = _originalEquipment.AdditionalProperties["CalculatedBuildCost"].ToString();
        
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedReliability"))
            _calculatedReliabilityText.Text = _originalEquipment.AdditionalProperties["CalculatedReliability"].ToString();
        
        // 備考
        if (_originalEquipment.AdditionalProperties.ContainsKey("Description"))
            _descriptionTextBox.Text = _originalEquipment.AdditionalProperties["Description"].ToString();
    }

    private void PopulateFromRawData(Dictionary<string, object> rawDCData)
    {
        // ウィンドウタイトルをカテゴリに合わせて設定
        var categoryId = rawDCData["Category"].ToString();
        var categoryName = GetCategoryDisplayName(categoryId);
        Title = $"{categoryName}の編集";

        // 基本情報の設定
        _idTextBox.Text = rawDCData["Id"].ToString();
        _nameTextBox.Text = rawDCData["Name"].ToString();

        // ComboBoxの選択
        UiHelper.SelectComboBoxItem(_categoryComboBox, "Id", categoryId);
        UiHelper.SelectComboBoxItem(_subCategoryComboBox, null, rawDCData["SubCategory"].ToString());
        
        // 開発年を設定
        if (rawDCData.ContainsKey("Year"))
            _yearNumeric.Value = NavalUtility.GetDecimalValue(rawDCData, "Year");
        else
            _yearNumeric.Value = 1936; // デフォルト値
            
        if (rawDCData.ContainsKey("Country") && rawDCData["Country"] != null)
        {
            var countryValue = rawDCData["Country"].ToString();
            SetCountrySelection(countryValue);
        }

        // 爆雷パラメータの設定
        UiHelper.SetNumericValue(_explosiveWeightNumeric, NavalUtility.GetDecimalValue(rawDCData, "ExplosiveWeight"));
        UiHelper.SetNumericValue(_explosiveEnergyDensityNumeric, NavalUtility.GetDecimalValue(rawDCData, "ExplosiveEnergyDensity"));
        UiHelper.SetNumericValue(_detectionRangeNumeric, NavalUtility.GetDecimalValue(rawDCData, "DetectionRange"));
        UiHelper.SetNumericValue(_weightNumeric, NavalUtility.GetDecimalValue(rawDCData, "Weight"));
        UiHelper.SetNumericValue(_manpowerNumeric, NavalUtility.GetDecimalValue(rawDCData, "Manpower"));
        
        // リソース設定
        UiHelper.SetNumericValue(_steelNumeric, NavalUtility.GetDecimalValue(rawDCData, "Steel"));
        UiHelper.SetNumericValue(_explosivesNumeric, NavalUtility.GetDecimalValue(rawDCData, "Explosives"));

        // 特殊機能の設定
        _isReactiveCheckBox.IsChecked = rawDCData.ContainsKey("IsReactive") && NavalUtility.GetBooleanValue(rawDCData, "IsReactive");
        _isMultiLayerCheckBox.IsChecked = rawDCData.ContainsKey("IsMultiLayer") && NavalUtility.GetBooleanValue(rawDCData, "IsMultiLayer");
        _isDirectionalCheckBox.IsChecked = rawDCData.ContainsKey("IsDirectional") && NavalUtility.GetBooleanValue(rawDCData, "IsDirectional");
        _isAdvancedFuseCheckBox.IsChecked = rawDCData.ContainsKey("IsAdvancedFuse") && NavalUtility.GetBooleanValue(rawDCData, "IsAdvancedFuse");
        _isDeepWaterCheckBox.IsChecked = rawDCData.ContainsKey("IsDeepWater") && NavalUtility.GetBooleanValue(rawDCData, "IsDeepWater");

        // 計算された性能値
        if (rawDCData.ContainsKey("CalculatedSubAttack"))
            _calculatedSubAttackText.Text = rawDCData["CalculatedSubAttack"].ToString();

        if (rawDCData.ContainsKey("CalculatedDamageRadius"))
            _calculatedDamageRadiusText.Text = rawDCData["CalculatedDamageRadius"] + " m";

        if (rawDCData.ContainsKey("CalculatedBuildCost"))
            _calculatedBuildCostText.Text = rawDCData["CalculatedBuildCost"].ToString();

        if (rawDCData.ContainsKey("CalculatedReliability"))
            _calculatedReliabilityText.Text = rawDCData["CalculatedReliability"].ToString();
            
        // 備考欄の設定
        if (rawDCData.ContainsKey("Description"))
            _descriptionTextBox.Text = NavalUtility.GetStringValue(rawDCData, "Description");
    }

    private string GetCategoryDisplayName(string categoryId)
    {
        if (_categories.ContainsKey(categoryId)) return _categories[categoryId].Name;
        return categoryId == "SMDC" ? "爆雷" : "爆雷投射機";
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
        // サブカテゴリに応じた値や機能設定
        if (_subCategoryComboBox.SelectedItem != null)
        {
            var subCategory = _subCategoryComboBox.SelectedItem.ToString();

            switch (subCategory)
            {
                case "深海型":
                    _isDeepWaterCheckBox.IsChecked = true;
                    break;
                case "対潜型":
                    // 対潜型の場合は探知範囲を強化
                    _detectionRangeNumeric.Value = Math.Max(_detectionRangeNumeric.Value ?? 0, 80m);
                    break;
                case "特殊型":
                    // 特殊型は指向性や多層などの特殊機能を持つ可能性が高い
                    _isDirectionalCheckBox.IsChecked = true;
                    break;
            }
        }
        
        // 自動生成がオンならIDを更新
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

    // 爆雷用のID自動生成メソッド
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
            var subCategory = _subCategoryComboBox.SelectedItem.ToString();
    
            // 国家タグを取得
            var countryTag = GetSelectedCountryTag();
    
            // サブカテゴリを英語に変換してID生成
            string subCategoryCode;
            switch (subCategory)
            {
                case "標準型": subCategoryCode = "std"; break;
                case "特殊型": subCategoryCode = "spec"; break;
                case "対潜型": subCategoryCode = "asw"; break;
                case "深海型": subCategoryCode = "deep"; break;
                case "小型": subCategoryCode = "small"; break;
                default: subCategoryCode = "std"; break;
            }
            
            // 爆薬重量を取得
            var explosiveWeight = (int)(_explosiveWeightNumeric.Value ?? 100);
            var weightCode = explosiveWeight.ToString();
            
            // IDを生成（例: smdc_usa_1940_asw_150_mk1）
            var generatedId = $"{category.ToLower()}_{countryTag.ToLower()}_{year}_{subCategoryCode}_{weightCode}_mk1";

            // テキストボックスに設定
            _idTextBox.Text = generatedId;
        }
    }

    // 選択された国家タグを取得するメソッド
    private string GetSelectedCountryTag()
    {
        if (_countryComboBox.SelectedIndex <= 0 || _countryComboBox.SelectedItem == null)
            // 未選択または「未設定」の場合
            return "gen";

        var selectedCountry = _countryComboBox.SelectedItem.ToString();

        // 表示名から国家タグを抽出（例: "日本 (JAP)" → "JAP"）
        var startIndex = selectedCountry.LastIndexOf('(');
        var endIndex = selectedCountry.LastIndexOf(')');

        if (startIndex > 0 && endIndex > startIndex)
        {
            var tag = selectedCountry.Substring(startIndex + 1, endIndex - startIndex - 1);
            return tag;
        }

        // タグが見つからない場合は gen（generic）を返す
        return "gen";
    }

    // カテゴリに応じたデフォルト値を設定
    private void SetDefaultValuesByCategory(string categoryId)
    {
        switch (categoryId)
        {
            case "SMDC": // 爆雷
                _explosiveWeightNumeric.Value = 100; // 100kg
                _explosiveEnergyDensityNumeric.Value = 5; // 5 MJ/kg
                _detectionRangeNumeric.Value = 50; // 50m
                _weightNumeric.Value = 150; // 150kg
                _manpowerNumeric.Value = 3; // 3人
                break;
                
            case "SMDCL": // 爆雷投射機
                _explosiveWeightNumeric.Value = 80; // 80kg
                _explosiveEnergyDensityNumeric.Value = 5; // 5 MJ/kg
                _detectionRangeNumeric.Value = 70; // 70m (投射機なので射程が長い)
                _weightNumeric.Value = 250; // 250kg
                _manpowerNumeric.Value = 5; // 5人
                break;
        }
        
        // リソース設定
        switch (categoryId)
        {
            case "SMDC":
                _steelNumeric.Value = 1;
                _explosivesNumeric.Value = 2;
                break;
            case "SMDCL":
                _steelNumeric.Value = 2;
                _explosivesNumeric.Value = 2;
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
            var explosiveWeight = (double)(_explosiveWeightNumeric.Value ?? 0);
            var explosiveEnergyDensity = (double)(_explosiveEnergyDensityNumeric.Value ?? 0);
            var detectionRange = (double)(_detectionRangeNumeric.Value ?? 0);
            var weight = (double)(_weightNumeric.Value ?? 0);
            var year = (int)(_yearNumeric.Value ?? 1936);
            
            // 特殊機能の取得
            var isReactive = _isReactiveCheckBox.IsChecked ?? false;
            var isMultiLayer = _isMultiLayerCheckBox.IsChecked ?? false;
            var isDirectional = _isDirectionalCheckBox.IsChecked ?? false;
            var isAdvancedFuse = _isAdvancedFuseCheckBox.IsChecked ?? false;
            var isDeepWater = _isDeepWaterCheckBox.IsChecked ?? false;
            
            // 年代による技術補正
            double techModifier = 1.0;
            if (year < 1930) 
                techModifier = 0.7;
            else if (year < 1940) 
                techModifier = 0.9;
            else if (year < 1950) 
                techModifier = 1.0;
            else if (year < 1960) 
                techModifier = 1.2;
            else if (year < 1970)
                techModifier = 1.3;
            else 
                techModifier = 1.5;
            
            // カテゴリによる基本補正
            double categoryModifier = 1.0;
            if (categoryItem.Id == "SMDCL") // 爆雷投射機の場合
                categoryModifier = 1.1;
                
            // 特殊機能による補正
            double specialModifier = 1.0;
            
            if (isReactive)
                specialModifier *= 1.3; // 反応型爆雷ボーナス
                
            if (isMultiLayer)
                specialModifier *= 1.2; // 多層爆雷ボーナス
                
            if (isDirectional)
                specialModifier *= 1.5; // 指向性ボーナス（方向を絞った分、威力増加）
                
            if (isAdvancedFuse)
                specialModifier *= 1.15; // 高性能信管ボーナス
                
            if (isDeepWater)
                specialModifier *= 1.25; // 深海型ボーナス
                
            // 爆発力の計算 = 炸薬重量 × エネルギー密度 × 各種修正
            var explosivePower = explosiveWeight * explosiveEnergyDensity * techModifier * categoryModifier * specialModifier;
            
            // 対潜攻撃力の計算
            var subAttack = explosivePower * 0.1; // 係数は適宜調整
            
            // 被害範囲の計算 (meters)
            var damageRadius = Math.Pow(explosiveWeight * explosiveEnergyDensity, 1/3.0) * 2.5; // 立方根に比例
            
            // 指向性爆雷は半径が小さい
            if (isDirectional)
                damageRadius *= 0.7;
                
            // 建造コスト計算
            var buildCost = 0.5 + (weight * 0.002) + (explosiveWeight * explosiveEnergyDensity * 0.0005);
            
            // 信頼性計算（0.0～1.0）
            var reliability = 0.8 + (techModifier * 0.1);
            
            if (isAdvancedFuse)
                reliability -= 0.05; // 高性能信管は複雑で信頼性が下がる
                
            if (isMultiLayer)
                reliability -= 0.05; // 多層構造も複雑で信頼性が下がる
                
            // 最大1.0に制限
            reliability = Math.Min(Math.Max(reliability, 0.6), 1.0);
            
            // 計算結果をUIに表示（小数点第10位まで表示するフォーマット）
            _calculatedSubAttackText.Text = subAttack.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedDamageRadiusText.Text = damageRadius.ToString("F10").TrimEnd('0').TrimEnd('.') + " m";
            _calculatedBuildCostText.Text = buildCost.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedReliabilityText.Text = reliability.ToString("F10").TrimEnd('0').TrimEnd('.');
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
        var countryValue = string.IsNullOrEmpty(countryTag) || countryTag == "gen" ? "" : countryTag;

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
    
        // 爆雷データを収集
        var dcData = new Dictionary<string, object>
        {
            { "Id", equipmentId },
            { "Name", _nameTextBox.Text },
            { "Category", ((NavalCategoryItem)_categoryComboBox.SelectedItem).Id },
            { "SubCategory", _subCategoryComboBox.SelectedItem.ToString() },
            { "Year", (int)_yearNumeric.Value },
            { "Tier", tier },
            { "Country", countryValue },
            { "ExplosiveWeight", (double)_explosiveWeightNumeric.Value },
            { "ExplosiveEnergyDensity", (double)_explosiveEnergyDensityNumeric.Value },
            { "DetectionRange", (double)_detectionRangeNumeric.Value },
            { "Weight", (double)_weightNumeric.Value },
            { "Manpower", (int)_manpowerNumeric.Value },
            { "Steel", (double)_steelNumeric.Value },
            { "Explosives", (double)_explosivesNumeric.Value },
            { "IsReactive", _isReactiveCheckBox.IsChecked ?? false },
            { "IsMultiLayer", _isMultiLayerCheckBox.IsChecked ?? false },
            { "IsDirectional", _isDirectionalCheckBox.IsChecked ?? false },
            { "IsAdvancedFuse", _isAdvancedFuseCheckBox.IsChecked ?? false },
            { "IsDeepWater", _isDeepWaterCheckBox.IsChecked ?? false },
            { "Description", _descriptionTextBox?.Text ?? "" }
        };
    
        // 計算された性能値も追加
        try
        {
            // パラメータの取得
            var explosiveWeight = (double)_explosiveWeightNumeric.Value;
            var explosiveEnergyDensity = (double)_explosiveEnergyDensityNumeric.Value;
            var detectionRange = (double)_detectionRangeNumeric.Value;
            var weight = (double)_weightNumeric.Value;
            var year = (int)_yearNumeric.Value;
            
            // 特殊機能の取得
            var isReactive = _isReactiveCheckBox.IsChecked ?? false;
            var isMultiLayer = _isMultiLayerCheckBox.IsChecked ?? false;
            var isDirectional = _isDirectionalCheckBox.IsChecked ?? false;
            var isAdvancedFuse = _isAdvancedFuseCheckBox.IsChecked ?? false;
            var isDeepWater = _isDeepWaterCheckBox.IsChecked ?? false;
            
            // 年代による技術補正
            double techModifier = 1.0;
            if (year < 1930) 
                techModifier = 0.7;
            else if (year < 1940) 
                techModifier = 0.9;
            else if (year < 1950) 
                techModifier = 1.0;
            else if (year < 1960) 
                techModifier = 1.2;
            else if (year < 1970)
                techModifier = 1.3;
            else 
                techModifier = 1.5;
            
            // カテゴリによる基本補正
            double categoryModifier = 1.0;
            if (((NavalCategoryItem)_categoryComboBox.SelectedItem).Id == "SMDCL") // 爆雷投射機の場合
                categoryModifier = 1.1;
                
            // 特殊機能による補正
            double specialModifier = 1.0;
            
            if (isReactive)
                specialModifier *= 1.3; // 反応型爆雷ボーナス
                
            if (isMultiLayer)
                specialModifier *= 1.2; // 多層爆雷ボーナス
                
            if (isDirectional)
                specialModifier *= 1.5; // 指向性ボーナス（方向を絞った分、威力増加）
                
            if (isAdvancedFuse)
                specialModifier *= 1.15; // 高性能信管ボーナス
                
            if (isDeepWater)
                specialModifier *= 1.25; // 深海型ボーナス
                
            // 爆発力の計算 = 炸薬重量 × エネルギー密度 × 各種修正
            var explosivePower = explosiveWeight * explosiveEnergyDensity * techModifier * categoryModifier * specialModifier;
            
            // 対潜攻撃力の計算
            var subAttack = explosivePower * 0.1; // 係数は適宜調整
            
            // 被害範囲の計算 (meters)
            var damageRadius = Math.Pow(explosiveWeight * explosiveEnergyDensity, 1/3.0) * 2.5; // 立方根に比例
            
            // 指向性爆雷は半径が小さい
            if (isDirectional)
                damageRadius *= 0.7;
                
            // 建造コスト計算
            var buildCost = 0.5 + (weight * 0.002) + (explosiveWeight * explosiveEnergyDensity * 0.0005);
            
            // 信頼性計算（0.0～1.0）
            var reliability = 0.8 + (techModifier * 0.1);
            
            if (isAdvancedFuse)
                reliability -= 0.05; // 高性能信管は複雑で信頼性が下がる
                
            if (isMultiLayer)
                reliability -= 0.05; // 多層構造も複雑で信頼性が下がる
                
            // 最大1.0に制限
            reliability = Math.Min(Math.Max(reliability, 0.6), 1.0);
            
            // 計算結果をデータに追加
            dcData["CalculatedSubAttack"] = subAttack;
            dcData["CalculatedDamageRadius"] = damageRadius;
            dcData["CalculatedBuildCost"] = buildCost;
            dcData["CalculatedReliability"] = reliability;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"計算値の設定中にエラーが発生しました: {ex.Message}");
        }
    
        // NavalEquipmentオブジェクトを作成
        var equipment = DepthChargeCalculator.DC_Processing(dcData);

        // 爆雷の生データも保存
        DCDataToDb.SaveDCData(equipment, dcData);
    
        // 処理結果を返して画面を閉じる
        Close(equipment);
    }
    
    // キャンセルボタンのイベントハンドラ
    public void On_Cancel_Click(object sender, RoutedEventArgs e)
    {
        // キャンセル
        Close();
    }
}