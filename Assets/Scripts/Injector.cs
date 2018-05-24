namespace Core.Injecting
{
    using System;
    using System.Linq;
    using System.Collections.Generic;

    using UnityEngine;

    using Core.Maybe;

    public delegate Maybe<IInjectingItem> CreationDelegate();

    public interface IInjectingItem
    {
        Maybe<T> Cast<T>() where T : class;
    }

    public interface IPool
    {
        Maybe<T> Get<T>(CreationDelegate _delegate) where T : class;
        void Returned<T>(Maybe<T> poolable) where T : class, IPoolable;
    }

    public interface IPoolable : IInjectingItem
    {
        bool CanBeReused();
        void PrepareForReuse(IPool fromPool);
        void Return();
    }

    public interface IPoolableBinding
    {
        IPool Pool { get; }
    }

    public interface IInjectionBinding : IPoolableBinding
    {
        Type ConcreteType { get; }
        Type InterfaceType { get; }
        CreationDelegate Manufacture { get; }
        void AddTo(Action<IInjectionBinding> _delegate);
        IInjectionBinding WithPool(IPool pool);
    }

    public struct Binding<I> : IInjectionBinding where I : class, IInjectingItem
    {
        Type IInjectionBinding.ConcreteType { get { return ConcreteType; } }
        Type IInjectionBinding.InterfaceType { get { return InterfaceType; } }
        CreationDelegate IInjectionBinding.Manufacture { get { return _delegate; } }

        public IPool Pool { get; private set; }

        private Type ConcreteType { get; set; }
        private Type InterfaceType { get; set; }
        private CreationDelegate _delegate;

        public static IInjectionBinding Make<T>(CreationDelegate _pattern) where T : class, IInjectingItem
        {
            return new Binding<I>() { ConcreteType = typeof(T), InterfaceType = typeof(I), _delegate = _pattern };
        }

        void IInjectionBinding.AddTo(Action<IInjectionBinding> _apply)
        {
            _apply(this);
        }

        IInjectionBinding IInjectionBinding.WithPool(IPool pool)
        {
            Pool = pool;
            return this;
        }
    }

    public class Injector : MonoBehaviour
    {
        static readonly List<IInjectionBinding> Registry = new List<IInjectionBinding>();

        private void Awake()
        {
            name = $"[{GetType().Name}]";
        }

        public static Maybe<T> Get<T>() where T : class => ExistingEntry<T>().TakeFrom(x => x.Pool != null ? x.Pool.Get<T>(x.Manufacture) : x.Manufacture().Cast<T>());

        public static void CreateBinding<Interface, Concrete>(CreationDelegate _creation) where Interface : class, IInjectingItem
                                                                                          where Concrete : class, IInjectingItem
        {
            ExistingEntry<Interface>().OnHasValue(x => Registry.Remove(x));
            Binding<Interface>.Make<Concrete>(_creation).AddTo(Registry.Add);
        }

        public static void CreatePoolableBinding<Interface, Concrete>(CreationDelegate _creation, IPool _pool) where Interface : class, IInjectingItem, IPoolable
                                                                                                               where Concrete  : class, IInjectingItem, IPoolable
        {
            ExistingEntry<Interface>().OnHasValue(x => Registry.Remove(x));
            Binding<Interface>.Make<Concrete>(_creation)
                              .WithPool(_pool)
                              .AddTo(Registry.Add);
        }

        public static void RegisterAsExistingPartOfCollection<T>(T item) where T : class, IInjectingItem
        {
            Binding<T>.Make<T>(() => new Maybe<IInjectingItem>(item)).AddTo(Registry.Add);
        }

        public static IEnumerable<Maybe<T>> GetCollection<T>() where T : class, IInjectingItem
        {
            return Registry.Where(x => x.InterfaceType == typeof(T)).Select(x => x.Manufacture().Cast<T>());
        }

        private static Maybe<IInjectionBinding> ExistingEntry<I>() where I : class
        {
            return Registry.FirstOrDefault(x => x.InterfaceType == typeof(I))
                           .ToMaybe();
        }
    }

}
