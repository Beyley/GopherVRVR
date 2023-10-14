using System.Numerics;

namespace GopherVRVR;

public struct Vertex
{
    public Vector3 Position;
    public Vector2 TexCoord;
    public Vector4 Color;

    public Vertex(Vector3 position, Vector2 texCoord, Vector4 color)
    {
        this.Position = position;
        this.TexCoord = texCoord;
        this.Color = color;
    }
}