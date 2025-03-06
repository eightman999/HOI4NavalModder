using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.Layout;
using Avalonia.Platform;
// 不足している名前空間を追加
using Avalonia.Controls.Primitives;
using Avalonia.Controls;
using HOI4NavalModder; // NavalBase等のクラス参照用

namespace HOI4NavalModder
{
    public class MapViewer : UserControl
    {
        private Dictionary<int, NavalBase> _navalBaseMarkers = new Dictionary<int, NavalBase>();
        private StateDataLoader _stateDataLoader;
        private MapCacheManager _cacheManager;
        private string _gameVersion = "1.12.14"; // ゲームバージョンを指定（実際の使用時は設定から取得）
        private string _modName;
        // マップ画像
        private Bitmap _mapImage;
        private WriteableBitmap _zoomableMap;
        private Image _mapImageControl;
        private ScrollViewer _mapScrollViewer;
        private Canvas _markersCanvas;
        private ToolTip _provinceTooltip;
        private TextBlock _tooltipTextBlock;
        // 座標からプロヴィンスへのマッピングを追加
        private Dictionary<Point, ProvinceInfo> _coordinateProvinceMapping = new Dictionary<Point, ProvinceInfo>();
        private bool _isProvinceMapInitialized = false;
        // 情報パネル
        private StackPanel _infoPanel;
        private TextBlock _infoTextBlock;
        
        // プロヴィンスデータ
        private Dictionary<Color, ProvinceInfo> _provinceData = new Dictionary<Color, ProvinceInfo>();
        
        // 現在選択中のプロヴィンス
        private ProvinceInfo _selectedProvince;
        
        // マップ表示モード
        public enum MapMode
        {
            Provinces,
            States
        }
        
        private MapMode _currentMapMode = MapMode.Provinces;
        private ComboBox _mapModeComboBox;
        
        // ズーム関連
        private double _zoomFactor = 1.0;
        private const double MIN_ZOOM = 0.5;
        private const double MAX_ZOOM = 4.0;
        private const double ZOOM_STEP = 0.1;
        
        // パス
        private string _provincesMapPath;
        private string _provincesDefinitionPath;
        private string _statesMapPath;
        private string _statesDefinitionPath;
        
        // モデルクラス
        public class ProvinceInfo
        {
            public int Id { get; set; }
            public Color Color { get; set; }
            public string Type { get; set; } // sea/lake/land
            public bool IsCoastal { get; set; }
            public string Terrain { get; set; }
            public string Continent { get; set; }
            public List<int> AdjacentProvinces { get; set; } = new List<int>();
            public int StateId { get; set; } = -1;
    
            // デフォルトコンストラクタ（JSONシリアライザ用）
            public ProvinceInfo() { }
    
            // クローン作成用コンストラクタ
            public ProvinceInfo(ProvinceInfo source)
            {
                Id = source.Id;
                Color = source.Color;
                Type = source.Type;
                IsCoastal = source.IsCoastal;
                Terrain = source.Terrain;
                Continent = source.Continent;
                AdjacentProvinces = new List<int>(source.AdjacentProvinces);
                StateId = source.StateId;
            }
    
            public override string ToString()
            {
                return $"プロヴィンスID: {Id}\n" +
                       $"色: R:{Color.R} G:{Color.G} B:{Color.B}\n" +
                       $"種類: {Type}\n" +
                       $"沿岸部: {(IsCoastal ? "はい" : "いいえ")}\n" +
                       $"地形: {Terrain}\n" +
                       $"大陸: {Continent}\n" +
                       $"ステートID: {(StateId >= 0 ? StateId.ToString() : "なし")}";
            }
        }
        
        public MapViewer()
        {
            InitializeComponent();
        }
        
