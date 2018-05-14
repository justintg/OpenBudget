﻿using NUnit.Framework;
using OpenBudget.Model.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Tests
{
    [TestFixture]
    public class VectorClockTests
    {
        [Test]
        public void CanConvertVectorClockToByteArray()
        {
            Guid deviceOne = Guid.NewGuid();
            Guid deviceTwo = Guid.NewGuid();
            VectorClock clock = new VectorClock();
            clock = clock.Increment(deviceOne);
            clock = clock.Increment(deviceTwo);
            clock = clock.Increment(deviceTwo);
            clock = clock.Increment(deviceTwo);
            clock = clock.Increment(deviceTwo);

            var bytes = clock.ToByteArray();

            var newClock = new VectorClock(bytes);
            Assert.That(clock.Count, Is.EqualTo(newClock.Count));
            foreach(var kvp in clock)
            {
                Assert.That(newClock.ContainsKey(kvp.Key), Is.True);
                Assert.That(newClock[kvp.Key], Is.EqualTo(clock[kvp.Key]));
            }

        }
    }
}
