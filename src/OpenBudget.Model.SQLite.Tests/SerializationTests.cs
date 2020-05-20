using NUnit.Framework;
using OpenBudget.Model.Entities;
using OpenBudget.Model.Events;
using OpenBudget.Model.Infrastructure;
using OpenBudget.Model.Infrastructure.Entities;
using OpenBudget.Model.Serialization;
using OpenBudget.Model.SQLite.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.SQLite.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void CanSerializeGroupedFieldEvent()
        {
            Serializer serializer = new Serializer(new SQLiteContractResolver());
            Transaction transaction = new Transaction();
            transaction.Amount = -100;
            transaction.TransactionDate = DateTime.Today;
            transaction.MakeSplitTransaction();

            var subTransaction = transaction.SubTransactions.Create();
            subTransaction.Amount = -100;

            Guid deviceId = Guid.NewGuid();

            var groupedFieldChangeEvent = (GroupedFieldChangeEvent)transaction.GetAndSaveChanges().Select(c => c.Event).Single();
            groupedFieldChangeEvent.StampEvent(deviceId, new VectorClock().Increment(deviceId));
            var json = serializer.SerializeToString(groupedFieldChangeEvent, typeof(ModelEvent));

            var deserialized = (GroupedFieldChangeEvent)serializer.DeserializeFromString<ModelEvent>(json);

            Assert.That(deserialized.EntityID, Is.Null);
            Assert.That(deserialized.EntityType, Is.Null);
            Assert.That(deserialized.EventVector, Is.Null);
            Assert.That(deserialized.DeviceID, Is.EqualTo(default(Guid)));
            Assert.That(deserialized.EventID, Is.EqualTo(default(Guid)));

            foreach (var groupedEvent in deserialized.GroupedEvents)
            {
                var originalEvent = groupedFieldChangeEvent.GroupedEvents.Single(o => o.EventID == groupedEvent.EventID);
                Assert.That(originalEvent, Is.Not.Null);

                Assert.That(groupedEvent.EntityID, Is.EqualTo(originalEvent.EntityID));
                Assert.That(groupedEvent.EntityType, Is.EqualTo(originalEvent.EntityType));
                Assert.That(groupedEvent.EventVector, Is.EqualTo(originalEvent.EventVector));
                Assert.That(groupedEvent.DeviceID, Is.EqualTo(originalEvent.DeviceID));
                Assert.That(groupedEvent.EventID, Is.EqualTo(originalEvent.EventID));

                foreach (var change in groupedEvent.Changes)
                {
                    bool originalHasKey = originalEvent.Changes.TryGetValue(change.Key, out FieldChange originalChange);
                    Assert.That(originalHasKey, Is.True);
                    if (change.Value.NewValue is EntityReference entityReference)
                    {
                        EntityReference originalReference = (EntityReference)originalChange.NewValue;

                        Assert.That(entityReference.EntityID, Is.EqualTo(originalReference.EntityID));
                        Assert.That(entityReference.EntityType, Is.EqualTo(originalReference.EntityType));
                    }
                    else
                    {
                        Assert.That(change.Value.NewValue, Is.EqualTo(originalChange.NewValue));
                    }
                }
            }
        }
    }
}
