using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

public static class ImpersonationHelper
{
    // P/Invoke that directly returns a SafeAccessTokenHandle
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool LogonUser(
        string lpszUsername,
        string lpszDomain,
        string lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out SafeAccessTokenHandle phToken
    );

    /// <summary>
    /// Executes <paramref name="action"/> under the credentials supplied.
    /// </summary>
    public static void RunAs(string username, string domain, SecureString securePwd, Action action)
    {
        // Convert SecureString to plaintext
        var ptr = Marshal.SecureStringToGlobalAllocUnicode(securePwd);
        string pwd = Marshal.PtrToStringUni(ptr) ?? string.Empty;
        Marshal.ZeroFreeGlobalAllocUnicode(ptr);

        const int LOGON32_LOGON_NEW_CREDENTIALS = 9;
        const int LOGON32_PROVIDER_WINNT50 = 3;

        if (!LogonUser(
                username,
                domain,
                pwd,
                LOGON32_LOGON_NEW_CREDENTIALS,
                LOGON32_PROVIDER_WINNT50,
                out SafeAccessTokenHandle safeHandle
            ))
        {
            throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error(),
                "LogonUser failed");
        }

        // Zero the plaintext password ASAP
        pwd = new string('\0', pwd.Length);

        // Run the delegate under the impersonated token
        WindowsIdentity.RunImpersonated(safeHandle, () =>
        {
            action();
        });

        safeHandle.Dispose();
    }
}

