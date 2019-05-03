using System;
using System.Collections.Generic;
using System.Text;

namespace PriorityQueue
{
    public readonly struct ValuePriority<T, TPriority>
    {
        public readonly T Value;
        public readonly TPriority Priority;

        public ValuePriority(T value, TPriority priority)
        {
            this.Value = value;
            this.Priority = priority;
        }

        public override string ToString()
        {
            return $"[Value: {Value}, Priority: {Priority}]";
        }
    }
}
