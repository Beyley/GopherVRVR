using Silk.NET.OpenGL;

namespace GopherVRVR;

public unsafe class Buffer<T> : IDisposable where T : unmanaged
{
    public readonly uint Count;
    public readonly nuint SizeInBytes;
    public readonly BufferTargetARB Target;
    public readonly BufferUsageARB Usage;
    public uint Handle;

    public Buffer(uint count, BufferTargetARB target, BufferUsageARB usage)
    {
        this.Target = target;
        this.Usage = usage;
        this.Count = count;
        this.SizeInBytes = (nuint)(count * sizeof(T));
        this.Handle = Global.gl.CreateBuffer();

        this.Bind();
        //Allocate the right size for the buffer
        Global.gl.BufferData(target, this.SizeInBytes, null, usage);
        this.Unbind();
    }

    public void Dispose()
    {
        Global.gl.DeleteBuffer(this.Handle);
        this.Handle = 0xAAAAAAAA;
    }

    public void Bind()
    {
        Global.gl.BindBuffer(this.Target, this.Handle);
    }

    public void Unbind()
    {
        Global.gl.BindBuffer(this.Target, 0);
    }

    public void SetData(ReadOnlySpan<T> data, nint offset = 0)
    {
        this.Bind();
        fixed (void* ptr = data)
        {
            Global.gl.BufferSubData(this.Target, offset, (nuint)(data.Length * sizeof(T)), ptr);
        }
        this.Unbind();
    }
    ~Buffer()
    {
        Global.DeadObjects.Enqueue(this);
        GC.SuppressFinalize(this);
    }
}