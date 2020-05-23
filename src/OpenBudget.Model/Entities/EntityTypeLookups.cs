using OpenBudget.Model.BudgetStore;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class EntityTypeLookups
    {
        private delegate void StoreSnapshotDelegate(ISnapshotStore snapshotStore, EntitySnapshot snapshot);

        private static Dictionary<Type, StoreSnapshotDelegate> storeSnapshotDelegates;

        private static Dictionary<Type, Type> entityTypeBySnapshotType;

        static EntityTypeLookups()
        {
            entityTypeBySnapshotType = new Dictionary<Type, Type>()
            {
                { typeof(AccountSnapshot), typeof(Account) },
                { typeof(BudgetSnapshot), typeof(Budget) },
                { typeof(CategorySnapshot), typeof(Category) },
                { typeof(CategoryMonthSnapshot), typeof(CategoryMonth) },
                { typeof(IncomeCategorySnapshot), typeof(IncomeCategory) },
                { typeof(MasterCategorySnapshot), typeof(MasterCategory) },
                { typeof(PayeeSnapshot), typeof(Payee) },
                { typeof(SubTransactionSnapshot), typeof(SubTransaction) },
                { typeof(TransactionSnapshot), typeof(Transaction) }
            };

            storeSnapshotDelegates = new Dictionary<Type, StoreSnapshotDelegate>()
            {
                { typeof(AccountSnapshot), (ISnapshotStore store, EntitySnapshot snapshot) => { store.StoreSnapshot((AccountSnapshot)snapshot); } },
                { typeof(BudgetSnapshot), (ISnapshotStore store, EntitySnapshot snapshot) => { store.StoreSnapshot((BudgetSnapshot)snapshot); } },
                { typeof(CategorySnapshot), (ISnapshotStore store, EntitySnapshot snapshot) => { store.StoreSnapshot((CategorySnapshot)snapshot); } },
                { typeof(CategoryMonthSnapshot), (ISnapshotStore store, EntitySnapshot snapshot) => { store.StoreSnapshot((CategoryMonthSnapshot)snapshot); } },
                { typeof(IncomeCategorySnapshot), (ISnapshotStore store, EntitySnapshot snapshot) => { store.StoreSnapshot((IncomeCategorySnapshot)snapshot); } },
                { typeof(MasterCategorySnapshot), (ISnapshotStore store, EntitySnapshot snapshot) => { store.StoreSnapshot((MasterCategorySnapshot)snapshot); } },
                { typeof(PayeeSnapshot), (ISnapshotStore store, EntitySnapshot snapshot) => { store.StoreSnapshot((PayeeSnapshot)snapshot); } },
                { typeof(SubTransactionSnapshot), (ISnapshotStore store, EntitySnapshot snapshot) => { store.StoreSnapshot((SubTransactionSnapshot)snapshot); } },
                { typeof(TransactionSnapshot), (ISnapshotStore store, EntitySnapshot snapshot) => { store.StoreSnapshot((TransactionSnapshot)snapshot); } }
            };
        }

        public static Type GetEntityType(Type snapshotType)
        {
            return entityTypeBySnapshotType[snapshotType];
        }

        public static void StoreSnapshot(ISnapshotStore snapshotStore, EntitySnapshot snapshot)
        {
            storeSnapshotDelegates[snapshot.GetType()](snapshotStore, snapshot);
        }
    }
}
