using System;
using System.Text.RegularExpressions;

namespace HOI4NavalModder.Core.Utilities;

/// <summary>
///     魚雷IDの自動生成を処理するクラス
/// </summary>
public static class NavalTorpedoIdGenerator
{
    /// <summary>
    ///     魚雷のIDを自動生成する
    /// </summary>
    /// <param name="category">カテゴリ (SMTP, SMSTP)</param>
    /// <param name="countryTag">国家タグ (JAP, USA, GER など)</param>
    /// <param name="year">開発年</param>
    /// <param name="diameter">直径 (mm)</param>
    /// <returns>生成されたID</returns>
    public static string GenerateTorpedoId(string category, string countryTag, int year, string diameter)
    {
        if (string.IsNullOrEmpty(category))
            throw new ArgumentException("カテゴリは必須です");

        // 国家タグがある場合は挿入、ない場合は省略
        var tagPart = string.IsNullOrEmpty(countryTag) ? "" : $"{countryTag.ToLower()}_";

        // ID形式: category_tag_year_diameter
        return $"{category.ToLower()}_{tagPart}{year}_{diameter}".ToLower();
    }

    /// <summary>
    ///     既存のIDからパラメータを抽出する（編集時に使用）
    /// </summary>
    /// <param name="id">魚雷ID</param>
    /// <param name="category">抽出されるカテゴリ</param>
    /// <param name="countryTag">抽出される国家タグ</param>
    /// <param name="year">抽出される開発年</param>
    /// <param name="diameter">抽出される直径</param>
    /// <returns>パラメータの抽出に成功したかどうか</returns>
    public static bool TryParseTorpedoId(string id, out string category, out string countryTag, out int year, out string diameter)
    {
        category = string.Empty;
        countryTag = string.Empty;
        year = 0;
        diameter = string.Empty;

        if (string.IsNullOrEmpty(id))
            return false;

        try
        {
            // すべて小文字に統一して処理
            id = id.ToLower();
            var parts = id.Split('_');
            
            // 新フォーマット: category_tag_year_diameter
            // 旧フォーマット: category_year_diameter
            // 部品数に基づいてパターンを判断
            if (parts.Length < 3)
                return false;

            // カテゴリ（大文字に戻す）
            category = parts[0].ToUpper();
            
            int yearIndex = 1;
            
            // 国家タグが存在するかチェック (parts[1]が数字でなければタグとみなす)
            if (!int.TryParse(parts[1], out year) && IsLikelyCountryTag(parts[1]))
            {
                countryTag = parts[1].ToUpper();
                yearIndex = 2;
                
                // 次の部分を年として解析
                if (parts.Length <= yearIndex || !int.TryParse(parts[yearIndex], out year))
                    return false;
            }

            // 直径（年の次の部分）
            int diameterIndex = yearIndex + 1;
            if (parts.Length <= diameterIndex)
                return false;
                
            diameter = parts[diameterIndex];

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
}