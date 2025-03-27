using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using HOI4NavalModder.Core.Models;
using Microsoft.Win32;

namespace HOI4NavalModder.Core.Utilities;

/// <summary>
///     HOI4の国家情報を読み込み・管理するためのクラス（改良版）
/// </summary>
public class CountryListManager
{
    private bool _hasReplaceTags;

    private string _modPath;
    private bool _modPathProvided; // MODパスが明示的に提供されたかを追跡
    private string _vanillaPath;

    /// <summary>
    ///     コンストラクタ
    /// </summary>
    /// <param name="modPath">MODのパス（null可）</param>
    /// <param name="gamePath">バニラのパス（null可）</param>
    public CountryListManager(string modPath, string gamePath)
    {
        _modPath = modPath;
        _vanillaPath = gamePath;
        _hasReplaceTags = false;
        _modPathProvided = !string.IsNullOrEmpty(modPath); // MODパスが提供されたかどうかを記録

        // 明示的にパスが指定されていなければ、自動的に探索
        if (string.IsNullOrEmpty(_modPath) || string.IsNullOrEmpty(_vanillaPath)) AutoDetectPaths();

        Console.WriteLine(
            $"CountryListManager初期化 - MODパス: {_modPath}, バニラパス: {_vanillaPath}, MODパスが提供: {_modPathProvided}");
    }

    /// <summary>
    ///     必要なパスを自動検出する
    /// </summary>
    private void AutoDetectPaths()
    {
        Console.WriteLine("パスの自動探索を開始します");

        // まずバニラゲームパスをIDESettingsから取得
        if (string.IsNullOrEmpty(_vanillaPath))
        {
            var settingsGamePath = GetGamePathFromSettings();
            if (!string.IsNullOrEmpty(settingsGamePath))
            {
                _vanillaPath = settingsGamePath;
                Console.WriteLine($"設定から取得したゲームパス: {_vanillaPath}");
            }
            else
            {
                // 設定になければレジストリから探索
                _vanillaPath = FindGamePathFromRegistry();
                Console.WriteLine($"レジストリから探索したゲームパス: {_vanillaPath}");

                // それでもなければ一般的なパスを試す
                if (string.IsNullOrEmpty(_vanillaPath))
                {
                    _vanillaPath = FindGamePathFromCommonLocations();
                    Console.WriteLine($"一般的な場所から探索したゲームパス: {_vanillaPath}");
                }
            }
        }

        // MODパスがない場合のみIDESettingsから取得（MODパスが明示的に提供された場合は上書きしない）
        if (string.IsNullOrEmpty(_modPath) && !_modPathProvided)
        {
            _modPath = FindModPath();
            Console.WriteLine($"設定から取得したMODパス: {_modPath}");
        }
    }

    /// <summary>
    ///     ゲームパスを探索する
    /// </summary>
    private string FindGamePathFromCommonLocations()
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
    ///     アプリケーション設定からゲームパスを取得
    /// </summary>
    private string GetGamePathFromSettings()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HOI4NavalModder");

            var settingsPath = Path.Combine(appDataPath, "idesettings.json");

