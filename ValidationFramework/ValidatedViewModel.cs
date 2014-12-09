using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using System.Windows.Input;
using Dell.Service.Client.Dto;
using FluentValidation;
using ReactiveUI;

namespace ValidationFramework
{

    public abstract class ValidatedViewModel : BaseDisposableReactiveObject, ICanValidate, ICanBeBusy
    {
        protected ValidatedViewModel()
        {
            InitializeIsValidObservable();
        }

        protected bool IsMetaProperty(string propertyName)
        {
            return propertyName == "CommitError" ||
                   propertyName == "Apply" ||
                   propertyName == "IsBusy" ||
                   propertyName == "IsDirty" ||
                   propertyName.Contains(".Changed");
        }

        #region Initialize

        public async Task Initialize()
        {
            _validationRegistrations.Clear();
            using (this.SetBusy())
            {
                await InitializationDelegate();
                if (Apply == null)
                {
                    Apply = CreateApplyCommand();
                }
                ValidationRegistrationDelegate().ForEach(x=>x.RegisterDisposable(this));
                IsDirty = false;
            }
            
        }

        protected virtual IEnumerable<IDisposable> ValidationRegistrationDelegate()
        {
            return new IDisposable[] {};
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

        protected abstract Task InitializationDelegate();
        #endregion

        #region Commit
        public async Task Commit()
        {
            using (this.SetBusy())
            {
                CommitError = string.Empty;
                try
                {
                    await CommitDelegate();
                }
                catch (Exception ex)
                {
                    CommitError = ex.Message;
                    return;
                }
                IsDirty = false;
            }
        }

        #region Property CommitError
        private string _commitError = default(string);
        public string CommitError
        {
            get { return _commitError; }
            set { this.RaiseAndSetIfChanged(ref _commitError, value); }
        }
        #endregion

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
            var dirtyAndNotBusy = this.Changed.Select(
                _ =>
                {
                    Debug.WriteLine("Dirty: {0}, Busy: {1}", IsDirty, IsBusy);
                    return !IsBusy && IsDirty;
                })
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
        
        protected abstract Task CommitDelegate();

        protected virtual bool DirtyCheck()
        {
            return true;
        }

        #region Property IsDirty
        private bool _isDirty = default(bool);
        public bool IsDirty
        {
            get { return _isDirty; }
            set
            {
                this.RaiseAndSetIfChanged(ref _isDirty, value);
            }
        }
        #endregion

        public IObservable<bool> IsValidObservable { get; private set; }
        
        private void InitializeIsValidObservable()
        {
            if (IsValidObservable != null) return;
            IsValidObservable = _validationRegistrations.CountChanged
                .Do(x => Debug.WriteLine("CountChanged"))
                .Where(x=>x > 0)
                .Select(
                    _ =>
                    {
                        var items = _validationRegistrations.ToList();
                        var first = items.First();
                        items.Remove(first);
                        first.ValidationObservable
                            .Do(
                                x =>
                                    Debug.WriteLine("Validation Observable [{0}]: {1}", (object) first.GetType().Name, x));
                        var obs = first.ValidationObservable;
                        foreach (var item in items)
                        {
                            obs = obs.CombineLatest(
                                item.ValidationObservable
                                    .Do(
                                        x =>
                                            Debug.WriteLine("Validation Observable [{0}]: {1}",
                                                (object) item.GetType().Name, x))
                                , (l, r) => l && r);
                        }
                        return obs;
                    })
                .StartWith(Observable.Return(true))
                .Switch();
            IsValidObservable.Subscribe(x => { }).RegisterDisposable(this);
        }
        #endregion

        #region ICanValidate members

        private readonly Dictionary<string, ICollection<string>>
            _validationErrors = new Dictionary<string, ICollection<string>>();

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

        #region Validation Registration

        readonly ReactiveList<ICanValidate> _validationRegistrations = new ReactiveList<ICanValidate>();

        protected IDisposable RegisterValidation<TObj>(TObj toValidate, AbstractValidator<TObj> validator)
            where TObj : class, INotifyPropertyChanged, ICanValidate
        {
            if (validator == null) throw new ArgumentException("validator");
            if (toValidate == null) throw new ArgumentException("toValidate");

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
                
                var property = x.EventArgs.PropertyName;
                
                if(Equals(x.Sender, this) && 
                    (property == "IsBusy" ||
                    property == "IsDirty")) return;

                IsDirty = DirtyCheck();
                try
                {
                    var results = await validator.ValidateAsync(toValidate, property);
                    if (results.Errors.Any())
                    {
                        toValidate.SetErrors(property, results.Errors.Select(err => err.ErrorMessage).ToList());
                    }
                    else
                    {
                        toValidate.ClearErrors(property);
                    }
                }
                catch (Exception ex)
                {
                    toValidate.SetErrors(property, new[] { "Validation failed: " + ex.Message });
                }
            });
        }
        #endregion
    }
}

