using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;

namespace HOI4NavalModder
{
    /// <summary>
    /// HOI4の国家情報を読み込み・管理するためのクラス
    /// </summary>
    public class CountryListManager
    {
        // 国家情報
        public class CountryInfo
        {
            public string Tag { get; set; }
            public string Name { get; set; }
            public string FlagPath { get; set; }
            public Bitmap FlagImage { get; set; }
            public bool IsSelected { get; set; }
        }

        private readonly string _modPath;
        private readonly string _vanillaPath;
        private bool _hasReplaceTags;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="modPath">MODのパス</param>
        /// <param name="vanillaPath">バニラのパス</param>
        public CountryListManager(string modPath, string vanillaPath)
        {
            _modPath = modPath;
            _vanillaPath = vanillaPath;
            _hasReplaceTags = false;
        }

        /// <summary>
        /// 国家リストを取得する
        /// </summary>
        /// <param name="showAllCountries">すべての国家を表示するかどうか</param>
        /// <returns>国家リスト</returns>
        public async Task<List<CountryInfo>> GetCountriesAsync(bool showAllCountries)
        {
            return await Task.Run(() => {
                try
                {
                    // MODパスのチェック - バニラパスがあれば国家データ読み込みを続行
                    if (string.IsNullOrEmpty(_modPath) && string.IsNullOrEmpty(_vanillaPath))
                    {
                        Console.WriteLine("MODパスまたはバニラパスが設定されていません。");
                        return new List<CountryInfo>();
                    }

                    List<string> countryTags = new List<string>();
                    Dictionary<string, string> tagDescriptions = new Dictionary<string, string>();

                    // MODのdescriptor.modを確認（replace_path設定の確認）
                    CheckReplaceTags();

                    // 国家タグの収集
                    CollectCountryTags(countryTags, tagDescriptions);

                    // 国名の取得
                    Dictionary<string, string> countryNames = CollectCountryNames();

                    // 国家リストを生成
                    var countryList = CreateCountryList(countryTags, countryNames, tagDescriptions, showAllCountries);

                    // 名前でソート
                    return countryList.OrderBy(c => c.Name).ToList();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"国家データ読み込みエラー: {ex.Message}");
                    return new List<CountryInfo>();
                }
            });
        }

        /// <summary>
        /// MODのdescriptor.modで replace_path="common/country_tags" の設定があるか確認
        /// </summary>
        private void CheckReplaceTags()
        {
            _hasReplaceTags = false;
            
            if (!string.IsNullOrEmpty(_modPath))
            {
                string descriptorPath = Path.Combine(_modPath, "descriptor.mod");
                if (File.Exists(descriptorPath))
                {
                    string[] descriptorLines = File.ReadAllLines(descriptorPath);
                    _hasReplaceTags = descriptorLines.Any(line => 
                        line.Contains("replace_path=\"common/country_tags\""));
                }
            }
        }

        /// <summary>
        /// 国家タグを収集する
        /// </summary>
        private void CollectCountryTags(List<string> countryTags, Dictionary<string, string> tagDescriptions)
        {
            // MODから国家タグを取得
            if (!string.IsNullOrEmpty(_modPath))
            {
                string modTagsPath = Path.Combine(_modPath, "common", "country_tags");
                if (Directory.Exists(modTagsPath))
                {
                    foreach (var file in Directory.GetFiles(modTagsPath, "*.txt"))
                    {
                        CollectCountryTagsFromFile(file, countryTags, tagDescriptions);
                    }
                }
            }

            // バニラからも国家タグを取得 (MODが置き換えてない場合または_modPath がない場合)
            if ((!_hasReplaceTags || string.IsNullOrEmpty(_modPath)) && !string.IsNullOrEmpty(_vanillaPath))
            {
                string vanillaTagsPath = Path.Combine(_vanillaPath, "common", "country_tags");
                if (Directory.Exists(vanillaTagsPath))
                {
                    foreach (var file in Directory.GetFiles(vanillaTagsPath, "*.txt"))
                    {
                        CollectCountryTagsFromFile(file, countryTags, tagDescriptions);
                    }
                }
            }
        }

