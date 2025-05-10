using System;
using System.Collections.Generic;
using HOI4NavalModder.Core.Models;

namespace HOI4NavalModder.Calculators;

/// <summary>
///     レーダーの性能計算を行うクラス
/// </summary>
public static class RadarCalculator
{
    /// <summary>
    ///     レーダーデータの処理
    /// </summary>
    /// <param name="rawRadarData">生のレーダーデータ</param>
    /// <returns>処理済みNavalEquipment</returns>
    public static NavalEquipment Radar_Processing(Dictionary<string, object> rawRadarData)
    {
        try
        {
            // 基本情報の取得
            var id = rawRadarData["Id"].ToString();
            var name = rawRadarData["Name"].ToString();
            var category = rawRadarData["Category"].ToString();
            var subCategory = rawRadarData["SubCategory"].ToString();
            var year = Convert.ToInt32(rawRadarData["Year"]);
            var tier = Convert.ToInt32(rawRadarData["Tier"]);
            var country = rawRadarData.ContainsKey("Country") ? rawRadarData["Country"].ToString() : "";

            // レーダーパラメータの取得
            var frequencyBand = rawRadarData["FrequencyBand"].ToString();
            var powerOutput = Convert.ToDouble(rawRadarData["PowerOutput"]);
            var antennaSize = Convert.ToDouble(rawRadarData["AntennaSize"]);
            var prf = Convert.ToDouble(rawRadarData["Prf"]);
            var pulseWidth = Convert.ToDouble(rawRadarData["PulseWidth"]);
            var weight = Convert.ToDouble(rawRadarData["Weight"]);
            var manpower = Convert.ToInt32(rawRadarData["Manpower"]);

            // リソースの取得
            var steel = rawRadarData.ContainsKey("Steel") ? Convert.ToDouble(rawRadarData["Steel"]) : 0;
            var tungsten = rawRadarData.ContainsKey("Tungsten") ? Convert.ToDouble(rawRadarData["Tungsten"]) : 0;
            var electronics = rawRadarData.ContainsKey("Electronics")
                ? Convert.ToDouble(rawRadarData["Electronics"])
                : 0;

            // 特殊機能の取得
            var is3D = rawRadarData.ContainsKey("Is3D") && Convert.ToBoolean(rawRadarData["Is3D"]);
            var isDigital = rawRadarData.ContainsKey("IsDigital") && Convert.ToBoolean(rawRadarData["IsDigital"]);
            var isDoppler = rawRadarData.ContainsKey("IsDoppler") && Convert.ToBoolean(rawRadarData["IsDoppler"]);
            var isLongRange = rawRadarData.ContainsKey("IsLongRange") && Convert.ToBoolean(rawRadarData["IsLongRange"]);
            var isFireControl = rawRadarData.ContainsKey("IsFireControl") &&
                                Convert.ToBoolean(rawRadarData["IsFireControl"]);
            var isStealth = rawRadarData.ContainsKey("IsStealth") && Convert.ToBoolean(rawRadarData["IsStealth"]);
            var isCompact = rawRadarData.ContainsKey("IsCompact") && Convert.ToBoolean(rawRadarData["IsCompact"]);
            var isAllWeather = rawRadarData.ContainsKey("IsAllWeather") &&
                               Convert.ToBoolean(rawRadarData["IsAllWeather"]);

            // 性能計算
            // 周波数帯の係数
            double airDetectionMultiplier = 0;
            double surfaceDetectionMultiplier = 0;
            double rangeMultiplier = 0;
            double weatherResistanceMultiplier = 0;

            switch (frequencyBand)
            {
                case "HF帯":
                    airDetectionMultiplier = 0.1;
                    surfaceDetectionMultiplier = 0.8;
                    rangeMultiplier = 1.5;
                    weatherResistanceMultiplier = 0.5;
                    break;
                case "VHF帯":
                    airDetectionMultiplier = 0.3;
                    surfaceDetectionMultiplier = 0.9;
                    rangeMultiplier = 1.3;
                    weatherResistanceMultiplier = 0.6;
                    break;
                case "UHF帯":
                    airDetectionMultiplier = 0.5;
                    surfaceDetectionMultiplier = 1.0;
                    rangeMultiplier = 1.2;
                    weatherResistanceMultiplier = 0.7;
                    break;
                case "L帯":
                    airDetectionMultiplier = 0.7;
                    surfaceDetectionMultiplier = 1.1;
                    rangeMultiplier = 1.0;
                    weatherResistanceMultiplier = 0.8;
                    break;
                case "S帯":
                    airDetectionMultiplier = 0.9;
                    surfaceDetectionMultiplier = 1.0;
                    rangeMultiplier = 0.9;
                    weatherResistanceMultiplier = 0.8;
                    break;
                case "C帯":
                    airDetectionMultiplier = 1.0;
                    surfaceDetectionMultiplier = 0.9;
                    rangeMultiplier = 0.8;
                    weatherResistanceMultiplier = 0.7;
                    break;
                case "X帯":
                    airDetectionMultiplier = 1.2;
                    surfaceDetectionMultiplier = 0.8;
                    rangeMultiplier = 0.7;
                    weatherResistanceMultiplier = 0.5;
                    break;
                case "Ku帯":
                    airDetectionMultiplier = 1.4;
                    surfaceDetectionMultiplier = 0.6;
                    rangeMultiplier = 0.6;
                    weatherResistanceMultiplier = 0.3;
                    break;
                default:
                    airDetectionMultiplier = 0.7;
                    surfaceDetectionMultiplier = 1.0;
                    rangeMultiplier = 1.0;
                    weatherResistanceMultiplier = 0.8;
                    break;
            }

            // 基本探知力計算
            // レーダー方程式に基づく単純化した計算: 
            // 探知力 ∝ (出力 × アンテナサイズ) / (パルス幅 × PRF)
            var baseDetectionPower = Math.Sqrt(powerOutput * antennaSize) / Math.Sqrt(pulseWidth * prf / 1000);

            // 年代による技術補正
            var techModifier = 1.0;
            if (year < 1930)
                techModifier = 0.5;
            else if (year < 1940)
                techModifier = 0.8;
            else if (year < 1950)
                techModifier = 1.0;
            else if (year < 1960)
                techModifier = 1.2;
            else if (year < 1970)
                techModifier = 1.5;
            else
                techModifier = 2.0;

            if (isDigital)
                techModifier *= 1.5; // デジタル信号処理ボーナス

            // 特殊機能による補正
            var specialModifier = 1.0;

            if (is3D)
                airDetectionMultiplier *= 1.3; // 3D探知は空中目標に特に効果的

            if (isDoppler)
                specialModifier *= 1.2; // ドップラー効果で探知精度向上

            if (isLongRange)
                rangeMultiplier *= 1.5; // 長距離探知ボーナス

            if (isFireControl)
                specialModifier *= 0.8; // 射撃管制は探知力を犠牲に

            if (isStealth)
                specialModifier *= 0.7; // ステルス設計は出力を抑える

            if (isCompact)
                specialModifier *= 0.8; // 小型化は性能を犠牲に

            if (isAllWeather)
                weatherResistanceMultiplier *= 2.0; // 全天候対応

            // 最終探知力計算
            var surfaceDetection = baseDetectionPower * surfaceDetectionMultiplier * techModifier * specialModifier;
            var airDetection = baseDetectionPower * airDetectionMultiplier * techModifier * specialModifier;

            // 探知範囲計算 (km) - 簡易的なレーダー方程式ベース
            var detectionRange = 15 * Math.Sqrt(powerOutput) * Math.Sqrt(antennaSize) * rangeMultiplier * techModifier;

            // 射撃管制能力計算
            var fireControl = baseDetectionPower * 0.5 * techModifier;

            if (isFireControl)
                fireControl *= 2.0; // 射撃管制特化型

            if (isDoppler)
                fireControl *= 1.3; // ドップラーがあると射撃精度向上

            // 建造コスト計算
            var buildCost = weight * 0.004 + powerOutput * 0.05 + antennaSize * 0.1;

            // 特殊機能によるコスト補正
            if (isDigital)
                buildCost *= 1.2;

            if (is3D)
                buildCost *= 1.2;

            if (isDoppler)
                buildCost *= 1.3;

            if (isFireControl)
                buildCost *= 1.4;

            if (isStealth)
                buildCost *= 1.5;

            if (isCompact)
                buildCost *= 0.8;

            if (isAllWeather)
                buildCost *= 1.3;

            // 信頼性計算（0.0～1.0）
            var reliability = 0.7 + techModifier * 0.2;

            if (isDigital)
                reliability -= 0.1; // 複雑なデジタル回路は故障しやすい

            if (isCompact)
                reliability -= 0.1; // 小型化は故障リスク増加

            if (isAllWeather)
                reliability += 0.1; // 全天候対応は堅牢

            // 天候抵抗値を信頼性に加味
            reliability *= 0.8 + weatherResistanceMultiplier * 0.2;

            // 最大1.0に制限
            reliability = Math.Min(reliability, 1.0);
            // 最小0.1に制限
            reliability = Math.Max(reliability, 0.1);

            // 可視度ペナルティ計算
            var visibilityPenalty = powerOutput * 0.2 + antennaSize * 2.0;

            if (isStealth)
                visibilityPenalty *= 0.5; // ステルス設計

            if (isCompact)
                visibilityPenalty *= 0.8; // 小型化は目立ちにくい

            // 計算結果をデータに追加
            rawRadarData["CalculatedSurfaceDetection"] = surfaceDetection;
            rawRadarData["CalculatedAirDetection"] = airDetection;
            rawRadarData["CalculatedDetectionRange"] = detectionRange;
            rawRadarData["CalculatedFireControl"] = fireControl;
            rawRadarData["CalculatedBuildCost"] = buildCost;
            rawRadarData["CalculatedReliability"] = reliability;
            rawRadarData["CalculatedVisibilityPenalty"] = visibilityPenalty;

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
                // 探知力を防御値として設定（水上艦探知と空中機探知の平均値）
                Defense = (surfaceDetection + airDetection) / 2,
                // 射撃管制能力を攻撃値として設定
                Attack = fireControl,
                // パラメータとデータをそのままコピー
                AdditionalProperties = new Dictionary<string, object>(rawRadarData)
            };

            // 特殊能力の設定
            if (is3D || isDigital || isDoppler || isLongRange || isFireControl || isStealth || isCompact ||
                isAllWeather)
            {
                var specialAbilities = new List<string>();

                if (is3D)
                    specialAbilities.Add("3D探知");

                if (isDigital)
                    specialAbilities.Add("デジタル処理");

                if (isDoppler)
                    specialAbilities.Add("ドップラー機能");

                if (isLongRange)
                    specialAbilities.Add("長距離探知");

                if (isFireControl)
                    specialAbilities.Add("射撃管制");

                if (isStealth)
                    specialAbilities.Add("低電波放射");

                if (isCompact)
                    specialAbilities.Add("小型化設計");

                if (isAllWeather)
                    specialAbilities.Add("全天候対応");

                equipment.SpecialAbility = string.Join(", ", specialAbilities);
            }

            return equipment;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"レーダー計算処理エラー: {ex.Message}");

            // エラー時は最低限のデータだけ設定して返す
            return new NavalEquipment
            {
                Id = rawRadarData.ContainsKey("Id") ? rawRadarData["Id"].ToString() : "error_id",
                Name = rawRadarData.ContainsKey("Name") ? rawRadarData["Name"].ToString() : "エラー発生",
                Category = rawRadarData.ContainsKey("Category") ? rawRadarData["Category"].ToString() : "SMLR",
                Year = 1936,
                AdditionalProperties = new Dictionary<string, object>
                {
                    { "Error", ex.Message }
                }
            };
        }
    }
}