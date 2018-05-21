﻿using OpenBudget.Model.Event;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Entities
{
    public class SubTransaction : EntityBase
    {
        public SubTransaction() : base(Guid.NewGuid().ToString())
        {
        }

        internal SubTransaction(EntityCreatedEvent evt) : base(evt)
        {
        }

        public decimal Amount
        {
            get => GetProperty<decimal>();
            set => SetProperty(value);
        }

        public string Memo
        {
            get => GetProperty<string>();
            set => SetProperty(value);
        }

        public Account TransferAccount
        {
            get => ResolveEntityReference<Account>();
            set => SetEntityReference(value);
        }

        public BudgetSubCategory TransactionCategory
        {
            get { return ResolveEntityReference<BudgetSubCategory>(nameof(Category)); }
            set { Category = value; }
        }

        public IncomeCategory IncomeCategory
        {
            get { return ResolveEntityReference<IncomeCategory>(nameof(Category)); }
            set { Category = value; }
        }

        public EntityBase Category
        {
            get { return ResolveEntityReference<EntityBase>(); }
            set
            {
                IEnumerable<Type> ValidTypes = (new Type[] { typeof(IncomeCategory), typeof(BudgetSubCategory) });
                if (value != null && !ValidTypes.Contains(value.GetType()))
                    throw new InvalidOperationException("You must assign either a IncomeCategory or BudgetSubCategory to the Category Property!");

                SetEntityReference(value);

                RaisePropertyChanged(nameof(IncomeCategory));
                RaisePropertyChanged(nameof(TransactionCategory));
            }
        }
    }
}
