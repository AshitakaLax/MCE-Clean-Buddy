using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MCEBuddy.Util
{
    public class Collections
    {
        /// <summary>
        /// A queue with a fixes size, following a FIFO algorithm.
        /// As new objects are added to the queue, older ones are dropped from the queue when the queue size is full
        /// </summary>
        public class FixedSizeQueue<T> : Queue<T>
        {
            public readonly int maxQueueSize;

            /// <summary>
            /// Create a queue with a limited size buffer, anything greater will be discarded using a FIFO algorithm
            /// </summary>
            /// <param name="maxQueueSize">Maximum items in the queue</param>
            public FixedSizeQueue(int maxQueueSize)
            {
                this.maxQueueSize = maxQueueSize;
            }

            /// <summary>
            /// Add items to the queue in a thread safe way
            /// </summary>
            public new void Enqueue(T item)
            {
                lock (this)
                {
                    base.Enqueue(item);
                    while (base.Count > maxQueueSize)
                        base.Dequeue(); // Throw away
                }
            }
        }
    }
}
