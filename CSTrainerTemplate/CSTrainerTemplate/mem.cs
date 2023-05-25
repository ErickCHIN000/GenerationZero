using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CSTrainerTemplate
{
    internal class mem
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [MarshalAs(UnmanagedType.AsAny)] object lpBuffer, int nSize, out IntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr OpenProcess(int dwDesiredAccess, bool bInheritHandle, int processId);

        public Process process;
        public IntPtr processHandle;
        public IntPtr baseAddress;
        public int processID;

        public mem(string processName)
        {
            process = Process.GetProcessesByName(processName)[0];
            // check if process is found if not show messagebox and exit
            if (process == null)
            {
                _ = System.Windows.Forms.MessageBox.Show("Process not found!", "Error", System.Windows.Forms.MessageBoxButtons.OK, System.Windows.Forms.MessageBoxIcon.Error);
                Environment.Exit(0);
            }

            processID = process.Id;
            processHandle = OpenProcess(0x1F0FFF, false, processID);
            baseAddress = process.MainModule.BaseAddress;
        }

        public IntPtr FindDMAAddy(IntPtr ptr, int[] offsets)
        {
            byte[] buffer = new byte[IntPtr.Size];

            foreach (int i in offsets)
            {
                _ = ReadProcessMemory(processHandle, ptr, buffer, buffer.Length, out IntPtr read);

                ptr = (IntPtr.Size == 4)
                ? IntPtr.Add(new IntPtr(BitConverter.ToInt32(buffer, 0)), i)
                : ptr = IntPtr.Add(new IntPtr(BitConverter.ToInt64(buffer, 0)), i);
            }
            return ptr;
        }

        #region ReplaceBytes

        public void ReplaceBytes(IntPtr address, byte[] bytes)
        {
            _ = WriteProcessMemory(processHandle, address, bytes, bytes.Length, out _);
        }

        public void ReplaceBytes(IntPtr address, string bytes)
        {
            ReplaceBytes(address, String2Bytes(bytes));
        }

        public byte[] String2Bytes(string input)
        {
            string[] byteStrings = input.Split(' ');
            byte[] bytes = new byte[byteStrings.Length];

            for (int i = 0; i < byteStrings.Length; i++)
            {
                bytes[i] = Convert.ToByte(byteStrings[i], 16);
            }

            return bytes;
        }
        #endregion

        #region AOBScan
        public IntPtr AOBScan(string aob, string mask, int instance)
        {
            byte[] buffer = new byte[process.MainModule.ModuleMemorySize];
            _ = ReadProcessMemory(processHandle, process.MainModule.BaseAddress, buffer, buffer.Length, out _);

            List<IntPtr> results = new List<IntPtr>();

            // Convert the AOB and mask strings to byte arrays
            byte[] aobBytes = ParseAOB(aob);
            byte[] maskBytes = ParseAOB(mask);

            // Perform the AOB scan
            for (int i = 0; i < buffer.Length - aobBytes.Length + 1; i++)
            {
                bool found = true;

                for (int j = 0; j < aobBytes.Length; j++)
                {
                    if (maskBytes[j] == 0x0)
                    {
                        continue;
                    }

                    if (buffer[i + j] != aobBytes[j])
                    {
                        found = false;
                        break;
                    }
                }

                if (found)
                {
                    IntPtr address = process.MainModule.BaseAddress + i;
                    results.Add(address);
                }
            }

            return results[instance];
        }

        private byte[] ParseAOB(string aob)
        {
            aob = aob.Replace(" ", ""); // Remove spaces from the AOB string

            int length = aob.Length / 2;
            byte[] bytes = new byte[length];

            for (int i = 0; i < length; i++)
            {
                bytes[i] = Convert.ToByte(aob.Substring(i * 2, 2), 16);
            }

            return bytes;
        }
        #endregion

        #region Read/Write
        public int ReadInt32(IntPtr address)
        {
            byte[] buffer = new byte[4];
            _ = ReadProcessMemory(processHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToInt32(buffer, 0);
        }

        public float ReadFloat(IntPtr address)
        {
            byte[] buffer = new byte[4];
            _ = ReadProcessMemory(processHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToSingle(buffer, 0);
        }

        public string ReadString(IntPtr address, int length)
        {
            byte[] buffer = new byte[length];
            _ = ReadProcessMemory(processHandle, address, buffer, buffer.Length, out _);
            return Encoding.ASCII.GetString(buffer);
        }

        public bool ReadBoolean(IntPtr address)
        {
            byte[] buffer = new byte[1];
            _ = ReadProcessMemory(processHandle, address, buffer, buffer.Length, out _);
            return BitConverter.ToBoolean(buffer, 0);
        }

        public void WriteInt32(IntPtr address, int value)
        {
            _ = WriteProcessMemory(processHandle, address, value, 4, out _);
        }

        public void WriteFloat(IntPtr address, float value)
        {
            _ = WriteProcessMemory(processHandle, address, value, 4, out _);
        }

        public void WriteString(IntPtr address, string value)
        {
            _ = WriteProcessMemory(processHandle, address, value, value.Length, out _);
        }

        public void WriteBoolean(IntPtr address, bool value)
        {
            _ = WriteProcessMemory(processHandle, address, value, 1, out _);
        }
        #endregion
    }
}
