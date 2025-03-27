using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace HOI4NavalModder.Core.Utilities;

public class NavalUnitLoader
{
    /// <summary>
    ///     国家タグごとの艦隊ファイルの存在を確認します
    /// </summary>
    /// <param name="modPath">MODパス</param>
    /// <param name="vanillaPath">バニラパス</param>
    /// <returns>艦隊を持つ国家タグのリスト</returns>
    public static List<string> GetCountriesWithNavies(string modPath, string vanillaPath)
    {
        var countriesWithNavies = new HashSet<string>();

        // MODのunitsディレクトリをチェック
        if (!string.IsNullOrEmpty(modPath))
        {
            var unitsPath = Path.Combine(modPath, "history", "units");
            if (Directory.Exists(unitsPath))
                foreach (var file in Directory.GetFiles(unitsPath, "*.txt"))
                {
                    var fileName = Path.GetFileName(file);
                    var match = Regex.Match(fileName, @"^([A-Z0-9]{3,})_");
                    if (match.Success)
                    {
                        var tag = match.Groups[1].Value;
                        countriesWithNavies.Add(tag);
                    }
                }
        }

        // バニラのunitsディレクトリをチェック
        if (!string.IsNullOrEmpty(vanillaPath))
        {
            var unitsPath = Path.Combine(vanillaPath, "history", "units");
            if (Directory.Exists(unitsPath))
                foreach (var file in Directory.GetFiles(unitsPath, "*.txt"))
                {
                    var fileName = Path.GetFileName(file);
                    var match = Regex.Match(fileName, @"^([A-Z0-9]{3,})_");
                    if (match.Success)
                    {
                        var tag = match.Groups[1].Value;
                        countriesWithNavies.Add(tag);
                    }
                }
        }

        return countriesWithNavies.ToList();
    }

    /// <summary>
    ///     国家の艦隊ファイルを読み込みます
    /// </summary>
    /// <param name="countryTag">国家タグ</param>
    /// <param name="modPath">MODパス</param>
    /// <param name="vanillaPath">バニラパス</param>
    /// <returns>読み込んだ艦隊リスト</returns>
    public static List<NavalUnit> LoadNavalUnits(string countryTag, string modPath, string vanillaPath)
    {
        var navalUnits = new List<NavalUnit>();
        string filePath = null;

        // MODから艦隊ファイルを検索
        if (!string.IsNullOrEmpty(modPath))
        {
            var unitsPath = Path.Combine(modPath, "history", "units");
            if (Directory.Exists(unitsPath))
            {
                // タグから始まるファイルを検索
                var files = Directory.GetFiles(unitsPath, $"{countryTag}_*.txt");
                if (files.Length > 0) filePath = files[0]; // 最初に見つかったファイルを使用
            }
        }

        // バニラから艦隊ファイルを検索（MODになければ）
        if (filePath == null && !string.IsNullOrEmpty(vanillaPath))
        {
            var unitsPath = Path.Combine(vanillaPath, "history", "units");
            if (Directory.Exists(unitsPath))
            {
                var files = Directory.GetFiles(unitsPath, $"{countryTag}_*.txt");
                if (files.Length > 0) filePath = files[0];
            }
        }

        // ファイルが見つかった場合、解析してNavalUnitを作成
        if (filePath != null && File.Exists(filePath))
        {
            var content = File.ReadAllText(filePath);
            navalUnits = ParseNavalUnitFile(content);
        }

        return navalUnits;
    }

