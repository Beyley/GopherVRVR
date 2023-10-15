using System.Numerics;

namespace GopherVRVR;

public class Item
{
    public Matrix4x4 ModelMatrix;

    public Item(Matrix4x4 modelMatrix)
    {
        this.ModelMatrix = modelMatrix;
    }
}