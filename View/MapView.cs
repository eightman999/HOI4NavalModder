// using System;
// using System.Collections.Generic;
// using System.Collections.Concurrent;
// using System.IO;
// using System.Linq;
// using System.Threading.Tasks;
// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Input;
// using Avalonia.Media;
// using Avalonia.Media.Imaging;
// using Avalonia.Threading;
// using Avalonia.Layout;
// using Avalonia.Platform;
// using Avalonia.Controls.Primitives;
// using Avalonia.Interactivity;
//
// namespace HOI4NavalModder
// {
//     public class MapViewer : UserControl
//     {
//         private Dictionary<int, NavalBase> _navalBaseMarkers = new Dictionary<int, NavalBase>();
//         private StateDataLoader _stateDataLoader;
//         private MapCacheManager _cacheManager;
//         private string _gameVersion = "1.12.14"; // ゲームバージョンを指定（実際の使用時は設定から取得）
//         private string _modName;
//         // マップ画像
//         private Bitmap _mapImage;
//         private WriteableBitmap _zoomableMap;
//         private Image _mapImageControl;
//         private ScrollViewer _mapScrollViewer;
//         private Canvas _markersCanvas;
//         private ToolTip _provinceTooltip;
//         private TextBlock _tooltipTextBlock;
//         // 座標からプロヴィンスへのマッピング
//         private Dictionary<Point, int> _pixelProvinceMapping = new Dictionary<Point, int>();
//         private bool _isProvinceMapInitialized = false;
//         // 高速アクセス用の色検索マップ
//         private Dictionary<(byte R, byte G, byte B), ProvinceInfo> _colorLookupMap;
//         // 情報パネル
//         private StackPanel _infoPanel;
//         private TextBlock _infoTextBlock;
//         
//         // プロヴィンスデータ
//         private Dictionary<Color, ProvinceInfo> _provinceData = new Dictionary<Color, ProvinceInfo>();
//         
//         // 現在選択中のプロヴィンス
//         private ProvinceInfo _selectedProvince;
//         
//         // マップ表示モード
//         public enum MapMode
//         {
//             Provinces,
//             States
//         }
//         
//         private MapMode _currentMapMode = MapMode.Provinces;
//         private ComboBox _mapModeComboBox;
//         
//         // ズーム関連
//         private double _zoomFactor = 1.0;
//         private const double MIN_ZOOM = 0.5;
//         private const double MAX_ZOOM = 4.0;
//         private const double ZOOM_STEP = 0.1;
//         
//         // パス
//         private string _provincesMapPath;
//         private string _provincesDefinitionPath;
//         private string _statesMapPath;
//         private string _statesDefinitionPath;
//         
//         // モデルクラス
//         public class ProvinceInfo
//         {
//             public int Id { get; set; }
//             public Color Color { get; set; }
//             public string Type { get; set; } // sea/lake/land
//             public bool IsCoastal { get; set; }
//             public string Terrain { get; set; }
//             public string Continent { get; set; }
//             public List<int> AdjacentProvinces { get; set; } = new List<int>();
//             public int StateId { get; set; } = -1;
//     
//             // デフォルトコンストラクタ（JSONシリアライザ用）
//             public ProvinceInfo() { }
//     
//             // クローン作成用コンストラクタ
//             public ProvinceInfo(ProvinceInfo source)
//             {
//                 Id = source.Id;
//                 Color = source.Color;
//                 Type = source.Type;
//                 IsCoastal = source.IsCoastal;
//                 Terrain = source.Terrain;
//                 Continent = source.Continent;
//                 AdjacentProvinces = new List<int>(source.AdjacentProvinces);
//                 StateId = source.StateId;
//             }
//     
//             public override string ToString()
//             {
//                 return $"プロヴィンスID: {Id}\n" +
//                        $"色: R:{Color.R} G:{Color.G} B:{Color.B}\n" +
//                        $"種類: {Type}\n" +
//                        $"沿岸部: {(IsCoastal ? "はい" : "いいえ")}\n" +
//                        $"地形: {Terrain}\n" +
//                        $"大陸: {Continent}\n" +
//                        $"ステートID: {(StateId >= 0 ? StateId.ToString() : "なし")}";
//             }
//         }
//         
//         public MapViewer()
//         {
//             InitializeComponent();
//             
//             // キャッシュマネージャーの初期化
//             _cacheManager = new MapCacheManager();
//         }
//         
//         private void InitializeComponent()
//         {
//             // メインレイアウト
//             var mainGrid = new Grid
//             {
//                 RowDefinitions = new RowDefinitions("Auto,*")
//             };
//             
//             // 上部コントロールパネル
//             var controlPanel = new StackPanel
//             {
//                 Orientation = Avalonia.Layout.Orientation.Horizontal,
//                 Margin = new Thickness(10),
//                 Spacing = 10
//             };
//             
//             // マップモード選択
//             var modeLabel = new TextBlock
//             {
//                 Text = "マップモード:",
//                 VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
//                 Foreground = Brushes.White
//             };
//             
//             _mapModeComboBox = new ComboBox
//             {
//                 Width = 150,
//                 SelectedIndex = 0
//             };
//             _mapModeComboBox.Items.Add("プロヴィンス");
//             _mapModeComboBox.Items.Add("ステート");
//             _mapModeComboBox.SelectionChanged += OnMapModeChanged;
//             
//             // ズームコントロール
//             var zoomOutButton = new Button
//             {
//                 Content = "-",
//                 Width = 30,
//                 Height = 30,
//                 Padding = new Thickness(0),
//                 VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
//                 HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
//             };
//             zoomOutButton.Click += OnZoomOutClick;
//             
//             var zoomInButton = new Button
//             {
//                 Content = "+",
//                 Width = 30,
//                 Height = 30,
//                 Padding = new Thickness(0),
//                 VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
//                 HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
//             };
//             zoomInButton.Click += OnZoomInClick;
//             
//             var zoomResetButton = new Button
//             {
//                 Content = "100%",
//                 Padding = new Thickness(8, 0, 8, 0),
//                 Height = 30,
//                 VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
//                 HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
//             };
//             zoomResetButton.Click += OnZoomResetClick;
//             
//             controlPanel.Children.Add(modeLabel);
//             controlPanel.Children.Add(_mapModeComboBox);
//             controlPanel.Children.Add(zoomOutButton);
//             controlPanel.Children.Add(zoomInButton);
//             controlPanel.Children.Add(zoomResetButton);
//             
//             Grid.SetRow(controlPanel, 0);
//             
//             // マップスクロールビュー - ScrollBarVisibilityの修正
//             _mapScrollViewer = new ScrollViewer
//             {
//                 HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
//                 VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
//                 Padding = new Thickness(0),
//                 Background = Brushes.Black
//             };
//             
//             // マーカー用のキャンバスを作成
//             _markersCanvas = new Canvas
//             {
//                 IsHitTestVisible = false // マウスイベントを下層に通過させる
//             };
//             
//             // マップ画像コントロール
//             _mapImageControl = new Image
//             {
//                 Stretch = Stretch.None,
//                 HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
//                 VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
//             };
//             var mapContainer = new Grid
//             {
//                 ColumnDefinitions = new ColumnDefinitions("*,250") // 情報パネルの幅を250に設定
//             };
//     
//             // マップスクロールビューアの設定を修正
//             _mapScrollViewer = new ScrollViewer
//             {
//                 HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
//                 VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
//                 Padding = new Thickness(0),
//                 Background = Brushes.Black,
//                 HorizontalAlignment = HorizontalAlignment.Stretch,
//                 VerticalAlignment = VerticalAlignment.Stretch
//             };
//             // ツールチップの設定
//             _tooltipTextBlock = new TextBlock
//             {
//                 MaxWidth = 300,
//                 TextWrapping = TextWrapping.Wrap,
//                 Padding = new Thickness(5)
//             };
//             
//             // ToolTipインスタンスの作成
//             _provinceTooltip = new ToolTip
//             {
//                 Content = _tooltipTextBlock
//             };
//             
//             // Panel を使用して Image と Canvas を重ねる
//             var mapPanel = new Panel();
//             mapPanel.Children.Add(_mapImageControl);
//             mapPanel.Children.Add(_markersCanvas);
//             
//             _mapScrollViewer.Content = mapPanel;
//             
//             // ツールチップを設定
//             ToolTip.SetTip(_mapImageControl, _tooltipTextBlock);
//             _provinceTooltip.IsVisible = false;
//             
//             // マウスイベントの設定
//             _mapImageControl.PointerMoved += OnMapPointerMoved;
//             _mapImageControl.PointerPressed += OnMapPointerPressed;
//             _mapImageControl.PointerWheelChanged += OnMapPointerWheelChanged;
//             
//             Grid.SetColumn(_mapScrollViewer, 0);
//             
//             // 情報パネル
//             var infoPanelBorder = new Border
//             {
//                 BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
//                 BorderThickness = new Thickness(1, 0, 0, 0),
//                 Padding = new Thickness(10)
//             };
//             
//             _infoPanel = new StackPanel
//             {
//                 Spacing = 10
//             };
//             
//             var infoPanelHeader = new TextBlock
//             {
//                 Text = "プロヴィンス情報",
//                 FontWeight = FontWeight.Bold,
//                 Foreground = Brushes.White,
//                 Margin = new Thickness(0, 0, 0, 10)
//             };
//             
//             _infoTextBlock = new TextBlock
//             {
//                 Text = "プロヴィンスを選択してください",
//                 Foreground = Brushes.White,
//                 TextWrapping = TextWrapping.Wrap
//             };
//             
//             _infoPanel.Children.Add(infoPanelHeader);
//             _infoPanel.Children.Add(_infoTextBlock);
//             
//             infoPanelBorder.Child = _infoPanel;
//             Grid.SetColumn(infoPanelBorder, 1);
//             
//             mapContainer.Children.Add(_mapScrollViewer);
//             mapContainer.Children.Add(infoPanelBorder);
//             
//             Grid.SetRow(mapContainer, 1);
//             
//             mainGrid.Children.Add(controlPanel);
//             mainGrid.Children.Add(mapContainer);
//             
//             // 利用可能なスペースを埋めるようにスクロールビューアを設定
//             _mapScrollViewer.SetValue(Grid.ColumnProperty, 0);
//             _mapScrollViewer.SetValue(Grid.RowProperty, 0);
//     
//             // ウィンドウが利用可能なスペースを最大化するよう保証
//             Width = 1200;
//             Height = 800;
//     
//             // マップスクロールビューアが伸縮して利用可能なスペースを埋めるよう設定
//             _mapScrollViewer.HorizontalAlignment = HorizontalAlignment.Stretch;
//             _mapScrollViewer.VerticalAlignment = VerticalAlignment.Stretch;
//     
//             // 最小サイズを設定して、コンテンツが適切にスクロールできるよう保証
//             _mapScrollViewer.MinWidth = 600;
//             _mapScrollViewer.MinHeight = 400;
//             Content = mainGrid;
//         }
//         
//         // 初期化処理 - キャッシュを利用するように更新
//         public async Task Initialize(string vanillaPath, string modPath)
//         {
//             try
//             {
//                 // MOD名を取得
//                 _modName = Path.GetFileName(modPath);
//         
//                 // マップファイルのパスを設定
//                 _provincesMapPath = Path.Combine(modPath, "map", "provinces.bmp");
//                 _provincesDefinitionPath = Path.Combine(modPath, "map", "definition.csv");
//         
//                 // MODにマップがない場合はバニラから読み込む
//                 bool isModMap = true;
//         
//                 // MODのdescriptor.modを確認（replace_path設定の確認）
//                 string descriptorPath = Path.Combine(modPath, "descriptor.mod");
//                 if (File.Exists(descriptorPath))
//                 {
//                     string[] descriptorLines = await File.ReadAllLinesAsync(descriptorPath);
//                     isModMap = descriptorLines.Any(line => 
//                         line.Contains("replace_path=\"map\""));
//                 }
//         
//                 // MODにマップがなければバニラのマップを使用
//                 if (!isModMap || !File.Exists(_provincesMapPath))
//                 {
//                     _provincesMapPath = Path.Combine(vanillaPath, "map", "provinces.bmp");
//                     _provincesDefinitionPath = Path.Combine(vanillaPath, "map", "definition.csv");
//                     _modName = "vanilla";
//                 }
//         
//                 // マップのキャッシュパスを取得
//                 string cachePath = _cacheManager.GetCachePath(_modName, _gameVersion);
//         
//                 // キャッシュが有効かチェック
//                 bool isCacheValid = _cacheManager.IsCacheValid(cachePath, _provincesMapPath, _provincesDefinitionPath);
//         
//                 // マップ画像を読み込む
//                 if (File.Exists(_provincesMapPath))
//                 {
//                     _infoTextBlock.Text = "マップデータを読み込み中...";
//             
//                     _mapImage = new Bitmap(_provincesMapPath);
//             
//                     // マップ画像の初期表示
//                     UpdateMapImage();
//             
//                     // キャッシュが有効ならキャッシュから読み込み
//                     if (isCacheValid)
//                     {
//                         _infoTextBlock.Text = "キャッシュからプロヴィンスデータを読み込み中...";
//                 
//                         var (provinceData, pixelMap) = await _cacheManager.LoadProvinceDataFromCache(cachePath);
//                         if (provinceData != null && provinceData.Count > 0)
//                         {
//                             _provinceData = provinceData;
//                             
//                             // ピクセルマップもロード
//                             if (pixelMap != null && pixelMap.Count > 0)
//                             {
//                                 _pixelProvinceMapping = pixelMap;
//                                 _isProvinceMapInitialized = true;
//                                 InitializeColorMap();
//                                 
//                                 _infoTextBlock.Text = $"キャッシュから {_provinceData.Count} プロヴィンスと " +
//                                                     $"{_pixelProvinceMapping.Count} ピクセルマッピングを読み込みました";
//                                 return;
//                             }
//                             else
//                             {
//                                 // 色マップを初期化
//                                 InitializeColorMap();
//                                 
//                                 // ピクセルマップがなければ最適化アルゴリズムで生成
//                                 await InitializeOptimizedPixelMapping();
//                             }
//                         }
//                     }
//                     else
//                     {
//                         // キャッシュが無効または存在しない場合は通常の読み込み
//                         _infoTextBlock.Text = "プロヴィンスデータを読み込み中...";
//                         await LoadProvinceDefinitions();
//                         
//                         // 読み込みが成功したらキャッシュを作成
//                         if (_provinceData.Count > 0)
//                         {
//                             _infoTextBlock.Text = "キャッシュを作成中...";
//                             await _cacheManager.CreateCache(cachePath, _provincesMapPath, _provincesDefinitionPath, 
//                                                         _provinceData, _pixelProvinceMapping);
//                             _infoTextBlock.Text = $"{_provinceData.Count} プロヴィンスと " +
//                                                $"{_pixelProvinceMapping.Count} ピクセルマッピングを読み込み、キャッシュしました";
//                         }
//                     }
//                 }
//                 else
//                 {
//                     _infoTextBlock.Text = "マップファイルが見つかりません";
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _infoTextBlock.Text = $"マップ初期化エラー: {ex.Message}";
//             }
//         }
//         
//         // プロヴィンス定義ファイル読み込み
//         private async Task LoadProvinceDefinitions()
//         {
//             try
//             {
//                 if (File.Exists(_provincesDefinitionPath))
//                 {
//                     string[] lines = await File.ReadAllLinesAsync(_provincesDefinitionPath);
//                     
//                     _provinceData.Clear();
//                     
//                     // 並列処理で定義ファイルを処理
//                     var tempProvinceData = new ConcurrentDictionary<Color, ProvinceInfo>();
//                     
//                     await Task.Run(() => {
//                         Parallel.ForEach(lines.Skip(1), line => // ヘッダー行をスキップ
//                         {
//                             try
//                             {
//                                 string[] parts = line.Split(';');
//                                 
//                                 if (parts.Length >= 7)
//                                 {
//                                     int id = int.Parse(parts[0]);
//                                     int r = int.Parse(parts[1]);
//                                     int g = int.Parse(parts[2]);
//                                     int b = int.Parse(parts[3]);
//                                     
//                                     var color = Color.FromRgb((byte)r, (byte)g, (byte)b);
//                                     
//                                     var province = new ProvinceInfo
//                                     {
//                                         Id = id,
//                                         Color = color,
//                                         Type = parts[4], // sea/lake/land
//                                         IsCoastal = parts[5] == "1", // 1 = coastal
//                                         Terrain = parts.Length > 6 ? parts[6] : "不明",
//                                         Continent = parts.Length > 7 ? parts[7] : "不明"
//                                     };
//                                     
//                                     tempProvinceData.TryAdd(color, province);
//                                 }
//                             }
//                             catch
//                             {
//                                 // 無効な行はスキップ
//                             }
//                         });
//                     });
//                     
//                     // 並列処理結果をメインのディクショナリにコピー
//                     foreach (var entry in tempProvinceData)
//                     {
//                         _provinceData[entry.Key] = entry.Value;
//                     }
//                     
//                     // 色検索マップを初期化
//                     InitializeColorMap();
//                     
//                     _infoTextBlock.Text = $"{_provinceData.Count} プロヴィンスを読み込みました";
//                     
//                     // 最適化されたピクセルマッピングを構築
//                     await InitializeOptimizedPixelMapping();
//                 }
//                 else
//                 {
//                     _infoTextBlock.Text = "プロヴィンス定義ファイルが見つかりません";
//                 }
//             }
//             catch (Exception ex)
//             {
//                 _infoTextBlock.Text = $"プロヴィンス定義読み込みエラー: {ex.Message}";
//             }
//         }
//
//         // 最適化されたピクセルマッピング初期化
//         private async Task InitializeOptimizedPixelMapping()
//         {
//             if (_mapImage == null || _provinceData.Count == 0)
//             {
//                 _infoTextBlock.Text = "マップまたはプロヴィンスデータが読み込まれていません";
//                 return;
//             }
//             
//             _infoTextBlock.Text = "ピクセルマッピングを最適化して生成中...";
//             
//             await Task.Run(() =>
//             {
//                 try
//                 {
//                     // Color → ProvinceID のマッピングを作成
//                     Dictionary<(byte R, byte G, byte B), int> colorToIdMap = new Dictionary<(byte R, byte G, byte B), int>();
//                     
//                     foreach (var entry in _provinceData)
//                     {
//                         Color color = entry.Key;
//                         int id = entry.Value.Id;
//                         colorToIdMap[(color.R, color.G, color.B)] = id;
//                     }
//                     
//                     // サンプリングレート（大きなマップは間引く）
//                     int mapSize = Math.Max(_mapImage.PixelSize.Width, _mapImage.PixelSize.Height);
//                     int samplingRate;
//                     
//                     if (mapSize > 4000) samplingRate = 8;      // 非常に大きいマップ
//                     else if (mapSize > 2000) samplingRate = 5; // 大きいマップ
//                     else if (mapSize > 1000) samplingRate = 3; // 中サイズマップ
//                     else samplingRate = 2;                     // 小さいマップ
//                     
//                     // 並列処理用の結果コレクション
//                     var resultMapping = new ConcurrentDictionary<Point, int>();
//                     
//                     // 効率的なピクセルデータ処理
//                     using (var writeableBitmap = new WriteableBitmap(
//                         new PixelSize(_mapImage.PixelSize.Width, _mapImage.PixelSize.Height),
//                         new Vector(96, 96),
//                         PixelFormat.Bgra8888,
//                         AlphaFormat.Unpremul))
//                     {
//                         using (var fbLock = writeableBitmap.Lock())
//                         {
//                             // マップ画像からピクセルデータをコピー
//                             _mapImage.CopyPixels(
//                                 new PixelRect(0, 0, _mapImage.PixelSize.Width, _mapImage.PixelSize.Height),
//                                 fbLock.Address,
//                                 fbLock.RowBytes * _mapImage.PixelSize.Height,
//                                 0);
//                             
//                             unsafe
//                             {
//                                 // 並列処理でピクセルを処理
//                                 Parallel.For(0, _mapImage.PixelSize.Height, y =>
//                                 {
//                                     if (y % samplingRate != 0) return; // 間引き
//                                     
//                                     byte* scanline = (byte*)fbLock.Address + y * fbLock.RowBytes;
//                                     
//                                     for (int x = 0; x < _mapImage.PixelSize.Width; x += samplingRate) // 間引き
//                                     {
//                                         int pixelOffset = x * 4; // 4バイト/ピクセル (BGRA)
//                                         
//                                         byte b = scanline[pixelOffset];
//                                         byte g = scanline[pixelOffset + 1];
//                                         byte r = scanline[pixelOffset + 2];
//                                         // byte a = scanline[pixelOffset + 3]; // アルファは使用しない
//                                         
//                                         // 完全一致を検索
//                                         if (colorToIdMap.TryGetValue((r, g, b), out int id))
//                                         {
//                                             resultMapping[new Point(x, y)] = id;
//                                         }
//                                         else
//                                         {
//                                             // 許容範囲内の近似色を検索（必要な場合）
//                                             const int tolerance = 3;
//                                             bool found = false;
//                                             
//                                             foreach (var entry in colorToIdMap.Take(100)) // パフォーマンス向上のため最初の100色のみチェック
//                                             {
//                                                 var c = entry.Key;
//                                                 if (Math.Abs(c.R - r) <= tolerance &&
//                                                     Math.Abs(c.G - g) <= tolerance &&
//                                                     Math.Abs(c.B - b) <= tolerance)
//                                                 {
//                                                     resultMapping[new Point(x, y)] = entry.Value;
//                                                     found = true;
//                                                     break;
//                                                 }
//                                             }
//                                         }
//                                     }
//                                 });
//                             }
//                         }
//                     }
//                     
//                     // 結果をクラスのフィールドに設定
//                     _pixelProvinceMapping = new Dictionary<Point, int>(resultMapping);
//                     _isProvinceMapInitialized = true;
//                     
//                     Dispatcher.UIThread.Post(() => {
//                         _infoTextBlock.Text = $"{_pixelProvinceMapping.Count} ピクセルのマッピングを生成しました";
//                     });
//                 }
//                 catch (Exception ex)
//                 {
//                     Dispatcher.UIThread.Post(() => {
//                         _infoTextBlock.Text = $"ピクセルマッピングエラー: {ex.Message}";
//                     });
//                 }
//             });
//         }
//         
//         // 高速アクセス用の色検索マップを初期化
//         private void InitializeColorMap()
//         {
//             _colorLookupMap = new Dictionary<(byte R, byte G, byte B), ProvinceInfo>();
//             
//             foreach (var entry in _provinceData)
//             {
//                 Color color = entry.Key;
//                 _colorLookupMap[(color.R, color.G, color.B)] = entry.Value;
//             }
//         }
//         
//         // 直接ビットマップからピクセル色を取得
//         private Color GetPixelColorDirect(Bitmap bitmap, int x, int y)
//         {
//             // 範囲チェック
//             if (x < 0 || y < 0 || x >= bitmap.PixelSize.Width || y >= bitmap.PixelSize.Height)
//             {
//                 return Colors.Black; // 範囲外の場合は黒を返す
//             }
//
//             try
//             {
//                 // サイズ1x1の新しいWriteableBitmapを作成
//                 using (var tempBitmap = new WriteableBitmap(
//                     new PixelSize(1, 1),
//                     new Vector(96, 96),
//                     PixelFormat.Bgra8888, // AvaloninaでのRGBAフォーマット
//                     AlphaFormat.Unpremul))
//                 {
//                     // 書き込み可能ビットマップをロック
//                     using (var context = tempBitmap.Lock())
//                     {
//                         // 指定した座標の1x1領域を対象のビットマップからコピー
//                         bitmap.CopyPixels(
//                             new PixelRect(x, y, 1, 1),
//                             context.Address,
//                             context.RowBytes,
//                             0);
//
//                         // ピクセルデータを読み取り
//                         byte[] pixelData = new byte[4]; // BGRA形式
//                         System.Runtime.InteropServices.Marshal.Copy(context.Address, pixelData, 0, 4);
//
//                         // BGRA形式の順番でピクセルデータが格納されている
//                         return Color.FromArgb(
//                             pixelData[3], // Alpha
//                             pixelData[2], // Red   (BGRA順なので2番目)
//                             pixelData[1], // Green (BGRA順なので1番目)
//                             pixelData[0]  // Blue  (BGRA順なので0番目)
//                         );
//                     }
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"ピクセル色取得エラー: {ex.Message}");
//                 return Colors.Magenta; // エラーの場合は目立つ色を返す
//             }
//         }
//         // マップ上の座標からプロヴィンス情報を取得する新しいメソッド
// private ProvinceInfo GetProvinceAtPosition(double x, double y)
// {
//     if (!_isProvinceMapInitialized || _mapImage == null)
//         return null;
//             
//     // 実際の座標に変換
//     int mapX = (int)(x / _zoomFactor);
//     int mapY = (int)(y / _zoomFactor);
//     
//     // 範囲チェック
//     if (mapX < 0 || mapY < 0 || mapX >= _mapImage.PixelSize.Width || mapY >= _mapImage.PixelSize.Height)
//         return null;
//     
//     // 1. キャッシュされたピクセル→プロヴィンスIDをチェック（最も高速）
//     if (_pixelProvinceMapping.TryGetValue(new Point(mapX, mapY), out int exactId))
//     {
//         var province = _provinceData.Values.FirstOrDefault(p => p.Id == exactId);
//         if (province != null)
//             return province;
//     }
//     
//     // 2. 直接ピクセルの色を取得
//     Color pixelColor = GetPixelColorDirect(_mapImage, mapX, mapY);
//             
//     // 3. 高速な色ルックアップマップで検索
//     var colorKey = (pixelColor.R, pixelColor.G, pixelColor.B);
//     if (_colorLookupMap.TryGetValue(colorKey, out var provinceByExactColor))
//     {
//         // キャッシュに追加して次回高速化
//         _pixelProvinceMapping[new Point(mapX, mapY)] = provinceByExactColor.Id;
//         return provinceByExactColor;
//     }
//     
//     // 4. 近接する座標を検索
//     const int searchRadius = 3;
//     for (int dy = -searchRadius; dy <= searchRadius; dy++)
//     {
//         for (int dx = -searchRadius; dx <= searchRadius; dx++)
//         {
//             if (dx == 0 && dy == 0) continue; // 中心点は既にチェック済み
//             
//             var nearbyPoint = new Point(mapX + dx, mapY + dy);
//             if (_pixelProvinceMapping.TryGetValue(nearbyPoint, out int nearbyId))
//             {
//                 var province = _provinceData.Values.FirstOrDefault(p => p.Id == nearbyId);
//                 if (province != null)
//                 {
//                     // 見つかった座標を今後のためにキャッシュ
//                     _pixelProvinceMapping[new Point(mapX, mapY)] = nearbyId;
//                     return province;
//                 }
//             }
//         }
//     }
//     
//     // 5. 色の近似値を検索（許容範囲内の色）
//     const int colorTolerance = 3;
//     ProvinceInfo bestMatch = null;
//     int minColorDistance = int.MaxValue;
//     
//     foreach (var entry in _colorLookupMap)
//     {
//         var storedColor = entry.Key;
//         var province = entry.Value;
//         
//         // RGB色空間でのユークリッド距離
//         int dr = storedColor.R - pixelColor.R;
//         int dg = storedColor.G - pixelColor.G;
//         int db = storedColor.B - pixelColor.B;
//         int distance = dr*dr + dg*dg + db*db; // 平方根なしで十分
//         
//         if (distance < minColorDistance)
//         {
//             minColorDistance = distance;
//             bestMatch = province;
//         }
//     }
//     
//     // 許容範囲内で最も近い色のプロヴィンスを返す
//     if (minColorDistance <= colorTolerance*colorTolerance*3 && bestMatch != null)
//     {
//         // 今後のためにキャッシュに追加
//         _pixelProvinceMapping[new Point(mapX, mapY)] = bestMatch.Id;
//         return bestMatch;
//     }
//     
//     return null;
// }
//
// // マウスホバー時のプロヴィンスツールチップ表示
// private void OnMapPointerMoved(object sender, PointerEventArgs e)
// {
//     if (_mapImage == null) return;
//
//     var position = e.GetPosition(_mapImageControl);
//
//     // カーソル位置のプロヴィンス情報を取得
//     var province = GetProvinceAtPosition(position.X, position.Y);
//
//     if (province != null)
//     {
//         // ツールチップの内容を作成
//         _tooltipTextBlock.Text = province.ToString();
//
//         // ツールチップが表示され、適切に設定されていることを確認
//         _provinceTooltip.Content = _tooltipTextBlock;
//         _provinceTooltip.IsVisible = true;
//
//         // マップ画像コントロールにツールチップを設定
//         ToolTip.SetTip(_mapImageControl, _tooltipTextBlock);
//         ToolTip.SetIsOpen(_mapImageControl, true);
//     }
//     else
//     {
//         // 不明なエリアの座標を表示
//         int mapX = (int)(position.X / _zoomFactor);
//         int mapY = (int)(position.Y / _zoomFactor);
//
//         _tooltipTextBlock.Text = $"不明なプロヴィンス\n座標: X:{mapX} Y:{mapY}";
//         ToolTip.SetTip(_mapImageControl, _tooltipTextBlock);
//         ToolTip.SetIsOpen(_mapImageControl, true);
//     }
// }
//
// // マップ上でのクリック時
// private void OnMapPointerPressed(object sender, PointerPressedEventArgs e)
// {
//     if (_mapImage == null) return;
//     
//     var position = e.GetPosition(_mapImageControl);
//     
//     // プロヴィンス情報を取得
//     var province = GetProvinceAtPosition(position.X, position.Y);
//     
//     if (province != null)
//     {
//         _selectedProvince = province;
//         _infoTextBlock.Text = province.ToString();
//     }
//     else
//     {
//         _selectedProvince = null;
//         
//         // 実際の座標を情報パネルに表示
//         int mapX = (int)(position.X / _zoomFactor);
//         int mapY = (int)(position.Y / _zoomFactor);
//         
//         _infoTextBlock.Text = $"不明なプロヴィンス\n座標: X:{mapX} Y:{mapY}";
//     }
// }
//
//         public async Task LoadNavalBaseMarkers(string modPath, string vanillaPath)
//         {
//             try
//             {
//                 _infoTextBlock.Text = "港湾施設データを読み込み中...";
//         
//                 // 既存のマーカーをクリア
//                 ClearNavalBaseMarkers();
//         
//                 // StateDataLoaderの初期化
//                 _stateDataLoader = new StateDataLoader(modPath, vanillaPath);
//         
//                 // 建物の位置情報を読み込み
//                 await _stateDataLoader.LoadBuildingPositions();
//         
//                 // Naval Baseデータを読み込み
//                 var navalBases = await _stateDataLoader.LoadNavalBases();
//         
//                 int validMarkers = 0;
//                 foreach (var navalBase in navalBases)
//                 {
//                     // プロヴィンスIDが有効で、Stateファイルとbuildings.txtから
//                     // 得られた位置情報が設定されている場合だけ追加
//                     if (navalBase.ProvinceId > 0 && navalBase.HasCustomPosition)
//                     {
//                         // マーカーを追加
//                         AddNavalBaseMarker(navalBase);
//                         validMarkers++;
//                     }
//                 }
//         
//                 _infoTextBlock.Text = $"{validMarkers} 港湾施設を読み込みました";
//             }
//             catch (Exception ex)
//             {
//                 _infoTextBlock.Text = $"港湾施設読み込みエラー: {ex.Message}";
//             }
//         }
//
//         // GetProvinceCenter メソッドは削除
//
//         // 港湾施設マーカーの追加
//         private void AddNavalBaseMarker(NavalBase navalBase)
//         {
//             try
//             {
//                 // すでに同じプロヴィンスにマーカーがある場合は更新
//                 if (_navalBaseMarkers.ContainsKey(navalBase.ProvinceId))
//                 {
//                     UpdateNavalBaseMarker(navalBase);
//                     return;
//                 }
//         
//                 // マーカースタイルを取得
//                 var (background, border) = navalBase.GetMarkerStyle();
//         
//                 // マーカー要素を作成
//                 var markerBorder = new Border
//                 {
//                     Width = 16,
//                     Height = 16,
//                     CornerRadius = new CornerRadius(8),
//                     Background = background,
//                     BorderBrush = border,
//                     BorderThickness = new Thickness(2),
//                     // ツールチップを設定
//                     // ToolTip = new ToolTip { Content = navalBase.ToString() }
//                 };
//         
//                 // マーカーをCanvasに追加
//                 Canvas.SetLeft(markerBorder, navalBase.Position.X * _zoomFactor - 8);
//                 Canvas.SetTop(markerBorder, navalBase.Position.Y * _zoomFactor - 8);
//                 _markersCanvas.Children.Add(markerBorder);
//         
//                 // マーカー要素を保存
//                 navalBase.MarkerElement = markerBorder;
//                 _navalBaseMarkers[navalBase.ProvinceId] = navalBase;
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"港湾施設マーカー追加エラー: {ex.Message}");
//             }
//         }
//
//         // 港湾施設マーカーの更新
//         private void UpdateNavalBaseMarker(NavalBase navalBase)
//         {
//             if (!_navalBaseMarkers.TryGetValue(navalBase.ProvinceId, out var existingMarker)) return;
//     
//             try
//             {
//                 // マーカースタイルを更新
//                 var (background, border) = navalBase.GetMarkerStyle();
//                 existingMarker.MarkerElement.Background = background;
//                 existingMarker.MarkerElement.BorderBrush = border;
//         
//                 // ツールチップを更新
//                 // existingMarker.MarkerElement.ToolTip = new ToolTip { Content = navalBase.ToString() };
//         
//                 // データを更新
//                 existingMarker.Level = navalBase.Level;
//                 existingMarker.OwnerTag = navalBase.OwnerTag;
//                 existingMarker.StateName = navalBase.StateName;
//                 existingMarker.StateId = navalBase.StateId;
//         
//                 // 位置を更新（カスタム位置がある場合のみ）
//                 if (navalBase.HasCustomPosition)
//                 {
//                     existingMarker.SetScreenPosition(navalBase.Position);
//                     Canvas.SetLeft(existingMarker.MarkerElement, navalBase.Position.X * _zoomFactor - 8);
//                     Canvas.SetTop(existingMarker.MarkerElement, navalBase.Position.Y * _zoomFactor - 8);
//                 }
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"港湾施設マーカー更新エラー: {ex.Message}");
//             }
//         }
//
//         // 港湾施設マーカーの削除
//         public void RemoveNavalBaseMarker(int provinceId)
//         {
//             if (!_navalBaseMarkers.TryGetValue(provinceId, out var marker)) return;
//     
//             _markersCanvas.Children.Remove(marker.MarkerElement);
//             _navalBaseMarkers.Remove(provinceId);
//         }
//
//         // 全ての港湾施設マーカーをクリア
//         public void ClearNavalBaseMarkers()
//         {
//             foreach (var marker in _navalBaseMarkers.Values)
//             {
//                 _markersCanvas.Children.Remove(marker.MarkerElement);
//             }
//             _navalBaseMarkers.Clear();
//         }
//
//         // ズーム時に港湾施設マーカー位置を更新
//         private void UpdateNavalBaseMarkerPositions()
//         {
//             foreach (var marker in _navalBaseMarkers.Values)
//             {
//                 Canvas.SetLeft(marker.MarkerElement, marker.Position.X * _zoomFactor - 8);
//                 Canvas.SetTop(marker.MarkerElement, marker.Position.Y * _zoomFactor - 8);
//             }
//         }
//         // マップモード変更時
//         private void OnMapModeChanged(object sender, SelectionChangedEventArgs e)
//         {
//             if (_mapModeComboBox.SelectedIndex == 0)
//             {
//                 _currentMapMode = MapMode.Provinces;
//             }
//             else
//             {
//                 _currentMapMode = MapMode.States;
//             }
//             
//             // マップの再読み込み
//             UpdateMapImage();
//         }
//         
//         // マップイメージ更新 - unsafe コードを使わない実装に変更
//         // 修正点1: ズームを適切に処理するためにUpdateMapImageメソッドを修正
//         private void UpdateMapImage()
//         {
//             if (_mapImage == null) return;
//
//             int width = (int)(_mapImage.PixelSize.Width * _zoomFactor);
//             int height = (int)(_mapImage.PixelSize.Height * _zoomFactor);
//
//             if (width <= 0 || height <= 0) return;
//
//             // 更新前に現在のスクロール位置を保存
//             double horizontalOffset = _mapScrollViewer.Offset.X;
//             double verticalOffset = _mapScrollViewer.Offset.Y;
//     
//             // 港湾施設マーカーの位置を更新
//             UpdateNavalBaseMarkerPositions();
//     
//             try
//             {
//                 // ビットマップをリサイズする代わりにScaleTransformを使用してズームを適用
//                 var scaleTransform = new ScaleTransform(_zoomFactor, _zoomFactor);
//                 _mapImageControl.RenderTransform = scaleTransform;
//         
//                 // リサイズせずに元の画像をソースとして設定
//                 _mapImageControl.Source = _mapImage;
//         
//                 // ズームされた寸法に合わせて画像コンテナのサイズを更新
//                 _mapImageControl.Width = width;
//                 _mapImageControl.Height = height;
//         
//                 // マーカーキャンバスのサイズも更新
//                 _markersCanvas.Width = width;
//                 _markersCanvas.Height = height;
//         
//                 // ズーム係数に合わせてスクロール位置を復元
//                 // これによりズーム時に表示の中心を維持
//                 Dispatcher.UIThread.Post(() =>
//                 {
//                     // 新しいスクロール位置を計算
//                     double newX = horizontalOffset;
//                     double newY = verticalOffset;
//             
//                     // ズームイン時、中心点を維持するよう試みる
//                     _mapScrollViewer.Offset = new Vector(newX, newY);
//                 }, DispatcherPriority.Render);
//             }
//             catch (Exception ex)
//             {
//                 _infoTextBlock.Text = $"マップ更新エラー: {ex.Message}";
//             }
//         }
//
//
//         
//         // マウスホイールでのズーム
//         private void OnMapPointerWheelChanged(object sender, PointerWheelEventArgs e)
//         {
//             if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
//             {
//                 if (e.Delta.Y > 0)
//                 {
//                     ZoomIn();
//                 }
//                 else if (e.Delta.Y < 0)
//                 {
//                     ZoomOut();
//                 }
//                 
//                 e.Handled = true;
//             }
//         }
//         
//         // ズームイン
//         private void ZoomIn()
//         {
//             if (_zoomFactor < MAX_ZOOM)
//             {
//                 _zoomFactor += ZOOM_STEP;
//                 UpdateMapImage();
//             }
//         }
//         
//         // ズームアウト
//         private void ZoomOut()
//         {
//             if (_zoomFactor > MIN_ZOOM)
//             {
//                 _zoomFactor -= ZOOM_STEP;
//                 UpdateMapImage();
//             }
//         }
//         
//         // ズームリセット
//         private void ZoomReset()
//         {
//             _zoomFactor = 1.0;
//             UpdateMapImage();
//         }
//         
//         // ズームボタンイベントハンドラ
//         private void OnZoomInClick(object sender, RoutedEventArgs e)
//         {
//             ZoomIn();
//         }
//         
//         private void OnZoomOutClick(object sender, RoutedEventArgs e)
//         {
//             ZoomOut();
//         }
//         
//         private void OnZoomResetClick(object sender, RoutedEventArgs e)
//         {
//             ZoomReset();
//         }
//         
//         // プロヴィンスIDに基づいてマップの表示位置を変更するメソッド
//         public void FocusOnProvince(int provinceId)
//         {
//             // プロヴィンスIDから色を検索
//             var province = _provinceData.Values.FirstOrDefault(p => p.Id == provinceId);
//             if (province == null) return;
//             
//             // そのプロヴィンスの位置を特定（実際の実装ではプロヴィンスの中心座標を事前に計算して格納する必要あり）
//             // この例では簡略化のためダミー実装
//             int centerX = _mapImage.PixelSize.Width / 2;  // 仮の中心X座標
//             int centerY = _mapImage.PixelSize.Height / 2; // 仮の中心Y座標
//             
//             // スクロールビューの中心に表示
//             double viewportWidth = _mapScrollViewer.Viewport.Width;
//             double viewportHeight = _mapScrollViewer.Viewport.Height;
//             
//             double offsetX = (centerX * _zoomFactor) - (viewportWidth / 2);
//             double offsetY = (centerY * _zoomFactor) - (viewportHeight / 2);
//             
//             // 負の値にならないよう調整
//             offsetX = Math.Max(0, offsetX);
//             offsetY = Math.Max(0, offsetY);
//             
//             // スクロール位置を設定
//             _mapScrollViewer.Offset = new Vector(offsetX, offsetY);
//             
//             // 選択状態を更新
//             _selectedProvince = province;
//             _infoTextBlock.Text = province.ToString();
//         }
//     }
// }

