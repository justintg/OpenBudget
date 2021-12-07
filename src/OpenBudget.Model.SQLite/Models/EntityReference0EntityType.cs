﻿// <auto-generated />
using System;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OpenBudget.Model.Infrastructure.Entities;

#pragma warning disable 219, 612, 618
#nullable disable

namespace SqliteModels
{
    internal partial class EntityReference0EntityType
    {
        public static RuntimeEntityType Create(RuntimeModel model, RuntimeEntityType baseEntityType = null)
        {
            var runtimeEntityType = model.AddEntityType(
                "OpenBudget.Model.Entities.BudgetSnapshot.Parent#EntityReference",
                typeof(EntityReference),
                baseEntityType,
                sharedClrType: true);

            var budgetSnapshotEntityID = runtimeEntityType.AddProperty(
                "BudgetSnapshotEntityID",
                typeof(string),
                afterSaveBehavior: PropertySaveBehavior.Throw);

            var entityID = runtimeEntityType.AddProperty(
                "EntityID",
                typeof(string),
                propertyInfo: typeof(EntityReference).GetProperty("EntityID", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(EntityReference).GetField("<EntityID>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var entityType = runtimeEntityType.AddProperty(
                "EntityType",
                typeof(string),
                propertyInfo: typeof(EntityReference).GetProperty("EntityType", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(EntityReference).GetField("<EntityType>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                nullable: true);

            var key = runtimeEntityType.AddKey(
                new[] { budgetSnapshotEntityID });
            runtimeEntityType.SetPrimaryKey(key);

            return runtimeEntityType;
        }

        public static RuntimeForeignKey CreateForeignKey1(RuntimeEntityType declaringEntityType, RuntimeEntityType principalEntityType)
        {
            var runtimeForeignKey = declaringEntityType.AddForeignKey(new[] { declaringEntityType.FindProperty("BudgetSnapshotEntityID") },
                principalEntityType.FindKey(new[] { principalEntityType.FindProperty("EntityID") }),
                principalEntityType,
                deleteBehavior: DeleteBehavior.Cascade,
                unique: true,
                required: true,
                ownership: true);

            var parent = principalEntityType.AddNavigation("Parent",
                runtimeForeignKey,
                onDependent: false,
                typeof(EntityReference),
                propertyInfo: typeof(EntitySnapshot).GetProperty("Parent", BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                fieldInfo: typeof(EntitySnapshot).GetField("<Parent>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly),
                eagerLoaded: true);

            return runtimeForeignKey;
        }

        public static void CreateAnnotations(RuntimeEntityType runtimeEntityType)
        {
            runtimeEntityType.AddAnnotation("Relational:FunctionName", null);
            runtimeEntityType.AddAnnotation("Relational:Schema", null);
            runtimeEntityType.AddAnnotation("Relational:SqlQuery", null);
            runtimeEntityType.AddAnnotation("Relational:TableName", "Budgets");
            runtimeEntityType.AddAnnotation("Relational:ViewName", null);
            runtimeEntityType.AddAnnotation("Relational:ViewSchema", null);

            Customize(runtimeEntityType);
        }

        static partial void Customize(RuntimeEntityType runtimeEntityType);
    }
}
