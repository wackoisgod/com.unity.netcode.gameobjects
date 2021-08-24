using System;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using UnityEngine;

namespace Unity.Multiplayer.Netcode
{
    public struct FastBufferWriter : IDisposable
    {
        internal unsafe byte* m_BufferPointer;
        internal int m_Position;
        private int m_Length;
        internal int m_Capacity;
        internal readonly int m_MaxCapacity;
        private readonly Allocator m_Allocator;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal int m_AllowedWriteMark;
        private bool m_InBitwiseContext;
#endif
        
        /// <summary>
        /// The current write position
        /// </summary>
        public int Position
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Position;
        }

        /// <summary>
        /// The current total buffer size
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Capacity;
        }

        /// <summary>
        /// The maximum possible total buffer size
        /// </summary>
        public int MaxCapacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_MaxCapacity;
        }

        /// <summary>
        /// The total amount of bytes that have been written to the stream
        /// </summary>
        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => m_Position > m_Length ? m_Position : m_Length;
        }

        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void CommitBitwiseWrites(int amount)
        {
            m_Position += amount;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InBitwiseContext = false;
#endif
        }

        /// <summary>
        /// Create a FastBufferWriter.
        /// </summary>
        /// <param name="size">Size of the buffer to create</param>
        /// <param name="allocator">Allocator to use in creating it</param>
        /// <param name="maxSize">Maximum size the buffer can grow to. If less than size, buffer cannot grow.</param>
        public unsafe FastBufferWriter(int size, Allocator allocator, int maxSize = -1)
        {
            void* buffer = UnsafeUtility.Malloc(size, UnsafeUtility.AlignOf<byte>(), allocator);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnsafeUtility.MemSet(buffer, 0, size);
#endif
            m_BufferPointer = (byte*)buffer;
            m_Position = 0;
            m_Length = 0;
            m_Capacity = size;
            m_Allocator = allocator;
            m_MaxCapacity = maxSize < size ? size : maxSize;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_AllowedWriteMark = 0;
            m_InBitwiseContext = false;
#endif
        }

        /// <summary>
        /// Frees the allocated buffer
        /// </summary>
        public unsafe void Dispose()
        {
            UnsafeUtility.Free(m_BufferPointer, m_Allocator);
        }

        /// <summary>
        /// Move the write position in the stream.
        /// Note that moving forward past the current length will extend the buffer's Length value even if you don't write.
        /// </summary>
        /// <param name="where">Absolute value to move the position to, truncated to Capacity</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Seek(int where)
        {
            // This avoids us having to synchronize length all the time.
            // Writing things is a much more common operation than seeking
            // or querying length. The length here is a high watermark of
            // what's been written. So before we seek, if the current position
            // is greater than the length, we update that watermark.
            // When querying length later, we'll return whichever of the two
            // values is greater, thus if we write past length, length increases
            // because position increases, and if we seek backward, length remembers
            // the position it was in.
            // Seeking forward will not update the length.
            where = Math.Min(where, m_Capacity);
            if (m_Position > m_Length && where < m_Position)
            {
                m_Length = m_Position;
            }
            m_Position = where;
        }

        /// <summary>
        /// Truncate the stream by setting Length to the specified value.
        /// If Position is greater than the specified value, it will be moved as well.
        /// </summary>
        /// <param name="where">The value to truncate to. If -1, the current position will be used.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Truncate(int where = -1)
        {
            if (where == -1)
            {
                where = Position;
            }

            if (m_Position > where)
            {
                m_Position = where;
            }
            if(m_Length > where)
            {
                m_Length = where;
            }
        }

        /// <summary>
        /// Retrieve a BitWriter to be able to perform bitwise operations on the buffer.
        /// No bytewise operations can be performed on the buffer until bitWriter.Dispose() has been called.
        /// At the end of the operation, FastBufferWriter will remain byte-aligned.
        /// </summary>
        /// <returns>A BitWriter</returns>
        public BitWriter EnterBitwiseContext()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_InBitwiseContext = true;
