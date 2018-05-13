using OpenBudget.Model.Entities;
using OpenBudget.Model.Event;
using OpenBudget.Model.EventStream;
using OpenBudget.Model.Infrastructure;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OpenBudget.Model.Entities.Account;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void EventStreamCanRoundTripFields()
        {
            EntityCreatedEvent evt = new EntityCreatedEvent(nameof(Account), Guid.NewGuid().ToString());
            evt.AddChange("Decimal", FieldChange.Create<decimal>(default(decimal), 100000M));
            evt.AddChange("Enum", FieldChange.Create<AccountBudgetTypes>(default(AccountBudgetTypes), AccountBudgetTypes.OnBudget));
            evt.AddChange("String", FieldChange.Create<string>(default(string), "Test"));
            evt.AddChange("Date", FieldChange.Create<DateTime>(default(DateTime), DateTime.Now));

            EntityCreatedEvent roundTripEvent = EventStreamRoundTripEvent(evt);

            Assert.That(roundTripEvent.Changes["Decimal"].NewValue.GetType(), Is.EqualTo(evt.Changes["Decimal"].NewValue.GetType()));
            Assert.That(roundTripEvent.Changes["Decimal"].NewValue, Is.EqualTo(evt.Changes["Decimal"].NewValue));

            Assert.That(roundTripEvent.Changes["Enum"].NewValue.GetType(), Is.EqualTo(evt.Changes["Enum"].NewValue.GetType()));
            Assert.That(roundTripEvent.Changes["Enum"].NewValue, Is.EqualTo(evt.Changes["Enum"].NewValue));

            Assert.That(roundTripEvent.Changes["String"].NewValue.GetType(), Is.EqualTo(evt.Changes["String"].NewValue.GetType()));
            Assert.That(roundTripEvent.Changes["String"].NewValue, Is.EqualTo(evt.Changes["String"].NewValue));

            Assert.That(roundTripEvent.Changes["Date"].NewValue.GetType(), Is.EqualTo(evt.Changes["Date"].NewValue.GetType()));
            Assert.That(roundTripEvent.Changes["Date"].NewValue, Is.EqualTo(evt.Changes["Date"].NewValue));
        }

        [Test]
        public void PreviousValueIsDefaultAfterRoundTrip()
        {
            /*
             * We don't want to serialize the Previous Value to the event store/stream, but we also
             * want to handle accessing the property gracefully
             */
            EntityCreatedEvent evt = new EntityCreatedEvent(nameof(Account), Guid.NewGuid().ToString());
            evt.AddChange("Decimal", FieldChange.Create<decimal>(500M, 100000M));
            evt.AddChange("Enum", FieldChange.Create<AccountBudgetTypes>(AccountBudgetTypes.OffBudget, AccountBudgetTypes.OnBudget));
            evt.AddChange("String", FieldChange.Create<string>(default(string), "Test"));
            evt.AddChange("Date", FieldChange.Create<DateTime>(default(DateTime), DateTime.Now));


            EntityCreatedEvent roundTripEvent = EventStreamRoundTripEvent(evt);

            Assert.That(roundTripEvent.Changes["Decimal"].PreviousValue, Is.EqualTo(default(decimal)));
            Assert.That(roundTripEvent.Changes["Enum"].PreviousValue, Is.EqualTo(default(AccountBudgetTypes)));
            Assert.That(roundTripEvent.Changes["String"].PreviousValue, Is.EqualTo(default(string)));
            Assert.That(roundTripEvent.Changes["Date"].PreviousValue, Is.EqualTo(default(DateTime)));
        }

        private EntityCreatedEvent EventStreamRoundTripEvent(EntityCreatedEvent evt)
        {
            EventStreamHeader header = new EventStreamHeader(new VectorClock(), new Infrastructure.VectorClock(), Guid.NewGuid(), Guid.NewGuid());

            MemoryStream stream = new MemoryStream();
            using (EventStreamWriter writer = new EventStreamWriter(stream, header, false))
            {
                writer.WriteEvent(evt);
            }

            stream.Seek(0, SeekOrigin.Begin);

            using (stream)
            using (EventStreamReader reader = new EventStreamReader(stream))
            {
                while (reader.Read())
                {
                    if (reader.ItemType == ItemType.Event)
                    {
                        return (EntityCreatedEvent)reader.CurrentEvent;
                    }
                }
            }

            throw new InvalidOperationException("The event could not be round tripped");
        }
    }
}