        private void InitializeComponent()
        {
            // メインレイアウト
            var mainGrid = new Grid
            {
                RowDefinitions = new RowDefinitions("Auto,*")
            };
            
            // 上部コントロールパネル
            var controlPanel = new StackPanel
            {
                Orientation = Avalonia.Layout.Orientation.Horizontal,
                Margin = new Thickness(10),
                Spacing = 10
            };
            
            // マップモード選択
            var modeLabel = new TextBlock
            {
                Text = "マップモード:",
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            
            _mapModeComboBox = new ComboBox
            {
                Width = 150,
                SelectedIndex = 0
            };
            _mapModeComboBox.Items.Add("プロヴィンス");
            _mapModeComboBox.Items.Add("ステート");
            _mapModeComboBox.SelectionChanged += OnMapModeChanged;
            
            // ズームコントロール
            var zoomOutButton = new Button
            {
                Content = "-",
                Width = 30,
                Height = 30,
                Padding = new Thickness(0),
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            zoomOutButton.Click += OnZoomOutClick;
            
            var zoomInButton = new Button
            {
                Content = "+",
                Width = 30,
                Height = 30,
                Padding = new Thickness(0),
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            zoomInButton.Click += OnZoomInClick;
            
            var zoomResetButton = new Button
            {
                Content = "100%",
                Padding = new Thickness(8, 0, 8, 0),
                Height = 30,
                VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center,
                HorizontalContentAlignment = Avalonia.Layout.HorizontalAlignment.Center
            };
            zoomResetButton.Click += OnZoomResetClick;
            
            controlPanel.Children.Add(modeLabel);
            controlPanel.Children.Add(_mapModeComboBox);
            controlPanel.Children.Add(zoomOutButton);
            controlPanel.Children.Add(zoomInButton);
            controlPanel.Children.Add(zoomResetButton);
            
            Grid.SetRow(controlPanel, 0);
            
            // マップ表示コンテナ
            var mapContainer = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("*,300")
            };
            
            // マップスクロールビュー - ScrollBarVisibilityの修正
            _mapScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Padding = new Thickness(0),
                Background = Brushes.Black
            };
            
            // マーカー用のキャンバスを作成
            _markersCanvas = new Canvas
            {
                IsHitTestVisible = false // マウスイベントを下層に通過させる
            };
            
            // マップ画像コントロール
            _mapImageControl = new Image
            {
                Stretch = Stretch.None,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Top
            };
            
            // ツールチップの設定
            _tooltipTextBlock = new TextBlock
            {
                MaxWidth = 300,
                TextWrapping = TextWrapping.Wrap,
                Padding = new Thickness(5)
            };
            
            // ToolTipインスタンスの作成
            _provinceTooltip = new ToolTip
            {
                Content = _tooltipTextBlock
            };
            
            // Panel を使用して Image と Canvas を重ねる
            var mapPanel = new Panel();
            mapPanel.Children.Add(_mapImageControl);
            mapPanel.Children.Add(_markersCanvas);
            
            _mapScrollViewer.Content = mapPanel;
            
            // ツールチップを設定
            // Avaloniaの適切なToolTipヘルパーメソッドを使用
            ToolTip.SetTip(_mapImageControl, _provinceTooltip.Content);
            _provinceTooltip.IsVisible = false;
            
            // マウスイベントの設定
            _mapImageControl.PointerMoved += OnMapPointerMoved;
            _mapImageControl.PointerPressed += OnMapPointerPressed;
            _mapImageControl.PointerWheelChanged += OnMapPointerWheelChanged;
            
            Grid.SetColumn(_mapScrollViewer, 0);
            
            // 情報パネル
            var infoPanelBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#3F3F46")),
                BorderThickness = new Thickness(1, 0, 0, 0),
                Padding = new Thickness(10)
            };
            
            _infoPanel = new StackPanel
            {
                Spacing = 10
            };
            
            var infoPanelHeader = new TextBlock
            {
                Text = "プロヴィンス情報",
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 10)
            };
            
            _infoTextBlock = new TextBlock
            {
                Text = "プロヴィンスを選択してください",
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap
            };
            
            _infoPanel.Children.Add(infoPanelHeader);
            _infoPanel.Children.Add(_infoTextBlock);
            
            infoPanelBorder.Child = _infoPanel;
            Grid.SetColumn(infoPanelBorder, 1);
            
            mapContainer.Children.Add(_mapScrollViewer);
            mapContainer.Children.Add(infoPanelBorder);
            
            Grid.SetRow(mapContainer, 1);
            
            mainGrid.Children.Add(controlPanel);
            mainGrid.Children.Add(mapContainer);
            
            Content = mainGrid;
        }
        
