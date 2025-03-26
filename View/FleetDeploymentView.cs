using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Collections.ObjectModel;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Layout;
using Avalonia.Threading;

namespace HOI4NavalModder
{
    public class FleetDeploymentView : UserControl
    {
        // モデルクラス
        public class CountryInfo
        {
            public string Tag { get; set; }
            public string Name { get; set; }
            public string FlagPath { get; set; }
            public Bitmap FlagImage { get; set; }
            public bool IsSelected { get; set; }
        }

        private readonly ObservableCollection<CountryInfo> _countriesList = new ObservableCollection<CountryInfo>();
        private ListBox _countriesListBox;
        private TextBlock _statusTextBlock;
        private Button _refreshButton;
        private Button _exportButton;
        private TextBox _searchBox;
        private CheckBox _showAllCountriesCheckBox;
        private ProgressBar _loadingProgressBar;
        
        // 設定ファイルパス
        private readonly string _configFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HOI4NavalModder",
            "modpaths.json");
            
        private string _activeMod;
        private string _vanillaPath;
        
        public FleetDeploymentView()
        {
            var grid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*")
            };

            var headerPanel = new Panel
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D30")),
                Height = 40
            };

            var headerText = new TextBlock
            {
                Text = "艦隊配備",
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(20, 0, 0, 0),
                FontSize = 16
            };

            headerPanel.Children.Add(headerText);
            Grid.SetRow(headerPanel, 0);

            // メインコンテンツ
            var mainPanel = new StackPanel
            {
                Margin = new Thickness(20)
            };

            // 説明テキスト
            var descriptionText = new TextBlock
            {
                Text = "国家別の艦隊配備設定ができます",
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 20)
            };
            mainPanel.Children.Add(descriptionText);

            // ツールバー
            var toolbarPanel = new Panel
            {
                Background = new SolidColorBrush(Color.Parse("#333337")),
                Height = 40,
                Margin = new Thickness(0, 0, 0, 1)
            };

            var toolbar = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10, 5, 0, 0)
            };

            _refreshButton = new Button
            {
                Content = "更新",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 5, 0),
                Background = new SolidColorBrush(Color.Parse("#3E3E42")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0)
            };
            _refreshButton.Click += OnRefreshButtonClick;

            _exportButton = new Button
            {
                Content = "書き出し",
                Padding = new Thickness(10, 5, 10, 5),
                Margin = new Thickness(0, 0, 5, 0),
                Background = new SolidColorBrush(Color.Parse("#3E3E42")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                IsEnabled = false // 初期状態では無効
            };
            _exportButton.Click += OnExportButtonClick;

            // 検索ボックス
            _searchBox = new TextBox
            {
                Watermark = "国家検索...",
                Width = 200,
                Margin = new Thickness(10, 0, 0, 0),
                Background = new SolidColorBrush(Color.Parse("#252526")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46"))
            };
            _searchBox.TextChanged += OnSearchTextChanged;

            // 全国家表示チェックボックス
            _showAllCountriesCheckBox = new CheckBox
            {
                Content = "全国家表示",
                Margin = new Thickness(10, 0, 0, 0),
                Foreground = Brushes.White,
                IsChecked = false
            };
            _showAllCountriesCheckBox.IsCheckedChanged += OnShowAllCountriesChanged;

            toolbar.Children.Add(_refreshButton);
            toolbar.Children.Add(_exportButton);
            toolbar.Children.Add(_searchBox);
            toolbar.Children.Add(_showAllCountriesCheckBox);
            toolbarPanel.Children.Add(toolbar);

            mainPanel.Children.Add(toolbarPanel);

            // ロード中表示用プログレスバー
            _loadingProgressBar = new ProgressBar
            {
                IsIndeterminate = true,
                Height = 4,
                Margin = new Thickness(0, 1, 0, 1),
                IsVisible = false
            };
            mainPanel.Children.Add(_loadingProgressBar);

            // 国家リスト
            var listContainer = new Border
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D30")),
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
                Padding = new Thickness(5),
                Height = 400
            };

            _countriesListBox = new ListBox
            {
                Background = new SolidColorBrush(Color.Parse("#2D2D30")),
                Foreground = Brushes.White,
                BorderThickness = new Thickness(0),
                ItemsSource = _countriesList,
                SelectionMode = SelectionMode.Multiple
            };
            _countriesListBox.SelectionChanged += OnCountrySelectionChanged;
            
            // リストアイテムテンプレート
            _countriesListBox.ItemTemplate = new FuncDataTemplate<CountryInfo>((item, scope) =>
            {
                if (item == null)
                {
                    return new TextBlock { Text = "データがありません", Foreground = Brushes.White };
                }

                var panel = new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"),
                    Margin = new Thickness(5)
                };

                // チェックボックス
                var checkBox = new CheckBox
                {
                    IsChecked = item.IsSelected,
                    Margin = new Thickness(0, 0, 10, 0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                checkBox.IsCheckedChanged += (s, e) => 
                { 
                    item.IsSelected = checkBox.IsChecked ?? false;
                };
                Grid.SetColumn(checkBox, 0);

                // 国旗と国名
                var contentPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 10, 0)
                };

                // 国旗表示
                var flagBorder = new Border
                {
                    Width = 40,
                    Height = 24,
                    Margin = new Thickness(0, 0, 10, 0),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
                    ClipToBounds = true
                };

                if (item.FlagImage != null)
                {
                    var flagImage = new Image
                    {
                        Source = item.FlagImage,
                        Width = 40,
                        Height = 24,
                        Stretch = Stretch.Uniform
                    };
                    flagBorder.Child = flagImage;
                }
                else
                {
                    var noFlagText = new TextBlock
                    {
                        Text = "?",
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        Foreground = Brushes.White
                    };
                    flagBorder.Child = noFlagText;
                }

                // 国名とタグ
                var infoStack = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center
                };

                var countryNameText = new TextBlock
                {
                    Text = item.Name ?? "不明な国家",
                    Foreground = Brushes.White,
                    FontWeight = FontWeight.Bold
                };

                var countryTagText = new TextBlock
                {
                    Text = item.Tag,
                    Foreground = new SolidColorBrush(Color.Parse("#CCCCCC")),
                    FontSize = 12
                };

                infoStack.Children.Add(countryNameText);
                infoStack.Children.Add(countryTagText);

                contentPanel.Children.Add(flagBorder);
                contentPanel.Children.Add(infoStack);
                
                Grid.SetColumn(contentPanel, 1);

                // 配備ボタン
                var configButton = new Button
                {
                    Content = "配備設定",
                    Padding = new Thickness(8, 4, 8, 4),
                    Background = new SolidColorBrush(Color.Parse("#1E90FF")),
                    Foreground = Brushes.White,
                    BorderThickness = new Thickness(0),
                    VerticalAlignment = VerticalAlignment.Center
                };
                configButton.Click += (s, e) => OnConfigCountryClick(item);
                Grid.SetColumn(configButton, 2);

                panel.Children.Add(checkBox);
                panel.Children.Add(contentPanel);
                panel.Children.Add(configButton);
                
                return panel;
            });
            
            listContainer.Child = _countriesListBox;
            mainPanel.Children.Add(listContainer);

            // ステータステキスト
            _statusTextBlock = new TextBlock
            {
                Text = "アクティブなMODを検出中...",
                Foreground = Brushes.White,
                Margin = new Thickness(5, 10, 0, 0)
            };
            mainPanel.Children.Add(_statusTextBlock);

            Grid.SetRow(mainPanel, 1);
            grid.Children.Add(headerPanel);
            grid.Children.Add(mainPanel);

            Content = grid;
            
            // 初期ロード
            LoadModConfigAndInitialize();
        }

        private void LoadModConfigAndInitialize()
        {
            try
            {
                // MOD設定の読み込み
                if (File.Exists(_configFilePath))
                {
                    var json = File.ReadAllText(_configFilePath);
                    var config = JsonSerializer.Deserialize<ModConfig>(json);

                    if (config != null)
                    {
                        _vanillaPath = config.VanillaGamePath;
                        var activeMod = config.Mods?.FirstOrDefault(m => m.IsActive);
                        
                        if (activeMod != null)
                        {
                            _activeMod = activeMod.Path;
                            _statusTextBlock.Text = $"アクティブMOD: {activeMod.Name}";
                        }
                        else
                        {
                            _statusTextBlock.Text = "アクティブなMODが設定されていません。";
                        }
                        
                        // 国家データのロード
                        LoadCountryData();
                    }
                }
                else
                {
                    _statusTextBlock.Text = "MOD設定ファイルが見つかりません。設定メニューで設定してください。";
                }
            }
            catch (Exception ex)
            {
                _statusTextBlock.Text = $"設定読み込みエラー: {ex.Message}";
            }
        }

