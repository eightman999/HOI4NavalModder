using System;
using System.Collections.Generic;
using HOI4NavalModder.Core.Models;

namespace HOI4NavalModder.Calculators
{
    /// <summary>
    /// 爆雷の性能計算を行うクラス
    /// </summary>
    public static class DepthChargeCalculator
    {
        /// <summary>
        /// 爆雷データの処理
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
                
                // リソースの取得
                var steel = rawDCData.ContainsKey("Steel") ? Convert.ToDouble(rawDCData["Steel"]) : 0;
                var explosives = rawDCData.ContainsKey("Explosives") ? Convert.ToDouble(rawDCData["Explosives"]) : 0;
                
                // 特殊機能はなし
                
                // 性能計算
                // 年代による技術補正
                double techModifier = 1.0;
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
                
                // カテゴリによる基本補正
                double categoryModifier = 1.0;
                if (category == "SMDCL") // 爆雷投射機の場合
                    categoryModifier = 1.1;
                    
                // 爆発力の計算 = 炸薬重量 × エネルギー密度 × 各種修正
                var explosivePower = explosiveWeight * explosiveEnergyDensity * techModifier * categoryModifier;
                
                // 対潜攻撃力の計算
                var subAttack = explosivePower * 0.1; // 係数は適宜調整
                
                // 建造コスト計算
                var buildCost = 0.5 + (weight * 0.002) + (explosiveWeight * explosiveEnergyDensity * 0.0005);
                
                // 信頼性計算（0.0～1.0）
                var reliability = 0.8 + (techModifier * 0.1);
                
                // 最大1.0に制限
                reliability = Math.Min(Math.Max(reliability, 0.6), 1.0);
                
                // 計算結果をデータに追加
                rawDCData["CalculatedSubAttack"] = subAttack;
                rawDCData["CalculatedBuildCost"] = buildCost;
                rawDCData["CalculatedReliability"] = reliability;
                
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
                    // 対潜攻撃力を攻撃値として設定
                    Attack = subAttack,
                    // 防御値はなし（攻撃兵器のため）
                    Defense = 0,
                    // パラメータとデータをそのままコピー
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
}