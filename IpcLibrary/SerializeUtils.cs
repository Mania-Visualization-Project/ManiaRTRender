using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace IpcLibrary
{
    public class SerializeUtils
    {
        public static byte[] Object2Bytes(object obj)
        {
            //byte[] buff;
            //using (MemoryStream ms = new MemoryStream())
            //{
            //    IFormatter iFormatter = new BinaryFormatter();
            //    iFormatter.Serialize(ms, obj);
            //    buff = ms.GetBuffer();
            //}
            //return buff;
            MemoryStream ms = new MemoryStream();
            //创建序列化的实例
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);//序列化对象，写入ms流中  
            byte[] bytes = ms.GetBuffer();
            return bytes;
        }

        public static object Bytes2Object(byte[] bytes)
        {
            //Console.WriteLine("byte 2 object 1");

            //object obj;
            //using (MemoryStream ms = new MemoryStream(buff))
            //{
            //    IFormatter iFormatter = new BinaryFormatter();
            //    obj = iFormatter.Deserialize(ms);
            //}
            //Console.WriteLine("byte 2 object 2");
            //return obj;
            object obj = null;
            MemoryStream ms = new MemoryStream(bytes);
            ms.Position = 0;
            BinaryFormatter formatter = new BinaryFormatter();
            obj = formatter.Deserialize(ms);//把内存流反序列成对象  
            ms.Close();
            return obj;
        }

        public static void Int2Bytes(int value, ref byte[] bytes, int start)
        {
            bytes[start] = (byte)((value >> 24) & 0xFF);
            bytes[start + 1] = (byte)((value >> 16) & 0xFF);
            bytes[start + 2] = (byte)((value >> 8) & 0xFF);
            bytes[start + 3] = (byte)(value & 0xFF);
        }

        public static int Bytes2Int(ref byte[] bytes, int start)
        {
            int value = 0;
            value += (bytes[start] & 0x000000FF) << 24;
            value += (bytes[start + 1] & 0x000000FF) << 16;
            value += (bytes[start + 2] & 0x000000FF) << 8;
            value += (bytes[start + 3] & 0x000000FF);
            return value;
        }

        private static List<ShareMem> MemDBList = new List<ShareMem>();

        public static int InitShareMemory(string name, int size)
        {
            ShareMem MemDB = new ShareMem();
            MemDB.Init($"ManiaRTRender_{name}", size);
            MemDBList.Add(MemDB);
            return MemDBList.Count - 1;
        }

        public static void Save(int id, ref byte[] bytes, int length)
        {
            //Console.WriteLine($"save: {4} {id}");

            byte[] sizeBytes = new byte[4];
            SerializeUtils.Int2Bytes(length, ref sizeBytes, 0);

            MemDBList[id].Write(sizeBytes, 0, 4);
            //Console.WriteLine($"save: {length} {id}");

            //string s = "";
            //for (int i = 0; i < length; i++)
            //{
            //    s = $"{s} {(int)bytes[i]}";
            //}
            //Console.WriteLine(s);


            MemDBList[id].Write(bytes, 4, length);
        }

        public static void Fetch(int id, ref byte[] buff)
        {
            byte[] bytes = new byte[4];
            MemDBList[id].Read(ref bytes, 0, 4);
            int size = SerializeUtils.Bytes2Int(ref bytes, 0);

            //Console.WriteLine($"fetch: {4} {id} {size}");

            

            MemDBList[id].Read(ref buff, 4, size);

            //string s = "";
            //for (int i = 0; i < size; i++)
            //{
            //    s = $"{s} {(int)buff[i + 4]}";
            //}
            //Console.WriteLine($"{s}");

            //Console.WriteLine($"fetch: {size} {id}");
        }


        public static int WriteInt(int value, ref byte[] buff, int start)
        {
            Int2Bytes(value, ref buff, start);
            return start + 4;
        }

        public static int ReadInt(out int value, ref byte[] buff, int start)
        {
            value = Bytes2Int(ref buff, start);
            return start + 4;
        }

        public static int WriteColor(ref Color color, ref byte[] buff, int start)
        {
            buff[start] = color.A;
            buff[start + 1] = color.R;
            buff[start + 2] = color.G;
            buff[start + 3] = color.B;
            return start + 4;
        }

        public static int ReadColor(out Color color, ref byte[] buff, int start)
        {
            color = Color.FromArgb(buff[start], buff[start + 1], buff[start + 2], buff[start + 3]);
            return start + 4;
        }

        public static int WriteString(ref string str, ref byte[] buff, int start)
        {
            byte[] content = System.Text.Encoding.Default.GetBytes(str);
            start = WriteInt(content.Length, ref buff, start);
            Array.Copy(content, 0, buff, start, content.Length);
            return start + content.Length;
        }

        public static int ReadString(out string str, ref byte[] buff, int start)
        {
            int length;
            start = ReadInt(out length, ref buff, start);
            byte[] content = new byte[length];
            Array.Copy(buff, start, content, 0, length);
            str = System.Text.Encoding.Default.GetString(content);
            return start + length;
        }

        public static int ReadBool(out bool value, ref byte[] buff, int start)
        {
            int data;
            start = ReadInt(out data, ref buff, start);
            value = data != 0;
            return start;
        }

        public static int WriteBool(bool value, ref byte[] buff, int start)
        {
            return WriteInt(value ? 1 : 0, ref buff, start);
        }
    }

}
