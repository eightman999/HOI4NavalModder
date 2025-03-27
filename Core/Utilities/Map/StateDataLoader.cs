using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Avalonia;
using HOI4NavalModder.Core.Models;

namespace HOI4NavalModder.Core.Utilities.Map;

public class StateDataLoader
{
    private readonly Dictionary<int, List<BuildingPosition>> _buildingPositions;

    private readonly string _modPath;
    private readonly string _vanillaPath;

    public StateDataLoader(string modPath, string vanillaPath)
    {
        _modPath = modPath;
        _vanillaPath = vanillaPath;
        _buildingPositions = new Dictionary<int, List<BuildingPosition>>();
    }

    // 建物位置データを読み込む
    public async Task LoadBuildingPositions()
    {
        _buildingPositions.Clear();

        // MODとバニラのbuildings.txtパスを取得
        var modBuildingsPath = Path.Combine(_modPath, "map", "buildings.txt");
        var vanillaBuildingsPath = Path.Combine(_vanillaPath, "map", "buildings.txt");

        // 実際に存在するファイルパスを取得
        var buildingsPath = File.Exists(modBuildingsPath) ? modBuildingsPath : vanillaBuildingsPath;

        if (!File.Exists(buildingsPath)) throw new FileNotFoundException("buildings.txtファイルが見つかりません");

        // ファイルを読み込み
        var content = await File.ReadAllTextAsync(buildingsPath);

        // 建物位置データを解析
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("//") ||
                trimmedLine.StartsWith("#")) continue; // コメント行やスキップ行

