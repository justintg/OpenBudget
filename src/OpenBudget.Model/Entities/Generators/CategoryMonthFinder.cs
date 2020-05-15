using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Entities.Generators
{
    public class CategoryMonthFinder : EntityFinder
    {
        private Category _parent;

        internal CategoryMonthFinder(Category parent)
        {
            _parent = parent;
        }

        public CategoryMonth GetCategoryMonth(DateTime date)
        {
            string entityId = _parent.EntityID.ToString() + $"/{date:yyyyMM}";
            return _model.FindEntity<CategoryMonth>(entityId);
        }

        public IEnumerable<CategoryMonth> GetAllMaterialized()
        {
            return _model.FindRepository<CategoryMonth>().GetEntitiesByParent(nameof(Category), _parent.EntityID);
        }
    }
}
