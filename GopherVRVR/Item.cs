using System.Numerics;

namespace GopherVRVR;

public class Item
{
    public VertexArrayObject Vao;
    public Buffer<Vertex> Buffer;
    public Matrix4x4 ModelMatrix;

    public Item(Buffer<Vertex> buffer, Matrix4x4 modelMatrix, VertexArrayObject vao)
    {
        this.Buffer = buffer;
        this.ModelMatrix = modelMatrix;
        this.Vao = vao;
    }
}