            var parts = trimmedLine.Split(';').Select(p => p.Trim()).ToArray();
            if (parts.Length >= 7)
                try
                {
                    var stateId = int.Parse(parts[0]);
                    var buildingId = parts[1];
                    var xPos = double.Parse(parts[2]);
                    var yPos = double.Parse(parts[3]);
                    var zPos = double.Parse(parts[4]);
                    var rotation = double.Parse(parts[5]);
                    var adjacentSea = int.Parse(parts[6]);

                    var buildingPos = new BuildingPosition
                    {
                        StateId = stateId,
                        BuildingId = buildingId,
                        XPosition = xPos,
                        YPosition = yPos,
                        ZPosition = zPos,
                        Rotation = rotation,
                        AdjacentSeaProvince = adjacentSea
                    };

                    if (!_buildingPositions.ContainsKey(stateId))
                        _buildingPositions[stateId] = new List<BuildingPosition>();

                    _buildingPositions[stateId].Add(buildingPos);
                }
                catch (Exception)
                {
                    // パース失敗はスキップして次の行へ
                }
        }
    }

    // ステートIDに対応する建物位置を取得
    public BuildingPosition GetNavalBasePosition(int stateId)
    {
        if (_buildingPositions.TryGetValue(stateId, out var buildings))
            // naval_baseのビルディングを検索
            return buildings.FirstOrDefault(b => b.BuildingId.Equals("naval_base", StringComparison.OrdinalIgnoreCase));

        return null;
    }

    // ステートファイルからNaval Baseデータを読み込む
    public async Task<List<NavalBase>> LoadNavalBases()
    {
        var navalBases = new List<NavalBase>();

        // ステートファイルのパス
        var modStatesPath = Path.Combine(_modPath, "history", "states");
        var vanillaStatesPath = Path.Combine(_vanillaPath, "history", "states");

        // MODとバニラのステートファイルを検索
        List<string> stateFiles = new List<string>();

        // MODにステートファイルがあれば追加
        if (Directory.Exists(modStatesPath))
            stateFiles.AddRange(Directory.GetFiles(modStatesPath, "*.txt", SearchOption.AllDirectories));

        // バニラのステートファイルも追加（MODで上書きされていないもの）
        if (Directory.Exists(vanillaStatesPath))
        {
            // MODですでに定義されているステートIDを取得
            var modStateIds = new HashSet<int>();
            foreach (var stateFile in stateFiles)
            {
                var stateId = ExtractStateId(stateFile);
                if (stateId.HasValue) modStateIds.Add(stateId.Value);
            }

            // バニラのステートファイルからMODにないものを追加
            foreach (var vanillaStateFile in
                     Directory.GetFiles(vanillaStatesPath, "*.txt", SearchOption.AllDirectories))
            {
                var stateId = ExtractStateId(vanillaStateFile);
                if (stateId.HasValue && !modStateIds.Contains(stateId.Value)) stateFiles.Add(vanillaStateFile);
            }
        }

        // 各ステートファイルから港湾施設を検索
        foreach (var stateFile in stateFiles)
        {
            var content = await File.ReadAllTextAsync(stateFile);
            var stateNavalBases = ParseNavalBases(content);
            navalBases.AddRange(stateNavalBases);
        }

        return navalBases;
    }

    // ステートファイルからステートIDを抽出
    private int? ExtractStateId(string filePath)
    {
        try
        {
            // ファイル名からIDを抽出（例: 100-State.txt -> 100）
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var match = Regex.Match(fileName, @"^(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out var id)) return id;

            // ファイル内容からIDを抽出
            var content = File.ReadAllText(filePath);
            match = Regex.Match(content, @"id\s*=\s*(\d+)");
            if (match.Success && int.TryParse(match.Groups[1].Value, out id)) return id;
        }
        catch
        {
            // エラーは無視して次へ
        }

        return null;
    }

    // ステートファイルから港湾施設情報を抽出
    private List<NavalBase> ParseNavalBases(string content)
    {
        var navalBases = new List<NavalBase>();

        try
        {
            // ステートIDとステート名の抽出
            var stateId = 0;
            var stateName = "Unknown";
            var ownerTag = "XXX";

            var stateIdMatch = Regex.Match(content, @"id\s*=\s*(\d+)");
            if (stateIdMatch.Success) stateId = int.Parse(stateIdMatch.Groups[1].Value);

            var stateNameMatch = Regex.Match(content, @"name\s*=\s*""(.+?)""");
            if (stateNameMatch.Success) stateName = stateNameMatch.Groups[1].Value;

            var ownerMatch = Regex.Match(content, @"owner\s*=\s*(\w+)");
            if (ownerMatch.Success) ownerTag = ownerMatch.Groups[1].Value;

            // プロヴィンスリストの抽出
            var provinces = new List<int>();
            var provincesMatch = Regex.Match(content, @"provinces\s*=\s*\{([^}]+)\}");
            if (provincesMatch.Success)
            {
                var provincesStr = provincesMatch.Groups[1].Value;
                var provinceMatches = Regex.Matches(provincesStr, @"(\d+)");
                foreach (Match m in provinceMatches)
                    if (int.TryParse(m.Groups[1].Value, out var provinceId))
                        provinces.Add(provinceId);
            }

            // naval_baseエントリの抽出
            var navalBaseMatches = Regex.Matches(content, @"(\d+)\s*=\s*\{\s*naval_base\s*=\s*(\d+)\s*\}");
            foreach (Match match in navalBaseMatches)
            {
                var provinceId = int.Parse(match.Groups[1].Value);
                var level = int.Parse(match.Groups[2].Value);

                // レベル1以上の港湾施設だけを追加
                if (level >= 1)
                {
                    var navalBase = new NavalBase(provinceId, level, ownerTag, stateName, stateId);

                    // 建物位置情報を取得して設定
                    var position = GetNavalBasePosition(stateId);
                    if (position != null) navalBase.SetScreenPosition(position.GetScreenPosition());

                    navalBases.Add(navalBase);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"港湾施設解析エラー: {ex.Message}");
        }

        return navalBases;
    }

    // 建物情報を格納するクラス
    public class BuildingPosition
    {
        public int StateId { get; set; }
        public string BuildingId { get; set; }
        public double XPosition { get; set; }
        public double YPosition { get; set; }
        public double ZPosition { get; set; }
        public double Rotation { get; set; }
        public int AdjacentSeaProvince { get; set; }

        public Point GetScreenPosition()
        {
            // ZポジションをYとして使用（HOI4のマップは上から見た形）
            return new Point(XPosition, ZPosition);
        }
    }
}