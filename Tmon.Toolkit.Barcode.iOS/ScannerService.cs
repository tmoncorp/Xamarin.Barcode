using System;
using Tmon.Toolkit.Barcode.iOS.Extensions;
using Tmon.Toolkit.Barcode.Log;
using System.Collections.Generic;
using AVFoundation;
using UIKit;
using CoreGraphics;
using CoreFoundation;
using Foundation;
using System.Linq;
using Xamarin.Forms;

namespace Tmon.Toolkit.Barcode.iOS
{
    public class ScannerService : ILog, IDisposable
    {
        public event Action<BarcodeData> BarcodeDetected;
        public event Action OrientationUpdated;

        /// <summary>
        /// 카메라 권한 확인
        /// </summary>
        public static bool IsCameraAuthorized
            => AVCaptureDevice.GetAuthorizationStatus(AVMediaType.Video) == AVAuthorizationStatus.Authorized 
               ? true 
               : false;
        
        /// <summary>
        /// 장치 방향
        /// </summary>
        /// <returns></returns>
        private AVCaptureVideoOrientation DeviceOrientation
            => (AVCaptureVideoOrientation)UIApplication.SharedApplication.StatusBarOrientation;
        /// <summary>
        /// 카메라 프리뷰어
        /// </summary>
        public AVCaptureVideoPreviewLayer Previewer { get; private set; }
        /// <summary>
        /// 바코드 검출 대리자
        /// </summary>
        CaptureDelegate CaptureDelegate { get; set; }
        /// <summary>
        /// 장치 방향 관찰자
        /// </summary>
        NSObject OrientationObserver { get; set; }
        /// <summary>
        /// 캡처할 디바이스(후면 카메라)
        /// </summary>
        AVCaptureDevice Device { get; set; }
        /// <summary>
        /// 캡처 장치 입력값(후면 카메라 영상)
        /// </summary>
        AVCaptureDeviceInput Input { get; set; }
        /// <summary>
        /// 캡처 장치 출력값(검출된 바코드)
        /// </summary>
        AVCaptureMetadataOutput Output { get; set; }
        /// <summary>
        /// 캡처 세션
        /// </summary>
        AVCaptureSession Session { get; set; }
        /// <summary>
        /// 스캔 영역 설정 여부
        /// </summary>
        public bool IsScanAreaUpdated { get; private set; }

        /// <summary>
        /// 바코드 스캐너 서비스를 생성합니다.
        /// </summary>
        /// <param name="barcodeFormat">인식할 바코드 포멧</param>
        /// <param name="sessionPreset">해상도</param>
        /// <returns></returns>
        public static ScannerService Create(AVMetadataObjectType barcodeFormat, NSString sessionPreset)
        {
            var ss = new ScannerService();
            var result = ss.InitScanner(barcodeFormat, sessionPreset);

            if (result)
                return ss;
            else
            {
                ss.Dispose();
                return null;
            }
        }

        /// <summary>
        /// 스캐너를 초기화합니다.
        /// </summary>
        /// <param name="barcodeFormat">인식할 바코드 포멧</param>
        /// <param name="sessionPreset">해상도</param>
        /// <returns></returns>
        private bool InitScanner(AVMetadataObjectType barcodeFormat, NSString sessionPreset)
        {
            // 카메라 접근 권한 확인
            if (!IsCameraAuthorized)
            {
                this.Write("카메라 사용이 허용되지 않습니다.");
                return false;
            }

            // 후면 카메라를 캡처할 장치로 설정
            Device = AVCaptureDevice
                .DevicesWithMediaType(AVMediaType.Video)
                .FirstOrDefault(e => e.Position == AVCaptureDevicePosition.Back);
            if (Device == null)
            {
                this.Write("후면 카메라가 없습니다.");
                return false;
            }

            // 입력 설정
            Input = AVCaptureDeviceInput.FromDevice(Device);
            if (Input == null)
            {
                this.Write("AVCaptureDeviceInput이 null 입니다.");
                return false;
            }

            // 출력 설정
            CaptureDelegate = new CaptureDelegate((metadataObjects) =>
            {
                if (BarcodeDetected == null)
                    return;

                foreach (var metadata in metadataObjects)
                {
                    var data = ((AVMetadataMachineReadableCodeObject)metadata).StringValue;
                    BarcodeDetected?.Invoke(new BarcodeData(metadata.Type.ToTmonFormat(), data));
                }
            });
            Output = new AVCaptureMetadataOutput();
            Output.SetDelegate(CaptureDelegate, DispatchQueue.MainQueue);

            // 세션 설정
            Session = new AVCaptureSession()
            {
                SessionPreset = sessionPreset,
            };
            Session.AddInput(Input);
            Session.AddOutput(Output);

            // 검출할 바코드 포멧 설정(중요 : 반드시 세션 설정 뒤에 와야함)
            Output.MetadataObjectTypes = barcodeFormat;

            // 프리뷰어 설정
            Previewer = AVCaptureVideoPreviewLayer.FromSession(Session);
            Previewer.Frame = CGRect.Empty;
            Previewer.VideoGravity = AVLayerVideoGravity.ResizeAspectFill;
            Previewer.Connection.VideoOrientation = DeviceOrientation;

            return true;
        }

        /// <summary>
        /// 스켄 가능 여부를 업데이트합니다.
        /// </summary>
        /// <param name="isScanning">true : 스캔 가능, false : 스캔 불가</param>
        public void UpdateScannerState(bool isScanning)
        {
            if (isScanning)
            {
                Session.StartRunning();
                Previewer.Connection.VideoOrientation = DeviceOrientation;
                OrientationObserver = NSNotificationCenter.DefaultCenter
                    .AddObserver(UIDevice.OrientationDidChangeNotification, UpdateOrientation);
            }
            else
            {
                Session.StopRunning();

                if (OrientationObserver != null)
                    NSNotificationCenter.DefaultCenter.RemoveObserver(OrientationObserver);
            }

            this.Write($"IsScanning : {isScanning}");
        }

