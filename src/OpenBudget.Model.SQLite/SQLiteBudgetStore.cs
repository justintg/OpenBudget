using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.SQLite.Tables;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    public class SQLiteBudgetStore : IBudgetStore
    {
        private SQLiteConnection _connection;
        private SQLiteEventStore _eventStore;
        private MemorySnapshotStore _snapshotStore;

        public SQLiteBudgetStore(Guid deviceId, string dbPath)
        {
            _connection = new SQLiteConnection(dbPath);
            EnsureTablesInitialized();
            _eventStore = new SQLiteEventStore(_connection);
            _snapshotStore = new MemorySnapshotStore();
        }

        private void EnsureTablesInitialized()
        {
            try
            {
                _connection.CreateTable<SQLiteEvent>();
                _connection.CreateTable<Info>();
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
                using (SQLiteConnection db = new SQLiteConnection(path))
                {
                    EnsureIsSQLiteDatabase(db);
                    EnsureTableExists(db, nameof(SQLiteEvent));
                    EnsureTableExists(db, nameof(Info));
                }
            }
            catch (Exception)
            {
                throw new InvalidBudgetStoreException($"The file {path} is not a Budget or is corrupt.");
            }

            return true;
        }

        private static void EnsureIsSQLiteDatabase(SQLiteConnection db)
        {
            db.ExecuteScalar<int>(@"select count(*) from sqlite_master");
        }

        private static void EnsureTableExists(SQLiteConnection db, string TableName)
        {
            int tableCount = db.ExecuteScalar<int>(@"select count(*) from sqlite_master where type = 'table' and name = ?", TableName);
            if (tableCount != 1)
                throw new InvalidBudgetStoreException();
        }
    }
}
