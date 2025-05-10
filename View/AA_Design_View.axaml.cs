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

namespace HOI4NavalModder.View
{
    public partial class AADesignView : Avalonia.Controls.Window
    {
        private readonly CheckBox _autoGenerateIdCheckBox;
        private readonly ComboBox _barrelCountComboBox;
        private readonly CheckBox _isAswCheckBox;
        private readonly CheckBox _hasAutoAimingCheckBox;
        private readonly CheckBox _hasProximityFuzeCheckBox;
        private readonly CheckBox _hasRadarGuidanceCheckBox;
        private readonly CheckBox _hasStabilizedMountCheckBox;
        private readonly CheckBox _hasRemoteControlCheckBox;
        private readonly NumericUpDown _barrelLengthNumeric;
        private readonly NumericUpDown _maxAltitudeNumeric;
        private readonly TextBlock _calculatedArmorPiercingText;
        private readonly TextBlock _calculatedBuildCostText;
        private readonly TextBlock _calculatedRangeText;
        private readonly TextBlock _calculatedLgAttackText;
        private readonly TextBlock _calculatedAntiAirText;
        private readonly TextBlock _calculatedTrackingText;
        private readonly TextBlock _calculatedEffectiveAltitudeText;
        private readonly TextBlock _calculatedSubAttackText;
        private readonly NumericUpDown _calibreNumeric;
        private readonly ComboBox _calibreTypeComboBox;
        private readonly Dictionary<string, NavalCategory> _categories;
        private readonly ComboBox _categoryComboBox;
        private readonly NumericUpDown _chromiumNumeric;
        private readonly NumericUpDown _tungstenNumeric;
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
        private CountryListManager _countryListManager;
        private string _vanillaPath;

