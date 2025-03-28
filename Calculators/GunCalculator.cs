using System;
using System.Collections.Generic;
using HOI4NavalModder.Core.Models;
using HOI4NavalModder.Core.Services;

namespace HOI4NavalModder.Calculators;

public static class GunCalculator
{
    /// <summary>
    ///     砲の装備データを処理し、計算された性能値を含む装備オブジェクトを返す
    /// </summary>
    /// <param name="gunData">砲のパラメータを含むディクショナリ</param>
    /// <returns>処理された装備オブジェクト</returns>
    public static NavalEquipment Gun_Processing(Dictionary<string, object> gunData)
    {
        // Create a new equipment object based on the collected data
        var equipment = new NavalEquipment
        {
            Id = gunData["Id"].ToString(),
            Name = gunData["Name"].ToString(),
            Category = gunData["Category"].ToString(),
            SubCategory = gunData["SubCategory"].ToString(),
            Year = (int)gunData["Year"],
            Tier = (int)gunData["Tier"],
            Country = gunData["Country"].ToString(),

            // 攻撃値と防御値は新しい計算式で算出
            Attack = CalculateAttackValue(gunData),
            Defense = CalculateDefenseValue(gunData),
            SpecialAbility = DetermineSpecialAbility(gunData),

            // Store all the detailed parameters in AdditionalProperties
            AdditionalProperties = new Dictionary<string, object>()
        };

        // Add all the gun data to AdditionalProperties
        foreach (var item in gunData)
            if (item.Key != "Id" && item.Key != "Name" && item.Key != "Category" &&
                item.Key != "SubCategory" && item.Key != "Year" && item.Key != "Tier")
                equipment.AdditionalProperties[item.Key] = item.Value;

        // 新しい計算式で各性能値を計算
        CalculateAndStorePerformanceValues(gunData, equipment);

        // データベースに性能値を保存
        SaveToDatabase(equipment, gunData);

        return equipment;
    }