        // 初期化処理
        public async Task Initialize(string vanillaPath, string modPath)
        {
            try
            {
                // キャッシュマネージャーの初期化
                _cacheManager = new MapCacheManager();
        
                // MOD名を取得
                _modName = Path.GetFileName(modPath);
        
                // マップファイルのパスを設定
                _provincesMapPath = Path.Combine(modPath, "map", "provinces.bmp");
                _provincesDefinitionPath = Path.Combine(modPath, "map", "definition.csv");
        
                // MODにマップがない場合はバニラから読み込む
                bool isModMap = true;
        
                // MODのdescriptor.modを確認（replace_path設定の確認）
                string descriptorPath = Path.Combine(modPath, "descriptor.mod");
                if (File.Exists(descriptorPath))
                {
                    string[] descriptorLines = await File.ReadAllLinesAsync(descriptorPath);
                    isModMap = descriptorLines.Any(line => 
                        line.Contains("replace_path=\"map\""));
                }
        
                // MODにマップがなければバニラのマップを使用
                if (!isModMap || !File.Exists(_provincesMapPath))
                {
                    _provincesMapPath = Path.Combine(vanillaPath, "map", "provinces.bmp");
                    _provincesDefinitionPath = Path.Combine(vanillaPath, "map", "definition.csv");
                    _modName = "vanilla";
                }
        
                // マップのキャッシュパスを取得
                string cachePath = _cacheManager.GetCachePath(_modName, _gameVersion);
        
                // キャッシュが有効かチェック
                bool isCacheValid = _cacheManager.IsCacheValid(cachePath, _provincesMapPath, _provincesDefinitionPath);
        
                // マップ画像を読み込む
                if (File.Exists(_provincesMapPath))
                {
                    _infoTextBlock.Text = "マップデータを読み込み中...";
            
                    _mapImage = new Bitmap(_provincesMapPath);
            
                    // マップ画像の初期表示
                    UpdateMapImage();
            
                    // キャッシュが有効ならキャッシュから読み込み、無効なら新たに読み込む
                    if (isCacheValid)
                    {
                        _infoTextBlock.Text = "キャッシュからプロヴィンスデータを読み込み中...";
                
                        var cachedProvinceData = await _cacheManager.LoadProvinceDataFromCache(cachePath);
                        if (cachedProvinceData != null && cachedProvinceData.Count > 0)
                        {
                            _provinceData = cachedProvinceData;
                            _infoTextBlock.Text = $"キャッシュから {_provinceData.Count} プロヴィンスを読み込みました";
                            return;
                        }
                    }
            
                    // キャッシュが無効または存在しない場合は通常の読み込み
                    _infoTextBlock.Text = "プロヴィンスデータを読み込み中...";
                    await LoadProvinceDefinitions();
            
                    // 読み込みが成功したらキャッシュを作成
                    if (_provinceData.Count > 0)
                    {
                        _infoTextBlock.Text = "キャッシュを作成中...";
                        await _cacheManager.CreateCache(cachePath, _provincesMapPath, _provincesDefinitionPath, _provinceData);
                        _infoTextBlock.Text = $"{_provinceData.Count} プロヴィンスを読み込み、キャッシュしました";
                    }
                }
                else
                {
                    _infoTextBlock.Text = "マップファイルが見つかりません";
                }
            }
            catch (Exception ex)
            {
                _infoTextBlock.Text = $"マップ初期化エラー: {ex.Message}";
            }
        }
        // プロヴィンス定義ファイル読み込み
        private async Task LoadProvinceDefinitions()
        {
            try
            {
                if (File.Exists(_provincesDefinitionPath))
                {
                    string[] lines = await File.ReadAllLinesAsync(_provincesDefinitionPath);
                    
                    _provinceData.Clear();
                    
                    foreach (var line in lines.Skip(1)) // ヘッダー行をスキップ
                    {
                        string[] parts = line.Split(';');
                        
                        if (parts.Length >= 7)
                        {
                            int id = int.Parse(parts[0]);
                            int r = int.Parse(parts[1]);
                            int g = int.Parse(parts[2]);
                            int b = int.Parse(parts[3]);
                            
                            var color = Color.FromRgb((byte)r, (byte)g, (byte)b);
                            
                            var province = new ProvinceInfo
                            {
                                Id = id,
                                Color = color,
                                Type = parts[4], // sea/lake/land
                                IsCoastal = parts[5] == "1", // 1 = coastal
                                Terrain = parts.Length > 6 ? parts[6] : "不明",
                                Continent = parts.Length > 7 ? parts[7] : "不明"
                            };
                            
                            _provinceData[color] = province;
                        }
                    }
                    
                    _infoTextBlock.Text = $"{_provinceData.Count} プロヴィンスを読み込みました";
                    
                    // マップ読み込み後に座標→プロヴィンスのマッピングを初期化
                    await InitializeProvinceCoordinateMapping();
                }
                else
                {
                    _infoTextBlock.Text = "プロヴィンス定義ファイルが見つかりません";
                }
            }
            catch (Exception ex)
            {
                _infoTextBlock.Text = $"プロヴィンス定義読み込みエラー: {ex.Message}";
            }
        }

