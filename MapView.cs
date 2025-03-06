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

namespace HOI4NavalModder
{
    public class MapViewer : UserControl
    {
        // マップ画像
        private Bitmap _mapImage;
        private WriteableBitmap _zoomableMap;
        private Image _mapImageControl;
        private ScrollViewer _mapScrollViewer;
        private ToolTip _provinceTooltip;
        private TextBlock _tooltipTextBlock;
        
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
            
            // マップスクロールビュー
            _mapScrollViewer = new ScrollViewer
            {
                HorizontalScrollBarVisibility = Avalonia.Controls.ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = Avalonia.Controls.ScrollBarVisibility.Auto,
                Padding = new Thickness(0),
                Background = Brushes.Black
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
            
            _provinceTooltip = new ToolTip
            {
                Content = _tooltipTextBlock,
                Placement = Avalonia.Controls.PlacementMode.Pointer
            };
            
            ToolTip.SetTip(_mapImageControl, _provinceTooltip);
            _provinceTooltip.IsOpen = false;
            
            // マウスイベントの設定
            _mapImageControl.PointerMoved += OnMapPointerMoved;
            _mapImageControl.PointerPressed += OnMapPointerPressed;
            _mapImageControl.PointerWheelChanged += OnMapPointerWheelChanged;
            
            _mapScrollViewer.Content = _mapImageControl;
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
                }
                
                // マップ画像を読み込む
                if (File.Exists(_provincesMapPath))
                {
                    _mapImage = new Bitmap(_provincesMapPath);
                    
                    // マップ画像の初期表示
                    UpdateMapImage();
                    
                    // プロヴィンス定義を読み込む
                    await LoadProvinceDefinitions();
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
        
        // マップイメージ更新
  private void UpdateMapImage()
        {
            if (_mapImage == null) return;

            int width = (int)(_mapImage.PixelSize.Width * _zoomFactor);
            int height = (int)(_mapImage.PixelSize.Height * _zoomFactor);

            if (width <= 0 || height <= 0) return;

            double horizontalOffset = _mapScrollViewer.Offset.X;
            double verticalOffset = _mapScrollViewer.Offset.Y;

            _zoomableMap = new WriteableBitmap(new PixelSize(width, height), new Vector(96, 96), PixelFormat.Bgra8888, AlphaFormat.Premul);

            using (var frameBuffer = _zoomableMap.Lock())
            {
                unsafe
                {
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            int sourceX = (int)(x / _zoomFactor);
                            int sourceY = (int)(y / _zoomFactor);

                            if (sourceX < _mapImage.PixelSize.Width && sourceY < _mapImage.PixelSize.Height)
                            {
                                Color color = GetPixelColor(_mapImage, sourceX, sourceY);
                                SetPixelColor(_zoomableMap, frameBuffer, x, y, color);
                            }
                        }
                    }
                }
            }

            _mapImageControl.Source = _zoomableMap;

            Dispatcher.UIThread.Post(() =>
            {
                _mapScrollViewer.Offset = new Vector(horizontalOffset * _zoomFactor, verticalOffset * _zoomFactor);
            }, DispatcherPriority.Render);
        }

        private Color GetPixelColor(Bitmap bitmap, int x, int y)
        {
            using (var frameBuffer = bitmap.Lock())
            {
                unsafe
                {
                    byte* pixelData = (byte*)frameBuffer.Address;
                    int bytesPerPixel = frameBuffer.Format.BitsPerPixel / 8;
                    int stride = frameBuffer.RowBytes;

                    int offset = y * stride + x * bytesPerPixel;

                    byte b = pixelData[offset];
                    byte g = pixelData[offset + 1];
                    byte r = pixelData[offset + 2];
                    byte a = bytesPerPixel > 3 ? pixelData[offset + 3] : (byte)255;

                    return Color.FromArgb(a, r, g, b);
                }
            }
        }

        private void SetPixelColor(WriteableBitmap bitmap, ILockedFramebuffer frameBuffer, int x, int y, Color color)
        {
            unsafe
            {
                byte* pixelData = (byte*)frameBuffer.Address;
                int bytesPerPixel = frameBuffer.Format.BitsPerPixel / 8;
                int stride = frameBuffer.RowBytes;

                int offset = y * stride + x * bytesPerPixel;

                pixelData[offset] = color.B;
                pixelData[offset + 1] = color.G;
                pixelData[offset + 2] = color.R;
                if (bytesPerPixel > 3)
                {
                    pixelData[offset + 3] = color.A;
                }
            }
        }
        // マップ上でのマウス移動時
        private void OnMapPointerMoved(object sender, PointerEventArgs e)
        {
            if (_mapImage == null || _zoomableMap == null) return;
            
            var position = e.GetPosition(_mapImageControl);
            
            int x = (int)(position.X / _zoomFactor);
            int y = (int)(position.Y / _zoomFactor);
            
            if (x >= 0 && y >= 0 && x < _mapImage.PixelSize.Width && y < _mapImage.PixelSize.Height)
            {
                Color pixelColor = GetPixelColor(_mapImage, x, y);
                
                if (_provinceData.TryGetValue(pixelColor, out var province))
                {
                    _tooltipTextBlock.Text = province.ToString();
                    _provinceTooltip.IsOpen = true;
                }
                else
                {
                    _tooltipTextBlock.Text = $"不明なプロヴィンス\n色: R:{pixelColor.R} G:{pixelColor.G} B:{pixelColor.B}";
                    _provinceTooltip.IsOpen = true;
                }
            }
            else
            {
                _provinceTooltip.IsOpen = false;
            }
        }
        
        // マップ上でのクリック時
        private void OnMapPointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (_mapImage == null) return;
            
            var position = e.GetPosition(_mapImageControl);
            
            int x = (int)(position.X / _zoomFactor);
            int y = (int)(position.Y / _zoomFactor);
            
            if (x >= 0 && y >= 0 && x < _mapImage.PixelSize.Width && y < _mapImage.PixelSize.Height)
            {
                Color pixelColor = GetPixelColor(_mapImage, x, y);
                
                if (_provinceData.TryGetValue(pixelColor, out var province))
                {
                    _selectedProvince = province;
                    _infoTextBlock.Text = province.ToString();
                }
                else
                {
                    _selectedProvince = null;
                    _infoTextBlock.Text = $"不明なプロヴィンス\n色: R:{pixelColor.R} G:{pixelColor.G} B:{pixelColor.B}";
                }
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
    }
}