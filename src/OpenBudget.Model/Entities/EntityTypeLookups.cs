using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class EntityTypeLookups
    {
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
        }

        public static Type GetEntityType(Type snapshotType)
        {
            return entityTypeBySnapshotType[snapshotType];
        }
    }
}
