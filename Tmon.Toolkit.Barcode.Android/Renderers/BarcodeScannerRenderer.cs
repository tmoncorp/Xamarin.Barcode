using System;
using Android.Views;
using System.ComponentModel;
using Tmon.Toolkit.Barcode;
using Tmon.Toolkit.Barcode.Droid.Renderers;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Android.Graphics;
using Android.Runtime;
using Android.Gms.Vision;
using Android.Gms.Vision.Barcodes;
using static Android.Gms.Vision.Detector;
using Tmon.Toolkit.Barcode.Droid.Extensions;
using AndroidCamera = Android.Hardware.Camera;
using Tmon.Toolkit.Barcode.Log;
using Android.Content;
using Tmon.Toolkit.Barcode.Enums;

[assembly: ExportRenderer(typeof(BarcodeScanner), typeof(BarcodeScannerRenderer))]
namespace Tmon.Toolkit.Barcode.Droid.Renderers
{
    public class BarcodeScannerRenderer
        : ViewRenderer<BarcodeScanner, SurfaceView>,
        ISurfaceHolderCallback,
        IProcessor,
        ILog
    {
        public readonly string TorchOn = AndroidCamera.Parameters.FlashModeTorch;
        public readonly string TorchOff = AndroidCamera.Parameters.FlashModeOff;

        /// <summary>
        /// Gms Vision의 카메라 접근 및 활용 객체
        /// </summary>
        CameraSource CameraSource { get; set; }
        /// <summary>
        /// Gms Vision의 바코드 검출 객체
        /// </summary>
        BarcodeDetector BarcodeDetector { get; set; }
        /// <summary>
        /// Android.Hardware.Camera 객체(자동포커스 및 플래시 on/off에 사용)
        /// </summary>
        AndroidCamera Camera { get; set; }
        /// <summary>
        /// 자동 포커스 콜백 관리
        /// </summary>
        AutoFocusCallback AutoFocusCallback { get; set; }

        /// <summary>
        /// 생성자
        /// </summary>
        /// <param name="context"></param>
        public BarcodeScannerRenderer(Context context) : base(context) { }

        protected override void OnElementChanged(ElementChangedEventArgs<BarcodeScanner> e)
        {
            base.OnElementChanged(e);

            // 카메라 프리뷰를 출력하기 위한 뷰
            var surfaceView = new SurfaceView(Context);
            surfaceView.Holder.AddCallback(this);
            SetNativeControl(surfaceView);
            this.Write("SurfaceView is created");
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);

            // 스케너 사용 가능여부 확인
            if (!CheckScannerStatus())
                return;

            if (CameraSource == null && Element.Width > 0 && Element.Height > 0)
            {
                // 바코드 검출 객체 설정 및 콜백 등록
                BarcodeDetector = new BarcodeDetector
                    .Builder(Context)
                    .SetBarcodeFormats(Element.BarcodeFormats.ToVisionFormat())
                    .Build();
                BarcodeDetector.SetProcessor(this);
                this.Write("BarcodeDetector is created");

                // 프리뷰어 사이즈 얻기
                var previewerSize = Element.GetPreviewSize(Resources.DisplayMetrics);
                this.Write($"Previewer size - Width : {previewerSize.Width}, Height : {previewerSize.Height}");

                // 카메라 소스 생성
                CameraSource = new CameraSource
                    .Builder(Context, BarcodeDetector)
                    .SetFacing(CameraFacing.Back)
                    .SetRequestedPreviewSize(previewerSize.Width, previewerSize.Height)
                    .SetRequestedFps(10.0f)
                    .Build();
                this.Write("CameraSource is created");
            }

            switch (e.PropertyName)
            {
                case "IsTorchOn":
                    {
                        try
                        {
                            if (Element.IsScanning && Camera != null)
                            {
                                var parameters = Camera.GetParameters();
                                parameters.FlashMode = Element.IsTorchOn ? TorchOn : TorchOff;
                                Camera.SetParameters(parameters);

                                this.Write($"IsTorchOn : {Element.IsTorchOn}");
                            }
                        }
                        catch (Exception ex)
                        {
                            this.Write($"Camera exception : {ex.Message}, IsTorchOn : {Element.IsTorchOn}");
                        }
                        break;
                    }
                case "IsPreviewing":
                case "IsScanning":
                    {
                        try
                        {
                            if (Element.IsScanning && Element.IsPreviewing)
                            {
                                CameraSource?.Start(Control.Holder);
                                Camera = CameraSource?.GetCamera();
                                SetAutoFocusCallback();
                            }
                            else
                            {
                                Element.SetValue(BarcodeScanner.IsTorchOnProperty, false);
                                CameraSource?.Stop();
                            }
                        }
                        catch (Exception ex)
                        {
                            var toggleValue = e.PropertyName == "IsScanning"
                                ? Element.IsScanning
                                : Element.IsPreviewing;

                            this.Write($"CameraSource exception : {ex.Message}, {e.PropertyName} : {toggleValue}");
                        }

                        this.Write($"{e.PropertyName} : {Element.IsScanning}");
                        break;
                    }
            }
        }

