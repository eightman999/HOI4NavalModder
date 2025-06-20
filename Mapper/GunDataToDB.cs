﻿using System;
using System.Collections.Generic;
using HOI4NavalModder.Core.Models;
using HOI4NavalModder.Core.Services;

namespace HOI4NavalModder.Mapper;

/// <summary>
///     Gun Design View と DB の間でデータを転送する中間クラス
/// </summary>
public static class GunDataToDb
{
    /// <summary>
    ///     砲の生データをデータベースに保存する
    /// </summary>
    /// <param name="equipment">GunCalculatorから処理された装備データ</param>
    /// <param name="rawGunData">Gun_Design_Viewから渡される元のパラメータデータ</param>
    /// <returns>保存に成功したかどうか</returns>
    public static bool SaveGunData(NavalEquipment equipment, Dictionary<string, object> rawGunData)
    {
        try
        {
            // データベースマネージャーのインスタンスを作成
            var dbManager = new DatabaseManager();

            // 生データをデータベースに保存（この中でJSONファイルも保存される）
            var rawDataSaved = dbManager.SaveRawGunData(equipment.Id, rawGunData);
            if (!rawDataSaved)
            {
                Console.WriteLine($"砲の生データの保存に失敗しました: {equipment.Id}");
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

            Console.WriteLine($"砲データ {equipment.Id} をデータベースに保存しました");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"砲データの保存中にエラーが発生しました: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     NavalEquipmentからModuleDataに変換する
    /// </summary>
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

        // 計算されたステータス値を適切なフィールドに設定
        switch (equipment.Category)
        {
            case "SMLG": // 小口径砲
            case "SMMG": // 中口径砲
                moduleData.AddStats.LgAttack = equipment.Attack;
                break;
            case "SMHG": // 大口径砲
            case "SMSHG": // 超大口径砲
                moduleData.AddStats.HgAttack = equipment.Attack;
                break;
        }

        // 共通のステータス
        moduleData.AddStats.FireRange = equipment.AdditionalProperties.ContainsKey("CalculatedRange")
            ? Convert.ToDouble(equipment.AdditionalProperties["CalculatedRange"])
            : 0;
        moduleData.AddStats.BuildCostIc = equipment.AdditionalProperties.ContainsKey("CalculatedBuildCost")
            ? Convert.ToDouble(equipment.AdditionalProperties["CalculatedBuildCost"])
            : 0;
        moduleData.AddStats.ShoreBombardment = equipment.Attack * 0.5; // 砲撃力の半分を海岸砲撃力として設定

        // 乗算ステータスは空のオブジェクトを使用
        moduleData.MultiplyStats = new ModuleStats();

        // 平均加算ステータス - 装甲貫通力をここに設定（修正）
        moduleData.AddAverageStats = new ModuleStats();

        // 装甲貫通力を平均加算に移動
        if (equipment.AdditionalProperties.ContainsKey("CalculatedLgArmorPiercing"))
            moduleData.AddAverageStats.LgArmorPiercing =
                Convert.ToDouble(equipment.AdditionalProperties["CalculatedLgArmorPiercing"]);

        if (equipment.AdditionalProperties.ContainsKey("CalculatedHgArmorPiercing"))
            moduleData.AddAverageStats.HgArmorPiercing =
                Convert.ToDouble(equipment.AdditionalProperties["CalculatedHgArmorPiercing"]);

        if (equipment.AdditionalProperties.ContainsKey("CalculatedArmorPiercing"))
        {
            var armorPiercing = Convert.ToDouble(equipment.AdditionalProperties["CalculatedArmorPiercing"]);

            // カテゴリによって適切なフィールドに配分
            switch (equipment.Category)
            {
                case "SMLG": // 小口径砲
                case "SMMG": // 中口径砲
                    moduleData.AddAverageStats.LgArmorPiercing = armorPiercing;
                    break;
                case "SMHG": // 大口径砲
                case "SMSHG": // 超大口径砲
                    moduleData.AddAverageStats.HgArmorPiercing = armorPiercing;
                    break;
            }
        }

        // リソース
        moduleData.Resources = new ModuleResources();
        if (equipment.AdditionalProperties.ContainsKey("Steel"))
            moduleData.Resources.Steel = Convert.ToInt32(equipment.AdditionalProperties["Steel"]);
        if (equipment.AdditionalProperties.ContainsKey("Chromium"))
            moduleData.Resources.Chromium = Convert.ToInt32(equipment.AdditionalProperties["Chromium"]);

        // 変換モジュール情報は空のリストを使用
        moduleData.ConvertModules = new List<ModuleConvert>();

        return moduleData;
    }

    /// <summary>
    ///     DBからGun_Design_View用の生データを取得する
    /// </summary>
    /// <param name="equipmentId">装備ID</param>
    /// <returns>砲パラメータの生データ</returns>
    public static Dictionary<string, object> GetRawGunData(string equipmentId)
    {
        try
        {
            var dbManager = new DatabaseManager();
            return dbManager.GetRawGunData(equipmentId);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"砲の生データ取得中にエラーが発生しました: {ex.Message}");
            return null;
        }
    }
}