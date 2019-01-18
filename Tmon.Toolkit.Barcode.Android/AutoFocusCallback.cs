using System;
using System.Timers;
using Tmon.Toolkit.Barcode.Droid.Extensions;
using Tmon.Toolkit.Barcode.Log;
using AndroidCamera = Android.Hardware.Camera;

namespace Tmon.Toolkit.Barcode.Droid
{
    /// <summary>
    /// 일정 시간 간격으로 카메라 자동 포커스 설정을 돕는 콜백 클래스입니다.
    /// </summary>
    internal class AutoFocusCallback :
        Java.Lang.Object,
        AndroidCamera.IAutoFocusCallback,
        ILog
    {
        AndroidCamera _camera;
        Timer _timer;

        /// <summary>
        /// AutoFocusCallback 객체 사용 종료 여부
        /// </summary>
        public bool IsDisposed { get; private set; }

        /// <summary>
        /// 지정된 주기로 자동 포커스를 설정합니다.
        /// </summary>
        /// <param name="camera">대상 카메라</param>
        /// <param name="interval">자동 포커스 시간 간격(ms)</param>
        public AutoFocusCallback(AndroidCamera camera, int interval)
        {
            _camera = camera;

            _timer = new Timer()
            {
                Interval = interval,
                Enabled = false,
                AutoReset = true
            };
            _timer.Elapsed += OnTick;
            _timer.Start();

            this.Write($"AutoFocusCallback is created");

            try
            {
                _camera.AutoFocus(this);
            }
            catch (Exception ex)
            {
                this.Write($"Camera AutoFocus setting exception : {ex.Message}");
            }
        }

        /// <summary>
        /// 지정된 주기로 포커싱합니다.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTick(object sender, ElapsedEventArgs e)
        {
            if (_timer.Enabled)
                _camera.AutoFocus(this);
        }

        /// <summary>
        /// 포커싱 시도 후 성공 여부를 알리는 콜백 함수입니다.
        /// </summary>
        /// <param name="success">포커싱 성공 결과</param>
        /// <param name="camera">대상 카메라</param>
        public void OnAutoFocus(bool success, AndroidCamera camera)
            => this.Write($"AutoFocusing : {success}");

        /// <summary>
        /// AutoFocusCallback 객체 사용을 종료합니다.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (IsDisposed)
            {
                this.Write($"Already disposed");
                return;
            }

            _timer.Stop();
            _timer.Elapsed -= OnTick;
            _timer.Dispose();

            _timer = null;
            _camera = null;

            IsDisposed = true;

            base.Dispose(disposing);

            this.Write($"Disposed");
        }
    }
}

