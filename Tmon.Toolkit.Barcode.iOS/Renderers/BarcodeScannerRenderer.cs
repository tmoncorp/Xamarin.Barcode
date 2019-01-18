using System;
using System.ComponentModel;
using Tmon.Toolkit.Barcode;
using Tmon.Toolkit.Barcode.iOS.Renderers;
using Xamarin.Forms;
using Tmon.Toolkit.Barcode.iOS.Extensions;
using Tmon.Toolkit.Barcode.Log;
using Xamarin.Forms.Platform.iOS;
using UIKit;
using CoreGraphics;
using Foundation;

[assembly: ExportRenderer(typeof(BarcodeScanner), typeof(BarcodeScannerRenderer))]
namespace Tmon.Toolkit.Barcode.iOS.Renderers
{
    public class BarcodeScannerRenderer : ViewRenderer<BarcodeScanner, UIView>, ILog
    {
        /// <summary>
        /// Forms 앱에서 렌더러 인식을 위해 초기화 필요
        /// </summary>
        public static void Initialize() { }

        /// <summary>
        /// 스캐너 서비스 객체
        /// </summary>
        public ScannerService ScannerService { get; private set; }
        /// <summary>
        /// 앱 백그라운드 상태 여부 알림
        /// </summary>
        NSObject BackgroundNotification { get; set; }

        /// <summary>
        /// 장치 회전 시 발생하는 콜백 함수입니다.
        /// </summary>
        private void OnOrientationUpdated() => ScannerService?.UpdateScanArea(Element);

        private DateTime _preDetectionDt = DateTime.Now;
        /// <summary>
        /// 바코드 검출 시 발생하는 콜백 함수입니다.
        /// </summary>
        /// <param name="barcodeData"></param>
        private void OnBarcodeDetected(BarcodeData barcodeData)
        {
            // 지연 시간 동안 바코드 스캔 결과를 전달하지 않음
            var ts = DateTime.Now.TimeOfDay - _preDetectionDt.TimeOfDay;
            if (ts.TotalMilliseconds < Element.DelayBetweenDetections)
                return;

            _preDetectionDt = DateTime.Now;

            Element?.OnBarcodeDetected(barcodeData);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<BarcodeScanner> e)
        {
            base.OnElementChanged(e);

            if (Control != null || Element == null)
                return;

            // 카메라 권한 확인
            if (!ScannerService.IsCameraAuthorized)
            {
                this.Write($"Accessing camera is not permitted!");
                Element?.OnError(Enums.ScannerError.NoCameraPermission);
                return;
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            
            if (ScannerService == null
                && Element.Width > 0
                && Element.Height > 0)
            {
                // ScannerService 생성
                ScannerService = ScannerService.Create(
                    Element.BarcodeFormats.ToAVFFormat(),
                    Element.Resolution.ToSessionPreset());

                // 스캐너 서비스 미생성 시 반환
                if (ScannerService == null)
                {
                    Element?.OnError(Enums.ScannerError.ServiceNotReady);
                    this.Write("Scanner service is not ready.");
                    return;
                }

                // 이벤트 등록
                ScannerService.BarcodeDetected += OnBarcodeDetected;
                ScannerService.OrientationUpdated += OnOrientationUpdated;

                // 스캔 초기 상태 설정
                ScannerService.UpdateScannerState(Element.IsScanning);

                // Previewer 생성 및 Forms.View에 추가
                var previewer = new UIView(CGRect.Empty);
                previewer.BackgroundColor = UIColor.Black;
                previewer.Layer.AddSublayer(ScannerService.Previewer);

                // ScanArea로 사용될 View(Element.Content)가 존재할 경우 Previewer위에 오버레이
                if (Element.Content != null)
                {
                    var g = new Grid();
                    g.Children.Add(previewer.ToView()); // [0]
                    g.Children.Add(Element.Content); // [1]
                    Element.Content = g;
                }
                else
                    Element.Content = previewer.ToView();

                // 앱이 백그라운드 모드로 전환 시 플래쉬가 자동으로 꺼짐
                // 따라서, 'IsTorchOn' 프로퍼티 값을 'false'로 동기화
                BackgroundNotification = UIApplication.Notifications.ObserveDidEnterBackground((x, z) =>
                {   
                    if (Element?.IsTorchOn == true)
                        Element.SetValue(BarcodeScanner.IsTorchOnProperty, false);
                });
            }

            // 스캐너 서비스 미생성 시 반환
            if (ScannerService == null)
                return;
            
            // 스캔 영역 설정
            if (!ScannerService.IsScanAreaUpdated && (e.PropertyName == "Width" || e.PropertyName == "Height"))
                ScannerService.UpdateScanArea(Element);

            switch (e.PropertyName)
            {
                case "IsTorchOn":
                    {
                        ScannerService.UpdateTorch(Element.IsTorchOn);
                        break;
                    }
                case "IsScanning":
                    {
                        ScannerService.UpdateScannerState(Element.IsScanning);

                        if (!Element.IsScanning)
                            Element.SetValue(BarcodeScanner.IsTorchOnProperty, false);
                        break;
                    }
                case "IsPreviewing":
                    {
                        ScannerService.ChangeSessionState(Element.IsPreviewing);

                        if (!Element.IsPreviewing)
                            Element.SetValue(BarcodeScanner.IsTorchOnProperty, false);
                        break;
                    }
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (ScannerService == null)
                return;

            ScannerService.BarcodeDetected -= OnBarcodeDetected;
            ScannerService.OrientationUpdated -= OnOrientationUpdated;
            ScannerService.Dispose();
            BackgroundNotification.Dispose();
            this.Write($"Disposed");
        }
    }
}