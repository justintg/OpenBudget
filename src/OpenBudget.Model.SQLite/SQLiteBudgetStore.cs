using Microsoft.Data.Sqlite;
using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.SQLite.Tables;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    public class SQLiteBudgetStore : IBudgetStore, IDisposable
    {
        private string _connectionString;
        private SqliteConnection _connection;
        private SQLiteEventStore _eventStore;
        private SQLiteSnapshotStore _snapshotStore;

        public SQLiteBudgetStore(Guid deviceId, string dbPath)
        {
            _connectionString = DBPathToConnectionString(dbPath);
            _connection = new SqliteConnection(_connectionString);
            _connection.Open();
            EnsureTablesInitialized();

            _eventStore = new SQLiteEventStore(_connection);
            _snapshotStore = new SQLiteSnapshotStore(_connectionString);
        }

        private static string DBPathToConnectionString(string dbPath)
        {
            SqliteConnectionStringBuilder connectionStringBuilder = new SqliteConnectionStringBuilder();
            connectionStringBuilder.DataSource = dbPath;

            return connectionStringBuilder.ToString();
        }

        private SqliteContext GetContext()
        {
            return new SqliteContext(_connection);
        }

        private void EnsureTablesInitialized()
        {
            try
            {
                using (var context = GetContext())
                {
                    context.Database.EnsureCreated();
                }
            }
            catch (Exception e)
            {
                throw new InvalidBudgetStoreException("Error initializing tables", e);
            }
        }

        public IEventStore EventStore => _eventStore;

        public ISnapshotStore SnapshotStore => _snapshotStore;

        public TExtension TryGetExtension<TExtension>() where TExtension : class
        {
            return null;
        }

        public static bool EnsureValidEventStore(string path)
        {
            if (!File.Exists(path))
                throw new InvalidBudgetStoreException($"The file {path} does not exist.");

            try
            {
                using (SqliteConnection db = new SqliteConnection(DBPathToConnectionString(path)))
                {
                    db.Open();
                    EnsureIsSQLiteDatabase(db);
                    EnsureTableExists(db, "Events");
                    EnsureTableExists(db, "Info");
                }
            }
            catch (Exception)
            {
                throw new InvalidBudgetStoreException($"The file {path} is not a Budget or is corrupt.");
            }

            return true;
        }

        private static void EnsureIsSQLiteDatabase(SqliteConnection db)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText = @"select count(*) from sqlite_master";
            cmd.ExecuteScalar();
        }

        private static void EnsureTableExists(SqliteConnection db, string TableName)
        {
            var cmd = db.CreateCommand();
            cmd.CommandText = @"select count(*) from sqlite_master where type = 'table' and name = @TableName";
            cmd.Parameters.AddWithValue(@"TableName", TableName);
            object scalar = cmd.ExecuteScalar();
            long tableCount = (long)scalar;
            if (tableCount != 1)
                throw new InvalidBudgetStoreException();
        }

        public void Dispose()
        {
            _connection?.Dispose();
            _snapshotStore?.Dispose();
        }
    }
}
