using System;
using System.Collections.Generic;

namespace PriorityQueue
{
    public class PriorityQueue<T, TPriority>
    {
        private ValuePriority<T, TPriority>[] _heap = new ValuePriority<T, TPriority>[1];
        private readonly IComparer<TPriority> _comparer;
        private int _version = 0;
        public int Count { get; private set; } = 0;

        public PriorityQueue() : this(Comparer<TPriority>.Default)
        {
        }

        public PriorityQueue(IComparer<TPriority> comparer)
        {
            _comparer = comparer;
        }

        public void Enqueue(T t, TPriority priority)
        {
            if (Count == _heap.Length)
            {
                ExpandHeap();
            }

            _heap[Count] = new ValuePriority<T, TPriority>(t, priority);
            BubbleUp(Count);
            Count++;
            _version++;
        }

        public T Peek()
        {
            return PeekWithPriority().Value;
        }

        public TPriority PeekPriority()
        {
            return PeekWithPriority().Priority;
        }

        public ValuePriority<T, TPriority> PeekWithPriority()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("The priority queue is empty");
            }

            return _heap[0];
        }

        public T Dequeue()
        {
            return DequeueWithPriority().Value;
        }

        public ValuePriority<T, TPriority> DequeueWithPriority()
        {
            if (Count == 0)
            {
                throw new InvalidOperationException("The priority queue is empty");
            }

            var top = _heap[0];
            _heap[0] = _heap[Count - 1];
            Count--;
            BubbleDown(0);

            _version++;
            return top;
        }

        public bool Contains(T value)
        {
            throw new NotImplementedException();
        }

        public bool TryIndexOf(T value, out ValuePriorityIndex index)
        {
            throw new NotImplementedException();
        }

        public void UpdateValue(T value, TPriority newPriority)
        {
            ValuePriorityIndex valueIndex;
            if (!TryIndexOf(value, out valueIndex))
            {
                throw new Exception("The value is not in the priority queue.");
            }

            UpdateValueFromIndex(valueIndex, newPriority);
        }

        public void UpdateValueFromIndex(ValuePriorityIndex pIndex, TPriority newPriority)
        {
            if (pIndex.Version != _version)
            {
                throw new InvalidOperationException("The priority queue was modified after the index was retrieved.");
            }

            int index = pIndex.Index;
            _heap[index] = new ValuePriority<T, TPriority>(_heap[index].Value, newPriority);
            _version++;
        }

        private void ExpandHeap()
        {
            Array.Resize(ref _heap, Count * 2 + 1);
        }

        private void BubbleUp(int childIndex)
        {
            while (!IsRoot(childIndex))
            {
                int parentIndex = Parent(childIndex);
                var child = _heap[childIndex];
                var parent = _heap[parentIndex];
                if (_comparer.Compare(child.Priority, parent.Priority) >= 0)
                {
                    break;
                }

                _heap[parentIndex] = child;
                _heap[childIndex] = parent;
                childIndex = parentIndex;
            }
        }

        private void BubbleDown(int parentIndex)
        {
            while (!IsLeaf(parentIndex))
            {
                int leftChildIndex = LeftChild(parentIndex);
                int rightChildIndex = RightChild(parentIndex);
                if (leftChildIndex >= Count)
                {
                    break;
                }

                int bestChildIndex = leftChildIndex;
                //If both childs are valid then the best child
                //between the two will be chosen. Otherwise the
                //old valid child will be the best child.
                if (rightChildIndex < Count)
                {
                    var leftChild = _heap[leftChildIndex];
                    var rightChild = _heap[rightChildIndex];

                    if (_comparer.Compare(leftChild.Priority, rightChild.Priority) > 0)
                    {
                        bestChildIndex = rightChildIndex;
                    }
                }

                var bestChild = _heap[bestChildIndex];
                var parent = _heap[parentIndex];
                if (_comparer.Compare(parent.Priority, bestChild.Priority) <= 0)
                {
                    break;
                }

                _heap[parentIndex] = bestChild;
                _heap[bestChildIndex] = parent;
                parentIndex = bestChildIndex;
            }
        }

        private int LeftChild(int index)
        {
            return (2 * index) + 1;
        }

        private int RightChild(int index)
        {
            return (2 * index) + 2;
        }

        private int Parent(int index)
        {
            return (index - 1) / 2;
        }

        private bool IsLeaf(int index)
        {
            return (index >= Count / 2) && (index < Count);
        }

        private bool IsRoot(int index)
        {
            return index == 0;
        }
    }
}
