using System;
using System.Text.RegularExpressions;

namespace HOI4NavalModder.Core.Utilities
{
    /// <summary>
    /// 対空砲のID自動生成ユーティリティクラス
    /// </summary>
    public static class AAGunIdGenerator
    {
        /// <summary>
        /// 対空砲のIDを生成する
        /// </summary>
        /// <param name="categoryId">カテゴリID（SMAA、SMHAAなど）</param>
        /// <param name="countryTag">国家タグ（JAP、USA、GERなど）</param>
        /// <param name="year">開発年</param>
        /// <param name="calibre">口径</param>
        /// <param name="calibreType">口径の単位（mm, cm, inch）</param>
        /// <param name="barrelLength">砲身長（L/口径）</param>
        /// <returns>生成されたID</returns>
        public static string GenerateGunId(string categoryId, string countryTag, int year, double calibre, string calibreType, double barrelLength)
        {
            // カテゴリIDを小文字に変換
            var category = categoryId.ToLower();
            
            // 口径を標準化（mm単位に変換）
            double calibreInMm = calibre;
            switch (calibreType.ToLower())
            {
                case "cm":
                    calibreInMm = calibre * 10;
                    break;
                case "inch":
                    calibreInMm = calibre * 25.4;
                    break;
            }
            
            // 口径文字列を作成（整数部分のみ）
            var calibreStr = ((int)Math.Round(calibreInMm)).ToString();
            
            // 国家タグ部分（存在する場合のみ追加）
            var countryPart = string.IsNullOrEmpty(countryTag) ? "" : "_" + countryTag.ToLower();
            
            // 年部分（下2桁のみ使用）
            var yearStr = (year % 100).ToString("D2");
            
            // 砲身長部分（整数部分のみ）
            var barrelLengthStr = ((int)Math.Round(barrelLength)).ToString();
            
            // 最終的なID形式: [category]_[calibre]mm_l[barrelLength]_[country]_[year]
            return $"{category}_{calibreStr}mm_l{barrelLengthStr}{countryPart}_{yearStr}";
        }
        
        /// <summary>
        /// 対空砲IDをパースして各パラメータを取得する
        /// </summary>
        /// <param name="gunId">対空砲ID</param>
        /// <param name="categoryId">出力：カテゴリID</param>
        /// <param name="countryTag">出力：国家タグ</param>
        /// <param name="year">出力：開発年</param>
        /// <param name="calibre">出力：口径</param>
        /// <param name="calibreType">出力：口径単位</param>
        /// <param name="barrelLength">出力：砲身長</param>
        /// <returns>パース成功したかどうか</returns>
        public static bool TryParseGunId(string gunId, out string categoryId, out string countryTag, out int year, 
            out double calibre, out string calibreType, out double barrelLength)
        {
            // デフォルト値の設定
            categoryId = "";
            countryTag = "";
            year = 1936;
            calibre = 0;
            calibreType = "mm";
            barrelLength = 45;
            
            if (string.IsNullOrEmpty(gunId))
                return false;
            
            try
            {
                // 基本的なパターン: [category]_[calibre]mm_l[barrelLength]_[country]_[year]
                var parts = gunId.Split('_');
                
                if (parts.Length < 2)
                    return false;
                
                // カテゴリの抽出
                categoryId = parts[0].ToUpper();
                
                // 口径の抽出
                var calibrePart = parts[1];
                if (calibrePart.EndsWith("mm"))
                {
                    // mm単位の場合
                    calibreType = "mm";
                    if (double.TryParse(calibrePart.Substring(0, calibrePart.Length - 2), out double parsedCalibre))
                        calibre = parsedCalibre;
                }
                
                // 砲身長の抽出
                if (parts.Length >= 3 && parts[2].StartsWith("l"))
                {
                    var barrelPart = parts[2].Substring(1);
                    if (double.TryParse(barrelPart, out double parsedBarrelLength))
                        barrelLength = parsedBarrelLength;
                }
                
                // 国家タグの抽出（存在する場合）
                if (parts.Length >= 4)
                {
                    // 3番目に国家タグがあるか、4番目に年があるかで判断
                    if (parts[3].Length == 2 && int.TryParse(parts[3], out _))
                    {
                        // 国家タグがなく、年が3番目にある場合
                        if (int.TryParse(parts[3], out int parsedYear))
                            year = 1900 + parsedYear;
                    }
                    else
                    {
                        // 国家タグが3番目にある場合
                        countryTag = parts[3].ToUpper();
                        
                        // 年の抽出（存在する場合）
                        if (parts.Length >= 5 && parts[4].Length == 2 && int.TryParse(parts[4], out int parsedYear))
                            year = 1900 + parsedYear;
                    }
                }
                
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}