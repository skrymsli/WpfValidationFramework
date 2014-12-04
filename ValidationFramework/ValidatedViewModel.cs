using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using Dell.Service.Client.Dto;
using ReactiveUI;

namespace ValidationFramework
{

    public interface IValidationInfo<in TProp>
    {
        IEnumerable<string> Validate(TProp value);
    }

    public class ValidationInfo<TObj, TProp> : IValidationInfo<TProp>
    {
        private Expression<Func<TObj, TProp>> _expression;

        private readonly List<KeyValuePair<Func<TProp, bool>, Func<TProp, string>>> _validationRules = 
            new List<KeyValuePair<Func<TProp, bool>, Func<TProp, string>>>();

        public ValidationInfo(Expression<Func<TObj, TProp>> expr)
        {
            _expression = expr;
            PropertyName = Reflection.ExpressionToPropertyNames(_expression.Body);
        }

        public string PropertyName { get; private set; }

        public ValidationInfo<TObj, TProp> AddRule(Func<TProp, bool> validationRule, Func<TProp,string> errorMessage)
        {
            _validationRules.Add(KvPair.New(validationRule, errorMessage));
            return this;
        }

        public ValidationInfo<TObj, TProp> As<TOtherProp>(Expression<Func<TObj,TOtherProp>> propAlias)
        {
            PropertyName = Reflection.ExpressionToPropertyNames(propAlias.Body);
            return this;
        }

        public IEnumerable<string> Validate(TProp value)
        {
            return _validationRules.Where(x => !x.Key(value)).Select(x => x.Value(value));
        }

    }


    public abstract class ValidatedViewModel : ReactiveObject, INotifyDataErrorInfo, ICanBeBusy
    {

        private readonly Dictionary<string, ICollection<string>>
            _validationErrors = new Dictionary<string, ICollection<string>>();

        //private readonly Dictionary<string, IValidationInfo> _validationInfos =
        //    new Dictionary<string, IValidationInfo>();

        protected ValidatedViewModel()
        {
            this.Changed.Subscribe(x =>
            {
                if (x.Sender == this &&
                    (x.PropertyName == "IsDirty" ||
                     x.PropertyName == "IsBusy"))
                    return;
                IsDirty = DirtyCheck();
            });
        }

        public async Task Initialize()
        {
            using (this.SuppressChangeNotifications())
            {
                using (this.SetBusy())
                {

                    await InitializationDelegate();
                }
                IsDirty = false;
            }
        }

        public async Task<bool> Commit()
        {
            using (this.SetBusy())
            {
                var result = await CommitDelegate();
                if(result) IsDirty = false;
                return result;
            }
        }

        protected abstract Task InitializationDelegate();
        protected abstract Task<bool> CommitDelegate();

        protected virtual bool DirtyCheck()
        {
            return true;
        }

        #region INotifyDataErrorInfo members
        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            if (ErrorsChanged != null)
                ErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));
            _validationBehaviorSubject.OnNext(!HasErrors);
        }

        private void ClearErrors(string propertyName)
        {
            if (!_validationErrors.ContainsKey(propertyName)) return;
            _validationErrors.Remove(propertyName);
            RaiseErrorsChanged(propertyName);
        }

        private void PutErrors(string propertyName, IList<string> errors)
        {
            if (_validationErrors.ContainsKey(propertyName))
                _validationErrors.Remove(propertyName);
            _validationErrors.Add(propertyName, new Collection<string>(errors));
            RaiseErrorsChanged(propertyName);
        }

        public System.Collections.IEnumerable GetErrors(string propertyName)
        {
            if (string.IsNullOrEmpty(propertyName)
                || !_validationErrors.ContainsKey(propertyName))
                return null;

            return _validationErrors[propertyName];
        }

        public bool HasErrors
        {
            get { return _validationErrors.Count > 0; }
        }
        #endregion

        #region ICanBeBusy members
        #region Property IsBusy
        private bool _isBusy = default(bool);
        public bool IsBusy
        {
            get { return _isBusy; }
            set { this.RaiseAndSetIfChanged(ref _isBusy, value); }
        }
        #endregion
        #endregion

        #region Property IsDirty
        private bool _isDirty = default(bool);
        public bool IsDirty
        {
            get { return _isDirty; }
            set { this.RaiseAndSetIfChanged(ref _isDirty, value); }
        }
        #endregion

        readonly BehaviorSubject<bool> _validationBehaviorSubject = new BehaviorSubject<bool>(false);

        public IObservable<bool> ValidationObservable
        {
            get { return _validationBehaviorSubject; }
        }

        private ReactiveCommand<object> _reset;
        public ICommand Reset
        {
            get
            {
                if (_reset != null) return _reset;
                _reset = ReactiveCommand.CreateAsyncTask(Observable.Return(true),
                    async param =>
                    {
                        await Initialize();
                        return param;
                    });
                return _reset;
            }
        }

        private ReactiveCommand<object> _apply;
        public ICommand Apply
        {
            get
            {
                if (_apply != null) return _apply;

                var dirtyAndNotBusy = this.WhenAny(x => x.IsBusy, x => x.IsDirty,
                    (busy, dirty) => (!busy.GetValue()) && dirty.GetValue())
                    .Do(x=> Debug.WriteLine("DirtyAndNotBusy: " + x.ToString()));

                var canExecuteApply = ValidationObservable
                    .Do(x=>Debug.WriteLine("ValidationObs: " + x.ToString()))
                    .CombineLatest(dirtyAndNotBusy, (x,y) => x && y)
                    .Do(x=>Debug.WriteLine("CanExecute: " + x.ToString()));

                _apply = ReactiveCommand.CreateAsyncTask(canExecuteApply,
                    async param =>
                    {
                        await Commit();
                        return param;
                    });
                return _apply;
            }
        }

    }

    public static class ValidatedViewModelExtensions
    {
        public static ValidationInfo<TObj, TProp> 
            Validate<TObj, TProp>(this TObj This, Expression<Func<TObj, TProp>> expr) 
            where TObj : ICanValidate, INotifyPropertyChanged
        {
            var observable = This.WhenAnyValue(expr);
            return CreateValidationInfo(This, expr, observable);
        }

        private static ValidationInfo<TObj, TProp>
            CreateValidationInfo<TObj, TProp>(
            ICanValidate validator,
            Expression<Func<TObj, TProp>> expr,
            IObservable<TProp> propertyChanged)
        {
            var info = new ValidationInfo<TObj, TProp>(expr);
            propertyChanged.Subscribe(x =>
            {
                var errors = info.Validate(x).ToList();
                if (!errors.Any()) validator.ClearErrors(info.PropertyName);
                else validator.SetErrors(info.PropertyName, errors);
            });
            return info;
        }
    }

}
