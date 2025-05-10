using System;
using System.Collections.Generic;
using HOI4NavalModder.Core.Models;
using HOI4NavalModder.Core.Services;

namespace HOI4NavalModder.Mapper;

/// <summary>
///     ソナー設計ビューとデータベースの間でデータを転送する中間クラス
/// </summary>
public static class SonarDataToDb
{
    /// <summary>
    ///     ソナーの生データをデータベースに保存する
    /// </summary>
    /// <param name="equipment">SonarCalculatorから処理された装備データ</param>
    /// <param name="rawSonarData">Sonar_Design_Viewから渡される元のパラメータデータ</param>
    /// <returns>保存に成功したかどうか</returns>
    public static bool SaveSonarData(NavalEquipment equipment, Dictionary<string, object> rawSonarData)
    {
        try
        {
            // データベースマネージャーのインスタンスを作成
            var dbManager = new DatabaseManager();

            // 生データをデータベースに保存（この中でJSONファイルも保存される）
            var rawDataSaved = dbManager.SaveRawGunData(equipment.Id, rawSonarData);
            if (!rawDataSaved)
            {
                Console.WriteLine($"ソナーの生データの保存に失敗しました: {equipment.Id}");
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

            Console.WriteLine($"ソナーデータ {equipment.Id} をデータベースに保存しました");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ソナーデータの保存中にエラーが発生しました: {ex.Message}");
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

        // 加算ステータス
        moduleData.AddStats = new ModuleStats();

        // ソナーのステータスを設定
        moduleData.AddStats.SubDetection = equipment.Defense;
        moduleData.AddStats.SubAttack = equipment.Attack;

        // 表面探知力
        if (equipment.AdditionalProperties.ContainsKey("CalculatedSurfaceDetection"))
            moduleData.AddStats.SurfaceDetection =
                Convert.ToDouble(equipment.AdditionalProperties["CalculatedSurfaceDetection"]);

        // 探知範囲
        if (equipment.AdditionalProperties.ContainsKey("CalculatedDetectionRange"))
            moduleData.AddStats.FireRange =
                Convert.ToDouble(equipment.AdditionalProperties["CalculatedDetectionRange"]);

        // 建造コスト
        moduleData.AddStats.BuildCostIc = equipment.AdditionalProperties.ContainsKey("CalculatedBuildCost")
            ? Convert.ToDouble(equipment.AdditionalProperties["CalculatedBuildCost"])
            : 1.0;

        // 信頼性
        if (equipment.AdditionalProperties.ContainsKey("CalculatedReliability"))
            moduleData.AddStats.Reliability = Convert.ToDouble(equipment.AdditionalProperties["CalculatedReliability"]);

        // 乗算ステータスは空のオブジェクトを使用
        moduleData.MultiplyStats = new ModuleStats();

        // 平均加算ステータスも空のオブジェクトを使用
        moduleData.AddAverageStats = new ModuleStats();

        // リソース
        moduleData.Resources = new ModuleResources();
        if (equipment.AdditionalProperties.ContainsKey("Steel"))
            moduleData.Resources.Steel = Convert.ToInt32(equipment.AdditionalProperties["Steel"]);
        if (equipment.AdditionalProperties.ContainsKey("Tungsten"))
            moduleData.Resources.Tungsten = Convert.ToInt32(equipment.AdditionalProperties["Tungsten"]);
        if (equipment.AdditionalProperties.ContainsKey("Electronics"))
            moduleData.Resources.Aluminium = Convert.ToInt32(equipment.AdditionalProperties["Electronics"]);

        // 変換モジュール情報は空のリストを使用
        moduleData.ConvertModules = new List<ModuleConvert>();

        return moduleData;
    }

    /// <summary>
    ///     DBからSonar_Design_View用の生データを取得する
    /// </summary>
    /// <param name="equipmentId">装備ID</param>
    /// <returns>ソナーパラメータの生データ</returns>
    public static Dictionary<string, object> GetRawSonarData(string equipmentId)
    {
        try
        {
            var dbManager = new DatabaseManager();
            return dbManager.GetRawGunData(equipmentId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ソナーの生データ取得中にエラーが発生しました: {ex.Message}");
            return null;
        }
    }
}