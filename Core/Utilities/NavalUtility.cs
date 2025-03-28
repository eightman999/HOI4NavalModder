using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using HOI4NavalModder.Calculators;

namespace HOI4NavalModder.Core.Utilities;

/// <summary>
/// 海軍関連の共通ユーティリティ機能を提供するクラス
/// </summary>
public static class NavalUtility
{
    /// <summary>
    /// 開発年からティア（開発世代）を計算するメソッド
    /// </summary>
    /// <param name="year">開発年</param>
    /// <returns>対応するティア値</returns>
    public static int GetTierFromYear(int year)
    {
        // 年に最も近いティアを返す
        if (year <= 1890) return 0;
        if (year <= 1895) return 1;
        if (year <= 1900) return 2;
        if (year <= 1905) return 3;
        if (year <= 1910) return 4;
        if (year <= 1915) return 5;
        if (year <= 1920) return 6;
        if (year <= 1925) return 7;
        if (year <= 1930) return 8;
        if (year <= 1935) return 9;
        if (year <= 1940) return 10;
        if (year <= 1945) return 11;
        if (year <= 1950) return 12;
        if (year <= 1955) return 13;
        if (year <= 1960) return 14;
        if (year <= 1965) return 15;
        if (year <= 1970) return 16;
        if (year <= 1975) return 17;
        if (year <= 1980) return 18;
        if (year <= 1985) return 19;
        if (year <= 1990) return 20;
        if (year <= 1995) return 21;
        if (year <= 2000) return 22;

        return 23; // 2000年以降
    }

    /// <summary>
    /// 各種口径単位をmmに変換
    /// </summary>
    /// <param name="calibre">口径の値</param>
    /// <param name="calibreType">口径の単位（cm/inch/mm）</param>
    /// <returns>mm単位での口径</returns>
    public static double ConvertCalibreToMm(double calibre, string calibreType)
    {
        switch (calibreType)
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
    
    /// <summary>
    /// 砲身長を口径倍数で計算する
    /// </summary>
    /// <param name="barrelLength">砲身の長さ (m)</param>
    /// <param name="calibre">口径 (cm, mm, inch)</param>
    /// <param name="caliberUnit">口径単位</param>
    /// <returns>口径倍数 (L/xx)</returns>
    public static double CalculateBarrelLengthMultiple(double barrelLength, double calibre, string caliberUnit)
    {
        // 口径をメートルに変換
        double caliberInMeters;

        switch (caliberUnit.ToLower())
        {
            case "cm":
                caliberInMeters = calibre / 100.0; // cm → m
                break;
            case "mm":
                caliberInMeters = calibre / 1000.0; // mm → m
                break;
            case "inch":
                caliberInMeters = calibre * 0.0254; // inch → m
                break;
            default:
                throw new ArgumentException("無効な口径単位です");
        }

        // 砲身長 ÷ 口径 = 口径倍数
        return barrelLength / caliberInMeters;
    }

    /// <summary>
    /// JSON要素から安全にint値を取得
    /// </summary>
    /// <param name="element">JsonElement</param>
    /// <returns>int値、取得できない場合は0</returns>
    public static int GetIntFromJsonElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value)) return value;
        return 0; // デフォルト値
    }

    /// <summary>
    /// JSON要素から安全に文字列を取得
    /// </summary>
    /// <param name="element">JsonElement</param>
    /// <returns>文字列値、取得できない場合は空文字</returns>
    public static string GetStringFromJsonElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String) return element.GetString();
        return string.Empty; // デフォルト値
    }

    /// <summary>
    /// Dictionary から安全に double 値を取得
    /// </summary>
    /// <param name="data">データディクショナリ</param>
    /// <param name="key">キー</param>
    /// <returns>取得した値、存在しない場合は0</returns>
    public static decimal GetDoubleValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return 0;

        try
        {
            if (data[key] is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number) return (decimal)jsonElement.GetDouble();

                if (jsonElement.ValueKind == JsonValueKind.String &&
                    decimal.TryParse(jsonElement.GetString(), out var result))
                    return result;
            }

            return Convert.ToDecimal(data[key]);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Dictionary から安全に int 値を取得
    /// </summary>
    /// <param name="data">データディクショナリ</param>
    /// <param name="key">キー</param>
    /// <returns>取得した値、存在しない場合は0</returns>
    public static decimal GetIntValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return 0;

        try
        {
            if (data[key] is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.Number) return jsonElement.GetInt32();

                if (jsonElement.ValueKind == JsonValueKind.String &&
                    int.TryParse(jsonElement.GetString(), out var result))
                    return result;
            }

            return Convert.ToInt32(data[key]);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Dictionary から安全に decimal 値を取得
    /// </summary>
    /// <param name="data">データディクショナリ</param>
    /// <param name="key">キー</param>
    /// <returns>取得した値、存在しない場合は0</returns>
    public static decimal GetDecimalValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return 0;

        try
        {
            if (data[key] is JsonElement jsonElement)
                if (jsonElement.ValueKind == JsonValueKind.Number)
                    return (decimal)jsonElement.GetDouble();

            return Convert.ToDecimal(data[key]);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Dictionary から安全に String 値を取得
    /// </summary>
    /// <param name="data">データディクショナリ</param>
    /// <param name="key">キー</param>
    /// <returns>取得した値、存在しない場合は空文字</returns>
    public static string GetStringValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return string.Empty;

        try
        {
            if (data[key] is JsonElement jsonElement)
                if (jsonElement.ValueKind == JsonValueKind.String)
                    return jsonElement.GetString();

            return data[key].ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Dictionary から安全に boolean 値を取得
    /// </summary>
    /// <param name="data">データディクショナリ</param>
    /// <param name="key">キー</param>
    /// <returns>取得した値、存在しない場合はfalse</returns>
    public static bool GetBooleanValue(Dictionary<string, object> data, string key)
    {
        if (!data.ContainsKey(key)) return false;

        try
        {
            if (data[key] is JsonElement jsonElement)
            {
                if (jsonElement.ValueKind == JsonValueKind.True)
                    return true;
                if (jsonElement.ValueKind == JsonValueKind.False)
                    return false;
                if (jsonElement.ValueKind == JsonValueKind.String &&
                    bool.TryParse(jsonElement.GetString(), out var result))
                    return result;
            }

            return Convert.ToBoolean(data[key]);
        }
        catch
        {
            return false;
        }
    }
}