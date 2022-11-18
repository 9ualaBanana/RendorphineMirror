namespace Common
{
    public interface IReadOnlyWeakEventManagerDelegate<T> where T : Delegate
    {
        void Subscribe(object obj, T action);
        void Unsubscribe(object obj, T action);
    }
    public interface IReadOnlyWeakEventManager : IReadOnlyWeakEventManagerDelegate<Action> { }
    public interface IReadOnlyWeakEventManager<T> : IReadOnlyWeakEventManagerDelegate<Action<T>> { }
    public interface IReadOnlyWeakEventManager<T1, T2> : IReadOnlyWeakEventManagerDelegate<Action<T1, T2>> { }

    public abstract class WeakEventManagerBase<T> : IReadOnlyWeakEventManagerDelegate<T> where T : Delegate
    {
        readonly Dictionary<WeakReference<object>, T> Dictionary = new Dictionary<WeakReference<object>, T>();

        protected IEnumerable<T> GetCallbacks()
        {
            foreach (var (wr, val) in Dictionary.ToArray())
            {
                if (!wr.TryGetTarget(out _))
                {
                    Dictionary.Remove(wr);
                    continue;
                }

                yield return val;
            }
        }

        public void Subscribe(object obj, T action) => Dictionary.Add(new WeakReference<object>(obj), action);
        public void Unsubscribe(object obj, T action)
        {
            var wr = Dictionary.FirstOrDefault(x => x.Value == action);
            if (wr is { Key: { }, Value: { } }) Dictionary.Remove(wr.Key);
        }
    }
    public class WeakEventManager : WeakEventManagerBase<Action>, IReadOnlyWeakEventManager
    {
        public void Invoke()
        {
            foreach (var callback in GetCallbacks())
                callback();
        }
    }
    public class WeakEventManager<T> : WeakEventManagerBase<Action<T>>, IReadOnlyWeakEventManager<T>
    {
        public void Invoke(T value)
        {
            foreach (var callback in GetCallbacks())
                callback(value);
        }
    }
    public class WeakEventManager<T1, T2> : WeakEventManagerBase<Action<T1, T2>>, IReadOnlyWeakEventManager<T1, T2>
    {
        public void Invoke(T1 value1, T2 value2)
        {
            foreach (var callback in GetCallbacks())
                callback(value1, value2);
        }
    }
}