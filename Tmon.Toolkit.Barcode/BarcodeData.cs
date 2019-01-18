using System;
using System.Collections.Generic;
using System.Text;
using Tmon.Toolkit.Barcode.Enums;

namespace Tmon.Toolkit.Barcode
{
    /// <summary>
    /// 바코드 스캔 결과
    /// </summary>
    public class BarcodeData
    {
        /// <summary>
        /// 바코드 포멧
        /// </summary>
        public BarcodeFormat Format { get; set; }
        /// <summary>
        /// 스캔된 데이터
        /// </summary>
        public string Data { get; set; }

        public BarcodeData(BarcodeFormat format, string data)
        {
            Format = format;
            Data = data;
        }
    }
}
