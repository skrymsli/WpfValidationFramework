using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;

namespace ValidationFramework
{
    class PersonDto : ReactiveObject
    {
        #region Property Name
        private string _name = default(string);
        public string Name
        {
            get { return _name; }
            set { this.RaiseAndSetIfChanged(ref _name, value); }
        }
        #endregion

        #region Property Age
        private int _age = default(int);
        public int Age
        {
            get { return _age; }
            set { this.RaiseAndSetIfChanged(ref _age, value); }
        }
        #endregion

        #region Property SerialNumber
        private string _serialNumber = default(string);
        public string SerialNumber
        {
            get { return _serialNumber; }
            set { this.RaiseAndSetIfChanged(ref _serialNumber, value); }
        }
        #endregion
    }
}
