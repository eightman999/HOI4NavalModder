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
    public Radar_Design_View(NavalEquipment equipment, Dictionary<string, NavalCategory> categories,
        Dictionary<int, string> tierYears)
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
    public Radar_Design_View(Dictionary<string, object> rawRadarData, Dictionary<string, NavalCategory> categories,
        Dictionary<int, string> tierYears)
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
        _isAllWeatherCheckBox.IsCheckedChanged += UpdateCalculatedValues;
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

            // 国家の選択処理
            // 元の装備データから国家を設定
            if (_originalEquipment != null && !string.IsNullOrEmpty(_originalEquipment.Country))
            {
                SetCountrySelection(_originalEquipment.Country);
            }
            // 生データから国家を設定（コンストラクタで渡された値がある場合）
            else if (this.DataContext is Dictionary<string, object> rawData &&
                     rawData.ContainsKey("Country") &&
                     rawData["Country"] != null)
            {
                string countryValue = rawData["Country"].ToString();
                SetCountrySelection(countryValue);
            }

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

        // レーダーの詳細パラメータを設定
        if (_originalEquipment.AdditionalProperties.ContainsKey("FrequencyBand"))
        {
            var frequencyBand = _originalEquipment.AdditionalProperties["FrequencyBand"].ToString();
            var frequencyBandIndex = _frequencyBandComboBox.Items.IndexOf(frequencyBand);
            if (frequencyBandIndex >= 0) _frequencyBandComboBox.SelectedIndex = frequencyBandIndex;
        }

        if (_originalEquipment.AdditionalProperties.ContainsKey("PowerOutput"))
            _powerOutputNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["PowerOutput"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("AntennaSize"))
            _antennaSizeNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["AntennaSize"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Prf"))
            _prfNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Prf"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("PulseWidth"))
            _pulseWidthNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["PulseWidth"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Weight"))
            _weightNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Weight"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Manpower"))
            _manpowerNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Manpower"]);

        // リソース設定
        if (_originalEquipment.AdditionalProperties.ContainsKey("Steel"))
            _steelNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Steel"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Tungsten"))
            _tungstenNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Tungsten"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Electronics"))
            _electronicsNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Electronics"]);

        // 特殊機能の設定
        if (_originalEquipment.AdditionalProperties.ContainsKey("Is3D"))
            _is3DCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["Is3D"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsDigital"))
            _isDigitalCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsDigital"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsDoppler"))
            _isDopplerCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsDoppler"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsLongRange"))
            _isLongRangeCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsLongRange"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsFireControl"))
            _isFireControlCheckBox.IsChecked =
                Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsFireControl"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsStealth"))
            _isStealthCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsStealth"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsCompact"))
            _isCompactCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsCompact"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsAllWeather"))
            _isAllWeatherCheckBox.IsChecked =
                Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsAllWeather"]);

        // 既存の計算値がある場合は表示
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedSurfaceDetection"))
            _calculatedSurfaceDetectionText.Text =
                _originalEquipment.AdditionalProperties["CalculatedSurfaceDetection"].ToString();

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedAirDetection"))
            _calculatedAirDetectionText.Text =
                _originalEquipment.AdditionalProperties["CalculatedAirDetection"].ToString();

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedDetectionRange"))
            _calculatedDetectionRangeText.Text =
                _originalEquipment.AdditionalProperties["CalculatedDetectionRange"] + " km";

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedFireControl"))
            _calculatedFireControlText.Text =
                _originalEquipment.AdditionalProperties["CalculatedFireControl"].ToString();

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedBuildCost"))
            _calculatedBuildCostText.Text = _originalEquipment.AdditionalProperties["CalculatedBuildCost"].ToString();

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedReliability"))
            _calculatedReliabilityText.Text =
                _originalEquipment.AdditionalProperties["CalculatedReliability"].ToString();

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedVisibilityPenalty"))
            _calculatedVisibilityPenaltyText.Text =
                _originalEquipment.AdditionalProperties["CalculatedVisibilityPenalty"].ToString();

        // 備考
        if (_originalEquipment.AdditionalProperties.ContainsKey("Description"))
            _descriptionTextBox.Text = _originalEquipment.AdditionalProperties["Description"].ToString();
    }

    private string GetCategoryDisplayName(string categoryId)
    {
        if (_categories.ContainsKey(categoryId)) return _categories[categoryId].Name;
        return categoryId == "SMLR" ? "小型電探" : "大型電探";
    }

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
            if (item != null && item.EndsWith($"({countryValue})"))
            {
                _countryComboBox.SelectedIndex = i;
                Console.WriteLine($"タグの完全一致で選択: {item}");
                return;
            }
        }

        // 2. 国家タグが直接マッチする場合
        if (_countryInfoList != null)
        {
            foreach (var country in _countryInfoList)
                if (country.Tag.Equals(countryValue, StringComparison.OrdinalIgnoreCase))
                    for (var i = 0; i < _countryComboBox.Items.Count; i++)
                    {
                        var item = _countryComboBox.Items[i].ToString();
                        if (item != null && item.Contains($"({country.Tag})"))
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
                        if (item != null && item.StartsWith(country.Name))
                        {
                            _countryComboBox.SelectedIndex = i;
                            Console.WriteLine($"国名の直接マッチで選択: {item}");
                            return;
                        }
                    }
        }

        // 4. 部分一致を試みる
        for (var i = 0; i < _countryComboBox.Items.Count; i++)
        {
            var item = _countryComboBox.Items[i].ToString();
            if (item != null && item.Contains(countryValue, StringComparison.OrdinalIgnoreCase))
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
            if (item != null && (item.Contains("その他") || item.Contains("(OTH)")))
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

    private void PopulateFromRawData(Dictionary<string, object> rawRadarData)
    {
        // ウィンドウタイトルをカテゴリに合わせて設定
        var categoryId = rawRadarData["Category"].ToString();
        var categoryName = GetCategoryDisplayName(categoryId);
        Title = $"{categoryName}の編集";

        // 基本情報の設定
        _idTextBox.Text = rawRadarData["Id"].ToString();
        _nameTextBox.Text = rawRadarData["Name"].ToString();

        // ComboBoxの選択
        SelectComboBoxItem(_categoryComboBox, "Id", categoryId);
        SelectComboBoxItem(_subCategoryComboBox, null, rawRadarData["SubCategory"].ToString());

        // 開発年を設定
        if (rawRadarData.ContainsKey("Year"))
            _yearNumeric.Value = NavalUtility.GetDecimalValue(rawRadarData, "Year");
        else
            _yearNumeric.Value = 1936; // デフォルト値

        // 国家選択は後で行う（InitializeCountryListが完了した後）

        // レーダーパラメータの設定
        SelectComboBoxItem(_frequencyBandComboBox, null,
            rawRadarData.ContainsKey("FrequencyBand") ? rawRadarData["FrequencyBand"].ToString() : "L帯");
        UiHelper.SetNumericValue(_powerOutputNumeric, NavalUtility.GetDecimalValue(rawRadarData, "PowerOutput"));
        UiHelper.SetNumericValue(_antennaSizeNumeric, NavalUtility.GetDecimalValue(rawRadarData, "AntennaSize"));
        UiHelper.SetNumericValue(_prfNumeric, NavalUtility.GetDecimalValue(rawRadarData, "Prf"));
        UiHelper.SetNumericValue(_pulseWidthNumeric, NavalUtility.GetDecimalValue(rawRadarData, "PulseWidth"));
        UiHelper.SetNumericValue(_weightNumeric, NavalUtility.GetDecimalValue(rawRadarData, "Weight"));
        UiHelper.SetNumericValue(_manpowerNumeric, NavalUtility.GetDecimalValue(rawRadarData, "Manpower"));

        // リソース設定
        UiHelper.SetNumericValue(_steelNumeric, NavalUtility.GetDecimalValue(rawRadarData, "Steel"));
        UiHelper.SetNumericValue(_tungstenNumeric, NavalUtility.GetDecimalValue(rawRadarData, "Tungsten"));
        UiHelper.SetNumericValue(_electronicsNumeric, NavalUtility.GetDecimalValue(rawRadarData, "Electronics"));

        // 特殊機能の設定
        _is3DCheckBox.IsChecked =
            rawRadarData.ContainsKey("Is3D") && NavalUtility.GetBooleanValue(rawRadarData, "Is3D");
        _isDigitalCheckBox.IsChecked = rawRadarData.ContainsKey("IsDigital") &&
                                       NavalUtility.GetBooleanValue(rawRadarData, "IsDigital");
        _isDopplerCheckBox.IsChecked = rawRadarData.ContainsKey("IsDoppler") &&
                                       NavalUtility.GetBooleanValue(rawRadarData, "IsDoppler");
        _isLongRangeCheckBox.IsChecked = rawRadarData.ContainsKey("IsLongRange") &&
                                         NavalUtility.GetBooleanValue(rawRadarData, "IsLongRange");
        _isFireControlCheckBox.IsChecked = rawRadarData.ContainsKey("IsFireControl") &&
                                           NavalUtility.GetBooleanValue(rawRadarData, "IsFireControl");
        _isStealthCheckBox.IsChecked = rawRadarData.ContainsKey("IsStealth") &&
                                       NavalUtility.GetBooleanValue(rawRadarData, "IsStealth");
        _isCompactCheckBox.IsChecked = rawRadarData.ContainsKey("IsCompact") &&
                                       NavalUtility.GetBooleanValue(rawRadarData, "IsCompact");
        _isAllWeatherCheckBox.IsChecked = rawRadarData.ContainsKey("IsAllWeather") &&
                                          NavalUtility.GetBooleanValue(rawRadarData, "IsAllWeather");

        // 計算された性能値
        if (rawRadarData.ContainsKey("CalculatedSurfaceDetection"))
            _calculatedSurfaceDetectionText.Text = rawRadarData["CalculatedSurfaceDetection"].ToString();

        if (rawRadarData.ContainsKey("CalculatedAirDetection"))
            _calculatedAirDetectionText.Text = rawRadarData["CalculatedAirDetection"].ToString();

        if (rawRadarData.ContainsKey("CalculatedDetectionRange"))
            _calculatedDetectionRangeText.Text = rawRadarData["CalculatedDetectionRange"] + " km";

        if (rawRadarData.ContainsKey("CalculatedFireControl"))
            _calculatedFireControlText.Text = rawRadarData["CalculatedFireControl"].ToString();

        if (rawRadarData.ContainsKey("CalculatedBuildCost"))
            _calculatedBuildCostText.Text = rawRadarData["CalculatedBuildCost"].ToString();

        if (rawRadarData.ContainsKey("CalculatedReliability"))
            _calculatedReliabilityText.Text = rawRadarData["CalculatedReliability"].ToString();

        if (rawRadarData.ContainsKey("CalculatedVisibilityPenalty"))
            _calculatedVisibilityPenaltyText.Text = rawRadarData["CalculatedVisibilityPenalty"].ToString();

        // 備考欄の設定
        if (rawRadarData.ContainsKey("Description"))
            _descriptionTextBox.Text = NavalUtility.GetStringValue(rawRadarData, "Description");
    }

    // SelectComboBoxItemヘルパーメソッドを追加
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
                case "対空型":
                    _is3DCheckBox.IsChecked = true;
                    break;
                case "射撃管制型":
                    _isFireControlCheckBox.IsChecked = true;
                    break;
                case "精密測距型":
                    _isDopplerCheckBox.IsChecked = true;
                    break;
            }
        }

        // 自動生成がオンならIDを更新
        if (_autoGenerateIdCheckBox.IsChecked == true)
            UpdateAutoGeneratedId(null, null);
    }

    private void OnFrequencyBandChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateCalculatedValues(null, null);
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

    // レーダー用のID自動生成メソッド
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

            // 周波数帯を取得
            var frequencyBand = _frequencyBandComboBox.SelectedItem?.ToString() ?? "L帯";

            // サブカテゴリを英語に変換してID生成
            string subCategoryCode;
            switch (subCategory)
            {
                case "対空型": subCategoryCode = "air"; break;
                case "対艦型": subCategoryCode = "surf"; break;
                case "汎用型": subCategoryCode = "gen"; break;
                case "精密測距型": subCategoryCode = "range"; break;
                case "射撃管制型": subCategoryCode = "fc"; break;
                default: subCategoryCode = "gen"; break;
            }

            // 周波数帯を英語に変換
            string frequencyCode;
            switch (frequencyBand)
            {
                case "HF帯": frequencyCode = "hf"; break;
                case "VHF帯": frequencyCode = "vhf"; break;
                case "UHF帯": frequencyCode = "uhf"; break;
                case "L帯": frequencyCode = "l"; break;
                case "S帯": frequencyCode = "s"; break;
                case "C帯": frequencyCode = "c"; break;
                case "X帯": frequencyCode = "x"; break;
                case "Ku帯": frequencyCode = "ku"; break;
                default: frequencyCode = "l"; break;
            }

            // IDを生成（例: smlr_usa_1940_air_l_mk1）
            var generatedId =
                $"{category.ToLower()}_{countryTag.ToLower()}_{year}_{subCategoryCode}_{frequencyCode}_mk1";

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
            case "SMLR": // 小型電探
                _powerOutputNumeric.Value = 5; // 5kW
                _antennaSizeNumeric.Value = 1; // 1m
                _prfNumeric.Value = 1000; // 1000Hz
                _pulseWidthNumeric.Value = 1; // 1μs
                _weightNumeric.Value = 150; // 150kg
                _manpowerNumeric.Value = 2; // 2人
                break;

            case "SMHR": // 大型電探
                _powerOutputNumeric.Value = 25; // 25kW
                _antennaSizeNumeric.Value = 3; // 3m
                _prfNumeric.Value = 500; // 500Hz
                _pulseWidthNumeric.Value = 2; // 2μs
                _weightNumeric.Value = 750; // 750kg
                _manpowerNumeric.Value = 5; // 5人
                break;
        }

        // リソース設定
        switch (categoryId)
        {
            case "SMLR":
                _steelNumeric.Value = 1;
                _tungstenNumeric.Value = 1;
                _electronicsNumeric.Value = 2;
                break;
            case "SMHR":
                _steelNumeric.Value = 3;
                _tungstenNumeric.Value = 2;
                _electronicsNumeric.Value = 4;
                break;
        }
    }

    // 性能値の計算と表示を更新するメソッド
    private void UpdateCalculatedValues(object sender, EventArgs e)
    {
        try
        {
            if (_categoryComboBox.SelectedItem is not NavalCategoryItem categoryItem ||
                _frequencyBandComboBox.SelectedItem == null)
                return;

            // パラメータの取得
            var powerOutput = (double)(_powerOutputNumeric.Value ?? 0);
            var antennaSize = (double)(_antennaSizeNumeric.Value ?? 0);
            var prf = (double)(_prfNumeric.Value ?? 0);
            var pulseWidth = (double)(_pulseWidthNumeric.Value ?? 0);
            var weight = (double)(_weightNumeric.Value ?? 0);
            var frequencyBand = _frequencyBandComboBox.SelectedItem.ToString();
            var year = (int)(_yearNumeric.Value ?? 1936);

            // 特殊機能の取得
            var is3D = _is3DCheckBox.IsChecked ?? false;
            var isDigital = _isDigitalCheckBox.IsChecked ?? false;
            var isDoppler = _isDopplerCheckBox.IsChecked ?? false;
            var isLongRange = _isLongRangeCheckBox.IsChecked ?? false;
            var isFireControl = _isFireControlCheckBox.IsChecked ?? false;
            var isStealth = _isStealthCheckBox.IsChecked ?? false;
            var isCompact = _isCompactCheckBox.IsChecked ?? false;
            var isAllWeather = _isAllWeatherCheckBox.IsChecked ?? false;

            // 周波数帯の係数
            double airDetectionMultiplier = 0;
            double surfaceDetectionMultiplier = 0;
            double rangeMultiplier = 0;
            double weatherResistanceMultiplier = 0;

            switch (frequencyBand)
            {
                case "HF帯":
                    airDetectionMultiplier = 0.1;
                    surfaceDetectionMultiplier = 0.8;
                    rangeMultiplier = 1.5;
                    weatherResistanceMultiplier = 0.5;
                    break;
                case "VHF帯":
                    airDetectionMultiplier = 0.3;
                    surfaceDetectionMultiplier = 0.9;
                    rangeMultiplier = 1.3;
                    weatherResistanceMultiplier = 0.6;
                    break;
                case "UHF帯":
                    airDetectionMultiplier = 0.5;
                    surfaceDetectionMultiplier = 1.0;
                    rangeMultiplier = 1.2;
                    weatherResistanceMultiplier = 0.7;
                    break;
                case "L帯":
                    airDetectionMultiplier = 0.7;
                    surfaceDetectionMultiplier = 1.1;
                    rangeMultiplier = 1.0;
                    weatherResistanceMultiplier = 0.8;
                    break;
                case "S帯":
                    airDetectionMultiplier = 0.9;
                    surfaceDetectionMultiplier = 1.0;
                    rangeMultiplier = 0.9;
                    weatherResistanceMultiplier = 0.8;
                    break;
                case "C帯":
                    airDetectionMultiplier = 1.0;
                    surfaceDetectionMultiplier = 0.9;
                    rangeMultiplier = 0.8;
                    weatherResistanceMultiplier = 0.7;
                    break;
                case "X帯":
                    airDetectionMultiplier = 1.2;
                    surfaceDetectionMultiplier = 0.8;
                    rangeMultiplier = 0.7;
                    weatherResistanceMultiplier = 0.5;
                    break;
                case "Ku帯":
                    airDetectionMultiplier = 1.4;
                    surfaceDetectionMultiplier = 0.6;
                    rangeMultiplier = 0.6;
                    weatherResistanceMultiplier = 0.3;
                    break;
                default:
                    airDetectionMultiplier = 0.7;
                    surfaceDetectionMultiplier = 1.0;
                    rangeMultiplier = 1.0;
                    weatherResistanceMultiplier = 0.8;
                    break;
            }

            // 基本探知力計算
            // レーダー方程式に基づく単純化した計算: 
            // 探知力 ∝ (出力 × アンテナサイズ) / (パルス幅 × PRF)
            var baseDetectionPower = Math.Sqrt(powerOutput * antennaSize) / Math.Sqrt(pulseWidth * prf / 1000);

            // 年代による技術補正
            double techModifier = 1.0;
            if (year < 1930)
                techModifier = 0.5;
            else if (year < 1940)
                techModifier = 0.8;
            else if (year < 1950)
                techModifier = 1.0;
            else if (year < 1960)
                techModifier = 1.2;
            else if (year < 1970)
                techModifier = 1.5;
            else
                techModifier = 2.0;

            if (isDigital)
                techModifier *= 1.5; // デジタル信号処理ボーナス

            // 特殊機能による補正
            double specialModifier = 1.0;

            if (is3D)
                airDetectionMultiplier *= 1.3; // 3D探知は空中目標に特に効果的

            if (isDoppler)
                specialModifier *= 1.2; // ドップラー効果で探知精度向上

            if (isLongRange)
                rangeMultiplier *= 1.5; // 長距離探知ボーナス

            if (isFireControl)
                specialModifier *= 0.8; // 射撃管制は探知力を犠牲に

            if (isStealth)
                specialModifier *= 0.7; // ステルス設計は出力を抑える

            if (isCompact)
                specialModifier *= 0.8; // 小型化は性能を犠牲に

            if (isAllWeather)
                weatherResistanceMultiplier *= 2.0; // 全天候対応

            // 最終探知力計算
            var surfaceDetection = baseDetectionPower * surfaceDetectionMultiplier * techModifier * specialModifier;
            var airDetection = baseDetectionPower * airDetectionMultiplier * techModifier * specialModifier;

            // 探知範囲計算 (km) - 簡易的なレーダー方程式ベース
            var detectionRange = 15 * Math.Sqrt(powerOutput) * Math.Sqrt(antennaSize) * rangeMultiplier * techModifier;

            // 射撃管制能力計算
            var fireControl = baseDetectionPower * 0.5 * techModifier;

            if (isFireControl)
                fireControl *= 2.0; // 射撃管制特化型

            if (isDoppler)
                fireControl *= 1.3; // ドップラーがあると射撃精度向上

            // 建造コスト計算
            var buildCost = weight * 0.004 + powerOutput * 0.05 + antennaSize * 0.1;

            // 特殊機能によるコスト補正
            if (isDigital)
                buildCost *= 1.2;

            if (is3D)
                buildCost *= 1.2;

            if (isDoppler)
                buildCost *= 1.3;

            if (isFireControl)
                buildCost *= 1.4;

            if (isStealth)
                buildCost *= 1.5;

            if (isCompact)
                buildCost *= 0.8;

            if (isAllWeather)
                buildCost *= 1.3;

            // 信頼性計算（0.0～1.0）
            var reliability = 0.7 + (techModifier * 0.2);

            if (isDigital)
                reliability -= 0.1; // 複雑なデジタル回路は故障しやすい

            if (isCompact)
                reliability -= 0.1; // 小型化は故障リスク増加

            if (isAllWeather)
                reliability += 0.1; // 全天候対応は堅牢

            // 天候抵抗値を信頼性に加味
            reliability *= (0.8 + weatherResistanceMultiplier * 0.2);

            // 最大1.0に制限
            reliability = Math.Min(reliability, 1.0);
            // 最小0.1に制限
            reliability = Math.Max(reliability, 0.1);

            // 可視度ペナルティ計算
            var visibilityPenalty = powerOutput * 0.2 + antennaSize * 2.0;

            if (isStealth)
                visibilityPenalty *= 0.5; // ステルス設計

            if (isCompact)
                visibilityPenalty *= 0.8; // 小型化は目立ちにくい

            // 計算結果をUIに表示（小数点第2位まで表示するフォーマット）
            _calculatedSurfaceDetectionText.Text = surfaceDetection.ToString("F2");
            _calculatedAirDetectionText.Text = airDetection.ToString("F2");
            _calculatedDetectionRangeText.Text = detectionRange.ToString("F2") + " km";
            _calculatedFireControlText.Text = fireControl.ToString("F2");
            _calculatedBuildCostText.Text = buildCost.ToString("F2");
            _calculatedReliabilityText.Text = reliability.ToString("F2");
            _calculatedVisibilityPenaltyText.Text = visibilityPenalty.ToString("F2");
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

        // レーダーの周波数帯を文字列で取得
        string frequencyBand = _frequencyBandComboBox.SelectedItem?.ToString() ?? "L帯";

        // レーダーデータを収集
        var radarData = new Dictionary<string, object>
        {
            { "Id", equipmentId },
            { "Name", _nameTextBox.Text },
            { "Category", ((NavalCategoryItem)_categoryComboBox.SelectedItem).Id },
            { "SubCategory", _subCategoryComboBox.SelectedItem.ToString() },
            { "Year", (int)_yearNumeric.Value },
            { "Tier", tier },
            { "Country", countryValue },
            { "FrequencyBand", frequencyBand },
            { "PowerOutput", (double)_powerOutputNumeric.Value },
            { "AntennaSize", (double)_antennaSizeNumeric.Value },
            { "Prf", (double)_prfNumeric.Value },
            { "PulseWidth", (double)_pulseWidthNumeric.Value },
            { "Weight", (double)_weightNumeric.Value },
            { "Manpower", (int)_manpowerNumeric.Value },
            { "Steel", (double)_steelNumeric.Value },
            { "Tungsten", (double)_tungstenNumeric.Value },
            { "Electronics", (double)_electronicsNumeric.Value },
            { "Is3D", _is3DCheckBox.IsChecked ?? false },
            { "IsDigital", _isDigitalCheckBox.IsChecked ?? false },
            { "IsDoppler", _isDopplerCheckBox.IsChecked ?? false },
            { "IsLongRange", _isLongRangeCheckBox.IsChecked ?? false },
            { "IsFireControl", _isFireControlCheckBox.IsChecked ?? false },
            { "IsStealth", _isStealthCheckBox.IsChecked ?? false },
            { "IsCompact", _isCompactCheckBox.IsChecked ?? false },
            { "IsAllWeather", _isAllWeatherCheckBox.IsChecked ?? false },
            { "Description", _descriptionTextBox?.Text ?? "" }
        };

        // 計算された性能値も追加
        try
        {
            // パラメータを取得
            var powerOutput = (double)_powerOutputNumeric.Value;
            var antennaSize = (double)_antennaSizeNumeric.Value;
            var prf = (double)_prfNumeric.Value;
            var pulseWidth = (double)_pulseWidthNumeric.Value;
            var year = (int)_yearNumeric.Value;

            // 特殊機能の取得
            var is3D = _is3DCheckBox.IsChecked ?? false;
            var isDigital = _isDigitalCheckBox.IsChecked ?? false;
            var isDoppler = _isDopplerCheckBox.IsChecked ?? false;
            var isLongRange = _isLongRangeCheckBox.IsChecked ?? false;
            var isFireControl = _isFireControlCheckBox.IsChecked ?? false;
            var isStealth = _isStealthCheckBox.IsChecked ?? false;
            var isCompact = _isCompactCheckBox.IsChecked ?? false;
            var isAllWeather = _isAllWeatherCheckBox.IsChecked ?? false;

            // 周波数帯の係数
            double airDetectionMultiplier = 0;
            double surfaceDetectionMultiplier = 0;
            double rangeMultiplier = 0;
            double weatherResistanceMultiplier = 0;

            switch (frequencyBand)
            {
                case "HF帯":
                    airDetectionMultiplier = 0.1;
                    surfaceDetectionMultiplier = 0.8;
                    rangeMultiplier = 1.5;
                    weatherResistanceMultiplier = 0.5;
                    break;
                case "VHF帯":
                    airDetectionMultiplier = 0.3;
                    surfaceDetectionMultiplier = 0.9;
                    rangeMultiplier = 1.3;
                    weatherResistanceMultiplier = 0.6;
                    break;
                case "UHF帯":
                    airDetectionMultiplier = 0.5;
                    surfaceDetectionMultiplier = 1.0;
                    rangeMultiplier = 1.2;
                    weatherResistanceMultiplier = 0.7;
                    break;
                case "L帯":
                    airDetectionMultiplier = 0.7;
                    surfaceDetectionMultiplier = 1.1;
                    rangeMultiplier = 1.0;
                    weatherResistanceMultiplier = 0.8;
                    break;
                case "S帯":
                    airDetectionMultiplier = 0.9;
                    surfaceDetectionMultiplier = 1.0;
                    rangeMultiplier = 0.9;
                    weatherResistanceMultiplier = 0.8;
                    break;
                case "C帯":
                    airDetectionMultiplier = 1.0;
                    surfaceDetectionMultiplier = 0.9;
                    rangeMultiplier = 0.8;
                    weatherResistanceMultiplier = 0.7;
                    break;
                case "X帯":
                    airDetectionMultiplier = 1.2;
                    surfaceDetectionMultiplier = 0.8;
                    rangeMultiplier = 0.7;
                    weatherResistanceMultiplier = 0.5;
                    break;
                case "Ku帯":
                    airDetectionMultiplier = 1.4;
                    surfaceDetectionMultiplier = 0.6;
                    rangeMultiplier = 0.6;
                    weatherResistanceMultiplier = 0.3;
                    break;
                default:
                    airDetectionMultiplier = 0.7;
                    surfaceDetectionMultiplier = 1.0;
                    rangeMultiplier = 1.0;
                    weatherResistanceMultiplier = 0.8;
                    break;
            }

            // 基本探知力計算
            var baseDetectionPower = Math.Sqrt(powerOutput * antennaSize) / Math.Sqrt(pulseWidth * prf / 1000);

            // 年代による技術補正
            double techModifier = 1.0;
            if (year < 1930)
                techModifier = 0.5;
            else if (year < 1940)
                techModifier = 0.8;
            else if (year < 1950)
                techModifier = 1.0;
            else if (year < 1960)
                techModifier = 1.2;
            else if (year < 1970)
                techModifier = 1.5;
            else
                techModifier = 2.0;

            if (isDigital)
                techModifier *= 1.5; // デジタル信号処理ボーナス

            // 特殊機能による補正
            double specialModifier = 1.0;

            if (is3D)
                airDetectionMultiplier *= 1.3; // 3D探知は空中目標に特に効果的

            if (isDoppler)
                specialModifier *= 1.2; // ドップラー効果で探知精度向上

            if (isLongRange)
                rangeMultiplier *= 1.5; // 長距離探知ボーナス

            if (isFireControl)
                specialModifier *= 0.8; // 射撃管制は探知力を犠牲に

            if (isStealth)
                specialModifier *= 0.7; // ステルス設計は出力を抑える

            if (isCompact)
                specialModifier *= 0.8; // 小型化は性能を犠牲に

            if (isAllWeather)
                weatherResistanceMultiplier *= 2.0; // 全天候対応

            // 最終探知力計算
            var surfaceDetection = baseDetectionPower * surfaceDetectionMultiplier * techModifier * specialModifier;
            var airDetection = baseDetectionPower * airDetectionMultiplier * techModifier * specialModifier;

            // 探知範囲計算 (km)
            var detectionRange = 15 * Math.Sqrt(powerOutput) * Math.Sqrt(antennaSize) * rangeMultiplier * techModifier;

            // 射撃管制能力計算
            var fireControl = baseDetectionPower * 0.5 * techModifier;

            if (isFireControl)
                fireControl *= 2.0; // 射撃管制特化型

            if (isDoppler)
                fireControl *= 1.3; // ドップラーがあると射撃精度向上

            // 建造コスト計算
            var buildCost = (double)_weightNumeric.Value * 0.004 + powerOutput * 0.05 + antennaSize * 0.1;

            // 特殊機能によるコスト補正
            if (isDigital)
                buildCost *= 1.2;

            if (is3D)
                buildCost *= 1.2;

            if (isDoppler)
                buildCost *= 1.3;

            if (isFireControl)
                buildCost *= 1.4;

            if (isStealth)
                buildCost *= 1.5;

            if (isCompact)
                buildCost *= 0.8;

            if (isAllWeather)
                buildCost *= 1.3;

            // 信頼性計算（0.0～1.0）
            var reliability = 0.7 + (techModifier * 0.2);

            if (isDigital)
                reliability -= 0.1; // 複雑なデジタル回路は故障しやすい

            if (isCompact)
                reliability -= 0.1; // 小型化は故障リスク増加

            if (isAllWeather)
                reliability += 0.1; // 全天候対応は堅牢

            // 天候抵抗値を信頼性に加味
            reliability *= (0.8 + weatherResistanceMultiplier * 0.2);

            // 最大1.0に制限
            reliability = Math.Min(reliability, 1.0);
            // 最小0.1に制限
            reliability = Math.Max(reliability, 0.1);

            // 可視度ペナルティ計算
            var visibilityPenalty = powerOutput * 0.2 + antennaSize * 2.0;

            if (isStealth)
                visibilityPenalty *= 0.5; // ステルス設計

            if (isCompact)
                visibilityPenalty *= 0.8; // 小型化は目立ちにくい

            // 計算結果をデータに追加
            radarData["CalculatedSurfaceDetection"] = surfaceDetection;
            radarData["CalculatedAirDetection"] = airDetection;
            radarData["CalculatedDetectionRange"] = detectionRange;
            radarData["CalculatedFireControl"] = fireControl;
            radarData["CalculatedBuildCost"] = buildCost;
            radarData["CalculatedReliability"] = reliability;
            radarData["CalculatedVisibilityPenalty"] = visibilityPenalty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"計算値の設定中にエラーが発生しました: {ex.Message}");
        }

        // NavalEquipmentオブジェクトを作成
        var equipment = RadarCalculator.Radar_Processing(radarData);

        // レーダーの生データも保存
        RadarDataToDb.SaveRadarData(equipment, radarData);

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