﻿using OpenBudget.Model.Events;
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
    public enum BudgetingTypes
    {
        None = 0,
        [Label("On-Budget")]
        OnBudget = 1,
        [Label("Off-Budget")]
        OffBudget = 2
    }

    public enum AccountTypes
    {
        [Label("Checking")]
        [DefaultBudgeting(BudgetingTypes.OnBudget)]
        Checking,

        [Label("Savings")]
        [DefaultBudgeting(BudgetingTypes.OnBudget)]
        Savings,

        [Label("Credit Card")]
        [DefaultBudgeting(BudgetingTypes.OnBudget)]
        CreditCard,

        [Label("Investment Account")]
        [DefaultBudgeting(BudgetingTypes.OffBudget)]
        Investment
    }

    public class AccountSnapshot : EntitySnapshot
    {
        public string Name { get; set; }
        public AccountTypes AccountType { get; set; }
        public BudgetingTypes BudgetingType { get; set; }
    }

    public class Account : EntityBase<AccountSnapshot>
    {
        public Account()
            : base(Guid.NewGuid().ToString())
        {
            Transactions = RegisterChildEntityCollection(new EntityCollection<Transaction>(this, true));
            //Transactions.CollectionChanged += (sender, e) => { BalanceChanged(); };
        }

        internal Account(EntityCreatedEvent evt)
            : base(evt)
        {
            Transactions = RegisterChildEntityCollection(new EntityCollection<Transaction>(this));
            //Transactions.CollectionChanged += (sender, e) => { BalanceChanged(); };
        }

        internal Account(AccountSnapshot snapshot) : base(snapshot)
        {
            Transactions = RegisterChildEntityCollection(new EntityCollection<Transaction>(this));
        }

        public string Name
        {
            get { return GetProperty<string>(); }
            set { SetProperty(value); }
        }

        public AccountTypes AccountType
        {
            get { return GetProperty<AccountTypes>(); }
            set { SetProperty(value); }
        }

        public BudgetingTypes BudgetingType
        {
            get { return GetProperty<BudgetingTypes>(); }
            set { SetProperty(value); }
        }

        private decimal _balance;
        private bool _balanceCalculated = false;

        public decimal Balance
        {
            get
            {
                return _balance;
            }
            internal set
            {
                _balance = value; RaisePropertyChanged();
            }
        }

        protected override void OnAttached(BudgetModel model)
        {
        }

        public EntityCollection<Transaction> Transactions { get; private set; }
    }
}
