using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace HOI4NavalModder
{
    /// <summary>
    /// Bitmapからピクセルデータを効率的に読み込むためのヘルパークラス
    /// </summary>
    public unsafe class UnsafePixelAccess : IDisposable
    {
        private readonly WriteableBitmap _writeableBitmap;
        private readonly ILockedFramebuffer _lockedFramebuffer;
        private readonly int _width;
        private readonly int _height;
        private readonly int _bytesPerPixel;
        private readonly IntPtr _startAddress;
        private readonly int _stride;
        private readonly PixelFormat _pixelFormat;
        private bool _disposed = false;

        public UnsafePixelAccess(Bitmap sourceBitmap)
        {
            _width = sourceBitmap.PixelSize.Width;
            _height = sourceBitmap.PixelSize.Height;
            
            // 32bit BGRA形式を使用（Avaloniaの標準）
            _pixelFormat = PixelFormat.Bgra8888;
            _bytesPerPixel = 4; // 32bit = 4bytes
            
            // 一時的なWriteableBitmapを作成
            _writeableBitmap = new WriteableBitmap(
                new PixelSize(_width, _height),
                new Vector(96, 96),
                _pixelFormat,
                AlphaFormat.Unpremul);
            
            // フレームバッファをロック
            _lockedFramebuffer = _writeableBitmap.Lock();
            
            // 元のビットマップからピクセルデータをコピー
            sourceBitmap.CopyPixels(
                new PixelRect(0, 0, _width, _height),
                _lockedFramebuffer.Address,
                _lockedFramebuffer.RowBytes * _height,
                0);
            
            _startAddress = _lockedFramebuffer.Address;
            _stride = _lockedFramebuffer.RowBytes;
        }

        /// <summary>
        /// 指定された座標の色を取得
        /// </summary>
        public Color GetPixel(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return Colors.Transparent;
            
            byte* ptr = (byte*)_startAddress + y * _stride + x * _bytesPerPixel;
            
            // BGRA順序
            byte b = ptr[0];
            byte g = ptr[1];
            byte r = ptr[2];
            byte a = ptr[3];
            
            return Color.FromArgb(a, r, g, b);
        }

        /// <summary>
        /// 指定された座標の色を(R,G,B)タプルとして取得
        /// </summary>
        public (byte R, byte G, byte B) GetRgbTuple(int x, int y)
        {
            if (x < 0 || x >= _width || y < 0 || y >= _height)
                return (0, 0, 0);
            
            byte* ptr = (byte*)_startAddress + y * _stride + x * _bytesPerPixel;
            
            // BGRA順序
            byte b = ptr[0];
            byte g = ptr[1];
            byte r = ptr[2];
            
            return (r, g, b);
        }
        
        /// <summary>
        /// マップ全体をスキャンし、色からIDへのマッピングを使用して各ピクセルの座標→IDマッピングを構築
        /// </summary>
        public Dictionary<Point, int> BuildPixelIdMapping(
            Dictionary<(byte R, byte G, byte B), int> colorToIdMap, 
            int samplingRate = 1,
            int tolerance = 0)
        {
            var result = new Dictionary<Point, int>();
            
            for (int y = 0; y < _height; y += samplingRate)
            {
                byte* scanline = (byte*)_startAddress + y * _stride;
                
                for (int x = 0; x < _width; x += samplingRate)
                {
                    int index = x * _bytesPerPixel;
                    
                    byte b = scanline[index];
                    byte g = scanline[index + 1];
                    byte r = scanline[index + 2];
                    
                    // 完全一致を検索
                    if (colorToIdMap.TryGetValue((r, g, b), out int id))
                    {
                        result[new Point(x, y)] = id;
                        continue;
                    }
                    
                    // 許容範囲内の色を検索
                    if (tolerance > 0)
                    {
                        foreach (var entry in colorToIdMap)
                        {
                            var color = entry.Key;
                            if (Math.Abs(color.R - r) <= tolerance &&
                                Math.Abs(color.G - g) <= tolerance &&
                                Math.Abs(color.B - b) <= tolerance)
                            {
                                result[new Point(x, y)] = entry.Value;
                                break;
                            }
                        }
                    }
                }
            }
            
            return result;
        }

        /// <summary>
        /// リソースの解放
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _lockedFramebuffer.Dispose();
                _writeableBitmap.Dispose();
                _disposed = true;
            }
        }
    }
}