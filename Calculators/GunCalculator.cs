using System;
using System.Collections.Generic;

namespace HOI4NavalModder
{
    public static class GunCalculator
    {
        /// <summary>
        /// 砲の装備データを処理し、計算された性能値を含む装備オブジェクトを返す
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
                
                // Calculate attack and defense values based on the gun parameters
                Attack = CalculateAttackValue(gunData),
                Defense = CalculateDefenseValue(gunData),
                SpecialAbility = DetermineSpecialAbility(gunData),
                
                // Store all the detailed parameters in AdditionalProperties
                AdditionalProperties = new Dictionary<string, object>()
            };
            
            // Add all the gun data to AdditionalProperties
            foreach (var item in gunData)
            {
                if (item.Key != "Id" && item.Key != "Name" && item.Key != "Category" &&
                    item.Key != "SubCategory" && item.Key != "Year" && item.Key != "Tier")
                {
                    equipment.AdditionalProperties[item.Key] = item.Value;
                }
            }
            
            // Calculate and add performance values
            double attackValue = CalculateAttackValue(gunData);
            double rangeValue = CalculateRangeValue(gunData);
            double armorPiercingValue = CalculateArmorPiercingValue(gunData);
            double buildCostValue = CalculateBuildCostValue(gunData);
            
            equipment.AdditionalProperties["CalculatedAttack"] = attackValue;
            equipment.AdditionalProperties["CalculatedRange"] = rangeValue;
            equipment.AdditionalProperties["CalculatedArmorPiercing"] = armorPiercingValue;
            equipment.AdditionalProperties["CalculatedBuildCost"] = buildCostValue;
            
