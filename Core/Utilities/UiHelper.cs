using System;
using System.Collections.Generic;
using Avalonia.Controls;
using System.Text.RegularExpressions;

namespace HOI4NavalModder.Core.Utilities;

/// <summary>
/// UI操作のためのヘルパーメソッドを提供するクラス
/// </summary>
public static class UiHelper
{
    /// <summary>
    /// ComboBoxの項目を選択するヘルパーメソッド
    /// </summary>
    /// <param name="comboBox">対象のComboBox</param>
    /// <param name="propertyName">比較するプロパティ名（nullの場合は文字列比較）</param>
    /// <param name="value">選択する値</param>
    public static void SelectComboBoxItem(ComboBox comboBox, string propertyName, object value)
    {
        if (comboBox.Items.Count == 0) return;

        if (propertyName == null)
        {
            // 単純な値比較
            for (var i = 0; i < comboBox.Items.Count; i++)
                if (comboBox.Items[i].ToString() == value.ToString())
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
        }
        else
        {
            // プロパティによる比較
            for (var i = 0; i < comboBox.Items.Count; i++)
            {
                var item = comboBox.Items[i];
                var prop = item.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    var propValue = prop.GetValue(item);
                    if (propValue.ToString() == value.ToString())
                    {
                        comboBox.SelectedIndex = i;
                        return;
                    }
                }
            }
        }

        // 一致するものがなければ最初の項目を選択
        comboBox.SelectedIndex = 0;
    }

    /// <summary>
    /// NumericUpDownの値を安全に設定するヘルパーメソッド
    /// </summary>
    /// <param name="numericUpDown">対象のNumericUpDown</param>
    /// <param name="value">設定する値</param>
    public static void SetNumericValue(NumericUpDown numericUpDown, decimal value)
    {
        if (value >= numericUpDown.Minimum && value <= numericUpDown.Maximum) 
            numericUpDown.Value = value;
    }

    /// <summary>
    /// エラーメッセージを表示する（現在はコンソール出力のみ）
    /// </summary>
    /// <param name="message">表示するメッセージ</param>
    public static void ShowError(string message)
    {
        // エラーメッセージを表示（実際の実装ではダイアログを表示する）
        Console.WriteLine($"エラー: {message}");
    }
    
    /// <summary>
    /// 国家タグや国名に基づいてコンボボックスの選択を設定
    /// </summary>
    /// <param name="countryComboBox">国家選択用コンボボックス</param>
    /// <param name="countryValue">国家タグ（JAP, USAなど）または国名</param>
    /// <param name="countryInfoList">国家情報リスト</param>
    public static void SetCountrySelection(ComboBox countryComboBox, string countryValue, List<CountryListManager.CountryInfo> countryInfoList)
    {
        if (string.IsNullOrEmpty(countryValue))
        {
            // 値が空の場合は「未設定」を選択
            countryComboBox.SelectedIndex = 0;
            return;
        }

        Console.WriteLine($"国家選択: {countryValue}");

        // 1. 完全一致（タグが括弧内にある場合）
        for (var i = 0; i < countryComboBox.Items.Count; i++)
        {
            var item = countryComboBox.Items[i].ToString();
            if (item.EndsWith($"({countryValue})"))
            {
                countryComboBox.SelectedIndex = i;
                Console.WriteLine($"タグの完全一致で選択: {item}");
                return;
            }
        }

        // 2. 国家タグが直接マッチする場合
        foreach (var country in countryInfoList)
            if (country.Tag.Equals(countryValue, StringComparison.OrdinalIgnoreCase))
                for (var i = 0; i < countryComboBox.Items.Count; i++)
                {
                    var item = countryComboBox.Items[i].ToString();
                    if (item.Contains($"({country.Tag})"))
                    {
                        countryComboBox.SelectedIndex = i;
                        Console.WriteLine($"タグの直接マッチで選択: {item}");
                        return;
                    }
                }

        // 3. 国名が直接マッチする場合
        foreach (var country in countryInfoList)
            if (country.Name.Equals(countryValue, StringComparison.OrdinalIgnoreCase))
                for (var i = 0; i < countryComboBox.Items.Count; i++)
                {
                    var item = countryComboBox.Items[i].ToString();
                    if (item.StartsWith(country.Name))
                    {
                        countryComboBox.SelectedIndex = i;
                        Console.WriteLine($"国名の直接マッチで選択: {item}");
                        return;
                    }
                }

        // 4. 部分一致を試みる
        for (var i = 0; i < countryComboBox.Items.Count; i++)
        {
            var item = countryComboBox.Items[i].ToString();
            if (item.Contains(countryValue, StringComparison.OrdinalIgnoreCase))
            {
                countryComboBox.SelectedIndex = i;
                Console.WriteLine($"部分一致で選択: {item}");
                return;
            }
        }

        // 5. 一致するものがなかった場合、mainとか一般的な接頭辞か判定
        var lowerValue = countryValue.ToLower();
        if (lowerValue == "main" || lowerValue == "generic" || lowerValue == "default")
        {
            // 未設定（デフォルト）を選択
            countryComboBox.SelectedIndex = 0;
            Console.WriteLine("一般的な値のため「未設定」を選択");
            return;
        }

        // それでも見つからない場合は「その他」を探す
        for (var i = 0; i < countryComboBox.Items.Count; i++)
        {
            var item = countryComboBox.Items[i].ToString();
            if (item.Contains("その他") || item.Contains("(OTH)"))
            {
                countryComboBox.SelectedIndex = i;
                Console.WriteLine("一致しないため「その他」を選択");
                return;
            }
        }

        // 最後の手段として最初の項目を選択
        Console.WriteLine($"一致する国家が見つからないため「未設定」を選択: {countryValue}");
        countryComboBox.SelectedIndex = 0;
    }
}