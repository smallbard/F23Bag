using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data.ChangeTracking
{
    /// <summary>
    /// Extract the state of an object in an object array, used in change tracking.
    /// </summary>
    internal class StateExtractor
    {
        private static readonly Dictionary<ISQLMapping, Dictionary<Type, StateExtractor>> _stateExtractors = new Dictionary<ISQLMapping, Dictionary<Type, StateExtractor>>();
        private static readonly MethodInfo _tupleCreate = typeof(Tuple).GetMethods().First(m => m.Name == nameof(Tuple.Create) && m.GetParameters().Length == 2).MakeGenericMethod(typeof(PropertyInfo), typeof(object));
        private Func<object, Tuple<PropertyInfo, object>[]> _extractState;

        private StateExtractor() { }

        public Tuple<PropertyInfo, object>[] GetState(object o)
        {
            if (o == null) return null;
            return _extractState(o);
        }

        public static StateExtractor GetStateExtractor(ISQLMapping sqlMapping, Type entityType)
        {
            lock (_stateExtractors)
            {
                if (!_stateExtractors.ContainsKey(sqlMapping)) _stateExtractors[sqlMapping] = new Dictionary<Type, StateExtractor>();
                var extractors = _stateExtractors[sqlMapping];
                if (!extractors.ContainsKey(entityType))
                {
                    var se = new StateExtractor();
                    extractors[entityType] = se;
                    se.Initialize(entityType, sqlMapping);

                }
                return extractors[entityType];
            }
        }

        private void Initialize(Type entityType, ISQLMapping sqlMapping)
        {
            var entityParameter = Expression.Parameter(typeof(object));
            var arrayInitializers = new List<Expression>();

            foreach (var property in sqlMapping.GetMappedSimpleProperties(entityType).Union(entityType.GetProperties().Where(p => p.PropertyType != typeof(string) && p.PropertyType.IsClass && p.GetCustomAttribute<TransientAttribute>() == null)))
            {
                if (!property.PropertyType.IsClass || property.PropertyType == typeof(string))
                    arrayInitializers.Add(Expression.Call(_tupleCreate, Expression.Constant(property), Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(entityParameter, entityType), property), typeof(object))));
                else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    var elementType = property.PropertyType.GetGenericArguments()[0];
                    var stateExtractor = GetStateExtractor(sqlMapping, elementType);

                    var select = typeof(Enumerable).GetMethods().First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.GetMethod("Invoke").GetParameters().Length == 1)
                        .MakeGenericMethod(typeof(object), typeof(object));
                    var ofType = typeof(Enumerable).GetMethod(nameof(Enumerable.OfType)).MakeGenericMethod(typeof(object));
                    var toArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray)).MakeGenericMethod(typeof(object));

                    arrayInitializers.Add(
                        Expression.Call(
                            _tupleCreate,
                            Expression.Constant(property),
                            Expression.Call(
                                toArray,
                                Expression.Call(
                                    select,
                                    Expression.Call(ofType,
                                        Expression.Coalesce(
                                            Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(entityParameter, entityType), property), typeof(IEnumerable<>).MakeGenericType(elementType)),
                                            Expression.Convert(Expression.Constant(Array.CreateInstance(elementType, 0)), typeof(IEnumerable<>).MakeGenericType(elementType)))),
                                    Expression.Constant(typeof(StateExtractor).GetMethod(nameof(GetState)).CreateDelegate(typeof(Func<object, Tuple<PropertyInfo, object>[]>), stateExtractor))))));
                }
                else
                {
                    var stateExtractor = GetStateExtractor(sqlMapping, property.PropertyType);
                    arrayInitializers.Add(Expression.Call(_tupleCreate, Expression.Constant(property), Expression.Call(Expression.Constant(stateExtractor), typeof(StateExtractor).GetMethod(nameof(GetState)), Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(entityParameter, entityType), property), typeof(object)))));
                }
            }

            _extractState = Expression.Lambda<Func<object, Tuple<PropertyInfo, object>[]>>(Expression.NewArrayInit(typeof(Tuple<PropertyInfo, object>), arrayInitializers.ToArray()), entityParameter).Compile();
        }
    }
}
