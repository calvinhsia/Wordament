using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MakeDictionary
{
    public static class HResult
    {
        /// <summary>
        /// Generic HRESULT for success. COM Return Code.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1707", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Legacy COM tradition.")]
        public const int S_OK = 0;

        /// <summary>
        /// Generic HRESULT for failure. COM Return Code.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1707", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Legacy COM tradition.")]
        public const int S_FALSE = 1;

        /// <summary>.
        /// Error for the request of a not implemented interface. COM  Return Code.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1707", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Legacy COM tradition.")]
        public const int E_NOINTERFACE = unchecked((int)0x80004002);

        /// <summary>.
        /// Error for the request of a not implemented property. COM  Return Code.
        /// </summary>
        [SuppressMessage("Microsoft.Naming", "CA1707", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Legacy COM tradition.")]
        public const int E_NOTIMPL = unchecked((int)0x80004001);

        /// <summary>
        /// Pointer that is not valid.
        /// </summary>
        public const int E_POINTER = unchecked((int)0x80004003);

        /// <summary>
        /// Operation aborted.
        /// </summary>
        public const int E_ABORT = unchecked((int)0x80004004);

        /// <summary>
        /// Unspecified failure.
        /// </summary>
        public const int E_FAIL = unchecked((int)0x80004005);

        /// <summary>
        /// Not supported failure.
        /// </summary>
        public const int E_NOTSUPPORTED = unchecked((int)0x80004021);

        /// <summary>
        /// Unexpected failure.
        /// </summary>
        public const int E_UNEXPECTED = unchecked((int)0x8000FFFF);

        /// <summary>
        /// General access denied error.
        /// </summary>
        public const int E_ACCESSDENIED = unchecked((int)0x80070005);

        /// <summary>
        /// Handle that is not valid.
        /// </summary>
        public const int E_HANDLE = unchecked((int)0x80070006);

        /// <summary>
        /// Failed to allocate necessary memory.
        /// </summary>
        public const int E_OUTOFMEMORY = unchecked((int)0x8007000E);

        /// <summary>
        /// Invalid argument
        /// </summary>
        public const int E_INVALIDARG = unchecked((int)0x80070057);

        /// <summary>
        /// Operation didn't complete and should be executed again
        /// </summary>
        public const int E_PENDING = unchecked((int)0x8000000A);

        /// <summary>
        /// Server call retry later error code.
        /// </summary>
        public const int RPC_E_SERVERCALL_RETRYLATER = unchecked((int)0x8001010A);

        /// <summary>
        /// If a HResult is not S_OK then throw the corresponding exception.
        /// </summary>
        /// <param name="hresult">HResult to check.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Legacy COM tradition.")]
        public static void ThrowIfNotSOK(int hresult)
        {
            if (!Succeeded(hresult))
            {
                System.Runtime.InteropServices.Marshal.ThrowExceptionForHR(hresult);
            }
        }

        /// <summary>
        /// Retry if the hresult is RPC_E_SERVERCALL_RETRYLATER, return the hresult after reach maximum retry times.
        /// </summary>
        /// <param name="retryFunc">The function may need to retry because of "server is busy" exception.</param>
        /// <param name="hresult">Maximum time to retry.</param>
        /// <param name="hresult">Sleep time between retries in milliseconds.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Legacy COM tradition.")]
        public static int RetryIfVSBusy(Func<int> retryFunc, int maxRetries = 5, int sleepTime = 100)
        {
            int hresult;
            int retries = 0;
            do
            {
                hresult = retryFunc.Invoke();
                retries++;
                if (hresult != RPC_E_SERVERCALL_RETRYLATER || retries >= maxRetries)
                    break;

                Thread.Sleep(sleepTime);
            } while (true);

            return hresult;
        }

        /// <summary>
        /// Tests if a HResult is S_OK
        /// </summary>
        /// <param name="hresult">HResult to check.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Legacy COM tradition.")]
        public static bool Succeeded(int hresult)
        {
            return hresult == HResult.S_OK;
        }

        /// <summary>
        /// Retry if the hresult is RPC_E_SERVERCALL_RETRYLATER, after reach maximum times, then throw the corresponding exception.
        /// </summary>
        /// <param name="retryFunc">The function may need to retry because of "server is busy" exception.</param>
        /// <param name="hresult">Maximum time to retry.</param>
        /// <param name="hresult">Sleep time between retries in milliseconds.</param>
        [SuppressMessage("Microsoft.Naming", "CA1704", Justification = "Legacy COM tradition.")]
        [SuppressMessage("Microsoft.Naming", "CA1709", Justification = "Legacy COM tradition.")]
        public static void ThrowIfNotSOKWithRetry(Func<int> retryFunc, int maxRetries = 5, int sleepTime = 100)
        {
            int hresult = RetryIfVSBusy(retryFunc, maxRetries, sleepTime);
            HResult.ThrowIfNotSOK(hresult);
        }
    }
}
