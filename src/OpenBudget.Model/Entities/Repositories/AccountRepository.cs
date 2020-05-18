using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Entities.Repositories
{
    public class AccountRepository : EntityRepository<Account, AccountSnapshot>
    {
        internal AccountRepository(BudgetModel budgetModel) : base(budgetModel)
        {
        }

        protected override Account LoadEntityFromSnapshot(AccountSnapshot snapshot)
        {
            Account account = base.LoadEntityFromSnapshot(snapshot);
            if (account != null)
            {
                account.Balance = _budgetModel.BudgetStore.SnapshotStore.GetAccountBalance(account.EntityID);
            }
            return account;
        }
    }
}
