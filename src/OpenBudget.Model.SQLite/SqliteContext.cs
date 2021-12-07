using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.SQLite.Converters;
using OpenBudget.Model.SQLite.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace OpenBudget.Model.SQLite
{
    public class SqliteContext : DbContext
    {
        private static Type[] SnapshotTypes;
        private static Dictionary<Type, Func<SqliteContext, object>> SnapshotSetLookups = new Dictionary<Type, Func<SqliteContext, object>>();

        static SqliteContext()
        {
            var snapshotSetProperties = typeof(SqliteContext).GetProperties()
                .Where(p => p.PropertyType.IsGenericType
                && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>)
                && typeof(EntitySnapshot).IsAssignableFrom(p.PropertyType.GenericTypeArguments.Single())).ToList();

            SnapshotTypes = snapshotSetProperties.Select(p => p.PropertyType.GenericTypeArguments.Single()).ToArray();

            foreach (var snapshotProperty in snapshotSetProperties)
            {
                var snapshotType = snapshotProperty.PropertyType.GenericTypeArguments.Single();
                var contextParam = Expression.Parameter(typeof(SqliteContext), "context");
                var getExpression = Expression.MakeMemberAccess(contextParam, snapshotProperty);

                Expression<Func<SqliteContext, object>> expr = Expression.Lambda<Func<SqliteContext, object>>(getExpression, contextParam);
                SnapshotSetLookups.Add(snapshotType, expr.Compile());
            }
        }

        private readonly SqliteConnection _connection;

        public DbSet<SQLiteEvent> Events { get; set; }
        public DbSet<Info> Info { get; set; }
        public DbSet<AccountSnapshot> Accounts { get; set; }
        public DbSet<BudgetSnapshot> Budgets { get; set; }
        public DbSet<CategorySnapshot> Categories { get; set; }
        public DbSet<CategoryMonthSnapshot> CategoryMonths { get; set; }
        public DbSet<IncomeCategorySnapshot> IncomeCategories { get; set; }
        public DbSet<MasterCategorySnapshot> MasterCategories { get; set; }
        public DbSet<PayeeSnapshot> Payees { get; set; }
        public DbSet<SubTransactionSnapshot> SubTransactions { get; set; }
        public DbSet<TransactionSnapshot> Transactions { get; set; }

        public SqliteContext(SqliteConnection connection)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        }

        public DbSet<TSnapshotType> GetSnapshotSet<TSnapshotType>() where TSnapshotType : EntitySnapshot
        {
            if (SnapshotSetLookups.TryGetValue(typeof(TSnapshotType), out Func<SqliteContext, object> dbSetGetter))
            {
                return (DbSet<TSnapshotType>)dbSetGetter(this);
            }

            return null;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseModel(SqliteModels.SqliteContextModel.Instance);
            optionsBuilder.UseSqlite(_connection);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SQLiteEvent>(builder =>
            {
                builder.ToTable("Events");
                builder.HasKey(e => e.EventID);
                builder.HasIndex(e => e.EntityID);
                builder.HasIndex(e => new { e.EntityType, e.EntityID });
                builder.Property(e => e.EventID).HasConversion<GuidConverter>();
                builder.Property(e => e.DeviceID).HasConversion<GuidConverter>();
            });

            modelBuilder.Entity<Info>(builder =>
            {
                builder.ToTable("Info");
                builder.HasIndex(i => i.Key).IsUnique();
            });

            foreach (var entityType in SnapshotTypes)
            {
                modelBuilder.Entity(entityType, builder =>
                {
                    builder.HasKey(nameof(EntitySnapshot.EntityID));
                    builder.Property<VectorClock>(nameof(EntitySnapshot.LastEventVector)).HasConversion<VectorClockConverter>();

                    var entityReferenceProperties = entityType.GetProperties().Where(p => p.PropertyType == typeof(EntityReference)).ToList();
                    foreach (var entityReference in entityReferenceProperties)
                    {
                        builder.OwnsOne(typeof(EntityReference), entityReference.Name, o =>
                        {
                            o.Ignore(nameof(EntityReference.ReferencedEntity));
                        });
                    }
                });
            }
        }
    }
}
