using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Navigation;
using Dell.Service.Client.Api.Dto;
using ReactiveUI;

namespace ValidationFramework
{
    class PersonViewModel : ValidatedViewModel
    {

        public PersonViewModel()
        {
            
        }


        #region Property Person
        private User _person = default(User);
        public User Person
        {
            get { return _person; }
            private set { this.RaiseAndSetIfChanged(ref _person, value); }
        }
        #endregion

        protected override Task InitializationDelegate()
        {
            Person = new User
            {
                FirstName = "Cody Batt",
                Id = 38,
                EmailAddress = "Inmate #666"
            };

            Observable.FromEventPattern<PropertyChangedEventHandler, PropertyChangedEventArgs>(
                x => Person.PropertyChanged += x,
                x => Person.PropertyChanged -= x)
                .Subscribe(x =>
                {
                    this.RaisePropertyChanged(x.EventArgs.PropertyName);
                });

            Person.Validate(x => x.FirstName)
                .AddRule(x => x == "Cody", x => "Name is " + x + ", but should be Cody");

            return Task.FromResult<object>(null);
        }

        protected override async Task<bool> CommitDelegate()
        {
            await Task.Delay(TimeSpan.FromSeconds(4));
            return true;
        }

    }
}
