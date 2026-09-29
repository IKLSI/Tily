using Microsoft.Data.SqlClient;

namespace Dock.Core.Worktrees;

public sealed class SqlServerReplicator : IDatabaseReplicator
{
    private const string MasterDatabase = "master";
    private const string DatabaseIdQuery = "SELECT DB_ID(@name)";
    private const string LogType = "L";
    private const int ConnectTimeoutSeconds = 30;

    public DatabaseProvider Provider => DatabaseProvider.SqlServer;

    public DatabaseOutcomeModel Replicate(DatabaseInfoModel info, string targetDb, DatabaseContextModel context)
    {
        using var connection = Open(info);
        if (!DatabaseExists(connection, info.SourceDb))
        {
            return new DatabaseOutcomeModel(false, $"Base source « {info.SourceDb} » absente sur « {info.Host} » : réplication ignorée.");
        }

        if (DatabaseExists(connection, targetDb))
        {
            return new DatabaseOutcomeModel(true, $"Base « {targetDb} » déjà présente : réutilisée.");
        }

        var dataDirectory = Scalar(connection, "SELECT CAST(SERVERPROPERTY('InstanceDefaultDataPath') AS nvarchar(4000))") as string;
        dataDirectory = string.IsNullOrWhiteSpace(dataDirectory) ? Path.GetTempPath() : dataDirectory;
        var backup = Backup(connection, info.SourceDb, targetDb, dataDirectory);
        try
        {
            var moves = Moves(connection, backup, targetDb, dataDirectory);
            if (moves.Count == 0)
            {
                return new DatabaseOutcomeModel(false, "Aucun fichier logique dans la sauvegarde : réplication ignorée.");
            }

            var clauses = string.Join(", ", moves.Select((_, index) => $"MOVE @logical{index} TO @physical{index}"));
            using var restore = Command(connection, $"RESTORE DATABASE {DatabaseNames.SqlServerIdentifier(targetDb)} FROM DISK = @disk WITH {clauses}, REPLACE");
            restore.Parameters.AddWithValue("@disk", backup);
            for (var index = 0; index < moves.Count; index++)
            {
                restore.Parameters.AddWithValue($"@logical{index}", moves[index].Logical);
                restore.Parameters.AddWithValue($"@physical{index}", moves[index].Physical);
            }

            restore.ExecuteNonQuery();
        }
        finally
        {
            TryDelete(backup);
        }

        return DatabaseExists(connection, targetDb)
            ? new DatabaseOutcomeModel(true, $"Base répliquée : {info.SourceDb} → {targetDb} (SQL Server).")
            : new DatabaseOutcomeModel(false, $"Échec du RESTORE SQL Server pour « {targetDb} ».");
    }

    public DatabaseOutcomeModel Drop(DatabaseInfoModel info, string targetDb, DatabaseContextModel context)
    {
        using var connection = Open(info);
        var identifier = DatabaseNames.SqlServerIdentifier(targetDb);
        using var drop = Command(connection, $"IF DB_ID(@name) IS NOT NULL BEGIN ALTER DATABASE {identifier} SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE {identifier}; END");
        drop.Parameters.AddWithValue("@name", targetDb);
        drop.ExecuteNonQuery();
        return new DatabaseOutcomeModel(true, $"Base « {targetDb} » supprimée.");
    }

    private static SqlConnection Open(DatabaseInfoModel info)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = info.Host ?? string.Empty,
            InitialCatalog = MasterDatabase,
            TrustServerCertificate = true,
            ConnectTimeout = ConnectTimeoutSeconds
        };
        if (info.IntegratedSecurity)
        {
            builder.IntegratedSecurity = true;
        }
        else
        {
            builder.UserID = info.User ?? string.Empty;
            builder.Password = info.Password ?? string.Empty;
        }

        var connection = new SqlConnection(builder.ConnectionString);
        connection.Open();
        return connection;
    }

    private static string Backup(SqlConnection connection, string sourceDb, string targetDb, string dataDirectory)
    {
        var fileName = $"{targetDb}.bak";
        foreach (var directory in new[] { Path.GetTempPath(), dataDirectory })
        {
            var path = Path.Combine(directory, fileName);
            try
            {
                using var backup = Command(connection, $"BACKUP DATABASE {DatabaseNames.SqlServerIdentifier(sourceDb)} TO DISK = @disk WITH INIT, COPY_ONLY, FORMAT");
                backup.Parameters.AddWithValue("@disk", path);
                backup.ExecuteNonQuery();
                return path;
            }
            catch (SqlException) when (directory != dataDirectory)
            {
            }
        }

        throw new WorktreeException("Sauvegarde SQL Server impossible.", WorktreeSteps.Database);
    }

    private static List<(string Logical, string Physical)> Moves(SqlConnection connection, string backup, string targetDb, string dataDirectory)
    {
        using var list = Command(connection, "RESTORE FILELISTONLY FROM DISK = @disk");
        list.Parameters.AddWithValue("@disk", backup);
        using var reader = list.ExecuteReader();
        var moves = new List<(string Logical, string Physical)>();
        var dataIndex = 0;
        var logIndex = 0;
        while (reader.Read())
        {
            var logical = Convert.ToString(reader["LogicalName"]) ?? string.Empty;
            var isLog = string.Equals(Convert.ToString(reader["Type"])?.Trim(), LogType, StringComparison.OrdinalIgnoreCase);
            var suffix = isLog
                ? (logIndex++ == 0 ? "_log.ldf" : $"_log{logIndex - 1}.ldf")
                : (dataIndex++ == 0 ? ".mdf" : $"_{dataIndex - 1}.ndf");
            moves.Add((logical, Path.Combine(dataDirectory, $"{targetDb}{suffix}")));
        }

        return moves;
    }

    private static bool DatabaseExists(SqlConnection connection, string name)
    {
        using var command = Command(connection, DatabaseIdQuery);
        command.Parameters.AddWithValue("@name", name);
        return command.ExecuteScalar() is not (null or DBNull);
    }

    private static object? Scalar(SqlConnection connection, string sql)
    {
        using var command = Command(connection, sql);
        return command.ExecuteScalar();
    }

    private static SqlCommand Command(SqlConnection connection, string sql) => new(sql, connection) { CommandTimeout = 0 };

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