#endif
            return new BitWriter(ref this);
        }

        internal unsafe void Grow()
        {
            var newSize = Math.Min(m_Capacity * 2, m_MaxCapacity);
            void* buffer = UnsafeUtility.Malloc(newSize, UnsafeUtility.AlignOf<byte>(), m_Allocator);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnsafeUtility.MemSet(buffer, 0, newSize);
#endif
            UnsafeUtility.MemCpy(buffer, m_BufferPointer, Length);
            UnsafeUtility.Free(m_BufferPointer, m_Allocator);
            m_BufferPointer = (byte*)buffer;
            m_Capacity = newSize;
        }

        /// <summary>
        /// Allows faster serialization by batching bounds checking.
        /// When you know you will be writing multiple fields back-to-back and you know the total size,
        /// you can call VerifyCanWrite() once on the total size, and then follow it with calls to
        /// WriteValue() instead of WriteValueSafe() for faster serialization.
        /// 
        /// Unsafe write operations will throw OverflowException in editor and development builds if you
        /// go past the point you've marked using VerifyCanWrite(). In release builds, OverflowException will not be thrown
        /// for performance reasons, since the point of using VerifyCanWrite is to avoid bounds checking in the following
        /// operations in release builds.
        /// </summary>
        /// <param name="bytes">Amount of bytes to write</param>
        /// <returns>True if the write is allowed, false otherwise</returns>
        /// <exception cref="InvalidOperationException">If called while in a bitwise context</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool VerifyCanWrite(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            if (m_Position + bytes > m_Capacity)
            {
                if (m_Capacity < m_MaxCapacity)
                {
                    Grow();
                }
                else
                {
                    return false;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_AllowedWriteMark = m_Position + bytes;
#endif
            return true;
        }
        
        /// <summary>
        /// Allows faster serialization by batching bounds checking.
        /// When you know you will be writing multiple fields back-to-back and you know the total size,
        /// you can call VerifyCanWrite() once on the total size, and then follow it with calls to
        /// WriteValue() instead of WriteValueSafe() for faster serialization.
        /// 
        /// Unsafe write operations will throw OverflowException in editor and development builds if you
        /// go past the point you've marked using VerifyCanWrite(). In release builds, OverflowException will not be thrown
        /// for performance reasons, since the point of using VerifyCanWrite is to avoid bounds checking in the following
        /// operations in release builds.
        /// </summary>
        /// <param name="value">The value you want to write</param>
        /// <returns>True if the write is allowed, false otherwise</returns>
        /// <exception cref="InvalidOperationException">If called while in a bitwise context</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe bool VerifyCanWriteValue<T>(in T value) where T : unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            int len = sizeof(T);
            if (m_Position + len > m_Capacity)
            {
                if (m_Capacity < m_MaxCapacity)
                {
                    Grow();
                }
                else
                {
                    return false;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            m_AllowedWriteMark = m_Position + len;
#endif
            return true;
        }
        
        /// <summary>
        /// Internal version of VerifyCanWrite.
        /// Differs from VerifyCanWrite only in that it won't ever move the AllowedWriteMark backward.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool VerifyCanWriteInternal(int bytes)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            if (m_Position + bytes > m_Capacity)
            {
                if (m_Capacity < m_MaxCapacity)
                {
                    Grow();
                }
                else
                {
                    return false;
                }
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_Position + bytes > m_AllowedWriteMark)
            {
                m_AllowedWriteMark = m_Position + bytes;
            }
#endif
            return true;
        }

        /// <summary>
        /// Returns an array representation of the underlying byte buffer.
        /// !!Allocates a new array!!
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte[] ToArray()
        {
            byte[] ret = new byte[Length];
            fixed (byte* b = ret)
            {
                UnsafeUtility.MemCpy(b, m_BufferPointer, Length);
            }
            return ret;
        }

        /// <summary>
        /// Gets a direct pointer to the underlying buffer
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte* GetUnsafePtr()
        {
            return m_BufferPointer;
        }

        /// <summary>
        /// Gets a direct pointer to the underlying buffer at the current read position
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe byte* GetUnsafePtrAtCurrentPosition()
        {
            return m_BufferPointer + m_Position;
        }

        /// <summary>
        /// Writes a boxed object in a standard format
        /// Named differently from other WriteValue methods to avoid accidental boxing
        /// </summary>
        /// <param name="value">The object to write</param>
        /// <param name="isNullable">
        /// If true, an extra byte will be written to indicate whether or not the value is null.
        /// Some types will always write this.
        /// </param>
        public void WriteObject(object value, bool isNullable = false)
        {
            if (isNullable || value.GetType().IsNullable())
            {
                bool isNull = value == null || (value is UnityEngine.Object o && o == null);

                WriteValueSafe(isNull);

                if (isNull)
                {
                    return;
                }
            }
            
            var type = value.GetType();
            var hasSerializer = SerializationTypeTable.Serializers.TryGetValue(type, out var serializer);
            if (hasSerializer)
            {
                serializer(ref this, value);
                return;
            }
            
            if (value is Array array)
            {
                WriteValueSafe(array.Length);

                for (int i = 0; i < array.Length; i++)
                {
                    WriteObject(array.GetValue(i));
                }

                return;
            }
            
            if (value.GetType().IsEnum)
            {
                switch (Convert.GetTypeCode(value))
                {
                    case TypeCode.Boolean:
                        WriteValueSafe((byte)value);
                        break;
                    case TypeCode.Char:
                        WriteValueSafe((char)value);
                        break;
                    case TypeCode.SByte:
                        WriteValueSafe((sbyte)value);
                        break;
                    case TypeCode.Byte:
                        WriteValueSafe((byte)value);
                        break;
                    case TypeCode.Int16:
                        WriteValueSafe((short)value);
                        break;
                    case TypeCode.UInt16:
                        WriteValueSafe((ushort)value);
                        break;
                    case TypeCode.Int32:
                        WriteValueSafe((int)value);
                        break;
                    case TypeCode.UInt32:
                        WriteValueSafe((uint)value);
                        break;
                    case TypeCode.Int64:
                        WriteValueSafe((long)value);
                        break;
                    case TypeCode.UInt64:
                        WriteValueSafe((ulong)value);
                        break;
                }
                return;
            }
            if (value is GameObject)
            {
                WriteValueSafe((GameObject)value);
                return;
            }
            if (value is NetworkObject)
            {
                WriteValueSafe((NetworkObject)value);
                return;
            }
            if (value is NetworkBehaviour)
            {
                WriteValueSafe((NetworkBehaviour)value);
                return;
            }
            if (value is INetworkSerializable)
            {
                //TODO ((INetworkSerializable)value).NetworkSerialize(new NetworkSerializer(this));
                return;
            }

            throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write type {value.GetType().Name} - it does not implement {nameof(INetworkSerializable)}");
        }

        /// <summary>
        /// Write an INetworkSerializable
        /// </summary>
        /// <param name="value">The value to write</param>
        /// <typeparam name="T"></typeparam>
        public void WriteNetworkSerializable<T>(in T value) where T : INetworkSerializable
        {
            // TODO
        }
        
        /// <summary>
        /// Get the required amount of space to write a GameObject
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetWriteSize(GameObject value)
        {
            return sizeof(ulong);
        }
        
        /// <summary>
        /// Get the required amount of space to write a GameObject
        /// </summary>
        /// <returns></returns>
        public static int GetGameObjectWriteSize()
        {
            return sizeof(ulong);
        }
        
        /// <summary>
        /// Write a GameObject
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteValue(GameObject value)
        {
            var networkObject = (value).GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(GameObject)} types that does not has a {nameof(NetworkObject)} component attached. {nameof(GameObject)}: {(value).name}");
            }

            if (!networkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {(value).name}");
            }

            WriteValue(networkObject.NetworkObjectId);
        }
        
        /// <summary>
        /// Write a GameObject
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling VerifyCanWrite.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteValueSafe(GameObject value)
        {
            var networkObject = (value).GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(GameObject)} types that does not has a {nameof(NetworkObject)} component attached. {nameof(GameObject)}: {(value).name}");
            }

            if (!networkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {(value).name}");
            }

            WriteValueSafe(networkObject.NetworkObjectId);
        }
        
        /// <summary>
        /// Get the required size to write a NetworkObject
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetWriteSize(NetworkObject value)
        {
            return sizeof(ulong);
        }
        
        /// <summary>
        /// Get the required size to write a NetworkObject
        /// </summary>
        /// <returns></returns>
        public static int GetNetworkObjectWriteSize()
        {
            return sizeof(ulong);
        }


        /// <summary>
        /// Write a NetworkObject
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteValue(in NetworkObject value)
        {
            if (!value.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {value.name}");
            }

            WriteValue(value.NetworkObjectId);
        }

        /// <summary>
        /// Write a NetworkObject
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling VerifyCanWrite.
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteValueSafe(NetworkObject value)
        {
            if (!value.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkObject)} types that are not spawned. {nameof(GameObject)}: {value.name}");
            }
            WriteValueSafe(value.NetworkObjectId);
        }
        
        /// <summary>
        /// Get the required size to write a NetworkBehaviour
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static int GetWriteSize(NetworkBehaviour value)
        {
            return sizeof(ulong) + sizeof(ushort);
        }
        
        
        /// <summary>
        /// Get the required size to write a NetworkBehaviour
        /// </summary>
        /// <returns></returns>
        public static int GetNetworkBehaviourWriteSize()
        {
            return sizeof(ulong) + sizeof(ushort);
        }


        /// <summary>
        /// Write a NetworkBehaviour
        /// </summary>
        /// <param name="value">The value to write</param>
        public void WriteValue(NetworkBehaviour value)
        {
            if (!value.HasNetworkObject || !value.NetworkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkBehaviour)} types that are not spawned. {nameof(GameObject)}: {(value).gameObject.name}");
            }

            WriteValue(value.NetworkObjectId);
            WriteValue(value.NetworkBehaviourId);
        }

        /// <summary>
        /// Write a NetworkBehaviour
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling VerifyCanWrite.
        /// </summary>
        /// <param name="value">The value to write</param>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="OverflowException"></exception>
        public void WriteValueSafe(NetworkBehaviour value)
        {
            if (!value.HasNetworkObject || !value.NetworkObject.IsSpawned)
            {
                throw new ArgumentException($"{nameof(FastBufferWriter)} cannot write {nameof(NetworkBehaviour)} types that are not spawned. {nameof(GameObject)}: {(value).gameObject.name}");
            }

            if (!VerifyCanWriteInternal(sizeof(ulong) + sizeof(ushort)))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            WriteValue(value.NetworkObjectId);
            WriteValue(value.NetworkBehaviourId);
        }

        /// <summary>
        /// Get the required size to write a string
        /// </summary>
        /// <param name="s">The string to write</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetWriteSize(string s, bool oneByteChars = false)
        {
            return sizeof(int) + s.Length * (oneByteChars ? sizeof(byte) : sizeof(char));
        }
        
        /// <summary>
        /// Writes a string
        /// </summary>
        /// <param name="s">The string to write</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        public unsafe void WriteValue(string s, bool oneByteChars = false)
        {
            WriteValue((uint)s.Length);
            int target = s.Length;
            if (oneByteChars)
            {
                for (int i = 0; i < target; ++i)
                {
                    WriteByte((byte) s[i]);
                }
            }
            else
            {
                fixed (char* native = s)
                {
                    WriteBytes((byte*)native, target * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Writes a string
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling VerifyCanWrite.
        /// </summary>
        /// <param name="s">The string to write</param>
        /// <param name="oneByteChars">Whether or not to use one byte per character. This will only allow ASCII</param>
        public unsafe void WriteValueSafe(string s, bool oneByteChars = false)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            int sizeInBytes = GetWriteSize(s, oneByteChars);
            
            if (!VerifyCanWriteInternal(sizeInBytes))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            
            WriteValue((uint)s.Length);
            int target = s.Length;
            if (oneByteChars)
            {
                for (int i = 0; i < target; ++i)
                {
                    WriteByte((byte) s[i]);
                }
            }
            else
            {
                fixed (char* native = s)
                {
                    WriteBytes((byte*)native, target * sizeof(char));
                }
            }
        }

        /// <summary>
        /// Get the required size to write an unmanaged array
        /// </summary>
        /// <param name="array">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        /// <param name="offset">Where in the array to start</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetWriteSize<T>(T[] array, int count = -1, int offset = 0) where T: unmanaged
        {
            int sizeInTs = count != -1 ? count : array.Length - offset;
            int sizeInBytes = sizeInTs * sizeof(T);
            return sizeof(int) + sizeInBytes;
        }

        /// <summary>
        /// Writes an unmanaged array
        /// </summary>
        /// <param name="array">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        /// <param name="offset">Where in the array to start</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValue<T>(T[] array, int count = -1, int offset = 0) where T: unmanaged
        {
            int sizeInTs = count != -1 ? count : array.Length - offset;
            int sizeInBytes = sizeInTs * sizeof(T);
            WriteValue(sizeInTs);
            fixed (T* native = array)
            {
                byte* bytes = (byte*)(native + offset);
                WriteBytes(bytes, sizeInBytes);
            }
        }

        /// <summary>
        /// Writes an unmanaged array
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling VerifyCanWrite.
        /// </summary>
        /// <param name="array">The array to write</param>
        /// <param name="count">The amount of elements to write</param>
        /// <param name="offset">Where in the array to start</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValueSafe<T>(T[] array, int count = -1, int offset = 0) where T: unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            int sizeInTs = count != -1 ? count : array.Length - offset;
            int sizeInBytes = sizeInTs * sizeof(T);
            
            if (!VerifyCanWriteInternal(sizeInBytes + sizeof(int)))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            WriteValue(sizeInTs);
            fixed (T* native = array)
            {
                byte* bytes = (byte*)(native + offset);
                WriteBytes(bytes, sizeInBytes);
            }
        }
        
        /// <summary>
        /// Write a partial value. The specified number of bytes is written from the value and the rest is ignored.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="bytesToWrite">Number of bytes</param>
        /// <param name="offsetBytes">Offset into the value to begin reading the bytes</param>
        /// <typeparam name="T"></typeparam>
        /// <exception cref="InvalidOperationException"></exception>
        /// <exception cref="OverflowException"></exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WritePartialValue<T>(T value, int bytesToWrite, int offsetBytes = 0) where T: unmanaged
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (m_Position + bytesToWrite > m_AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif

            byte* ptr = ((byte*) &value) + offsetBytes;
            byte* bufferPointer = m_BufferPointer + m_Position;
            BytewiseUtility.FastCopyBytes(bufferPointer, ptr, bytesToWrite);

            m_Position += bytesToWrite;
        }

        /// <summary>
        /// Write a byte to the stream.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteByte(byte value)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (m_Position + 1 > m_AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif
            m_BufferPointer[m_Position++] = value;
        }

        /// <summary>
        /// Write a byte to the stream.
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling VerifyCanWrite.
        /// </summary>
        /// <param name="value">Value to write</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteByteSafe(byte value)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanWriteInternal(1))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            m_BufferPointer[m_Position++] = value;
        }

        /// <summary>
        /// Write multiple bytes to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        /// <param name="offset">Offset into the buffer to begin writing</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytes(byte* value, int size, int offset = 0)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (m_Position + size > m_AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif
            UnsafeUtility.MemCpy((m_BufferPointer + m_Position), value + offset, size);
            m_Position += size;
        }
        
        /// <summary>
        /// Write multiple bytes to the stream
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling VerifyCanWrite.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        /// <param name="offset">Offset into the buffer to begin writing</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytesSafe(byte* value, int size, int offset = 0)
        {      
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanWriteInternal(size))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            UnsafeUtility.MemCpy((m_BufferPointer + m_Position), value + offset, size);
            m_Position += size;
        }
        
        /// <summary>
        /// Write multiple bytes to the stream
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        /// <param name="offset">Offset into the buffer to begin writing</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytes(byte[] value, int size, int offset = 0)
        {
            fixed (byte* ptr = value)
            {
                WriteBytes(ptr, size, offset);
            }
        }
        
        /// <summary>
        /// Write multiple bytes to the stream
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling VerifyCanWrite.
        /// </summary>
        /// <param name="value">Value to write</param>
        /// <param name="size">Number of bytes to write</param>
        /// <param name="offset">Offset into the buffer to begin writing</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteBytesSafe(byte[] value, int size, int offset = 0)
        {
            fixed (byte* ptr = value)
            {
                WriteBytesSafe(ptr, size, offset);
            }
        }
        
        /// <summary>
        /// Copy the contents of this writer into another writer.
        /// The contents will be copied from the beginning of this writer to its current position.
        /// They will be copied to the other writer starting at the other writer's current position.
        /// </summary>
        /// <param name="other">Writer to copy to</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void CopyTo(FastBufferWriter other)
        {
            other.WriteBytes(m_BufferPointer, m_Position);
        }
        
        /// <summary>
        /// Copy the contents of another writer into this writer.
        /// The contents will be copied from the beginning of the other writer to its current position.
        /// They will be copied to this writer starting at this writer's current position.
        /// </summary>
        /// <param name="other">Writer to copy to</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void CopyFrom(FastBufferWriter other)
        {
            WriteBytes(other.m_BufferPointer, other.m_Position);
        }
        
        /// <summary>
        /// Get the size required to write an unmanaged value
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe int GetWriteSize<T>(in T value) where T : unmanaged
        {
            return sizeof(T);
        }

        /// <summary>
        /// Get the size required to write an unmanaged value of type T
        /// </summary>
        /// <param name="value"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static unsafe int GetWriteSize<T>() where T : unmanaged
        {
            return sizeof(T);
        }

        /// <summary>
        /// Write a value of any unmanaged type (including unmanaged structs) to the buffer.
        /// It will be copied into the buffer exactly as it exists in memory.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValue<T>(in T value) where T : unmanaged
        {
            int len = sizeof(T);
            
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
            if (m_Position + len > m_AllowedWriteMark)
            {
                throw new OverflowException("Attempted to write without first calling VerifyCanWrite()");
            }
#endif
            
            T* pointer = (T*)(m_BufferPointer+m_Position);
            *pointer = value;
            m_Position += len;
        }
        
        /// <summary>
        /// Write a value of any unmanaged type (including unmanaged structs) to the buffer.
        /// It will be copied into the buffer exactly as it exists in memory.
        ///
        /// "Safe" version - automatically performs bounds checking. Less efficient than bounds checking
        /// for multiple writes at once by calling VerifyCanWrite.
        /// </summary>
        /// <param name="value">The value to copy</param>
        /// <typeparam name="T">Any unmanaged type</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void WriteValueSafe<T>(in T value) where T : unmanaged
        {
            int len = sizeof(T);
            
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (m_InBitwiseContext)
            {
                throw new InvalidOperationException(
                    "Cannot use BufferWriter in bytewise mode while in a bitwise context.");
            }
#endif
            
            if (!VerifyCanWriteInternal(len))
            {
                throw new OverflowException("Writing past the end of the buffer");
            }
            
            T* pointer = (T*)(m_BufferPointer+m_Position);
            *pointer = value;
            m_Position += len;
        }
    }
}