        /// <summary>
        /// ファイルから国家タグを読み取る
        /// </summary>
        private void CollectCountryTagsFromFile(string filePath, List<string> tags, Dictionary<string, string> descriptions)
        {
            try
            {
                string[] lines = File.ReadAllLines(filePath);
                foreach (var line in lines)
                {
                    // コメントを除去
                    string trimmedLine = line.Split('#')[0].Trim();
                    if (string.IsNullOrEmpty(trimmedLine))
                    {
                        continue;
                    }
                    
                    // 複数のパターンを試す
                    // パターン1: 標準的な定義 - IBE = "countries/Western Europe.txt"
                    var match = Regex.Match(trimmedLine, @"([A-Za-z0-9_]{2,})\s*=\s*[""'](.+?)[""']");
                    
                    // パターン2: 引用符なしの定義 - IBE = countries/Western Europe.txt
                    if (!match.Success)
                    {
                        match = Regex.Match(trimmedLine, @"([A-Za-z0-9_]{2,})\s*=\s*([^\s#]+)");
                    }
                    
                    // パターン3: 動的定義 - dynamic_tags = { IBE FRA GER }
                    if (!match.Success && (trimmedLine.Contains("dynamic_tags") || trimmedLine.Contains("allowed_tags")))
                    {
                        var dynamicTagMatches = Regex.Matches(trimmedLine, @"([A-Za-z0-9_]{2,})");
                        foreach (Match dynamicMatch in dynamicTagMatches)
                        {
                            string potentialTag = dynamicMatch.Groups[1].Value;
                            // 一般的なキーワードを除外
                            if (potentialTag != "dynamic_tags" && potentialTag != "allowed_tags" && !tags.Contains(potentialTag))
                            {
                                // [0-9A-Z][0-9][0-9] パターンの動的TAGを除外
                                if (!Regex.IsMatch(potentialTag, @"^[0-9A-Z][0-9][0-9]$"))
                                {
                                    tags.Add(potentialTag);
                                    descriptions[potentialTag] = "Dynamic Tag";
                                }
                            }
                        }
                        continue;
                    }
                    
                    if (match.Success)
                    {
                        string tag = match.Groups[1].Value.ToUpper(); // TAGは通常大文字で標準化

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
                Console.WriteLine($"国家タグファイル読み込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 国家名のローカライズを収集する
        /// </summary>
        private Dictionary<string, string> CollectCountryNames()
        {
            Dictionary<string, string> countryNames = new Dictionary<string, string>();
            
            // IDE設定から言語設定を取得
            bool isJapanese = GetLanguagePreference();
            string localeSuffix = isJapanese ? "japanese" : "english";
            
            // MODからのローカライズ取得
            if (!string.IsNullOrEmpty(_modPath))
            {
                string locPath = Path.Combine(_modPath, "localisation", localeSuffix);
                if (Directory.Exists(locPath))
                {
                    CollectLocalizedCountryNames(locPath, countryNames);
                }
                else
                {
                    // 下位フォルダなしのケース
                    locPath = Path.Combine(_modPath, "localisation");
                    if (Directory.Exists(locPath))
                    {
                        CollectLocalizedCountryNames(locPath, countryNames);
                    }
                }
            }
            
            // バニラからのローカライズ取得
            if (!string.IsNullOrEmpty(_vanillaPath))
            {
                string locPath = Path.Combine(_vanillaPath, "localisation", localeSuffix);
                if (Directory.Exists(locPath))
                {
                    CollectLocalizedCountryNames(locPath, countryNames);
                }
            }

            return countryNames;
        }

        /// <summary>
        /// アプリの言語設定を取得する
        /// </summary>
        private bool GetLanguagePreference()
        {
            bool isJapanese = true; // デフォルトは日本語
            string ideSettingsPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HOI4NavalModder",
                "idesettings.json");
                
            if (File.Exists(ideSettingsPath))
            {
                try
                {
                    var json = File.ReadAllText(ideSettingsPath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<IDESettings>(json);
                    if (settings != null)
                    {
                        isJapanese = settings.IsJapanese;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"言語設定読み込みエラー: {ex.Message}");
                }
            }

            return isJapanese;
        }

        /// <summary>
        /// ディレクトリからローカライズされた国家名を収集する
        /// </summary>
        private void CollectLocalizedCountryNames(string localizationDir, Dictionary<string, string> countryNames)
        {
            try
            {
                // YMLファイルを再帰的に検索
                foreach (var file in Directory.GetFiles(localizationDir, "*.yml", SearchOption.AllDirectories))
                {
                    string[] lines = File.ReadAllLines(file);
                    foreach (var line in lines)
                    {
                        // 国名のローカライズキーを検索（例: IBE:0 "イベリア連合"）
                        var match = Regex.Match(line, @"([A-Z0-9_]{2,}):0 *""(.+?)""");
                        if (match.Success)
                        {
                            string tag = match.Groups[1].Value;
                            string name = match.Groups[2].Value;
                            
                            // 特殊文字やフォーマット指定子を削除
                            name = Regex.Replace(name, @"\$.*?\$", "");
                            name = Regex.Replace(name, @"\§[A-Za-z]", "");
                            name = name.Trim();
                            
                            if (!string.IsNullOrEmpty(name) && !countryNames.ContainsKey(tag))
                            {
                                countryNames[tag] = name;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ローカライゼーション読み込みエラー: {ex.Message}");
            }
        }

        /// <summary>
        /// 国家リストを生成する
        /// </summary>
        private List<CountryInfo> CreateCountryList(
            List<string> countryTags, 
            Dictionary<string, string> countryNames, 
            Dictionary<string, string> tagDescriptions,
            bool showAllCountries)
        {
            var countryList = new List<CountryInfo>();

            foreach (var tag in countryTags)
            {
                string flagPath = null;
                Bitmap flagImage = null;
                
                // MODから国旗を検索
                if (!string.IsNullOrEmpty(_modPath))
                {
                    string modFlagPath = Path.Combine(_modPath, "gfx", "flags", $"{tag}.tga");
                    if (File.Exists(modFlagPath))
                    {
                        flagPath = modFlagPath;
                        try
                        {
                            // TGAデコーダーを使用して国旗画像を読み込む
                            flagImage = TgaDecoder.LoadFromFile(modFlagPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"MOD国旗読み込みエラー: {ex.Message}");
                        }
                    }
                }
                
                // バニラから国旗を検索（MODになければ）
                if ((flagPath == null || flagImage == null) && !string.IsNullOrEmpty(_vanillaPath))
                {
                    string vanillaFlagPath = Path.Combine(_vanillaPath, "gfx", "flags", $"{tag}.tga");
                    if (File.Exists(vanillaFlagPath))
                    {
                        flagPath = vanillaFlagPath;
                        try
                        {
                            // TGAデコーダーを使用して国旗画像を読み込む
                            flagImage = TgaDecoder.LoadFromFile(vanillaFlagPath);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"バニラ国旗読み込みエラー: {ex.Message}");
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
                    {
                        countryName = match.Groups[1].Value;
                    }
                    else
                    {
                        countryName = desc;
                    }
                }
                else
                {
                    countryName = tag;
                }
                
                // 主要国か判定（3文字以下のタグは主要国と仮定）
                bool isMajorCountry = tag.Length <= 3;
                
                // 全国家表示がオンか、主要国の場合のみ追加
                if (showAllCountries || isMajorCountry)
                {
                    countryList.Add(new CountryInfo
                    {
                        Tag = tag,
                        Name = countryName,
                        FlagPath = flagPath,
                        FlagImage = flagImage,
                        IsSelected = false
                    });
                }
            }

            return countryList;
        }

        /// <summary>
        /// 国旗画像を取得する（非同期処理用）
        /// </summary>
        public async Task<Bitmap> LoadFlagImageAsync(string flagPath)
        {
            return await Task.Run(() => {
                try
                {
                    if (string.IsNullOrEmpty(flagPath) || !File.Exists(flagPath))
                        return null;

                    return TgaDecoder.LoadFromFile(flagPath);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"国旗画像読み込みエラー: {ex.Message}");
                    return null;
                }
            });
        }
    }
}