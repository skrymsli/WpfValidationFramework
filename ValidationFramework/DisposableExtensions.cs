using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ValidationFramework
{
    internal static class DisposableExtensions
    {

        public static CompositeDisposable AddAll(this CompositeDisposable This, params IDisposable[] disposables)
        {
            return AddRange(This, disposables);
        }

        public static CompositeDisposable AddRange(this CompositeDisposable This, IEnumerable<IDisposable> disposables)
        {
            if (disposables == null) return This;

            var disposableItems = disposables.ToList();
            if (!disposableItems.Any()) return This;

            disposableItems.ForEach(This.Add);

            return This;
        }

        public static void CleanupDisposable<T>(this IHaveDisposables This, ref T disposable, T newDisposable = null)
            where T : class, IDisposable
        {
            if (This == null) return;

            var oldDisposable = Interlocked.Exchange(ref disposable, newDisposable);

            if (oldDisposable != null)
            {
                oldDisposable.Dispose();
            }
        }

        public static T ResetDisposable<T>(this SerialDisposable This)
            where T : class, IDisposable, new()
        {
            var disposable = new T();
            This.Disposable = disposable;
            return disposable;
        }

        public static IDisposable ResetDisposable(this SerialDisposable This, IDisposable disposable = null)
        {
            This.Disposable = disposable ?? Disposable.Empty;
            return disposable;
        }

        public static IDisposable MuteRxSubscription(this IHaveDisposables This, SerialDisposable disposable, Func<IDisposable> resubscribe)
        {
            resubscribe = resubscribe ?? (() => Disposable.Empty);

            if (disposable == null) return Disposable.Create(() => resubscribe());

            disposable.ResetDisposable(Disposable.Empty);
            return Disposable.Create(() => disposable.ResetDisposable(resubscribe()));
        }

        public static IDisposable MuteRxSubscription(this IHaveDisposables This, IDisposable disposable, Action resubscribe)
        {
            if (disposable != null) disposable.Dispose();
            return Disposable.Create(resubscribe);
        }

        public static void RegisterDisposable(this IDisposable This, ICompositeDisposable compositeDisposable)
        {
            Ensure.NotNull(compositeDisposable);

            compositeDisposable.Disposables.Add(This);
        }

        public static void RegisterDisposable(this IDisposable This, ref CompositeDisposable compositeDisposable)
        {
            Ensure.NotNull(compositeDisposable);

            compositeDisposable.Add(This);
        }

        public static void RegisterDisposable(this IEnumerable<IDisposable> This, ICompositeDisposable compositeDisposable)
        {
            This.ToList().ForEach(x => x.RegisterDisposable(compositeDisposable));
        }

        public static void RegisterDisposable(this ICompositeDisposable This, IDisposable disposable)
        {
            This.Disposables.Add(disposable);
        }

        public static void RegisterDisposable(this ICompositeDisposable This, params IDisposable[] disposables)
        {
            disposables.ToList().ForEach(This.Disposables.Add);
        }

        public static void RegisterDisposable(this IDisposable This, ref SingleAssignmentDisposable disposable)
        {
            Ensure.NotNull(disposable);
            disposable.Disposable = This;
        }

        public static void RegisterDisposable(this IDisposable This, ref SerialDisposable disposable)
        {
            Ensure.NotNull(disposable);
            disposable.Disposable = This;
        }
    }
}
