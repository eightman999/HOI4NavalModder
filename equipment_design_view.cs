using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Avalonia.Media;
using System.Linq;
using System.IO;
using Avalonia.VisualTree;

namespace HOI4NavalModder
{
    public partial class EquipmentDesignView : UserControl
    {
        private DataGrid _equipmentDataGrid;
        private ObservableCollection<NavalEquipment> _equipmentList = new ObservableCollection<NavalEquipment>();
        private Dictionary<string, NavalCategory> _categories = new Dictionary<string, NavalCategory>();
        private Dictionary<int, string> _tierYears = new Dictionary<int, string>();
        
        private DatabaseManager _dbManager;
        
        public EquipmentDesignView()
        {
            InitializeComponent();
            
            // コントロールの取得
            _equipmentDataGrid = this.FindControl<DataGrid>("EquipmentDataGrid");
            
            // データベースマネージャーの初期化
            _dbManager = new DatabaseManager();
            _dbManager.InitializeDatabase();
            
            // カテゴリの初期化
            InitializeCategories();
            
            // 開発年(ティア)の初期化
            InitializeTierYears();
            
            // データの読み込み
            LoadEquipmentData();
            
            // データグリッドの設定
            _equipmentDataGrid.ItemsSource = _equipmentList;
            _equipmentDataGrid.DoubleTapped += OnEquipmentDoubleTapped;
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
                
                // データベースから全てのモジュール情報を取得
                var modules = _dbManager.GetAllModules();
                
                foreach (var moduleInfo in modules)
                {
                    // モジュールの詳細データを取得
                    ModuleData moduleData = _dbManager.GetModuleData(moduleInfo.ID);
                    
                    if (moduleData != null)
                    {
                        // NavalEquipmentオブジェクトに変換
                        var equipment = new NavalEquipment
                        {
                            Id = moduleData.Info.ID,
                            Name = moduleData.Info.Name,
                            Category = GetCategoryFromGfx(moduleData.Info.Gfx), // GfxからカテゴリーIDを取得
                            SubCategory = GetSubCategoryFromGfx(moduleData.Info.Gfx), // 適切なサブカテゴリを設定
                            Year = moduleData.Info.Year,
                            Tier = GetTierFromYear(moduleData.Info.Year),
                            Country = moduleData.Info.Country,
                            Attack = GetAttackValue(moduleData.AddStats), // 適切な攻撃力値を設定
                            Defense = GetDefenseValue(moduleData.AddStats), // 適切な防御力値を設定
                            SpecialAbility = moduleData.Info.CriticalParts, // 特殊能力として重要部品情報を設定
                            AdditionalProperties = new Dictionary<string, object>()
                        };
                        
                        // 追加プロパティの設定
                        PopulateAdditionalProperties(equipment, moduleData);
                        
                        // リストに追加
                        _equipmentList.Add(equipment);
                    }
                }
                
                Console.WriteLine($"データベースから{_equipmentList.Count}件の装備を読み込みました。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"装備データの読み込み中にエラーが発生しました: {ex.Message}");
                // エラー時は空のリストを使用
                _equipmentList.Clear();
            }
        }
        