    /// <summary>
    /// 新しい計算式で各性能値を計算し、装備のAdditionalPropertiesに保存
    /// </summary>
    private static void CalculateAndStorePerformanceValues(Dictionary<string, object> gunData, NavalEquipment equipment)
    {
        try
        {
            // パラメータを取得
            var shellWeight = Convert.ToDouble(gunData["ShellWeight"]);               // 砲弾重量 (kg)
            var muzzleVelocity = Convert.ToDouble(gunData["MuzzleVelocity"]);         // 初速 (m/s)
            var rpm = Convert.ToDouble(gunData["RPM"]);                               // 毎分発射数
            var barrelCount = Convert.ToInt32(gunData["BarrelCount"]);                // 砲身数
            
            // 口径関連の処理
            var calibre = Convert.ToDouble(gunData["Calibre"]);                       // 口径
            var calibreType = gunData["CalibreType"].ToString();                      // 口径単位
            var calibreInMm = ConvertCalibreToMm(calibre, calibreType);               // 口径をmmに統一
            
            var elevationAngle = Convert.ToDouble(gunData["ElevationAngle"]);         // 最大仰俯角
            var barrelLength = Convert.ToDouble(gunData["BarrelLength"]);             // 砲身長
            var turretWeight = Convert.ToDouble(gunData["TurretWeight"]);             // 砲塔重量
            var year = (int)gunData["Year"];                                          // 開発年
            var isAsw = gunData.ContainsKey("IsAsw") && Convert.ToBoolean(gunData["IsAsw"]); // 対潜攻撃可能フラグ
            
            // 攻撃力を計算 (m*v^2/4000000*RoF)
            var attackBase = shellWeight * Math.Pow(muzzleVelocity, 2) / 4000000 * rpm;
            
            // 口径範囲によって攻撃タイプを決定 (14cm-24cm間はグラデーション)
            double lgAttackValue = 0;
            double hgAttackValue = 0;
            
            if (calibreInMm < 140) {
                // 完全に軽砲攻撃
                lgAttackValue = attackBase;
            } else if (calibreInMm > 240) {
                // 完全に重砲攻撃
                hgAttackValue = attackBase;
            } else {
                // 14cm～24cmの間は線形補間でグラデーション
                var heavyRatio = (calibreInMm - 140) / 100.0; // 14cmで0%、24cmで100%
                lgAttackValue = attackBase * (1 - heavyRatio);
                hgAttackValue = attackBase * heavyRatio;
            }
            
            // 砲身数による倍率
            lgAttackValue *= barrelCount;
            hgAttackValue *= barrelCount;
            
            // 装甲貫通値を計算 (攻撃力/口径の二乗)
            var lgArmorPiercing = lgAttackValue > 0 ? lgAttackValue / Math.Pow(calibreInMm / 10, 2) : 0;
            var hgArmorPiercing = hgAttackValue > 0 ? hgAttackValue / Math.Pow(calibreInMm / 10, 2) : 0;
            
            // 射程計算（新しい計算式: 口径(mm) * 初速 * 砲身長 * 仰角 / 80000）
            var rangeValue = calibreInMm * muzzleVelocity * barrelLength * elevationAngle / 80000;
            
            // 対空攻撃力を計算 (RoF/8.5)*(v*750)*(θ_max/75)*(100/d)
            var antiAirAttack = (rpm / 8.5) * (muzzleVelocity / 750) * (elevationAngle / 75) * (100 / calibreInMm) * barrelCount;
            
            // 建造コストを計算 (d^1.5+Wt/10d+RoF*0.75+(Y-1920)/10)
            var buildCost = Math.Pow(calibreInMm / 10, 1.5) + turretWeight / (10 * calibreInMm / 10) + rpm * 0.75 + (year - 1920) / 10;
            
            // 対潜攻撃力を計算（対潜攻撃可能フラグがある場合のみ）
            var subAttack = isAsw ? (shellWeight / 35) * (870 / muzzleVelocity) * (rpm / 5) * 1.25 * barrelCount : 0;

            // 計算結果を装備のAdditionalPropertiesに保存
            equipment.AdditionalProperties["CalculatedLgAttack"] = lgAttackValue;
            equipment.AdditionalProperties["CalculatedHgAttack"] = hgAttackValue;
            equipment.AdditionalProperties["CalculatedLgArmorPiercing"] = lgArmorPiercing;
            equipment.AdditionalProperties["CalculatedHgArmorPiercing"] = hgArmorPiercing;
            equipment.AdditionalProperties["CalculatedAntiAirAttack"] = antiAirAttack;
            equipment.AdditionalProperties["CalculatedBuildCost"] = buildCost;
            equipment.AdditionalProperties["CalculatedSubAttack"] = subAttack;
            
            // 計算値を直接保存
            equipment.AdditionalProperties["CalculatedAttack"] = attackBase * barrelCount;
            equipment.AdditionalProperties["CalculatedRange"] = rangeValue;
            
            // 装甲貫通値（合計値）
            var armorPiercingValue = lgArmorPiercing + hgArmorPiercing;
            equipment.AdditionalProperties["CalculatedArmorPiercing"] = armorPiercingValue;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"性能値計算中にエラーが発生しました: {ex.Message}");
            Console.WriteLine(ex.StackTrace);
        }
    }

    /// <summary>
    /// 各種口径単位をmmに変換
    /// </summary>
    private static double ConvertCalibreToMm(double calibre, string calibreType)
    {
        switch (calibreType)
        {
            case "cm":
                return calibre * 10;
            case "inch":
                return calibre * 25.4;
            case "mm":
                return calibre;
            default:
                return calibre * 10; // デフォルトはcmとして扱う
        }
    }

    /// <summary>
    /// 計算されたパラメータをデータベースに保存
    /// </summary>
    private static void SaveToDatabase(NavalEquipment equipment, Dictionary<string, object> gunData)
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
                Manpower = Convert.ToInt32(gunData["Manpower"]),
                Country = equipment.Country,
                CriticalParts = GetCriticalPartsForCategory(equipment.Category) // カテゴリに応じたcritical_partsを設定
            };
            
            // module_add_stats用のデータ作成（加算ステータス）
            var addStats = new ModuleStats();
            
            // 軽重攻撃力、対空攻撃力、建造コスト、対潜攻撃力、射程
            addStats.LgAttack = Convert.ToDouble(equipment.AdditionalProperties["CalculatedLgAttack"]);
            addStats.HgAttack = Convert.ToDouble(equipment.AdditionalProperties["CalculatedHgAttack"]);
            addStats.AntiAirAttack = Convert.ToDouble(equipment.AdditionalProperties["CalculatedAntiAirAttack"]);
            addStats.BuildCostIc = Convert.ToDouble(equipment.AdditionalProperties["CalculatedBuildCost"]);
            addStats.FireRange = Convert.ToDouble(equipment.AdditionalProperties["CalculatedRange"]);
            
            if (gunData.ContainsKey("IsAsw") && Convert.ToBoolean(gunData["IsAsw"]))
                addStats.SubAttack = Convert.ToDouble(equipment.AdditionalProperties["CalculatedSubAttack"]);
            
            // module_add_average_stats用のデータ作成（平均加算ステータス）
            var addAverageStats = new ModuleStats();
            
            // 装甲貫通力
            addAverageStats.LgArmorPiercing = Convert.ToDouble(equipment.AdditionalProperties["CalculatedLgArmorPiercing"]);
            addAverageStats.HgArmorPiercing = Convert.ToDouble(equipment.AdditionalProperties["CalculatedHgArmorPiercing"]);
            
            // デバッグ用：保存される値のログ出力
            Console.WriteLine($"=== 保存データ確認 ({equipment.Id}) ===");
            Console.WriteLine($"module_add_stats.LgAttack: {addStats.LgAttack}");
            Console.WriteLine($"module_add_stats.HgAttack: {addStats.HgAttack}");
            Console.WriteLine($"module_add_stats.AntiAirAttack: {addStats.AntiAirAttack}");
            Console.WriteLine($"module_add_stats.BuildCostIc: {addStats.BuildCostIc}");
            Console.WriteLine($"module_add_stats.FireRange: {addStats.FireRange}");
            Console.WriteLine($"module_add_stats.SubAttack: {addStats.SubAttack}");
            Console.WriteLine($"module_add_average_stats.LgArmorPiercing: {addAverageStats.LgArmorPiercing}");
            Console.WriteLine($"module_add_average_stats.HgArmorPiercing: {addAverageStats.HgArmorPiercing}");
            Console.WriteLine($"=== 保存データ終了({equipment.Id}) ===");
            
            // module_multiply_stats用のデータ作成（乗算ステータス）
            var multiplyStats = new ModuleStats();
            
            // リソース要件用のデータ作成
            var resources = new ModuleResources
            {
                Steel = (int)Math.Round(Convert.ToDouble(gunData["Steel"])),
                Chromium = (int)Math.Round(Convert.ToDouble(gunData["Chromium"])),
                Aluminium = 0,
                Oil = 0,
                Tungsten = 0,
                Rubber = 0
            };
            
            // 変換可能モジュール情報（砲には特にないため空リスト）
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

    // 新しい計算式に基づく攻撃値の計算
    private static double CalculateAttackValue(Dictionary<string, object> gunData)
    {
        try
        {
            // パラメータを取得
            var shellWeight = Convert.ToDouble(gunData["ShellWeight"]);              
            var muzzleVelocity = Convert.ToDouble(gunData["MuzzleVelocity"]);        
            var rpm = Convert.ToDouble(gunData["RPM"]);                              
            var barrelCount = Convert.ToInt32(gunData["BarrelCount"]);               
            
            // 攻撃力計算: m*v^2/4000000*RoF*barrelCount
            return shellWeight * Math.Pow(muzzleVelocity, 2) / 4000000 * rpm * barrelCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"攻撃値計算中にエラーが発生しました: {ex.Message}");
            return 0;
        }
    }

    // 新しい計算式に基づく防御値の計算
    private static double CalculateDefenseValue(Dictionary<string, object> gunData)
    {
        try
        {
            // 対空攻撃力を防御値として使用
            var rpm = Convert.ToDouble(gunData["RPM"]);
            var muzzleVelocity = Convert.ToDouble(gunData["MuzzleVelocity"]);
            var elevationAngle = Convert.ToDouble(gunData["ElevationAngle"]);
            var barrelCount = Convert.ToInt32(gunData["BarrelCount"]);
            
            // 口径をmmに統一
            var calibre = Convert.ToDouble(gunData["Calibre"]);
            var calibreType = gunData["CalibreType"].ToString();
            var calibreInMm = ConvertCalibreToMm(calibre, calibreType);
            
            // 対空攻撃力計算: (RoF/8.5)*(v*750)*(θ_max/75)*(100/d)*barrelCount
            return (rpm / 8.5) * (muzzleVelocity / 750) * (elevationAngle / 75) * (100 / calibreInMm) * barrelCount;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"防御値計算中にエラーが発生しました: {ex.Message}");
            return 0;
        }
    }

    // Determine special abilities based on gun parameters
    private static string DetermineSpecialAbility(Dictionary<string, object> gunData)
    {
        // 対潜攻撃可能フラグのみを確認
        var isAsw = gunData.ContainsKey("IsAsw") && Convert.ToBoolean(gunData["IsAsw"]);

        // 対潜能力
        if (isAsw) return "対潜攻撃可能";

        // 特殊能力がない場合
        return "なし";
    }
    
    /// <summary>
    /// カテゴリに応じたcritical_partsを返す
    /// </summary>
    private static string GetCriticalPartsForCategory(string category)
    {
        switch (category)
        {
            case "SMLG": // 小口径砲
                return "damaged_light_guns";
            case "SMMG": // 中口径砲
                return "damaged_secondaries";
            case "SMHG": // 大口径砲
            case "SMSHG": // 超大口径砲
                return "damaged_heavy_guns";
            default:
                return "";
        }
    }
}