            if (File.Exists(settingsPath))
            {
                var settingsJson = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<ModConfig>(settingsJson);
                if (settings != null && !string.IsNullOrEmpty(settings.VanillaGamePath))
                {
                    // 設定から取得したパスが実際に存在するか確認
                    if (Directory.Exists(settings.VanillaGamePath)) return settings.VanillaGamePath;

                    Console.WriteLine($"設定のゲームパスが存在しません: {settings.VanillaGamePath}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定からのゲームパス取得エラー: {ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>
    ///     レジストリからゲームパスを探索
    /// </summary>
    private string FindGamePathFromRegistry()
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
    ///     MODパスを設定ファイルから取得
    /// </summary>
    private string FindModPath()
    {
        try
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HOI4NavalModder");

            var settingsPath = Path.Combine(appDataPath, "idesettings.json");

            if (File.Exists(settingsPath))
            {
                var settingsJson = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<ModConfig>(settingsJson);

                if (settings != null)
                {
                    var activeMod = settings.Mods?.FirstOrDefault(m => m.IsActive);
                    if (activeMod != null)
                    {
                        Console.WriteLine($"アクティブMOD: {activeMod.Name}");
                        return activeMod.Path;
                    }

                    Console.WriteLine("設定のMODパスが存在しません");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"設定からのMODパス取得エラー: {ex.Message}");
        }

        return string.Empty;
    }

    /// <summary>
    ///     国家リストを取得する
    /// </summary>
    /// <param name="showAllCountries">すべての国家を表示するかどうか</param>
    /// <returns>国家リスト</returns>
    public async Task<List<CountryInfo>> GetCountriesAsync(bool showAllCountries)
    {
        return await Task.Run(() =>
        {
            try
            {
                // パスが設定されているか確認、なければ自動探索
                if (string.IsNullOrEmpty(_modPath) && string.IsNullOrEmpty(_vanillaPath)) AutoDetectPaths();

                // バニラパスが必要（MODだけでは不十分）
                if (string.IsNullOrEmpty(_vanillaPath))
                {
                    Console.WriteLine("バニラゲームパスが設定されていません。デフォルトの国家リストを使用します。");
                    return GetDefaultCountryList();
                }

                List<string> countryTags = new List<string>();
                var tagDescriptions = new Dictionary<string, string>();

                // MODのdescriptor.modを確認（replace_path設定の確認）
                CheckReplaceTags();

                // 国家タグの収集
                CollectCountryTags(countryTags, tagDescriptions);

                // 国名の取得
                var countryNames = CollectCountryNames();

                // 国家リストを生成
                var countryList = CreateCountryList(countryTags, countryNames, tagDescriptions, showAllCountries);

                Console.WriteLine($"国家リスト生成完了: {countryList.Count}件");

                // 名前でソート
                return countryList.OrderBy(c => c.Name).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"国家データ読み込みエラー: {ex.Message}");
                Console.WriteLine($"スタックトレース: {ex.StackTrace}");
                return GetDefaultCountryList();
            }
        });
    }

    /// <summary>
    ///     デフォルトの国家リストを取得
    /// </summary>
    private List<CountryInfo> GetDefaultCountryList()
    {
        Console.WriteLine("デフォルトの国家リストを使用します");
        var defaultList = new List<CountryInfo>
        {
            new() { Tag = "JAP", Name = "日本", IsSelected = false },
            new() { Tag = "USA", Name = "アメリカ", IsSelected = false },
            new() { Tag = "ENG", Name = "イギリス", IsSelected = false },
            new() { Tag = "GER", Name = "ドイツ", IsSelected = false },
            new() { Tag = "SOV", Name = "ソ連", IsSelected = false },
            new() { Tag = "ITA", Name = "イタリア", IsSelected = false },
            new() { Tag = "FRA", Name = "フランス", IsSelected = false },
            new() { Tag = "CHI", Name = "中国", IsSelected = false },
            new() { Tag = "OTH", Name = "その他", IsSelected = false }
        };

        return defaultList;
    }

    /// <summary>
    ///     MODのdescriptor.modで replace_path="common/country_tags" の設定があるか確認
    /// </summary>
    public void CheckReplaceTags()
    {
        _hasReplaceTags = false;

        if (!string.IsNullOrEmpty(_modPath))
        {
            var descriptorPath = Path.Combine(_modPath, "descriptor.mod");
            if (File.Exists(descriptorPath))
                try
                {
                    string[] descriptorLines = File.ReadAllLines(descriptorPath);
                    _hasReplaceTags = descriptorLines.Any(line =>
                        line.Contains("replace_path=\"common/country_tags\""));

                    Console.WriteLine($"MODのreplace_path設定: {_hasReplaceTags}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"descriptor.mod読み込みエラー: {ex.Message}");
                }
            else
                Console.WriteLine($"descriptor.modファイルが見つかりません: {descriptorPath}");
        }
    }

    /// <summary>
    ///     国家タグを収集する
    /// </summary>
    public void CollectCountryTags(List<string> countryTags, Dictionary<string, string> tagDescriptions)
    {
        // MODから国家タグを取得（優先）
        var modTagsCollected = false;
        if (!string.IsNullOrEmpty(_modPath))
        {
            var modTagsPath = Path.Combine(_modPath, "common", "country_tags");
            if (Directory.Exists(modTagsPath))
            {
                Console.WriteLine($"MODから国家タグを収集: {modTagsPath}");
                try
                {
                    foreach (var file in Directory.GetFiles(modTagsPath, "*.txt"))
                        CollectCountryTagsFromFile(file, countryTags, tagDescriptions);

                    modTagsCollected = true;
                    Console.WriteLine($"MODから {countryTags.Count} 件のタグを収集しました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"MODからのタグ収集エラー: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"MODに国家タグディレクトリがありません: {modTagsPath}");
            }
        }

        // バニラからも国家タグを取得 (MODが置き換えてない場合または_modPath がない場合)
        if ((!_hasReplaceTags || !modTagsCollected) && !string.IsNullOrEmpty(_vanillaPath))
        {
            var vanillaTagsPath = Path.Combine(_vanillaPath, "common", "country_tags");
            if (Directory.Exists(vanillaTagsPath))
            {
                Console.WriteLine($"バニラから国家タグを収集: {vanillaTagsPath}");
                try
                {
                    foreach (var file in Directory.GetFiles(vanillaTagsPath, "*.txt"))
                        CollectCountryTagsFromFile(file, countryTags, tagDescriptions);

                    Console.WriteLine($"バニラから追加で収集し、合計 {countryTags.Count} 件のタグとなりました");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"バニラからのタグ収集エラー: {ex.Message}");
                }
            }
            else
            {
                Console.WriteLine($"バニラに国家タグディレクトリがありません: {vanillaTagsPath}");
            }
        }
    }

    /// <summary>
    ///     ファイルから国家タグを読み取る
    /// </summary>
    private void CollectCountryTagsFromFile(string filePath, List<string> tags,
        Dictionary<string, string> descriptions)
    {
        try
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                // コメントを除去
                var trimmedLine = line.Split('#')[0].Trim();
                if (string.IsNullOrEmpty(trimmedLine)) continue;

                // 複数のパターンを試す
                // パターン1: 標準的な定義 - IBE = "countries/Western Europe.txt"
                var match = Regex.Match(trimmedLine, @"([A-Za-z0-9_]{2,})\s*=\s*[""'](.+?)[""']");

                // パターン2: 引用符なしの定義 - IBE = countries/Western Europe.txt
                if (!match.Success) match = Regex.Match(trimmedLine, @"([A-Za-z0-9_]{2,})\s*=\s*([^\s#]+)");

                // パターン3: 動的定義 - dynamic_tags = { IBE FRA GER }
                if (!match.Success &&
                    (trimmedLine.Contains("dynamic_tags") || trimmedLine.Contains("allowed_tags")))
                {
                    var dynamicTagMatches = Regex.Matches(trimmedLine, @"([A-Za-z0-9_]{2,})");
                    foreach (Match dynamicMatch in dynamicTagMatches)
                    {
                        var potentialTag = dynamicMatch.Groups[1].Value;
                        // 一般的なキーワードを除外
                        if (potentialTag != "dynamic_tags" && potentialTag != "allowed_tags" &&
                            !tags.Contains(potentialTag))
                            // [0-9A-Z][0-9][0-9] パターンの動的TAGを除外
                            if (!Regex.IsMatch(potentialTag, @"^[0-9A-Z][0-9][0-9]$"))
                            {
                                tags.Add(potentialTag);
                                descriptions[potentialTag] = "Dynamic Tag";
                            }
                    }

                    continue;
                }

                if (match.Success)
                {
                    var tag = match.Groups[1].Value.ToUpper(); // TAGは通常大文字で標準化

                    // [0-9A-Z][0-9][0-9] パターンのTAGを除外
                    if (!Regex.IsMatch(tag, @"^[0-9A-Z][0-9][0-9]$") && !tags.Contains(tag))
                    {
                        tags.Add(tag);
                        descriptions[tag] = match.Groups[2].Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"国家タグファイル読み込みエラー ({Path.GetFileName(filePath)}): {ex.Message}");
        }
    }

    /// <summary>
    ///     国家名のローカライズを収集する
    /// </summary>
    private Dictionary<string, string> CollectCountryNames()
    {
        Dictionary<string, string> countryNames = new Dictionary<string, string>();

        // IDE設定から言語設定を取得
        var isJapanese = GetLanguagePreference();
        var localeSuffix = isJapanese ? "japanese" : "english";

        // MODからのローカライズ取得（優先）
        var modLocalizationCollected = false;
        if (!string.IsNullOrEmpty(_modPath))
        {
            var locPath = Path.Combine(_modPath, "localisation", localeSuffix);
            if (Directory.Exists(locPath))
            {
                Console.WriteLine($"MODから言語固有のローカライズを収集: {locPath}");
                CollectLocalizedCountryNames(locPath, countryNames);
                modLocalizationCollected = true;
            }
            else
            {
                // 下位フォルダなしのケース
                locPath = Path.Combine(_modPath, "localisation");
                if (Directory.Exists(locPath))
                {
                    Console.WriteLine($"MODから一般的なローカライズを収集: {locPath}");
                    CollectLocalizedCountryNames(locPath, countryNames);
                    modLocalizationCollected = true;
                }
                else
                {
                    Console.WriteLine($"MODにlocalisationディレクトリがありません: {locPath}");
                }
            }
        }

        // バニラからのローカライズ取得（MODになければ）
        if (!string.IsNullOrEmpty(_vanillaPath))
        {
            var locPath = Path.Combine(_vanillaPath, "localisation", localeSuffix);
            if (Directory.Exists(locPath))
            {
                Console.WriteLine($"バニラからローカライズを収集: {locPath}");
                CollectLocalizedCountryNames(locPath, countryNames);
            }
            else
            {
                Console.WriteLine($"バニラに言語固有のローカライズディレクトリがありません: {locPath}");

                // 一般的なローカライズディレクトリも確認
                locPath = Path.Combine(_vanillaPath, "localisation");
                if (Directory.Exists(locPath))
                {
                    Console.WriteLine($"バニラから一般的なローカライズを収集: {locPath}");
                    CollectLocalizedCountryNames(locPath, countryNames);
                }
                else
                {
                    Console.WriteLine($"バニラにlocalisationディレクトリがありません: {locPath}");
                }
            }
        }

        Console.WriteLine($"収集したローカライズ数: {countryNames.Count}件");
        return countryNames;
    }

    /// <summary>
    ///     アプリの言語設定を取得する
    /// </summary>
    private bool GetLanguagePreference()
    {
        var isJapanese = true; // デフォルトは日本語
        var ideSettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HOI4NavalModder",
            "idesettings.json");

        if (File.Exists(ideSettingsPath))
            try
            {
                var json = File.ReadAllText(ideSettingsPath);
                var settings = JsonSerializer.Deserialize<IdeSettings>(json);
                if (settings != null)
                {
                    isJapanese = settings.IsJapanese;
                    Console.WriteLine($"言語設定: {(isJapanese ? "日本語" : "英語")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"言語設定読み込みエラー: {ex.Message}");
            }
        else
            Console.WriteLine("言語設定ファイルが見つかりません、デフォルトの日本語を使用します");

        return isJapanese;
    }

    /// <summary>
    ///     ディレクトリからローカライズされた国家名を収集する
    /// </summary>
    private void CollectLocalizedCountryNames(string localizationDir, Dictionary<string, string> countryNames)
    {
        try
        {
            var fileCount = 0;
            var nameCount = 0;

            // YMLファイルを再帰的に検索
            foreach (var file in Directory.GetFiles(localizationDir, "*.yml", SearchOption.AllDirectories))
            {
                fileCount++;
                try
                {
                    string[] lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        // 国名のローカライズキーを検索（例: IBE:0 "イベリア連合"）
                        var match = Regex.Match(line, @"([A-Z0-9_]{2,}):0 *""(.+?)""");
                        if (match.Success)
                        {
                            var tag = match.Groups[1].Value;
                            var name = match.Groups[2].Value;

                            // 特殊文字やフォーマット指定子を削除
                            name = Regex.Replace(name, @"\$.*?\$", "");
                            name = Regex.Replace(name, @"\§[A-Za-z]", "");
                            name = name.Trim();

                            if (!string.IsNullOrEmpty(name) && !countryNames.ContainsKey(tag))
                            {
                                countryNames[tag] = name;
                                nameCount++;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"YMLファイル読み込みエラー ({Path.GetFileName(file)}): {ex.Message}");
                }
            }

            Console.WriteLine($"処理したYMLファイル: {fileCount}件、抽出した国家名: {nameCount}件");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ローカライゼーション読み込みエラー: {ex.Message}");
        }
    }

    /// <summary>
    ///     国家リストを生成する
    /// </summary>
    private List<CountryInfo> CreateCountryList(
        List<string> countryTags,
        Dictionary<string, string> countryNames,
        Dictionary<string, string> tagDescriptions,
        bool showAllCountries)
    {
        var countryList = new List<CountryInfo>();
        var flagsLoaded = 0;

        foreach (var tag in countryTags)
        {
            string flagPath = null;
            Bitmap flagImage = null;

            // MODから国旗を検索（優先）
            if (!string.IsNullOrEmpty(_modPath))
            {
                var modFlagPath = Path.Combine(_modPath, "gfx", "flags", $"{tag}.tga");
                if (File.Exists(modFlagPath))
                {
                    flagPath = modFlagPath;
                    try
                    {
                        // TGAデコーダーを使用して国旗画像を読み込む
                        flagImage = TgaDecoder.LoadFromFile(modFlagPath);
                        if (flagImage != null) flagsLoaded++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"MOD国旗読み込みエラー ({tag}): {ex.Message}");
                    }
                }
            }

            // バニラから国旗を検索（MODになければ）
            if ((flagPath == null || flagImage == null) && !string.IsNullOrEmpty(_vanillaPath))
            {
                var vanillaFlagPath = Path.Combine(_vanillaPath, "gfx", "flags", $"{tag}.tga");
                if (File.Exists(vanillaFlagPath))
                {
                    flagPath = vanillaFlagPath;
                    try
                    {
                        // TGAデコーダーを使用して国旗画像を読み込む
                        flagImage = TgaDecoder.LoadFromFile(vanillaFlagPath);
                        if (flagImage != null) flagsLoaded++;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"バニラ国旗読み込みエラー ({tag}): {ex.Message}");
                    }
                }
            }

            // 国名がない場合はタグ説明から代替テキストを作成
            string countryName = null;
            if (countryNames.TryGetValue(tag, out var name))
            {
                countryName = name;
            }
            else if (tagDescriptions.TryGetValue(tag, out var desc))
            {
                // ファイルパスから国名を推測（"countries/Western Europe.txt" → "Western Europe"）
                var match = Regex.Match(desc, @"countries/([^.]+)\.txt");
                if (match.Success)
                    countryName = match.Groups[1].Value;
                else
                    countryName = desc;
            }
            else
            {
                countryName = tag;
            }

            // 主要国か判定（3文字以下のタグは主要国と仮定）
            var isMajorCountry = tag.Length <= 3;

            // 全国家表示がオンか、主要国の場合のみ追加
            if (showAllCountries || isMajorCountry)
                countryList.Add(new CountryInfo
                {
                    Tag = tag,
                    Name = countryName,
                    FlagPath = flagPath,
                    FlagImage = flagImage,
                    IsSelected = false
                });
        }

        Console.WriteLine($"国旗の読み込み: 成功={flagsLoaded}件/全体={countryList.Count}件");
        return countryList;
    }

    /// <summary>
    ///     国旗画像を取得する（非同期処理用）
    /// </summary>
    public async Task<Bitmap> LoadFlagImageAsync(string flagPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrEmpty(flagPath) || !File.Exists(flagPath))
                {
                    Console.WriteLine($"国旗画像ファイルが見つかりません: {flagPath}");
                    return null;
                }

                return TgaDecoder.LoadFromFile(flagPath);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"国旗画像読み込みエラー: {ex.Message}");
                return null;
            }
        });
    }

    /// <summary>
    ///     パスを手動で設定する（動作中に変更が必要な場合に使用）
    /// </summary>
    public void SetPaths(string modPath, string vanillaPath)
    {
        if (!string.IsNullOrEmpty(modPath))
        {
            _modPath = modPath;
            _modPathProvided = true;
            Console.WriteLine($"MODパスを手動設定: {_modPath}");
        }

        if (!string.IsNullOrEmpty(vanillaPath))
        {
            _vanillaPath = vanillaPath;
            Console.WriteLine($"バニラパスを手動設定: {_vanillaPath}");
        }
    }

    // 国家情報
    public class CountryInfo
    {
        public string Tag { get; set; }
        public string Name { get; set; }
        public string FlagPath { get; set; }
        public Bitmap FlagImage { get; set; }
        public bool IsSelected { get; set; }
    }
}