using System;
using System.Collections.Generic;
using HOI4NavalModder.Core.Models;

namespace HOI4NavalModder.Calculators
{
    /// <summary>
    /// ソナーの性能計算を行うクラス
    /// </summary>
    public static class SonarCalculator
    {
        /// <summary>
        /// ソナーデータの処理
        /// </summary>
        /// <param name="rawSonarData">生のソナーデータ</param>
        /// <returns>処理済みNavalEquipment</returns>
        public static NavalEquipment Sonar_Processing(Dictionary<string, object> rawSonarData)
        {
            try
            {
                // 基本情報の取得
                var id = rawSonarData["Id"].ToString();
                var name = rawSonarData["Name"].ToString();
                var category = rawSonarData["Category"].ToString();
                var subCategory = rawSonarData["SubCategory"].ToString();
                var year = Convert.ToInt32(rawSonarData["Year"]);
                var tier = Convert.ToInt32(rawSonarData["Tier"]);
                var country = rawSonarData.ContainsKey("Country") ? rawSonarData["Country"].ToString() : "";
                
                // ソナーパラメータの取得
                var frequency = Convert.ToDouble(rawSonarData["Frequency"]);
                var detectionPower = Convert.ToDouble(rawSonarData["DetectionPower"]);
                var detectionSpeed = Convert.ToDouble(rawSonarData["DetectionSpeed"]);
                var weight = Convert.ToDouble(rawSonarData["Weight"]);
                var sonarType = rawSonarData["SonarType"].ToString();
                var manpower = Convert.ToInt32(rawSonarData["Manpower"]);
                
                // リソースの取得
                var steel = rawSonarData.ContainsKey("Steel") ? Convert.ToDouble(rawSonarData["Steel"]) : 0;
                var tungsten = rawSonarData.ContainsKey("Tungsten") ? Convert.ToDouble(rawSonarData["Tungsten"]) : 0;
                var electronics = rawSonarData.ContainsKey("Electronics") ? Convert.ToDouble(rawSonarData["Electronics"]) : 0;
                
                // 特殊機能の取得
                var isNoiseReduction = rawSonarData.ContainsKey("IsNoiseReduction") && Convert.ToBoolean(rawSonarData["IsNoiseReduction"]);
                var isHighFrequency = rawSonarData.ContainsKey("IsHighFrequency") && Convert.ToBoolean(rawSonarData["IsHighFrequency"]);
                var isLongRange = rawSonarData.ContainsKey("IsLongRange") && Convert.ToBoolean(rawSonarData["IsLongRange"]);
                var isDigital = rawSonarData.ContainsKey("IsDigital") && Convert.ToBoolean(rawSonarData["IsDigital"]);
                var isTowedArray = rawSonarData.ContainsKey("IsTowedArray") && Convert.ToBoolean(rawSonarData["IsTowedArray"]);
                
                // 性能計算
                // ソナータイプの係数
                double activeMultiplier = 0;
                double passiveMultiplier = 0;
                
                switch (sonarType)
                {
                    case "アクティブ":
                        activeMultiplier = 1.0;
                        passiveMultiplier = 0.0;
                        break;
                    case "パッシブ":
                        activeMultiplier = 0.0;
                        passiveMultiplier = 1.0;
                        break;
                    case "アクティブ＆パッシブ":
                        activeMultiplier = 0.7;
                        passiveMultiplier = 0.7;
                        break;
                }
                
                // 基本探知力計算
                var baseSurfaceDetection = detectionPower * activeMultiplier * 0.12 + detectionPower * passiveMultiplier * 0.05;
                var baseSubDetection = detectionPower * activeMultiplier * 0.15 + detectionPower * passiveMultiplier * 0.2;
                
                // 周波数による補正
                double frequencyModifier = 1.0;
                if (frequency < 10) 
                    frequencyModifier = 0.8; // 低周波
                else if (frequency > 40) 
                    frequencyModifier = 1.2; // 高周波
                    
                if (isHighFrequency)
                    frequencyModifier *= 1.3; // 高周波機能ボーナス
                    
                // 探知速度による補正
                double speedModifier = detectionSpeed / 10.0;
                
                // 年代による技術補正
                double techModifier = 1.0;
                if (year < 1930) 
                    techModifier = 0.7;
                else if (year < 1940) 
                    techModifier = 0.9;
                else if (year < 1950) 
                    techModifier = 1.0;
                else if (year < 1960) 
                    techModifier = 1.1;
                else 
                    techModifier = 1.2;
                    
                if (isDigital)
                    techModifier *= 1.5; // デジタル信号処理ボーナス
                    
                // 特殊機能による補正
                double specialModifier = 1.0;
                
                if (isNoiseReduction)
                    specialModifier *= 1.2; // 静音化ボーナス
                    
                if (isLongRange)
                    specialModifier *= 1.3; // 長距離探知ボーナス
                    
                if (isTowedArray)
                    specialModifier *= 1.4; // 曳航式アレイボーナス
                    
                // 最終探知力計算
                var surfaceDetection = baseSurfaceDetection * frequencyModifier * speedModifier * techModifier * specialModifier;
                var subDetection = baseSubDetection * frequencyModifier * speedModifier * techModifier * specialModifier;
                
                // 探知範囲計算 (km)
                var detectionRange = 10 + detectionPower * 0.1 * frequencyModifier * techModifier;
                
                if (isLongRange)
                    detectionRange *= 1.5; // 長距離探知ボーナス
                    
                if (isTowedArray)
                    detectionRange *= 1.3; // 曳航式アレイボーナス
                    
                // 潜水艦攻撃力計算（アクティブソナーのみが対潜攻撃力を持つ）
                var subAttack = activeMultiplier * detectionPower * 0.05 * techModifier;
                
                // 建造コスト計算
                var buildCost = weight * 0.005 + detectionPower * 0.01;
                
                // ソナータイプによるコスト補正
                if (sonarType == "アクティブ＆パッシブ")
                    buildCost *= 1.5;
                    
                // 信頼性計算（0.0～1.0）
                var reliability = 0.7 + (techModifier * 0.2);
                
                if (isDigital)
                    reliability += 0.1;
                    
                // 最大1.0に制限
                reliability = Math.Min(reliability, 1.0);
                
                // 計算結果をデータに追加
                rawSonarData["CalculatedSubDetection"] = subDetection;
                rawSonarData["CalculatedSurfaceDetection"] = surfaceDetection;
                rawSonarData["CalculatedDetectionRange"] = detectionRange;
                rawSonarData["CalculatedSubAttack"] = subAttack;
                rawSonarData["CalculatedBuildCost"] = buildCost;
                rawSonarData["CalculatedReliability"] = reliability;
                
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
                    // 探知力を防御値として設定
                    Defense = subDetection,
                    // 対潜攻撃力を攻撃値として設定
                    Attack = subAttack,
                    // パラメータとデータをそのままコピー
                    AdditionalProperties = new Dictionary<string, object>(rawSonarData)
                };
                
                // 特殊能力の設定
                if (isNoiseReduction || isHighFrequency || isLongRange || isDigital || isTowedArray)
                {
                    var specialAbilities = new List<string>();
                    
                    if (isNoiseReduction)
                        specialAbilities.Add("静音化");
                        
                    if (isHighFrequency)
                        specialAbilities.Add("高周波");
                        
                    if (isLongRange)
                        specialAbilities.Add("長距離探知");
                        
                    if (isDigital)
                        specialAbilities.Add("デジタル信号処理");
                        
                    if (isTowedArray)
                        specialAbilities.Add("曳航式アレイ");
                        
                    equipment.SpecialAbility = string.Join(", ", specialAbilities);
                }
                
                return equipment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ソナー計算処理エラー: {ex.Message}");
                
                // エラー時は最低限のデータだけ設定して返す
                return new NavalEquipment
                {
                    Id = rawSonarData.ContainsKey("Id") ? rawSonarData["Id"].ToString() : "error_id",
                    Name = rawSonarData.ContainsKey("Name") ? rawSonarData["Name"].ToString() : "エラー発生",
                    Category = rawSonarData.ContainsKey("Category") ? rawSonarData["Category"].ToString() : "SMSO",
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