using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using HOI4NavalModder.Core.Models;

namespace HOI4NavalModder.Core.Services;

public class DatabaseManager
{
    private readonly string _connectionString;
    private readonly string _dbPath;

    public DatabaseManager()
    {
        // ApplicationDataディレクトリにデータベースを配置
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "HOI4NavalModder");

        // フォルダが存在しない場合は作成
        if (!Directory.Exists(appDataPath)) Directory.CreateDirectory(appDataPath);

        _dbPath = Path.Combine(appDataPath, "naval_module_data.db");
        _connectionString = $"Data Source={_dbPath};Version=3;";
    }

    /// <summary>
    ///     データベースが存在しない場合、新規作成して初期化する
    /// </summary>
    public void InitializeDatabase()
    {
        var needToCreateTables = !File.Exists(_dbPath);

        if (needToCreateTables)
        {
            Console.WriteLine("データベースファイルが見つかりません。新規作成します。");
            SQLiteConnection.CreateFile(_dbPath);
            CreateTables();
        }
        else
        {
            ValidateDatabaseStructure();
            UpdateGunRawDataSchema();
        }

        UpdateGunRawDataSchema();
    }


    // DatabaseManager.csのValidateDatabaseStructureメソッド内または初期化時に追加
    public void UpdateGunRawDataSchema()
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // テーブルの存在を確認
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText =
                        "SELECT name FROM sqlite_master WHERE type='table' AND name='guns_raw_datas';";
                    var result = command.ExecuteScalar();

                    if (result != null)
                    {
                        // テーブルが存在する場合、一時テーブルを作成してデータを移行
                        using (var transaction = connection.BeginTransaction())
                        {
                            try
                            {
                                // 古いテーブルのデータをバックアップ
                                command.CommandText =
                                    "CREATE TABLE IF NOT EXISTS guns_raw_datas_backup AS SELECT * FROM guns_raw_datas;";
                                command.ExecuteNonQuery();

                                // 古いテーブルを削除
                                command.CommandText = "DROP TABLE guns_raw_datas;";
                                command.ExecuteNonQuery();

                                // 新しいテーブルを作成
                                command.CommandText = @"
                                CREATE TABLE guns_raw_datas (
                                    ID TEXT PRIMARY KEY,
                                    json_data TEXT NOT NULL
                                );";
                                command.ExecuteNonQuery();

                                // 古いテーブルからデータを移行（UUIDとパスに変換）
                                // 実際のデータ移行はアプリ起動時に必要に応じて実装

                                transaction.Commit();
                                Console.WriteLine("guns_raw_datasテーブルのスキーマを更新しました。");
                            }
                            catch (Exception ex)
                            {
                                transaction.Rollback();
                                Console.WriteLine($"テーブル更新中にエラーが発生しました: {ex.Message}");
                                throw;
                            }
                        }
                    }
                    else
                    {
                        // テーブルが存在しない場合は新規作成
                        command.CommandText = @"
                        CREATE TABLE IF NOT EXISTS guns_raw_datas (
                            ID TEXT PRIMARY KEY,
                            json_data TEXT NOT NULL
                        );";
                        command.ExecuteNonQuery();
                        Console.WriteLine("guns_raw_datasテーブルを作成しました。");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"スキーマ更新中にエラーが発生しました: {ex.Message}");
        }
    }

    /// <summary>
    ///     データベースのテーブルを作成
    /// </summary>
    private void CreateTables()
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // module_infoテーブルの作成
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS module_info (
                                ID TEXT PRIMARY KEY,
                                name TEXT NOT NULL,
                                gfx TEXT,
                                sfx TEXT,
                                year INTEGER,
                                manpower INTEGER,
                                country TEXT,
                                critical_parts TEXT
                            );";
                    command.ExecuteNonQuery();
                }

                // module_add_statsテーブルの作成
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS module_add_stats (
                                ID TEXT PRIMARY KEY,
                                build_cost_ic REAL,
                                naval_speed REAL,
                                fire_range REAL,
                                lg_armor_piercing REAL,
                                lg_attack REAL,
                                hg_armor_piercing REAL,
                                hg_attack REAL,
                                torpedo_attack REAL,
                                anti_air_attack REAL,
                                shore_bombardment REAL,
                                evasion REAL,
                                surface_detection REAL,
                                sub_attack REAL,
                                sub_detection REAL,
                                surface_visibility REAL,
                                sub_visibility REAL,
                                naval_range REAL,
                                port_capacity_usage REAL,
                                search_and_destroy_coordination REAL,
                                convoy_raiding_coordination REAL,
                                FOREIGN KEY (ID) REFERENCES module_info(ID)
                            );";
                    command.ExecuteNonQuery();
                }

                // module_multiply_statsテーブルの作成
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS module_multiply_stats (
                                ID TEXT PRIMARY KEY,
                                build_cost_ic REAL,
                                naval_speed REAL,
                                fire_range REAL,
                                lg_armor_piercing REAL,
                                lg_attack REAL,
                                hg_armor_piercing REAL,
                                hg_attack REAL,
                                torpedo_attack REAL,
                                anti_air_attack REAL,
                                shore_bombardment REAL,
                                evasion REAL,
                                surface_detection REAL,
                                sub_attack REAL,
                                sub_detection REAL,
                                surface_visibility REAL,
                                sub_visibility REAL,
                                naval_range REAL,
                                port_capacity_usage REAL,
                                search_and_destroy_coordination REAL,
                                convoy_raiding_coordination REAL,
                                FOREIGN KEY (ID) REFERENCES module_info(ID)
                            );";
                    command.ExecuteNonQuery();
                }

                // module_add_average_statsテーブルの作成
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS module_add_average_stats (
                                ID TEXT PRIMARY KEY,
                                build_cost_ic REAL,
                                naval_speed REAL,
                                fire_range REAL,
                                lg_armor_piercing REAL,
                                lg_attack REAL,
                                hg_armor_piercing REAL,
                                hg_attack REAL,
                                torpedo_attack REAL,
                                anti_air_attack REAL,
                                shore_bombardment REAL,
                                evasion REAL,
                                surface_detection REAL,
                                sub_attack REAL,
                                sub_detection REAL,
                                surface_visibility REAL,
                                sub_visibility REAL,
                                naval_range REAL,
                                port_capacity_usage REAL,
                                search_and_destroy_coordination REAL,
                                convoy_raiding_coordination REAL,
                                FOREIGN KEY (ID) REFERENCES module_info(ID)
                            );";
                    command.ExecuteNonQuery();
                }

                // module_resourcesテーブルの作成
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS module_resources (
                                ID TEXT PRIMARY KEY,
                                aluminium INTEGER,
                                oil INTEGER,
                                steel INTEGER,
                                chromium INTEGER,
                                tungsten INTEGER,
                                rubber INTEGER,
                                FOREIGN KEY (ID) REFERENCES module_info(ID)
                            );";
                    command.ExecuteNonQuery();
                }

                // module_can_convertテーブルの作成
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                            CREATE TABLE IF NOT EXISTS module_can_convert (
                                ID TEXT,
                                module TEXT,
                                category TEXT,
                                PRIMARY KEY (ID, module),
                                FOREIGN KEY (ID) REFERENCES module_info(ID)
                            );";
                    command.ExecuteNonQuery();
                }

                Console.WriteLine("データベーステーブルを作成しました。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"テーブル作成中にエラーが発生しました: {ex.Message}");
            throw;
        }

        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // 既存のテーブル作成コード...

                // Guns_raw_datasテーブルの作成
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS guns_raw_datas (
                        ID TEXT PRIMARY KEY,
                        json_data TEXT NOT NULL
                    );";
                    command.ExecuteNonQuery();
                }

                Console.WriteLine("データベーステーブルを作成しました。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"テーブル作成中にエラーが発生しました: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     データベース構造を検証し、必要に応じて更新する
    /// </summary>
    private void ValidateDatabaseStructure()
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // 必要なテーブルが存在するか確認
                var requiredTables = new List<string>
                {
                    "module_info",
                    "module_add_stats",
                    "module_multiply_stats",
                    "module_add_average_stats",
                    "module_resources",
                    "module_can_convert"
                };

                foreach (var tableName in requiredTables)
                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText =
                            $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}';";
                        var result = command.ExecuteScalar();

                        if (result == null)
                        {
                            Console.WriteLine($"テーブル {tableName} が見つかりません。再作成します。");
                            // テーブルが存在しない場合は全てのテーブルを再作成
                            CreateTables();
                            return;
                        }
                    }

                Console.WriteLine("データベース構造の検証が完了しました。");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"データベース構造の検証中にエラーが発生しました: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     モジュール情報を保存する
    /// </summary>
    /// <param name="moduleInfo">モジュール基本情報</param>
    /// <param name="addStats">加算ステータス</param>
    /// <param name="multiplyStats">乗算ステータス</param>
    /// <param name="addAverageStats">平均加算ステータス</param>
    /// <param name="resources">必要リソース</param>
    /// <param name="convertModules">変換可能モジュール一覧</param>
    public void SaveModuleData(
        ModuleInfo moduleInfo,
        ModuleStats addStats,
        ModuleStats multiplyStats,
        ModuleStats addAverageStats,
        ModuleResources resources,
        List<ModuleConvert> convertModules)
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // module_infoテーブルに挿入/更新
                        using (var command = new SQLiteCommand(connection))
                        {
                            command.CommandText = @"
                                    INSERT OR REPLACE INTO module_info (ID, name, gfx, sfx, year, manpower, country, critical_parts)
                                    VALUES (@ID, @name, @gfx, @sfx, @year, @manpower, @country, @critical_parts);";

                            command.Parameters.AddWithValue("@ID", moduleInfo.Id);
                            command.Parameters.AddWithValue("@name", moduleInfo.Name);
                            command.Parameters.AddWithValue("@gfx", moduleInfo.Gfx);
                            command.Parameters.AddWithValue("@sfx", moduleInfo.Sfx);
                            command.Parameters.AddWithValue("@year", moduleInfo.Year);
                            command.Parameters.AddWithValue("@manpower", moduleInfo.Manpower);
                            command.Parameters.AddWithValue("@country", moduleInfo.Country);
                            command.Parameters.AddWithValue("@critical_parts", moduleInfo.CriticalParts);

                            command.ExecuteNonQuery();
                        }

                        // module_add_statsテーブルに挿入/更新
                        SaveModuleStats(connection, "module_add_stats", moduleInfo.Id, addStats);

                        // module_multiply_statsテーブルに挿入/更新
                        SaveModuleStats(connection, "module_multiply_stats", moduleInfo.Id, multiplyStats);

                        // module_add_average_statsテーブルに挿入/更新
                        SaveModuleStats(connection, "module_add_average_stats", moduleInfo.Id, addAverageStats);

                        // module_resourcesテーブルに挿入/更新
                        using (var command = new SQLiteCommand(connection))
                        {
                            command.CommandText = @"
                                    INSERT OR REPLACE INTO module_resources (ID, aluminium, oil, steel, chromium, tungsten, rubber)
                                    VALUES (@ID, @aluminium, @oil, @steel, @chromium, @tungsten, @rubber);";

                            command.Parameters.AddWithValue("@ID", moduleInfo.Id);
                            command.Parameters.AddWithValue("@aluminium", resources.Aluminium);
                            command.Parameters.AddWithValue("@oil", resources.Oil);
                            command.Parameters.AddWithValue("@steel", resources.Steel);
                            command.Parameters.AddWithValue("@chromium", resources.Chromium);
                            command.Parameters.AddWithValue("@tungsten", resources.Tungsten);
                            command.Parameters.AddWithValue("@rubber", resources.Rubber);

                            command.ExecuteNonQuery();
                        }

                        // 既存の変換モジュール情報を削除
                        using (var command = new SQLiteCommand(connection))
                        {
                            command.CommandText = "DELETE FROM module_can_convert WHERE ID = @ID;";
                            command.Parameters.AddWithValue("@ID", moduleInfo.Id);
                            command.ExecuteNonQuery();
                        }

                        // 変換モジュール情報を挿入
                        if (convertModules != null && convertModules.Count > 0)
                            foreach (var convert in convertModules)
                                using (var command = new SQLiteCommand(connection))
                                {
                                    command.CommandText = @"
                                            INSERT INTO module_can_convert (ID, module, category)
                                            VALUES (@ID, @module, @category);";

                                    command.Parameters.AddWithValue("@ID", moduleInfo.Id);
                                    command.Parameters.AddWithValue("@module", convert.Module);
                                    command.Parameters.AddWithValue("@category", convert.Category);

                                    command.ExecuteNonQuery();
                                }

                        transaction.Commit();
                        Console.WriteLine($"モジュール {moduleInfo.Id} のデータを保存しました。");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"データ保存中にエラーが発生しました: {ex.Message}");
                        throw;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"データベース接続中にエラーが発生しました: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    ///     モジュールステータスをデータベースに保存
    /// </summary>
    private void SaveModuleStats(SQLiteConnection connection, string tableName, string moduleId, ModuleStats stats)
    {
        using (var command = new SQLiteCommand(connection))
        {
            command.CommandText = $@"
                    INSERT OR REPLACE INTO {tableName} (
                        ID, build_cost_ic, naval_speed, fire_range, lg_armor_piercing, lg_attack,
                        hg_armor_piercing, hg_attack, torpedo_attack, anti_air_attack, shore_bombardment,
                        evasion, surface_detection, sub_attack, sub_detection, surface_visibility,
                        sub_visibility, naval_range, port_capacity_usage, search_and_destroy_coordination,
                        convoy_raiding_coordination
                    )
                    VALUES (
                        @ID, @build_cost_ic, @naval_speed, @fire_range, @lg_armor_piercing, @lg_attack,
                        @hg_armor_piercing, @hg_attack, @torpedo_attack, @anti_air_attack, @shore_bombardment,
                        @evasion, @surface_detection, @sub_attack, @sub_detection, @surface_visibility,
                        @sub_visibility, @naval_range, @port_capacity_usage, @search_and_destroy_coordination,
                        @convoy_raiding_coordination
                    );";

            command.Parameters.AddWithValue("@ID", moduleId);
            command.Parameters.AddWithValue("@build_cost_ic", stats.BuildCostIc);
            command.Parameters.AddWithValue("@naval_speed", stats.NavalSpeed);
            command.Parameters.AddWithValue("@fire_range", stats.FireRange);
            command.Parameters.AddWithValue("@lg_armor_piercing", stats.LgArmorPiercing);
            command.Parameters.AddWithValue("@lg_attack", stats.LgAttack);
            command.Parameters.AddWithValue("@hg_armor_piercing", stats.HgArmorPiercing);
            command.Parameters.AddWithValue("@hg_attack", stats.HgAttack);
            command.Parameters.AddWithValue("@torpedo_attack", stats.TorpedoAttack);
            command.Parameters.AddWithValue("@anti_air_attack", stats.AntiAirAttack);
            command.Parameters.AddWithValue("@shore_bombardment", stats.ShoreBombardment);
            command.Parameters.AddWithValue("@evasion", stats.Evasion);
            command.Parameters.AddWithValue("@surface_detection", stats.SurfaceDetection);
            command.Parameters.AddWithValue("@sub_attack", stats.SubAttack);
            command.Parameters.AddWithValue("@sub_detection", stats.SubDetection);
            command.Parameters.AddWithValue("@surface_visibility", stats.SurfaceVisibility);
            command.Parameters.AddWithValue("@sub_visibility", stats.SubVisibility);
            command.Parameters.AddWithValue("@naval_range", stats.NavalRange);
            command.Parameters.AddWithValue("@port_capacity_usage", stats.PortCapacityUsage);
            command.Parameters.AddWithValue("@search_and_destroy_coordination", stats.SearchAndDestroyCoordination);
            command.Parameters.AddWithValue("@convoy_raiding_coordination", stats.ConvoyRaidingCoordination);

            command.ExecuteNonQuery();
        }
    }

    /// <summary>
    ///     モジュール情報を取得する
    /// </summary>
    /// <param name="moduleId">モジュールID</param>
    /// <returns>モジュール情報</returns>
    public ModuleData GetModuleData(string moduleId)
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                var moduleData = new ModuleData();

                // 基本情報を取得
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT * FROM module_info WHERE ID = @ID;";
                    command.Parameters.AddWithValue("@ID", moduleId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            moduleData.Info = new ModuleInfo
                            {
                                Id = reader["ID"].ToString(),
                                Name = reader["name"].ToString(),
                                Gfx = reader["gfx"].ToString(),
                                Sfx = reader["sfx"].ToString(),
                                Year = Convert.ToInt32(reader["year"]),
                                Manpower = Convert.ToInt32(reader["manpower"]),
                                Country = reader["country"].ToString(),
                                CriticalParts = reader["critical_parts"].ToString()
                            };
                        else
                            return null; // モジュールが見つからない
                    }
                }

                // 各種ステータスを取得
                moduleData.AddStats = GetModuleStats(connection, "module_add_stats", moduleId);
                moduleData.MultiplyStats = GetModuleStats(connection, "module_multiply_stats", moduleId);
                moduleData.AddAverageStats = GetModuleStats(connection, "module_add_average_stats", moduleId);

                // リソース情報を取得
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT * FROM module_resources WHERE ID = @ID;";
                    command.Parameters.AddWithValue("@ID", moduleId);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                            moduleData.Resources = new ModuleResources
                            {
                                Aluminium = Convert.ToInt32(reader["aluminium"]),
                                Oil = Convert.ToInt32(reader["oil"]),
                                Steel = Convert.ToInt32(reader["steel"]),
                                Chromium = Convert.ToInt32(reader["chromium"]),
                                Tungsten = Convert.ToInt32(reader["tungsten"]),
                                Rubber = Convert.ToInt32(reader["rubber"])
                            };
                    }
                }

                // 変換モジュール情報を取得
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT * FROM module_can_convert WHERE ID = @ID;";
                    command.Parameters.AddWithValue("@ID", moduleId);

                    moduleData.ConvertModules = new List<ModuleConvert>();

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            moduleData.ConvertModules.Add(new ModuleConvert
                            {
                                Module = reader["module"].ToString(),
                                Category = reader["category"].ToString()
                            });
                    }
                }

                return moduleData;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"モジュールデータ取得中にエラーが発生しました: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     モジュールステータスを取得
    /// </summary>
    private ModuleStats GetModuleStats(SQLiteConnection connection, string tableName, string moduleId)
    {
        using (var command = new SQLiteCommand(connection))
        {
            command.CommandText = $"SELECT * FROM {tableName} WHERE ID = @ID;";
            command.Parameters.AddWithValue("@ID", moduleId);

            using (var reader = command.ExecuteReader())
            {
                if (reader.Read())
                    return new ModuleStats
                    {
                        BuildCostIc = reader["build_cost_ic"] != DBNull.Value
                            ? Convert.ToDouble(reader["build_cost_ic"])
                            : 0,
                        NavalSpeed = reader["naval_speed"] != DBNull.Value
                            ? Convert.ToDouble(reader["naval_speed"])
                            : 0,
                        FireRange = reader["fire_range"] != DBNull.Value
                            ? Convert.ToDouble(reader["fire_range"])
                            : 0,
                        LgArmorPiercing = reader["lg_armor_piercing"] != DBNull.Value
                            ? Convert.ToDouble(reader["lg_armor_piercing"])
                            : 0,
                        LgAttack = reader["lg_attack"] != DBNull.Value ? Convert.ToDouble(reader["lg_attack"]) : 0,
                        HgArmorPiercing = reader["hg_armor_piercing"] != DBNull.Value
                            ? Convert.ToDouble(reader["hg_armor_piercing"])
                            : 0,
                        HgAttack = reader["hg_attack"] != DBNull.Value ? Convert.ToDouble(reader["hg_attack"]) : 0,
                        TorpedoAttack = reader["torpedo_attack"] != DBNull.Value
                            ? Convert.ToDouble(reader["torpedo_attack"])
                            : 0,
                        AntiAirAttack = reader["anti_air_attack"] != DBNull.Value
                            ? Convert.ToDouble(reader["anti_air_attack"])
                            : 0,
                        ShoreBombardment = reader["shore_bombardment"] != DBNull.Value
                            ? Convert.ToDouble(reader["shore_bombardment"])
                            : 0,
                        Evasion = reader["evasion"] != DBNull.Value ? Convert.ToDouble(reader["evasion"]) : 0,
                        SurfaceDetection = reader["surface_detection"] != DBNull.Value
                            ? Convert.ToDouble(reader["surface_detection"])
                            : 0,
                        SubAttack = reader["sub_attack"] != DBNull.Value
                            ? Convert.ToDouble(reader["sub_attack"])
                            : 0,
                        SubDetection = reader["sub_detection"] != DBNull.Value
                            ? Convert.ToDouble(reader["sub_detection"])
                            : 0,
                        SurfaceVisibility = reader["surface_visibility"] != DBNull.Value
                            ? Convert.ToDouble(reader["surface_visibility"])
                            : 0,
                        SubVisibility = reader["sub_visibility"] != DBNull.Value
                            ? Convert.ToDouble(reader["sub_visibility"])
                            : 0,
                        NavalRange = reader["naval_range"] != DBNull.Value
                            ? Convert.ToDouble(reader["naval_range"])
                            : 0,
                        PortCapacityUsage = reader["port_capacity_usage"] != DBNull.Value
                            ? Convert.ToDouble(reader["port_capacity_usage"])
                            : 0,
                        SearchAndDestroyCoordination = reader["search_and_destroy_coordination"] != DBNull.Value
                            ? Convert.ToDouble(reader["search_and_destroy_coordination"])
                            : 0,
                        ConvoyRaidingCoordination = reader["convoy_raiding_coordination"] != DBNull.Value
                            ? Convert.ToDouble(reader["convoy_raiding_coordination"])
                            : 0
                    };

                return new ModuleStats(); // 空のステータスを返す
            }
        }
    }

    /// <summary>
    ///     全てのモジュールIDと名前のリストを取得
    /// </summary>
    public List<ModuleBasicInfo> GetAllModules()
    {
        var modules = new List<ModuleBasicInfo>();

        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT ID, name, year FROM module_info ORDER BY name;";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                            modules.Add(new ModuleBasicInfo
                            {
                                Id = reader["ID"].ToString(),
                                Name = reader["name"].ToString(),
                                Year = Convert.ToInt32(reader["year"])
                            });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"モジュールリスト取得中にエラーが発生しました: {ex.Message}");
        }

        return modules;
    }

    /// <summary>
    ///     モジュールを削除
    /// </summary>
    public bool DeleteModule(string moduleId)
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // module_can_convertテーブルから削除
                        using (var command = new SQLiteCommand(connection))
                        {
                            command.CommandText = "DELETE FROM module_can_convert WHERE ID = @ID;";
                            command.Parameters.AddWithValue("@ID", moduleId);
                            command.ExecuteNonQuery();
                        }

                        // module_resourcesテーブルから削除
                        using (var command = new SQLiteCommand(connection))
                        {
                            command.CommandText = "DELETE FROM module_resources WHERE ID = @ID;";
                            command.Parameters.AddWithValue("@ID", moduleId);
                            command.ExecuteNonQuery();
                        }

                        // module_add_average_statsテーブルから削除
                        using (var command = new SQLiteCommand(connection))
                        {
                            command.CommandText = "DELETE FROM module_add_average_stats WHERE ID = @ID;";
                            command.Parameters.AddWithValue("@ID", moduleId);
                            command.ExecuteNonQuery();
                        }

                        // module_multiply_statsテーブルから削除
                        using (var command = new SQLiteCommand(connection))
                        {
                            command.CommandText = "DELETE FROM module_multiply_stats WHERE ID = @ID;";
                            command.Parameters.AddWithValue("@ID", moduleId);
                            command.ExecuteNonQuery();
                        }

                        // module_add_statsテーブルから削除
                        using (var command = new SQLiteCommand(connection))
                        {
                            command.CommandText = "DELETE FROM module_add_stats WHERE ID = @ID;";
                            command.Parameters.AddWithValue("@ID", moduleId);
                            command.ExecuteNonQuery();
                        }

                        // 最後にmodule_infoテーブルから削除
                        using (var command = new SQLiteCommand(connection))
                        {
                            command.CommandText = "DELETE FROM module_info WHERE ID = @ID;";
                            command.Parameters.AddWithValue("@ID", moduleId);
                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                        Console.WriteLine($"モジュール {moduleId} を削除しました。");
                        return true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"モジュール削除中にエラーが発生しました: {ex.Message}");
                        return false;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"データベース接続中にエラーが発生しました: {ex.Message}");
            return false;
        }
    }
    // Add these methods to your existing DatabaseManager class


    /// <summary>
    ///     砲の生データをJSON形式で保存する（計算前の入力値のみ）
    /// </summary>
    /// <param name="gunId">砲のID</param>
    /// <param name="rawGunData">砲の生データ</param>
    /// <returns>保存に成功したかどうか</returns>
    /// <summary>
    ///     砲の生データをJSON形式で保存する（計算前の入力値のみ）
    /// </summary>
    /// <param name="gunId">砲のID</param>
    /// <param name="rawGunData">砲の生データ</param>
    /// <returns>保存に成功したかどうか</returns>
    public bool SaveRawGunData(string gunId, Dictionary<string, object> rawGunData)
    {
        try
        {
            // UUIDを生成
            var uuid = Guid.NewGuid().ToString();

            // フォルダパスを作成
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HOI4NavalModder");

            var equipmentsPath = Path.Combine(appDataPath, "equipments");

            // カテゴリに基づいたサブディレクトリを決定
            var categoryDir = "other";
            if (rawGunData.ContainsKey("Category"))
            {
                var category = rawGunData["Category"].ToString().ToLower();
                switch (category)
                {
                    case "smlg":
                        categoryDir = "guns/small";
                        break;
                    case "smmg":
                        categoryDir = "guns/medium";
                        break;
                    case "smhg":
                        categoryDir = "guns/heavy";
                        break;
                    case "smshg":
                        categoryDir = "guns/super_heavy";
                        break;
                    default:
                        categoryDir = $"guns/{category}";
                        break;
                }
            }

            var fullDir = Path.Combine(equipmentsPath, categoryDir);

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(fullDir)) Directory.CreateDirectory(fullDir);

            // JSONファイルのパス - UUIDではなくIDベースのファイル名を使用
            var jsonFileName = $"{gunId}.json";
            var jsonFilePath = Path.Combine(fullDir, jsonFileName);

            // データをJSON文字列に変換して保存
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var jsonData = JsonSerializer.Serialize(rawGunData, options);
            File.WriteAllText(jsonFilePath, jsonData);

            Console.WriteLine($"装備データをJSONファイルに保存しました: {jsonFilePath}");

            // データベースにも参照情報を保存（互換性のため）
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = @"
                INSERT OR REPLACE INTO guns_raw_datas (ID, json_data)
                VALUES (@ID, @json_data);";

                    command.Parameters.AddWithValue("@ID", gunId);
                    command.Parameters.AddWithValue("@json_data", jsonFilePath);

                    command.ExecuteNonQuery();
                }
            }

            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"砲の生データ保存中にエラーが発生しました: {ex.Message}");
            return false;
        }
    }

    public Dictionary<string, object> GetRawGunData(string gunId)
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT json_data FROM guns_raw_datas WHERE ID = @ID;";
                    command.Parameters.AddWithValue("@ID", gunId);

                    var result = command.ExecuteScalar();
                    if (result != null && result != DBNull.Value)
                    {
                        var reference = result.ToString();
                        // UUIDとパスを取得
                        string[] parts = reference.Split(new[] { ':' }, 2);
                        if (parts.Length == 2)
                        {
                            var filePath = parts[1];

                            // ファイルが存在するか確認
                            if (File.Exists(filePath))
                            {
                                // JSONファイルから生データを読み込む
                                var jsonData = File.ReadAllText(filePath);
                                return JsonSerializer.Deserialize<Dictionary<string, object>>(jsonData,
                                    new JsonSerializerOptions
                                    {
                                        PropertyNameCaseInsensitive = true
                                    });
                            }

                            Console.WriteLine($"JSONファイルが見つかりません: {filePath}");
                        }
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"砲の生データ取得中にエラーが発生しました: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    ///     装備の基本情報のみ取得する
    /// </summary>
    /// <summary>
    ///     装備の基本情報のみ取得する - equipmentsディレクトリからJSONファイルを読み込む
    /// </summary>
    public List<NavalEquipment> GetBasicEquipmentInfo()
    {
        var equipmentList = new List<NavalEquipment>();

        try
        {
            // ApplicationDataディレクトリのパスを取得
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HOI4NavalModder");

            // equipmentsディレクトリのパス
            var equipmentsPath = Path.Combine(appDataPath, "equipments");

            // ディレクトリが存在しない場合は作成
            if (!Directory.Exists(equipmentsPath))
            {
                Directory.CreateDirectory(equipmentsPath);
                Console.WriteLine($"equipmentsディレクトリを作成しました: {equipmentsPath}");
                return equipmentList; // 空のリストを返す
            }

            Console.WriteLine($"equipmentsディレクトリからJSONファイルを検索します: {equipmentsPath}");

            // 再帰的にすべてのJSONファイルを検索
            var jsonFiles = Directory.GetFiles(equipmentsPath, "*.json", SearchOption.AllDirectories);
            Console.WriteLine($"検出されたJSONファイル数: {jsonFiles.Length}件");

            foreach (var jsonFile in jsonFiles)
                try
                {
                    // JSONファイルを読み込む
                    var jsonContent = File.ReadAllText(jsonFile);

                    // JSONをデシリアライズ
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };

                    var equipmentData = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonContent, options);

                    if (equipmentData != null &&
                        equipmentData.ContainsKey("Id") &&
                        equipmentData.ContainsKey("Name"))
                    {
                        // 必要な情報のみを抽出
                        var id = equipmentData["Id"].ToString();
                        var name = equipmentData["Name"].ToString();

                        // Year、Categoryなどは存在しない場合があるのでデフォルト値を用意
                        var year = 1900;
                        var category = "SMOT";
                        var country = "";

                        if (equipmentData.ContainsKey("Year"))
                            if (equipmentData["Year"] is JsonElement yearElement)
                                year = GetIntFromJsonElement(yearElement);

                        if (equipmentData.ContainsKey("Category"))
                            if (equipmentData["Category"] is JsonElement categoryElement)
                                category = GetStringFromJsonElement(categoryElement);

                        if (equipmentData.ContainsKey("Country"))
                            if (equipmentData["Country"] is JsonElement categoryElement)
                                country = GetStringFromJsonElement(categoryElement);

                        var equipment = new NavalEquipment
                        {
                            Id = id,
                            Name = name,
                            Category = category,
                            Year = year,
                            Tier = GetTierFromYear(year),
                            Country = country,
                            AdditionalProperties = new Dictionary<string, object>
                            {
                                { "FilePath", jsonFile } // ファイルパスを保存しておく
                            }
                        };

                        Console.WriteLine($"装備データ読み込み: ID={id}, Name={name}");
                        equipmentList.Add(equipment);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"JSONファイル {jsonFile} の解析中にエラーが発生しました: {ex.Message}");
                    // エラーが発生してもスキップして次のファイルを処理
                }

            // データベースからの情報も取得して追加（存在する場合）
            try
            {
                using (var connection = new SQLiteConnection(_connectionString))
                {
                    connection.Open();
                    Console.WriteLine("データベース接続成功");

                    using (var command = new SQLiteCommand(connection))
                    {
                        command.CommandText = @"
                        SELECT 
                            mi.ID, 
                            mi.name, 
                            mi.year, 
                            mi.country
                        FROM 
                            module_info mi
                        ORDER BY 
                            mi.name;";

                        using (var reader = command.ExecuteReader())
                        {
                            var count = 0;
                            while (reader.Read())
                            {
                                count++;
                                var id = reader["ID"].ToString();

                                // 既に同じIDのデータがあるかチェック
                                if (!equipmentList.Any(e => e.Id == id))
                                {
                                    Console.WriteLine($"DBからの装備データ取得: ID={id}, Name={reader["name"]}");

                                    var equipment = new NavalEquipment
                                    {
                                        Id = id,
                                        Name = reader["name"].ToString(),
                                        Year = Convert.ToInt32(reader["year"]),
                                        Tier = GetTierFromYear(Convert.ToInt32(reader["year"])),
                                        Country = reader["country"]?.ToString() ?? "",
                                        AdditionalProperties = new Dictionary<string, object>()
                                    };

                                    equipmentList.Add(equipment);
                                }
                            }

                            Console.WriteLine($"DBから取得したデータ数: {count}件");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"データベースからの基本装備情報の取得中にエラーが発生しました: {ex.Message}");
                // データベースからの取得に失敗しても、JSONからのデータはそのまま返す
            }

            // 結果を名前でソート
            return equipmentList.OrderBy(e => e.Name).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"基本装備情報の取得中にエラーが発生しました: {ex.Message}");
            Console.WriteLine($"スタックトレース: {ex.StackTrace}");
            return new List<NavalEquipment>();
        }
    }

