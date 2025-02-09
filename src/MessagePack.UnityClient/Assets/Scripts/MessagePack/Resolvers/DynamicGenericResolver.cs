﻿// Copyright (c) All contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !UNITY_2018_3_OR_NEWER

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using MessagePack.Formatters;
using MessagePack.Internal;

#if !UNITY_2018_3_OR_NEWER
using System.Threading.Tasks;
#endif

#pragma warning disable SA1403 // File may only contain a single namespace

namespace MessagePack.Resolvers
{
    public sealed class DynamicGenericResolver : IFormatterResolver
    {
        /// <summary>
        /// The singleton instance that can be used.
        /// </summary>
        public static readonly DynamicGenericResolver Instance = new DynamicGenericResolver();

        private DynamicGenericResolver()
        {
        }

        public IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            public static readonly IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                Formatter = (IMessagePackFormatter<T>)DynamicGenericResolverGetFormatterHelper.GetFormatter(typeof(T));
            }
        }
    }
}

namespace MessagePack.Internal
{
    internal static class DynamicGenericResolverGetFormatterHelper
    {
        private static readonly Dictionary<Type, Type> FormatterMap = new Dictionary<Type, Type>()
        {
              { typeof(List<>), typeof(ListFormatter<>) },
              { typeof(LinkedList<>), typeof(LinkedListFormatter<>) },
              { typeof(Queue<>), typeof(QeueueFormatter<>) },
              { typeof(Stack<>), typeof(StackFormatter<>) },
              { typeof(HashSet<>), typeof(HashSetFormatter<>) },
              { typeof(ReadOnlyCollection<>), typeof(ReadOnlyCollectionFormatter<>) },
              { typeof(IList<>), typeof(InterfaceListFormatter<>) },
              { typeof(ICollection<>), typeof(InterfaceCollectionFormatter<>) },
              { typeof(IEnumerable<>), typeof(InterfaceEnumerableFormatter<>) },
              { typeof(Dictionary<,>), typeof(DictionaryFormatter<,>) },
              { typeof(IDictionary<,>), typeof(InterfaceDictionaryFormatter<,>) },
              { typeof(SortedDictionary<,>), typeof(SortedDictionaryFormatter<,>) },
              { typeof(SortedList<,>), typeof(SortedListFormatter<,>) },
              { typeof(ILookup<,>), typeof(InterfaceLookupFormatter<,>) },
              { typeof(IGrouping<,>), typeof(InterfaceGroupingFormatter<,>) },
#if !UNITY_2018_3_OR_NEWER
              { typeof(ObservableCollection<>), typeof(ObservableCollectionFormatter<>) },
              { typeof(ReadOnlyObservableCollection<>), typeof(ReadOnlyObservableCollectionFormatter<>) },
              { typeof(IReadOnlyList<>), typeof(InterfaceReadOnlyListFormatter<>) },
              { typeof(IReadOnlyCollection<>), typeof(InterfaceReadOnlyCollectionFormatter<>) },
              { typeof(ISet<>), typeof(InterfaceSetFormatter<>) },
              { typeof(System.Collections.Concurrent.ConcurrentBag<>), typeof(ConcurrentBagFormatter<>) },
              { typeof(System.Collections.Concurrent.ConcurrentQueue<>), typeof(ConcurrentQueueFormatter<>) },
              { typeof(System.Collections.Concurrent.ConcurrentStack<>), typeof(ConcurrentStackFormatter<>) },
              { typeof(ReadOnlyDictionary<,>), typeof(ReadOnlyDictionaryFormatter<,>) },
              { typeof(IReadOnlyDictionary<,>), typeof(InterfaceReadOnlyDictionaryFormatter<,>) },
              { typeof(System.Collections.Concurrent.ConcurrentDictionary<,>), typeof(ConcurrentDictionaryFormatter<,>) },
              { typeof(Lazy<>), typeof(LazyFormatter<>) },
#endif
        };

        // Reduce IL2CPP code generate size(don't write long code in <T>)
        internal static object GetFormatter(Type t)
        {
            TypeInfo ti = t.GetTypeInfo();

            if (t.IsArray)
            {
                var rank = t.GetArrayRank();
                if (rank == 1)
                {
                    if (t.GetElementType() == typeof(byte))
                    {
                        // byte[] is also supported in builtin formatter.
                        return ByteArrayFormatter.Instance;
                    }

                    return Activator.CreateInstance(typeof(ArrayFormatter<>).MakeGenericType(t.GetElementType()));
                }
                else if (rank == 2)
                {
                    return Activator.CreateInstance(typeof(TwoDimensionalArrayFormatter<>).MakeGenericType(t.GetElementType()));
                }
                else if (rank == 3)
                {
                    return Activator.CreateInstance(typeof(ThreeDimensionalArrayFormatter<>).MakeGenericType(t.GetElementType()));
                }
                else if (rank == 4)
                {
                    return Activator.CreateInstance(typeof(FourDimensionalArrayFormatter<>).MakeGenericType(t.GetElementType()));
                }
                else
                {
                    return null; // not supported built-in
                }
            }
            else if (ti.IsGenericType)
            {
                Type genericType = ti.GetGenericTypeDefinition();
                TypeInfo genericTypeInfo = genericType.GetTypeInfo();
                var isNullable = genericTypeInfo.IsNullable();
                Type nullableElementType = isNullable ? ti.GenericTypeArguments[0] : null;

                if (genericType == typeof(KeyValuePair<,>))
                {
                    return CreateInstance(typeof(KeyValuePairFormatter<,>), ti.GenericTypeArguments);
                }
                else if (isNullable && nullableElementType.GetTypeInfo().IsConstructedGenericType() && nullableElementType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                }
#if !UNITY_2018_3_OR_NEWER
                else if (isNullable && nullableElementType.IsConstructedGenericType && nullableElementType.GetGenericTypeDefinition() == typeof(ValueTask<>))
                {
                    return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                }

                // Tuple
                else if (ti.FullName.StartsWith("System.Tuple"))
                {
                    Type tupleFormatterType = null;
                    switch (ti.GenericTypeArguments.Length)
                    {
                        case 1:
                            tupleFormatterType = typeof(TupleFormatter<>);
                            break;
                        case 2:
                            tupleFormatterType = typeof(TupleFormatter<,>);
                            break;
                        case 3:
                            tupleFormatterType = typeof(TupleFormatter<,,>);
                            break;
                        case 4:
                            tupleFormatterType = typeof(TupleFormatter<,,,>);
                            break;
                        case 5:
                            tupleFormatterType = typeof(TupleFormatter<,,,,>);
                            break;
                        case 6:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,>);
                            break;
                        case 7:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,>);
                            break;
                        case 8:
                            tupleFormatterType = typeof(TupleFormatter<,,,,,,,>);
                            break;
                        default:
                            break;
                    }