    /// <summary>
    ///     艦隊ファイルの内容を解析します
    /// </summary>
    private static List<NavalUnit> ParseNavalUnitFile(string content)
    {
        var navalUnits = new List<NavalUnit>();

        try
        {
            // 'units = {' と '}' の間のコンテンツを抽出
            var unitsMatch = Regex.Match(content, @"units\s*=\s*{(.+?)}\s*instant_effect", RegexOptions.Singleline);
            if (unitsMatch.Success)
            {
                var unitsContent = unitsMatch.Groups[1].Value;

                // 各フリートを抽出
                var fleetMatches = Regex.Matches(unitsContent, @"fleet\s*=\s*{(.+?)}\s*}", RegexOptions.Singleline);
                foreach (Match fleetMatch in fleetMatches)
                {
                    var fleetContent = fleetMatch.Groups[1].Value;
                    var navalUnit = new NavalUnit();

                    // フリート名を抽出
                    var nameMatch = Regex.Match(fleetContent, @"name\s*=\s*""(.+?)""");
                    if (nameMatch.Success) navalUnit.FleetName = nameMatch.Groups[1].Value;

                    // 基地IDを抽出
                    var baseMatch = Regex.Match(fleetContent, @"naval_base\s*=\s*(\d+)");
                    if (baseMatch.Success) navalUnit.NavalBaseId = int.Parse(baseMatch.Groups[1].Value);

                    // 各タスクフォースを抽出
                    var taskForceMatches = Regex.Matches(fleetContent, @"task_force\s*=\s*{(.+?)}\s*}",
                        RegexOptions.Singleline);
                    foreach (Match taskForceMatch in taskForceMatches)
                    {
                        var taskForceContent = taskForceMatch.Groups[1].Value;
                        var taskForce = new TaskForce();

                        // タスクフォース名を抽出
                        var tfNameMatch = Regex.Match(taskForceContent, @"name\s*=\s*""(.+?)""");
                        if (tfNameMatch.Success) taskForce.Name = tfNameMatch.Groups[1].Value;

                        // 配置場所を抽出
                        var locationMatch = Regex.Match(taskForceContent, @"location\s*=\s*(\d+)");
                        if (locationMatch.Success) taskForce.LocationId = int.Parse(locationMatch.Groups[1].Value);

                        // 各艦船を抽出
                        var shipMatches = Regex.Matches(taskForceContent, @"ship\s*=\s*{(.+?)}\s*}",
                            RegexOptions.Singleline);
                        foreach (Match shipMatch in shipMatches)
                        {
                            var shipContent = shipMatch.Groups[1].Value;
                            var ship = new Ship();

                            // 艦船名を抽出
                            var shipNameMatch = Regex.Match(shipContent, @"name\s*=\s*""(.+?)""");
                            if (shipNameMatch.Success) ship.Name = shipNameMatch.Groups[1].Value;

                            // 艦種を抽出
                            var defMatch = Regex.Match(shipContent, @"definition\s*=\s*(\w+)");
                            if (defMatch.Success) ship.Definition = defMatch.Groups[1].Value;

                            // 装備を抽出
                            var equipMatch = Regex.Match(shipContent, @"equipment\s*=\s*{(.+?)}\s*}",
                                RegexOptions.Singleline);
                            if (equipMatch.Success)
                            {
                                var equipContent = equipMatch.Groups[1].Value;

                                // 艦船タイプを抽出
                                var typeMatch = Regex.Match(equipContent, @"(\w+)\s*=");
                                if (typeMatch.Success) ship.Equipment = typeMatch.Groups[1].Value;

                                // バージョン名を抽出
                                var versionMatch = Regex.Match(equipContent, @"version_name\s*=\s*""(.+?)""");
                                if (versionMatch.Success) ship.VersionName = versionMatch.Groups[1].Value;
                            }

                            // Pride of the Fleetフラグを確認
                            ship.IsPrideOfFleet = shipContent.Contains("pride_of_the_fleet = yes");

                            taskForce.Ships.Add(ship);
                        }

                        navalUnit.TaskForces.Add(taskForce);
                    }

                    navalUnits.Add(navalUnit);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"艦隊ファイル解析エラー: {ex.Message}");
        }

        return navalUnits;
    }

    public class NavalUnit
    {
        public string FleetName { get; set; }
        public int NavalBaseId { get; set; }
        public List<TaskForce> TaskForces { get; set; } = new();
    }

    public class TaskForce
    {
        public string Name { get; set; }
        public int LocationId { get; set; }
        public List<Ship> Ships { get; set; } = new();
    }

    public class Ship
    {
        public string Name { get; set; }
        public string Definition { get; set; }
        public string Equipment { get; set; }
        public string VersionName { get; set; }
        public bool IsPrideOfFleet { get; set; }
    }
}