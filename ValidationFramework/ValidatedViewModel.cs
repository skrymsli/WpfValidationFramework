using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Input;
using Dell.Service.Client.Dto;
using FluentValidation;
using ReactiveUI;

namespace ValidationFramework
{

//    public interface IValidationInfo<in TProp>
//    {
//        IEnumerable<string> Validate(TProp value);
//    }
//
//    public class ValidationInfo<TObj, TProp> : IValidationInfo<TProp>
//    {
//        private Expression<Func<TObj, TProp>> _expression;
//
//        private readonly List<KeyValuePair<Func<TProp, bool>, Func<TProp, string>>> _validationRules = 
//            new List<KeyValuePair<Func<TProp, bool>, Func<TProp, string>>>();
//
//        public ValidationInfo(Expression<Func<TObj, TProp>> expr)
//        {
//            _expression = expr;
//            PropertyName = Reflection.ExpressionToPropertyNames(_expression.Body);
//        }
//
//        public string PropertyName { get; private set; }
//
//        public ValidationInfo<TObj, TProp> AddRule(Func<TProp, bool> validationRule, Func<TProp,string> errorMessage)
//        {
//            _validationRules.Add(KvPair.New(validationRule, errorMessage));
//            return this;
//        }
//
//        public ValidationInfo<TObj, TProp> As<TOtherProp>(Expression<Func<TObj,TOtherProp>> propAlias)
//        {
//            PropertyName = Reflection.ExpressionToPropertyNames(propAlias.Body);
//            return this;
//        }
//
//        public IEnumerable<string> Validate(TProp value)
//        {
//            return _validationRules.Where(x => !x.Key(value)).Select(x => x.Value(value));
//        }
//
//    }


    public abstract class ValidatedViewModel : BaseDisposableReactiveObject, ICanValidate, ICanBeBusy
    {

        private readonly Dictionary<string, ICollection<string>>
            _validationErrors = new Dictionary<string, ICollection<string>>();

        protected ValidatedViewModel()
        {
            InitializeIsValidObservable();

            Changed.Subscribe(x =>
            {
                if (x.Sender == this &&
                    (x.PropertyName == "IsDirty" ||
                     x.PropertyName == "IsBusy"))
                    return;
                IsDirty = DirtyCheck();
            }).RegisterDisposable(this);
        }

        public async Task Initialize()
        {
            _validationRegistrations.Clear();
            using (this.SuppressChangeNotifications())
            {
                using (this.SetBusy())
                {
                    await InitializationDelegate();
                }
                IsDirty = false;
                Apply = CreateApplyCommand();
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

        public IObservable<bool> IsValidObservable { get; private set; }
//
        private void InitializeIsValidObservable()
        {
            var sub = _validationRegistrations.CountChanged
//                .Do(x => Debug.WriteLine("CountChanged"))
                .Select(
                    _ =>
                    {
                        if (!_validationRegistrations.Any()) return Observable.Return(true);
                        var items = _validationRegistrations.ToList();
                        var first = items.First();
                        items.Remove(first);
//                        first.ValidationObservable
//                            .Do(x => Debug.WriteLine("Validation Observable [{0}]: {1}", (object)first.GetType().Name, x));
                        var obs = first.ValidationObservable;
                        foreach (var item in items)
                        {
                            obs = obs.CombineLatest(
                                item.ValidationObservable
//                                .Do(x => Debug.WriteLine("Validation Observable [{0}]: {1}", (object)item.GetType().Name, x))
                                , (l, r) => l && r);
                        }
                        return obs;
                    })
                    .Switch()
                .Publish();
            sub.Connect();
            IsValidObservable = sub;
        }

        #region ICanValidate members

        readonly BehaviorSubject<bool> _validationBehaviorSubject = new BehaviorSubject<bool>(false);

        public IObservable<bool> ValidationObservable
        {
            get { return _validationBehaviorSubject; }
        }

        public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
        private void RaiseErrorsChanged(string propertyName)
        {
            if (ErrorsChanged != null)
                ErrorsChanged(this, new DataErrorsChangedEventArgs(propertyName));
            _validationBehaviorSubject.OnNext(!HasErrors);
        }

        public void ClearErrors(string propertyName)
        {
            if (!_validationErrors.ContainsKey(propertyName)) return;
            _validationErrors.Remove(propertyName);
            RaiseErrorsChanged(propertyName);
        }

        public void SetErrors(string propertyName, IList<string> errors)
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

        #region Property Apply
        private ReactiveCommand<object> _apply = default(ReactiveCommand<object>);
        public ReactiveCommand<object> Apply
        {
            get { return _apply; }
            set { this.RaiseAndSetIfChanged(ref _apply, value); }
        }
        #endregion

        ReactiveCommand<object> CreateApplyCommand()
        {
            var dirtyAndNotBusy = this.WhenAny(x => x.IsBusy, x => x.IsDirty,
                    (busy, dirty) => (!busy.GetValue()) && dirty.GetValue())
                    .Do(x => Debug.WriteLine("DirtyAndNotBusy: " + x.ToString()));

            var canExecuteApply = IsValidObservable
                .Do(x => Debug.WriteLine("ValidationObs: " + x.ToString()))
                .CombineLatest(dirtyAndNotBusy, (x, y) => x && y)
                .Do(x => Debug.WriteLine("CanExecute: " + x.ToString()));

            return ReactiveCommand.CreateAsyncTask(canExecuteApply,
                async param =>
                {
                    await Commit();
                    return param;
                });
        }

        ReactiveList<ICanValidate> _validationRegistrations = new ReactiveList<ICanValidate>();

        protected IDisposable RegisterValidation<TObj>(TObj toValidate, AbstractValidator<TObj> validator)
            where TObj : class, INotifyPropertyChanged, ICanValidate
        {
            if(validator == null) throw new ArgumentException("validator");
            if(toValidate == null) throw new ArgumentException("toValidate");

            if (_validationRegistrations.Contains(toValidate)) return Disposable.Empty;
            _validationRegistrations.Add(toValidate);
            if (Equals(toValidate, this))
            {
                validator.ValidateAsync(toValidate)
                    .ContinueWith(x => _validationBehaviorSubject.OnNext(!x.IsFaulted && x.Result.IsValid));
            }

            var changed = Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                x => toValidate.PropertyChanged += x,
                x => toValidate.PropertyChanged -= x);

            return changed.Subscribe(async x =>
            {
                IsDirty = true;
                var property = x.EventArgs.PropertyName;
                try
                {
                    var results = await validator.ValidateAsync(toValidate, property);
                    if (results.Errors.Any())
                    {
                        toValidate.SetErrors(property, results.Errors.Select(err=>err.ErrorMessage).ToList());
                    }
                    else
                    {
                        toValidate.ClearErrors(property);
                    }
                }
                catch (Exception ex)
                {
                    toValidate.SetErrors(property, new []{"Validation failed: " + ex.Message});
                }
            });
        }
    }
}
