using System;
using System.Collections.Generic;
using HOI4NavalModder.Core.Models;

namespace HOI4NavalModder.Calculators
{
    /// <summary>
    /// 対空砲の性能計算を行うクラス
    /// </summary>
    public static class AAGunCalculator
    {
        /// <summary>
        /// 対空砲データの処理
        /// </summary>
        /// <param name="rawAAData">生の対空砲データ</param>
        /// <returns>処理済みNavalEquipment</returns>
        public static NavalEquipment AA_Processing(Dictionary<string, object> rawAAData)
        {
            try
            {
                // 基本情報の取得
                var id = rawAAData["Id"].ToString();
                var name = rawAAData["Name"].ToString();
                var category = rawAAData["Category"].ToString();
                var subCategory = rawAAData["SubCategory"].ToString();
                var year = Convert.ToInt32(rawAAData["Year"]);
                var tier = Convert.ToInt32(rawAAData["Tier"]);
                var country = rawAAData.ContainsKey("Country") ? rawAAData["Country"].ToString() : "";
                
                // 対空砲パラメータの取得
                var shellWeight = Convert.ToDouble(rawAAData["ShellWeight"]);
                var muzzleVelocity = Convert.ToDouble(rawAAData["MuzzleVelocity"]);
                var rpm = Convert.ToDouble(rawAAData["RPM"]);
                var calibre = Convert.ToDouble(rawAAData["Calibre"]);
                var calibreType = rawAAData["CalibreType"].ToString();
                var barrelCount = Convert.ToInt32(rawAAData["BarrelCount"]);
                var barrelLength = Convert.ToDouble(rawAAData["BarrelLength"]);
                var elevationAngle = Convert.ToDouble(rawAAData["ElevationAngle"]);
                var maxAltitude = Convert.ToDouble(rawAAData["MaxAltitude"]);
                var turretWeight = Convert.ToDouble(rawAAData["TurretWeight"]);
                var manpower = Convert.ToInt32(rawAAData["Manpower"]);
                
                // リソースの取得
                var steel = rawAAData.ContainsKey("Steel") ? Convert.ToDouble(rawAAData["Steel"]) : 0;
                var chromium = rawAAData.ContainsKey("Chromium") ? Convert.ToDouble(rawAAData["Chromium"]) : 0;
                var tungsten = rawAAData.ContainsKey("Tungsten") ? Convert.ToDouble(rawAAData["Tungsten"]) : 0;
                
                // 特殊機能の取得
                var isAsw = rawAAData.ContainsKey("IsAsw") && Convert.ToBoolean(rawAAData["IsAsw"]);
                var hasAutoAiming = rawAAData.ContainsKey("HasAutoAiming") && Convert.ToBoolean(rawAAData["HasAutoAiming"]);
                var hasProximityFuze = rawAAData.ContainsKey("HasProximityFuze") && Convert.ToBoolean(rawAAData["HasProximityFuze"]);
                var hasRadarGuidance = rawAAData.ContainsKey("HasRadarGuidance") && Convert.ToBoolean(rawAAData["HasRadarGuidance"]);
                var hasStabilizedMount = rawAAData.ContainsKey("HasStabilizedMount") && Convert.ToBoolean(rawAAData["HasStabilizedMount"]);
                var hasRemoteControl = rawAAData.ContainsKey("HasRemoteControl") && Convert.ToBoolean(rawAAData["HasRemoteControl"]);
                
                // 性能計算
                // 口径をmmに変換
                var calibreInMm = ConvertCalibreToMm(calibre, calibreType);
                
                // 技術レベル補正（年代による）
                var techLevelMultiplier = GetTechLevelMultiplier(year);
                
                // 特殊機能による補正係数
                var specialFeaturesMultiplier = 1.0;
                if (hasAutoAiming) specialFeaturesMultiplier *= 1.15;
                if (hasProximityFuze) specialFeaturesMultiplier *= 1.25;
                if (hasRadarGuidance) specialFeaturesMultiplier *= 1.2;
                if (hasStabilizedMount) specialFeaturesMultiplier *= 1.1;
                if (hasRemoteControl) specialFeaturesMultiplier *= 1.15;
                
                // 対空攻撃力計算
                var baseAntiAir = (rpm * shellWeight * muzzleVelocity) / (1000 * calibreInMm) * barrelCount;
                var antiAirValue = baseAntiAir * specialFeaturesMultiplier * techLevelMultiplier;
                
                // 追尾精度計算
                var baseTracking = (rpm / calibreInMm) * (muzzleVelocity / 500) * (60 / elevationAngle);
                var trackingValue = baseTracking * specialFeaturesMultiplier * techLevelMultiplier;
                
                // 有効射高計算
                var effectiveAltitudeBase = maxAltitude * (elevationAngle / 90.0) * (muzzleVelocity / 800.0);
                var effectiveAltitudeValue = effectiveAltitudeBase * specialFeaturesMultiplier;
                
                // 軽砲攻撃力計算（対艦攻撃力）
                var lgAttackBase = (shellWeight * Math.Pow(muzzleVelocity, 2)) / 5000000 * rpm * barrelCount;
                var lgAttackValue = lgAttackBase * (calibreInMm < 75 ? 1.0 : 0.7); // 75mm以上は効率が落ちる
                
                // 装甲貫通値計算
                var armorPiercingValue = lgAttackValue / Math.Pow(calibreInMm / 10, 1.5) * techLevelMultiplier;
                
                // 射程計算
                var rangeValue = (calibreInMm * barrelLength * muzzleVelocity * elevationAngle) / 100000;
                
                // 対潜攻撃力計算
                var subAttackValue = isAsw ? 
                    (rpm / 60) * (shellWeight / 20) * barrelCount * 1.5 * techLevelMultiplier : 0;
                
                // 建造コスト計算
                var buildCostBase = (calibreInMm / 20) + (rpm / 100) + (turretWeight / 5);
                var buildCostSpecialMultiplier = 1.0;
                if (hasAutoAiming) buildCostSpecialMultiplier *= 1.1;
                if (hasProximityFuze) buildCostSpecialMultiplier *= 1.15;
                if (hasRadarGuidance) buildCostSpecialMultiplier *= 1.25;
                if (hasStabilizedMount) buildCostSpecialMultiplier *= 1.1;
                if (hasRemoteControl) buildCostSpecialMultiplier *= 1.2;
                
                var buildCostValue = buildCostBase * buildCostSpecialMultiplier * barrelCount * 0.25;
                
                // 計算結果をデータに追加
                rawAAData["CalculatedAntiAir"] = antiAirValue;
                rawAAData["CalculatedRange"] = rangeValue;
                rawAAData["CalculatedArmorPiercing"] = armorPiercingValue;
                rawAAData["CalculatedBuildCost"] = buildCostValue;
                rawAAData["CalculatedLgAttack"] = lgAttackValue;
                rawAAData["CalculatedTracking"] = trackingValue;
                rawAAData["CalculatedEffectiveAltitude"] = effectiveAltitudeValue;
                rawAAData["CalculatedSubAttack"] = subAttackValue;
                
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
                    Attack = antiAirValue,  // 対空攻撃力を攻撃値として設定
                    Defense = trackingValue,  // 追尾精度を防御値として設定
                    AdditionalProperties = new Dictionary<string, object>(rawAAData)
                };
                
                // 特殊能力の設定
                if (isAsw || hasAutoAiming || hasProximityFuze || hasRadarGuidance || hasStabilizedMount || hasRemoteControl)
                {
                    var specialAbilities = new List<string>();
                    
                    if (isAsw)
                        specialAbilities.Add("対潜攻撃");
                        
                    if (hasAutoAiming)
                        specialAbilities.Add("自動照準");
                        
                    if (hasProximityFuze)
                        specialAbilities.Add("近接信管");
                        
                    if (hasRadarGuidance)
                        specialAbilities.Add("レーダー誘導");
                        
                    if (hasStabilizedMount)
                        specialAbilities.Add("安定化マウント");
                        
                    if (hasRemoteControl)
                        specialAbilities.Add("遠隔操作");
                        
                    equipment.SpecialAbility = string.Join(", ", specialAbilities);
                }
                
                return equipment;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"対空砲計算処理エラー: {ex.Message}");
                
                // エラー時は最低限のデータだけ設定して返す
                return new NavalEquipment
                {
                    Id = rawAAData.ContainsKey("Id") ? rawAAData["Id"].ToString() : "error_id",
                    Name = rawAAData.ContainsKey("Name") ? rawAAData["Name"].ToString() : "エラー発生",
                    Category = rawAAData.ContainsKey("Category") ? rawAAData["Category"].ToString() : "SMAA",
                    Year = 1936,
                    AdditionalProperties = new Dictionary<string, object>
                    {
                        { "Error", ex.Message }
                    }
                };
            }
        }
        
        // 年代による技術レベル補正を取得
        private static double GetTechLevelMultiplier(int year)
        {
            if (year < 1900) return 0.7;
            if (year < 1920) return 0.8;
            if (year < 1935) return 0.9;
            if (year < 1942) return 1.0;
            if (year < 1950) return 1.1;
            if (year < 1960) return 1.2;
            if (year < 1970) return 1.3;
            if (year < 1980) return 1.4;
            if (year < 1990) return 1.5;
            return 1.6; // 1990年以降
        }
        
        // 各種口径単位をmmに変換
        private static double ConvertCalibreToMm(double calibre, string calibreType)
        {
            switch (calibreType.ToLower())
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
    }
}