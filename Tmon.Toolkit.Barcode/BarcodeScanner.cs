using System;
using System.Collections.Generic;
using System.Windows.Input;
using Tmon.Toolkit.Barcode.Enums;
using Xamarin.Forms;

namespace Tmon.Toolkit.Barcode
{
    public class BarcodeScanner : ContentView
    {
        #region Bindable Properties
        public static readonly BindableProperty DelayBetweenDetectionsProperty =BindableProperty.Create(
                nameof(DelayBetweenDetections), 
                typeof(int), 
                typeof(BarcodeScanner), 
                1000, 
                BindingMode.OneWayToSource);

        public static readonly BindableProperty AutoFocusIntervalProperty =BindableProperty.Create(
            nameof(AutoFocusInterval), 
            typeof(int), 
            typeof(BarcodeScanner), 
            1500, 
            BindingMode.OneWay);

        public static readonly BindableProperty IsTorchOnProperty = BindableProperty.Create(
            nameof(IsTorchOn), 
            typeof(bool), 
            typeof(BarcodeScanner), 
            false, 
            BindingMode.TwoWay);

        public static readonly BindableProperty IsPreviewingProperty = BindableProperty.Create(
            nameof(IsPreviewing), 
            typeof(bool), 
            typeof(BarcodeScanner), 
            true, 
            BindingMode.TwoWay);

        public static readonly BindableProperty IsScanningProperty = BindableProperty.Create(
            nameof(IsScanning), 
            typeof(bool), 
            typeof(BarcodeScanner), 
            true, 
            BindingMode.TwoWay);

        public static readonly BindableProperty ResolutionProperty = BindableProperty.Create(
            nameof(Resolution), 
            typeof(Resolution), 
            typeof(BarcodeScanner), 
            Resolution.Medium, 
            BindingMode.OneWay);

        public static readonly BindableProperty BarcodeDetectedCommandProperty = BindableProperty.Create(
            nameof(BarcodeDetectedCommand), 
            typeof(ICommand), 
            typeof(BarcodeScanner), 
            null, 
            BindingMode.TwoWay);

        public static readonly BindableProperty ErrorOccuredCommandProperty = BindableProperty.Create(
            nameof(ErrorOccuredCommand), 
            typeof(ICommand), 
            typeof(BarcodeScanner), 
            null, 
            BindingMode.OneWay);
        #endregion

        #region Constructors
        public BarcodeScanner() : this(null) { }

        public BarcodeScanner(IEnumerable<BarcodeFormat> barcodeFormats)
        {
            BarcodeFormats = barcodeFormats;
        }
        #endregion

        /// <summary>
        /// 바코드 인식 후 발생 이벤트
        /// </summary>
        public event Action<BarcodeData> BarcodeDetected;
        /// <summary>
        /// 스캐너 에러 후 발생 이벤트
        /// </summary>
        public event Action<ScannerError> ErrorOccurred;

        /// <summary>
        /// 인식 가능 바코드 포멧 목록
        /// </summary>
        public IEnumerable<BarcodeFormat> BarcodeFormats { get; private set; }

        /// <summary>
        /// 바코드 인식 후 재인식 까지 지연 시간 설정(단위 : ms, 기본값 : 1000ms)
        /// </summary>
        public int DelayBetweenDetections
        {
            get { return (int)GetValue(DelayBetweenDetectionsProperty); }
            set { SetValue(DelayBetweenDetectionsProperty, value); }
        }

        /// <summary>
        /// 자동 초첨 간격을 설정(Android 전용, 단위 : ms, 기본값 : 1500ms)
        /// </summary>
        public int AutoFocusInterval
        {
            get { return (int)GetValue(AutoFocusIntervalProperty); }
            set { SetValue(AutoFocusIntervalProperty, value); }
        }
        
        /// <summary>
        /// 플래시 켬/꺼짐
        /// </summary>
        public bool IsTorchOn
        {
            get { return (bool)GetValue(IsTorchOnProperty); }
            set { SetValue(IsTorchOnProperty, value); }
        }

        /// <summary>
        /// 프리뷰 켬/꺼짐
        /// </summary>
        public bool IsPreviewing
        {
            get { return (bool)GetValue(IsPreviewingProperty); }
            set { SetValue(IsPreviewingProperty, value); }
        }

        /// <summary>
        /// 스캔 모드 켬/꺼짐
        /// </summary>
        public bool IsScanning
        {
            get { return (bool)GetValue(IsScanningProperty); }
            set { SetValue(IsScanningProperty, value); }
        }
        
        /// <summary>
        /// 프리뷰 해상도 (기본값 : High)
        /// </summary>
        public Resolution Resolution
        {
            get { return (Resolution)GetValue(ResolutionProperty); }
            set { SetValue(ResolutionProperty, value); }
        }
        
        /// <summary>
        /// 바코드 인식 후 실행 Command
        /// </summary>
        public ICommand BarcodeDetectedCommand
        {
            get { return (ICommand)GetValue(BarcodeDetectedCommandProperty); }
            set { SetValue(BarcodeDetectedCommandProperty, value); }
        }
        /// <summary>
        /// 에러 발생 시 실행 Command
        /// </summary>
        public ICommand ErrorOccuredCommand
        {
            get { return (ICommand)GetValue(ErrorOccuredCommandProperty); }
            set { SetValue(ErrorOccuredCommandProperty, value); }
        }
        
        /// <summary>
        /// 바코드 검출 시 호출되는 콜백 함수입니다.
        /// </summary>
        /// <param name="barcodeData">검출된 바코드 데이터</param>
        public void OnBarcodeDetected(BarcodeData barcodeData)
        {
            if (barcodeData != null)
            {
                BarcodeDetected?.Invoke(barcodeData);
                BarcodeDetectedCommand?.Execute(barcodeData);
            }
        }

        /// <summary>
        /// 에러 발생 시 호출되는 콜백 함수입니다.
        /// </summary>
        /// <param name="error">에러 내용</param>
        public virtual void OnError(ScannerError error)
        {
            ErrorOccurred?.Invoke(error);
            ErrorOccuredCommand?.Execute(error);
        }
    }
}
