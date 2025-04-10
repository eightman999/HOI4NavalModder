using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Win32;

namespace HOI4NavalModder.Core.Models;

/// <summary>
///     アプリケーション設定を管理するクラス
/// </summary>
public class IdeSettingsManager
{
    /// <summary>
    ///     設定ファイルのパス
    /// </summary>
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HOI4NavalModder",
        "idesettings.json");

    /// <summary>
    ///     MOD設定ファイルのパス
    /// </summary>
    private static readonly string ModConfigFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "HOI4NavalModder",
        "modpaths.json");

    /// <summary>
    ///     設定データの取得
    /// </summary>
    /// <returns>IDESettings オブジェクト</returns>
    public static IdeSettings GetSettings()
    {
        try
        {
            // 設定ディレクトリの確認と作成
            var settingsDir = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(settingsDir)) Directory.CreateDirectory(settingsDir);

            // 設定ファイルが存在する場合は読み込み
            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                var settings = JsonSerializer.Deserialize<IdeSettings>(json);

                // NULLチェック
                if (settings == null) settings = new IdeSettings();

                return settings;
            }

            // デフォルト設定を返す
            var defaultSettings = new IdeSettings
            {
                IsJapanese = true
            };

            // ゲームパスの自動検出を試みる
            defaultSettings.GamePath = FindGamePathFromRegistry();
            if (string.IsNullOrEmpty(defaultSettings.GamePath))
                defaultSettings.GamePath = FindGamePathFromCommonLocations();

            return defaultSettings;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定読み込みエラー: {ex.Message}");
            return new IdeSettings();
        }
    }

    /// <summary>
    ///     設定の保存
    /// </summary>
    /// <param name="settings">保存する設定</param>
    /// <returns>保存成功の場合はtrue</returns>
    public static bool SaveSettings(IdeSettings settings)
    {
        try
        {
            var settingsDir = Path.GetDirectoryName(SettingsFilePath);
            if (!Directory.Exists(settingsDir)) Directory.CreateDirectory(settingsDir);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(SettingsFilePath, json);

            Console.WriteLine("設定を保存しました");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定保存エラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     MOD設定の取得
    /// </summary>
    /// <returns>ModConfig オブジェクト</returns>
    public static ModConfig GetModConfig()
    {
        try
        {
            if (File.Exists(ModConfigFilePath))
            {
                var json = File.ReadAllText(ModConfigFilePath);
                var config = JsonSerializer.Deserialize<ModConfig>(json);

                // NULLチェック
                if (config == null) config = new ModConfig();

                return config;
            }

            // デフォルト設定
            var defaultConfig = new ModConfig
            {
                VanillaGamePath = FindGamePathFromRegistry()
            };

            if (string.IsNullOrEmpty(defaultConfig.VanillaGamePath))
                defaultConfig.VanillaGamePath = FindGamePathFromCommonLocations();

            return defaultConfig;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MOD設定読み込みエラー: {ex.Message}");
            return new ModConfig();
        }
    }

    /// <summary>
    ///     MOD設定の保存
    /// </summary>
    /// <param name="config">保存する設定</param>
    /// <returns>保存成功の場合はtrue</returns>
    public static bool SaveModConfig(ModConfig config)
    {
        try
        {
            var settingsDir = Path.GetDirectoryName(ModConfigFilePath);
            if (!Directory.Exists(settingsDir)) Directory.CreateDirectory(settingsDir);

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(ModConfigFilePath, json);

            Console.WriteLine("MOD設定を保存しました");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"MOD設定保存エラー: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    ///     ゲームインストールパスをレジストリから探す
    /// </summary>
    /// <returns>ゲームパスまたは空文字</returns>
    public static string FindGamePathFromRegistry()
    {
        try
        {
            // Steamのインストールパスを取得（64ビット版）
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Wow6432Node\Valve\Steam"))
            {
                if (key != null)
                {
                    var steamPath = key.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        // libraryfolders.vdfからゲームライブラリを探す
                        var libraryFoldersPath = Path.Combine(steamPath, "steamapps", "libraryfolders.vdf");
                        if (File.Exists(libraryFoldersPath))
                        {
                            string[] lines = File.ReadAllLines(libraryFoldersPath);
                            foreach (var line in lines)
                                if (line.Contains("\"path\""))
                                {
                                    var libraryPath = line.Split('"')[3].Replace("\\\\", "\\");
                                    var hoi4Path = Path.Combine(libraryPath, "steamapps", "common",
                                        "Hearts of Iron IV");
                                    if (Directory.Exists(hoi4Path)) return hoi4Path;
                                }
                        }

                        // デフォルトのSteamライブラリを確認
                        var defaultHoi4Path =
                            Path.Combine(steamPath, "steamapps", "common", "Hearts of Iron IV");
                        if (Directory.Exists(defaultHoi4Path)) return defaultHoi4Path;
                    }
                }
            }

            // 32ビット版も確認
            using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Valve\Steam"))
            {
                if (key != null)
                {
                    var steamPath = key.GetValue("InstallPath") as string;
                    if (!string.IsNullOrEmpty(steamPath))
                    {
                        var defaultHoi4Path =
                            Path.Combine(steamPath, "steamapps", "common", "Hearts of Iron IV");
                        if (Directory.Exists(defaultHoi4Path)) return defaultHoi4Path;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"レジストリからのゲームパス取得エラー: {ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>
    ///     一般的なインストール場所からゲームパスを探す
    /// </summary>
    /// <returns>ゲームパスまたは空文字</returns>
    public static string FindGamePathFromCommonLocations()
    {
        // 一般的なインストールパスをチェック
        string[] commonPaths =
        {
            @"C:\Program Files (x86)\Steam\steamapps\common\Hearts of Iron IV",
            @"C:\Program Files\Steam\steamapps\common\Hearts of Iron IV",
            @"D:\Steam\steamapps\common\Hearts of Iron IV",
            @"E:\Steam\steamapps\common\Hearts of Iron IV"
        };

        foreach (var path in commonPaths)
            if (Directory.Exists(path))
                return path;

        return string.Empty;
    }

    /// <summary>
    ///     ゲームパスが有効かどうか確認
    /// </summary>
    /// <param name="path">確認するパス</param>
    /// <returns>有効な場合はtrue</returns>
    public static bool IsValidGamePath(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return false;

        // ゲームフォルダに含まれる特徴的なディレクトリやファイルを確認
        return Directory.Exists(Path.Combine(path, "common")) &&
               Directory.Exists(Path.Combine(path, "gfx")) &&
               File.Exists(Path.Combine(path, "hoi4.exe"));
    }

    /// <summary>
    ///     MODパスが有効かどうか確認
    /// </summary>
    /// <param name="path">確認するパス</param>
    /// <returns>有効な場合はtrue</returns>
    public static bool IsValidModPath(string path)
    {
        if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return false;

        // MODフォルダに含まれる特徴的なファイルを確認
        return File.Exists(Path.Combine(path, "descriptor.mod"));
    }
}

/// <summary>
///     アプリケーション設定クラス
/// </summary>
public class IdeSettings
{
    /// <summary>
    ///     日本語UIを使用するかどうか
    /// </summary>
    public bool IsJapanese { get; set; } = true;

    /// <summary>
    ///     UIテーマ設定
    /// </summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>
    ///     自動保存の有効/無効
    /// </summary>
    public bool AutoSave { get; set; } = true;

    /// <summary>
    ///     自動保存間隔（分）
    /// </summary>
    public int AutoSaveInterval { get; set; } = 5;

    public bool IsDarkTheme { get; set; }
    public string FontFamily { get; set; }
    public object? FontSize { get; set; }
    public bool? IsEquipmentFileIntegrated { get; set; }
    public string GamePath { get; set; }
}

/// <summary>
///     MOD設定クラス
/// </summary>
public class ModConfig
{
    /// <summary>
    ///     HOI4バニラゲームのパス
    /// </summary>
    public string VanillaGamePath { get; set; } = string.Empty;

    /// <summary>
    ///     MODのリスト
    /// </summary>
    public List<ModInfo> Mods { get; set; } = new();

    public string VanillaLogPath { get; set; }
}

/// <summary>
///     MOD情報クラス
/// </summary>
public class ModInfo
{
    /// <summary>
    ///     MODの名前
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    ///     MODのパス
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    ///     アクティブ状態
    /// </summary>
    public bool IsActive { get; set; } = false;

    public string ThumbnailPath { get; set; }
    public string? Version { get; set; }
}