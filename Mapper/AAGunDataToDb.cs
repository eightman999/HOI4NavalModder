using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HOI4NavalModder.Core.Models;
using HOI4NavalModder.Core.Services;

namespace HOI4NavalModder.Mapper
{
    /// <summary>
    /// AA Design View と DB の間でデータを転送する中間クラス
    /// </summary>
    public static class AAGunDataToDb
    {
        /// <summary>
        /// 対空砲の生データをデータベースに保存する
        /// </summary>
        /// <param name="equipment">AAGunCalculatorから処理された装備データ</param>
        /// <param name="rawAAData">AA_Design_Viewから渡される元のパラメータデータ</param>
        /// <returns>保存に成功したかどうか</returns>
        public static bool SaveAAGunData(NavalEquipment equipment, Dictionary<string, object> rawAAData)
        {
            try
            {
                // データベースマネージャーのインスタンスを作成
                var dbManager = new DatabaseManager();

                // 生データをデータベースに保存（この中でJSONファイルも保存される）
                var rawDataSaved = SaveRawAAGunData(equipment.Id, rawAAData);
                if (!rawDataSaved)
                {
                    Console.WriteLine($"対空砲の生データの保存に失敗しました: {equipment.Id}");
                    return false;
                }

                // NavalEquipmentをModuleDataに変換
                var moduleData = ConvertToModuleData(equipment);

                // ModuleDataをデータベースに保存
                dbManager.SaveModuleData(
                    moduleData.Info,
                    moduleData.AddStats,
                    moduleData.MultiplyStats,
                    moduleData.AddAverageStats,
                    moduleData.Resources,
                    moduleData.ConvertModules
                );

                Console.WriteLine($"対空砲データ {equipment.Id} をデータベースに保存しました");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"対空砲データの保存中にエラーが発生しました: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 対空砲の生データをJSONとして保存する
        /// </summary>
        public static bool SaveRawAAGunData(string gunId, Dictionary<string, object> rawAAData)
        {
            try
            {
                // フォルダパスを作成
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HOI4NavalModder");

                var equipmentsPath = Path.Combine(appDataPath, "equipments");

                // カテゴリに基づいたサブディレクトリを決定
                var categoryDir = "aa/light";
                if (rawAAData.ContainsKey("Category") && rawAAData["Category"].ToString() == "SMHAA")
                {
                    categoryDir = "aa/heavy";
                }

                var fullDir = Path.Combine(equipmentsPath, categoryDir);

                // フォルダが存在しない場合は作成
                if (!Directory.Exists(fullDir)) Directory.CreateDirectory(fullDir);

                // JSONファイルのパス - UUIDではなくIDベースのファイル名を使用
                var jsonFileName = $"{gunId}.json";
                var jsonFilePath = Path.Combine(fullDir, jsonFileName);

                // データをJSON文字列に変換して保存
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var jsonData = JsonSerializer.Serialize(rawAAData, options);
                File.WriteAllText(jsonFilePath, jsonData);

                Console.WriteLine($"対空砲データをJSONファイルに保存しました: {jsonFilePath}");

                // データベースにも参照情報を保存
                using (var connection = new System.Data.SQLite.SQLiteConnection($"Data Source={Path.Combine(appDataPath, "naval_module_data.db")};Version=3;"))
                {
                    connection.Open();

                    // テーブルが存在しない場合は作成
                    using (var command = new System.Data.SQLite.SQLiteCommand(connection))
                    {
                        command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS aa_guns_raw_datas (
                            ID TEXT PRIMARY KEY,
                            json_data TEXT NOT NULL
                        );";
                        command.ExecuteNonQuery();
                    }

                    using (var command = new System.Data.SQLite.SQLiteCommand(connection))
                    {
                        command.CommandText = @"
                        INSERT OR REPLACE INTO aa_guns_raw_datas (ID, json_data)
                        VALUES (@ID, @json_data);";

                        command.Parameters.AddWithValue("@ID", gunId);
                        command.Parameters.AddWithValue("@json_data", jsonFilePath);

                        command.ExecuteNonQuery();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"対空砲の生データ保存中にエラーが発生しました: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        ///     NavalEquipmentからModuleDataに変換する
        /// </summary>
        private static ModuleData ConvertToModuleData(NavalEquipment equipment)
        {
            var moduleData = new ModuleData();

            // 基本情報
            moduleData.Info = new ModuleInfo
            {
                Id = equipment.Id,
                Name = equipment.Name,
                Gfx = equipment.AdditionalProperties.ContainsKey("Gfx")
                    ? equipment.AdditionalProperties["Gfx"].ToString()
                    : $"gfx_{equipment.Category.ToLower()}_{equipment.Id.ToLower()}",
                Sfx = equipment.AdditionalProperties.ContainsKey("Sfx")
                    ? equipment.AdditionalProperties["Sfx"].ToString()
                    : "",
                Year = equipment.Year,
                Manpower = equipment.AdditionalProperties.ContainsKey("Manpower")
                    ? Convert.ToInt32(equipment.AdditionalProperties["Manpower"])
                    : 0,
                Country = equipment.Country,
                CriticalParts = equipment.SpecialAbility
            };

            // 加算ステータス - 計算された値を設定
            moduleData.AddStats = new ModuleStats();
            
            // 対空攻撃力を設定
            moduleData.AddStats.AntiAirAttack = equipment.Attack;
            
            // 軽砲攻撃力があれば設定
            if (equipment.AdditionalProperties.ContainsKey("CalculatedLgAttack"))
                moduleData.AddStats.LgAttack = Convert.ToDouble(equipment.AdditionalProperties["CalculatedLgAttack"]);
            
            // 射程
            if (equipment.AdditionalProperties.ContainsKey("CalculatedRange"))
                moduleData.AddStats.FireRange = Convert.ToDouble(equipment.AdditionalProperties["CalculatedRange"]);
            
            // 建造コスト
            if (equipment.AdditionalProperties.ContainsKey("CalculatedBuildCost"))
                moduleData.AddStats.BuildCostIc = Convert.ToDouble(equipment.AdditionalProperties["CalculatedBuildCost"]);
            
            // 対潜攻撃力
            if (equipment.AdditionalProperties.ContainsKey("CalculatedSubAttack"))
                moduleData.AddStats.SubAttack = Convert.ToDouble(equipment.AdditionalProperties["CalculatedSubAttack"]);

            // 乗算ステータスは空のオブジェクトを使用
            moduleData.MultiplyStats = new ModuleStats();
            
            // 平均加算ステータス - 装甲貫通力をここに設定
            moduleData.AddAverageStats = new ModuleStats();
            
            if (equipment.AdditionalProperties.ContainsKey("CalculatedArmorPiercing"))
                moduleData.AddAverageStats.LgArmorPiercing = Convert.ToDouble(equipment.AdditionalProperties["CalculatedArmorPiercing"]);

            // リソース
            moduleData.Resources = new ModuleResources();
            if (equipment.AdditionalProperties.ContainsKey("Steel"))
                moduleData.Resources.Steel = Convert.ToInt32(equipment.AdditionalProperties["Steel"]);
            if (equipment.AdditionalProperties.ContainsKey("Chromium"))
                moduleData.Resources.Chromium = Convert.ToInt32(equipment.AdditionalProperties["Chromium"]);
            if (equipment.AdditionalProperties.ContainsKey("Tungsten"))
                moduleData.Resources.Tungsten = Convert.ToInt32(equipment.AdditionalProperties["Tungsten"]);

            // 変換モジュール情報は空のリストを使用
            moduleData.ConvertModules = new List<ModuleConvert>();

            return moduleData;
        }

        /// <summary>
        ///     DBからAA_Design_View用の生データを取得する
        /// </summary>
        /// <param name="equipmentId">装備ID</param>
        /// <returns>対空砲パラメータの生データ</returns>
        public static Dictionary<string, object> GetRawAAGunData(string equipmentId)
        {
            try
            {
                // 装備のベースディレクトリを取得
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HOI4NavalModder");
                    
                var equipmentsPath = Path.Combine(appDataPath, "equipments");
                
                // 対空砲カテゴリのディレクトリを検索
                var aaDirs = new[] { "aa/light", "aa/heavy" };
                
                foreach (var dir in aaDirs)
                {
                    var dirPath = Path.Combine(equipmentsPath, dir);
                    
                    if (!Directory.Exists(dirPath))
                        continue;
                    
                    // 指定されたIDと一致するJSONファイルを検索
                    var jsonFilePath = Path.Combine(dirPath, $"{equipmentId}.json");
                    
                    if (File.Exists(jsonFilePath))
                    {
                        var jsonContent = File.ReadAllText(jsonFilePath);
                        
                        // JSONをDictionaryに変換
                        var options = new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        };
                        
                        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent, options);
                        
                        // ファイルパスを追加情報として保存
                        result["FilePath"] = jsonFilePath;
                        
                        return result;
                    }
                }
                
                // SQLiteデータベースから検索（JSONファイルが見つからない場合）
                using (var connection = new System.Data.SQLite.SQLiteConnection(
                    $"Data Source={Path.Combine(appDataPath, "naval_module_data.db")};Version=3;"))
                {
                    connection.Open();
                    
                    // テーブルが存在するか確認
                    using (var command = new System.Data.SQLite.SQLiteCommand(
                        "SELECT name FROM sqlite_master WHERE type='table' AND name='aa_guns_raw_datas';",
                        connection))
                    {
                        var result = command.ExecuteScalar();
                        if (result == null)
                            return null; // テーブルが存在しない
                    }
                    
                    // 対空砲データテーブルからデータを検索
                    using (var command = new System.Data.SQLite.SQLiteCommand(
                        "SELECT json_data FROM aa_guns_raw_datas WHERE ID = @id",
                        connection))
                    {
                        command.Parameters.AddWithValue("@id", equipmentId);
                        
                        var result = command.ExecuteScalar();
                        
                        if (result != null && result != DBNull.Value)
                        {
                            var jsonPath = result.ToString();
                            
                            if (File.Exists(jsonPath))
                            {
                                // JSONファイルから生データを読み込む
                                var jsonContent = File.ReadAllText(jsonPath);
                                var options = new JsonSerializerOptions
                                {
                                    PropertyNameCaseInsensitive = true
                                };
                                return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent, options);
                            }
                        }
                    }
                }
                
                // データが見つからない場合
                Console.WriteLine($"指定されたID '{equipmentId}' の対空砲データが見つかりませんでした");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"対空砲データの取得中にエラーが発生しました: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                return null;
            }
        }
    }
}