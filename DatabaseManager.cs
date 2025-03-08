using System;
using System.IO;
using System.Data.SQLite;
using System.Collections.Generic;

namespace HOI4NavalModder
{
    public class DatabaseManager
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public DatabaseManager()
        {
            // ApplicationDataディレクトリにデータベースを配置
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "HOI4NavalModder");

            // フォルダが存在しない場合は作成
            if (!Directory.Exists(appDataPath))
            {
                Directory.CreateDirectory(appDataPath);
            }

            _dbPath = Path.Combine(appDataPath, "naval_module_data.db");
            _connectionString = $"Data Source={_dbPath};Version=3;";
        }

        /// <summary>
        /// データベースが存在しない場合、新規作成して初期化する
        /// </summary>
        public void InitializeDatabase()
        {
            bool needToCreateTables = !File.Exists(_dbPath);

            if (needToCreateTables)
            {
                Console.WriteLine("データベースファイルが見つかりません。新規作成します。");
                SQLiteConnection.CreateFile(_dbPath);
                CreateTables();
            }
            else
            {
                ValidateDatabaseStructure();
            }
        }

        /// <summary>
        /// データベースのテーブルを作成
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
        }

        /// <summary>
        /// データベース構造を検証し、必要に応じて更新する
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
                    {
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
        /// モジュール情報を保存する
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

                                command.Parameters.AddWithValue("@ID", moduleInfo.ID);
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
                            SaveModuleStats(connection, "module_add_stats", moduleInfo.ID, addStats);

                            // module_multiply_statsテーブルに挿入/更新
                            SaveModuleStats(connection, "module_multiply_stats", moduleInfo.ID, multiplyStats);

                            // module_add_average_statsテーブルに挿入/更新
                            SaveModuleStats(connection, "module_add_average_stats", moduleInfo.ID, addAverageStats);

                            // module_resourcesテーブルに挿入/更新
                            using (var command = new SQLiteCommand(connection))
                            {
                                command.CommandText = @"
                                    INSERT OR REPLACE INTO module_resources (ID, aluminium, oil, steel, chromium, tungsten, rubber)
                                    VALUES (@ID, @aluminium, @oil, @steel, @chromium, @tungsten, @rubber);";

                                command.Parameters.AddWithValue("@ID", moduleInfo.ID);
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
                                command.Parameters.AddWithValue("@ID", moduleInfo.ID);
                                command.ExecuteNonQuery();
                            }

                            // 変換モジュール情報を挿入
                            if (convertModules != null && convertModules.Count > 0)
                            {
                                foreach (var convert in convertModules)
                                {
                                    using (var command = new SQLiteCommand(connection))
                                    {
                                        command.CommandText = @"
                                            INSERT INTO module_can_convert (ID, module, category)
                                            VALUES (@ID, @module, @category);";

                                        command.Parameters.AddWithValue("@ID", moduleInfo.ID);
                                        command.Parameters.AddWithValue("@module", convert.Module);
                                        command.Parameters.AddWithValue("@category", convert.Category);

                                        command.ExecuteNonQuery();
                                    }
                                }
                            }

                            transaction.Commit();
                            Console.WriteLine($"モジュール {moduleInfo.ID} のデータを保存しました。");
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
        /// モジュールステータスをデータベースに保存
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
        /// モジュール情報を取得する
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
                            {
                                moduleData.Info = new ModuleInfo
                                {
                                    ID = reader["ID"].ToString(),
                                    Name = reader["name"].ToString(),
                                    Gfx = reader["gfx"].ToString(),
                                    Sfx = reader["sfx"].ToString(),
                                    Year = Convert.ToInt32(reader["year"]),
                                    Manpower = Convert.ToInt32(reader["manpower"]),
                                    Country = reader["country"].ToString(),
                                    CriticalParts = reader["critical_parts"].ToString()
                                };
                            }
                            else
                            {
                                return null; // モジュールが見つからない
                            }
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
                            {
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
                            {
                                moduleData.ConvertModules.Add(new ModuleConvert
                                {
                                    Module = reader["module"].ToString(),
                                    Category = reader["category"].ToString()
                                });
                            }
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
        /// モジュールステータスを取得
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
                    {
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
                    }

                    return new ModuleStats(); // 空のステータスを返す
                }
            }
        }

        /// <summary>
        /// 全てのモジュールIDと名前のリストを取得
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
                            {
                                modules.Add(new ModuleBasicInfo
                                {
                                    ID = reader["ID"].ToString(),
                                    Name = reader["name"].ToString(),
                                    Year = Convert.ToInt32(reader["year"])
                                });
                            }
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
        /// モジュールを削除
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
    }

    // モジュール基本情報クラス
    public class ModuleInfo
    {
        public string ID { get; set; }
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
        public string ID { get; set; }
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
}