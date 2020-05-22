﻿using System;
using System.Runtime.InteropServices;

namespace IpcLibrary
{
    public class ShareMem
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr CreateFileMapping(int hFile, IntPtr lpAttributes, uint flProtect, uint dwMaxSizeHi, uint dwMaxSizeLow, string lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr OpenFileMapping(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, string lpName);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr MapViewOfFile(IntPtr hFileMapping, uint dwDesiredAccess, uint dwFileOffsetHigh, uint dwFileOffsetLow, uint dwNumberOfBytesToMap);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool UnmapViewOfFile(IntPtr pvBaseAddress);

        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        public static extern bool CloseHandle(IntPtr handle);

        [DllImport("kernel32", EntryPoint = "GetLastError")]
        public static extern int GetLastError();

        const int ERROR_ALREADY_EXISTS = 183;

        const int FILE_MAP_COPY = 0x0001;
        const int FILE_MAP_WRITE = 0x0002;
        const int FILE_MAP_READ = 0x0004;
        const int FILE_MAP_ALL_ACCESS = 0x0002 | 0x0004;

        const int PAGE_READONLY = 0x02;
        const int PAGE_READWRITE = 0x04;
        const int PAGE_WRITECOPY = 0x08;
        const int PAGE_EXECUTE = 0x10;
        const int PAGE_EXECUTE_READ = 0x20;
        const int PAGE_EXECUTE_READWRITE = 0x40;

        const int SEC_COMMIT = 0x8000000;
        const int SEC_IMAGE = 0x1000000;
        const int SEC_NOCACHE = 0x10000000;
        const int SEC_RESERVE = 0x4000000;

        const int INVALID_HANDLE_VALUE = -1;

        IntPtr m_hSharedMemoryFile = IntPtr.Zero;
        IntPtr m_pwData = IntPtr.Zero;
        bool m_bAlreadyExist = false;
        bool m_bInit = false;
        long m_MemSize = 0;

        public ShareMem()
        {
        }
        ~ShareMem()
        {
            Close();
        }

        /// <summary>
        /// 初始化共享内存
        /// </summary>
        /// <param name="strName">共享内存名称</param>
        /// <param name="lngSize">共享内存大小</param>
        /// <returns></returns>
        public int Init(string strName, long lngSize)
        {
            if (lngSize <= 0 || lngSize > 0x00800000) lngSize = 0x00800000;
            m_MemSize = lngSize;
            if (strName.Length > 0)
            {
                //创建内存共享体(INVALID_HANDLE_VALUE)
                m_hSharedMemoryFile = CreateFileMapping(INVALID_HANDLE_VALUE, IntPtr.Zero, (uint)PAGE_READWRITE, 0, (uint)lngSize, strName);
                if (GetLastError() == ERROR_ALREADY_EXISTS) //已经创建
                {
                    m_bAlreadyExist = true;
                    m_hSharedMemoryFile = OpenFileMapping(FILE_MAP_ALL_ACCESS, false, strName);
                }
                if (m_hSharedMemoryFile == IntPtr.Zero)
                {
                    m_bAlreadyExist = false;
                    m_bInit = false;
                    throw new ShareMemException("Fail to CreateFileMapping!"); //创建共享体失败
                }
                else
                {
                    m_bAlreadyExist = GetLastError() == ERROR_ALREADY_EXISTS;
                }
                //---------------------------------------
                //创建内存映射

                m_pwData = MapViewOfFile(m_hSharedMemoryFile, FILE_MAP_WRITE, 0, 0, (uint)lngSize);
                if (m_pwData == IntPtr.Zero)
                {
                    m_bInit = false;
                    CloseHandle(m_hSharedMemoryFile);
                    throw new ShareMemException("Fail to do MapViewOfFile!"); //创建内存映射失败
                }
                else
                {
                    m_bInit = true;
                    if (m_bAlreadyExist == false)
                    {
                        //初始化
                    }
                }
                //----------------------------------------
            }
            else
            {
                throw new ShareMemException("Illegal parameters!"); //参数错误     
            }

            return 0;     //创建成功
        }
        /// <summary>
        /// 关闭共享内存
        /// </summary>
        public void Close()
        {
            if (!m_bInit) return;
            UnmapViewOfFile(m_pwData);
            CloseHandle(m_hSharedMemoryFile);
        }

        /// <summary>
        /// 读数据
        /// </summary>
        /// <param name="bytData">数据</param>
        /// <param name="lngAddr">起始地址</param>
        /// <param name="lngSize">个数</param>
        /// <returns></returns>
        public int Read(ref byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > m_MemSize) throw new ShareMemException("Out of index!"); //超出数据区
            if (m_bInit)
            {
                Marshal.Copy(m_pwData + lngAddr, bytData, 0, lngSize);
            }
            else
            {
                throw new ShareMemException("No init!"); //共享内存未初始化
            }
            return 0;     //读成功
        }

        /// <summary>
        /// 写数据
        /// </summary>
        /// <param name="bytData">数据</param>
        /// <param name="lngAddr">起始地址</param>
        /// <param name="lngSize">个数</param>
        /// <returns></returns>
        public int Write(byte[] bytData, int lngAddr, int lngSize)
        {
            if (lngAddr + lngSize > m_MemSize) throw new ShareMemException("Out of index!"); //超出数据区
            if (m_bInit)
            {
                Marshal.Copy(bytData, 0, m_pwData + lngAddr, lngSize);
            }
            else
            {
                throw new ShareMemException("No init!"); //共享内存未初始化
            }
            return 0;     //写成功
        }
    }

    public class ShareMemException : Exception
    {
        public ShareMemException(string message) : base(message)
        {
        }
    }
}
