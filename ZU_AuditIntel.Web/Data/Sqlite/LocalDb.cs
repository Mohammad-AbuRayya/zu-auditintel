using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace ZU_AuditIntel.Web.Data
{
    /// <summary>قسم LocalDb في appsettings.json: مسار ملف القاعدة. فارغٌ يعني App_Data داخل شجرة التطبيق.</summary>
    public sealed class LocalDbOptions
    {
        public const string SectionName = "LocalDb";

        public string Path { get; set; } = "";
    }

    /// <summary>
    /// القاعدة المحلية ملفٌ واحد على قرص دائم. الاتصال يُفتح لكل عملية ويُعاد إلى المجمّع، فوضع WAL
    /// يقرأ منه عدة طلبات معاً بينما يكتب واحد، ولا يمسك طلبٌ اتصالاً عمرَ جلسةٍ كاملة.
    /// والمخطط يُقرأ من schema.sql نفسه الذي تقرأه أداة الاستيراد، فلا مخططَين يتباعدان.
    /// </summary>
    public sealed class LocalDb
    {
        private readonly string connectionString;
        private readonly string schemaPath;

        public string DatabasePath { get; }

        public LocalDb(IOptions<LocalDbOptions> options, IWebHostEnvironment environment)
        {
            var configured = options.Value.Path;

            DatabasePath = string.IsNullOrWhiteSpace(configured)
                ? Path.Combine(environment.ContentRootPath, "App_Data", "zu_auditintel.db")
                : Path.GetFullPath(configured);

            connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = DatabasePath,
                ForeignKeys = true,
                DefaultTimeout = 30
            }.ToString();

            schemaPath = Path.Combine(AppContext.BaseDirectory, "Data", "Sqlite", "schema.sql");
        }

        /// <summary>ينشئ المجلد والجداول والفهارس إن لم تكن: متكرّر الآمن، فلا يمحو قاعدةً قائمة.</summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            var directory = Path.GetDirectoryName(DatabasePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            await using var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            await using (var wal = connection.CreateCommand())
            {
                wal.CommandText = "pragma journal_mode=WAL;";
                await wal.ExecuteNonQueryAsync(cancellationToken);
            }

            var schema = await File.ReadAllTextAsync(schemaPath, cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = schema;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken = default)
        {
            var connection = new SqliteConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

        public async Task<List<T>> QueryAsync<T>(
            string sql,
            Func<SqliteDataReader, T> map,
            CancellationToken cancellationToken = default,
            params (string Name, object? Value)[] args)
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = Build(connection, sql, args);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var rows = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(map(reader));

            return rows;
        }

        public async Task<T?> FirstAsync<T>(
            string sql,
            Func<SqliteDataReader, T> map,
            CancellationToken cancellationToken = default,
            params (string Name, object? Value)[] args) where T : class
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = Build(connection, sql, args);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            return await reader.ReadAsync(cancellationToken) ? map(reader) : null;
        }

        public async Task<object?> ScalarAsync(
            string sql,
            CancellationToken cancellationToken = default,
            params (string Name, object? Value)[] args)
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = Build(connection, sql, args);
            return await command.ExecuteScalarAsync(cancellationToken);
        }

        public async Task<int> ExecuteAsync(
            string sql,
            CancellationToken cancellationToken = default,
            params (string Name, object? Value)[] args)
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var command = Build(connection, sql, args);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        /// <summary>كتابةٌ واحدة أو عدة كتابات في معاملة واحدة: إمّا كلها وإمّا لا شيء منها.</summary>
        public async Task InTransactionAsync(
            Func<SqliteConnection, SqliteTransaction, CancellationToken, Task> work,
            CancellationToken cancellationToken = default)
        {
            await using var connection = await OpenAsync(cancellationToken);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            await work(connection, transaction, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }

        public static SqliteCommand Build(
            SqliteConnection connection,
            string sql,
            (string Name, object? Value)[] args,
            SqliteTransaction? transaction = null)
        {
            var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = transaction;

            foreach (var (name, value) in args)
                command.Parameters.AddWithValue(name, value ?? DBNull.Value);

            return command;
        }

        /// <summary>لحظة UTC بصيغة round-trip: فرزها النصي فرزٌ زمني، وقراءتها تردّها إلى توقيت الجهاز.</summary>
        public static string Moment(DateTime local) => local.ToUniversalTime().ToString("O");

        public static DateTime ReadMoment(string stored) =>
            DateTime.Parse(stored, null, System.Globalization.DateTimeStyles.RoundtripKind).ToLocalTime();

        public static DateTime? ReadMomentOrNull(string? stored) =>
            stored is null ? null : ReadMoment(stored);

        public static string? Text(SqliteDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
        }

        public static int? Int(SqliteDataReader reader, string column)
        {
            var ordinal = reader.GetOrdinal(column);
            return reader.IsDBNull(ordinal) ? null : reader.GetInt32(ordinal);
        }
    }
}