#if NETSTANDARD2_0

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Stl.Compatibility;

internal static class Interop
{
    internal static unsafe void GetRandomBytes(byte* buffer, int length)
    {
        Debug.Assert(buffer != null);
        Debug.Assert(length >= 0);

        var status = BCrypt.BCryptGenRandom(IntPtr.Zero, buffer, length, BCrypt.BCRYPT_USE_SYSTEM_PREFERRED_RNG);
        if (status != BCrypt.NTSTATUS.STATUS_SUCCESS) {
            throw status == BCrypt.NTSTATUS.STATUS_NO_MEMORY
#pragma warning disable CA2201
                ? new OutOfMemoryException()
#pragma warning restore CA2201
                : new InvalidOperationException();
        }
    }

    internal static partial class BCrypt
    {
        internal const int BCRYPT_USE_SYSTEM_PREFERRED_RNG = 0x00000002;

        [DllImport(Libraries.BCrypt, CharSet = CharSet.Unicode)]
#pragma warning disable CA5392
        internal static extern unsafe NTSTATUS BCryptGenRandom(IntPtr hAlgorithm, byte* pbBuffer, int cbBuffer, int dwFlags);
#pragma warning restore CA5392
    }

    internal static partial class BCrypt
    {
        internal enum NTSTATUS : uint
        {
            STATUS_SUCCESS = 0x0,
            STATUS_NOT_FOUND = 0xc0000225,
            STATUS_INVALID_PARAMETER = 0xc000000d,
            STATUS_NO_MEMORY = 0xc0000017,
            STATUS_AUTH_TAG_MISMATCH = 0xc000a002,
        }
    }

    internal static partial class Libraries
    {
        internal const string Advapi32 = "advapi32.dll";
        internal const string BCrypt = "BCrypt.dll";
        internal const string CoreComm_L1_1_1 = "api-ms-win-core-comm-l1-1-1.dll";
        internal const string CoreComm_L1_1_2 = "api-ms-win-core-comm-l1-1-2.dll";
        internal const string Crypt32 = "crypt32.dll";
        internal const string CryptUI = "cryptui.dll";
        internal const string Error_L1 = "api-ms-win-core-winrt-error-l1-1-0.dll";
        internal const string Gdi32 = "gdi32.dll";
        internal const string HttpApi = "httpapi.dll";
        internal const string IpHlpApi = "iphlpapi.dll";
        internal const string Kernel32 = "kernel32.dll";
        internal const string Memory_L1_3 = "api-ms-win-core-memory-l1-1-3.dll";
        internal const string Mswsock = "mswsock.dll";
        internal const string NCrypt = "ncrypt.dll";
        internal const string NtDll = "ntdll.dll";
        internal const string Odbc32 = "odbc32.dll";
        internal const string Ole32 = "ole32.dll";
        internal const string OleAut32 = "oleaut32.dll";
        internal const string PerfCounter = "perfcounter.dll";
        internal const string RoBuffer = "api-ms-win-core-winrt-robuffer-l1-1-0.dll";
        internal const string Secur32 = "secur32.dll";
        internal const string Shell32 = "shell32.dll";
        internal const string SspiCli = "sspicli.dll";
        internal const string User32 = "user32.dll";
        internal const string Version = "version.dll";
        internal const string WebSocket = "websocket.dll";
        internal const string WinHttp = "winhttp.dll";
        internal const string WinMM = "winmm.dll";
        internal const string Ws2_32 = "ws2_32.dll";
        internal const string Wtsapi32 = "wtsapi32.dll";
        internal const string CompressionNative = "clrcompression.dll";
        internal const string CoreWinRT = "api-ms-win-core-winrt-l1-1-0.dll";
        internal const string MsQuic = "msquic.dll";
        internal const string HostPolicy = "hostpolicy.dll";
    }
}

#endif