// In the LoadCountryData method of FleetDeploymentView.cs
private async void LoadCountryData()
{
    _loadingProgressBar.IsVisible = true;
    _refreshButton.IsEnabled = false;
    _countriesList.Clear();
    
    // UIスレッドをブロックしないために非同期で実行
    await Dispatcher.UIThread.InvokeAsync(async () => 
    {
        try
        {
            // MODパスのチェック - バニラパスがあれば国家データ読み込みを続行
            if (string.IsNullOrEmpty(_activeMod) && string.IsNullOrEmpty(_vanillaPath))
            {
                _statusTextBlock.Text = "MODパスまたはバニラパスが設定されていません。";
                _loadingProgressBar.IsVisible = false;
                _refreshButton.IsEnabled = true;
                return;
            }

            List<string> countryTags = new List<string>();
            Dictionary<string, string> tagDescriptions = new Dictionary<string, string>();
            bool hasReplaceTags = false;

            // MODのdescriptor.modを確認（replace_path設定の確認）
            if (!string.IsNullOrEmpty(_activeMod))
            {
                string descriptorPath = Path.Combine(_activeMod, "descriptor.mod");
                if (File.Exists(descriptorPath))
                {
                    string[] descriptorLines = await File.ReadAllLinesAsync(descriptorPath);
                    hasReplaceTags = descriptorLines.Any(line => 
                        line.Contains("replace_path=\"common/country_tags\""));
                }
            }

            // 国家タグの収集 - MODから取得
            if (!string.IsNullOrEmpty(_activeMod))
            {
                string modTagsPath = Path.Combine(_activeMod, "common", "country_tags");
                if (Directory.Exists(modTagsPath))
                {
                    foreach (var file in Directory.GetFiles(modTagsPath, "*.txt"))
                    {
                        CollectCountryTags(file, countryTags, tagDescriptions);
                    }
                }
            }

            // バニラからも国家タグを取得 (MODが置き換えてない場合または_activeMod がない場合)
            if ((!hasReplaceTags || string.IsNullOrEmpty(_activeMod)) && !string.IsNullOrEmpty(_vanillaPath))
            {
                string vanillaTagsPath = Path.Combine(_vanillaPath, "common", "country_tags");
                if (Directory.Exists(vanillaTagsPath))
                {
                    foreach (var file in Directory.GetFiles(vanillaTagsPath, "*.txt"))
                    {
                        CollectCountryTags(file, countryTags, tagDescriptions);
                    }
                }
            }

            // 残りのコードは変更なし...

                    // 国名の取得
                    Dictionary<string, string> countryNames = new Dictionary<string, string>();
                    
                    // IDE設定から言語設定を取得
                    bool isJapanese = true; // デフォルトは日本語
                    string ideSettingsPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "HOI4NavalModder",
                        "idesettings.json");
                        
                    if (File.Exists(ideSettingsPath))
                    {
                        try
                        {
                            var json = await File.ReadAllTextAsync(ideSettingsPath);
                            var settings = JsonSerializer.Deserialize<IDESettings>(json);
                            if (settings != null)
                            {
                                isJapanese = settings.IsJapanese;
                            }
                        }
                        catch {}
                    }
                    
                    string localeSuffix = isJapanese ? "japanese" : "english";
                    
                    // MODからのローカライズ取得
                    if (!string.IsNullOrEmpty(_activeMod))
                    {
                        string locPath = Path.Combine(_activeMod, "localisation", localeSuffix);
                        if (Directory.Exists(locPath))
                        {
                            CollectCountryNames(locPath, countryNames);
                        }
                        else
                        {
                            // 下位フォルダなしのケース
                            locPath = Path.Combine(_activeMod, "localisation");
                            if (Directory.Exists(locPath))
                            {
                                CollectCountryNames(locPath, countryNames);
                            }
                        }
                    }
                    
                    // バニラからのローカライズ取得（MODになければ）
                    if (!string.IsNullOrEmpty(_vanillaPath))
                    {
                        string locPath = Path.Combine(_vanillaPath, "localisation", localeSuffix);
                        if (Directory.Exists(locPath))
                        {
                            foreach (var tag in countryTags)
                            {
                                if (!countryNames.ContainsKey(tag))
                                {
                                    CollectCountryNames(locPath, countryNames);
                                }
                            }
                        }
                    }

                                            // 国旗の収集と国家リストの生成
                    foreach (var tag in countryTags)
                    {
                        string flagPath = null;
                        Bitmap flagImage = null;
                        
                        // MODから国旗を検索
                        if (!string.IsNullOrEmpty(_activeMod))
                        {
                            string modFlagPath = Path.Combine(_activeMod, "gfx", "flags", $"{tag}.tga");
                            if (File.Exists(modFlagPath))
                            {
                                flagPath = modFlagPath;
                                try
                                {
                                    // TGAデコーダーを使用して国旗画像を読み込む
                                    flagImage = TgaDecoder.LoadFromFile(modFlagPath);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"MOD国旗読み込みエラー: {ex.Message}");
                                }
                            }
                        }
                        
                        // バニラから国旗を検索（MODになければ）
                        if ((flagPath == null || flagImage == null) && !string.IsNullOrEmpty(_vanillaPath))
                        {
                            string vanillaFlagPath = Path.Combine(_vanillaPath, "gfx", "flags", $"{tag}.tga");
                            if (File.Exists(vanillaFlagPath))
                            {
                                flagPath = vanillaFlagPath;
                                try
                                {
                                    // TGAデコーダーを使用して国旗画像を読み込む
                                    flagImage = TgaDecoder.LoadFromFile(vanillaFlagPath);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"バニラ国旗読み込みエラー: {ex.Message}");
                                }
                            }
                        }
                        
                        // 国名がない場合はタグ説明から代替テキストを作成
                        string countryName = null;
                        if (countryNames.TryGetValue(tag, out var name))
                        {
                            countryName = name;
                        }
                        else if (tagDescriptions.TryGetValue(tag, out var desc))
                        {
                            // ファイルパスから国名を推測（"countries/Western Europe.txt" → "Western Europe"）
                            var match = Regex.Match(desc, @"countries/([^.]+)\.txt");
                            if (match.Success)
                            {
                                countryName = match.Groups[1].Value;
                            }
                            else
                            {
                                countryName = desc;
                            }
                        }
                        
                        // 主要国か判定（3文字以下のタグは主要国と仮定）
                        bool isMajorCountry = tag.Length <= 3;
                        
                        // 全国家表示がオンか、主要国の場合のみ追加（初期状態）
                        if (_showAllCountriesCheckBox.IsChecked == true || isMajorCountry)
                        {
                            _countriesList.Add(new CountryInfo
                            {
                                Tag = tag,
                                Name = countryName,
                                FlagPath = flagPath,
                                FlagImage = flagImage,
                                IsSelected = false
                            });
                        }
                    }
                    
                    // 国名でソート
                    var sorted = _countriesList.OrderBy(c => c.Name).ToList();
                    _countriesList.Clear();
                    foreach (var country in sorted)
                    {
                        _countriesList.Add(country);
                    }
                    
                    _statusTextBlock.Text = $"合計 {countryTags.Count} か国を検出しました。表示: {_countriesList.Count} か国";
                }
                catch (Exception ex)
                {
                    _statusTextBlock.Text = $"国家データ読み込みエラー: {ex.Message}";
                }
                finally
                {
                    _loadingProgressBar.IsVisible = false;
                    _refreshButton.IsEnabled = true;
                }
            });
        }

        private void CollectCountryTags(string filePath, List<string> tags, Dictionary<string, string> descriptions)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    // コメントを除去
                    string trimmedLine = line.Split('#')[0].Trim();
                    if (string.IsNullOrEmpty(trimmedLine))
                    {
                        continue;
                    }
                    
                    // 複数のパターンを試す
                    // パターン1: 標準的な定義 - IBE = "countries/Western Europe.txt"
                    var match = Regex.Match(trimmedLine, @"([A-Za-z0-9_]{2,})\s*=\s*[""'](.+?)[""']");
                    
                    // パターン2: 引用符なしの定義 - IBE = countries/Western Europe.txt
                    if (!match.Success)
                    {
                        match = Regex.Match(trimmedLine, @"([A-Za-z0-9_]{2,})\s*=\s*([^\s#]+)");
                    }
                    
                    // パターン3: 動的定義 - dynamic_tags = { IBE FRA GER }
                    if (!match.Success && trimmedLine.Contains("dynamic_tags") || trimmedLine.Contains("allowed_tags"))
                    {
                        var dynamicTagMatches = Regex.Matches(trimmedLine, @"([A-Za-z0-9_]{2,})");
                        foreach (Match dynamicMatch in dynamicTagMatches)
                        {
                            string potentialTag = dynamicMatch.Groups[1].Value;
                            // 一般的なキーワードを除外
                            if (potentialTag != "dynamic_tags" && potentialTag != "allowed_tags" && !tags.Contains(potentialTag))
                            {
                                tags.Add(potentialTag);
                                descriptions[potentialTag] = "Dynamic Tag";
                            }
                        }
                        continue;
                    }
                    
                    if (match.Success)
                    {
                        string tag = match.Groups[1].Value.ToUpper(); // TAGは通常大文字で標準化
                        string description = match.Groups[2].Value;
                        
                        if (!tags.Contains(tag))
                        {
                            tags.Add(tag);
                            descriptions[tag] = description;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"国家タグファイル読み込みエラー: {ex.Message}");
            }
        }

        private void CollectCountryNames(string localizationDir, Dictionary<string, string> countryNames)
        {
            try
            {
                // YMLファイルを再帰的に検索
                foreach (var file in Directory.GetFiles(localizationDir, "*.yml", SearchOption.AllDirectories))
                {
                    string[] lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        // 国名のローカライズキーを検索（例: IBE:0 "イベリア連合"）
                        var match = Regex.Match(line, @"([A-Z0-9_]{2,}):0 *""(.+?)""");
                        if (match.Success)
                        {
                            string tag = match.Groups[1].Value;
                            string name = match.Groups[2].Value;
                            
                            // 特殊文字やフォーマット指定子を削除
                            name = Regex.Replace(name, @"\$.*?\$", "");
                            name = Regex.Replace(name, @"\§[A-Za-z]", "");
                            name = name.Trim();
                            
                            if (!string.IsNullOrEmpty(name) && !countryNames.ContainsKey(tag))
                            {
                                countryNames[tag] = name;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ローカライゼーション読み込みエラー: {ex.Message}");
            }
        }

        private void OnRefreshButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            LoadModConfigAndInitialize();
        }

        private void OnExportButtonClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 選択された国家の配備設定をエクスポート
            var selectedCountries = _countriesList.Where(c => c.IsSelected).ToList();
            if (selectedCountries.Count > 0)
            {
                _statusTextBlock.Text = $"{selectedCountries.Count} か国の配備設定をエクスポート中...";
                // ここにエクスポートロジックを実装
            }
            else
            {
                _statusTextBlock.Text = "エクスポートする国家が選択されていません。";
            }
        }

        private void OnSearchTextChanged(object sender, Avalonia.Controls.TextChangedEventArgs e)
        {
            string searchText = _searchBox.Text?.ToLower() ?? "";
            
            // 検索テキストが空の場合は全表示/主要国のみの設定に従う
            if (string.IsNullOrWhiteSpace(searchText))
            {
                OnShowAllCountriesChanged(null, null);
                return;
            }
            
            // 検索条件に一致する国家のみ表示
            var originalList = _countriesList.ToList();
            _countriesList.Clear();
            
            foreach (var country in originalList)
            {
                bool nameMatch = country.Name?.ToLower().Contains(searchText) ?? false;
                bool tagMatch = country.Tag.ToLower().Contains(searchText);
                
                if (nameMatch || tagMatch)
                {
                    _countriesList.Add(country);
                }
            }
            
            _statusTextBlock.Text = $"検索結果: {_countriesList.Count} か国";
        }

        private void OnShowAllCountriesChanged(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            // 検索テキストがある場合は検索を優先
            if (!string.IsNullOrWhiteSpace(_searchBox.Text))
            {
                OnSearchTextChanged(null, null);
                return;
            }
            
            // 国家リストの再読み込み
            LoadCountryData();
        }

        private void OnCountrySelectionChanged(object sender, Avalonia.Controls.SelectionChangedEventArgs e)
        {
            // 選択された国家の数に基づいてエクスポートボタンの有効/無効を切り替え
            int selectedCount = _countriesList.Count(c => c.IsSelected);
            _exportButton.IsEnabled = selectedCount > 0;
            
            if (selectedCount > 0)
            {
                _statusTextBlock.Text = $"{selectedCount} か国が選択されています";
            }
        }

        private void OnConfigCountryClick(CountryInfo country)
        {
            // 国家配備設定ダイアログを表示
            _statusTextBlock.Text = $"{country.Name ?? country.Tag} の配備設定を編集中...";
            Console.WriteLine($"配備設定: {country.Tag}");
            
              
            // 新たな艦隊配備マップビューを表示
            var mapView = new FleetDeploymentWindow(
                country.Tag, 
                country.Name ?? country.Tag, 
                country.FlagImage, 
                _activeMod, 
                _vanillaPath);
                
            // ダイアログとして表示
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                mapView.ShowDialog((Window)topLevel);
            }
        }   
    }
    
  
}