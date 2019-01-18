using Android.Content;
using Android.Content.PM;
using Android.Gms.Vision.Barcodes;

namespace Tmon.Toolkit.Barcode.Droid
{
    internal class Platform
    {
        /// <summary>
        ///  현재 실행된 앱의 Context 얻습니다.
        /// </summary>
        private static Context Context => Android.App.Application.Context;
        /// <summary>
        /// 현재 실행된 앱의 PackageManager를 얻습니다.
        /// </summary>
        private static PackageManager PackageManager => Android.App.Application.Context.PackageManager;
        /// <summary>
        /// 장치에 카메라가 있는지 확인합니다.
        /// </summary>
        public static bool HasCamera => HasFeature(PackageManager.FeatureCamera);
        /// <summary>
        /// 카메라 접근 권한이 부여됐는지 확인합니다.
        /// </summary>
        public static bool HasCameraPermission => HasPermission(Android.Manifest.Permission.Camera);
        /// <summary>
        /// Google Mobile Service 사용 가능 상태를 확인합니다.
        /// </summary>
        public static bool IsGmsReady => new BarcodeDetector.Builder(Context).Build().IsOperational;
        /// <summary>
        /// 접근 권한 부여 상태를 확인합니다.
        /// </summary>
        /// <param name="permission">권한명</param>
        /// <returns></returns>
        public static bool HasPermission(string permission) => Permission.Granted == PackageManager.CheckPermission(permission, Context.PackageName);
        /// <summary>
        /// 시스템이 해당 특성 소유를 확인합니다.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        public static bool HasFeature(string feature) => PackageManager.HasSystemFeature(feature);
    }
}