        /// <summary>
        /// AutoFocusCallback 객체 생성 후, 카메라 객체에 설정합니다.
        /// </summary>
        private void SetAutoFocusCallback()
        {
            if (Camera == null)
            {
                this.Write("Camera instance is null.");
                return;
            }

            AutoFocusCallback = new AutoFocusCallback(Camera, Element.AutoFocusInterval);
        }

        /// <summary>
        /// 카메라 유무 및 권한, Google Play Service 상태를 확인 후 스케너 사용 가능 여부를 판단합니다.
        /// </summary>
        /// <returns>true : 사용 가능, false : 사용 불가</returns>
        private bool CheckScannerStatus()
        {
            if (!Platform.HasCameraPermission || !Platform.HasCamera || !Platform.IsGmsReady)
            {
                this.Write($"Disabled to use scanner - " +
                           $"Permission : {Platform.HasCameraPermission}, " +
                           $"HasCamera : {Platform.HasCamera}, " +
                           $"IsGmsReady : {Platform.IsGmsReady}");

                ScannerError error = ScannerError.Unknown;
                if (!Platform.HasCameraPermission)
                    error = ScannerError.NoCameraPermission;
                else if (!Platform.HasCamera)
                    error = ScannerError.NoCamera;
                else if (!Platform.IsGmsReady)
                    error = ScannerError.ServiceNotReady;

                Element?.OnError(error);

                return false;
            }
            else
                return true;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            CameraSource?.Release();
            BarcodeDetector?.Release();
            this.Write($"Disposed");
        }

        #region IProcessor Implementation
        private DateTime _preDetectionDt = DateTime.Now;
        /// <summary>
        /// 바코드 검출시 호출되는 콜백 함수입니다.
        /// </summary>
        /// <param name="detections">검출된 바코드(들)</param>
        public void ReceiveDetections(Detections detections)
        {
            var barcodes = detections.DetectedItems;
            var meta = detections.FrameMetadata;

            for (int i = 0; i < barcodes.Size(); i++)
            {
                int key = barcodes.KeyAt(i);
                var barcode = barcodes.Get(key) as Android.Gms.Vision.Barcodes.Barcode;

                if (barcode == null)
                    continue;

                // 검출 영역 범위 설정(비율에 맞게 변환)
                var ratioX = meta.Width / Element.Width;
                var ratioY = meta.Height / Element.Height;

                var area = Element.Content != null
                    ? Element.Content.Bounds
                    : Element.Bounds;

                var scanAreaLeft = area.Left * ratioX;
                var scanAreaRight = area.Right * ratioX;
                var scanAreaTop = area.Top * ratioY;
                var scanAreaBottom = area.Bottom * ratioY;

                // 검출 영역 내 스캔된 바코드 처리
                var boundingBox = barcode.BoundingBox;

                if (scanAreaLeft < boundingBox.Left
                    && scanAreaRight > boundingBox.Right
                    && scanAreaTop < boundingBox.Top
                    && scanAreaBottom > boundingBox.Bottom)
                {
                    // 지연 시간 동안 바코드 스캔 결과를 전달하지 않음
                    var ts = DateTime.Now.TimeOfDay - _preDetectionDt.TimeOfDay;
                    if (ts.TotalMilliseconds < Element.DelayBetweenDetections)
                        return;

                    _preDetectionDt = DateTime.Now;

                    var b = ((Android.Gms.Vision.Barcodes.Barcode)barcodes.ValueAt(i));
                    this.Write($"Barcode Detection, Format : {b.Format.ToTmonFormat()}, Data : {b.RawValue}");
                    Element?.OnBarcodeDetected(new BarcodeData(b.Format.ToTmonFormat(), b.RawValue));

                    return;
                }
            }
        }

        public void Release() { }
        #endregion

        #region ISurfaceHolderCallback Implementation
        /// <summary>
        /// 바코드 프리뷰어 영역이 생성될 때 호출되는 콜백 함수입니다.
        /// </summary>
        /// <param name="holder"></param>
        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                if (!Element.IsScanning || !Element.IsPreviewing)
                    return;

                CameraSource?.Start(holder);
                Camera = CameraSource?.GetCamera();
                this.Write($"CameraSource started");
            }
            catch (Exception ex)
            {
                this.Write($"CameraSource start exception : {ex.Message}");
            }
        }

        /// <summary>
        /// 바코드 프리뷰어 영역이 소멸될 때 호출되는 콜백 함수입니다.
        /// </summary>
        /// <param name="holder"></param>
        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            CameraSource?.Stop();
            Camera = null;
            Element.SetValue(BarcodeScanner.IsTorchOnProperty, false);
            this.Write($"CameraSource stopped");
        }

        /// <summary>
        /// 바코드 프리뷰어 영역이 변경될 때 호출되는 콜백 함수입니다.
        /// </summary>
        /// <param name="holder"></param>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height) 
            => SetAutoFocusCallback();
        #endregion
    }
}