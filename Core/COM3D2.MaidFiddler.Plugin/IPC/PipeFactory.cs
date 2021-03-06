﻿using System;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using COM3D2.MaidFiddler.Core.Utils;
using Microsoft.Win32.SafeHandles;

namespace COM3D2.MaidFiddler.Core.IPC
{
    public static class PipeFactory
    {
        private const int PIPE_ACCESS_DUPLEX = 0x00000003;
        private const int PIPE_TYPE_BYTE = 0x00000000;

        public static NamedPipeServerStream CreatePipe(string name)
        {
            var sa = new SecurityAttributes();
            sa.Length = Marshal.SizeOf(typeof(SecurityAttributes));
            sa.Inheritable = true;
            Debugger.Debug(LogLevel.Info, $"Created SA: {sa}");

            // Create a YOLO security descriptor to get around Access Denied problems
            // This is a pipe for a game; nothing of value to steal
            SafeSecurityDescriptor securityDescriptor = SafeSecurityDescriptor.FromSDDL("D:(A;OICI;GA;;;WD)");
            Debugger.Debug(LogLevel.Info, $"Got security descriptor: {securityDescriptor}");

            sa.SecurityDescriptor = securityDescriptor.DangerousGetHandle();

            IntPtr pipeHandle = CreateNamedPipe($@"\\.\pipe\{name}", PIPE_ACCESS_DUPLEX, PIPE_TYPE_BYTE, 1, 64 * 1024, 64 * 1024, 0, sa);

            Debugger.Debug(LogLevel.Info, $"Created pipe: {pipeHandle.ToString("X")}");
            securityDescriptor.Dispose();

            return new NamedPipeServerStream(PipeDirection.InOut, false, false, new SafePipeHandle(pipeHandle, true));
        }

        [DllImport("kernel32.dll", SetLastError = true, BestFitMapping = false, CharSet = CharSet.Auto)]
        private static extern IntPtr CreateNamedPipe(string pipeName,
                                                     int openMode,
                                                     int pipeMode,
                                                     int maxInstances,
                                                     int outBufferSize,
                                                     int inBufferSize,
                                                     int defaultTimeout,
                                                     SecurityAttributes securityAttributes);

        [StructLayout(LayoutKind.Sequential)]
        private struct SecurityAttributes
        {
            public int Length;
            public IntPtr SecurityDescriptor;
            public bool Inheritable;
        }

        private sealed class SafeSecurityDescriptor : SafeHandleZeroOrMinusOneIsInvalid
        {
            private const int SDDL_REVISION_1 = 1;

            public SafeSecurityDescriptor() : base(true) { }

            public SafeSecurityDescriptor(IntPtr existingHandle, bool ownsHandle) : base(ownsHandle)
            {
                SetHandle(existingHandle);
            }

            public override string ToString()
            {
                return handle.ToString("X");
            }


            public static SafeSecurityDescriptor FromSDDL(string sddl)
            {
                ConvertStringSecurityDescriptorToSecurityDescriptor(sddl,
                                                                    SDDL_REVISION_1,
                                                                    out SafeSecurityDescriptor securityDescriptor,
                                                                    IntPtr.Zero);
                return securityDescriptor;
            }

            protected override bool ReleaseHandle()
            {
                return LocalFree(handle) == IntPtr.Zero;
            }

            [DllImport("Advapi32.dll", CharSet = CharSet.Auto, SetLastError = true, BestFitMapping = false)]
            private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
                    string stringSecurityDescriptor,
                    int stringSdRevision,
                    out SafeSecurityDescriptor pSecurityDescriptor,
                    IntPtr securityDescriptorSize);

            [DllImport("kernel32.dll")]
            private static extern IntPtr LocalFree(IntPtr hMem);
        }
    }
}