        // GfxからカテゴリIDを取得するヘルパーメソッド
        private string GetCategoryFromGfx(string gfx)
        {
            if (string.IsNullOrEmpty(gfx))
                return "SMOT"; // デフォルトはその他
                
            // gfxから適切なカテゴリを判断するロジック
            // 例: "gfx_smlg_152mm"のような形式を想定
            if (gfx.StartsWith("gfx_smlg_")) return "SMLG";
            if (gfx.StartsWith("gfx_smmg_")) return "SMMG";
            if (gfx.StartsWith("gfx_smhg_")) return "SMHG";
            if (gfx.StartsWith("gfx_smshg_")) return "SMSHG";
            if (gfx.StartsWith("gfx_smtp_")) return "SMTP";
            if (gfx.StartsWith("gfx_smstp_")) return "SMSTP";
            if (gfx.StartsWith("gfx_smsp_")) return "SMSP";
            if (gfx.StartsWith("gfx_smcr_")) return "SMCR";
            if (gfx.StartsWith("gfx_smhc_")) return "SMHC";
            if (gfx.StartsWith("gfx_smasp_")) return "SMASP";
            if (gfx.StartsWith("gfx_smlsp_")) return "SMLSP";
            if (gfx.StartsWith("gfx_smdcl_")) return "SMDCL";
            if (gfx.StartsWith("gfx_smso_")) return "SMSO";
            if (gfx.StartsWith("gfx_smlso_")) return "SMLSO";
            if (gfx.StartsWith("gfx_smdc_")) return "SMDC";
            if (gfx.StartsWith("gfx_smlr_")) return "SMLR";
            if (gfx.StartsWith("gfx_smhr_")) return "SMHR";
            if (gfx.StartsWith("gfx_smaa_")) return "SMAA";
            if (gfx.StartsWith("gfx_smtr_")) return "SMTR";
            if (gfx.StartsWith("gfx_smmbl_")) return "SMMBL";
            if (gfx.StartsWith("gfx_smhbl_")) return "SMHBL";
            if (gfx.StartsWith("gfx_smhaa_")) return "SMHAA";
            if (gfx.StartsWith("gfx_smasm_")) return "SMASM";
            if (gfx.StartsWith("gfx_smsam_")) return "SMSAM";
            if (gfx.StartsWith("gfx_smhng_")) return "SMHNG";
            
            return "SMOT"; // デフォルトはその他
        }
        
        // GfxからサブカテゴリIDを取得するヘルパーメソッド
        private string GetSubCategoryFromGfx(string gfx)
        {
            if (string.IsNullOrEmpty(gfx))
                return "";
                
            // 砲カテゴリーの場合はサブカテゴリを設定
            if (gfx.Contains("_single")) return "単装砲";
            if (gfx.Contains("_double")) return "連装砲";
            if (gfx.Contains("_triple")) return "三連装砲";
            if (gfx.Contains("_quad")) return "四連装砲";
            
            return "";
        }
        
        // 年からティアIDを取得するヘルパーメソッド
        private int GetTierFromYear(int year)
        {
            // 年に最も近いティアを返す
            if (year <= 1890) return 0;
            if (year <= 1895) return 1;
            if (year <= 1900) return 2;
            if (year <= 1905) return 3;
            if (year <= 1910) return 4;
            if (year <= 1915) return 5;
            if (year <= 1920) return 6;
            if (year <= 1925) return 7;
            if (year <= 1930) return 8;
            if (year <= 1935) return 9;
            if (year <= 1940) return 10;
            if (year <= 1945) return 11;
            if (year <= 1950) return 12;
            if (year <= 1955) return 13;
            if (year <= 1960) return 14;
            if (year <= 1965) return 15;
            if (year <= 1970) return 16;
            if (year <= 1975) return 17;
            if (year <= 1980) return 18;
            if (year <= 1985) return 19;
            if (year <= 1990) return 20;
            if (year <= 1995) return 21;
            if (year <= 2000) return 22;
            
            return 23; // 2000年以降
        }
        
        // モジュールのカテゴリに応じた攻撃力を算出するヘルパーメソッド
        private double GetAttackValue(ModuleStats stats)
        {
            // 各種攻撃力の中から最も高い値を採用
            return Math.Max(
                Math.Max(
                    Math.Max(stats.LgAttack, stats.HgAttack),
                    Math.Max(stats.TorpedoAttack, stats.AntiAirAttack)
                ),
                Math.Max(
                    stats.SubAttack,
                    stats.ShoreBombardment
                )
            );
        }
        
        // モジュールの防御力を算出するヘルパーメソッド
        private double GetDefenseValue(ModuleStats stats)
        {
            // 防御力として使用できる値がないため、回避力などを使用
            return stats.Evasion;
        }
        
