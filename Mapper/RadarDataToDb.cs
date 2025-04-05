using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using HOI4NavalModder.Core.Models;
using HOI4NavalModder.Core.Services;

namespace HOI4NavalModder.Mapper
{
    /// <summary>
    ///     Radar Design View と DB の間でデータを転送する中間クラス
    /// </summary>
    public static class RadarDataToDb
    {
        /// <summary>
        ///     レーダーの生データをデータベースに保存する
        /// </summary>
        /// <param name="equipment">RadarCalculatorから処理された装備データ</param>
        /// <param name="rawRadarData">Radar_Design_Viewから渡される元のパラメータデータ</param>
        /// <returns>保存に成功したかどうか</returns>
        public static bool SaveRadarData(NavalEquipment equipment, Dictionary<string, object> rawRadarData)
        {
            try
            {
                // データベースマネージャーのインスタンスを作成
                var dbManager = new DatabaseManager();

                // 生データをデータベースに保存（この中でJSONファイルも保存される）
                var rawDataSaved = dbManager.SaveRawRadarData(equipment.Id, rawRadarData);
                if (!rawDataSaved)
                {
                    Console.WriteLine($"レーダーの生データの保存に失敗しました: {equipment.Id}");
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

                Console.WriteLine($"レーダーデータ {equipment.Id} をデータベースに保存しました");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"レーダーデータの保存中にエラーが発生しました: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
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

            // 加算ステータス - カテゴリに応じた適切なステータス設定
            moduleData.AddStats = new ModuleStats();

            // レーダーの種類によって異なるステータスを設定
            switch (equipment.Category)
            {
                case "SMLR": // 小型電探
                    // 小型レーダーは対空性能が主
                    moduleData.AddStats.AntiAirAttack = equipment.AdditionalProperties.ContainsKey("CalculatedAirDetection")
                        ? Convert.ToDouble(equipment.AdditionalProperties["CalculatedAirDetection"]) * 0.2
                        : 0;
                    break;
                case "SMHR": // 大型電探
                    // 大型レーダーは対艦性能が主
                    moduleData.AddStats.FireRange = equipment.AdditionalProperties.ContainsKey("CalculatedDetectionRange")
                        ? Convert.ToDouble(equipment.AdditionalProperties["CalculatedDetectionRange"]) * 0.5
                        : 0;
                    
                    // 射撃管制能力が高い場合は砲撃力に加算
                    if (equipment.AdditionalProperties.ContainsKey("CalculatedFireControl") && 
                        equipment.AdditionalProperties.ContainsKey("IsFireControl") &&
                        Convert.ToBoolean(equipment.AdditionalProperties["IsFireControl"]))
                    {
                        var fireControl = Convert.ToDouble(equipment.AdditionalProperties["CalculatedFireControl"]);
                        moduleData.AddStats.HgAttack = fireControl * 0.1;
                        moduleData.AddStats.LgAttack = fireControl * 0.1;
                    }
                    break;
            }

            // 共通のステータス
            // 探知力をセット
            moduleData.AddStats.SurfaceDetection = equipment.AdditionalProperties.ContainsKey("CalculatedSurfaceDetection")
                ? Convert.ToDouble(equipment.AdditionalProperties["CalculatedSurfaceDetection"])
                : 0;
                
            // 可視度ペナルティがある場合は設定
            if (equipment.AdditionalProperties.ContainsKey("CalculatedVisibilityPenalty"))
            {
                var visibility = Convert.ToDouble(equipment.AdditionalProperties["CalculatedVisibilityPenalty"]);
                moduleData.AddStats.SurfaceVisibility = visibility;
            }
            
            // コストを設定
            moduleData.AddStats.BuildCostIc = equipment.AdditionalProperties.ContainsKey("CalculatedBuildCost")
                ? Convert.ToDouble(equipment.AdditionalProperties["CalculatedBuildCost"])
                : 0;

            // 乗算ステータス - 信頼性をここに設定
            moduleData.MultiplyStats = new ModuleStats();
            if (equipment.AdditionalProperties.ContainsKey("CalculatedReliability"))
                moduleData.MultiplyStats.Reliability = 
                    Convert.ToDouble(equipment.AdditionalProperties["CalculatedReliability"]);
            
            // 平均加算ステータス - 空中機探知をここに設定
            moduleData.AddAverageStats = new ModuleStats();
            if (equipment.AdditionalProperties.ContainsKey("CalculatedAirDetection"))
                moduleData.AddAverageStats.AntiAirAttack = 
                    Convert.ToDouble(equipment.AdditionalProperties["CalculatedAirDetection"]) * 0.5;

            // リソース
            moduleData.Resources = new ModuleResources();
            if (equipment.AdditionalProperties.ContainsKey("Steel"))
                moduleData.Resources.Steel = Convert.ToInt32(equipment.AdditionalProperties["Steel"]);
            if (equipment.AdditionalProperties.ContainsKey("Tungsten"))
                moduleData.Resources.Tungsten = Convert.ToInt32(equipment.AdditionalProperties["Tungsten"]);
            if (equipment.AdditionalProperties.ContainsKey("Electronics"))
                moduleData.Resources.Chromium = Convert.ToInt32(equipment.AdditionalProperties["Electronics"]);

            // 変換モジュール情報は空のリストを使用
            moduleData.ConvertModules = new List<ModuleConvert>();

            return moduleData;
        }

        /// <summary>
        ///     DBからRadar_Design_View用の生データを取得する
        /// </summary>
        /// <param name="equipmentId">装備ID</param>
        /// <returns>レーダーパラメータの生データ</returns>
        public static Dictionary<string, object> GetRawRadarData(string equipmentId)
        {
            try
            {
                var dbManager = new DatabaseManager();
                return dbManager.GetRawRadarData(equipmentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"レーダーの生データ取得中にエラーが発生しました: {ex.Message}");
                return null;
            }
        }
    }
}