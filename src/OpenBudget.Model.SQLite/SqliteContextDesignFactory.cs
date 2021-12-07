using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Design;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.SQLite
{
    public class SqliteContextDesignFactory : IDesignTimeDbContextFactory<SqliteContext>
    {
        public SqliteContext CreateDbContext(string[] args)
        {
            SqliteConnectionStringBuilder builder = new SqliteConnectionStringBuilder();
            builder.Mode = SqliteOpenMode.Memory;
            builder.Cache = SqliteCacheMode.Shared;
            builder.DataSource = "BudgetDesign";

            SqliteConnection connections = new SqliteConnection(builder.ToString());
            connections.Open();

            return new SqliteContext(connections);
        }
    }
}
