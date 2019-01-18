using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Sample
{
    public partial class BarcodePage : ContentPage
    {
        MainVM _mainVM = null;

        public BarcodePage()
        {
            InitializeComponent();

            _mainVM = this.BindingContext as MainVM;

            this.Appearing += BarcodePage_Appearing;
            this.Disappearing += BarcodePage_Disappearing;
        }

        private async void BlinkScanArea()
        {
            while (true)
            {
                await scanArea.FadeTo(0.0, 500);
                await scanArea.FadeTo(1.0, 500);

                if (!_mainVM.IsPreviewing)
                    break;
            }
        }

        private void BarcodePage_Appearing(object sender, EventArgs e)
        {   
            _mainVM.IsPreviewing = true;
            BlinkScanArea();
        }

        private void BarcodePage_Disappearing(object sender, EventArgs e)
        {
            _mainVM.IsPreviewing = false;
        }

        private async void Button_Clicked(object sender, EventArgs e)
            => await Navigation.PushAsync(new NextPage());
    }
}