// Helper methods needed by GetBasicEquipmentInfo
// These would typically be in your EquipmentDesignView but are needed here for the query
    private string GetCategoryFromGfx(string gfx)
    {
        if (string.IsNullOrEmpty(gfx))
            return "SMOT"; // デフォルトはその他

        // gfxから適切なカテゴリを判断するロジック
        if (gfx.StartsWith("gfx_smlg_")) return "SMLG";
        if (gfx.StartsWith("gfx_smmg_")) return "SMMG";
        if (gfx.StartsWith("gfx_smhg_")) return "SMHG";
        if (gfx.StartsWith("gfx_smshg_")) return "SMSHG";
        if (gfx.StartsWith("gfx_smtp_")) return "SMTP";
        if (gfx.StartsWith("gfx_smstp_")) return "SMSTP";
        if (gfx.StartsWith("gfx_smsp_")) return "SMSP";
        if (gfx.StartsWith("gfx_smcr_")) return "SMCR";
        if (gfx.StartsWith("gfx_smhc_")) return "SMHC";
        if (gfx.StartsWith("gfx_smasp_")) return "SMASP";
        if (gfx.StartsWith("gfx_smlsp_")) return "SMLSP";
        if (gfx.StartsWith("gfx_smdcl_")) return "SMDCL";
        if (gfx.StartsWith("gfx_smso_")) return "SMSO";
        if (gfx.StartsWith("gfx_smlso_")) return "SMLSO";
        if (gfx.StartsWith("gfx_smdc_")) return "SMDC";
        if (gfx.StartsWith("gfx_smlr_")) return "SMLR";
        if (gfx.StartsWith("gfx_smhr_")) return "SMHR";
        if (gfx.StartsWith("gfx_smaa_")) return "SMAA";
        if (gfx.StartsWith("gfx_smtr_")) return "SMTR";
        if (gfx.StartsWith("gfx_smmbl_")) return "SMMBL";
        if (gfx.StartsWith("gfx_smhbl_")) return "SMHBL";
        if (gfx.StartsWith("gfx_smhaa_")) return "SMHAA";
        if (gfx.StartsWith("gfx_smasm_")) return "SMASM";
        if (gfx.StartsWith("gfx_smsam_")) return "SMSAM";
        if (gfx.StartsWith("gfx_smhng_")) return "SMHNG";

        return "SMOT"; // デフォルトはその他
    }

    private string GetSubCategoryFromGfx(string gfx)
    {
        if (string.IsNullOrEmpty(gfx))
            return "";

        // 砲カテゴリーの場合はサブカテゴリを設定
        if (gfx.Contains("_single")) return "単装砲";
        if (gfx.Contains("_double")) return "連装砲";
        if (gfx.Contains("_triple")) return "三連装砲";
        if (gfx.Contains("_quad")) return "四連装砲";

        return "";
    }

    private int GetTierFromYear(int year)
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

    private static int GetIntFromJsonElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value)) return value;
        return 0; // デフォルト値
    }

    private static string GetStringFromJsonElement(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.String) return element.GetString();
        return string.Empty; // デフォルト値
    }
    // DatabaseManager.cs に以下のメソッドを追加

    /// <summary>
    ///     指定したIDが既に存在するかどうか確認する
    /// </summary>
    /// <param name="id">確認するID</param>
    /// <returns>存在する場合はtrue</returns>
    public bool IdExists(string id)
    {
        try
        {
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // module_info テーブルでの確認
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT COUNT(*) FROM module_info WHERE ID = @ID;";
                    command.Parameters.AddWithValue("@ID", id);

                    var count = Convert.ToInt32(command.ExecuteScalar());
                    if (count > 0) return true;
                }

                // guns_raw_datas テーブルでの確認
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT COUNT(*) FROM guns_raw_datas WHERE ID = @ID;";
                    command.Parameters.AddWithValue("@ID", id);

                    var count = Convert.ToInt32(command.ExecuteScalar());
                    if (count > 0) return true;
                }

                // ローカルファイルシステムでの確認
                // ApplicationDataディレクトリのパスを取得
                var appDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HOI4NavalModder");

                // equipmentsディレクトリのパス
                var equipmentsPath = Path.Combine(appDataPath, "equipments");

                if (Directory.Exists(equipmentsPath))
                {
                    // ID名.jsonという形式のファイルを探す
                    var jsonFiles = Directory.GetFiles(equipmentsPath, $"{id}.json", SearchOption.AllDirectories);
                    if (jsonFiles.Length > 0) return true;

                    // サブディレクトリを再帰的に検索
                    var allJsonFiles = Directory.GetFiles(equipmentsPath, "*.json", SearchOption.AllDirectories);
                    foreach (var jsonFile in allJsonFiles)
                        try
                        {
                            var jsonContent = File.ReadAllText(jsonFile);
                            if (jsonContent.Contains($"\"Id\":\"{id}\"") || jsonContent.Contains($"\"id\":\"{id}\""))
                                return true;
                        }
                        catch
                        {
                            // ファイル読み込みエラーは無視
                        }
                }

                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ID確認中にエラーが発生しました: {ex.Message}");
            return false; // エラー時は存在しないと見なす
        }
    }

    /// <summary>
    ///     すべての装備IDのリストを取得する
    /// </summary>
    /// <returns>IDのリスト</returns>
    public List<string> GetAllEquipmentIds()
    {
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase); // 大文字小文字を区別しない一意のリスト

        try
        {
            // データベースからIDを取得
            using (var connection = new SQLiteConnection(_connectionString))
            {
                connection.Open();

                // module_info テーブルからIDを取得
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT ID FROM module_info;";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) ids.Add(reader["ID"].ToString());
                    }
                }

                // guns_raw_datas テーブルからIDを取得
                using (var command = new SQLiteCommand(connection))
                {
                    command.CommandText = "SELECT ID FROM guns_raw_datas;";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read()) ids.Add(reader["ID"].ToString());
                    }
                }
            }

            // ファイルシステムからもIDを取得
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HOI4NavalModder");

            var equipmentsPath = Path.Combine(appDataPath, "equipments");

            if (Directory.Exists(equipmentsPath))
            {
                // JSONファイルからIDを抽出
                var jsonFiles = Directory.GetFiles(equipmentsPath, "*.json", SearchOption.AllDirectories);
                foreach (var jsonFile in jsonFiles)
                    try
                    {
                        // ファイル名がIDの場合
                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(jsonFile);
                        ids.Add(fileNameWithoutExt);

                        // ファイル内容も確認
                        var jsonContent = File.ReadAllText(jsonFile);
                        var matches = Regex.Matches(jsonContent, @"""[Ii]d""\s*:\s*""([^""]+)""");
                        foreach (Match match in matches)
                            if (match.Groups.Count > 1)
                                ids.Add(match.Groups[1].Value);
                    }
                    catch
                    {
                        // 読み込みエラーは無視
                    }
            }

            return ids.ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ID一覧取得中にエラーが発生しました: {ex.Message}");
            return new List<string>(); // エラー時は空のリストを返す
        }
    }
}

