using System;
using System.Collections.Generic;
using HOI4NavalModder.Core.Models;
using HOI4NavalModder.Core.Services;

namespace HOI4NavalModder.Calculators;

public static class TorpedoCalculator
{
    /// <summary>
    ///     魚雷の装備データを処理し、計算された性能値を含む装備オブジェクトを返す
    /// </summary>
    /// <param name="torpedoData">魚雷のパラメータを含むディクショナリ</param>
    /// <returns>処理された装備オブジェクト</returns>
    public static NavalEquipment Torpedo_Processing(Dictionary<string, object> torpedoData)
    {
        // Create a new equipment object based on the collected data
        var equipment = new NavalEquipment
        {
            Id = torpedoData["Id"].ToString(),
            Name = torpedoData["Name"].ToString(),
            Category = torpedoData["Category"].ToString(),
            SubCategory = torpedoData["SubCategory"].ToString(),
            Year = Convert.ToInt32(torpedoData["Year"]),
            Tier = Convert.ToInt32(torpedoData["Tier"]),
            Country = torpedoData["Country"].ToString(),

            // 攻撃値と防御値は計算式で算出
            Attack = CalculateTorpedoAttackValue(torpedoData),
            Defense = 0, // 魚雷は防御力なし
            SpecialAbility = DetermineSpecialAbility(torpedoData),

            // Store all the detailed parameters in AdditionalProperties
            AdditionalProperties = new Dictionary<string, object>()
        };

        // Add all the torpedo data to AdditionalProperties
        foreach (var item in torpedoData)
            if (item.Key != "Id" && item.Key != "Name" && item.Key != "Category" &&
                item.Key != "SubCategory" && item.Key != "Year" && item.Key != "Tier" &&
                item.Key != "Country")
                equipment.AdditionalProperties[item.Key] = item.Value;

        // 計算式で各性能値を計算
        CalculateAndStorePerformanceValues(torpedoData, equipment);

        // データベースに性能値を保存
        SaveToDatabase(equipment, torpedoData);

        return equipment;
    }

