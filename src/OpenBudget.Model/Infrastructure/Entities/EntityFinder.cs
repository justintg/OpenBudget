using System;
using System.Collections.Generic;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public abstract class EntityFinder
    {
        protected BudgetModel _model;

        internal void AttachToModel(BudgetModel model)
        {
            if (_model != null && _model != model)
                throw new InvalidOperationException("You cannot attach this entity to a different model after it has already been attached");

            _model = model;
        }
    }
}
