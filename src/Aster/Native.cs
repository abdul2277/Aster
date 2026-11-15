using System;
using System.Runtime.InteropServices;

namespace Aster;

internal static class Native
{
	public struct Times
	{
		public uint Low;

		public uint High;

		public ulong Value => ((ulong)High << 32) | Low;
	}

	[StructLayout(LayoutKind.Sequential)]
	public class Memory
	{
		public uint Length = (uint)Marshal.SizeOf(typeof(Memory));

		public uint Load;

		public ulong Total;

		public ulong Available;

		public ulong TotalPage;

		public ulong AvailPage;

		public ulong TotalVirtual;

		public ulong AvailVirtual;

		public ulong AvailExtended;
	}

	[DllImport("kernel32.dll")]
	public static extern bool GetSystemTimes(out Times idle, out Times kernel, out Times user);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool GlobalMemoryStatusEx([In][Out] Memory value);

	[DllImport("kernel32.dll")]
	public static extern ulong GetTickCount64();

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool IsProcessCritical(IntPtr process, out bool critical);
}
