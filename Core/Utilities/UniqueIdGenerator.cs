using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HOI4NavalModder;

/// <summary>
///     一意のIDを生成するためのユーティリティクラス
/// </summary>
public static class UniqueIdGenerator
{
    private static readonly char[] Base36Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

    /// <summary>
    ///     既存のIDに連番を付けて新しいIDを生成する
    /// </summary>
    /// <param name="existingId">既存のID</param>
    /// <param name="existingIds">現在使用中の全ID一覧</param>
    /// <returns>一意の新しいID</returns>
    public static string GenerateUniqueId(string existingId, IEnumerable<string> existingIds)
    {
        // 既存のIDが "_X" で終わっているかチェック
        var match = Regex.Match(existingId, @"_([0-9A-Z])$");
        string baseId;

        if (match.Success)
            // 既に連番がある場合はベースを抽出
            baseId = existingId.Substring(0, existingId.Length - 2);
        else
            // 連番がない場合は既存のIDがベース
            baseId = existingId;

        // 次の有効な連番を見つける
        for (var i = 0; i < Base36Chars.Length; i++)
        {
            var candidateId = $"{baseId}_{Base36Chars[i]}";
            if (!IdExists(candidateId, existingIds)) return candidateId;
        }

        // すべての可能な連番が使用されている場合、タイムスタンプベースの接尾辞を追加
        return $"{baseId}_{DateTime.Now.ToString("HHmmss")}";
    }

    /// <summary>
    ///     指定したIDが既存のID一覧に存在するかチェック
    /// </summary>
    /// <param name="id">チェックするID</param>
    /// <param name="existingIds">既存のID一覧</param>
    /// <returns>存在する場合はtrue</returns>
    public static bool IdExists(string id, IEnumerable<string> existingIds)
    {
        foreach (var existingId in existingIds)
            if (string.Equals(existingId, id, StringComparison.OrdinalIgnoreCase))
                return true;

        return false;
    }
}