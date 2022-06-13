using System.Runtime.InteropServices;

namespace Benchmark;

internal static class FileHelper
{
    static NativeOverlapped _nativeOverlapped;

    internal static IntPtr CreateUnbufferedFile(string path)
    {
        return CreateFile(
            Path.Combine($"{path}", Path.GetRandomFileName()),
            FileAccess.ReadWrite,
            FileShare.None,
            IntPtr.Zero,
            FileMode.Create,
            UnmanagedFileAttributes.FILE_FLAG_NO_BUFFERING | UnmanagedFileAttributes.FILE_FLAG_WRITE_THROUGH,
            IntPtr.Zero);
    }

    internal static bool WriteFile(IntPtr hFile, byte[] lpBuffer)
    {
        return WriteFile(
            hFile,
            lpBuffer,
            (uint)lpBuffer.Length,
            out var _,
            ref _nativeOverlapped);
    }

    internal static bool ReadFile(IntPtr hFile, byte[] lpBuffer)
    {
        return ReadFile(
            hFile,
            lpBuffer,
            (uint)lpBuffer.Length,
            out var _,
            ref _nativeOverlapped);
    }

    [DllImport("kernel32.dll")]
    static extern IntPtr CreateFile(
     [MarshalAs(UnmanagedType.LPWStr)] string filename,
     [MarshalAs(UnmanagedType.U4)] FileAccess access,
     [MarshalAs(UnmanagedType.U4)] FileShare share,
     IntPtr securityAttributes,
     [MarshalAs(UnmanagedType.U4)] FileMode creationDisposition,
     [MarshalAs(UnmanagedType.U4)] UnmanagedFileAttributes flagsAndAttributes,
     IntPtr templateFile);

    [DllImport("kernel32.dll")]
    static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer,
        uint nNumberOfBytesToWrite, out uint lpNumberOfBytesWritten,
        [In] ref NativeOverlapped lpOverlapped);

    [DllImport("kernel32.dll")]
    static extern bool ReadFile(IntPtr hFile, byte[] lpBuffer,
        uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead,
        [In] ref NativeOverlapped lpOverlapped);

    [Flags]
    enum UnmanagedFileAttributes : uint
    {
        FILE_FLAG_NO_BUFFERING = 0x20000000,
        FILE_FLAG_WRITE_THROUGH = 0x80000000
    }
}
