using System.Runtime.InteropServices;

namespace Benchmark;

internal static class FileHelper
{
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

    internal static bool WriteFile(IntPtr hFile, byte[] lpBuffer, out uint bytesWritten, long offset = 0)
    {
        var bytes = ToHighLowOrderBytes(offset);
        var overlapped = new NativeOverlapped() { OffsetLow = bytes.Low, OffsetHigh = bytes.High };
        return WriteFile(
            hFile,
            lpBuffer,
            (uint)lpBuffer.Length,
            out bytesWritten,
            ref overlapped);
    }

    internal static bool ReadFile(IntPtr hFile, byte[] lpBuffer, out uint bytesRead, long offset = 0)
    {
        var bytes = ToHighLowOrderBytes(offset);
        var overlapped = new NativeOverlapped() { OffsetLow = bytes.Low, OffsetHigh = bytes.High };
        return ReadFile(
            hFile,
            lpBuffer,
            (uint)lpBuffer.Length,
            out bytesRead,
            ref overlapped);
    }

    static (int High, int Low) ToHighLowOrderBytes(long value)
    {
        var bytes = BitConverter.GetBytes(value);
        int highBytes, lowBytes;
        if (BitConverter.IsLittleEndian)
        {
            lowBytes = BitConverter.ToInt32(bytes[..(bytes.Length / 2)]);
            highBytes = BitConverter.ToInt32(bytes, bytes.Length / 2);
        }
        else
        {
            lowBytes = BitConverter.ToInt32(bytes, bytes.Length / 2);
            highBytes = BitConverter.ToInt32(bytes[..(bytes.Length / 2)]);
        }
        return (highBytes, lowBytes);
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
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
