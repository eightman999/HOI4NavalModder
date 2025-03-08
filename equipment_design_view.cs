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
using System.Text.Json;
using Avalonia.VisualTree;

namespace HOI4NavalModder
{
    public partial class EquipmentDesignView : UserControl
    {
        private DataGrid _equipmentDataGrid;
        private ObservableCollection<NavalEquipment> _equipmentList = new ObservableCollection<NavalEquipment>();
        private Dictionary<string, NavalCategory> _categories = new Dictionary<string, NavalCategory>();
        private Dictionary<int, string> _tierYears = new Dictionary<int, string>();
        
        private readonly string _equipmentDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HOI4NavalModder",
            "equipment_data.json");
            
        public EquipmentDesignView()
        {
            InitializeComponent();
            
            // コントロールの取得
            _equipmentDataGrid = this.FindControl<DataGrid>("EquipmentDataGrid");
            
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
                var dataDir = Path.GetDirectoryName(_equipmentDataPath);
                if (!Directory.Exists(dataDir))
                {
                    Directory.CreateDirectory(dataDir);
                }

                if (File.Exists(_equipmentDataPath))
                {
                    var json = File.ReadAllText(_equipmentDataPath);
                    var equipmentData = JsonSerializer.Deserialize<List<NavalEquipment>>(json);
                    
                    if (equipmentData != null)
                    {
                        _equipmentList.Clear();
                        foreach (var equipment in equipmentData)
                        {
                            _equipmentList.Add(equipment);
                        }
                    }
                }
                // 初回起動時はデータファイルが存在しないため、空のリストで開始
            }
            catch (Exception ex)
            {
                Console.WriteLine($"装備データの読み込み中にエラーが発生しました: {ex.Message}");
                // エラー時は空のリストを使用
                _equipmentList.Clear();
            }
        }
        
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
                _equipmentList.Remove(selectedEquipment);
                SaveEquipmentData();
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
                    Attack = selectedEquipment.Attack,
                    Defense = selectedEquipment.Defense,
                    SpecialAbility = selectedEquipment.SpecialAbility,
                    AdditionalProperties = new Dictionary<string, object>(selectedEquipment.AdditionalProperties)
                };
                
                _equipmentList.Add(newEquipment);
                SaveEquipmentData();
                
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
                    //editorWindow = new SMLG_Design_View(equipment, _categories, _tierYears);
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
                    // 既存の装備を編集した場合
                    if (_equipmentList.Contains(equipment))
                    {
                        int index = _equipmentList.IndexOf(equipment);
                        if (index >= 0)
                        {
                            _equipmentList[index] = result;
                        }
                    }
                    else // 新規装備の場合
                    {
                        _equipmentList.Add(result);
                    }
                    
                    // データを保存
                    SaveEquipmentData();
                }
            }
        }
    }
    
    // 海軍装備クラス
    public class NavalEquipment
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public int Year { get; set; }
        public int Tier { get; set; }
        public string Country { get; set; } // 追加
        public double Attack { get; set; }
        public double Defense { get; set; }
        public string SpecialAbility { get; set; }
        public Dictionary<string, object> AdditionalProperties { get; set; } = new Dictionary<string, object>();
    }
    
    // カテゴリクラス
    public class NavalCategory
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public List<string> SubCategories { get; set; } = new List<string>();
    }
    
    // カテゴリ選択結果
    public class CategorySelectionResult
    {
        public string CategoryId { get; set; }
        public int TierId { get; set; }
    }
}