// モジュール基本情報クラス
public class ModuleInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Gfx { get; set; }
    public string Sfx { get; set; }
    public int Year { get; set; }
    public int Manpower { get; set; }
    public string Country { get; set; }
    public string CriticalParts { get; set; }
}

// モジュールステータスクラス
public class ModuleStats
{
    public double BuildCostIc { get; set; }
    public double NavalSpeed { get; set; }
    public double FireRange { get; set; }
    public double LgArmorPiercing { get; set; }
    public double LgAttack { get; set; }
    public double HgArmorPiercing { get; set; }
    public double HgAttack { get; set; }
    public double TorpedoAttack { get; set; }
    public double AntiAirAttack { get; set; }
    public double ShoreBombardment { get; set; }
    public double Evasion { get; set; }
    public double SurfaceDetection { get; set; }
    public double SubAttack { get; set; }
    public double SubDetection { get; set; }
    public double SurfaceVisibility { get; set; }
    public double SubVisibility { get; set; }
    public double NavalRange { get; set; }
    public double PortCapacityUsage { get; set; }
    public double SearchAndDestroyCoordination { get; set; }
    public double ConvoyRaidingCoordination { get; set; }
    public double Reliability { get; set; }
}

// モジュールリソースクラス
public class ModuleResources
{
    public int Aluminium { get; set; }
    public int Oil { get; set; }
    public int Steel { get; set; }
    public int Chromium { get; set; }
    public int Tungsten { get; set; }
    public int Rubber { get; set; }
}

// モジュール変換情報クラス
public class ModuleConvert
{
    public string Module { get; set; }
    public string Category { get; set; }
}

// モジュール基本情報（リスト表示用）
public class ModuleBasicInfo
{
    public string Id { get; set; }
    public string Name { get; set; }
    public int Year { get; set; }
}

// モジュールデータ総合クラス
public class ModuleData
{
    public ModuleInfo Info { get; set; }
    public ModuleStats AddStats { get; set; }
    public ModuleStats MultiplyStats { get; set; }
    public ModuleStats AddAverageStats { get; set; }
    public ModuleResources Resources { get; set; }
    public List<ModuleConvert> ConvertModules { get; set; }
}