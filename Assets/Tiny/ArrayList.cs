using System;

namespace Wargon.TinyEcs {
    internal class ArrayList<T> {
        private T[] buffer;
        public int capacity;
        public int Count;
        private readonly int ResizeStep;
        internal ArrayList(int size, int resizeStep = 16) {
            Count = 0;
            capacity = size;
            buffer = new T[capacity];
            this.ResizeStep = resizeStep;
        }

        public ref T this[int index] {
            get => ref buffer[index];
        }
        public void Add(T item) {
            if (capacity <= Count) {
                capacity += ResizeStep;
                Array.Resize(ref buffer, capacity);
            }

            buffer[Count++] = item;
        }

        public void Clear(bool full = false) {
            Count = 0;
            if(full) Array.Clear(buffer, 0, buffer.Length);
        }
        public ref T Last() {
            return ref buffer[Count - 1];
        }
        public void RemoveLast() {
            Count--;
        }

        public Span<T> AsSpan() {
            return buffer;
        }
    }
}