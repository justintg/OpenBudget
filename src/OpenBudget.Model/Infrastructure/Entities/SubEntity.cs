using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using OpenBudget.Model.Events;
using OpenBudget.Model.Util;

namespace OpenBudget.Model.Infrastructure.Entities
{
    public abstract class SubEntity : EntityBase
    {
        protected SubEntity(string entityId) : base(entityId)
        {
        }

        protected SubEntity(EntityCreatedEvent evt) : base(evt)
        {
        }

        //Override the Parent property so we can store a reference to the Parent
        //the reference won't be persisted to the EventStore since it is unecessary
        //A SubEntity can't exist outside of it's parent
        EntityBase _parent;

        public override EntityBase Parent
        {
            get => _parent;
            internal set => _parent = value;
        }

        public override bool IsAttached => _parent.IsAttached;

        public override EntitySaveState SaveState
        {
            get => _parent.SaveState;
            internal set { }
        }

        protected override void EnsureRegisteredForChanges()
        {
            _parent?.RegisterHasChanges(this);
        }
    }

    public abstract class SubEntity<TSnapshot> : SubEntity where TSnapshot : EntitySnapshot, new()
    {
        private static PropertyAccessorSet<TSnapshot> _snapshotProperties;

        static SubEntity()
        {
            _snapshotProperties = new PropertyAccessorSet<TSnapshot>();
        }

        private TSnapshot _entityData = new TSnapshot();

        protected SubEntity(string entityId) : base(entityId)
        {
        }

        protected SubEntity(EntityCreatedEvent evt) : base(evt)
        {
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
}
