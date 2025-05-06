using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Math = System.Math;
using Regex = System.Text.RegularExpressions.Regex;

namespace HOI4NavalModder.Core.Utilities
{
    /// <summary>
    /// 対空砲のID生成を行うユーティリティクラス
    /// </summary>
    public static class AAGunIdGenerator
    {
        /// <summary>
        /// 対空砲のIDを生成する
        /// </summary>
        /// <param name="category">カテゴリ（SMAA/SMHAA）</param>
        /// <param name="countryTag">国家タグ（JAPなど）</param>
        /// <param name="year">開発年</param>
        /// <param name="calibre">口径</param>
        /// <param name="calibreType">口径単位（mm/cm/inch）</param>
        /// <param name="barrelLength">砲身長（口径比L）</param>
        /// <returns>生成されたID</returns>
        public static string GenerateGunId(string category, string countryTag, int year, double calibre, string calibreType, double barrelLength)
        {
            // カテゴリをチェック（デフォルトはSMAA）
            string cat = string.IsNullOrEmpty(category) ? "smaa" : category.ToLower();
            
            // 国家タグ処理（空の場合は非表示、それ以外は小文字化）
            string country = string.IsNullOrEmpty(countryTag) ? "" : "_" + countryTag.ToLower();
            
            // 口径をmmに変換して整数化
            int calibreMm;
            switch (calibreType.ToLower())
            {
                case "cm":
                    calibreMm = (int)Math.Round(calibre * 10);
                    break;
                case "inch":
                    calibreMm = (int)Math.Round(calibre * 25.4);
                    break;
                default: // mm
                    calibreMm = (int)Math.Round(calibre);
                    break;
            }
            
            // 砲身長を整数化
            int barrelLengthInt = (int)Math.Round(barrelLength);
            
            // 最終的なID形式: smaa_countryTag_50mm_l60
            return $"{cat}{country}_{calibreMm}mm_l{barrelLengthInt}";
        }
        
        /// <summary>
        /// 対空砲IDを解析し、各コンポーネントを取得する
        /// </summary>
        /// <param name="id">解析するID</param>
        /// <param name="category">カテゴリ</param>
        /// <param name="countryTag">国家タグ</param>
        /// <param name="year">開発年</param>
        /// <param name="calibre">口径</param>
        /// <param name="calibreType">口径単位</param>
        /// <param name="barrelLength">砲身長</param>
        /// <returns>解析に成功したかどうか</returns>
        public static bool TryParseGunId(string id, out string category, out string countryTag, 
            out int year, out double calibre, out string calibreType, out int barrelLength)
        {
            // デフォルト値を設定
            category = "SMAA";
            countryTag = "";
            year = 1936;
            calibre = 40;
            calibreType = "mm";
            barrelLength = 45;
            
            if (string.IsNullOrEmpty(id))
                return false;
                
            try
            {
                // IDを小文字に変換
                string lowerId = id.ToLower();
                
                // カテゴリ部分を取得
                if (lowerId.StartsWith("smaa"))
                    category = "SMAA";
                else if (lowerId.StartsWith("smhaa"))
                    category = "SMHAA";
                else
                    return false; // サポートされていないカテゴリ
                
                // 国家タグを抽出 (例: smaa_jap_40mm_l60)
                var countryMatch = Regex.Match(lowerId, @"^sm[h]?aa_([a-z]{3})_");
                if (countryMatch.Success)
                    countryTag = countryMatch.Groups[1].Value.ToUpper();
                
                // 口径を抽出 (例: 40mm)
                var calibreMatch = Regex.Match(lowerId, @"_(\d+)(mm|cm|inch)");
                if (calibreMatch.Success)
                {
                    calibre = double.Parse(calibreMatch.Groups[1].Value);
                    calibreType = calibreMatch.Groups[2].Value;
                    
                    // 単位に基づいて値を変換
                    if (calibreType == "cm")
                        calibre /= 10; // mmをcmに変換
                    else if (calibreType == "inch")
                        calibre /= 25.4; // mmをインチに変換
                }
                
                // 砲身長を抽出 (例: l60)
                var barrelLengthMatch = Regex.Match(lowerId, @"_l(\d+)");
                if (barrelLengthMatch.Success)
                    barrelLength = int.Parse(barrelLengthMatch.Groups[1].Value);
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}