    /// <summary>
    /// 計算式で各性能値を計算し、装備のAdditionalPropertiesに保存
    /// </summary>
    private static void CalculateAndStorePerformanceValues(Dictionary<string, object> torpedoData, NavalEquipment equipment)
    {
        try
        {
            // パラメータを取得
            var torpedoWeight = Convert.ToDouble(torpedoData["Weight"]);
            var torpedoSpeed = Convert.ToDouble(torpedoData["TorpedoSpeed"]);
            var explosionWeight = Convert.ToDouble(torpedoData["ExplosionWeight"]);
            var range = Convert.ToDouble(torpedoData["Range"]);
            
            var isAsw = Convert.ToBoolean(torpedoData["IsAsw"]);
            var isAip = Convert.ToBoolean(torpedoData["IsAip"]);
            var isOxi = Convert.ToBoolean(torpedoData["IsOxi"]);
            var isWal = Convert.ToBoolean(torpedoData["IsWal"]);
            var isLine = Convert.ToBoolean(torpedoData["IsLine"]);
            var isHoming = Convert.ToBoolean(torpedoData["IsHoming"]);
            
            // サブカテゴリから口径を取得
            var calibre = 0.0;
            var subCategory = torpedoData["SubCategory"].ToString();
            if (subCategory.EndsWith("mm"))
            {
                var diameterStr = subCategory.Substring(0, subCategory.Length - 2);
                if (double.TryParse(diameterStr, out var diameter))
                    calibre = diameter;
            }
            
            // 魚雷攻撃力計算
            var speedModifier = torpedoSpeed / 40.0; // 40ktを基準とした補正
            var explosionEfficiency = 0.075; // 基本効率
            
            // 種類別補正
            if (isOxi) explosionEfficiency *= 1.25; // 酸素魚雷の場合、効率25%増加
            if (isWal) explosionEfficiency *= 1.3; // ヴァルターエンジンの場合、効率30%増加
            if (isAip) explosionEfficiency *= 1.15; // 閉サイクルの場合、効率15%増加
            
            // 誘導方式による補正
            if (isLine) speedModifier *= 0.9; // 有線誘導の場合、速度補正10%減少
            if (isHoming) explosionEfficiency *= 1.2; // 音響ホーミングの場合、効率20%増加
            
            // 最終的な攻撃力計算
            var torpedoAttack = explosionWeight * explosionEfficiency * speedModifier;
            
            // 対潜攻撃力ボーナス
            if (isAsw) torpedoAttack *= 1.5; // 対潜用の場合、攻撃力50%増加
            
            // 装甲貫通力計算（速度×弾頭重量/口径）
            var armorPiercing = torpedoSpeed * explosionWeight / calibre * 0.05;
            
            // 射程を km 単位に変換
            var rangeKm = range / 1000.0;
            
            // 建造コスト計算（重量+弾頭重量×0.3+誘導方式追加）
            var buildCost = torpedoWeight * 0.001 + explosionWeight * 0.0003;
            
            // 誘導方式によるコスト上昇
            if (isLine) buildCost += 1.0;
            if (isHoming) buildCost += 1.5;
            if (isWal) buildCost += 2.0;
            
            // 計算結果を装備のAdditionalPropertiesに保存
            equipment.AdditionalProperties["CalculatedTorpedoAttack"] = torpedoAttack;
            equipment.AdditionalProperties["CalculatedArmorPiercing"] = armorPiercing;
            equipment.AdditionalProperties["CalculatedRange"] = rangeKm;
            equipment.AdditionalProperties["CalculatedBuildCost"] = buildCost;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"性能値計算中にエラーが発生しました: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    // 魚雷攻撃値の計算
    private static double CalculateTorpedoAttackValue(Dictionary<string, object> torpedoData)
    {
        try
        {
            // パラメータを取得
            var torpedoSpeed = Convert.ToDouble(torpedoData["TorpedoSpeed"]);
            var explosionWeight = Convert.ToDouble(torpedoData["ExplosionWeight"]);
            
            var isAsw = Convert.ToBoolean(torpedoData["IsAsw"]);
            var isAip = Convert.ToBoolean(torpedoData["IsAip"]);
            var isOxi = Convert.ToBoolean(torpedoData["IsOxi"]);
            var isWal = Convert.ToBoolean(torpedoData["IsWal"]);
            var isLine = Convert.ToBoolean(torpedoData["IsLine"]);
            var isHoming = Convert.ToBoolean(torpedoData["IsHoming"]);
            
            // 魚雷攻撃力計算
            var speedModifier = torpedoSpeed / 40.0;
            var explosionEfficiency = 0.075;
            
            // 種類別補正
            if (isOxi) explosionEfficiency *= 1.25;
            if (isWal) explosionEfficiency *= 1.3;
            if (isAip) explosionEfficiency *= 1.15;
            
            // 誘導方式による補正
            if (isLine) speedModifier *= 0.9;
            if (isHoming) explosionEfficiency *= 1.2;
            
            // 最終的な攻撃力計算
            var torpedoAttack = explosionWeight * explosionEfficiency * speedModifier;
            
            // 対潜攻撃力ボーナス
            if (isAsw) torpedoAttack *= 1.5;
            
            return torpedoAttack;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"魚雷攻撃値計算中にエラーが発生しました: {ex.Message}");
            return 0;
        }
    }

    // 魚雷特殊能力の決定
    private static string DetermineSpecialAbility(Dictionary<string, object> torpedoData)
    {
        var abilities = new List<string>();
        
        if (Convert.ToBoolean(torpedoData["IsAsw"]))
            abilities.Add("対潜攻撃");
            
        if (Convert.ToBoolean(torpedoData["IsOxi"]))
            abilities.Add("酸素魚雷");
            
        if (Convert.ToBoolean(torpedoData["IsAip"]))
            abilities.Add("閉サイクル蒸気タービン");
            
        if (Convert.ToBoolean(torpedoData["IsWal"]))
            abilities.Add("ヴァルターエンジン");
            
        if (Convert.ToBoolean(torpedoData["IsLine"]))
            abilities.Add("有線誘導");
            
        if (Convert.ToBoolean(torpedoData["IsHoming"]))
            abilities.Add("音響ホーミング");
            
        return abilities.Count > 0 ? string.Join(", ", abilities) : "なし";
    }

    /// <summary>
    /// 計算されたパラメータをデータベースに保存
    /// </summary>
    private static void SaveToDatabase(NavalEquipment equipment, Dictionary<string, object> torpedoData)
    {
        try
        {
            var dbManager = new DatabaseManager();
            
            // 基本情報用のデータ作成 (module_info)
            var moduleInfo = new ModuleInfo
            {
                Id = equipment.Id,
                Name = equipment.Name,
                Gfx = $"gfx_{equipment.Category.ToLower()}_{equipment.Id}", // GFXパスを生成
                Sfx = "sfx_ui_sd_module_installed",                         // デフォルトSFX
                Year = equipment.Year,
                Manpower = 0, // 魚雷には必要人員なし
                Country = equipment.Country,
                CriticalParts = GetCriticalPartsForCategory(equipment.Category) // カテゴリに応じたcritical_partsを設定
            };
            
            // module_add_stats用のデータ作成（加算ステータス）
            var addStats = new ModuleStats();
            
            // 魚雷攻撃力、建造コスト
            addStats.TorpedoAttack = Convert.ToDouble(equipment.AdditionalProperties["CalculatedTorpedoAttack"]);
            addStats.BuildCostIc = Convert.ToDouble(equipment.AdditionalProperties["CalculatedBuildCost"]);
            
            // 射程
            addStats.NavalRange = Convert.ToDouble(equipment.AdditionalProperties["CalculatedRange"]) * 100; // km → 100km単位
            
            // module_add_average_stats用のデータ作成（平均加算ステータス）
            var addAverageStats = new ModuleStats();
            
            // 特殊な魚雷の場合は視認性を下げる
            if (Convert.ToBoolean(torpedoData["IsOxi"]) || Convert.ToBoolean(torpedoData["IsWal"]))
                addAverageStats.SurfaceVisibility = -5; // 視認性低下（酸素魚雷、ヴァルターエンジン）
            
            // // サブマリン魚雷の場合は潜在視認性も下げる
            // if (equipment.Category == "SMSTP")
            //     addAverageStats.SubVisibility = -5;
            
            // デバッグ用：保存される値のログ出力
            Console.WriteLine($"=== 保存データ確認 ({equipment.Id}) ===");
            Console.WriteLine($"module_add_stats.TorpedoAttack: {addStats.TorpedoAttack}");
            Console.WriteLine($"module_add_stats.BuildCostIc: {addStats.BuildCostIc}");
            Console.WriteLine($"module_add_stats.NavalRange: {addStats.NavalRange}");
            Console.WriteLine($"module_add_average_stats.SurfaceVisibility: {addAverageStats.SurfaceVisibility}");
            Console.WriteLine($"module_add_average_stats.SubVisibility: {addAverageStats.SubVisibility}");
            Console.WriteLine($"=== 保存データ終了({equipment.Id}) ===");
            
            // module_multiply_stats用のデータ作成（乗算ステータス）
            var multiplyStats = new ModuleStats();
            
            // 魚雷は機動性を向上させる
            multiplyStats.Evasion = 1.05; // 5%増加
            
            // リソース要件用のデータ作成
            var resources = new ModuleResources
            {
                Steel = 1,
                Chromium = 0,
                Aluminium = 0,
                Oil = 0,
                Tungsten = 0,
                Rubber = 0
            };
            
            // 変換可能モジュール情報（魚雷には特にないため空リスト）
            var convertModules = new List<ModuleConvert>();
            
            // データベースに保存
            dbManager.SaveModuleData(
                moduleInfo,
                addStats,
                multiplyStats,
                addAverageStats,
                resources,
                convertModules
            );
            
            Console.WriteLine($"装備 {equipment.Id} のデータをデータベースに保存しました。");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"データベース保存中にエラーが発生しました: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }
    
    /// <summary>
    /// カテゴリに応じたcritical_partsを返す
    /// </summary>
    private static string GetCriticalPartsForCategory(string category)
    {
        switch (category)
        {
            case "SMTP": // 魚雷
                return "damaged_torpedo_tubes";
            case "SMSTP": // 潜水艦魚雷
                return "damaged_torpedo_tubes";
            default:
                return "";
        }
    }
}