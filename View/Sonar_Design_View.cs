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

public partial class Sonar_Design_View : Avalonia.Controls.Window
{
    private readonly CheckBox _autoGenerateIdCheckBox;
    private readonly TextBlock _calculatedBuildCostText;
    private readonly TextBlock _calculatedDetectionRangeText;
    private readonly TextBlock _calculatedReliabilityText;
    private readonly TextBlock _calculatedSubAttackText;

    // 計算値表示用コントロール
    private readonly TextBlock _calculatedSubDetectionText;
    private readonly TextBlock _calculatedSurfaceDetectionText;

    private readonly Dictionary<string, NavalCategory> _categories;
    private readonly ComboBox _categoryComboBox;
    private readonly ComboBox _countryComboBox;

    private readonly TextBox _descriptionTextBox;
    private readonly NumericUpDown _detectionPowerNumeric;
    private readonly NumericUpDown _detectionSpeedNumeric;
    private readonly NumericUpDown _electronicsNumeric;

    // ソナー特有のパラメータコントロール
    private readonly NumericUpDown _frequencyNumeric;
    private readonly TextBox _idTextBox;
    private readonly CheckBox _isDigitalCheckBox;
    private readonly CheckBox _isHighFrequencyCheckBox;
    private readonly CheckBox _isLongRangeCheckBox;

    // 特殊機能チェックボックス
    private readonly CheckBox _isNoiseReductionCheckBox;
    private readonly CheckBox _isTowedArrayCheckBox;
    private readonly NumericUpDown _manpowerNumeric;
    private readonly TextBox _nameTextBox;
    private readonly NavalEquipment _originalEquipment;
    private readonly ComboBox _sonarTypeComboBox;

    // リソース関連コントロール
    private readonly NumericUpDown _steelNumeric;
    private readonly ComboBox _subCategoryComboBox;
    private readonly Dictionary<int, string> _tierYears;
    private readonly NumericUpDown _tungstenNumeric;
    private readonly NumericUpDown _weightNumeric;
    private readonly NumericUpDown _yearNumeric;

    private List<CountryListManager.CountryInfo> _countryInfoList;
    private CountryListManager _countryListManager;