            return equipment;
        }
        
        // Calculate attack value based on gun parameters
        private static double CalculateAttackValue(Dictionary<string, object> gunData)
        {
            // 計算式
            double shellWeight = Convert.ToDouble(gunData["ShellWeight"]);
            double muzzleVelocity = Convert.ToDouble(gunData["MuzzleVelocity"]);
            double rpm = Convert.ToDouble(gunData["RPM"]);
            int barrelCount = Convert.ToInt32(gunData["BarrelCount"]);
            
            // 砲のカテゴリに応じて計算を調整
            string category = gunData["Category"].ToString();
            double categoryMultiplier = 1.0;
            
            switch (category)
            {
                case "SMLG": // 小口径砲
                    categoryMultiplier = 0.8;
                    break;
                case "SMMG": // 中口径砲
                    categoryMultiplier = 1.0;
                    break;
                case "SMHG": // 大口径砲
                    categoryMultiplier = 1.2;
                    break;
                case "SMSHG": // 超大口径砲
                    categoryMultiplier = 1.5;
                    break;
            }
            
            // 攻撃力計算: (砲弾重量 * 初速 * 毎分発射数 * 砲身数 * カテゴリ補正) / 10000
            return Math.Round((shellWeight * muzzleVelocity * rpm * barrelCount * categoryMultiplier) / 10000, 1);
        }
        
        // Calculate defense value based on gun parameters
        private static double CalculateDefenseValue(Dictionary<string, object> gunData)
        {
            // 防御力は主に対空能力に関連
            double calibre = Convert.ToDouble(gunData["Calibre"]);
            double elevationAngle = Convert.ToDouble(gunData["ElevationAngle"]);
            double rpm = Convert.ToDouble(gunData["RPM"]);
            
            // 口径による補正
            double defenseBase = 0;
            if (calibre <= 12) // 12cm以下は対空に有効
            {
                defenseBase = (rpm * elevationAngle) / 50;
            }
            else if (calibre <= 18) // 18cm以下は中程度の対空能力
            {
                defenseBase = (rpm * elevationAngle) / 100;
            }
            else // それ以上は対空にあまり適さない
            {
                defenseBase = (rpm * elevationAngle) / 200;
            }
            
            return Math.Round(defenseBase, 1);
        }
        
        // Calculate range value based on gun parameters
        private static double CalculateRangeValue(Dictionary<string, object> gunData)
        {
            double calibre = Convert.ToDouble(gunData["Calibre"]);
            double muzzleVelocity = Convert.ToDouble(gunData["MuzzleVelocity"]);
            double elevationAngle = Convert.ToDouble(gunData["ElevationAngle"]);
            
            // 口径タイプに応じた補正
            string calibreType = gunData["CalibreType"].ToString();
            double calibreMultiplier = 1.0;
            
            switch (calibreType)
            {
                case "cm":
                    calibreMultiplier = 1.0;
                    break;
                case "inch":
                    // インチをcmに変換して計算
                    calibre = calibre * 2.54;
                    calibreMultiplier = 1.0;
                    break;
                case "mm":
                    // mmをcmに変換して計算
                    calibre = calibre / 10;
                    calibreMultiplier = 1.0;
                    break;
            }
            
            // 射程計算: (口径 * 初速 * 仰角 * 補正) / 1000
            return Math.Round((calibre * muzzleVelocity * elevationAngle * calibreMultiplier) / 1000, 1);
        }
        
        // Calculate armor piercing value based on gun parameters
        private static double CalculateArmorPiercingValue(Dictionary<string, object> gunData)
        {
            double shellWeight = Convert.ToDouble(gunData["ShellWeight"]);
            double muzzleVelocity = Convert.ToDouble(gunData["MuzzleVelocity"]);
            double calibre = Convert.ToDouble(gunData["Calibre"]);
            
            // 口径タイプに応じた補正
            string calibreType = gunData["CalibreType"].ToString();
            
            switch (calibreType)
            {
                case "inch":
                    calibre = calibre * 2.54; // インチをcmに変換
                    break;
                case "mm":
                    calibre = calibre / 10; // mmをcmに変換
                    break;
            }
            
            // 装甲貫通計算: (砲弾重量 * 初速) / (口径 * 100)
            return Math.Round((shellWeight * muzzleVelocity) / (calibre * 100), 1);
        }
        
        // Calculate build cost value based on gun parameters
        private static double CalculateBuildCostValue(Dictionary<string, object> gunData)
        {
            double turretWeight = Convert.ToDouble(gunData["TurretWeight"]);
            int steel = Convert.ToInt32(gunData["Steel"]);
            int chromium = Convert.ToInt32(gunData["Chromium"]);
            int manpower = Convert.ToInt32(gunData["Manpower"]);
            
            // 開発年による補正（新しい技術ほど高コスト）
            int year = (int)gunData["Year"];
            double yearMultiplier = 1.0 + (year - 1890) / 200.0; // 1890年を基準にして年ごとに上昇
            
            if (year < 1890)
            {
                yearMultiplier = 0.9; // 1890年以前の旧式技術
            }
            
            // 建造コスト計算: 砲塔重量 * (鋼材 + クロム * 2 + 人員 / 10) * 年補正 / 10
            return Math.Round(turretWeight * (steel + chromium * 2 + manpower / 10) * yearMultiplier / 10, 1);
        }
        
        // Determine special abilities based on gun parameters
        private static string DetermineSpecialAbility(Dictionary<string, object> gunData)
        {
            // パラメータに基づいて特殊能力を決定
            double calibre = Convert.ToDouble(gunData["Calibre"]);
            double rpm = Convert.ToDouble(gunData["RPM"]);
            double shellWeight = Convert.ToDouble(gunData["ShellWeight"]);
            double muzzleVelocity = Convert.ToDouble(gunData["MuzzleVelocity"]);
            double elevationAngle = Convert.ToDouble(gunData["ElevationAngle"]);
            int year = (int)gunData["Year"];
            
            List<string> abilities = new List<string>();
            
            // 口径に基づく特性
            if (calibre <= 8 && rpm >= 12)
            {
                abilities.Add("高射撃速度");
            }
            
            if (calibre >= 30)
            {
                abilities.Add("重装甲破壊");
            }
            
            // 仰角に基づく特性
            if (elevationAngle >= 70)
            {
                abilities.Add("対空射撃可能");
            }
            
            // 初速に基づく特性
            if (muzzleVelocity >= 900)
            {
                abilities.Add("高貫通");
            }
            
            // 開発年に基づく特性
            if (year >= 1940)
            {
                abilities.Add("近代的");
            }
            else if (year <= 1900)
            {
                abilities.Add("旧式");
            }
            
            // 特殊能力がない場合
            if (abilities.Count == 0)
            {
                return "なし";
            }
            
            return string.Join(", ", abilities);
        }
    }
}