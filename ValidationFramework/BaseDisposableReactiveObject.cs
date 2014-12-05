using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReactiveUI;

namespace ValidationFramework
{

    public interface ICompositeDisposable : IDisposable
    {

        CompositeDisposable Disposables { get; }

    }

    /// <summary>
    /// Marker interface which allows us to hang extension methods off of
    /// </summary>
    internal interface IHaveDisposables
    {

    }

    public abstract class BaseReactiveObject : ReactiveObject
    {

        protected BaseReactiveObject()
        {
            // HACK: This is to work around https://github.com/reactiveui/ReactiveUI/issues/667
            var ignoredChanging = Changing;
            var ignoredChanged = Changed;
        }

    }

    public abstract class BaseDisposableReactiveObject : BaseReactiveObject, ICompositeDisposable, IHaveDisposables
    {

        protected BaseDisposableReactiveObject()
        {
            // HACK: This is to work around https://github.com/reactiveui/ReactiveUI/issues/667
            var ignoredChanging = Changing;
            var ignoredChanged = Changed;
        }

        private const int DisposedFlag = 1;

        private readonly CompositeDisposable _disposables = new CompositeDisposable();

        private int _isDisposed;

        public bool IsDisposed
        {
            get
            {
                Interlocked.MemoryBarrier();
                return _isDisposed == DisposedFlag;
            }
        }

        public CompositeDisposable Disposables
        {
            get { return _disposables; }
        }

        public void Dispose()
        {
            var isDisposed = _isDisposed;
            Interlocked.CompareExchange(ref _isDisposed, DisposedFlag, isDisposed);
            if (isDisposed != DisposedFlag)
            {
                if (Disposables != null)
                {
                    Disposables.Dispose();
                }
                Dispose(true);
                GC.SuppressFinalize(this);
            }
        }

        protected virtual void Dispose(bool disposing) { }

    }
}