    /// <summary>
    ///     既存装備の編集用コンストラクタ
    /// </summary>
    public Sonar_Design_View(NavalEquipment equipment, Dictionary<string, NavalCategory> categories,
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

        // ソナー特有のパラメータUI要素の取得
        _frequencyNumeric = this.FindControl<NumericUpDown>("FrequencyNumeric");
        _detectionPowerNumeric = this.FindControl<NumericUpDown>("DetectionPowerNumeric");
        _detectionSpeedNumeric = this.FindControl<NumericUpDown>("DetectionSpeedNumeric");
        _weightNumeric = this.FindControl<NumericUpDown>("WeightNumeric");
        _sonarTypeComboBox = this.FindControl<ComboBox>("SonarTypeComboBox");
        _manpowerNumeric = this.FindControl<NumericUpDown>("ManpowerNumeric");

        // リソース関連UI要素の取得
        _steelNumeric = this.FindControl<NumericUpDown>("SteelNumeric");
        _tungstenNumeric = this.FindControl<NumericUpDown>("TungstenNumeric");
        _electronicsNumeric = this.FindControl<NumericUpDown>("ElectronicsNumeric");

        // 計算値表示用UI要素の取得
        _calculatedSubDetectionText = this.FindControl<TextBlock>("CalculatedSubDetectionText");
        _calculatedSurfaceDetectionText = this.FindControl<TextBlock>("CalculatedSurfaceDetectionText");
        _calculatedDetectionRangeText = this.FindControl<TextBlock>("CalculatedDetectionRangeText");
        _calculatedSubAttackText = this.FindControl<TextBlock>("CalculatedSubAttackText");
        _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
        _calculatedReliabilityText = this.FindControl<TextBlock>("CalculatedReliabilityText");

        // 特殊機能チェックボックスの取得
        _isNoiseReductionCheckBox = this.FindControl<CheckBox>("IsNoiseReductionCheckBox");
        _isHighFrequencyCheckBox = this.FindControl<CheckBox>("IsHighFrequencyCheckBox");
        _isLongRangeCheckBox = this.FindControl<CheckBox>("IsLongRangeCheckBox");
        _isDigitalCheckBox = this.FindControl<CheckBox>("IsDigitalCheckBox");
        _isTowedArrayCheckBox = this.FindControl<CheckBox>("IsTowedArrayCheckBox");

        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");

        // カテゴリの設定（ソナー関連のもののみフィルタリング）
        var filteredCategories = new Dictionary<string, NavalCategory>();
        if (_categories.ContainsKey("SMSO")) filteredCategories.Add("SMSO", _categories["SMSO"]);
        if (_categories.ContainsKey("SMLSO")) filteredCategories.Add("SMLSO", _categories["SMLSO"]);

        foreach (var category in filteredCategories)
            _categoryComboBox.Items.Add(new NavalCategoryItem
                { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

        // サブカテゴリの設定
        _subCategoryComboBox.Items.Add("標準型");
        _subCategoryComboBox.Items.Add("接触型");
        _subCategoryComboBox.Items.Add("可変深度型");
        _subCategoryComboBox.Items.Add("船体装備型");
        _subCategoryComboBox.Items.Add("曳航式");

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

            if (_sonarTypeComboBox.Items.Count > 0)
                _sonarTypeComboBox.SelectedIndex = 0;

            // デフォルトの開発年を設定
            _yearNumeric.Value = 1936;

            if (_countryComboBox.Items.Count > 0)
                _countryComboBox.SelectedIndex = 0;
        }

        // イベントハンドラの設定
        _categoryComboBox.SelectionChanged += OnCategoryChanged;
        _subCategoryComboBox.SelectionChanged += OnSubCategoryChanged;
        _sonarTypeComboBox.SelectionChanged += OnSonarTypeChanged;

        // 自動ID生成のためのイベントハンドラ
        _autoGenerateIdCheckBox.IsCheckedChanged += OnAutoGenerateIdChanged;
        _categoryComboBox.SelectionChanged += UpdateAutoGeneratedId;
        _yearNumeric.ValueChanged += UpdateAutoGeneratedId;
        _subCategoryComboBox.SelectionChanged += UpdateAutoGeneratedId;

        // 性能値計算のためのイベントハンドラ
        _frequencyNumeric.ValueChanged += UpdateCalculatedValues;
        _detectionPowerNumeric.ValueChanged += UpdateCalculatedValues;
        _detectionSpeedNumeric.ValueChanged += UpdateCalculatedValues;
        _weightNumeric.ValueChanged += UpdateCalculatedValues;
        _sonarTypeComboBox.SelectionChanged += UpdateCalculatedValues;
        _yearNumeric.ValueChanged += UpdateCalculatedValues;

        // 特殊機能チェックボックスのイベントハンドラ
        _isNoiseReductionCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isHighFrequencyCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isLongRangeCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDigitalCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isTowedArrayCheckBox.IsCheckedChanged += UpdateCalculatedValues;

        // 初期ID生成（自動生成がオンの場合）
        if (_autoGenerateIdCheckBox.IsChecked == true)
            UpdateAutoGeneratedId(null, EventArgs.Empty);

        // 初期計算値更新
        UpdateCalculatedValues(null, EventArgs.Empty);
    }

    /// <summary>
    ///     生データから作成するコンストラクタ
    /// </summary>
    /// <summary>
    ///     生データから作成するコンストラクタ
    /// </summary>
    public Sonar_Design_View(Dictionary<string, object> rawSonarData, Dictionary<string, NavalCategory> categories,
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

        // ソナー特有のパラメータUI要素の取得
        _frequencyNumeric = this.FindControl<NumericUpDown>("FrequencyNumeric");
        _detectionPowerNumeric = this.FindControl<NumericUpDown>("DetectionPowerNumeric");
        _detectionSpeedNumeric = this.FindControl<NumericUpDown>("DetectionSpeedNumeric");
        _weightNumeric = this.FindControl<NumericUpDown>("WeightNumeric");
        _sonarTypeComboBox = this.FindControl<ComboBox>("SonarTypeComboBox");
        _manpowerNumeric = this.FindControl<NumericUpDown>("ManpowerNumeric");

        // リソース関連UI要素の取得
        _steelNumeric = this.FindControl<NumericUpDown>("SteelNumeric");
        _tungstenNumeric = this.FindControl<NumericUpDown>("TungstenNumeric");
        _electronicsNumeric = this.FindControl<NumericUpDown>("ElectronicsNumeric");

        // 計算値表示用UI要素の取得
        _calculatedSubDetectionText = this.FindControl<TextBlock>("CalculatedSubDetectionText");
        _calculatedSurfaceDetectionText = this.FindControl<TextBlock>("CalculatedSurfaceDetectionText");
        _calculatedDetectionRangeText = this.FindControl<TextBlock>("CalculatedDetectionRangeText");
        _calculatedSubAttackText = this.FindControl<TextBlock>("CalculatedSubAttackText");
        _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
        _calculatedReliabilityText = this.FindControl<TextBlock>("CalculatedReliabilityText");

        // 特殊機能チェックボックスの取得
        _isNoiseReductionCheckBox = this.FindControl<CheckBox>("IsNoiseReductionCheckBox");
        _isHighFrequencyCheckBox = this.FindControl<CheckBox>("IsHighFrequencyCheckBox");
        _isLongRangeCheckBox = this.FindControl<CheckBox>("IsLongRangeCheckBox");
        _isDigitalCheckBox = this.FindControl<CheckBox>("IsDigitalCheckBox");
        _isTowedArrayCheckBox = this.FindControl<CheckBox>("IsTowedArrayCheckBox");

        _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");

        // UI項目の選択肢を初期化
        InitializeUiOptions();

        // 国家リストを初期化（これは非同期操作）
        InitializeCountryList();

        // 生データから基本的な値を設定（国家以外）
        if (rawSonarData != null)
        {
            PopulateFromRawData(rawSonarData);

            // 編集モードでは自動生成をオフに
            _autoGenerateIdCheckBox.IsChecked = false;
            _idTextBox.IsEnabled = true;

            // 国家を設定する（これは国家リスト初期化後に行われる必要があるため、
            // InitializeCountryListメソッド内で行うようにしてもよい）
            if (rawSonarData.ContainsKey("Country") && rawSonarData["Country"] != null)
            {
                var countryValue = rawSonarData["Country"].ToString();
                // 初期状態では国家リストがないので、ここではデフォルト選択のみ行う
                // 実際の国家設定はInitializeCountryList完了後に行われる
                _countryComboBox.SelectedIndex = 0;
            }
        }

        // イベントハンドラを設定
        _categoryComboBox.SelectionChanged += OnCategoryChanged;
        _subCategoryComboBox.SelectionChanged += OnSubCategoryChanged;
        _sonarTypeComboBox.SelectionChanged += OnSonarTypeChanged;

        // 自動ID生成のためのイベントハンドラ
        _autoGenerateIdCheckBox.IsCheckedChanged += OnAutoGenerateIdChanged;
        _categoryComboBox.SelectionChanged += UpdateAutoGeneratedId;
        _yearNumeric.ValueChanged += UpdateAutoGeneratedId;
        _subCategoryComboBox.SelectionChanged += UpdateAutoGeneratedId;

        // 性能値計算のためのイベントハンドラ
        _frequencyNumeric.ValueChanged += UpdateCalculatedValues;
        _detectionPowerNumeric.ValueChanged += UpdateCalculatedValues;
        _detectionSpeedNumeric.ValueChanged += UpdateCalculatedValues;
        _weightNumeric.ValueChanged += UpdateCalculatedValues;
        _sonarTypeComboBox.SelectionChanged += UpdateCalculatedValues;
        _yearNumeric.ValueChanged += UpdateCalculatedValues;

        // 特殊機能チェックボックスのイベントハンドラ
        _isNoiseReductionCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isHighFrequencyCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isLongRangeCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isDigitalCheckBox.IsCheckedChanged += UpdateCalculatedValues;
        _isTowedArrayCheckBox.IsCheckedChanged += UpdateCalculatedValues;
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
        // カテゴリの設定（ソナー関連のもののみフィルタリング）
        var filteredCategories = new Dictionary<string, NavalCategory>();
        if (_categories.ContainsKey("SMSO")) filteredCategories.Add("SMSO", _categories["SMSO"]);
        if (_categories.ContainsKey("SMLSO")) filteredCategories.Add("SMLSO", _categories["SMLSO"]);

        foreach (var category in filteredCategories)
            _categoryComboBox.Items.Add(new NavalCategoryItem
                { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

        // サブカテゴリの設定
        _subCategoryComboBox.Items.Add("標準型");
        _subCategoryComboBox.Items.Add("接触型");
        _subCategoryComboBox.Items.Add("可変深度型");
        _subCategoryComboBox.Items.Add("船体装備型");
        _subCategoryComboBox.Items.Add("曳航式");
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
            else if (DataContext is Dictionary<string, object> rawData &&
                     rawData.ContainsKey("Country") &&
                     rawData["Country"] != null)
            {
                var countryValue = rawData["Country"].ToString();
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

        // ソナーの詳細パラメータを設定
        if (_originalEquipment.AdditionalProperties.ContainsKey("Frequency"))
            _frequencyNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Frequency"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("DetectionPower"))
            _detectionPowerNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["DetectionPower"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("DetectionSpeed"))
            _detectionSpeedNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["DetectionSpeed"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("Weight"))
            _weightNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Weight"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("SonarType"))
        {
            var sonarType = _originalEquipment.AdditionalProperties["SonarType"].ToString();
            var sonarTypeIndex = _sonarTypeComboBox.Items.IndexOf(sonarType);
            if (sonarTypeIndex >= 0) _sonarTypeComboBox.SelectedIndex = sonarTypeIndex;
        }

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
        if (_originalEquipment.AdditionalProperties.ContainsKey("IsNoiseReduction"))
            _isNoiseReductionCheckBox.IsChecked =
                Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsNoiseReduction"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsHighFrequency"))
            _isHighFrequencyCheckBox.IsChecked =
                Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsHighFrequency"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsLongRange"))
            _isLongRangeCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsLongRange"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsDigital"))
            _isDigitalCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsDigital"]);

        if (_originalEquipment.AdditionalProperties.ContainsKey("IsTowedArray"))
            _isTowedArrayCheckBox.IsChecked =
                Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsTowedArray"]);

        // 既存の計算値がある場合は表示
        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedSubDetection"))
            _calculatedSubDetectionText.Text =
                _originalEquipment.AdditionalProperties["CalculatedSubDetection"].ToString();

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedSurfaceDetection"))
            _calculatedSurfaceDetectionText.Text =
                _originalEquipment.AdditionalProperties["CalculatedSurfaceDetection"].ToString();

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedDetectionRange"))
            _calculatedDetectionRangeText.Text =
                _originalEquipment.AdditionalProperties["CalculatedDetectionRange"] + " km";

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedSubAttack"))
            _calculatedSubAttackText.Text = _originalEquipment.AdditionalProperties["CalculatedSubAttack"].ToString();

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedBuildCost"))
            _calculatedBuildCostText.Text = _originalEquipment.AdditionalProperties["CalculatedBuildCost"].ToString();

        if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedReliability"))
            _calculatedReliabilityText.Text =
                _originalEquipment.AdditionalProperties["CalculatedReliability"].ToString();

        // 備考
        if (_originalEquipment.AdditionalProperties.ContainsKey("Description"))
            _descriptionTextBox.Text = _originalEquipment.AdditionalProperties["Description"].ToString();
    }


    private string GetCategoryDisplayName(string categoryId)
    {
        if (_categories.ContainsKey(categoryId)) return _categories[categoryId].Name;
        return categoryId == "SMSO" ? "ソナー" : "大型ソナー";
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

    private void PopulateFromRawData(Dictionary<string, object> rawSonarData)
    {
        // ウィンドウタイトルをカテゴリに合わせて設定
        var categoryId = rawSonarData["Category"].ToString();
        var categoryName = GetCategoryDisplayName(categoryId);
        Title = $"{categoryName}の編集";

        // 基本情報の設定
        _idTextBox.Text = rawSonarData["Id"].ToString();
        _nameTextBox.Text = rawSonarData["Name"].ToString();

        // ComboBoxの選択
        SelectComboBoxItem(_categoryComboBox, "Id", categoryId);
        SelectComboBoxItem(_subCategoryComboBox, null, rawSonarData["SubCategory"].ToString());

        // 開発年を設定
        if (rawSonarData.ContainsKey("Year"))
            _yearNumeric.Value = NavalUtility.GetDecimalValue(rawSonarData, "Year");
        else
            _yearNumeric.Value = 1936; // デフォルト値

        // 国家選択は後で行う（InitializeCountryListが完了した後）

        // ソナーパラメータの設定
        UiHelper.SetNumericValue(_frequencyNumeric, NavalUtility.GetDecimalValue(rawSonarData, "Frequency"));
        UiHelper.SetNumericValue(_detectionPowerNumeric, NavalUtility.GetDecimalValue(rawSonarData, "DetectionPower"));
        UiHelper.SetNumericValue(_detectionSpeedNumeric, NavalUtility.GetDecimalValue(rawSonarData, "DetectionSpeed"));
        UiHelper.SetNumericValue(_weightNumeric, NavalUtility.GetDecimalValue(rawSonarData, "Weight"));
        UiHelper.SelectComboBoxItem(_sonarTypeComboBox, null,
            rawSonarData.ContainsKey("SonarType") ? rawSonarData["SonarType"].ToString() : "アクティブ");
        UiHelper.SetNumericValue(_manpowerNumeric, NavalUtility.GetDecimalValue(rawSonarData, "Manpower"));

        // リソース設定
        UiHelper.SetNumericValue(_steelNumeric, NavalUtility.GetDecimalValue(rawSonarData, "Steel"));
        UiHelper.SetNumericValue(_tungstenNumeric, NavalUtility.GetDecimalValue(rawSonarData, "Tungsten"));
        UiHelper.SetNumericValue(_electronicsNumeric, NavalUtility.GetDecimalValue(rawSonarData, "Electronics"));

        // 特殊機能の設定
        _isNoiseReductionCheckBox.IsChecked = rawSonarData.ContainsKey("IsNoiseReduction") &&
                                              NavalUtility.GetBooleanValue(rawSonarData, "IsNoiseReduction");
        _isHighFrequencyCheckBox.IsChecked = rawSonarData.ContainsKey("IsHighFrequency") &&
                                             NavalUtility.GetBooleanValue(rawSonarData, "IsHighFrequency");
        _isLongRangeCheckBox.IsChecked = rawSonarData.ContainsKey("IsLongRange") &&
                                         NavalUtility.GetBooleanValue(rawSonarData, "IsLongRange");
        _isDigitalCheckBox.IsChecked = rawSonarData.ContainsKey("IsDigital") &&
                                       NavalUtility.GetBooleanValue(rawSonarData, "IsDigital");
        _isTowedArrayCheckBox.IsChecked = rawSonarData.ContainsKey("IsTowedArray") &&
                                          NavalUtility.GetBooleanValue(rawSonarData, "IsTowedArray");

        // 計算された性能値
        if (rawSonarData.ContainsKey("CalculatedSubDetection"))
            _calculatedSubDetectionText.Text = rawSonarData["CalculatedSubDetection"].ToString();

        if (rawSonarData.ContainsKey("CalculatedSurfaceDetection"))
            _calculatedSurfaceDetectionText.Text = rawSonarData["CalculatedSurfaceDetection"].ToString();

        if (rawSonarData.ContainsKey("CalculatedDetectionRange"))
            _calculatedDetectionRangeText.Text = rawSonarData["CalculatedDetectionRange"] + " km";

        if (rawSonarData.ContainsKey("CalculatedSubAttack"))
            _calculatedSubAttackText.Text = rawSonarData["CalculatedSubAttack"].ToString();

        if (rawSonarData.ContainsKey("CalculatedBuildCost"))
            _calculatedBuildCostText.Text = rawSonarData["CalculatedBuildCost"].ToString();

        if (rawSonarData.ContainsKey("CalculatedReliability"))
            _calculatedReliabilityText.Text = rawSonarData["CalculatedReliability"].ToString();

        // 備考欄の設定
        if (rawSonarData.ContainsKey("Description"))
            _descriptionTextBox.Text = NavalUtility.GetStringValue(rawSonarData, "Description");
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
                case "曳航式":
                    _isTowedArrayCheckBox.IsChecked = true;
                    break;
                case "可変深度型":
                    _isLongRangeCheckBox.IsChecked = true;
                    break;
            }
        }

        // 自動生成がオンならIDを更新
        if (_autoGenerateIdCheckBox.IsChecked == true)
            UpdateAutoGeneratedId(null, null);
    }

    private void OnSonarTypeChanged(object sender, SelectionChangedEventArgs e)
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

    // ソナー用のID自動生成メソッド
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

            // ソナー種類を取得
            var sonarType = _sonarTypeComboBox.SelectedItem?.ToString() ?? "アクティブ";

            // サブカテゴリを英語に変換してID生成
            string subCategoryCode;
            switch (subCategory)
            {
                case "標準型": subCategoryCode = "std"; break;
                case "接触型": subCategoryCode = "hull"; break;
                case "可変深度型": subCategoryCode = "vds"; break;
                case "船体装備型": subCategoryCode = "ship"; break;
                case "曳航式": subCategoryCode = "towed"; break;
                default: subCategoryCode = "std"; break;
            }

            // ソナータイプを英語に変換
            string sonarTypeCode;
            switch (sonarType)
            {
                case "アクティブ": sonarTypeCode = "act"; break;
                case "パッシブ": sonarTypeCode = "pas"; break;
                case "アクティブ＆パッシブ": sonarTypeCode = "dual"; break;
                default: sonarTypeCode = "act"; break;
            }

            // IDを生成（例: smso_usa_1940_std_act_mk1）
            var generatedId =
                $"{category.ToLower()}_{countryTag.ToLower()}_{year}_{subCategoryCode}_{sonarTypeCode}_mk1";

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
            case "SMSO": // 通常ソナー
                _frequencyNumeric.Value = 20; // 20kHz
                _detectionPowerNumeric.Value = 80; // 80dB
                _detectionSpeedNumeric.Value = 10; // 10kt
                _weightNumeric.Value = 250; // 250kg
                _manpowerNumeric.Value = 5; // 5人
                break;

            case "SMLSO": // 大型ソナー
                _frequencyNumeric.Value = 15; // 15kHz
                _detectionPowerNumeric.Value = 120; // 120dB
                _detectionSpeedNumeric.Value = 8; // 8kt
                _weightNumeric.Value = 1000; // 1000kg
                _manpowerNumeric.Value = 12; // 12人
                break;
        }

        // リソース設定
        switch (categoryId)
        {
            case "SMSO":
                _steelNumeric.Value = 1;
                _tungstenNumeric.Value = 1;
                _electronicsNumeric.Value = 1;
                break;
            case "SMLSO":
                _steelNumeric.Value = 3;
                _tungstenNumeric.Value = 2;
                _electronicsNumeric.Value = 3;
                break;
        }
    }

    // 性能値の計算と表示を更新するメソッド
    private void UpdateCalculatedValues(object sender, EventArgs e)
    {
        try
        {
            if (_categoryComboBox.SelectedItem is not NavalCategoryItem categoryItem ||
                _sonarTypeComboBox.SelectedItem == null)
                return;

            // パラメータの取得
            var frequency = (double)(_frequencyNumeric.Value ?? 0);
            var detectionPower = (double)(_detectionPowerNumeric.Value ?? 0);
            var detectionSpeed = (double)(_detectionSpeedNumeric.Value ?? 0);
            var weight = (double)(_weightNumeric.Value ?? 0);
            var sonarType = _sonarTypeComboBox.SelectedItem.ToString();
            var year = (int)(_yearNumeric.Value ?? 1936);

            // 特殊機能の取得
            var isNoiseReduction = _isNoiseReductionCheckBox.IsChecked ?? false;
            var isHighFrequency = _isHighFrequencyCheckBox.IsChecked ?? false;
            var isLongRange = _isLongRangeCheckBox.IsChecked ?? false;
            var isDigital = _isDigitalCheckBox.IsChecked ?? false;
            var isTowedArray = _isTowedArrayCheckBox.IsChecked ?? false;

            // ソナータイプの係数
            double activeMultiplier = 0;
            double passiveMultiplier = 0;

            switch (sonarType)
            {
                case "アクティブ":
                    activeMultiplier = 1.0;
                    passiveMultiplier = 0.0;
                    break;
                case "パッシブ":
                    activeMultiplier = 0.0;
                    passiveMultiplier = 1.0;
                    break;
                case "アクティブ＆パッシブ":
                    activeMultiplier = 0.7;
                    passiveMultiplier = 0.7;
                    break;
            }

            // 基本探知力計算
            var baseSurfaceDetection =
                detectionPower * activeMultiplier * 0.12 + detectionPower * passiveMultiplier * 0.05;
            var baseSubDetection = detectionPower * activeMultiplier * 0.15 + detectionPower * passiveMultiplier * 0.2;

            // 周波数による補正
            var frequencyModifier = 1.0;
            if (frequency < 10)
                frequencyModifier = 0.8; // 低周波
            else if (frequency > 40)
                frequencyModifier = 1.2; // 高周波

            if (isHighFrequency)
                frequencyModifier *= 1.3; // 高周波機能ボーナス

            // 探知速度による補正
            var speedModifier = detectionSpeed / 10.0;

            // 年代による技術補正
            var techModifier = 1.0;
            if (year < 1930)
                techModifier = 0.7;
            else if (year < 1940)
                techModifier = 0.9;
            else if (year < 1950)
                techModifier = 1.0;
            else if (year < 1960)
                techModifier = 1.1;
            else
                techModifier = 1.2;

            if (isDigital)
                techModifier *= 1.5; // デジタル信号処理ボーナス

            // 特殊機能による補正
            var specialModifier = 1.0;

            if (isNoiseReduction)
                specialModifier *= 1.2; // 静音化ボーナス

            if (isLongRange)
                specialModifier *= 1.3; // 長距離探知ボーナス

            if (isTowedArray)
                specialModifier *= 1.4; // 曳航式アレイボーナス

            // 最終探知力計算
            var surfaceDetection = baseSurfaceDetection * frequencyModifier * speedModifier * techModifier *
                                   specialModifier;
            var subDetection = baseSubDetection * frequencyModifier * speedModifier * techModifier * specialModifier;

            // 探知範囲計算 (km)
            var detectionRange = 10 + detectionPower * 0.1 * frequencyModifier * techModifier;

            if (isLongRange)
                detectionRange *= 1.5; // 長距離探知ボーナス

            if (isTowedArray)
                detectionRange *= 1.3; // 曳航式アレイボーナス

            // 潜水艦攻撃力計算（アクティブソナーのみが対潜攻撃力を持つ）
            var subAttack = activeMultiplier * detectionPower * 0.05 * techModifier;

            // 建造コスト計算
            var buildCost = weight * 0.005 + detectionPower * 0.01;

            // ソナータイプによるコスト補正
            if (sonarType == "アクティブ＆パッシブ")
                buildCost *= 1.5;

            // 信頼性計算（0.0～1.0）
            var reliability = 0.7 + techModifier * 0.2;

            if (isDigital)
                reliability += 0.1;

            // 最大1.0に制限
            reliability = Math.Min(reliability, 1.0);

            // 計算結果をUIに表示（小数点第10位まで表示するフォーマット）
            _calculatedSubDetectionText.Text = subDetection.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedSurfaceDetectionText.Text = surfaceDetection.ToString("F10").TrimEnd('0').TrimEnd('.');
            _calculatedDetectionRangeText.Text = detectionRange.ToString("F10").TrimEnd('0').TrimEnd('.') + " km";
            _calculatedSubAttackText.Text = subAttack.ToString("F10").TrimEnd('0').TrimEnd('.');
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
            var conflictDialog = new IdConflictWindow(equipmentId);
            var result = await conflictDialog.ShowDialog<IdConflictWindow.ConflictResolution>(this);

            switch (result)
            {
                case IdConflictWindow.ConflictResolution.Cancel:
                    // キャンセル - 何もせずに戻る
                    return;

                case IdConflictWindow.ConflictResolution.Overwrite:
                    // 上書き保存 - そのまま続行
                    break;

                case IdConflictWindow.ConflictResolution.SaveAsNew:
                    // 別物として保存 - 一意のIDを生成
                    var allIds = dbManager.GetAllEquipmentIds();
                    equipmentId = UniqueIdGenerator.GenerateUniqueId(equipmentId, allIds);
                    break;
            }
        }

        // Tier（開発世代）を年度から計算
        var tier = NavalUtility.GetTierFromYear((int)_yearNumeric.Value);

        // ソナーのタイプを文字列で取得
        var sonarType = _sonarTypeComboBox.SelectedItem?.ToString() ?? "アクティブ";

        // ソナーデータを収集
        var sonarData = new Dictionary<string, object>
        {
            { "Id", equipmentId },
            { "Name", _nameTextBox.Text },
            { "Category", ((NavalCategoryItem)_categoryComboBox.SelectedItem).Id },
            { "SubCategory", _subCategoryComboBox.SelectedItem.ToString() },
            { "Year", (int)_yearNumeric.Value },
            { "Tier", tier },
            { "Country", countryValue },
            { "Frequency", (double)_frequencyNumeric.Value },
            { "DetectionPower", (double)_detectionPowerNumeric.Value },
            { "DetectionSpeed", (double)_detectionSpeedNumeric.Value },
            { "Weight", (double)_weightNumeric.Value },
            { "SonarType", sonarType },
            { "Manpower", (int)_manpowerNumeric.Value },
            { "Steel", (double)_steelNumeric.Value },
            { "Tungsten", (double)_tungstenNumeric.Value },
            { "Electronics", (double)_electronicsNumeric.Value },
            { "IsNoiseReduction", _isNoiseReductionCheckBox.IsChecked ?? false },
            { "IsHighFrequency", _isHighFrequencyCheckBox.IsChecked ?? false },
            { "IsLongRange", _isLongRangeCheckBox.IsChecked ?? false },
            { "IsDigital", _isDigitalCheckBox.IsChecked ?? false },
            { "IsTowedArray", _isTowedArrayCheckBox.IsChecked ?? false },
            { "Description", _descriptionTextBox?.Text ?? "" }
        };

        // 計算された性能値も追加
        try
        {
            // パラメータの取得
            var frequency = (double)_frequencyNumeric.Value;
            var detectionPower = (double)_detectionPowerNumeric.Value;
            var detectionSpeed = (double)_detectionSpeedNumeric.Value;
            var weight = (double)_weightNumeric.Value;
            var year = (int)_yearNumeric.Value;

            // 特殊機能の取得
            var isNoiseReduction = _isNoiseReductionCheckBox.IsChecked ?? false;
            var isHighFrequency = _isHighFrequencyCheckBox.IsChecked ?? false;
            var isLongRange = _isLongRangeCheckBox.IsChecked ?? false;
            var isDigital = _isDigitalCheckBox.IsChecked ?? false;
            var isTowedArray = _isTowedArrayCheckBox.IsChecked ?? false;

            // ソナータイプの係数
            double activeMultiplier = 0;
            double passiveMultiplier = 0;

            switch (sonarType)
            {
                case "アクティブ":
                    activeMultiplier = 1.0;
                    passiveMultiplier = 0.0;
                    break;
                case "パッシブ":
                    activeMultiplier = 0.0;
                    passiveMultiplier = 1.0;
                    break;
                case "アクティブ＆パッシブ":
                    activeMultiplier = 0.7;
                    passiveMultiplier = 0.7;
                    break;
            }

            // 基本探知力計算
            var baseSurfaceDetection =
                detectionPower * activeMultiplier * 0.12 + detectionPower * passiveMultiplier * 0.05;
            var baseSubDetection = detectionPower * activeMultiplier * 0.15 + detectionPower * passiveMultiplier * 0.2;

            // 周波数による補正
            var frequencyModifier = 1.0;
            if (frequency < 10)
                frequencyModifier = 0.8; // 低周波
            else if (frequency > 40)
                frequencyModifier = 1.2; // 高周波

            if (isHighFrequency)
                frequencyModifier *= 1.3; // 高周波機能ボーナス

            // 探知速度による補正
            var speedModifier = detectionSpeed / 10.0;

            // 年代による技術補正
            var techModifier = 1.0;
            if (year < 1930)
                techModifier = 0.7;
            else if (year < 1940)
                techModifier = 0.9;
            else if (year < 1950)
                techModifier = 1.0;
            else if (year < 1960)
                techModifier = 1.1;
            else
                techModifier = 1.2;

            if (isDigital)
                techModifier *= 1.5; // デジタル信号処理ボーナス

            // 特殊機能による補正
            var specialModifier = 1.0;

            if (isNoiseReduction)
                specialModifier *= 1.2; // 静音化ボーナス

            if (isLongRange)
                specialModifier *= 1.3; // 長距離探知ボーナス

            if (isTowedArray)
                specialModifier *= 1.4; // 曳航式アレイボーナス

            // 最終探知力計算
            var surfaceDetection = baseSurfaceDetection * frequencyModifier * speedModifier * techModifier *
                                   specialModifier;
            var subDetection = baseSubDetection * frequencyModifier * speedModifier * techModifier * specialModifier;

            // 探知範囲計算 (km)
            var detectionRange = 10 + detectionPower * 0.1 * frequencyModifier * techModifier;

            if (isLongRange)
                detectionRange *= 1.5; // 長距離探知ボーナス

            if (isTowedArray)
                detectionRange *= 1.3; // 曳航式アレイボーナス

            // 潜水艦攻撃力計算（アクティブソナーのみが対潜攻撃力を持つ）
            var subAttack = activeMultiplier * detectionPower * 0.05 * techModifier;

            // 建造コスト計算
            var buildCost = weight * 0.005 + detectionPower * 0.01;

            // ソナータイプによるコスト補正
            if (sonarType == "アクティブ＆パッシブ")
                buildCost *= 1.5;

            // 信頼性計算（0.0～1.0）
            var reliability = 0.7 + techModifier * 0.2;

            if (isDigital)
                reliability += 0.1;

            // 最大1.0に制限
            reliability = Math.Min(reliability, 1.0);

            // 計算結果をデータに追加
            sonarData["CalculatedSubDetection"] = subDetection;
            sonarData["CalculatedSurfaceDetection"] = surfaceDetection;
            sonarData["CalculatedDetectionRange"] = detectionRange;
            sonarData["CalculatedSubAttack"] = subAttack;
            sonarData["CalculatedBuildCost"] = buildCost;
            sonarData["CalculatedReliability"] = reliability;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"計算値の設定中にエラーが発生しました: {ex.Message}");
        }

        // NavalEquipmentオブジェクトを作成
        var equipment = SonarCalculator.Sonar_Processing(sonarData);

        // ソナーの生データも保存
        SonarDataToDb.SaveSonarData(equipment, sonarData);

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