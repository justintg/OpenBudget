using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Entities
{
    public enum AccountBudgetTypes
    {
        None = 0,
        [Label("On-Budget")]
        OnBudget = 1,
        [Label("Off-Budget")]
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

        public AccountBudgetTypes AccountType
        {
            get { return GetProperty<AccountBudgetTypes>(); }
            set { SetProperty(value); }
        }

        private decimal _balance;
        private bool _balanceCalculated = false;

        public decimal Balance
        {
            get
            {
                if (!_balanceCalculated) CalculateBalance();
                return _balance;
            }
        }

        private void CalculateBalance()
        {
            _balanceCalculated = true;
            _balance = Transactions.Sum(t => t.Amount);
        }

        private void BalanceChanged()
        {
            if (_balanceCalculated) CalculateBalance();
            RaisePropertyChanged(nameof(Balance));
        }

        //private PropertyChangedMessageFilter<Transaction> _amountUpdatedFilter;

        internal override void AttachToModel(BudgetModel model)
        {
            base.AttachToModel(model);

            model.MessageBus.EntityCreatedOrUpdated
                .Where(e => e.EntityType == nameof(Transaction)
                && Transactions.Select(t => t.EntityID).Contains(e.EntityID)
                && e.Changes.ContainsKey(nameof(Transaction.Amount)))
                .Subscribe(e => BalanceChanged());

            //_amountUpdatedFilter = new PropertyChangedMessageFilter<Transaction>(model, id => Transactions.Select(t => t.EntityID).Contains(id), nameof(Transaction.Amount), e => BalanceChanged());
        }

        public EntityCollection<Transaction> Transactions { get; private set; }
    }
}