        // 装備から開く場合のコンストラクタ
        public AADesignView(NavalEquipment equipment, Dictionary<string, NavalCategory> categories,
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
            _maxAltitudeNumeric = this.FindControl<NumericUpDown>("MaxAltitudeNumeric");
            _turretWeightNumeric = this.FindControl<NumericUpDown>("TurretWeightNumeric");
            _manpowerNumeric = this.FindControl<NumericUpDown>("ManpowerNumeric");

            _steelNumeric = this.FindControl<NumericUpDown>("SteelNumeric");
            _chromiumNumeric = this.FindControl<NumericUpDown>("ChromiumNumeric");
            _tungstenNumeric = this.FindControl<NumericUpDown>("TungstenNumeric");

            _calculatedAntiAirText = this.FindControl<TextBlock>("CalculatedAntiAirText");
            _calculatedRangeText = this.FindControl<TextBlock>("CalculatedRangeText");
            _calculatedArmorPiercingText = this.FindControl<TextBlock>("CalculatedArmorPiercingText");
            _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
            _calculatedLgAttackText = this.FindControl<TextBlock>("CalculatedLgAttackText");
            _calculatedTrackingText = this.FindControl<TextBlock>("CalculatedTrackingText");
            _calculatedEffectiveAltitudeText = this.FindControl<TextBlock>("CalculatedEffectiveAltitudeText");
            _calculatedSubAttackText = this.FindControl<TextBlock>("CalculatedSubAttackText");

            _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
            _barrelLengthNumeric = this.FindControl<NumericUpDown>("BarrelLengthNumeric");
            _autoGenerateIdCheckBox = this.FindControl<CheckBox>("AutoGenerateIdCheckBox");
            _isAswCheckBox = this.FindControl<CheckBox>("IsAswCheckBox");
            _hasAutoAimingCheckBox = this.FindControl<CheckBox>("HasAutoAimingCheckBox");
            _hasProximityFuzeCheckBox = this.FindControl<CheckBox>("HasProximityFuzeCheckBox");
            _hasRadarGuidanceCheckBox = this.FindControl<CheckBox>("HasRadarGuidanceCheckBox");
            _hasStabilizedMountCheckBox = this.FindControl<CheckBox>("HasStabilizedMountCheckBox");
            _hasRemoteControlCheckBox = this.FindControl<CheckBox>("HasRemoteControlCheckBox");

            // カテゴリの設定（対空砲関連のもののみフィルタリング）
            var filteredCategories = new Dictionary<string, NavalCategory>();
            if (_categories.ContainsKey("SMAA")) filteredCategories.Add("SMAA", _categories["SMAA"]);
            if (_categories.ContainsKey("SMHAA")) filteredCategories.Add("SMHAA", _categories["SMHAA"]);

            foreach (var category in filteredCategories)
                _categoryComboBox.Items.Add(new NavalCategoryItem
                    { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

            // サブカテゴリの設定
            _subCategoryComboBox.Items.Add("単装砲");
            _subCategoryComboBox.Items.Add("連装砲");
            _subCategoryComboBox.Items.Add("三連装砲");
            _subCategoryComboBox.Items.Add("四連装砲");
            _subCategoryComboBox.Items.Add("六連装砲");
            _subCategoryComboBox.Items.Add("八連装砲");

            InitializeCountryList();

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
            _barrelCountComboBox.Items.Add("6");
            _barrelCountComboBox.Items.Add("8");
        }

        // 生データからフォームに値を設定するメソッド
        private void PopulateFromRawData(Dictionary<string, object> rawAAData)
        {
            // ウィンドウタイトルをカテゴリに合わせて設定
            var categoryId = rawAAData["Category"].ToString();
            var categoryName = GetCategoryDisplayName(categoryId);
            Title = $"{categoryName}の編集";

            // 基本情報の設定
            _idTextBox.Text = rawAAData["Id"].ToString();
            _nameTextBox.Text = rawAAData["Name"].ToString();

            // ComboBoxの選択
            SelectComboBoxItem(_categoryComboBox, "Id", categoryId);
            SelectComboBoxItem(_subCategoryComboBox, null, rawAAData["SubCategory"].ToString());

            // 開発年を設定
            if (rawAAData.ContainsKey("Year"))
                _yearNumeric.Value = GetDecimalValue(rawAAData, "Year");
            else
                _yearNumeric.Value = 1936; // デフォルト値

            SelectComboBoxItem(_countryComboBox, null, rawAAData["Country"].ToString());

            // 数値の設定
            SetNumericValue(_shellWeightNumeric, GetDoubleValue(rawAAData, "ShellWeight"));
            SetNumericValue(_muzzleVelocityNumeric, GetDoubleValue(rawAAData, "MuzzleVelocity"));
            SetNumericValue(_rpmNumeric, GetDoubleValue(rawAAData, "RPM"));
            SetNumericValue(_calibreNumeric, GetDoubleValue(rawAAData, "Calibre"));
            SelectComboBoxItem(_calibreTypeComboBox, null, rawAAData["CalibreType"].ToString());
            SelectComboBoxItem(_barrelCountComboBox, null, rawAAData["BarrelCount"].ToString());
            SetNumericValue(_elevationAngleNumeric, GetDoubleValue(rawAAData, "ElevationAngle"));
            SetNumericValue(_maxAltitudeNumeric, GetDoubleValue(rawAAData, "MaxAltitude"));
            SetNumericValue(_turretWeightNumeric, GetDoubleValue(rawAAData, "TurretWeight"));
            SetNumericValue(_manpowerNumeric, GetIntValue(rawAAData, "Manpower"));

            // リソース
            SetNumericValue(_steelNumeric, GetIntValue(rawAAData, "Steel"));
            SetNumericValue(_chromiumNumeric, GetIntValue(rawAAData, "Chromium"));
            SetNumericValue(_tungstenNumeric, GetIntValue(rawAAData, "Tungsten"));
            if (rawAAData.ContainsKey("Country") && rawAAData["Country"] != null)
            {
                var countryValue = rawAAData["Country"].ToString();
                SetCountrySelection(countryValue);
            }

            // 特殊機能設定
            if (rawAAData.ContainsKey("IsAsw"))
                _isAswCheckBox.IsChecked = GetBooleanValue(rawAAData, "IsAsw");

            if (rawAAData.ContainsKey("HasAutoAiming"))
                _hasAutoAimingCheckBox.IsChecked = GetBooleanValue(rawAAData, "HasAutoAiming");

            if (rawAAData.ContainsKey("HasProximityFuze"))
                _hasProximityFuzeCheckBox.IsChecked = GetBooleanValue(rawAAData, "HasProximityFuze");

            if (rawAAData.ContainsKey("HasRadarGuidance"))
                _hasRadarGuidanceCheckBox.IsChecked = GetBooleanValue(rawAAData, "HasRadarGuidance");

            if (rawAAData.ContainsKey("HasStabilizedMount"))
                _hasStabilizedMountCheckBox.IsChecked = GetBooleanValue(rawAAData, "HasStabilizedMount");

            if (rawAAData.ContainsKey("HasRemoteControl"))
                _hasRemoteControlCheckBox.IsChecked = GetBooleanValue(rawAAData, "HasRemoteControl");

            // 計算された性能値
            if (rawAAData.ContainsKey("CalculatedAntiAir"))
                _calculatedAntiAirText.Text = rawAAData["CalculatedAntiAir"].ToString();

            if (rawAAData.ContainsKey("CalculatedRange"))
                _calculatedRangeText.Text = rawAAData["CalculatedRange"] + " km";

            if (rawAAData.ContainsKey("CalculatedArmorPiercing"))
                _calculatedArmorPiercingText.Text = rawAAData["CalculatedArmorPiercing"].ToString();

            if (rawAAData.ContainsKey("CalculatedBuildCost"))
                _calculatedBuildCostText.Text = rawAAData["CalculatedBuildCost"].ToString();

            if (rawAAData.ContainsKey("CalculatedLgAttack"))
                _calculatedLgAttackText.Text = rawAAData["CalculatedLgAttack"].ToString();

            if (rawAAData.ContainsKey("CalculatedTracking"))
                _calculatedTrackingText.Text = rawAAData["CalculatedTracking"].ToString();

            if (rawAAData.ContainsKey("CalculatedEffectiveAltitude"))
                _calculatedEffectiveAltitudeText.Text = rawAAData["CalculatedEffectiveAltitude"] + " m";

            if (rawAAData.ContainsKey("CalculatedSubAttack"))
                _calculatedSubAttackText.Text = rawAAData["CalculatedSubAttack"].ToString();

            // 備考欄の設定
            if (rawAAData.ContainsKey("Description"))
                _descriptionTextBox.Text = GetStringValue(rawAAData, "Description");
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

            // 装備の詳細パラメータを設定
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

            if (_originalEquipment.AdditionalProperties.ContainsKey("MaxAltitude"))
                _maxAltitudeNumeric.Value =
                    Convert.ToDecimal(_originalEquipment.AdditionalProperties["MaxAltitude"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("TurretWeight"))
                _turretWeightNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["TurretWeight"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("Manpower"))
                _manpowerNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Manpower"]);

            // リソース
            if (_originalEquipment.AdditionalProperties.ContainsKey("Steel"))
                _steelNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Steel"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("Chromium"))
                _chromiumNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Chromium"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("Tungsten"))
                _tungstenNumeric.Value = Convert.ToDecimal(_originalEquipment.AdditionalProperties["Tungsten"]);

            // 特殊機能の設定
            if (_originalEquipment.AdditionalProperties.ContainsKey("IsAsw"))
                _isAswCheckBox.IsChecked = Convert.ToBoolean(_originalEquipment.AdditionalProperties["IsAsw"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("HasAutoAiming"))
                _hasAutoAimingCheckBox.IsChecked =
                    Convert.ToBoolean(_originalEquipment.AdditionalProperties["HasAutoAiming"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("HasProximityFuze"))
                _hasProximityFuzeCheckBox.IsChecked =
                    Convert.ToBoolean(_originalEquipment.AdditionalProperties["HasProximityFuze"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("HasRadarGuidance"))
                _hasRadarGuidanceCheckBox.IsChecked =
                    Convert.ToBoolean(_originalEquipment.AdditionalProperties["HasRadarGuidance"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("HasStabilizedMount"))
                _hasStabilizedMountCheckBox.IsChecked =
                    Convert.ToBoolean(_originalEquipment.AdditionalProperties["HasStabilizedMount"]);

            if (_originalEquipment.AdditionalProperties.ContainsKey("HasRemoteControl"))
                _hasRemoteControlCheckBox.IsChecked =
                    Convert.ToBoolean(_originalEquipment.AdditionalProperties["HasRemoteControl"]);

            // 既存の計算値がある場合は表示
            if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedAntiAirAttack"))
                _calculatedAntiAirText.Text =
                    _originalEquipment.AdditionalProperties["CalculatedAntiAirAttack"].ToString();

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

            if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedTracking"))
                _calculatedTrackingText.Text = _originalEquipment.AdditionalProperties["CalculatedTracking"].ToString();

            if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedEffectiveAltitude"))
                _calculatedEffectiveAltitudeText.Text =
                    _originalEquipment.AdditionalProperties["CalculatedEffectiveAltitude"] + " m";

            if (_originalEquipment.AdditionalProperties.ContainsKey("CalculatedSubAttack"))
                _calculatedSubAttackText.Text =
                    _originalEquipment.AdditionalProperties["CalculatedSubAttack"].ToString();

            // 備考欄を設定
            if (_originalEquipment.AdditionalProperties.ContainsKey("Description"))
                _descriptionTextBox.Text = _originalEquipment.AdditionalProperties["Description"].ToString();
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
            return categoryId == "SMAA" ? "対空砲" : "高射装置";
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
                    case "六連装砲":
                        _barrelCountComboBox.SelectedIndex = 4; // 6門
                        break;
                    case "八連装砲":
                        _barrelCountComboBox.SelectedIndex = 5; // 8門
                        break;
                }
            }
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
                var generatedId = AAGunIdGenerator.GenerateGunId(
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
        

        // 生データから開く場合のコンストラクタ
        public AADesignView(Dictionary<string, object> rawAAData, Dictionary<string, NavalCategory> categories,
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
            _maxAltitudeNumeric = this.FindControl<NumericUpDown>("MaxAltitudeNumeric");
            _turretWeightNumeric = this.FindControl<NumericUpDown>("TurretWeightNumeric");
            _manpowerNumeric = this.FindControl<NumericUpDown>("ManpowerNumeric");

            _steelNumeric = this.FindControl<NumericUpDown>("SteelNumeric");
            _chromiumNumeric = this.FindControl<NumericUpDown>("ChromiumNumeric");
            _tungstenNumeric = this.FindControl<NumericUpDown>("TungstenNumeric");

            _calculatedAntiAirText = this.FindControl<TextBlock>("CalculatedAntiAirText");
            _calculatedRangeText = this.FindControl<TextBlock>("CalculatedRangeText");
            _calculatedArmorPiercingText = this.FindControl<TextBlock>("CalculatedArmorPiercingText");
            _calculatedBuildCostText = this.FindControl<TextBlock>("CalculatedBuildCostText");
            _calculatedLgAttackText = this.FindControl<TextBlock>("CalculatedLgAttackText");
            _calculatedTrackingText = this.FindControl<TextBlock>("CalculatedTrackingText");
            _calculatedEffectiveAltitudeText = this.FindControl<TextBlock>("CalculatedEffectiveAltitudeText");
            _calculatedSubAttackText = this.FindControl<TextBlock>("CalculatedSubAttackText");
            _descriptionTextBox = this.FindControl<TextBox>("DescriptionTextBox");
            _barrelLengthNumeric = this.FindControl<NumericUpDown>("BarrelLengthNumeric");
            _autoGenerateIdCheckBox = this.FindControl<CheckBox>("AutoGenerateIdCheckBox");
            _isAswCheckBox = this.FindControl<CheckBox>("IsAswCheckBox");
            _hasAutoAimingCheckBox = this.FindControl<CheckBox>("HasAutoAimingCheckBox");
            _hasProximityFuzeCheckBox = this.FindControl<CheckBox>("HasProximityFuzeCheckBox");
            _hasRadarGuidanceCheckBox = this.FindControl<CheckBox>("HasRadarGuidanceCheckBox");
            _hasStabilizedMountCheckBox = this.FindControl<CheckBox>("HasStabilizedMountCheckBox");
            _hasRemoteControlCheckBox = this.FindControl<CheckBox>("HasRemoteControlCheckBox");

            // UI項目の選択肢を初期化
            InitializeUiOptions();

            // 生データから値を設定
            if (rawAAData != null)
            {
                PopulateFromRawData(rawAAData);

                // 既存IDから砲身長を抽出して設定
                var id = rawAAData["Id"].ToString();
                string countrytag;
                if (AAGunIdGenerator.TryParseGunId(id, out _, out countrytag, out _, out _, out _,
                        out var barrelLength))
                    _barrelLengthNumeric.Value = barrelLength;
                else if (rawAAData.ContainsKey("BarrelLength"))
                    // 生データに砲身長がある場合
                    _barrelLengthNumeric.Value = GetDecimalValue(rawAAData, "BarrelLength");
                else
                    // デフォルト値
                    _barrelLengthNumeric.Value = 45;

                // 編集モードでは自動生成をオフに
                _autoGenerateIdCheckBox.IsChecked = false;
                _idTextBox.IsEnabled = true;
            }

            InitializeCountryList();

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
            _maxAltitudeNumeric.ValueChanged += UpdateCalculatedValues;
            _turretWeightNumeric.ValueChanged += UpdateCalculatedValues;
            _yearNumeric.ValueChanged += UpdateCalculatedValues;
            _isAswCheckBox.IsCheckedChanged += UpdateCalculatedValues;
            _hasAutoAimingCheckBox.IsCheckedChanged += UpdateCalculatedValues;
            _hasProximityFuzeCheckBox.IsCheckedChanged += UpdateCalculatedValues;
            _hasRadarGuidanceCheckBox.IsCheckedChanged += UpdateCalculatedValues;
            _hasStabilizedMountCheckBox.IsCheckedChanged += UpdateCalculatedValues;
            _hasRemoteControlCheckBox.IsCheckedChanged += UpdateCalculatedValues;

            // 初期ID生成（自動生成がオンの場合）
            if (_autoGenerateIdCheckBox.IsChecked == true) UpdateAutoGeneratedId(null, EventArgs.Empty);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
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

        private void InitializeUiOptions()
        {
            // カテゴリの設定（対空砲関連のもののみフィルタリング）
            var filteredCategories = new Dictionary<string, NavalCategory>();
            if (_categories.ContainsKey("SMAA")) filteredCategories.Add("SMAA", _categories["SMAA"]);
            if (_categories.ContainsKey("SMHAA")) filteredCategories.Add("SMHAA", _categories["SMHAA"]);

            foreach (var category in filteredCategories)
                _categoryComboBox.Items.Add(new NavalCategoryItem
                    { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

            // サブカテゴリの設定
            _subCategoryComboBox.Items.Add("単装砲");
            _subCategoryComboBox.Items.Add("連装砲");
            _subCategoryComboBox.Items.Add("三連装砲");
            _subCategoryComboBox.Items.Add("四連装砲");
            _subCategoryComboBox.Items.Add("六連装砲");
            _subCategoryComboBox.Items.Add("八連装砲");

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
            _barrelCountComboBox.Items.Add("6");
            _barrelCountComboBox.Items.Add("8");
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
                var maxAltitude = (double)(_maxAltitudeNumeric.Value ?? 5000);
                var barrelLength = (double)(_barrelLengthNumeric.Value ?? 45);
                var turretWeight = (double)(_turretWeightNumeric.Value ?? 0);
                var year = (int)(_yearNumeric.Value ?? 1936);
                var isAsw = _isAswCheckBox.IsChecked ?? false;
                var hasAutoAiming = _hasAutoAimingCheckBox.IsChecked ?? false;
                var hasProximityFuze = _hasProximityFuzeCheckBox.IsChecked ?? false;
                var hasRadarGuidance = _hasRadarGuidanceCheckBox.IsChecked ?? false;
                var hasStabilizedMount = _hasStabilizedMountCheckBox.IsChecked ?? false;
                var hasRemoteControl = _hasRemoteControlCheckBox.IsChecked ?? false;

                // 口径をmmに変換
                var calibreInMm = ConvertCalibreToMm(calibre, calibreType);

                // 技術レベル補正（年代による）
                var techLevelMultiplier = GetTechLevelMultiplier(year);

                // 特殊機能による補正係数
                var specialFeaturesMultiplier = 1.0;
                if (hasAutoAiming) specialFeaturesMultiplier *= 1.15;
                if (hasProximityFuze) specialFeaturesMultiplier *= 1.25;
                if (hasRadarGuidance) specialFeaturesMultiplier *= 1.2;
                if (hasStabilizedMount) specialFeaturesMultiplier *= 1.1;
                if (hasRemoteControl) specialFeaturesMultiplier *= 1.15;

                // 対空攻撃力計算
                var baseAntiAir = (rpm * shellWeight * muzzleVelocity) / (1000 * calibreInMm) * barrelCount;
                var antiAirValue = baseAntiAir * specialFeaturesMultiplier * techLevelMultiplier;

                // 追尾精度計算
                var baseTracking = (rpm / calibreInMm) * (muzzleVelocity / 500) * (60 / elevationAngle);
                var trackingValue = baseTracking * specialFeaturesMultiplier * techLevelMultiplier;

                // 有効射高計算
                var effectiveAltitudeBase = maxAltitude * (elevationAngle / 90.0) * (muzzleVelocity / 800.0);
                var effectiveAltitudeValue = effectiveAltitudeBase * specialFeaturesMultiplier;

                // 軽砲攻撃力計算（対艦攻撃力）
                var lgAttackBase = (shellWeight * Math.Pow(muzzleVelocity, 2)) / 5000000 * rpm * barrelCount;
                var lgAttackValue = lgAttackBase * (calibreInMm < 75 ? 1.0 : 0.7); // 75mm以上は効率が落ちる

                // 装甲貫通値計算
                var armorPiercingValue = lgAttackValue / Math.Pow(calibreInMm / 10, 1.5) * techLevelMultiplier;

                // 射程計算
                var rangeValue = (calibreInMm * barrelLength * muzzleVelocity * elevationAngle) / 100000;

                // 対潜攻撃力計算
                var subAttackValue =
                    isAsw ? (rpm / 60) * (shellWeight / 20) * barrelCount * 1.5 * techLevelMultiplier : 0;

                // 建造コスト計算
                var buildCostBase = (calibreInMm / 20) + (rpm / 100) + (turretWeight / 5);
                var buildCostSpecialMultiplier = 1.0;
                if (hasAutoAiming) buildCostSpecialMultiplier *= 1.1;
                if (hasProximityFuze) buildCostSpecialMultiplier *= 1.15;
                if (hasRadarGuidance) buildCostSpecialMultiplier *= 1.25;
                if (hasStabilizedMount) buildCostSpecialMultiplier *= 1.1;
                if (hasRemoteControl) buildCostSpecialMultiplier *= 1.2;

                var buildCostValue = buildCostBase * buildCostSpecialMultiplier * barrelCount * 0.25;

                // 計算結果をUIに表示（小数点第2位まで表示するフォーマット）
                _calculatedAntiAirText.Text = antiAirValue.ToString("F2");
                _calculatedLgAttackText.Text = lgAttackValue.ToString("F2");
                _calculatedArmorPiercingText.Text = armorPiercingValue.ToString("F2");
                _calculatedRangeText.Text = rangeValue.ToString("F2") + " km";
                _calculatedTrackingText.Text = trackingValue.ToString("F2");
                _calculatedEffectiveAltitudeText.Text = effectiveAltitudeValue.ToString("F0") + " m";
                _calculatedSubAttackText.Text = subAttackValue.ToString("F2");
                _calculatedBuildCostText.Text = buildCostValue.ToString("F2");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"計算エラー: {ex.Message}");
            }
        }

// 年代による技術レベル補正を取得
        private double GetTechLevelMultiplier(int year)
        {
            if (year < 1900) return 0.7;
            if (year < 1920) return 0.8;
            if (year < 1935) return 0.9;
            if (year < 1942) return 1.0;
            if (year < 1950) return 1.1;
            if (year < 1960) return 1.2;
            if (year < 1970) return 1.3;
            if (year < 1980) return 1.4;
            if (year < 1990) return 1.5;
            return 1.6; // 1990年以降
        }

// 各種口径単位をmmに変換
        private static double ConvertCalibreToMm(double calibre, string calibreType)
        {
            switch (calibreType.ToLower())
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
                    case "SMAA": // 対空砲
                        _shellWeightNumeric.Value = 0.9M; // 0.9kg
                        _muzzleVelocityNumeric.Value = 800; // 800m/s
                        _rpmNumeric.Value = 120; // 120発/分
                        _calibreNumeric.Value = 40M; // 40mm
                        _calibreTypeComboBox.SelectedIndex = 2; // mm
                        _barrelCountComboBox.SelectedIndex = 1; // 2門（連装）
                        _elevationAngleNumeric.Value = 80; // 80度
                        _maxAltitudeNumeric.Value = 5000; // 5000m
                        _turretWeightNumeric.Value = 2.5M; // 2.5トン
                        _manpowerNumeric.Value = 5; // 5人
                        break;
                    case "SMHAA": // 高射装置
                        _shellWeightNumeric.Value = 0.5M; // 0.5kg
                        _muzzleVelocityNumeric.Value = 700; // 700m/s
                        _rpmNumeric.Value = 150; // 150発/分
                        _calibreNumeric.Value = 20M; // 20mm
                        _calibreTypeComboBox.SelectedIndex = 2; // mm
                        _barrelCountComboBox.SelectedIndex = 3; // 4門（四連装）
                        _elevationAngleNumeric.Value = 85; // 85度
                        _maxAltitudeNumeric.Value = 3000; // 3000m
                        _turretWeightNumeric.Value = 1.0M; // 1.0トン
                        _manpowerNumeric.Value = 3; // 3人
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
                case "SMAA": // 対空砲
                    _steelNumeric.Value = 2; // 2単位
                    _chromiumNumeric.Value = 0.5M; // 0.5単位
                    _tungstenNumeric.Value = 0; // タングステン不要
                    break;
                case "SMHAA": // 高射装置
                    _steelNumeric.Value = 1; // 1単位
                    _chromiumNumeric.Value = 0.2M; // 0.2単位
                    _tungstenNumeric.Value = 0.5M; // 0.5単位
                    break;
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

            var countryValue = GetSelectedCountryValue();

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
            int tier = NavalUtility.GetTierFromYear((int)_yearNumeric.Value);

            // 全てのパラメータを格納するデータ辞書を作成
            var aaData = new Dictionary<string, object>
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
                { "MaxAltitude", (double)_maxAltitudeNumeric.Value },
                { "TurretWeight", (double)_turretWeightNumeric.Value },
                { "Manpower", (int)_manpowerNumeric.Value },
                { "Steel", (double)_steelNumeric.Value },
                { "Chromium", (double)_chromiumNumeric.Value },
                { "Tungsten", (double)_tungstenNumeric.Value },
                { "Description", _descriptionTextBox?.Text ?? "" },
                { "IsAsw", _isAswCheckBox.IsChecked ?? false },
                { "HasAutoAiming", _hasAutoAimingCheckBox.IsChecked ?? false },
                { "HasProximityFuze", _hasProximityFuzeCheckBox.IsChecked ?? false },
                { "HasRadarGuidance", _hasRadarGuidanceCheckBox.IsChecked ?? false },
                { "HasStabilizedMount", _hasStabilizedMountCheckBox.IsChecked ?? false },
                { "HasRemoteControl", _hasRemoteControlCheckBox.IsChecked ?? false }
            };

            // AAGunCalculatorを使って処理
            var processedEquipment = AAGunCalculator.AA_Processing(aaData);

            // AAGunDataToDb を使ってデータを保存
            AAGunDataToDb.SaveAAGunData(processedEquipment, aaData);

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
}