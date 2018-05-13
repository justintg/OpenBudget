using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Entities
{
    public enum AccountBudgetTypes
    {
        None = 0,
        OnBudget = 1,
        OffBudget = 2
    }

    public class Account : EntityBase
    {
        public Account()
            : base(Guid.NewGuid().ToString())
        {
            Transactions = RegisterChildEntityCollection(new EntityCollection<Transaction>(this));
            //Transactions.CollectionChanged += (sender, e) => { BalanceChanged(); };
        }

        internal Account(EntityCreatedEvent evt)
            : base(evt)
        {
            Transactions = RegisterChildEntityCollection(new EntityCollection<Transaction>(this));
            //Transactions.CollectionChanged += (sender, e) => { BalanceChanged(); };
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public string Type
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public decimal Balance
        {
            get
            {
                return Transactions.Sum(t => t.Amount);
            }
        }

        private PropertyChangedMessageFilter<Transaction> _amountUpdatedFilter;

        internal override void AttachToModel(BudgetModel model)
        {
            base.AttachToModel(model);

            _amountUpdatedFilter = new PropertyChangedMessageFilter<Transaction>(model, id => Transactions.Select(t => t.EntityID).Contains(id), nameof(Transaction.Amount), e => BalanceChanged());
        }

        private void BalanceChanged()
        {
            RaisePropertyChanged(nameof(Balance));
        }

        public EntityCollection<Transaction> Transactions { get; private set; }
    }
}
