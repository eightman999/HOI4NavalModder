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

public partial class Radar_Design_View : Avalonia.Controls.Window
{
    private readonly CheckBox _autoGenerateIdCheckBox;
    private readonly TextBox _idTextBox;
    private readonly TextBox _nameTextBox;
    private readonly ComboBox _categoryComboBox;
    private readonly ComboBox _subCategoryComboBox;
    private readonly NumericUpDown _yearNumeric;
    private readonly ComboBox _countryComboBox;

    // レーダー特有のパラメータコントロール
    private readonly ComboBox _frequencyBandComboBox;
    private readonly NumericUpDown _powerOutputNumeric;
    private readonly NumericUpDown _antennaSizeNumeric;
    private readonly NumericUpDown _prfNumeric;
    private readonly NumericUpDown _pulseWidthNumeric;
    private readonly NumericUpDown _weightNumeric;
    private readonly NumericUpDown _manpowerNumeric;

    // リソース関連コントロール
    private readonly NumericUpDown _steelNumeric;
    private readonly NumericUpDown _tungstenNumeric;
    private readonly NumericUpDown _electronicsNumeric;

    // 計算値表示用コントロール
    private readonly TextBlock _calculatedSurfaceDetectionText;
    private readonly TextBlock _calculatedAirDetectionText;
    private readonly TextBlock _calculatedDetectionRangeText;
    private readonly TextBlock _calculatedFireControlText;
    private readonly TextBlock _calculatedBuildCostText;
    private readonly TextBlock _calculatedReliabilityText;
    private readonly TextBlock _calculatedVisibilityPenaltyText;

    // 特殊機能チェックボックス
    private readonly CheckBox _is3DCheckBox;
    private readonly CheckBox _isDigitalCheckBox;
    private readonly CheckBox _isDopplerCheckBox;
    private readonly CheckBox _isLongRangeCheckBox;
    private readonly CheckBox _isFireControlCheckBox;
    private readonly CheckBox _isStealthCheckBox;
    private readonly CheckBox _isCompactCheckBox;
    private readonly CheckBox _isAllWeatherCheckBox;

    private readonly TextBox _descriptionTextBox;

    private readonly Dictionary<string, NavalCategory> _categories;
    private readonly Dictionary<int, string> _tierYears;
    private readonly NavalEquipment _originalEquipment;

    private List<CountryListManager.CountryInfo> _countryInfoList;
    private CountryListManager _countryListManager;

    /// <summary>
    /// 既存装備の編集用コンストラクタ
    /// </summary>
    public Radar_Design_View(NavalEquipment equipment, Dictionary<string, NavalCategory> categories, Dictionary<int, string> tierYears)
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

        // レーダー特有のパラメータUI要素の取得
        _frequencyBandComboBox = this.FindControl<ComboBox>("FrequencyBandComboBox");
        _powerOutputNumeric = this.FindControl<NumericUpDown>("PowerOutputNumeric");
        _antennaSizeNumeric = this.FindControl<NumericUpDown>("AntennaSizeNumeric");
        _prfNumeric = this.FindControl<NumericUpDown>("PrfNumeric");
        _pulseWidthNumeric = this.FindControl<NumericUpDown>("PulseWidthNumeric");
        _weightNumeric = this.FindControl<NumericUpDown>("WeightNumeric");
        _manpowerNumeric = this.FindControl<NumericUpDown>("ManpowerNumeric");

        // リソース関連UI要素の取得
        _steelNumeric = this.FindControl<NumericUpDown>("SteelNumeric");
        _tungstenNumeric = this.FindControl<NumericUpDown>("TungstenNumeric");
        _electronicsNumeric = this.FindControl<NumericUpDown>("ElectronicsNumeric");

        // 計算値表示用UI要素の取得
        _calculatedSurfaceDetectionText = this.FindControl<TextBlock>("CalculatedSurfaceDetectionText");
        _calculatedAirDetectionText = this.FindControl<TextBlock>("CalculatedAirDetectionText");
        _calculatedDetectionRangeText = this.FindControl<TextBlock>("CalculatedDetectionRangeText");
        _calculatedFireControlText = this.FindControl<TextBlock>("CalculatedFireControlText");
        _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
        _calculatedReliabilityText = this.FindControl<TextBlock>("CalculatedReliabilityText");
        _calculatedVisibilityPenaltyText = this.FindControl<TextBlock>("CalculatedVisibilityPenaltyText");

