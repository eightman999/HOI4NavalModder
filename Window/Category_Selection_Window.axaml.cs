using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace HOI4NavalModder;

public partial class CategorySelectionWindow : Window
{
    private readonly Dictionary<string, NavalCategory> _categories;
    private readonly ComboBox _categoryComboBox;
    private readonly Dictionary<int, string> _tierYears;
    private readonly ComboBox _yearComboBox;

    public CategorySelectionWindow()
    {
        InitializeComponent();
    }

    public CategorySelectionWindow(Dictionary<string, NavalCategory> categories, Dictionary<int, string> tierYears)
    {
        InitializeComponent();

        _categories = categories;
        _tierYears = tierYears;

        // UI要素を取得
        _categoryComboBox = this.FindControl<ComboBox>("CategoryComboBox");
        _yearComboBox = this.FindControl<ComboBox>("YearComboBox");

        // カテゴリの設定
        foreach (var category in _categories)
            _categoryComboBox.Items.Add(new NavalCategoryItem
                { Id = category.Key, DisplayName = $"{category.Value.Name} ({category.Key})" });

        // 開発年の設定
        foreach (var year in _tierYears)
            _yearComboBox.Items.Add(new NavalYearItem { Tier = year.Key, Year = year.Value });

        // 最初の項目を選択
        if (_categoryComboBox.Items.Count > 0)
            _categoryComboBox.SelectedIndex = 0;

        if (_yearComboBox.Items.Count > 0)
            _yearComboBox.SelectedIndex = 0;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);

#if DEBUG
        this.AttachDevTools();
#endif
    }

    private void OnContinueClick(object sender, RoutedEventArgs e)
    {
        // 選択されたカテゴリとティアを返す
        if (_categoryComboBox.SelectedItem != null && _yearComboBox.SelectedItem != null)
        {
            var categoryItem = _categoryComboBox.SelectedItem as NavalCategoryItem;
            var yearItem = _yearComboBox.SelectedItem as NavalYearItem;

            var result = new CategorySelectionResult
            {
                CategoryId = categoryItem.Id,
                TierId = yearItem.Tier
            };

            Close(result);
        }
        else
        {
            // エラーメッセージを表示
            Console.WriteLine("カテゴリと開発年を選択してください");
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        // キャンセル
        Close();
    }
}

// カテゴリ選択用アイテムクラス
public class NavalCategoryItem
{
    public string Id { get; set; }
    public string DisplayName { get; set; }

    public override string ToString()
    {
        return DisplayName;
    }
}

// 開発年選択用アイテムクラス
public class NavalYearItem
{
    public int Tier { get; set; }
    public string Year { get; set; }

    public override string ToString()
    {
        return Year;
    }
}