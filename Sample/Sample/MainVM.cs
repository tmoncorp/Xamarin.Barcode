using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Input;
using Tmon.Toolkit.Barcode;
using Tmon.Toolkit.Barcode.Enums;
using Xamarin.Forms;

namespace Sample
{
    public partial class MainVM : INotifyPropertyChanged
    {
        private string _BarcodeString = string.Empty;
        public string BarcodeString
        {
            get { return _BarcodeString; }
            set
            {
                _BarcodeString = value;
                RaisePropertyChanged(nameof(BarcodeString));
            }
        }

        private bool _IsTorchOn = false;
        public bool IsTorchOn
        {
            get { return _IsTorchOn; }
            set
            {
                _IsTorchOn = value;
                RaisePropertyChanged(nameof(IsTorchOn));
            }
        }

        private bool _IsPreviewing = true;
        public bool IsPreviewing
        {
            get { return _IsPreviewing; }
            set
            {
                _IsPreviewing = value;
                RaisePropertyChanged(nameof(IsPreviewing));
            }
        }

        private ICommand _BarcodeDetectedCommand;
        public ICommand BarcodeDetectedCommand
        {
            get
            {
                return _BarcodeDetectedCommand ?? (_BarcodeDetectedCommand = new Command<BarcodeData>((e) =>
                {
                    Random rd = new Random();
                    BarcodeString = $"{@rd.Next(100, 999)}@ {e.Data}";
                }));
            }
        }

        private ICommand _ErrorOccuredCommand;
        public ICommand ErrorOccuredCommand
        {
            get
            {
                return _ErrorOccuredCommand ?? (_ErrorOccuredCommand = new Command<ScannerError>((error) =>
                {
                    Console.WriteLine($"Barcode Scanner Error : {error.ToString()}");
                }));
            }
        }
    }

    public partial class MainVM : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
