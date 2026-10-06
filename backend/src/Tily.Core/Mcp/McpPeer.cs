using System.IO.Pipes;
using Tily.Core.Native;

namespace Tily.Core.Mcp;

public static class McpPeer
{
    public static int? ProcessIdOf(PipeStream stream)
    {
        if (!OperatingSystem.IsMacOS())
        {
            return null;
        }

        try
        {
            var handle = stream.SafePipeHandle;
            var added = false;
            try
            {
                handle.DangerousAddRef(ref added);
                var length = (uint)sizeof(int);
                var result = PosixApi.GetSocketOption(handle.DangerousGetHandle(), PosixApi.LocalSocketLevel, PosixApi.LocalPeerProcessId, out var processId, ref length);
                return result == 0 && processId > 0 ? processId : null;
            }
            finally
            {
                if (added)
                {
                    handle.DangerousRelease();
                }
            }
        }
        catch (Exception exception) when (exception is ObjectDisposedException or InvalidOperationException)
        {
            return null;
        }
    }
}