        /// <summary>
        /// 장치 방향이 바뀔 경우 호출됩니다.
        /// </summary>
        /// <param name="notification"></param>
        private void UpdateOrientation(NSNotification notification) => OrientationUpdated?.Invoke();
        
        /// <summary>
        /// 프리뷰어 / 스캔 가능 영역을 설정합니다. 
        /// </summary>
        /// <param name="scanner"></param>
        public void UpdateScanArea(BarcodeScanner scanner)
        {
            Previewer.Frame = new CGRect(0, 0, scanner.Width, scanner.Height);

            if (Previewer.Connection.SupportsVideoOrientation)
                Previewer.Connection.VideoOrientation = DeviceOrientation;

            this.Write($"Previewer frame changed : {Previewer.Frame}");

            UpdateRectOfInterest(scanner);
            IsScanAreaUpdated = true;
        }
        
        /// <summary>
        /// 스캔 가능 영역을 설정합니다.
        /// </summary>
        /// <param name="scanner"></param>
        private void UpdateRectOfInterest(BarcodeScanner scanner)
        {
            // todo : 하드코딩 아름답게 빼는 방법은????
            // BarcodeScannerRender에서 View를 감싼 Grid가 존재할 경우 
            // [0] : 프리뷰어, [1] : 스캔영역
            var interestArea = (scanner.Content as Grid)?.Children[1];
            if (interestArea == null)
                return;

            double x = 0.0,
                   y = 0.0,
                   height = 1.0,
                   width = 1.0;

            // Orientation 따른 정규화된 좌표계로 변환 : Min(0,0), Max(1,1)
            switch (DeviceOrientation)
            {
                case AVCaptureVideoOrientation.Portrait:
                    {
                        x = interestArea.Y / scanner.Height;
                        y = (scanner.Width - (interestArea.X + interestArea.Width)) / scanner.Width;
                        width = interestArea.Height / scanner.Height;
                        height = interestArea.Width / scanner.Width;
                    }
                    break;
                case AVCaptureVideoOrientation.PortraitUpsideDown:
                    {
                        x = (scanner.Height - (interestArea.Y + interestArea.Height)) / scanner.Height;
                        y = (scanner.Width - (interestArea.X + interestArea.Width)) / scanner.Width;
                        width = interestArea.Height / scanner.Height;
                        height = interestArea.Width / scanner.Width;
                    }
                    break;
                case AVCaptureVideoOrientation.LandscapeRight:
                    {
                        x = interestArea.X / scanner.Width;
                        y = interestArea.Y / scanner.Height;
                        width = interestArea.Width / scanner.Width;
                        height = interestArea.Height / scanner.Height;
                    }
                    break;
                case AVCaptureVideoOrientation.LandscapeLeft:
                    {
                        x = (scanner.Width - (interestArea.X + interestArea.Width)) / scanner.Width;
                        y = (scanner.Height - (interestArea.Y + interestArea.Height)) / scanner.Height;
                        width = interestArea.Width / scanner.Width;
                        height = interestArea.Height / scanner.Height;
                    }
                    break;
            }

            if (Output != null)
                Output.RectOfInterest = new CGRect(x, y, width, height);
            else
                this.Write($"[UpdateRectOfInterest] AVCaptureMetadataOutput(Output) is null!");
        }

        /// <summary>
        /// 플래시 상태를 변경합니다.
        /// </summary>
        /// <param name="isTorchOn">true : 켬, false : 끔</param>
        public void UpdateTorch(bool isTorchOn)
        {
            if (!Device.TorchAvailable)
            {
                this.Write($"Torch is not available.");
                return;
            }

            Device.LockForConfiguration(out NSError error);
            if (error != null)
            {
                this.Write($"Torch error : Description - {error.DebugDescription}, ErrorCode : {error.Code}");
                return;
            }
            Device.TorchMode = isTorchOn ? AVCaptureTorchMode.On : AVCaptureTorchMode.Off;
            Device.UnlockForConfiguration();
        }

        /// <summary>
        /// 프리뷰어 상태를 변경합니다.
        /// </summary>
        /// <param name="isRunning">true : 동작, false : 멈춤</param>
        public void ChangeSessionState(bool isRunning)
        {
            if (isRunning)
                Session?.StartRunning();
            else
                Session?.StopRunning();
        }

        public void Dispose()
        {
            Device = null;
            Input = null;
            Output = null;
            Session = null;
            Previewer = null;
            BarcodeDetected = null;
            OrientationUpdated = null;
            CaptureDelegate = null;
        }
    }

    /// <summary>
    /// 캡처된 장치의 출력값을 받는 대리자 클래스
    /// </summary>
    class CaptureDelegate : AVCaptureMetadataOutputObjectsDelegate
    {
        public Action<IEnumerable<AVMetadataObject>> OnCapture { get; set; }

        public CaptureDelegate(Action<IEnumerable<AVMetadataObject>> onCapture)
        {
            OnCapture = onCapture;
        }

        /// <summary>
        /// 출력값이 있을 경우 호출되는 콜백 함수입니다.
        /// </summary>
        /// <param name="captureOutput">출력값</param>
        /// <param name="metadataObjects"></param>
        /// <param name="connection"></param>
        public override void DidOutputMetadataObjects(
            AVCaptureMetadataOutput captureOutput,
            AVMetadataObject[] metadataObjects,
            AVCaptureConnection connection)
        {
            if (OnCapture != null && metadataObjects != null)
                OnCapture(metadataObjects);
        }
    }
}