        // 座標→プロヴィンスのマッピングを初期化する新しいメソッド
        private async Task InitializeProvinceCoordinateMapping()
        {
            // 非同期処理として実行 (UIをブロックしないため)
            await Task.Run(() => {
                try
                {
                    _infoTextBlock.Text = "プロヴィンスマップを解析中...";
                    _coordinateProvinceMapping.Clear();
                    
                    // マップ画像の全ピクセルをスキャン
                    int width = _mapImage.PixelSize.Width;
                    int height = _mapImage.PixelSize.Height;
                    
                    // 処理時間短縮のため間引いてサンプリング (10ピクセルごとに1ピクセル)
                    // 実際の実装ではパフォーマンスに応じて調整
                    const int samplingRate = 10;
                    
                    for (int y = 0; y < height; y += samplingRate)
                    {
                        for (int x = 0; x < width; x += samplingRate)
                        {
                            Color pixelColor = GetPixelColorDirect(_mapImage, x, y);
                            
                            if (_provinceData.TryGetValue(pixelColor, out var province))
                            {
                                _coordinateProvinceMapping[new Point(x, y)] = province;
                            }
                        }
                    }
                    
                    _isProvinceMapInitialized = true;
                    
                    Dispatcher.UIThread.Post(() => {
                        _infoTextBlock.Text = $"{_coordinateProvinceMapping.Count} 座標ポイントをマッピングしました";
                    });
                }
                catch (Exception ex)
                {
                    Dispatcher.UIThread.Post(() => {
                        _infoTextBlock.Text = $"プロヴィンスマッピングエラー: {ex.Message}";
                    });
                }
            });
        }
        