        // 追加プロパティを設定するヘルパーメソッド
        private void PopulateAdditionalProperties(NavalEquipment equipment, ModuleData moduleData)
        {
            // 基本情報
            equipment.AdditionalProperties["Gfx"] = moduleData.Info.Gfx;
            equipment.AdditionalProperties["Sfx"] = moduleData.Info.Sfx;
            equipment.AdditionalProperties["Manpower"] = moduleData.Info.Manpower;
            equipment.AdditionalProperties["CriticalParts"] = moduleData.Info.CriticalParts;
            
            // 加算ステータス
            equipment.AdditionalProperties["BuildCostIc"] = moduleData.AddStats.BuildCostIc;
            equipment.AdditionalProperties["NavalSpeed"] = moduleData.AddStats.NavalSpeed;
            equipment.AdditionalProperties["FireRange"] = moduleData.AddStats.FireRange;
            equipment.AdditionalProperties["LgArmorPiercing"] = moduleData.AddStats.LgArmorPiercing;
            equipment.AdditionalProperties["LgAttack"] = moduleData.AddStats.LgAttack;
            equipment.AdditionalProperties["HgArmorPiercing"] = moduleData.AddStats.HgArmorPiercing;
            equipment.AdditionalProperties["HgAttack"] = moduleData.AddStats.HgAttack;
            equipment.AdditionalProperties["TorpedoAttack"] = moduleData.AddStats.TorpedoAttack;
            equipment.AdditionalProperties["AntiAirAttack"] = moduleData.AddStats.AntiAirAttack;
            equipment.AdditionalProperties["ShoreBombardment"] = moduleData.AddStats.ShoreBombardment;
            
            // リソース
            equipment.AdditionalProperties["Steel"] = moduleData.Resources.Steel;
            equipment.AdditionalProperties["Chromium"] = moduleData.Resources.Chromium;
            equipment.AdditionalProperties["Tungsten"] = moduleData.Resources.Tungsten;
            equipment.AdditionalProperties["Oil"] = moduleData.Resources.Oil;
            equipment.AdditionalProperties["Aluminium"] = moduleData.Resources.Aluminium;
            equipment.AdditionalProperties["Rubber"] = moduleData.Resources.Rubber;
            
            // 砲に特有のプロパティ
            if (equipment.Category.StartsWith("SML") || 
                equipment.Category.StartsWith("SMM") ||
                equipment.Category.StartsWith("SMH") ||
                equipment.Category.StartsWith("SMS"))
            {
                // 既存のNavalEquipmentモデルには存在しないがデータベースから取得したい追加情報
                if (moduleData.AddStats.FireRange > 0)
                {
                    equipment.AdditionalProperties["Range"] = moduleData.AddStats.FireRange;
                }
                
                // 口径や砲身数などのプロパティを追加
                if (moduleData.Info.CriticalParts.Contains("calibre"))
                {
                    try
                    {
                        // CriticalPartsから口径情報を抽出する例
                        // "calibre:15.2cm"のような形式を想定
                        var parts = moduleData.Info.CriticalParts.Split(',');
                        foreach (var part in parts)
                        {
                            if (part.StartsWith("calibre:"))
                            {
                                var calibreInfo = part.Substring(8);
                                equipment.AdditionalProperties["Calibre"] = calibreInfo;
                            }
                            
                            // 他の重要部品情報も同様に抽出
                        }
                    }
                    catch
                    {
                        // 解析エラー時は無視
                    }
                }
            }
        }
        
        // JSONファイルを使用する古い実装
        /*
        private void SaveEquipmentData()
        {
            try
            {
                var dataDir = Path.GetDirectoryName(_equipmentDataPath);
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };

                var json = JsonSerializer.Serialize(_equipmentList.ToList(), options);
                File.WriteAllText(_equipmentDataPath, json);
                
                Console.WriteLine("装備データを保存しました");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"装備データの保存中にエラーが発生しました: {ex.Message}");
            }
        }
        */
        
