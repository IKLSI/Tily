using System.Runtime.InteropServices;

namespace Tily.Core.Native;

[StructLayout(LayoutKind.Sequential)]
public struct Coord
{
    public short X;
    public short Y;
}

public static class PseudoConsoleApi
{
    public const string EmbeddedLibraryName = "conpty.dll";

    public static int Create(PseudoConsoleProvider provider, Coord size, IntPtr input, IntPtr output, out IntPtr handle) =>
        provider == PseudoConsoleProvider.Embedded
            ? Embedded.CreatePseudoConsole(size, input, output, 0, out handle)
            : Windows.CreatePseudoConsole(size, input, output, 0, out handle);

    public static int Resize(PseudoConsoleProvider provider, IntPtr handle, Coord size) =>
        provider == PseudoConsoleProvider.Embedded
            ? Embedded.ResizePseudoConsole(handle, size)
            : Windows.ResizePseudoConsole(handle, size);

    public static void Close(PseudoConsoleProvider provider, IntPtr handle)
    {
        if (provider == PseudoConsoleProvider.Embedded)
        {
            Embedded.ClosePseudoConsole(handle);
        }
        else
        {
            Windows.ClosePseudoConsole(handle);
        }
    }

    private static class Windows
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int CreatePseudoConsole(Coord size, IntPtr hInput, IntPtr hOutput, uint dwFlags, out IntPtr phPC);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern int ResizePseudoConsole(IntPtr hPC, Coord size);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void ClosePseudoConsole(IntPtr hPC);
    }

    private static class Embedded
    {
        [DllImport(EmbeddedLibraryName, SetLastError = true)]
        public static extern int CreatePseudoConsole(Coord size, IntPtr hInput, IntPtr hOutput, uint dwFlags, out IntPtr phPC);

        [DllImport(EmbeddedLibraryName, SetLastError = true)]
        public static extern int ResizePseudoConsole(IntPtr hPC, Coord size);

        [DllImport(EmbeddedLibraryName, SetLastError = true)]
        public static extern void ClosePseudoConsole(IntPtr hPC);
    }
}
