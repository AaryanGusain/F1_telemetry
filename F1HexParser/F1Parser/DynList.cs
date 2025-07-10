using System.Collections.Generic;
using System.Collections;

namespace F1Parser
{
    public class DynList<T> : List<T>
    {
        public DynList() : base() { }
        public DynList(int capacity) : base(capacity) { }
        public IEnumerable<T> AsEnumerable() => this;
    }
} 