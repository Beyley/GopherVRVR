namespace GopherVRVR;

public class VertexArrayObject : IDisposable
{
    public uint Handle;

    public VertexArrayObject()
    {
        this.Handle = Global.gl.CreateVertexArray();
    }

    public void Dispose()
    {
        Global.gl.DeleteVertexArray(this.Handle);
        this.Handle = 0xAAAAAAAA;
    }

    public void Bind()
    {
        Global.gl.BindVertexArray(this.Handle);
    }

    public void Unbind()
    {
        Global.gl.BindVertexArray(0);
    }

    ~VertexArrayObject()
    {
        Global.DeadObjects.Enqueue(this);
        GC.SuppressFinalize(this);
    }
}