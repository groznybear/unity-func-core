namespace Core.Maybe
{
    using System;
    using System.Collections.Generic;

    public struct Maybe<T> where T : class
    {
        public delegate void ActionOverObject(T content);
        public delegate void UnwrapDelegate(bool hasValue, T content, ExceptionOfMaybe<T> exce);

        public bool HasValue { get { return !IsNull(Value); } }

        private T Value;
        private ExceptionOfMaybe<T> Exception;


        /// <summary>
        /// Static wrapper
        /// </summary>
        /// <returns>Maybe with value</returns>
        /// <param name="obj">Object to wrap</param>
        public static Maybe<T> Wrap(T obj) => new Maybe<T>(obj);


        /// <summary>
        /// Initializes a new instance of the <see cref="T:Core.Maybe.Maybe`1"/> struct.
        /// Prefered constructor.
        /// </summary>
        /// <param name="content">Object to hold inside</param>
        public Maybe(T content) 
        {
            Value = content;
            Exception = new ExceptionOfMaybe<T>();

            Exception.Wrap(this);
        }
        /// <summary>
        /// Compare wrapped value with something
        /// </summary>
        /// <returns>The compare.</returns>
        /// <param name="comparison">What to compare?</param>
        public bool Compare(Func<T, bool> comparison) => HasValue ? comparison(Value) : HasValue;

        /// <summary>
        /// Compare two Maybes
        /// </summary>
        /// <returns>Comparison result</returns>
        /// <param name="another">Another Maybe to compare with</param>
        /// <param name="predicate">Predicate</param>
        public bool Compare(Maybe<T> another, Func<T, T, bool> predicate) => HasValue && another.HasValue && predicate(Value, another.Value);

        /// <summary>
        /// Casts wrapped value using AS to specified type.
        /// </summary>
        /// <returns>New Maybe with result of cast</returns>
        /// <typeparam name="R">Type to cast AS</typeparam>
        public Maybe<R> Cast<R>() where R : class => new Maybe<R>(Value as R);

        /// <summary>
        /// Will return replacement if value of this is NULL
        /// </summary>
        /// <returns>Specified replacement</returns>
        /// <param name="replacement">Replacement provider</param>
        public Maybe<T> ReplaceWithAnother(Func<Maybe<T>> replacement) => HasValue ? this : replacement();

        /// <summary>
        /// Returns selected value from wrapped value
        /// </summary>
        /// <returns>Selected value</returns>
        /// <param name="predicate">What to return?</param>
        /// <param name="defaultValue">If wrapped value is NULL, will return specified default value</param>
        /// <typeparam name="R">Type of value to return</typeparam>
        public R Return<R>(Func<T, R> predicate, R defaultValue = default(R)) => HasValue ? predicate(Value) : defaultValue;

        /// <summary>
        /// Expose all content of Maybe as is
        /// </summary>
        /// <returns>void</returns>
        /// <param name="unwrapDelegate">Unwrap handler</param>
        public void Unwrap(UnwrapDelegate unwrapDelegate) => unwrapDelegate(HasValue, Value, Exception);

        /// <summary>
        /// Perform action over wrapped value, if value not NULL or default
        /// </summary>
        /// <returns>Exception struct, which contains additional information</returns>
        /// <param name="action">Action over value</param>
        public ExceptionOfMaybe<T> OnHasValue(ActionOverObject action)
        {
            if (HasValue)
            {
                action(Value);
            }
            return Exception;
        }

        /// <summary>
        /// If there is no wrapped value
        /// </summary>
        /// <returns>This</returns>
        /// <param name="noValueAction">Action if value is NULL</param>
        public Maybe<T> OnNullValue(Action noValueAction)
        {
            if (!HasValue)
            {
                noValueAction();
            }
            return this;
        }

        /// <summary>
        /// Wrapped value will perform specified action after casting AS specified type
        /// </summary>
        /// <param name="action">Specified action over cast</param>
        /// <typeparam name="R">Type to cast to</typeparam>
        public void ActAs<R>(Action<R> action) where R : class
        {
            if (HasValue && !IsNull(Value as R))
                action(Value as R);
        }

        /// <summary>
        /// Pass both values in one method as separate parameters if BOTH of them has values.
        /// Otherwise, do nothing
        /// </summary>
        /// <param name="anotherMaybe">Another maybe</param>
        /// <param name="action">Action over two values</param>
        /// <typeparam name="R">The 1st type parameter.</typeparam>
        public void Merge<R>(Maybe<R> anotherMaybe, Action<T, R> action) where R : class
        {
            if(HasValue && anotherMaybe.HasValue)
            {
                action(Value, anotherMaybe.Value);
            }
        }

        /// <summary>
        /// Null check
        /// </summary>
        /// <returns><c>true</c>, if wrapped value is null, <c>false</c> otherwise.</returns>
        /// <param name="toCheck">Object to check</param>
        /// <typeparam name="R">Type of object</typeparam>
        private bool IsNull<R>(R toCheck) => EqualityComparer<R>.Default.Equals(toCheck, default(R)) || toCheck.Equals(null);
    }

    public class ExceptionOfMaybe<T> : Exception where T : class
    {
        public bool HasException { get; private set; } = false;
        public new string Message { get; private set; }

        public Maybe<T> Wrapped { get; private set; }

        public void Wrap(Maybe<T> maybe)
        {
            Wrapped = maybe;
            HasException = !maybe.HasValue;
            Message = $"Value of type {typeof(T).Name} is null";
        }

        public void OnExceptionExists(Action<ExceptionOfMaybe<T>> handler)
        {
            if(HasException)
            {
                handler(this);
            }
        }
    }

    public static class MaybeExtension
    {
        public static Maybe<T> ToMaybe<T>(this T obj) where T : class
        {
            return new Maybe<T>(obj);
        }

        public static Maybe<R> ToMaybe<T, R>(this T obj, System.Func<T, R> selector) where T : class where R : class
        {
            return new Maybe<R>(selector(obj));
        }

        public static R Apply<T, R>(this T obj, Func<T, R> action)
        {
            return action(obj);
        }

        public static Maybe<T> GetComponentAsMaybe<T>(this UnityEngine.GameObject source) where T : class
        {
            return new Maybe<T>(source?.GetComponent<T>());
        }

        public static Maybe<T> GetComponentAsMaybe<T>(this UnityEngine.MonoBehaviour source) where T : class
        {
            return new Maybe<T>(source?.GetComponent<T>());
        }
    }
}
