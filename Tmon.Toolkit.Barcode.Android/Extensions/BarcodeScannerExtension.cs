using Android.Gms.Vision;
using Android.Gms.Vision.Barcodes;
using Android.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using Android.Util;
using AndroidCamera = Android.Hardware.Camera;

namespace Tmon.Toolkit.Barcode.Droid.Extensions
{
    internal static class BarcodeScannerExtension
    {
        private static readonly Dictionary<BarcodeFormat, Enums.BarcodeFormat>
            _MappingDic = new Dictionary<BarcodeFormat, Enums.BarcodeFormat>
            {
                { BarcodeFormat.Codabar, Enums.BarcodeFormat.Codabar },
                { BarcodeFormat.Code128, Enums.BarcodeFormat.Code128 },
                { BarcodeFormat.Code39, Enums.BarcodeFormat.Code39 },
                { BarcodeFormat.Code93, Enums.BarcodeFormat.Code93 },
                { BarcodeFormat.DataMatrix, Enums.BarcodeFormat.DataMatrix },
                { BarcodeFormat.Ean13, Enums.BarcodeFormat.Ean13 },
                { BarcodeFormat.Ean8, Enums.BarcodeFormat.Ean8 },
                { BarcodeFormat.Itf, Enums.BarcodeFormat.Itf },
                { BarcodeFormat.Pdf417, Enums.BarcodeFormat.Pdf417 },
                { BarcodeFormat.QrCode, Enums.BarcodeFormat.QrCode },
                { BarcodeFormat.UpcA, Enums.BarcodeFormat.UpcA },
                { BarcodeFormat.UpcE, Enums.BarcodeFormat.UpcE },
            };

        /// <summary>
        /// Google Visison 바코드 포멧을 Tmon 바코드 포멧으로 변환합니다.
        /// </summary>
        /// <param name="format">Google Visison 바코드 포멧</param>
        /// <returns>Tmon 바코드 포멧</returns>
        public static Enums.BarcodeFormat ToTmonFormat(this BarcodeFormat format)
        {
            if (_MappingDic.ContainsKey(format))
                return _MappingDic[format];
            else
                return Enums.BarcodeFormat.Unknown;
        }

        /// <summary>
        /// Tmon 바코드 포멧(들)을 Google Visison 바코드 포멧으로 변환합니다.
        /// </summary>
        /// <param name="formats"></param>
        /// <returns></returns>
        public static BarcodeFormat ToVisionFormat(this IEnumerable<Enums.BarcodeFormat> formats)
        {
            int result = 0;

            if (formats == null || formats.Count() == 0)
            {
                foreach (var f in _MappingDic)
                    result |= (int)f.Key;
            }
            else
            {
                foreach (var f in formats)
                {
                    if (_MappingDic.ContainsValue(f))
                        result |= (int)_MappingDic.FirstOrDefault(e => e.Value == f).Key;
                    else
                        throw new ArgumentException($"Google Vision이 지원하지 않는 바코드 포멧입니다. - {f}");
                }
            }
            return (BarcodeFormat)result;
        }

        /// <summary>
        /// Camera 객체를 찾아 반환합니다.
        /// </summary>
        /// <param name="cameraSource"></param>
        /// <returns></returns>
        public static AndroidCamera GetCamera(this CameraSource cameraSource)
        {
            var javaHero = cameraSource.JavaCast<Java.Lang.Object>();
            var fields = javaHero?.Class?.GetDeclaredFields();

            if (fields == null) return null;

            foreach (var field in fields)
            {
                if (field.Type.CanonicalName.Equals("android.hardware.camera",
                    System.StringComparison.OrdinalIgnoreCase))
                {
                    field.Accessible = true;
                    var camera = (AndroidCamera)field.Get(javaHero);
                    return camera;
                }
            }

            return null;
        }

        /// <summary>
        /// 카메라 프리뷰 크기를 해상도 설정에 따라 지정합니다.
        /// </summary>
        /// <param name="displayMatrics">디스플레이 정보</param>
        /// <param name="resolution">해상도(높음/중간/낮음)</param>
        /// <returns>프리뷰 크기</returns>
        public static System.Drawing.Size GetPreviewSize(this BarcodeScanner scanner, DisplayMetrics displayMatrics)
        {
            double ratio = 1.0;

            switch (scanner.Resolution)
            {
                case Enums.Resolution.High:
                    ratio = 1.0;
                    break;
                case Enums.Resolution.Medium:
                    ratio = 0.7;
                    break;
                case Enums.Resolution.Low:
                    ratio = 0.5;
                    break;
                default:
                    break;
            }

            // Camera Preview의 높이/넓이 기준이 Display Matrics에서 사용하는 기준과 반대
            return new System.Drawing.Size(
                (int)(scanner.Height.ToPixels(displayMatrics) * ratio),
                (int)(scanner.Width.ToPixels(displayMatrics) * ratio));
        }

        /// <summary>
        /// Dip값을 Pixel값으로 변환합니다.
        /// </summary>
        /// <param name="dip"></param>
        /// <param name="displayMetrics"></param>
        /// <returns></returns>
        public static float ToPixels(this double dip, DisplayMetrics displayMetrics)
            => TypedValue.ApplyDimension(ComplexUnitType.Dip, (float)dip, displayMetrics);
    }
}