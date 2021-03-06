﻿using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpenBudget.Model.Infrastructure.UnitOfWork
{
    internal class UnitOfWork
    {
        private Dictionary<string, EntityBase> _entityIdentityMap = new Dictionary<string, EntityBase>();
        private List<EntityBase> _changedEntities = new List<EntityBase>();
        private readonly BudgetModel _budgetModel;

        public UnitOfWork(BudgetModel budgetModel)
        {
            _budgetModel = budgetModel ?? throw new ArgumentNullException(nameof(budgetModel));
        }

        /// <summary>
        /// Returns true if this instance of the entity is contained in the UnitOfWork.
        /// </summary>
        /// <param name="entity">An instance of entity</param>
        /// <returns>true if the isntance of the entity is registered in the unit of work.</returns>
        public bool ContainsEntity(EntityBase entity)
        {
            EntityBase registeredEntity = null;
            if (_entityIdentityMap.TryGetValue(entity.EntityID, out registeredEntity))
            {
                return entity == registeredEntity;
            }
            return false;
        }

        /// <summary>
        /// Returns true if the UnitOfWork contains any copy of this entity, based on the entity key.
        /// </summary>
        /// <param name="entityId"></param>
        /// <returns></returns>
        public bool ContainsEntityId(string entityId)
        {
            return _entityIdentityMap.ContainsKey(entityId);
        }

        public void RegisterChangedEntity(EntityBase entity)
        {
            if (_entityIdentityMap.ContainsKey(entity.EntityID))
            {
                throw new InvalidOperationException("This entity has already been registered for changes. You cannot perform operations on different copies of the same entity.");
            }
            _entityIdentityMap.Add(entity.EntityID, entity);
            _changedEntities.Add(entity);

            foreach (var childCollection in entity.EnumerateChildEntityCollections())
            {
                foreach (var childEntity in childCollection.EnumerateUnattachedEntities())
                {
                    this.RegisterChangedEntity(childEntity);
                }
            }

            UpdateEntityState(entity);
        }

        internal void EnsureSaveOrder(EntityBase first, EntityBase second)
        {
            int firstIndex = _changedEntities.IndexOf(first);
            int secondIndex = _changedEntities.IndexOf(second);

            if (firstIndex < 0 || secondIndex < 0)
                throw new InvalidOperationException("One of the two entities wasn't already registered for changes.");

            if (firstIndex > secondIndex)
            {
                _changedEntities[secondIndex] = first;
                _changedEntities[firstIndex] = second;
            }
        }

        private void UpdateEntityState(EntityBase entity)
        {
            if (entity.SaveState == EntitySaveState.Unattached)
            {
                entity.SaveState = EntitySaveState.UnattachedRegistered;
            }
            else if (entity.SaveState == EntitySaveState.AttachedNoChanges)
            {
                entity.SaveState = EntitySaveState.AttachedHasChanges;
            }
        }

        public List<EventSaveInfo> GetChangeEvents()
        {
            List<EventSaveInfo> events = new List<EventSaveInfo>();

            foreach (var entity in _changedEntities.ToList())
            {
                entity.BeforeSaveChanges(_budgetModel);
            }

            foreach (var entity in _changedEntities)
            {
                var entityEvents = entity.GetAndSaveChanges();
                events.AddRange(entityEvents);
            }

            return events;
        }

        internal void CancelChanges()
        {
            foreach (var entity in _changedEntities)
            {
                entity.CancelCurrentChanges();
            }

            _changedEntities.Clear();
            _entityIdentityMap.Clear();
        }
    }
}
