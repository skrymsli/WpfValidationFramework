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
using FluentValidation;
using ReactiveUI;

namespace ValidationFramework
{

    public class UserValidator : AbstractValidator<User>
    {
        public UserValidator() {
            RuleFor(user => user.FirstName)
                .Equal("Cody")
                .WithName("Name")
                .WithMessage("{PropertyName} is {PropertyValue}, but should be {ComparisonValue}");

            RuleFor(user => user.Id)
                .LessThan(100)
                .GreaterThan(50)
                .WithMessage("{PropertyName} must be between 50 and 100");
        }
    }

    class PersonViewModelValidator : AbstractValidator<PersonViewModel>
    {
        public PersonViewModelValidator()
        {
            RuleFor(pvm => pvm.Person).NotNull();
        }
    }


    class PersonViewModel : ValidatedViewModel
    {

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

            RegisterValidation(this, new PersonViewModelValidator()).RegisterDisposable(this);
            RegisterValidation(Person, new UserValidator()).RegisterDisposable(this);
            
            return Task.FromResult<object>(null);
        }

        protected override async Task CommitDelegate()
        {
            await Task.Delay(TimeSpan.FromSeconds(4));
        }

    }
}
