using Godot;
using System;
using System.Text;

/// <summary>
/// Base class for network messages in a 3D arena FPS.
/// Supports serialization/deserialization of primitives, arrays, enums, Vector2/Vector3, and Quaternion.
/// </summary>
public class Message
{
    public Msg MessageType { get; set; }
    public ENetPacketFlags ENetFlags { get; set; }
    public int Flags => (int)ENetFlags;

    protected int _dataPartIndex;
    protected int _dataSize = 0;
    protected byte[] _data;

    // -------------------
    // Compute buffer size
    // -------------------
    protected virtual int BufferSize()
    {
        _dataSize = 1; // first byte = MessageType
        return _dataSize;
    }

    // -------------------
    // Pack / Unpack
    // -------------------
    public virtual byte[] WriteMessage()
    {
        _data = new byte[BufferSize()];
        _dataPartIndex = 0;

        Write(MessageType); // always first

        return _data;
    }

    public virtual void ReadMessage(byte[] data)
    {
        _data = data ?? throw new ArgumentException("Data is null or empty");
        _dataPartIndex = 0;

        byte raw;
        Read(out raw);
        MessageType = (Msg)raw;
    }

    public static Msg GetType(byte[] data)
    {
        if (data == null || data.Length == 0)
            throw new ArgumentException("Data is null or empty.");
        return (Msg)data[0];
    }

    public static T FromData<T>(byte[] data) where T : Message, new()
    {
        T packet = new T();
        packet.ReadMessage(data);
        return packet;
    }

    public static Message CreateEmpty(Msg packetType, ENetPacketFlags flags)
    {
        var packet = new Message();
        packet.MessageType = packetType;
        packet.ENetFlags = flags;
        return packet;
    }

    // -------------------
    // Add methods (for buffer size)
    // -------------------
    protected void Add(byte _) => _dataSize += 1;
    protected void Add(bool _) => _dataSize += 1;
    protected void Add(int _) => _dataSize += 4;
    protected void Add(uint _) => _dataSize += 4;

    protected void Add(float _) => _dataSize += 4;
    protected void Add(ushort _) => _dataSize += 2;
    protected void Add(string s) => _dataSize += 1 + (s != null ? Encoding.UTF8.GetByteCount(s) : 0);

    protected void Add(byte[] arr) => _dataSize += 1 + arr.Length;
    protected void Add(int[] arr) => _dataSize += 4 + 4 * arr.Length;
    protected void Add(bool[] arr) => _dataSize += 1 + arr.Length;
    protected void Add(string[] arr)
    {
        _dataSize += 1;
        foreach (var s in arr)
            _dataSize += 1 + (s != null ? Encoding.UTF8.GetByteCount(s) : 0);
    }

    protected void Add(Vector2[] arr) => _dataSize += 4 + 8 * arr.Length;
    protected void Add(Vector3 value) => _dataSize += 12;
    protected void Add(Vector3[] arr) => _dataSize += 4 + 12 * arr.Length;
    protected void Add(Quaternion value) => _dataSize += 16;
    protected void Add(Quaternion[] arr) => _dataSize += 4 + 16 * arr.Length;

    // Generic enum Add
    protected void Add<T>(T value) where T : Enum
    {
        if (Enum.GetUnderlyingType(typeof(T)) != typeof(byte))
            throw new InvalidOperationException($"{typeof(T)} is not a byte enum!");
        _dataSize += 1;
    }

    protected void Add<T>(T[] arr) where T : Enum
    {
        _dataSize += 1; // for count
        _dataSize += arr.Length; // each element is 1 byte
    }

    // -------------------
    // Write overloads
    // -------------------
    protected void Write(byte value) => _data[_dataPartIndex++] = value;
    protected void Write(bool value) => Write((byte)(value ? 1 : 0));
    protected void Write(int value) { Array.Copy(BitConverter.GetBytes(value), 0, _data, _dataPartIndex, 4); _dataPartIndex += 4; }

    protected void Write(uint value) { Array.Copy(BitConverter.GetBytes(value), 0, _data, _dataPartIndex, 4); _dataPartIndex += 4; }
    protected void Write(float value) { Array.Copy(BitConverter.GetBytes(value), 0, _data, _dataPartIndex, 4); _dataPartIndex += 4; }
    protected void Write(ushort value) { _data[_dataPartIndex++] = (byte)(value & 0xFF); _data[_dataPartIndex++] = (byte)((value >> 8) & 0xFF); }
    protected void Write(string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        if (bytes.Length > 255) Array.Resize(ref bytes, 255);
        Write((byte)bytes.Length);
        foreach (var b in bytes) Write(b);
    }