        // 特殊機能チェックボックスの取得
        _is3DCheckBox = this.FindControl<CheckBox>("Is3DCheckBox");
        _isDigitalCheckBox = this.FindControl<CheckBox>("IsDigitalCheckBox");
        _isDopplerCheckBox = this.FindControl<CheckBox>("IsDopplerCheckBox");
        _isLongRangeCheckBox = this.FindControl<CheckBox>("IsLongRangeCheckBox");
        _isFireControlCheckBox = this.FindControl<CheckBox>("IsFireControlCheckBox");
        _isStealthCheckBox = this.FindControl<CheckBox>("IsStealthCheckBox");
        _isCompactCheckBox = this.FindControl<CheckBox>("IsCompactCheckBox");
        _isAllWeatherCheckBox = this.FindControl<CheckBox>("IsAllWeatherCheckBox");

        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");

        // UI項目の選択肢を初期化
        InitializeUiOptions();
        
        // 生データから値を設定
        if (rawRadarData != null)
        {
            PopulateFromRawData(rawRadarData);
            
            // 編集モードでは自動生成をオフに
            _autoGenerateIdCheckBox.IsChecked = false;
            _idTextBox.IsEnabled = true;
        }

        InitializeCountryList();
        
        // イベントハンドラを設定
        _categoryComboBox.SelectionChanged += OnCategoryChanged;
        _subCategoryComboBox.SelectionChanged += OnSubCategoryChanged;
        _frequencyBandComboBox.SelectionChanged += OnFrequencyBandChanged;

        // 自動ID生成のためのイベントハンドラ
        _autoGenerateIdCheckBox.IsCheckedChanged += OnAutoGenerateIdChanged;
        _categoryComboBox.SelectionChanged += UpdateAutoGeneratedId;
        _yearNumeric.ValueChanged += UpdateAutoGeneratedId;
        _subCategoryComboBox.SelectionChanged += UpdateAutoGeneratedId;

        // 性能値計算のためのイベントハンドラ
        _frequencyBandComboBox.SelectionChanged += UpdateCalculatedValues;
        _powerOutputNumeric.ValueChanged += UpdateCalculatedValues;
        _antennaSizeNumeric.ValueChanged += UpdateCalculatedValues;
        _prfNumeric.ValueChanged += UpdateCalculatedValues;
        _pulseWidthNumeric.ValueChanged += UpdateCalculatedValues;
        _weightNumeric.ValueChanged += UpdateCalculatedValues;
        _yearNumeric.ValueChanged += UpdateCalculatedValues;
        
        // 特殊機能チェックボックスのイベントハンドラ
        _is3DCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDigitalCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDopplerCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isLongRangeCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isFireControlCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isStealthCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isCompactCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isAllWeatherCheckBox.IsCheckedChanged += UpdateCalculatedValues
        CheckBox = this.FindControl<CheckBox>("IsAllWeatherCheckBox");

        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");

        // カテゴリの設定（レーダー関連のもののみフィルタリング）
        var filteredCategories = new Dictionary<string, NavalCategory>();
        if (_categories.ContainsKey("SMLR")) filteredCategories.Add("SMLR", _categories["SMLR"]);
        if (_categories.ContainsKey("SMHR")) filteredCategories.Add("SMHR", _categories["SMHR"]);

