using Plugin.Permissions;
using Plugin.Permissions.Abstractions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace Sample
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class PreviousPage : ContentPage
    {
        public PreviousPage()
        {
            InitializeComponent();
        }

        private async void Button_Clicked(object sender, EventArgs e)
        {
            var cameraPermission = new CameraPermission(CrossPermissions.Current);
            var result = await cameraPermission.RequestCameraPermissionIfNeeded();

            if (result)
            {
                if (Navigation.NavigationStack.LastOrDefault() is BarcodePage == false)
                    await Navigation.PushAsync(new BarcodePage());
            }
            else
                await DisplayAlert("오류", "카메라 접근 권한 없음", "확인");
        }

        public class CameraPermission
        {
            private readonly IPermissions permissions;

            public CameraPermission(IPermissions permissions)
            {
                this.permissions = permissions;
            }

            public async Task<bool> RequestCameraPermissionIfNeeded()
            {
                var status = await permissions.CheckPermissionStatusAsync(Permission.Camera);
                if (status != PermissionStatus.Granted)
                {
                    var results = await permissions.RequestPermissionsAsync(new[] { Permission.Camera });

                    status = results[Permission.Camera];
                }

                return status == PermissionStatus.Granted;
            }
        }
    }
}