        // 直接ビットマップからピクセル色を取得する内部メソッド (マッピング初期化用)
        private Color GetPixelColorDirect(Bitmap bitmap, int x, int y)
        {
            // 元のGetPixelColorメソッドの実装をここで使用
            // 範囲チェック
            if (x < 0 || y < 0 || x >= bitmap.PixelSize.Width || y >= bitmap.PixelSize.Height)
            {
                return Colors.Black; // 範囲外の場合は黒を返す
            }

            try
            {
                // 24ビットRGBの場合は3バイト/ピクセル
                byte[] pixelData = new byte[4]; // 4バイト確保（4バイト境界にアラインするため）
                
                // 1x1サイズの一時的な書き込み可能ビットマップを作成
                using (var tempBitmap = new WriteableBitmap(
                    new PixelSize(1, 1),
                    new Vector(96, 96),
                    PixelFormat.Rgb32,// 24ビットRGB形式
                    AlphaFormat.Opaque)) // アルファなし
                {
                    // 書き込み可能ビットマップをロック
                    using (var context = tempBitmap.Lock())
                    {
                        // 指定した座標の1x1領域を対象のビットマップからコピー
                        bitmap.CopyPixels(
                            new Avalonia.PixelRect(x, y, 1, 1),
                            context.Address,
                            context.RowBytes,
                            0);

                        // メモリからピクセルデータを安全にコピー
                        System.Runtime.InteropServices.Marshal.Copy(context.Address, pixelData, 0, 3); // 3バイトだけコピー
                    }
                }
                
                // RGB形式からColorオブジェクトを作成（アルファは255で固定）
                return Color.FromRgb(
                    pixelData[0],  // Red
                    pixelData[1],  // Green
                    pixelData[2]   // Blue
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ピクセル色取得エラー: {ex.Message}");
                return Colors.Magenta; // エラーの場合は目立つ色を返す
            }
        }

        // マップ上の座標からプロヴィンス情報を取得する新しいメソッド
        private ProvinceInfo GetProvinceAtPosition(double x, double y)
        {
            if (!_isProvinceMapInitialized || _mapImage == null)
                return null;
                
            // 実際の座標に変換
            int mapX = (int)(x / _zoomFactor);
            int mapY = (int)(y / _zoomFactor);
            
            // 範囲チェック
            if (mapX < 0 || mapY < 0 || mapX >= _mapImage.PixelSize.Width || mapY >= _mapImage.PixelSize.Height)
                return null;
                
            // 最も近い登録済み座標ポイントを探す
            const int searchRadius = 15; // 探索半径 (パフォーマンスに応じて調整)
            ProvinceInfo nearestProvince = null;
            double minDistance = double.MaxValue;
            
            foreach (var entry in _coordinateProvinceMapping)
            {
                var point = entry.Key;
                var province = entry.Value;
                
                double distance = Math.Sqrt(Math.Pow(point.X - mapX, 2) + Math.Pow(point.Y - mapY, 2));
                
                if (distance < searchRadius && distance < minDistance)
                {
                    minDistance = distance;
                    nearestProvince = province;
                }
            }
            
            // 最も近いプロヴィンスが見つかった場合、または直接座標が登録されている場合
            if (nearestProvince != null)
            {
                return nearestProvince;
            }
            
            // 見つからなかった場合、特殊ケースとして直接色を取得
            if (_mapImage != null)
            {
                // 最後の手段として直接ピクセル色を取得
                Color pixelColor = GetPixelColorDirect(_mapImage, mapX, mapY);
                
                if (_provinceData.TryGetValue(pixelColor, out var province))
                {
                    // この座標も今後のためにマッピングに追加
                    _coordinateProvinceMapping[new Point(mapX, mapY)] = province;
                    return province;
                }
            }
            
            return null;
        }

        // マップ上でのマウス移動時 - 修正版
        private void OnMapPointerMoved(object sender, PointerEventArgs e)
        {
            if (_mapImage == null) return;
            
            var position = e.GetPosition(_mapImageControl);
            
            // 新しいメソッドを使用してプロヴィンス情報を取得
            var province = GetProvinceAtPosition(position.X, position.Y);
            
            if (province != null)
            {
                _tooltipTextBlock.Text = province.ToString();
                // ToolTipを設定する
                ToolTip.SetTip(_mapImageControl, _tooltipTextBlock);
            }
            else
            {
                // 実際の座標をツールチップに表示
                int mapX = (int)(position.X / _zoomFactor);
                int mapY = (int)(position.Y / _zoomFactor);
                
                _tooltipTextBlock.Text = $"不明なプロヴィンス\n座標: X:{mapX} Y:{mapY}";
                ToolTip.SetTip(_mapImageControl, _tooltipTextBlock);
            }
        }
        
        // マップ上でのクリック時 - 修正版
        private void OnMapPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_mapImage == null) return;
            
            var position = e.GetPosition(_mapImageControl);
            
            // 新しいメソッドを使用してプロヴィンス情報を取得
            var province = GetProvinceAtPosition(position.X, position.Y);
            
