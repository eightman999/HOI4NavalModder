using System;
using System.Collections.Generic;
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
        /// <param name="equipment">AACalculatorから処理された装備データ</param>
        /// <param name="rawAAData">AA_Design_Viewから渡される元のパラメータデータ</param>
        /// <returns>保存に成功したかどうか</returns>
        public static bool SaveAAGunData(NavalEquipment equipment, Dictionary<string, object> rawAAData)
        {
            try
            {
                // データベースマネージャーのインスタンスを作成
                var dbManager = new DatabaseManager();
                
                // 生データをデータベースに保存（この中でJSONファイルも保存される）
                var rawDataSaved = dbManager.SaveRawGunData(equipment.Id, rawAAData);
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
        /// NavalEquipmentからModuleDataに変換する
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
            
            // 加算ステータス
            moduleData.AddStats = new ModuleStats();
            
            // 対空値は必ず設定
            moduleData.AddStats.AntiAirAttack = equipment.Attack;
            
            // 特殊機能に応じて軽砲攻撃力を設定
            if (equipment.AdditionalProperties.ContainsKey("CalculatedLgAttack"))
                moduleData.AddStats.LgAttack = Convert.ToDouble(equipment.AdditionalProperties["CalculatedLgAttack"]);
            
            // 対潜攻撃力を設定
            if (equipment.AdditionalProperties.ContainsKey("CalculatedSubAttack"))
                moduleData.AddStats.SubAttack = Convert.ToDouble(equipment.AdditionalProperties["CalculatedSubAttack"]);
            
            // 射程を設定
            if (equipment.AdditionalProperties.ContainsKey("CalculatedRange"))
                moduleData.AddStats.FireRange = Convert.ToDouble(equipment.AdditionalProperties["CalculatedRange"]);
            
            // 建造コストを設定
            if (equipment.AdditionalProperties.ContainsKey("CalculatedBuildCost"))
                moduleData.AddStats.BuildCostIc = Convert.ToDouble(equipment.AdditionalProperties["CalculatedBuildCost"]);
            
            // 乗算ステータスは空のオブジェクトを使用
            moduleData.MultiplyStats = new ModuleStats();
            
            // 平均加算ステータス - 装甲貫通力など
            moduleData.AddAverageStats = new ModuleStats();
            
            if (equipment.AdditionalProperties.ContainsKey("CalculatedArmorPiercing"))
                moduleData.AddAverageStats.LgArmorPiercing = 
                    Convert.ToDouble(equipment.AdditionalProperties["CalculatedArmorPiercing"]);
            
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
        /// DBからAA_Design_View用の生データを取得する
        /// </summary>
        /// <param name="equipmentId">装備ID</param>
        /// <returns>対空砲パラメータの生データ</returns>
        public static Dictionary<string, object> GetRawAAGunData(string equipmentId)
        {
            try
            {
                var dbManager = new DatabaseManager();
                return dbManager.GetRawGunData(equipmentId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"対空砲の生データ取得中にエラーが発生しました: {ex.Message}");
                return null;
            }
        }
    }
}