        // データベースに装備データを保存するメソッド
        private void SaveEquipmentData(NavalEquipment equipment)
        {
            try
            {
                // NavalEquipmentをModuleDataに変換
                ModuleData moduleData = ConvertToModuleData(equipment);
                
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
                ID = equipment.Id,
                Name = equipment.Name,
                Gfx = equipment.AdditionalProperties.ContainsKey("Gfx") ? equipment.AdditionalProperties["Gfx"].ToString() : $"gfx_{equipment.Category.ToLower()}_{equipment.Id.ToLower()}",
                Sfx = equipment.AdditionalProperties.ContainsKey("Sfx") ? equipment.AdditionalProperties["Sfx"].ToString() : "",
                Year = equipment.Year,
                Manpower = equipment.AdditionalProperties.ContainsKey("Manpower") ? Convert.ToInt32(equipment.AdditionalProperties["Manpower"]) : 0,
                Country = equipment.Country,
                CriticalParts = equipment.SpecialAbility
            };
            
            // 加算ステータス
            moduleData.AddStats = new ModuleStats();
            if (equipment.AdditionalProperties.ContainsKey("BuildCostIc")) moduleData.AddStats.BuildCostIc = Convert.ToDouble(equipment.AdditionalProperties["BuildCostIc"]);
            if (equipment.AdditionalProperties.ContainsKey("NavalSpeed")) moduleData.AddStats.NavalSpeed = Convert.ToDouble(equipment.AdditionalProperties["NavalSpeed"]);
            if (equipment.AdditionalProperties.ContainsKey("FireRange")) moduleData.AddStats.FireRange = Convert.ToDouble(equipment.AdditionalProperties["FireRange"]);
            if (equipment.AdditionalProperties.ContainsKey("LgArmorPiercing")) moduleData.AddStats.LgArmorPiercing = Convert.ToDouble(equipment.AdditionalProperties["LgArmorPiercing"]);
            if (equipment.AdditionalProperties.ContainsKey("LgAttack")) moduleData.AddStats.LgAttack = Convert.ToDouble(equipment.AdditionalProperties["LgAttack"]);
            if (equipment.AdditionalProperties.ContainsKey("HgArmorPiercing")) moduleData.AddStats.HgArmorPiercing = Convert.ToDouble(equipment.AdditionalProperties["HgArmorPiercing"]);
            if (equipment.AdditionalProperties.ContainsKey("HgAttack")) moduleData.AddStats.HgAttack = Convert.ToDouble(equipment.AdditionalProperties["HgAttack"]);
            if (equipment.AdditionalProperties.ContainsKey("TorpedoAttack")) moduleData.AddStats.TorpedoAttack = Convert.ToDouble(equipment.AdditionalProperties["TorpedoAttack"]);
            if (equipment.AdditionalProperties.ContainsKey("AntiAirAttack")) moduleData.AddStats.AntiAirAttack = Convert.ToDouble(equipment.AdditionalProperties["AntiAirAttack"]);
            if (equipment.AdditionalProperties.ContainsKey("ShoreBombardment")) moduleData.AddStats.ShoreBombardment = Convert.ToDouble(equipment.AdditionalProperties["ShoreBombardment"]);
            
            // 乗算ステータスと平均加算ステータスは新規作成時は空のオブジェクトを使用
            moduleData.MultiplyStats = new ModuleStats();
            moduleData.AddAverageStats = new ModuleStats();
            
            // リソース
            moduleData.Resources = new ModuleResources();
            if (equipment.AdditionalProperties.ContainsKey("Steel")) moduleData.Resources.Steel = Convert.ToInt32(equipment.AdditionalProperties["Steel"]);
            if (equipment.AdditionalProperties.ContainsKey("Chromium")) moduleData.Resources.Chromium = Convert.ToInt32(equipment.AdditionalProperties["Chromium"]);
            if (equipment.AdditionalProperties.ContainsKey("Tungsten")) moduleData.Resources.Tungsten = Convert.ToInt32(equipment.AdditionalProperties["Tungsten"]);
            if (equipment.AdditionalProperties.ContainsKey("Oil")) moduleData.Resources.Oil = Convert.ToInt32(equipment.AdditionalProperties["Oil"]);
            if (equipment.AdditionalProperties.ContainsKey("Aluminium")) moduleData.Resources.Aluminium = Convert.ToInt32(equipment.AdditionalProperties["Aluminium"]);
            if (equipment.AdditionalProperties.ContainsKey("Rubber")) moduleData.Resources.Rubber = Convert.ToInt32(equipment.AdditionalProperties["Rubber"]);
            
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
            if (_equipmentDataGrid.SelectedItem is NavalEquipment selectedEquipment)
            {
                OpenCategorySpecificEditor(selectedEquipment);
            }
        }
        
