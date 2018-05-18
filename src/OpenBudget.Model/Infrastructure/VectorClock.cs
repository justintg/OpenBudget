using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace OpenBudget.Model.Infrastructure
{
    [DataContract]
    public class VectorClock : IComparable, IReadOnlyDictionary<Guid, int>
    {
        /// <summary>
        /// Initialises a new instance of the <see cref="VectorClock"/> class.
        /// </summary>
        public VectorClock()
        {
            this.Vector = new Dictionary<Guid, int>();
            this.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VectorClock"/> class.
        /// </summary>
        /// <param name="vector">Initial vector</param>
        public VectorClock(Dictionary<Guid, int> vector) : this(vector, true)
        {
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VectorClock"/> class.
        /// </summary>
        /// <param name="vector">Initial vector in byte array format</param>
        public VectorClock(byte[] vector) : this(ConvertBytesToVector(vector), false)
        {
            this.Timestamp = GetDate(vector);
        }

        /// <summary>
        /// Initialises a new instance of the <see cref="VectorClock"/> class.
        /// </summary>
        /// <param name="vector">Initial vector</param>
        /// <param name="copyVector">If the vector should be copied</param>
        private VectorClock(Dictionary<Guid, int> vector, bool copyVector)
        {
            this.Vector = vector;
            if (copyVector)
            {
                this.Vector = this.CopyVector();
            }

            this.Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// Comparison result for vector clocks
        /// </summary>
        public enum ComparisonResult
        {
            /// <summary>
            /// Both vector clocks are equal
            /// </summary>
            Equal,

            /// <summary>
            /// The vector clock is greater (later)
            /// </summary>
            Greater,

            /// <summary>
            /// The vector clock is smaller (earlier)
            /// </summary>
            Smaller,

            /// <summary>
            /// Both vector clocks are simultaneous
            /// </summary>
            Simultaneous
        }

        /// <summary>
        /// Gets the timestamp
        /// </summary>
        [DataMember]
        public DateTime Timestamp { get; private set; }

        /// <summary>
        /// Gets an IEnumerable of Keys/Devices in the Vector Clock.
        /// </summary>
        public IEnumerable<Guid> Keys => this.Vector.Keys;

        /// <summary>
        /// Gets an IEnumerable of integer values in the Vector Clock.
        /// </summary>
        public IEnumerable<int> Values => this.Vector.Values;

        /// <summary>
        /// The number of devices in the Vector Clock.
        /// </summary>
        public int Count => this.Vector.Count;

        /// <summary>
        /// Gets or sets the Vector.
        /// </summary>
        [DataMember]
        private Dictionary<Guid, int> Vector { get; set; }

        /// <summary>
        /// Gets the value for a particular device.
        /// </summary>
        /// <param name="key">The device</param>
        /// <returns>The value for the device</returns>
        public int this[Guid key]
        {
            get { return this.Vector[key]; }
        }

        /// <summary>
        /// Create a copy of the current VectorClock and Increment the Vector for the given Device ID by 1 on the new VectorClock
        /// </summary>
        /// <param name="deviceId">The deviceId to increment.</param>
        /// <returns>The new incremented VectorClock</returns>
        public VectorClock Increment(Guid deviceId)
        {
            Dictionary<Guid, int> newVector = this.CopyVector();
            if (newVector.ContainsKey(deviceId))
            {
                newVector[deviceId]++;
            }
            else
            {
                newVector[deviceId] = 1;
            }

            return new VectorClock(newVector, false);
        }

        /// <summary>
        /// Merges the VectorClock with a second VectorClock by returning a new VectorClock
        /// that has maximum vector value for each device.
        /// </summary>
        /// <param name="clock2">The VectorClock to be merged into the current VectorClock.</param>
        /// <returns>A new VectorClock with the maximum value for each device.</returns>
        public VectorClock Merge(VectorClock clock2)
        {
            Dictionary<Guid, int> mergedVector = this.CopyVector();

            foreach (Guid deviceId in this.Vector.Keys)
            {
                if (clock2.Vector.ContainsKey(deviceId))
                {
                    mergedVector[deviceId] = Math.Max(this.Vector[deviceId], clock2.Vector[deviceId]);
                }
                else
                {
                    mergedVector[deviceId] = this.Vector[deviceId];
                }
            }

            foreach (Guid deviceId in clock2.Vector.Keys)
            {
                if (!this.Vector.ContainsKey(deviceId))
                {
                    mergedVector[deviceId] = clock2.Vector[deviceId];
                }
            }

            return new VectorClock(mergedVector, false);
        }

        /// <summary>
        /// A function to compare the current VectorClock to another and 
        /// determine which came first, or if the events happened simultaneously.
        /// </summary>
        /// <param name="clock2">The VectorClock to compare.</param>
        /// <returns>A ComparisonResult enum with the result of the comparison.</returns>
        public ComparisonResult CompareVectors(VectorClock clock2)
        {
            /* We check every deviceId that is a key in this vector clock against every deviceId in clock2.
             * If all deviceId values in both clocks are equal they are the same clock(equal).  This result should never happen in OpenBudget since we always increment the clock for each event.
             * If every deviceId value in this clock is less than the deviceId in clock2 then this VectorClock came before clock2. Some of the deviceIds can be equal but at least one must be less for this case.
             * If every deviceId value in this clock is greater than the deviceId in clock2 then this VectorClock came after clock2.   Some of the deviceIds can be equal but at least one must be greater for this case.
             * If at least one deviceId is greater, and at least one other deviceId is less between the two clocks, the events happened simultaneously.
             */

            bool equal = true;
            bool greater = true;
            bool smaller = true;

            foreach (Guid deviceId in this.Keys)
            {
                if (clock2.ContainsKey(deviceId))
                {
                    if (this[deviceId] < clock2[deviceId])
                    {
                        equal = false;
                        greater = false;
                    }

                    if (this[deviceId] > clock2[deviceId])
                    {
                        equal = false;
                        smaller = false;
                    }
                }
                else if (this[deviceId] != 0)
                {
                    equal = false;
                    smaller = false;
                }
            }

            // Check if clock2 has any deviceIds that are not present in this VectorClock
            foreach (Guid deviceId in clock2.Keys)
            {
                if (!this.ContainsKey(deviceId) && clock2[deviceId] != 0)
                {
                    equal = false;
                    greater = false;
                }
            }

            if (equal)
            {
                // The vectors are the same
                return ComparisonResult.Equal;
            }
            else if (greater && !smaller)
            {
                return ComparisonResult.Greater;
            }
            else if (smaller && !greater)
            {
                return ComparisonResult.Smaller;
            }
            else
            {
                // The events were simultaneous
                return ComparisonResult.Simultaneous;
            }
        }

        /// <summary>
        /// The default comparer for VectorClock.  Determines the order of events 
        /// based on the vector and the timestamp.
        /// </summary>
        /// <param name="obj">The VectorClock to compare to.</param>
        /// <returns>1 if this VectorClock happened after, 
        /// -1 if this VectorClock happened before,
        /// 0 if the order can not be determined(Simultaneous or Equal Vectors and the same timestamp)</returns>
        public int CompareTo(object obj)
        {
            VectorClock clock2 = obj as VectorClock;
            ComparisonResult compare = this.CompareVectors(clock2);

            switch (compare)
            {
                case ComparisonResult.Greater:
                    return 1;
                case ComparisonResult.Smaller:
                    return -1;
                default:
                    return DateTime.Compare(this.Timestamp, clock2.Timestamp);
            }
        }

        /// <summary>
        /// Create an exact copy of this current VectorClock.
        /// </summary>
        /// <returns>A copy of the current VectorClock</returns>
        public VectorClock Copy()
        {
            return new VectorClock(this.CopyVector(), false);
        }

        /// <summary>
        /// Returns a boolean value of whether the VectorClock contains a device.
        /// </summary>
        /// <param name="key">The device</param>
        /// <returns>True if the device is present in the VectorClock, otherwise false</returns>
        public bool ContainsKey(Guid key) => this.Vector.ContainsKey(key);

        /// <summary>
        /// Tries to get the value for a device, and returns a boolean if the get is successful or not.
        /// </summary>
        /// <param name="key">The device</param>
        /// <param name="value">The value for the device</param>
        /// <returns>Whether the get is successful or not</returns>
        public bool TryGetValue(Guid key, out int value) => this.Vector.TryGetValue(key, out value);

        /// <summary>
        /// Gets an Enumerator of KeyValuePairs representing the VectorClock.
        /// </summary>
        /// <returns>The Enumerator</returns>
        public IEnumerator<KeyValuePair<Guid, int>> GetEnumerator() => this.Vector.GetEnumerator();

        /// <summary>
        /// Gets the Enumerator for the VectorClock's Vector.
        /// </summary>
        /// <returns>The Enumerator</returns>
        IEnumerator IEnumerable.GetEnumerator() => (this.Vector as IEnumerable).GetEnumerator();

        /// <summary>
        /// Creates a Dictionary Copy of the IReadOnlyDictionary Vector.
        /// </summary>
        /// <returns>Copy of the vector</returns>
        private Dictionary<Guid, int> CopyVector()
        {
            return this.Vector.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }

        private const int GUID_LENGTH = 16;
        private const int INT_LENGTH = 4;
        private const int KEY_VALUE_LENGTH = GUID_LENGTH + INT_LENGTH;
        private const int DATETIME_LENGTH = 8;

        /// <summary>
        /// Converts the Vector represented by this VectorClock into a byte array.
        /// </summary>
        /// <returns>The vector as a byte array.</returns>
        public byte[] ToByteArray()
        {
            byte[] bytes = new byte[DATETIME_LENGTH + (KEY_VALUE_LENGTH) * this.Vector.Count];
            byte[] dateBytes = BitConverter.GetBytes(this.Timestamp.Ticks);
            Array.Copy(dateBytes, bytes, DATETIME_LENGTH);
            int i = DATETIME_LENGTH;
            foreach (var kvp in this.Vector)
            {
                byte[] guidBytes = kvp.Key.ToByteArray();
                byte[] intBytes = BitConverter.GetBytes(kvp.Value);
                Array.Copy(guidBytes, 0, bytes, i, GUID_LENGTH);
                Array.Copy(intBytes, 0, bytes, i + GUID_LENGTH, INT_LENGTH);
                i += KEY_VALUE_LENGTH;
            }

            return bytes;
        }

        private DateTime GetDate(byte[] vector)
        {
            long ticks = BitConverter.ToInt64(vector, 0);
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        /// <summary>
        /// Convert a byte array from <see cref="VectorClock.ToByteArray"/> back into a vector
        /// dictionary.
        /// </summary>
        /// <param name="bytes">The vector in byte[] format.</param>
        /// <returns>The vector as a <see cref="Dictionary{Guid, string}"/>.</returns>
        public static Dictionary<Guid, int> ConvertBytesToVector(byte[] bytes)
        {
            int count = (bytes.Length - DATETIME_LENGTH) / (KEY_VALUE_LENGTH);
            int remainder = (bytes.Length - DATETIME_LENGTH) % (KEY_VALUE_LENGTH);
            if (remainder != 0) throw new ArgumentException("Byte array is invalid", nameof(bytes));

            Dictionary<Guid, int> vector = new Dictionary<Guid, int>();
            byte[] guidBuffer = new byte[GUID_LENGTH];

            for (int i = DATETIME_LENGTH; i < bytes.Length; i += KEY_VALUE_LENGTH)
            {
                Array.Copy(bytes, i, guidBuffer, 0, GUID_LENGTH);
                Guid guid = new Guid(guidBuffer);
                int value = BitConverter.ToInt32(bytes, i + GUID_LENGTH);
                vector.Add(guid, value);
            }

            return vector;
        }
    }
}