    protected void Write(byte[] arr) { foreach (var b in arr) Write(b); }
    protected void Write(int[] arr) { Write(arr.Length); foreach (var i in arr) Write(i); }
    protected void Write(bool[] arr) { Write((byte)arr.Length); foreach (var b in arr) Write(b); }
    protected void Write(string[] arr) { Write((byte)arr.Length); foreach (var s in arr) Write(s); }
    protected void Write(Vector2[] arr) { Write(arr.Length); foreach (var v in arr) { Write((int)v.X); Write((int)v.Y); } }
    protected void Write(Vector3 value) { Write(value.X); Write(value.Y); Write(value.Z); }
    protected void Write(Vector3[] arr) { Write(arr.Length); foreach (var v in arr) Write(v); }
    protected void Write(Quaternion value) { Write(value.X); Write(value.Y); Write(value.Z); Write(value.W); }
    protected void Write(Quaternion[] arr) { Write(arr.Length); foreach (var q in arr) Write(q); }

    protected void Write<T>(T value) where T : Enum
    {
        if (Enum.GetUnderlyingType(typeof(T)) != typeof(byte))
            throw new InvalidOperationException($"{typeof(T)} is not a byte enum!");
        Write(Convert.ToByte(value));
    }

    protected void Write<T>(T[] arr) where T : Enum
    {
        Write((byte)arr.Length);
        foreach (var e in arr) Write(e);
    }

    // -------------------
    // Read overloads
    // -------------------
    protected void Read(out byte value) => value = _data[_dataPartIndex++];
    protected void Read(out bool value) { byte b = _data[_dataPartIndex++]; value = b != 0; }
    protected void Read(out int value) { value = BitConverter.ToInt32(_data, _dataPartIndex); _dataPartIndex += 4; }
    protected void Read(out uint value) { value = BitConverter.ToUInt32(_data, _dataPartIndex); _dataPartIndex += 4; }

    protected void Read(out float value) { value = BitConverter.ToSingle(_data, _dataPartIndex); _dataPartIndex += 4; }
    protected void Read(out ushort value) { value = (ushort)(_data[_dataPartIndex] | (_data[_dataPartIndex + 1] << 8)); _dataPartIndex += 2; }
    protected void Read(out string value)
    {
        byte length = _data[_dataPartIndex++];
        value = Encoding.UTF8.GetString(_data, _dataPartIndex, length);
        _dataPartIndex += length;
    }

    protected void Read(out byte[] arr)
    {
        Read(out byte count);
        arr = new byte[count];
        for (int i = 0; i < count; i++) Read(out arr[i]);
    }

    protected void Read(out int[] arr)
    {
        Read(out int count);
        arr = new int[count];
        for (int i = 0; i < count; i++) Read(out arr[i]);
    }

    protected void Read(out bool[] arr)
    {
        Read(out byte count);
        arr = new bool[count];
        for (int i = 0; i < count; i++) Read(out arr[i]);
    }

    protected void Read(out string[] arr)
    {
        Read(out byte count);
        arr = new string[count];
        for (int i = 0; i < count; i++) Read(out arr[i]);
    }

    protected void Read(out Vector2[] arr)
    {
        Read(out int count);
        arr = new Vector2[count];
        for (int i = 0; i < count; i++)
        {
            Read(out int x);
            Read(out int y);
            arr[i] = new Vector2(x, y);
        }
    }

    protected void Read(out Vector3 value)
    {
        Read(out float x);
        Read(out float y);
        Read(out float z);
        value = new Vector3(x, y, z);
    }

    protected void Read(out Vector3[] arr)
    {
        Read(out int count);
        arr = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            Read(out arr[i]);
        }
    }

    protected void Read(out Quaternion value)
    {
        Read(out float x);
        Read(out float y);
        Read(out float z);
        Read(out float w);
        value = new Quaternion(x, y, z, w);
    }

    protected void Read(out Quaternion[] arr)
    {
        Read(out int count);
        arr = new Quaternion[count];
        for (int i = 0; i < count; i++)
        {
            Read(out arr[i]);
        }
    }

    // Generic enum Read
    protected void Read<T>(out T value) where T : Enum
    {
        if (Enum.GetUnderlyingType(typeof(T)) != typeof(byte))
            throw new InvalidOperationException($"{typeof(T)} is not a byte enum!");
        Read(out byte raw);
        value = (T)(object)raw;
    }

    protected void Read<T>(out T[] arr) where T : Enum
    {
        Read(out byte count);
        arr = new T[count];
        for (int i = 0; i < count; i++) Read(out arr[i]);
    }
}