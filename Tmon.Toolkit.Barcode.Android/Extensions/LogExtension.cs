using Tmon.Toolkit.Barcode.Log;

namespace Tmon.Toolkit.Barcode.Droid.Extensions
{
    internal static class LogExtension
    {
        /// <summary>
        /// 로그를 기록하는 확장 함수입니다.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="message">로그 내용</param>
        public static void Write(this ILog log, string message) => Write(log, "{0}", message);

        /// <summary>
        /// 로그를 기록하는 확장 함수입니다.
        /// </summary>
        /// <param name="log"></param>
        /// <param name="message">로그 내용(앞)</param>
        /// <param name="args">로그 내용(뒤)</param>
        public static void Write(this ILog log, string message, params object[] args)
        {
            var prefixedMsg = $"[{log.GetType().Name}], {message}";
            global::Android.Util.Log.Debug("Tmon.BarcodeScanner", prefixedMsg, args);
        }
    }
}