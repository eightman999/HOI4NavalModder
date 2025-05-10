using System;
using System.Collections.Generic;
using HOI4NavalModder.Core.Models;

namespace HOI4NavalModder.Calculators;

/// <summary>
///     爆雷の性能計算を行うクラス
/// </summary>
public static class DepthChargeCalculator
{
    /// <summary>
    ///     爆雷データの処理
    /// </summary>
    /// <param name="rawDCData">生の爆雷データ</param>
    /// <returns>処理済みNavalEquipment</returns>
    public static NavalEquipment DC_Processing(Dictionary<string, object> rawDCData)
    {
        try
        {
            // 基本情報の取得
            var id = rawDCData["Id"].ToString();
            var name = rawDCData["Name"].ToString();
            var category = rawDCData["Category"].ToString();
            var subCategory = rawDCData["SubCategory"].ToString();
            var year = Convert.ToInt32(rawDCData["Year"]);
            var tier = Convert.ToInt32(rawDCData["Tier"]);
            var country = rawDCData.ContainsKey("Country") ? rawDCData["Country"].ToString() : "";

            // 爆雷パラメータの取得
            var explosiveWeight = Convert.ToDouble(rawDCData["ExplosiveWeight"]);
            var explosiveEnergyDensity = Convert.ToDouble(rawDCData["ExplosiveEnergyDensity"]);
            var detectionRange = Convert.ToDouble(rawDCData["DetectionRange"]);
            var weight = Convert.ToDouble(rawDCData["Weight"]);
            var manpower = Convert.ToInt32(rawDCData["Manpower"]);

            // 特殊機能の取得
            var isReactive = rawDCData.ContainsKey("IsReactive") && Convert.ToBoolean(rawDCData["IsReactive"]);
            var isMultiLayer = rawDCData.ContainsKey("IsMultiLayer") && Convert.ToBoolean(rawDCData["IsMultiLayer"]);
            var isDirectional = rawDCData.ContainsKey("IsDirectional") && Convert.ToBoolean(rawDCData["IsDirectional"]);
            var isAdvancedFuse = rawDCData.ContainsKey("IsAdvancedFuse") &&
                                 Convert.ToBoolean(rawDCData["IsAdvancedFuse"]);
            var isDeepWater = rawDCData.ContainsKey("IsDeepWater") && Convert.ToBoolean(rawDCData["IsDeepWater"]);

            // カテゴリによる基本補正
            var categoryModifier = 1.0;
            if (category == "SMDCL") // 爆雷投射機の場合
                categoryModifier = 1.1;

            // 年代による技術補正
            var techModifier = 1.0;
            if (year < 1930)
                techModifier = 0.7;
            else if (year < 1940)
                techModifier = 0.9;
            else if (year < 1950)
                techModifier = 1.0;
            else if (year < 1960)
                techModifier = 1.2;
            else if (year < 1970)
                techModifier = 1.3;
            else
                techModifier = 1.5;

            // 特殊機能による補正
            var specialModifier = 1.0;

            if (isReactive)
                specialModifier *= 1.3; // 反応型爆雷ボーナス

            if (isMultiLayer)
                specialModifier *= 1.2; // 多層爆雷ボーナス

            if (isDirectional)
                specialModifier *= 1.5; // 指向性ボーナス（方向を絞った分、威力増加）

            if (isAdvancedFuse)
                specialModifier *= 1.15; // 高性能信管ボーナス

            if (isDeepWater)
                specialModifier *= 1.25; // 深海型ボーナス

            // 爆発力の計算
            var explosivePower = explosiveWeight * explosiveEnergyDensity * techModifier * categoryModifier *
                                 specialModifier;

            // 対潜攻撃力の計算
            var subAttack = explosivePower * 0.1; // 係数は適宜調整

            // 被害範囲の計算 (meters)
            var damageRadius = Math.Pow(explosiveWeight * explosiveEnergyDensity, 1 / 3.0) * 2.5; // 立方根に比例

            // 指向性爆雷は半径が小さい
            if (isDirectional)
                damageRadius *= 0.7;

            // 建造コスト計算
            var buildCost = 0.5 + weight * 0.002 + explosiveWeight * explosiveEnergyDensity * 0.0005;

            // 信頼性計算（0.0～1.0）
            var reliability = 0.8 + techModifier * 0.1;

            if (isAdvancedFuse)
                reliability -= 0.05; // 高性能信管は複雑で信頼性が下がる

            if (isMultiLayer)
                reliability -= 0.05; // 多層構造も複雑で信頼性が下がる

            // 最大1.0に制限
            reliability = Math.Min(Math.Max(reliability, 0.6), 1.0);

            // 計算結果をデータに追加
            rawDCData["CalculatedSubAttack"] = subAttack;
            rawDCData["CalculatedDamageRadius"] = damageRadius;
            rawDCData["CalculatedBuildCost"] = buildCost;
            rawDCData["CalculatedReliability"] = reliability;

            // 特殊能力の設定
            var specialAbilities = new List<string>();

            if (isReactive)
                specialAbilities.Add("反応型");

            if (isMultiLayer)
                specialAbilities.Add("多層構造");

            if (isDirectional)
                specialAbilities.Add("指向性");

            if (isAdvancedFuse)
                specialAbilities.Add("高性能信管");

            if (isDeepWater)
                specialAbilities.Add("深海対応");

            var specialAbilityText = string.Join(", ", specialAbilities);

            // NavalEquipmentオブジェクトを作成
            var equipment = new NavalEquipment
            {
                Id = id,
                Name = name,
                Category = category,
                SubCategory = subCategory,
                Year = year,
                Tier = tier,
                Country = country,
                Attack = subAttack, // 対潜攻撃力
                Defense = 0, // 爆雷は防御値を持たない
                SpecialAbility = specialAbilityText,
                AdditionalProperties = new Dictionary<string, object>(rawDCData)
            };

            return equipment;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"爆雷計算処理エラー: {ex.Message}");

            // エラー時は最低限のデータだけ設定して返す
            return new NavalEquipment
            {
                Id = rawDCData.ContainsKey("Id") ? rawDCData["Id"].ToString() : "error_id",
                Name = rawDCData.ContainsKey("Name") ? rawDCData["Name"].ToString() : "エラー発生",
                Category = rawDCData.ContainsKey("Category") ? rawDCData["Category"].ToString() : "SMDC",
                Year = 1936,
                AdditionalProperties = new Dictionary<string, object>
                {
                    { "Error", ex.Message }
                }
            };
        }
    }
}