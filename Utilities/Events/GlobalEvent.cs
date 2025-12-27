using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GazeStream.Utilities.Events
{
    public class GlobalEvent
    {
        private event Action action = delegate { };

        public void Invoke()
        {
            action?.Invoke();
        }

        public void Add(Action subscriber)
        {
            action -= subscriber;
            action += subscriber;
        }

        public void Remove(Action subscriber)
        {
            action -= subscriber;
        }
    }

    public class GlobalEvent<T>
    {
        private event Action<T>? action;

        public void Invoke(T param)
        {
            action?.Invoke(param);
        }

        public void Add(Action<T> subscriber)
        {
            action -= subscriber;
            action += subscriber;
        }

        public void Remove(Action<T> subscriber)
        {
            action -= subscriber;
        }
    }
}