            if (province != null)
            {
                _selectedProvince = province;
                _infoTextBlock.Text = province.ToString();
            }
            else
            {
                _selectedProvince = null;
                
                // 実際の座標を情報パネルに表示
                int mapX = (int)(position.X / _zoomFactor);
                int mapY = (int)(position.Y / _zoomFactor);
                
                _infoTextBlock.Text = $"不明なプロヴィンス\n座標: X:{mapX} Y:{mapY}";
            }
        }
    

        public async Task LoadNavalBaseMarkers(string modPath, string vanillaPath)
        {
            try
            {
                _infoTextBlock.Text = "港湾施設データを読み込み中...";
        
                // 既存のマーカーをクリア
                ClearNavalBaseMarkers();
        
                // StateDataLoaderの初期化
                _stateDataLoader = new StateDataLoader(modPath, vanillaPath);
        
                // 建物の位置情報を読み込み
                await _stateDataLoader.LoadBuildingPositions();
        
                // Naval Baseデータを読み込み
                var navalBases = await _stateDataLoader.LoadNavalBases();
        
                int validMarkers = 0;
                foreach (var navalBase in navalBases)
                {
                    // プロヴィンスIDが有効で、Stateファイルとbuildings.txtから
                    // 得られた位置情報が設定されている場合だけ追加
                    if (navalBase.ProvinceId > 0 && navalBase.HasCustomPosition)
                    {
                        // マーカーを追加
                        AddNavalBaseMarker(navalBase);
                        validMarkers++;
                    }
                }
        
                _infoTextBlock.Text = $"{validMarkers} 港湾施設を読み込みました";
            }
            catch (Exception ex)
            {
                _infoTextBlock.Text = $"港湾施設読み込みエラー: {ex.Message}";
            }
        }

        // GetProvinceCenter メソッドは削除

        // 港湾施設マーカーの追加
        private void AddNavalBaseMarker(NavalBase navalBase)
        {
            try
            {
                // すでに同じプロヴィンスにマーカーがある場合は更新
                if (_navalBaseMarkers.ContainsKey(navalBase.ProvinceId))
                {
                    UpdateNavalBaseMarker(navalBase);
                    return;
                }
        
                // マーカースタイルを取得
                var (background, border) = navalBase.GetMarkerStyle();
        
                // マーカー要素を作成
                var markerBorder = new Border
                {
                    Width = 16,
                    Height = 16,
                    CornerRadius = new CornerRadius(8),
                    Background = background,
                    BorderBrush = border,
                    BorderThickness = new Thickness(2),
                    // ツールチップを設定
                    // ToolTip = new ToolTip { Content = navalBase.ToString() }
                };
        
                // マーカーをCanvasに追加
                Canvas.SetLeft(markerBorder, navalBase.Position.X * _zoomFactor - 8);
                Canvas.SetTop(markerBorder, navalBase.Position.Y * _zoomFactor - 8);
                _markersCanvas.Children.Add(markerBorder);
        
                // マーカー要素を保存
                navalBase.MarkerElement = markerBorder;
                _navalBaseMarkers[navalBase.ProvinceId] = navalBase;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"港湾施設マーカー追加エラー: {ex.Message}");
            }
        }

        // 港湾施設マーカーの更新
        private void UpdateNavalBaseMarker(NavalBase navalBase)
        {
            if (!_navalBaseMarkers.TryGetValue(navalBase.ProvinceId, out var existingMarker)) return;
    
            try
            {
                // マーカースタイルを更新
                var (background, border) = navalBase.GetMarkerStyle();
                existingMarker.MarkerElement.Background = background;
                existingMarker.MarkerElement.BorderBrush = border;
        
                // ツールチップを更新
                // existingMarker.MarkerElement.ToolTip = new ToolTip { Content = navalBase.ToString() };
        
                // データを更新
                existingMarker.Level = navalBase.Level;
                existingMarker.OwnerTag = navalBase.OwnerTag;
                existingMarker.StateName = navalBase.StateName;
                existingMarker.StateId = navalBase.StateId;
        
                // 位置を更新（カスタム位置がある場合のみ）
                if (navalBase.HasCustomPosition)
                {
                    existingMarker.SetScreenPosition(navalBase.Position);
                    Canvas.SetLeft(existingMarker.MarkerElement, navalBase.Position.X * _zoomFactor - 8);
                    Canvas.SetTop(existingMarker.MarkerElement, navalBase.Position.Y * _zoomFactor - 8);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"港湾施設マーカー更新エラー: {ex.Message}");
            }
        }

        // 港湾施設マーカーの削除
        public void RemoveNavalBaseMarker(int provinceId)
        {
            if (!_navalBaseMarkers.TryGetValue(provinceId, out var marker)) return;
    
            _markersCanvas.Children.Remove(marker.MarkerElement);
            _navalBaseMarkers.Remove(provinceId);
        }

        // 全ての港湾施設マーカーをクリア
        public void ClearNavalBaseMarkers()
        {
            foreach (var marker in _navalBaseMarkers.Values)
            {
                _markersCanvas.Children.Remove(marker.MarkerElement);
            }
            _navalBaseMarkers.Clear();
        }

        // ズーム時に港湾施設マーカー位置を更新
        private void UpdateNavalBaseMarkerPositions()
        {
            foreach (var marker in _navalBaseMarkers.Values)
            {
                Canvas.SetLeft(marker.MarkerElement, marker.Position.X * _zoomFactor - 8);
                Canvas.SetTop(marker.MarkerElement, marker.Position.Y * _zoomFactor - 8);
            }
        }
        // マップモード変更時
        private void OnMapModeChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_mapModeComboBox.SelectedIndex == 0)
            {
                _currentMapMode = MapMode.Provinces;
            }
            else
            {
                _currentMapMode = MapMode.States;
            }
            
            // マップの再読み込み
            UpdateMapImage();
        }
        
        // マップイメージ更新 - unsafe コードを使わない実装に変更
        private void UpdateMapImage()
        {
            if (_mapImage == null) return;

            int width = (int)(_mapImage.PixelSize.Width * _zoomFactor);
            int height = (int)(_mapImage.PixelSize.Height * _zoomFactor);

            if (width <= 0 || height <= 0) return;

            double horizontalOffset = _mapScrollViewer.Offset.X;
            double verticalOffset = _mapScrollViewer.Offset.Y;
            
            // 港湾施設マーカー位置を更新
            UpdateNavalBaseMarkerPositions();
            
            // 画像をリサイズして表示する方法に変更
            // ズーム処理は別のアプローチを使用
            try
            {
                // 新しいビットマップを作成
                var resizedBitmap = new WriteableBitmap(
                    new PixelSize(width, height),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Premul);

                // 画像処理ライブラリを使用してリサイズするか、
                // 単純にイメージコントロールのストレッチプロパティを使用
                _mapImageControl.Source = _mapImage;
                _mapImageControl.Width = width;
                _mapImageControl.Height = height;
                
                // マーカーキャンバスのサイズも更新
                _markersCanvas.Width = width;
                _markersCanvas.Height = height;
                
                Dispatcher.UIThread.Post(() =>
                {
                    _mapScrollViewer.Offset = new Vector(horizontalOffset * _zoomFactor, verticalOffset * _zoomFactor);
                }, DispatcherPriority.Render);
            }
            catch (Exception ex)
            {
                _infoTextBlock.Text = $"マップ更新エラー: {ex.Message}";
            }
        }
        

        
        // マウスホイールでのズーム
        private void OnMapPointerWheelChanged(object sender, PointerWheelEventArgs e)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                if (e.Delta.Y > 0)
                {
                    ZoomIn();
                }
                else if (e.Delta.Y < 0)
                {
                    ZoomOut();
                }
                
                e.Handled = true;
            }
        }
        
        // ズームイン
        private void ZoomIn()
        {
            if (_zoomFactor < MAX_ZOOM)
            {
                _zoomFactor += ZOOM_STEP;
                UpdateMapImage();
            }
        }
        
        // ズームアウト
        private void ZoomOut()
        {
            if (_zoomFactor > MIN_ZOOM)
            {
                _zoomFactor -= ZOOM_STEP;
                UpdateMapImage();
            }
        }
        
        // ズームリセット
        private void ZoomReset()
        {
            _zoomFactor = 1.0;
            UpdateMapImage();
        }
        
        // ズームボタンイベントハンドラ
        private void OnZoomInClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ZoomIn();
        }
        
        private void OnZoomOutClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ZoomOut();
        }
        
        private void OnZoomResetClick(object sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            ZoomReset();
        }
        
        // プロヴィンスIDに基づいてマップの表示位置を変更するメソッド
        public void FocusOnProvince(int provinceId)
        {
            // プロヴィンスIDから色を検索
            var province = _provinceData.Values.FirstOrDefault(p => p.Id == provinceId);
            if (province == null) return;
            
            // そのプロヴィンスの位置を特定（実際の実装ではプロヴィンスの中心座標を事前に計算して格納する必要あり）
            // この例では簡略化のためダミー実装
            int centerX = _mapImage.PixelSize.Width / 2;  // 仮の中心X座標
            int centerY = _mapImage.PixelSize.Height / 2; // 仮の中心Y座標
            
            // スクロールビューの中心に表示
            double viewportWidth = _mapScrollViewer.Viewport.Width;
            double viewportHeight = _mapScrollViewer.Viewport.Height;
            
            double offsetX = (centerX * _zoomFactor) - (viewportWidth / 2);
            double offsetY = (centerY * _zoomFactor) - (viewportHeight / 2);
            
            // 負の値にならないよう調整
            offsetX = Math.Max(0, offsetX);
            offsetY = Math.Max(0, offsetY);
            
            // スクロール位置を設定
            _mapScrollViewer.Offset = new Vector(offsetX, offsetY);
            
            // 選択状態を更新
            _selectedProvince = province;
            _infoTextBlock.Text = province.ToString();
        }
    }
}