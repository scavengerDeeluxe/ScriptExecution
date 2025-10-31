using System;
using System.Runtime.InteropServices;
using System.IO;

public class SymbolicLinkCreator
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool CreateSymbolicLink(string lpSymlinkFileName, string lpTargetFileName, int dwFlags);

    // 0x0 = file, 0x1 = directory
    private const int SYMBOLIC_LINK_FLAG_FILE = 0x0;
    private const int SYMBOLIC_LINK_FLAG_DIRECTORY = 0x1;

    public static bool CreateLink(string linkPath, string targetPath, bool isDirectory)
    {
        int flags = isDirectory ? SYMBOLIC_LINK_FLAG_DIRECTORY : SYMBOLIC_LINK_FLAG_FILE;
        bool result = CreateSymbolicLink(linkPath, targetPath, flags);
        if (!result)
        {
            int error = Marshal.GetLastWin32Error();
            Console.WriteLine($"Failed to create symlink. Error {error}: {new System.ComponentModel.Win32Exception(error).Message}");
        }
        return result;
    }
}