        foreach (var category in filteredCategories)
            _categoryComboBox.Items.Add(new NavalCategoryItem
                { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

        // サブカテゴリの設定
        _subCategoryComboBox.Items.Add("対空型");
        _subCategoryComboBox.Items.Add("対艦型");
        _subCategoryComboBox.Items.Add("汎用型");
        _subCategoryComboBox.Items.Add("精密測距型");
        _subCategoryComboBox.Items.Add("射撃管制型");

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
                
            if (_frequencyBandComboBox.Items.Count > 0)
                _frequencyBandComboBox.SelectedIndex = 3; // デフォルトでL帯

            // デフォルトの開発年を設定
            _yearNumeric.Value = 1936;

            if (_countryComboBox.Items.Count > 0)
                _countryComboBox.SelectedIndex = 0;
        }

        // イベントハンドラの設定
        _categoryComboBox.SelectionChanged += OnCategoryChanged;
        _subCategoryComboBox.SelectionChanged += OnSubCategoryChanged;
        _frequencyBandComboBox.SelectionChanged += OnFrequencyBandChanged;

        // 自動ID生成のためのイベントハンドラ
        _autoGenerateIdCheckBox.IsCheckedChanged += OnAutoGenerateIdChanged;
        _categoryComboBox.SelectionChanged += UpdateAutoGeneratedId;
        _yearNumeric.ValueChanged += UpdateAutoGeneratedId;
        _subCategoryComboBox.SelectionChanged += UpdateAutoGeneratedId;

        // 性能値計算のためのイベントハンドラ
        _frequencyBandComboBox.SelectionChanged += UpdateCalculatedValues;
        _powerOutputNumeric.ValueChanged += UpdateCalculatedValues;
        _antennaSizeNumeric.ValueChanged += UpdateCalculatedValues;
        _prfNumeric.ValueChanged += UpdateCalculatedValues;
        _pulseWidthNumeric.ValueChanged += UpdateCalculatedValues;
        _weightNumeric.ValueChanged += UpdateCalculatedValues;
        _yearNumeric.ValueChanged += UpdateCalculatedValues;
        
        // 特殊機能チェックボックスのイベントハンドラ
        _is3DCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDigitalCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDopplerCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isLongRangeCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isFireControlCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isStealthCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isCompactCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isAllWeatherCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        
        // 初期ID生成（自動生成がオンの場合）
        if (_autoGenerateIdCheckBox.IsChecked == true)
            UpdateAutoGeneratedId(null, EventArgs.Empty);
            
        // 初期計算値更新
        UpdateCalculatedValues(null, EventArgs.Empty);
    }

    /// <summary>
    /// 生データから作成するコンストラクタ
    /// </summary>
    public Radar_Design_View(Dictionary<string, object> rawRadarData, Dictionary<string, NavalCategory> categories, Dictionary<int, string> tierYears)
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

        // レーダー特有のパラメータUI要素の取得
        _frequencyBandComboBox = this.FindControl<ComboBox>("FrequencyBandComboBox");
        _powerOutputNumeric = this.FindControl<NumericUpDown>("PowerOutputNumeric");
        _antennaSizeNumeric = this.FindControl<NumericUpDown>("AntennaSizeNumeric");
        _prfNumeric = this.FindControl<NumericUpDown>("PrfNumeric");
        _pulseWidthNumeric = this.FindControl<NumericUpDown>("PulseWidthNumeric");
        _weightNumeric = this.FindControl<NumericUpDown>("WeightNumeric");
        _manpowerNumeric = this.FindControl<NumericUpDown>("ManpowerNumeric");

        // リソース関連UI要素の取得
        _steelNumeric = this.FindControl<NumericUpDown>("SteelNumeric");
        _tungstenNumeric = this.FindControl<NumericUpDown>("TungstenNumeric");
        _electronicsNumeric = this.FindControl<NumericUpDown>("ElectronicsNumeric");

        // 計算値表示用UI要素の取得
        _calculatedSurfaceDetectionText = this.FindControl<TextBlock>("CalculatedSurfaceDetectionText");
        _calculatedAirDetectionText = this.FindControl<TextBlock>("CalculatedAirDetectionText");
        _calculatedDetectionRangeText = this.FindControl<TextBlock>("CalculatedDetectionRangeText");
        _calculatedFireControlText = this.FindControl<TextBlock>("CalculatedFireControlText");
        _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
        _calculatedReliabilityText = this.FindControl<TextBlock>("CalculatedReliabilityText");
        _calculatedVisibilityPenaltyText = this.FindControl<TextBlock>("CalculatedVisibilityPenaltyText");

        // 特殊機能チェックボックスの取得
        _is3DCheckBox = this.FindControl<CheckBox>("Is3DCheckBox");
        _isDigitalCheckBox = this.FindControl<CheckBox>("IsDigitalCheckBox");
        _isDopplerCheckBox = this.FindControl<CheckBox>("IsDopplerCheckBox");
        _isLongRangeCheckBox = this.FindControl<CheckBox>("IsLongRangeCheckBox");
        _isFireControlCheckBox = this.FindControl<CheckBox>("IsFireControlCheckBox");
        _isStealthCheckBox = this.FindControl<CheckBox>("IsStealthCheckBox");
        _isCompactCheckBox = this.FindControl<CheckBox>("IsCompactCheckBox");
        _isAllWeather = this.FindControl<CheckBox>("IsAllWeatherCheckBox");