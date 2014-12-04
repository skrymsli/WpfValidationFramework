using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Text;
using System.Threading.Tasks;

namespace ValidationFramework
{
    public interface ICanBeBusy : INotifyPropertyChanged
    {

        bool IsBusy { get; set; }

    }

    internal static class CanBeBusyExtensions
    {

        public static IDisposable SetBusy(this ICanBeBusy This)
        {
            if (This == null) return Disposable.Create(() => { });

            This.IsBusy = true;
            return Disposable.Create(() => This.IsBusy = false);
        }

    }


}
