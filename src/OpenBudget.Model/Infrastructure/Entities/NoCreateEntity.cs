using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using OpenBudget.Model.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public abstract class NoCreateEntity<TSnapshot> : NoCreateEntity where TSnapshot : EntitySnapshot, new()
    {
        private static PropertyAccessorSet<TSnapshot> _snapshotProperties;

        static NoCreateEntity()
        {
            _snapshotProperties = new PropertyAccessorSet<TSnapshot>();
        }

        private TSnapshot _entityData = new TSnapshot();

        internal NoCreateEntity(string entityId) : base(entityId)
        {
        }

        internal NoCreateEntity(TSnapshot snapshot) : base(snapshot.EntityID)
        {
            _entityData = snapshot;
        }

        internal TSnapshot GetSnapshot()
        {
            return _entityData;
        }

        protected override T GetEntityData<T>(string property)
        {
            return _snapshotProperties.GetEntityData<T>(_entityData, property);
        }

        protected override void SetEntityData<T>(T value, string property)
        {
            _snapshotProperties.SetEntityData<T>(_entityData, value, property);
        }

        protected override IEnumerable<string> GetPropertyNames()
        {
            return _snapshotProperties.GetPropertyNames();
        }

        protected override void SetEntityDataObject(object value, string property)
        {
            _snapshotProperties.SetEntityDataObject(_entityData, value, property);
        }

        protected override object GetEntityDataObject(string property)
        {
            return _snapshotProperties.GetEntityDataObject(_entityData, property);
        }

        protected override void ClearEntityData()
        {
            _entityData = new TSnapshot();
        }
    }

    public abstract class NoCreateEntity : EntityBase
    {
        internal NoCreateEntity(string entityId) : base(entityId)
        {
            this.CurrentEvent = new EntityUpdatedEvent(this.GetType().Name, entityId);
        }

        public override void Delete()
        {
            throw new InvalidBudgetActionException("Entities of this type always exist and cannot be deleted");
        }
    }
}
