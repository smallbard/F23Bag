using System;
using System.Collections.ObjectModel;

namespace F23Bag.Data.Mapping
{
    internal class CollectionActivator
    {
        public object CreateInstance(Type collectionType)
        {
            if (collectionType.IsInterface)
                return Activator.CreateInstance(typeof(ObservableCollection<>).MakeGenericType(collectionType.IsGenericType ? collectionType.GetGenericArguments()[0] : typeof(object)));
            else if (collectionType.IsAbstract)
                return LazyProxyGenerator.ProxyGenerator.CreateClassProxy(collectionType);

            return Activator.CreateInstance(collectionType);
        }

        public object CreateInstance(Type collectionType, System.Collections.IEnumerable items)
        {
            var collection = CreateInstance(collectionType);
            foreach (var item in items) ((System.Collections.IList)collection).Add(item);
            return collection;
        }
    }
}