                    return CreateInstance(tupleFormatterType, ti.GenericTypeArguments);
                }

                // ValueTuple
                else if (ti.FullName.StartsWith("System.ValueTuple"))
                {
                    Type tupleFormatterType = null;
                    switch (ti.GenericTypeArguments.Length)
                    {
                        case 1:
                            tupleFormatterType = typeof(ValueTupleFormatter<>);
                            break;
                        case 2:
                            tupleFormatterType = typeof(ValueTupleFormatter<,>);
                            break;
                        case 3:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,>);
                            break;
                        case 4:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,>);
                            break;
                        case 5:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,>);
                            break;
                        case 6:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,,>);
                            break;
                        case 7:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,,,>);
                            break;
                        case 8:
                            tupleFormatterType = typeof(ValueTupleFormatter<,,,,,,,>);
                            break;
                        default:
                            break;
                    }

                    return CreateInstance(tupleFormatterType, ti.GenericTypeArguments);
                }

#endif

                // ArraySegement
                else if (genericType == typeof(ArraySegment<>))
                {
                    if (ti.GenericTypeArguments[0] == typeof(byte))
                    {
                        return ByteArraySegmentFormatter.Instance;
                    }
                    else
                    {
                        return CreateInstance(typeof(ArraySegmentFormatter<>), ti.GenericTypeArguments);
                    }
                }
                else if (isNullable && nullableElementType.GetTypeInfo().IsConstructedGenericType() && nullableElementType.GetGenericTypeDefinition() == typeof(ArraySegment<>))
                {
                    if (nullableElementType == typeof(ArraySegment<byte>))
                    {
                        return new StaticNullableFormatter<ArraySegment<byte>>(ByteArraySegmentFormatter.Instance);
                    }
                    else
                    {
                        return CreateInstance(typeof(NullableFormatter<>), new[] { nullableElementType });
                    }
                }

                // Mapped formatter
                else
                {
                    Type formatterType;
                    if (FormatterMap.TryGetValue(genericType, out formatterType))
                    {
                        return CreateInstance(formatterType, ti.GenericTypeArguments);
                    }

                    // generic collection
                    else if (ti.GenericTypeArguments.Length == 1
                          && ti.ImplementedInterfaces.Any(x => x.GetTypeInfo().IsConstructedGenericType() && x.GetGenericTypeDefinition() == typeof(ICollection<>))
                          && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                    {
                        Type elemType = ti.GenericTypeArguments[0];
                        return CreateInstance(typeof(GenericCollectionFormatter<,>), new[] { elemType, t });
                    }

                    // generic dictionary
                    else if (ti.GenericTypeArguments.Length == 2
                          && ti.ImplementedInterfaces.Any(x => x.GetTypeInfo().IsConstructedGenericType() && x.GetGenericTypeDefinition() == typeof(IDictionary<,>))
                          && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                    {
                        Type keyType = ti.GenericTypeArguments[0];
                        Type valueType = ti.GenericTypeArguments[1];
                        return CreateInstance(typeof(GenericDictionaryFormatter<,,>), new[] { keyType, valueType, t });
                    }
                }
            }
            else
            {
                // NonGeneric Collection
                if (t == typeof(IList))
                {
                    return NonGenericInterfaceListFormatter.Instance;
                }
                else if (t == typeof(IDictionary))
                {
                    return NonGenericInterfaceDictionaryFormatter.Instance;
                }

                if (typeof(IList).GetTypeInfo().IsAssignableFrom(ti) && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                {
                    return Activator.CreateInstance(typeof(NonGenericListFormatter<>).MakeGenericType(t));
                }
                else if (typeof(IDictionary).GetTypeInfo().IsAssignableFrom(ti) && ti.DeclaredConstructors.Any(x => x.GetParameters().Length == 0))
                {
                    return Activator.CreateInstance(typeof(NonGenericDictionaryFormatter<>).MakeGenericType(t));
                }
            }

            return null;
        }

        private static object CreateInstance(Type genericType, Type[] genericTypeArguments, params object[] arguments)
        {
            return Activator.CreateInstance(genericType.MakeGenericType(genericTypeArguments), arguments);
        }
    }
}

#endif
