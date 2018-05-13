using OpenBudget.Model.Event;
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

        public BudgetSubCategory TransactionCategory
        {
            get { return ResolveEntityReference<BudgetSubCategory>("Category"); }
            set { SetEntityReference(value, "Category"); }
        }

        public IncomeCategory IncomeCategory
        {
            get { return ResolveEntityReference<IncomeCategory>("Category"); }
            set { SetEntityReference(value, "Category"); }
        }

        public EntityReference Category
        {
            get { return GetProperty<EntityReference>(); }
            set
            {
                IEnumerable<string> ValidTypes = (new Type[] { typeof(IncomeCategory), typeof(BudgetSubCategory) }).Select(t => t.Name);
                if (!ValidTypes.Contains(value.EntityType))
                    throw new InvalidOperationException("You must assign either a IncomeCategory or BudgetSubCategory to the Category Property!");

                SetProperty(value);
            }
        }
    }
}
