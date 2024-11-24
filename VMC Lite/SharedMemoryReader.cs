using System.IO;
using System.IO.MemoryMappedFiles;

namespace VMC_Lite
{
    public class SharedMemoryReader
    {
        // 读取共享内存中的字节数组
        public static byte[] ReadMemoryByOffset(string mmfName, long offset, int length)
        {
            if (!MemoryMappedFileExists(mmfName)) // 检查共享内存是否存在
            {
                return null; // 如果共享内存不存在，返回 null 或者其他值
            }

            using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mmfName))
            {
                using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
                {
                    byte[] buffer = new byte[length];
                    accessor.ReadArray(offset, buffer, 0, length);
                    return buffer;
                }
            }
        }

        // 读取共享内存中的整数
        public static int ReadIntByOffset(string mmfName, long offset)
        {
            if (!MemoryMappedFileExists(mmfName)) // 检查共享内存是否存在
            {
                return 0; // 如果共享内存不存在，返回默认值 0
            }

            using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mmfName))
            {
                using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
                {
                    int value;
                    accessor.Read(offset, out value);
                    return value;
                }
            }
        }

        // 读取共享内存中的浮动点数
        public static float ReadFloatByOffset(string mmfName, long offset)
        {
            if (!MemoryMappedFileExists(mmfName)) // 检查共享内存是否存在
            {
                return 0f; // 如果共享内存不存在，返回默认值 0f
            }

            using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mmfName))
            {
                using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
                {
                    byte[] buffer = new byte[sizeof(float)];
                    accessor.ReadArray(offset, buffer, 0, sizeof(float));
                    return BitConverter.ToSingle(buffer, 0);
                }
            }
        }

        // 读取共享内存中的布尔值
        public static bool ReadBoolByOffset(string mmfName, long offset)
        {
            if (!MemoryMappedFileExists(mmfName)) // 检查共享内存是否存在
            {
                return false; // 如果共享内存不存在，返回默认值 false
            }

            using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mmfName))
            {
                using (MemoryMappedViewAccessor accessor = mmf.CreateViewAccessor())
                {
                    byte value = 0;
                    accessor.Read(offset, out value);
                    return value != 0; // 判断字节值是否为非零，若为非零则返回 true
                }
            }
        }

        //检查共享内存是否存在
        private static bool MemoryMappedFileExists(string mmfName)
        {
            try
            {
                // 尝试打开共享内存，不抛出异常则说明共享内存存在
                using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mmfName))
                {
                    return true;
                }
            }
            catch (FileNotFoundException)
            {
                return false; // 如果共享内存不存在，则返回 false
            }
        }
        //private static bool MemoryMappedFileExists(string mmfName)
        //{
        //    try
        //    {
        //        // 尝试打开共享内存，如果没有则不会抛出异常
        //        using (MemoryMappedFile mmf = MemoryMappedFile.CreateOrOpen(mmfName, 1))
        //        {
        //            return true; // 如果成功打开共享内存，则说明它存在
        //        }
        //    }
        //    catch (UnauthorizedAccessException)
        //    {
        //        // 如果没有访问权限，也可以处理
        //        return false;
        //    }
        //    catch (Exception)
        //    {
        //        // 其他异常处理
        //        return false;
        //    }
        //}

    }
}
