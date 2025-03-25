using System;
using System.Text.RegularExpressions;

namespace HOI4NavalModder
{
    /// <summary>
    /// 砲IDの自動生成を処理するクラス
    /// </summary>
    public static class NavalGunIdGenerator
    {
        /// <summary>
        /// 砲のIDを自動生成する
        /// </summary>
        /// <param name="category">カテゴリ (SMLG, SMMG, SMHG, SMSHG)</param>
        /// <param name="year">開発年</param>
        /// <param name="caliber">口径</param>
        /// <param name="caliberUnit">口径単位 (cm, mm, inch)</param>
        /// <param name="barrelLength">砲身長（口径倍数）</param>
        /// <returns>生成されたID</returns>
        public static string GenerateGunId(string category, int year, double caliber, string caliberUnit, double barrelLength)
        {
            if (string.IsNullOrEmpty(category))
                throw new ArgumentException("カテゴリは必須です");

            // 砲身長は口径倍数（L/xx）を整数に丸める
            int roundedBarrelLength = (int)Math.Round(barrelLength);
            
            // 口径をフォーマット（小数点以下がない場合は整数として表示）
            string formattedCaliber;
            if (Math.Floor(caliber) == caliber)
                formattedCaliber = ((int)caliber).ToString();
            else
                formattedCaliber = caliber.ToString("0.0").Replace(".", "p"); // 小数点を p に置き換え
            
            // 口径の単位をフォーマット
            string unitSuffix = caliberUnit.ToLower();
            
            // ID形式: CATEGORY_YEAR_CALIBERunit_Lxx
            return $"{category}_{year}_{formattedCaliber}{unitSuffix}_L{roundedBarrelLength}".ToLower();
        }
        
    /// <summary>
    /// 既存のIDからパラメータを抽出する（編集時に使用）
    /// </summary>
    /// <param name="id">砲ID</param>
    /// <param name="category">抽出されるカテゴリ</param>
    /// <param name="year">抽出される開発年</param>
    /// <param name="caliber">抽出される口径</param>
    /// <param name="caliberUnit">抽出される口径単位
    /// /// <param name="barrelLength">抽出される砲身長</param> 
        /// <returns>パラメータの抽出に成功したかどうか</returns>
        public static bool TryParseGunId(string id, out string category, out int year, out double caliber, out string caliberUnit, out int barrelLength)
        {
            category = string.Empty;
            year = 0;
            caliber = 0;
            caliberUnit = string.Empty;
            barrelLength = 0;
            
            if (string.IsNullOrEmpty(id))
                return false;
                
            try
            {
                // ID形式: CATEGORY_YEAR_CALIBERunit_Lxx または category_year_caliberunit_lxx
                // すべて小文字に統一して処理
                id = id.ToLower();
                var parts = id.Split('_');
                if (parts.Length < 4)
                    return false;
                    
                // カテゴリ（大文字に戻す）
                category = parts[0].ToUpper();
                
                // 開発年
                if (!int.TryParse(parts[1], out year))
                    return false;
                    
                // 口径と単位
                string caliberPart = parts[2];
                var match = Regex.Match(caliberPart, @"^(\d+(?:p\d+)?)([a-z]+)$");
                if (!match.Success)
                    return false;
                    
                string caliberStr = match.Groups[1].Value.Replace("p", ".");
                if (!double.TryParse(caliberStr, out caliber))
                    return false;
                    
                caliberUnit = match.Groups[2].Value;
                
                // 砲身長
                var lengthMatch = Regex.Match(parts[3], @"^l(\d+)$");
                if (!lengthMatch.Success || !int.TryParse(lengthMatch.Groups[1].Value, out barrelLength))
                    return false;
                    
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// 砲身長を口径倍数で計算する
        /// </summary>
        /// <param name="barrelLength">砲身の長さ (m)</param>
        /// <param name="caliber">口径 (cm, mm, inch)</param>
        /// <param name="caliberUnit">口径単位</param>
        /// <returns>口径倍数 (L/xx)</returns>
        public static double CalculateBarrelLengthMultiple(double barrelLength, double caliber, string caliberUnit)
        {
            // 口径をメートルに変換
            double caliberInMeters;
            
            switch (caliberUnit.ToLower())
            {
                case "cm":
                    caliberInMeters = caliber / 100.0; // cm → m
                    break;
                case "mm":
                    caliberInMeters = caliber / 1000.0; // mm → m
                    break;
                case "inch":
                    caliberInMeters = caliber * 0.0254; // inch → m
                    break;
                default:
                    throw new ArgumentException("無効な口径単位です");
            }
            
            // 砲身長 ÷ 口径 = 口径倍数
            return barrelLength / caliberInMeters;
        }
    }
}