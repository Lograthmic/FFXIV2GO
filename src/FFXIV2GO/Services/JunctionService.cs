using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace FFXIV2GO.Services;

/// <summary>
/// 目录联接（Junction）创建/删除。
/// 创建用 P/Invoke DeviceIoControl(FSCTL_SET_REPARSE_POINT)，删除用 Directory.Delete（仅删重解析点）。
/// </summary>
public static class JunctionService
{
    private const uint FileFlagBackupSemantics = 0x02000000;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint GenericWrite = 0x40000000;
    private const uint OpenExisting = 3;
    private const uint IoReparseTagMountPoint = 0xA0000003;
    private const uint FsctlSetReparsePoint = 0x000900A4;
    private const string NonInterpretedPathPrefix = @"\??\";
    private const int InvalidHandleValue = -1;
    private const uint ReparseDataBufferHeaderSize = 8;

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr CreateFile(string lpFileName, uint dwDesiredAccess, uint dwShareMode,
        IntPtr lpSecurityAttributes, uint dwCreationDisposition, uint dwFlagsAndAttributes, IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(IntPtr hDevice, uint dwIoControlCode,
        IntPtr lpInBuffer, uint nInBufferSize, IntPtr lpOutBuffer, uint nOutBufferSize,
        out uint lpBytesReturned, IntPtr lpOverlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

    public static bool IsJunction(string path)
    {
        try
        {
            return (new DirectoryInfo(path).Attributes & FileAttributes.ReparsePoint) != 0;
        }
        catch
        {
            return false;
        }
    }

    public static void Create(string junctionPoint, string targetDir, bool overwrite = true)
    {
        LogService.Info($"创建目录联接: {junctionPoint} -> {targetDir}");
        targetDir = Path.GetFullPath(targetDir);
        if (!Directory.Exists(targetDir))
            throw new DirectoryNotFoundException($"目标路径不存在或不是目录: {targetDir}");

        if (Directory.Exists(junctionPoint))
        {
            if (!overwrite)
                throw new IOException($"路径已存在: {junctionPoint}");
            Directory.Delete(junctionPoint);
        }

        Directory.CreateDirectory(junctionPoint);

        var handle = CreateFile(junctionPoint, GenericWrite, 0, IntPtr.Zero, OpenExisting,
            FileFlagOpenReparsePoint | FileFlagBackupSemantics, IntPtr.Zero);
        if (handle.ToInt64() == InvalidHandleValue)
        {
            Directory.Delete(junctionPoint);
            throw new Win32Exception(Marshal.GetLastWin32Error(), $"无法打开重解析点: {junctionPoint}");
        }

        try
        {
            var targetBytes = Encoding.Unicode.GetBytes(NonInterpretedPathPrefix + targetDir);
            var substituteNameLength = (ushort)targetBytes.Length;
            var printNameOffset = (ushort)(substituteNameLength + 2);
            var reparseDataLength = (uint)(substituteNameLength + 12);

            var buffer = new byte[ReparseDataBufferHeaderSize + reparseDataLength];

            WriteUInt32(buffer, 0, IoReparseTagMountPoint);
            WriteUInt16(buffer, 4, (ushort)reparseDataLength);
            WriteUInt16(buffer, 6, 0);
            WriteUInt16(buffer, 8, 0);
            WriteUInt16(buffer, 10, substituteNameLength);
            WriteUInt16(buffer, 12, printNameOffset);
            WriteUInt16(buffer, 14, 0);
            Buffer.BlockCopy(targetBytes, 0, buffer, 16, targetBytes.Length);

            var pBuffer = Marshal.AllocHGlobal(buffer.Length);
            try
            {
                Marshal.Copy(buffer, 0, pBuffer, buffer.Length);
                if (!DeviceIoControl(handle, FsctlSetReparsePoint, pBuffer, (uint)buffer.Length,
                        IntPtr.Zero, 0, out _, IntPtr.Zero))
                {
                    throw new Win32Exception(Marshal.GetLastWin32Error(), $"设置重解析点失败: {junctionPoint}");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(pBuffer);
            }
        }
        finally
        {
            CloseHandle(handle);
        }
    }

    /// <summary>解除联接：删除重解析点，不影响目标目录内容。</summary>
    public static void Delete(string junctionPoint)
    {
        if (!Directory.Exists(junctionPoint)) return;
        LogService.Info($"解除目录联接: {junctionPoint}");
        Directory.Delete(junctionPoint);
    }

    private static void WriteUInt16(byte[] buffer, int offset, ushort value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)(value >> 8);
    }

    private static void WriteUInt32(byte[] buffer, int offset, uint value)
    {
        buffer[offset] = (byte)(value & 0xFF);
        buffer[offset + 1] = (byte)((value >> 8) & 0xFF);
        buffer[offset + 2] = (byte)((value >> 16) & 0xFF);
        buffer[offset + 3] = (byte)(value >> 24);
    }
}
