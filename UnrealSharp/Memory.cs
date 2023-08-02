using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;

namespace UnrealSharp
{
    public unsafe class Memory
    {
        public static readonly nint kernel = NativeLibrary.Load("kernel32.dll");
        public static readonly delegate* unmanaged[Stdcall]<int, int, int, nint> OpenProcess = (delegate* unmanaged[Stdcall]<int, int, int, nint>)NativeLibrary.GetExport(kernel, nameof(OpenProcess));
        public static readonly delegate* unmanaged[Stdcall]<nint, nint, byte[], int, out int, int> ReadProcessMemory2 = (delegate* unmanaged[Stdcall]<nint, nint, byte[], int, out int, int>)NativeLibrary.GetExport(kernel, nameof(ReadProcessMemory));
        //public static readonly delegate* unmanaged[Stdcall]<nint, int> CloseHandle;
        //public static readonly delegate* unmanaged[Stdcall]<nint, nint, void*, nint, out nint, uint> NtReadVirtualMemory;


        // [DllImport("kernel32")] static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Int32 bInheritHandle, Int32 dwProcessId);
        //[DllImport("kernel32")] static extern Int32 ReadProcessMemory(IntPtr hProcess, UInt64 lpBaseAddress, [In, Out] Byte[] buffer, Int32 size, out Int32 lpNumberOfBytesRead);
        [DllImport("kernel32")] static extern Boolean WriteProcessMemory(nint hProcess, nint lpBaseAddress, Byte[] buffer, Int32 nSize, out Int32 lpNumberOfBytesWritten);
        [DllImport("kernel32")] static extern Int32 CloseHandle(IntPtr hObject);
        [DllImport("kernel32")] static extern IntPtr CreateRemoteThread(IntPtr hProcess, IntPtr lpThreadAttributes, UInt32 dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, UInt32 dwCreationFlags, IntPtr lpThreadId);
        [DllImport("kernel32")] static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
        [DllImport("kernel32")] static extern IntPtr VirtualAllocEx(IntPtr hProcess, IntPtr lpAddress, Int32 dwSize, Int32 flAllocationType, Int32 flProtect);
        [DllImport("kernel32")] static extern Boolean VirtualFreeEx(IntPtr hProcess, IntPtr lpAddress, Int32 dwSize, Int32 dwFreeType);
        [DllImport("user32")] public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, String lpszClass, String lpszWindow);
        [DllImport("user32")] public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out Int32 lpdwProcessId);
        public IntPtr procHandle = IntPtr.Zero;
        public Process Process { get; private set; }
        public nint BaseAddress { get { return Process.MainModule.BaseAddress; } }
        public nint MainWindowHandle { get { return Process.MainWindowHandle; } }
        [StructLayout(LayoutKind.Sequential, Pack = 0)] public struct OBJECT_ATTRIBUTES
        {
            public int Length;
            public IntPtr RootDirectory;
            public IntPtr ObjectName;
            public uint Attributes;
            public IntPtr SecurityDescriptor;
            public IntPtr SecurityQualityOfService;
        }
        [StructLayout(LayoutKind.Sequential)] public struct CLIENT_ID
        {
            public IntPtr UniqueProcess;
            public IntPtr UniqueThread;
        }
        public Memory(Process proc)
        {
            //r result = NtOpenProcess(ref hProcess, 0x001F0FFF, ref oa, ref ci);
            //var handle = NativeLibrary.Load("kernel32.dll");
            //ReadProcMemInternal = (delegate* unmanaged[Stdcall]<IntPtr, UInt64, Byte[], Int32, out Int32, Int32>)NativeLibrary.GetExport(handle, "ReadProcessMemory");
            Process = proc;
            if (Process == null) return;
            OpenProcessById(Process.Id);
        }
        public Memory(String name)
        {
            var procs = Process.GetProcessesByName(name);
            Process = procs.FirstOrDefault();
            if (Process == null) return;
            OpenProcessById(Process.Id);
        }
        private Dictionary<IntPtr, int> _allocations = new Dictionary<IntPtr, int>();
        public void OpenProcessById(Int32 procId)
        {
            procHandle = OpenProcess(0x38, 1, procId);
        }
        public Int32 maxStringLength = 0x100;
        /*public static Int32 ReadProcessMemory(IntPtr hProcess, UInt64 lpBaseAddress, [In, Out] Byte[] buffer, Int32 size, out Int32 lpNumberOfBytesRead)
        {
            return ReadProcMemInternal(hProcess, lpBaseAddress, buffer, size, out lpNumberOfBytesRead);
        }*/
        public Byte[] ReadProcessMemory(nint addr, Int32 length)
        {
            var maxSize = 0x64000;
            var buffer = new Byte[length];
            for (var i = 0; i < length / (Single)maxSize; i++)
            {
                var buf = new Byte[0x64000];
                var blockSize = (i == (length / maxSize)) ? length : i % maxSize;
                ReadProcessMemory2(procHandle, addr + i * maxSize, buf, blockSize, out Int32 bytesRead);
                Array.Copy(buf, 0, buffer, i * maxSize, blockSize);
            }
            return buffer;
        }
        public unsafe Object ReadProcessMemory(Type type, nint addr)
        {
            if (type == typeof(String))
            {
                var stringLength = maxStringLength;
                List<Byte> bytes = new List<Byte>();
                var isUtf16 = false;
                for (var i = 0; i < 64; i++)
                {
                    var letters8 = ReadProcessMemory<nint>(addr + i * 8);
                    var tempBytes = BitConverter.GetBytes(letters8);
                    for (int j = 0; j < 8 && stringLength > 0; j++)
                    {
                        if (tempBytes[j] == 0 && j == 1 && bytes.Count == 1)
                            isUtf16 = true;
                        if (isUtf16 && j % 2 == 1)
                            continue;
                        if (tempBytes[j] == 0)
                            return (Object)Encoding.UTF8.GetString(bytes.ToArray());
                        if ((tempBytes[j] < 32 || tempBytes[j] > 126) && tempBytes[j] != '\n')
                            return (Object)"null";
                        bytes.Add(tempBytes[j]);
                        stringLength--;
                    }
                }
                return (Object)Encoding.UTF8.GetString(bytes.ToArray());
            }
            var buffer = new Byte[Marshal.SizeOf(type)];
            ReadProcessMemory2(procHandle, addr, buffer, Marshal.SizeOf(type), out Int32 bytesRead);
            var structPtr = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var obj = Marshal.PtrToStructure(structPtr.AddrOfPinnedObject(), type);
            var members = obj.GetType().GetFields();
            foreach (var member in members)
            {
                continue;
                if (member.FieldType == typeof(String))
                {
                    var offset = Marshal.OffsetOf(type, member.Name).ToInt32();
                    var ptr = BitConverter.ToUInt32(buffer.Skip(offset).Take(4).ToArray(), 0);
                    if (ptr == 0xffffffff || ptr == 0)
                    {
                        member.SetValueDirect(__makeref(obj), "null");
                        continue;
                    }
                    /* var val = member.GetValue(obj);
                     var validStr = true;
                     for (int i = 0; i < 16; i++)
                     {
                         if (buffer[offset + i] == 0 && i > 1)
                             break;
                         if (buffer[offset + i] < 32 || buffer[offset + i] > 126)
                         {
                             validStr = false;
                             break;
                         }
                     }
                     if (validStr)
                         continue;*/
                    var strPtr = Marshal.ReadIntPtr(structPtr.AddrOfPinnedObject(), offset);
                    var str = ReadProcessMemory<String>(strPtr);
                    if (str != "null" && str != "")
                        member.SetValueDirect(__makeref(obj), str);
                }
                /*if (member.FieldType.IsPointer)
                {
                    var address = System.Reflection.Pointer.Unbox(member.GetValue(obj));
                    var value = ReadProcessMemory(member.FieldType.GetElementType(), (UInt32)address);
                }*/
            }
            structPtr.Free();
            return obj;
        }
        public T ReadProcessMemory<T>(nint addr)
        {
            return (T)ReadProcessMemory(typeof(T), addr);
        }
        public void WriteProcessMemory(nint addr, Byte[] buffer)
        {
            WriteProcessMemory(procHandle, addr, buffer, buffer.Length, out Int32 bytesRead);
        }
        public void WriteProcessMemory<T>(nint addr, T value)
        {
            var objSize = Marshal.SizeOf(value);
            var objBytes = new Byte[objSize];
            var objPtr = Marshal.AllocHGlobal(objSize);
            Marshal.StructureToPtr(value, objPtr, true);
            Marshal.Copy(objPtr, objBytes, 0, objSize);
            Marshal.FreeHGlobal(objPtr);
            WriteProcessMemory(procHandle, addr, objBytes, objBytes.Length, out Int32 bytesRead);
        }
        public nint Execute(nint fPtr, nint a1, nint a2, nint a3, nint a4, params nint[] args)
        {
            var retValPtr = VirtualAllocEx(procHandle, IntPtr.Zero, 0x40, 0x1000, 0x40);
            WriteProcessMemory(retValPtr, BitConverter.GetBytes((nint)0xcafeb00));

            var asm = new List<Byte>();
            asm.AddRange(new Byte[] { 0x48, 0x83, 0xEC }); // sub rsp
            asm.Add(104);

            asm.AddRange(new Byte[] { 0x48, 0xB9 }); // mov rcx
            asm.AddRange(BitConverter.GetBytes(a1));

            asm.AddRange(new Byte[] { 0x48, 0xBA }); // mov rdx
            asm.AddRange(BitConverter.GetBytes(a2));

            asm.AddRange(new Byte[] { 0x49, 0xB8 }); // mov r8
            asm.AddRange(BitConverter.GetBytes(a3));

            asm.AddRange(new Byte[] { 0x49, 0xB9 }); // mov r9
            asm.AddRange(BitConverter.GetBytes(a4));

            var offset = 0u;
            foreach (var obj in args)
            {
                asm.AddRange(new Byte[] { 0x48, 0xB8 }); // mov rax
                asm.AddRange(BitConverter.GetBytes(obj));
                asm.AddRange(new Byte[] { 0x48, 0x89, 0x44, 0x24, (Byte)(0x28 + 8 * offset++) }); // mov rax to stack
            }
            asm.AddRange(new Byte[] { 0x48, 0xB8 }); // mov rax
            asm.AddRange(BitConverter.GetBytes(fPtr));

            asm.AddRange(new Byte[] { 0xFF, 0xD0 }); // call rax
            asm.AddRange(new Byte[] { 0x48, 0x83, 0xC4 }); // add rsp
            asm.Add(104);

            asm.AddRange(new Byte[] { 0x48, 0xA3 }); // mov rax to
            asm.AddRange(BitConverter.GetBytes((UInt64)retValPtr));
            asm.Add(0xC3); // ret
            var codePtr = VirtualAllocEx(procHandle, IntPtr.Zero, asm.Count, 0x1000, 0x40);
            WriteProcessMemory(procHandle, codePtr, asm.ToArray(), asm.Count, out Int32 bytesRead);

            var thread = CreateRemoteThread(procHandle, IntPtr.Zero, 0, codePtr, IntPtr.Zero, 0, IntPtr.Zero);
            WaitForSingleObject(thread, 10000);
            var returnValue = ReadProcessMemory<nint>(retValPtr);
            VirtualFreeEx(procHandle, codePtr, 0, 0x8000);
            VirtualFreeEx(procHandle, retValPtr, 0, 0x8000);
            CloseHandle(thread);
            return returnValue;
        }
        public T ExecuteUEFunc<T>(nint vtableAddr, nint objAddr, nint funcAddr, params Object[] args)
        {
            //var retValPtr = VirtualAllocEx(procHandle, IntPtr.Zero, 0x40, 0x1000, 0x40);
            //WriteProcessMemory((UInt64)retValPtr, BitConverter.GetBytes((UInt64)0xdeadbeefcafef00d));
            var dummyParms = VirtualAllocEx(procHandle, IntPtr.Zero, 0x100, 0x1000, 0x40);

            WriteProcessMemory(dummyParms, BitConverter.GetBytes((UInt64)0xffffffffffffffff));
            var offset = 0;
            foreach (var obj in args)
            {
                WriteProcessMemory(dummyParms + offset, obj);
                offset += Marshal.SizeOf(obj);
            }

            var asm = new List<Byte>();
            asm.AddRange(new byte[] { 0x48, 0x83, 0xEC }); // sub rsp
            asm.Add(40);
            asm.AddRange(new byte[] { 0x48, 0xB8 }); // mov rax
            asm.AddRange(BitConverter.GetBytes((UInt64)vtableAddr));

            asm.AddRange(new byte[] { 0x48, 0xB9 }); // mov rcx
            asm.AddRange(BitConverter.GetBytes((UInt64)objAddr));

            asm.AddRange(new byte[] { 0x48, 0xBA }); // mov rdx
            asm.AddRange(BitConverter.GetBytes((UInt64)funcAddr));

            asm.AddRange(new byte[] { 0x49, 0xB8 }); // mov r8
            asm.AddRange(BitConverter.GetBytes((UInt64)dummyParms));

            asm.AddRange(new byte[] { 0xFF, 0xD0 }); // call rax
            asm.AddRange(new byte[] { 0x48, 0x83, 0xC4 }); // add rsp
            asm.Add(40);
            //asm.AddRange(new byte[] { 0x48, 0xA3 }); // mov rax to
            //asm.AddRange(BitConverter.GetBytes((UInt64)retValPtr));
            asm.Add(0xC3); // ret
            var codePtr = VirtualAllocEx(procHandle, IntPtr.Zero, asm.Count, 0x1000, 0x40);
            WriteProcessMemory(procHandle, codePtr, asm.ToArray(), asm.Count, out Int32 bytesRead);

            IntPtr thread = CreateRemoteThread(procHandle, IntPtr.Zero, 0, codePtr, IntPtr.Zero, 0, IntPtr.Zero);
            WaitForSingleObject(thread, 10000);

            var returnValue = ReadProcessMemory<T>(dummyParms);
            VirtualFreeEx(procHandle, dummyParms, 0, 0x8000);
            VirtualFreeEx(procHandle, codePtr, 0, 0x8000);
            //VirtualFreeEx(procHandle, retValPtr, 0, 0x8000);
            CloseHandle(thread);
            return returnValue;
        }

        public List<nint> SearchProcessMemory(String pattern, nint start, nint end, Boolean absolute = true)
        {
            var arrayOfBytes = pattern.Split(' ').Select(b => b.Contains("?") ? -1 : Convert.ToInt32(b, 16)).ToArray();
            var addresses = new List<nint>();
            var iters = 1 + ((end - start) / 0x1000);
            if (iters == 0) iters++;
            for (uint i = 0; i < iters; i++)
            {
                var buffer = new Byte[0x1000];
                ReadProcessMemory2(procHandle, (nint)(start + i * 0x1000), buffer, 0x1000, out Int32 bytesRead);
                var results = Scan(buffer, arrayOfBytes).Select(j => (nint)(j + start + i * 0x1000)).ToList();
                if (start + (i + 1) * 0x1000 > end && results.Count > 0)
                    results.RemoveAll(r => r > end);
                addresses.AddRange(results);
            }
            if (absolute)
                return addresses;
            else
                return addresses.Select(a => a - start).ToList();
        }
        public List<nint> ReSearchProcessMemory(List<nint> existing, String pattern)
        {
            var arrayOfBytes = pattern.Split(' ').Select(b => b.Contains("?") ? -1 : Convert.ToInt32(b, 16)).ToArray();
            var addresses = new List<nint>();
            foreach (var val in existing)
            {
                var buffer = new Byte[4];
                ReadProcessMemory2(procHandle, (nint)val, buffer, 4, out int bytesRead);
                var results = Scan(buffer, arrayOfBytes).Select(j => j + val).ToList();
                addresses.AddRange(results);
            }
            return addresses;
        }
        public String DumpSurroundString(UInt64 start)
        {
            var buffer = new Byte[0x100];
            ReadProcessMemory2(procHandle, (nint)(start - 0x80), buffer, buffer.Length, out Int32 bytesRead);
            var val = "";
            for (int i = 0x7f; i > 0; i--)
            {
                if (buffer[i] == 0)
                    break;
                val = Encoding.UTF8.GetString(buffer, i, 1) + val;
            }
            for (int i = 0x80; i < 0x100; i++)
            {
                if (buffer[i] == 0)
                    break;
                val += Encoding.UTF8.GetString(buffer, i, 1);
            }
            return val;
        }
        public String GetString(nint start)
        {
            var buffer = new Byte[0x1000];
            ReadProcessMemory2(procHandle, start, buffer, buffer.Length, out Int32 bytesRead);
            return String.Join(",", buffer.Select(b => "0x" + b.ToString("X2")));
        }
        static Byte[] FileBytes;
        public static List<nint> Scan(Byte[] buf, Int32[] pattern)
        {
            var addresses = new List<nint>();

            for (int i = 0; i <= buf.Length - pattern.Length; i++)
            {
                var found = true;
                for (int j = 0; j < pattern.Length; j++)
                {
                    if (pattern[j] == -1)
                        continue;
                    if (buf[i + j] != pattern[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found)
                    addresses.Add(i);
            }
            return addresses;
        }

        public static void SetImageFile(String file)
        {
            FileBytes = System.IO.File.ReadAllBytes(file);
        }
        public static int GetImageBase()
        {
            var pe = BitConverter.ToUInt16(FileBytes, 0x3c);
            return BitConverter.ToInt32(FileBytes, pe + 0x34);
        }
        public static int FindAddr(String sig, Int32 offset, Boolean isOffset = false)
        {
            var arrayOfBytes = sig.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).Select(b => b.Contains("?") ? -1 : Convert.ToInt32(b, 16)).ToArray();
            var offs = Scan(FileBytes, arrayOfBytes);
            if (isOffset)
                return BitConverter.ToInt32(FileBytes, (int)offs.First() + offset);
            var addr = BitConverter.ToInt32(FileBytes, (int)offs.First() + offset) - GetImageBase();
            return addr;
        }
        public static nint FindAddr(String sig)
        {
            var arrayOfBytes = sig.Split(' ').Select(b => b.Contains("?") ? -1 : Convert.ToInt32(b, 16)).ToArray();
            var offs = Scan(FileBytes, arrayOfBytes);
            return offs.First() + GetImageBase();
        }
        public nint FindPattern(String pattern)
        {
            return FindPattern(pattern, Process.MainModule.BaseAddress, Process.MainModule.ModuleMemorySize);
        }
        public nint FindStringRef(String str)
        {
            var stringAddr = FindPattern(BitConverter.ToString(Encoding.Unicode.GetBytes(str)).Replace("-", " "));
            var sigScan = new SigScan(Process, Process.MainModule.BaseAddress, Process.MainModule.ModuleMemorySize);
            sigScan.DumpMemory();
            for (var i = 0; i < sigScan.Size; i++)
            {
                if ((sigScan.m_vDumpedRegion[i] == 0x48 || sigScan.m_vDumpedRegion[i] == 0x4c) && sigScan.m_vDumpedRegion[i + 1] == 0x8d)
                {
                    var jmpTo = BitConverter.ToInt32(sigScan.m_vDumpedRegion, i + 3);
                    var addr = sigScan.Address + i + jmpTo + 7;
                    if (addr == stringAddr)
                    {
                        return Process.MainModule.BaseAddress + i;
                    }
                }
            }
            return 0;
        }
        public nint FindPattern(String pattern, nint start, Int32 length)
        {
            //var skip = pattern.ToLower().Contains("cc") ? 0xcc : pattern.ToLower().Contains("aa") ? 0xaa : 0;
            var sigScan = new SigScan(Process, start, length);
            var arrayOfBytes = pattern.Split(' ').Select(b => b.Contains("?") ? (Byte)0 : (Byte)Convert.ToInt32(b, 16)).ToArray();
            var strMask = String.Join("", pattern.Split(' ').Select(b => b.Contains("?") ? '?' : 'x'));
            return sigScan.FindPattern(arrayOfBytes, strMask, 0);
        }
        public List<nint> FindPatterns(String pattern)
        {
            //var skip = pattern.ToLower().Contains("cc") ? 0xcc : pattern.ToLower().Contains("aa") ? 0xaa : 0;
            var sigScan = new SigScan(Process, Process.MainModule.BaseAddress, Process.MainModule.ModuleMemorySize);
            var arrayOfBytes = pattern.Split(' ').Select(b => b.Contains("?") ? (Byte)0 : (Byte)Convert.ToInt32(b, 16)).ToArray();
            var strMask = String.Join("", pattern.Split(' ').Select(b => b.Contains("?") ? '?' : 'x'));
            return sigScan.FindPatterns(arrayOfBytes, strMask, 0);
        }
    }
}