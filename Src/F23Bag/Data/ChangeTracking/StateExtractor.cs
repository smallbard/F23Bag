using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.Data.ChangeTracking
{
    /// <summary>
    /// Extract the state of an object in an object array, used in change tracking.
    /// </summary>
    internal class StateExtractor
    {
        private static readonly Dictionary<ISQLMapping, Dictionary<Type, StateExtractor>> _stateExtractors = new Dictionary<ISQLMapping, Dictionary<Type, StateExtractor>>();
        private Func<object, Dictionary<object, State>, StateElement[]> _extractState;

        private StateExtractor() { }

        public Dictionary<object, State> GetAllComponentStates(object o)
        {
            var states = new Dictionary<object, State>();
            GetState(o, states);
            return states;
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

        private State GetState(object o, Dictionary<object, State> states)
        {
            if (o == null) return null;
            if (states.ContainsKey(o)) return states[o];
            return states[o] = new State(o, _extractState(o, states));
        }

        private void Initialize(Type entityType, ISQLMapping sqlMapping)
        {
            var entityParameter = Expression.Parameter(typeof(object));
            var statesParameter = Expression.Parameter(typeof(Dictionary<object, State>));
            var arrayInitializers = new List<Expression>();
            var stateElementCtor = typeof(StateElement).GetConstructors()[0];

            foreach (var property in sqlMapping.GetMappedSimpleProperties(entityType).Union(entityType.GetProperties().Where(p => p.PropertyType != typeof(string) && p.PropertyType.IsClass && p.GetCustomAttribute<TransientAttribute>() == null)))
            {
                if (!property.PropertyType.IsClass || property.PropertyType == typeof(string))
                    arrayInitializers.Add(Expression.New(stateElementCtor, Expression.Constant(property), Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(entityParameter, entityType), property), typeof(object))));
                else if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    var elementType = property.PropertyType.GetGenericArguments()[0];
                    var stateExtractor = GetStateExtractor(sqlMapping, elementType);

                    var select = typeof(Enumerable).GetMethods().First(m => m.Name == nameof(Enumerable.Select) && m.GetParameters().Length == 2 && m.GetParameters()[1].ParameterType.GetMethod("Invoke").GetParameters().Length == 1)
                        .MakeGenericMethod(typeof(object), typeof(object));
                    var ofType = typeof(Enumerable).GetMethod(nameof(Enumerable.OfType)).MakeGenericMethod(typeof(object));
                    var toArray = typeof(Enumerable).GetMethod(nameof(Enumerable.ToArray)).MakeGenericMethod(typeof(object));

                    var oParemeter = Expression.Parameter(typeof(object));

                    arrayInitializers.Add(
                        Expression.New(
                            stateElementCtor,
                            Expression.Constant(property),
                            Expression.Call(
                                toArray,
                                Expression.Call(
                                    select,
                                    Expression.Call(ofType,
                                        Expression.Coalesce(
                                            Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(entityParameter, entityType), property), typeof(IEnumerable<>).MakeGenericType(elementType)),
                                            Expression.Convert(Expression.Constant(Array.CreateInstance(elementType, 0)), typeof(IEnumerable<>).MakeGenericType(elementType)))),
                                    Expression.Lambda<Func<object, State>>(
                                            Expression.Call(Expression.Constant(stateExtractor), typeof(StateExtractor).GetMethod(nameof(GetState), BindingFlags.NonPublic | BindingFlags.Instance), oParemeter, statesParameter), oParemeter)))));
                }
                else
                {
                    var stateExtractor = GetStateExtractor(sqlMapping, property.PropertyType);
                    arrayInitializers.Add(Expression.New(
                        stateElementCtor, 
                        Expression.Constant(property), 
                        Expression.Call(Expression.Constant(stateExtractor), typeof(StateExtractor).GetMethod(nameof(GetState), BindingFlags.NonPublic | BindingFlags.Instance), Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(entityParameter, entityType), property), typeof(object)), statesParameter)));
                }
            }

            _extractState = Expression.Lambda<Func<object, Dictionary<object, State>, StateElement[]>>(Expression.NewArrayInit(typeof(StateElement), arrayInitializers.ToArray()), entityParameter, statesParameter).Compile();
        }
    }

    internal class StateElement
    {
        public StateElement(PropertyInfo property, object value)
        {
            Property = property;
            Value = value;
        }

        public PropertyInfo Property { get; private set; }

        public object Value { get; private set; }
    }

    internal class State
    {
        public State(object stateOwner, StateElement[] stateElements)
        {
            StateOwner = stateOwner;
            StateElements = stateElements;
        }

        public object StateOwner { get; private set; }

        public StateElement[] StateElements { get; private set; }

        public IEnumerable<PropertyInfo> GetChangedProperties(State newState)
        {
            for (var i = 0; i < StateElements.Length; i++)
            {
                var value = StateElements[i].Value;

                if (value is State)
                {
                    if (((State)value).GetChangedProperties((State)newState.StateElements[i].Value).Any())
                        yield return StateElements[i].Property;
                }
                else if (value is object[])
                {
                    var collectionInitialState = (object[])value;
                    var collectionNewState = (object[])newState.StateElements[i].Value ?? new object[] { };

                    if (collectionInitialState.Length != collectionNewState.Length)
                        yield return StateElements[i].Property;
                    else
                    {
                        for (var collectionIndex = 0; collectionIndex < collectionInitialState.Length; collectionIndex++)
                        {
                            var collectionItemInitialState = (State)collectionInitialState[collectionIndex];
                            var collectionItemNewState = (State)collectionNewState[collectionIndex];

                            if (collectionItemInitialState.GetChangedProperties(collectionItemNewState).Any())
                            {
                                yield return StateElements[i].Property;
                                break;
                            }
                        }
                    }
                }
                else if (!object.Equals(value, newState.StateElements[i].Value))
                    yield return StateElements[i].Property;
            }
        }
    }
}
