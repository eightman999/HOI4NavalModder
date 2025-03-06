using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace HOI4NavalModder
{
    // マップキャッシュマネージャークラス
    public class MapCacheManager
    {
        private const string CACHE_FOLDER = "MapCache";
        private const string PROVINCE_CACHE_FILE = "province_data.json";
        private const string STATE_CACHE_FILE = "state_data.json";
        private const string MAP_INFO_FILE = "map_info.json";
        
        private readonly string _cacheBasePath;
        
        public MapCacheManager(string appDataPath = null)
        {
            // アプリケーションデータパスが指定されていなければ、ユーザーのローカルAppDataフォルダを使用
            if (string.IsNullOrEmpty(appDataPath))
            {
                _cacheBasePath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "HOI4NavalModder",
                    CACHE_FOLDER);
            }
            else
            {
                _cacheBasePath = Path.Combine(appDataPath, CACHE_FOLDER);
            }
            
            // キャッシュディレクトリが存在しない場合は作成
            if (!Directory.Exists(_cacheBasePath))
            {
                Directory.CreateDirectory(_cacheBasePath);
            }
        }
        
        // マップ情報のキャッシュパスを生成（MOD名やバージョンでユニークに）
        public string GetCachePath(string modName, string gameVersion)
        {
            // 無効な文字を置換
            modName = string.IsNullOrEmpty(modName) ? "vanilla" : CleanPathString(modName);
            gameVersion = string.IsNullOrEmpty(gameVersion) ? "unknown" : CleanPathString(gameVersion);
            
            return Path.Combine(_cacheBasePath, $"{modName}_{gameVersion}");
        }
        
        // パス文字列をクリーンアップ
        private string CleanPathString(string input)
        {
            // パスに使用できない文字を置き換え
            return string.Join("_", input.Split(Path.GetInvalidFileNameChars()));
        }
        
        // キャッシュが有効かチェック
        public bool IsCacheValid(string cachePath, string provincesMapPath, string definitionPath)
        {
            // キャッシュディレクトリが存在しない場合
            if (!Directory.Exists(cachePath))
            {
                return false;
            }
            
            // マップ情報ファイルパス
            string mapInfoPath = Path.Combine(cachePath, MAP_INFO_FILE);
            
            // マップ情報ファイルが存在しない場合
            if (!File.Exists(mapInfoPath))
            {
                return false;
            }
            
            try
            {
                // マップ情報を読み込み
                string mapInfoJson = File.ReadAllText(mapInfoPath);
                var mapInfo = JsonSerializer.Deserialize<MapInfo>(mapInfoJson);
                
                // 元ファイルの最終更新日時が変わっていないかチェック
                if (mapInfo == null) return false;
                
                var provincesMapLastWrite = new FileInfo(provincesMapPath).LastWriteTime;
                var definitionLastWrite = new FileInfo(definitionPath).LastWriteTime;
                
                return provincesMapLastWrite <= mapInfo.ProvincesMapLastModified &&
                       definitionLastWrite <= mapInfo.DefinitionLastModified;
            }
            catch (Exception)
            {
                return false;
            }
        }
        
        // 新しいキャッシュを作成
        public async Task CreateCache(
            string cachePath, 
            string provincesMapPath, 
            string definitionPath,
            Dictionary<Color, MapViewer.ProvinceInfo> provinceData)
        {
            try
            {
                // キャッシュディレクトリがなければ作成
                if (!Directory.Exists(cachePath))
                {
                    Directory.CreateDirectory(cachePath);
                }
                
                // マップ情報を保存
                var mapInfo = new MapInfo
                {
                    ProvincesMapLastModified = new FileInfo(provincesMapPath).LastWriteTime,
                    DefinitionLastModified = new FileInfo(definitionPath).LastWriteTime,
                    CreatedDate = DateTime.Now,
                    ProvinceCount = provinceData.Count
                };
                
                string mapInfoJson = JsonSerializer.Serialize(mapInfo, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(cachePath, MAP_INFO_FILE), mapInfoJson);
                
                // プロヴィンスデータを保存
                var provinceCache = provinceData.Values.ToList();
                string provinceCacheJson = JsonSerializer.Serialize(provinceCache, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(Path.Combine(cachePath, PROVINCE_CACHE_FILE), provinceCacheJson);
                
                // プロヴィンスマップの縮小版を保存（オプション）
                // この実装はオプションです。マップ画像自体をキャッシュする場合に使用します。
                /*
                using (var originalMap = new Bitmap(provincesMapPath))
                {
                    // 縮小版を作成（例：1/4サイズ）
                    int thumbWidth = originalMap.PixelSize.Width / 4;
                    int thumbHeight = originalMap.PixelSize.Height / 4;
                    
                    // 縮小処理（Avaloniaで直接リサイズする方法がないため、別の方法が必要）
                    // 実際の実装ではイメージ処理ライブラリを使用することをお勧めします
                }
                */
            }
            catch (Exception ex)
            {
                Console.WriteLine($"キャッシュ作成エラー: {ex.Message}");
                throw;
            }
        }
        
        // キャッシュからプロヴィンスデータを読み込み
        public async Task<Dictionary<Color, MapViewer.ProvinceInfo>> LoadProvinceDataFromCache(string cachePath)
        {
            try
            {
                string provinceCacheFilePath = Path.Combine(cachePath, PROVINCE_CACHE_FILE);
                
                if (!File.Exists(provinceCacheFilePath))
                {
                    return null;
                }
                
                string provinceCacheJson = await File.ReadAllTextAsync(provinceCacheFilePath);
                var provinceList = JsonSerializer.Deserialize<List<MapViewer.ProvinceInfo>>(provinceCacheJson);
                
                if (provinceList == null)
                {
                    return null;
                }
                
                // Dictionary形式に変換して返す
                return provinceList.ToDictionary(p => p.Color);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"キャッシュ読み込みエラー: {ex.Message}");
                return null;
            }
        }
        
        // マップ情報クラス
        public class MapInfo
        {
            public DateTime ProvincesMapLastModified { get; set; }
            public DateTime DefinitionLastModified { get; set; }
            public DateTime CreatedDate { get; set; }
            public int ProvinceCount { get; set; }
        }
    }
}