        public void OnDeleteEquipmentClick(object sender, RoutedEventArgs e)
        {
            if (_equipmentDataGrid.SelectedItem is NavalEquipment selectedEquipment)
            {
                // ToDo: 確認ダイアログを表示
                
                // データベースから削除
                bool success = _dbManager.DeleteModule(selectedEquipment.Id);
                
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
            if (_equipmentDataGrid.SelectedItem is NavalEquipment selectedEquipment)
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
                _equipmentDataGrid.SelectedItem = newEquipment;
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
            if (_equipmentDataGrid.SelectedItem is NavalEquipment selectedEquipment)
            {
                OpenCategorySpecificEditor(selectedEquipment);
            }
        }
        
        private async void ShowCategorySelectionWindow()
        {
            // カテゴリ選択ウィンドウを表示
            var categoryWindow = new CategorySelectionWindow(_categories, _tierYears);
            
            // 結果の処理
            var result = await categoryWindow.ShowDialog<CategorySelectionResult>(this.GetVisualRoot() as Window);
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
            if (_tierYears.TryGetValue(tierId, out string yearStr))
            {
                if (yearStr.EndsWith("以前"))
                {
                    return 1889; // 1890以前の場合
                }
                else
                {
                    if (int.TryParse(yearStr, out int year))
                    {
                        return year;
                    }
                }
            }
            return 1900; // デフォルト値
        }
        
private async void OpenCategorySpecificEditor(NavalEquipment equipment)
{
    Window editorWindow = null;
    
    // カテゴリに応じた適切なエディタを選択
    switch (equipment.Category)
    {
        case "SMLG": // 小口径砲
        case "SMMG": // 中口径砲
        case "SMHG": // 大口径砲
        case "SMSHG": // 超大口径砲
            editorWindow = new GunDesignView(equipment, _categories, _tierYears);
            break;
        case "SMTP": // 魚雷
        case "SMSTP": // 潜水艦魚雷
            //editorWindow = new SMTP_Design_View(equipment, _categories, _tierYears);
            break;
        case "SMSP": // 水上機
        case "SMCR": // 艦上偵察機
        case "SMHC": // 回転翼機
        case "SMASP": // 対潜哨戒機
        case "SMLSP": // 大型飛行艇
            //editorWindow = new SMSP_Design_View(equipment, _categories, _tierYears);
            break;
        case "SMDCL": // 爆雷投射機
        case "SMSO": // ソナー
        case "SMLSO": // 大型ソナー
        case "SMDC": // 爆雷
            //editorWindow = new SMDC_Design_View(equipment, _categories, _tierYears);
            break;
        case "SMLR": // 小型電探
        case "SMHR": // 大型電探
            //editorWindow = new SMLR_Design_View(equipment, _categories, _tierYears);
            break;
        case "SMAA": // 対空砲
        case "SMHAA": // 高射装置
            //editorWindow = new SMAA_Design_View(equipment, _categories, _tierYears);
            break;
        case "SMTR": // 機関
            //editorWindow = new SMTR_Design_View(equipment, _categories, _tierYears);
            break;
        case "SMMBL": // 増設バルジ(中型艦)
        case "SMHBL": // 増設バルジ(大型艦)
            //editorWindow = new SMMBL_Design_View(equipment, _categories, _tierYears);
            break;
        case "SMASM": // 対艦ミサイル
        case "SMSAM": // 対空ミサイル
            //editorWindow = new SMASM_Design_View(equipment, _categories, _tierYears);
            break;
        case "SMHNG": // 格納庫
            //ditorWindow = new SMHNG_Design_View(equipment, _categories, _tierYears);
            break;
        default: // その他
            //editorWindow = new SMOT_Design_View(equipment, _categories, _tierYears);
            break;
    }
    
    // エディタウィンドウを表示
    if (editorWindow != null)
    {
        var result = await editorWindow.ShowDialog<NavalEquipment>(this.GetVisualRoot() as Window);
        if (result != null)
        {
            // データベースに保存
            SaveEquipmentData(result);
            
            // 既存の装備を編集した場合
            if (_equipmentList.Any(e => e.Id == result.Id))
            {
                // リストの中の該当する装備を更新
                int index = _equipmentList.IndexOf(_equipmentList.First(e => e.Id == result.Id));
                if (index >= 0)
                {
                    _equipmentList[index] = result;
                }
            }
            else // 新規装備の場合
            {
                _equipmentList.Add(result);
            }
        }
    }
}