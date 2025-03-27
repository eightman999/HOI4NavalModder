// using System;
// using System.Collections.Generic;
// using System.IO;
// using System.Security.Cryptography;
// using System.Text;
// using System.Text.Json;
// using System.Threading.Tasks;
// using Avalonia;
// using Color = Avalonia.Media.Color;
//
// namespace HOI4NavalModder
// {
//     public class MapCacheManager
//     {
//         // キャッシュのバージョン（互換性のため）
//         private const int CACHE_VERSION = 1;
//         
//         // キャッシュディレクトリパス
//         private readonly string _cacheDirPath;
//         
//         public MapCacheManager()
//         {
//             // アプリケーションのキャッシュディレクトリを設定
//             _cacheDirPath = Path.Combine(
//                 Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
//                 "HOI4NavalModder",
//                 "cache",
//                 "maps");
//             
//             // キャッシュディレクトリが存在しない場合は作成
//             if (!Directory.Exists(_cacheDirPath))
//             {
//                 Directory.CreateDirectory(_cacheDirPath);
//             }
//         }
//         
//         // モッド名とゲームバージョンに基づいたキャッシュファイルパスを取得
//         public string GetCachePath(string modName, string gameVersion)
//         {
//             // 無効な文字を除去
//             string safeName = string.Join("_", modName.Split(Path.GetInvalidFileNameChars()));
//             
//             return Path.Combine(_cacheDirPath, $"{safeName}_{gameVersion}_map_cache.json");
//         }
//         
//         // キャッシュが有効かどうかをチェック（ソースファイルが更新されていないか）
//         public bool IsCacheValid(string cachePath, string provincesMapPath, string provincesDefinitionPath)
//         {
//             try
//             {
//                 if (!File.Exists(cachePath)) return false;
//                 
//                 // キャッシュファイルのメタデータを読み込み
//                 var metadataPath = cachePath + ".meta";
//                 if (!File.Exists(metadataPath)) return false;
//                 
//                 var metadata = JsonSerializer.Deserialize<CacheMetadata>(File.ReadAllText(metadataPath));
//                 
//                 // バージョンチェック
//                 if (metadata.Version != CACHE_VERSION) return false;
//                 
//                 // ソースファイルのハッシュを計算
//                 string provincesMapHash = CalculateFileHash(provincesMapPath);
//                 string provincesDefinitionHash = CalculateFileHash(provincesDefinitionPath);
//                 
//                 // ハッシュが一致するかどうかをチェック
//                 return provincesMapHash == metadata.ProvincesMapHash &&
//                        provincesDefinitionHash == metadata.ProvincesDefinitionHash;
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"キャッシュ検証エラー: {ex.Message}");
//                 return false;
//             }
//         }
//         
//         // キャッシュからプロヴィンスデータを読み込み
// public async Task<(Dictionary<Color, MapViewer.ProvinceInfo> ProvinceData, Dictionary<Point, int> PixelMap)> 
//     LoadProvinceDataFromCache(string cachePath)
// {
//     try
//     {
//         string json = await File.ReadAllTextAsync(cachePath);
//         
//         // ProvinceInfoリストを読み込み
//         var serializedData = JsonSerializer.Deserialize<List<SerializedProvinceInfo>>(json);
//         
//         // 色をキーとしたDictionaryに変換
//         var provinceData = new Dictionary<Color, MapViewer.ProvinceInfo>();
//         
//         foreach (var serInfo in serializedData)
//         {
//             var color = Color.FromRgb(serInfo.R, serInfo.G, serInfo.B);
//             
//             var province = new MapViewer.ProvinceInfo
//             {
//                 Id = serInfo.Id,
//                 Color = color,
//                 Type = serInfo.Type,
//                 IsCoastal = serInfo.IsCoastal,
//                 Terrain = serInfo.Terrain,
//                 Continent = serInfo.Continent,
//                 StateId = serInfo.StateId
//             };
//             
//             // 隣接プロヴィンスがあれば追加
//             if (serInfo.AdjacentProvinces != null)
//             {
//                 province.AdjacentProvinces = new List<int>(serInfo.AdjacentProvinces);
//             }
//             
//             provinceData[color] = province;
//         }
//         
//         // ピクセルマッピングをロード
//         var pixelMapPath = cachePath + ".pixels";
//         Dictionary<Point, int> pixelMap = new Dictionary<Point, int>();
//         
//         if (File.Exists(pixelMapPath))
//         {
//             string pixelJson = await File.ReadAllTextAsync(pixelMapPath);
//             var pixelData = JsonSerializer.Deserialize<List<SerializedPixelMapping>>(pixelJson);
//             
//             foreach (var pixel in pixelData)
//             {
//                 pixelMap[new Point(pixel.X, pixel.Y)] = pixel.ProvinceId;
//             }
//         }
//         
//         return (provinceData, pixelMap);
//     }
//     catch (Exception ex)
//     {
//         Console.WriteLine($"キャッシュ読み込みエラー: {ex.Message}");
//         return (null, null);
//     }
// }    
//         // プロヴィンスデータをキャッシュに保存
//         public async Task CreateCache(string cachePath, string provincesMapPath, string provincesDefinitionPath, 
//             Dictionary<Color, MapViewer.ProvinceInfo> provinceData, Dictionary<Point, int> pixelMap)
//         {
//             try
//             {
//                 // 既存のProvinceDataのシリアライズ処理
//         
//                 // ピクセルマッピングのシリアライズ
//                 var pixelData = new List<SerializedPixelMapping>();
//         
//                 foreach (var entry in pixelMap)
//                 {
//                     var point = entry.Key;
//                     var provinceId = entry.Value;
//             
//                     pixelData.Add(new SerializedPixelMapping
//                     {
//                         X = (int)point.X,
//                         Y = (int)point.Y,
//                         ProvinceId = provinceId
//                     });
//                 }
//         
//                 var options = new JsonSerializerOptions
//                 {
//                     WriteIndented = false // サイズ削減のため改行なし
//                 };
//         
//                 var pixelJson = JsonSerializer.Serialize(pixelData, options);
//                 await File.WriteAllTextAsync(cachePath + ".pixels", pixelJson);
//         
//                 // 既存のメタデータの保存処理
//             }
//             catch (Exception ex)
//             {
//                 Console.WriteLine($"ピクセルマップキャッシュ作成エラー: {ex.Message}");
//             }
//         }
//         // ファイルのハッシュ値を計算（ファイル変更の検出用）
//         private string CalculateFileHash(string filePath)
//         {
//             using (var md5 = MD5.Create())
//             using (var stream = File.OpenRead(filePath))
//             {
//                 byte[] hash = md5.ComputeHash(stream);
//                 return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
//             }
//         }
//         
//         // キャッシュのメタデータ（検証用）
//         private class CacheMetadata
//         {
//             public int Version { get; set; }
//             public DateTime CreatedAt { get; set; }
//             public string ProvincesMapHash { get; set; }
//             public string ProvincesDefinitionHash { get; set; }
//         }
//         
//         // シリアライズ可能なプロヴィンス情報
//         private class SerializedProvinceInfo
//         {
//             public int Id { get; set; }
//             public byte R { get; set; }
//             public byte G { get; set; }
//             public byte B { get; set; }
//             public string Type { get; set; }
//             public bool IsCoastal { get; set; }
//             public string Terrain { get; set; }
//             public string Continent { get; set; }
//             public List<int> AdjacentProvinces { get; set; }
//             public int StateId { get; set; }
//         }
//         private class SerializedPixelMapping 
//         {
//             public int X { get; set; }
//             public int Y { get; set; }
//             public int ProvinceId { get; set; }
//         }
//     }
//     
// }

