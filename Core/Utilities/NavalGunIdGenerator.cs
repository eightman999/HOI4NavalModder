using System;
using System.Text.RegularExpressions;

namespace HOI4NavalModder.Core.Utilities;

/// <summary>
///     砲IDの自動生成を処理するクラス
/// </summary>
public static class NavalGunIdGenerator
{
    /// <summary>
    ///     砲のIDを自動生成する
    /// </summary>
    /// <param name="category">カテゴリ (SMLG, SMMG, SMHG, SMSHG)</param>
    /// <param name="countryTag">国家タグ (JAP, USA, GER など)</param>
    /// <param name="year">開発年</param>
    /// <param name="caliber">口径</param>
    /// <param name="caliberUnit">口径単位 (cm, mm, inch)</param>
    /// <param name="barrelLength">砲身長（口径倍数）</param>
    /// <returns>生成されたID</returns>
    public static string GenerateGunId(string category, string countryTag, int year, double caliber, string caliberUnit,
        double barrelLength)
    {
        if (string.IsNullOrEmpty(category))
            throw new ArgumentException("カテゴリは必須です");

        // 砲身長は口径倍数（L/xx）を整数に丸める
        var roundedBarrelLength = (int)Math.Round(barrelLength);

        // 口径をフォーマット（小数点以下がない場合は整数として表示）
        string formattedCaliber;
        if (Math.Floor(caliber) == caliber)
            formattedCaliber = ((int)caliber).ToString();
        else
            formattedCaliber = caliber.ToString("0.0").Replace(".", "p"); // 小数点を p に置き換え

        // 口径の単位をフォーマット
        var unitSuffix = caliberUnit.ToLower();

        // 国家タグがある場合は挿入、ない場合は省略
        var tagPart = string.IsNullOrEmpty(countryTag) ? "" : $"{countryTag.ToLower()}_";

        // ID形式: CATEGORY_TAG_YEAR_CALIBERunit_Lxx
        return $"{category.ToLower()}_{tagPart}{year}_{formattedCaliber}{unitSuffix}_L{roundedBarrelLength}".ToLower();
    }

    /// <summary>
    ///     既存のIDからパラメータを抽出する（編集時に使用）
    /// </summary>
    /// <param name="id">砲ID</param>
    /// <param name="category">抽出されるカテゴリ</param>
    /// <param name="countryTag">抽出される国家タグ</param>
    /// <param name="year">抽出される開発年</param>
    /// <param name="caliber">抽出される口径</param>
    /// <param name="caliberUnit">抽出される口径単位</param>
    /// <param name="barrelLength">抽出される砲身長</param>
    /// <returns>パラメータの抽出に成功したかどうか</returns>
    public static bool TryParseGunId(string id, out string category, out string countryTag, out int year,
        out double caliber,
        out string caliberUnit, out int barrelLength)
    {
        category = string.Empty;
        countryTag = string.Empty;
        year = 0;
        caliber = 0;
        caliberUnit = string.Empty;
        barrelLength = 0;

        if (string.IsNullOrEmpty(id))
            return false;

        try
        {
            // すべて小文字に統一して処理
            id = id.ToLower();
            var parts = id.Split('_');

            // 新フォーマット: category_tag_year_caliberunit_lxx
            // 旧フォーマット: category_year_caliberunit_lxx
            // 部品数に基づいてパターンを判断
            if (parts.Length < 4)
                return false;

            // カテゴリ（大文字に戻す）
            category = parts[0].ToUpper();

            var yearIndex = 1;

            // 国家タグが存在するかチェック (parts[1]が数字でなければタグとみなす)
            if (!int.TryParse(parts[1], out year) && IsLikelyCountryTag(parts[1]))
            {
                countryTag = parts[1].ToUpper();
                yearIndex = 2;

                // 次の部分を年として解析
                if (parts.Length <= yearIndex || !int.TryParse(parts[yearIndex], out year))
                    return false;
            }

            // 口径と単位（年の次の部分）
            var caliberIndex = yearIndex + 1;
            if (parts.Length <= caliberIndex)
                return false;

            var caliberPart = parts[caliberIndex];
            var match = Regex.Match(caliberPart, @"^(\d+(?:p\d+)?)([a-z]+)$");
            if (!match.Success)
                return false;

            var caliberStr = match.Groups[1].Value.Replace("p", ".");
            if (!double.TryParse(caliberStr, out caliber))
                return false;

            caliberUnit = match.Groups[2].Value;

            // 砲身長（最後の部分）
            var lengthIndex = caliberIndex + 1;
            if (parts.Length <= lengthIndex)
                return false;

            var lengthMatch = Regex.Match(parts[lengthIndex], @"^l(\d+)$");
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
    ///     文字列が国家タグのパターンに一致するか判定する
    /// </summary>
    /// <param name="str">判定する文字列</param>
    /// <returns>国家タグのパターンに一致する場合はtrue</returns>
    private static bool IsLikelyCountryTag(string str)
    {
        if (string.IsNullOrEmpty(str))
            return false;

        // 国家タグは通常3～4文字のアルファベット
        if (str.Length < 2 || str.Length > 5)
            return false;

        // 数字だけの場合はタグではない（年の可能性）
        if (int.TryParse(str, out _))
            return false;

        // アルファベットか確認（一部の特殊文字も許容）
        return Regex.IsMatch(str, @"^[A-Za-z0-9_-]+$");
    }

    /// <summary>
    ///     砲身長を口径倍数で計算する
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