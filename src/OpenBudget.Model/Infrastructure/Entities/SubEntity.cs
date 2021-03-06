﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using OpenBudget.Model.Entities;
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
            internal set
            {
                _parent = value;
                if (GetProperty<EntityReference>(nameof(Parent)) == null)
                {
                    SetProperty<EntityReference>(value.ToEntityReference(), nameof(Parent));
                }
            }
        }

        public override bool IsAttached => _parent == null ? false : _parent.IsAttached;

        public override BudgetModel Model
        {
            get => _parent.Model == null ? null : _parent.Model;
            internal set { }
        }

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

        internal TSnapshot GetSnapshot()
        {
            return _entityData;
        }

        protected override EntitySnapshot GetSnapshotInternal()
        {
            return _entityData;
        }

        public override EntityBase Parent
        {
            get => base.Parent;
            internal set
            {
                base.Parent = value;
                _entityData.Parent = value.ToEntityReference();
            }
        }

        protected SubEntity(TSnapshot snapshot) : base(snapshot.EntityID)
        {
            _entityData = snapshot;
            CurrentEvent = new EntityUpdatedEvent(this.GetType().Name, EntityID);
        }

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
