using AVFoundation;
using Foundation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Tmon.Toolkit.Barcode.iOS.Extensions
{
    internal static class BarcodeScannerExtension
    {
        // ITF14Code와 Interleaved2of5Code를 하나로 통합
        private static readonly AVMetadataObjectType COMBINED_ITF
            = AVMetadataObjectType.ITF14Code | AVMetadataObjectType.Interleaved2of5Code;

        private static readonly Dictionary<AVMetadataObjectType, Enums.BarcodeFormat>
            _MappingDic = new Dictionary<AVMetadataObjectType, Enums.BarcodeFormat>
            {
                // [Not Supported Formats]
                // Enums.BarcodeFormat.Codabar 
                // Enums.BarcodeFormat.UpcA,
                { AVMetadataObjectType.Code128Code, Enums.BarcodeFormat.Code128 },
                { AVMetadataObjectType.Code39Code, Enums.BarcodeFormat.Code39 },
                { AVMetadataObjectType.Code93Code, Enums.BarcodeFormat.Code93 },
                { AVMetadataObjectType.DataMatrixCode, Enums.BarcodeFormat.DataMatrix },
                { AVMetadataObjectType.EAN13Code, Enums.BarcodeFormat.Ean13 },
                { AVMetadataObjectType.EAN8Code, Enums.BarcodeFormat.Ean8 },
                { COMBINED_ITF, Enums.BarcodeFormat.Itf },
                { AVMetadataObjectType.PDF417Code, Enums.BarcodeFormat.Pdf417 },
                { AVMetadataObjectType.QRCode, Enums.BarcodeFormat.QrCode },
                { AVMetadataObjectType.UPCECode, Enums.BarcodeFormat.UpcE },
            };

        /// <summary>
        /// AV Foundation 바코드 포멧을 Tmon 바코드 포멧으로 변환
        /// </summary>
        /// <param name="format">AV Foundation 바코드 포멧</param>
        /// <returns>Tmon 바코드 포멧</returns>
        public static Enums.BarcodeFormat ToTmonFormat(this AVMetadataObjectType format)
        {
            if (format == AVMetadataObjectType.Interleaved2of5Code || format == AVMetadataObjectType.ITF14Code)
                format = COMBINED_ITF;

            if (_MappingDic.ContainsKey(format))
                return _MappingDic[format];
            else
                return Enums.BarcodeFormat.Unknown;
        }

        /// <summary>
        /// Tmon 바코드 포멧(들)을 AV Foundation 바코드 포멧으로 변환
        /// </summary>
        /// <param name="formats">Tmon 바코드 포멧들</param>
        /// <returns>AV Foundation 바코드 포멧</returns>
        public static AVMetadataObjectType ToAVFFormat(this IEnumerable<Enums.BarcodeFormat> formats)
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
                        throw new ArgumentException($"AV Foundation이 지원하지 않는 바코드 포멧입니다. - {f}");
                }
            }
            return (AVMetadataObjectType)result;
        }

        /// <summary>
        /// Resolution을 AVCaptureSession의 SessionPreset 값으로 변환
        /// </summary>
        /// <param name="resolution">Resolution</param>
        /// <returns>AVCaptureSession의 SessionPreset</returns>
        public static NSString ToSessionPreset(this Enums.Resolution resolution)
        {
            switch (resolution)
            {
                case Enums.Resolution.High:
                    return AVCaptureSession.PresetHigh;
                case Enums.Resolution.Medium:
                    return AVCaptureSession.PresetMedium;
                case Enums.Resolution.Low:
                    return AVCaptureSession.PresetLow;
                default:
                    return AVCaptureSession.PresetMedium;
            }
        }
    }
}
