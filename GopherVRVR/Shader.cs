using System.Numerics;
using Silk.NET.OpenGL;

namespace GopherVRVR;

public unsafe class Shader : IDisposable
{
    private readonly Dictionary<string, int> _uniformLocations = new();
    public uint ProgramHandle;

    public Shader()
    {
        string vertSource = ResourceHelper.GetStringResource("World.vert", typeof(Shader));
        string fragSource = ResourceHelper.GetStringResource("World.frag", typeof(Shader));

        uint vertShader = Global.gl.CreateShader(ShaderType.VertexShader);
        uint fragShader = Global.gl.CreateShader(ShaderType.FragmentShader);

        Global.gl.ShaderSource(vertShader, vertSource);
        Global.gl.ShaderSource(fragShader, fragSource);

        Global.gl.CompileShader(vertShader);
        if (Global.gl.GetShader(vertShader, ShaderParameterName.CompileStatus) == 0)
            throw new Exception($"Failed to compile vertex shader: {Global.gl.GetShaderInfoLog(vertShader)}");

        Global.gl.CompileShader(fragShader);
        if (Global.gl.GetShader(fragShader, ShaderParameterName.CompileStatus) == 0)
            throw new Exception($"Failed to compile vertex shader: {Global.gl.GetShaderInfoLog(fragShader)}");

        this.ProgramHandle = Global.gl.CreateProgram();
        Global.gl.AttachShader(this.ProgramHandle, vertShader);
        Global.gl.AttachShader(this.ProgramHandle, fragShader);

        Global.gl.LinkProgram(this.ProgramHandle);
        if (Global.gl.GetProgram(this.ProgramHandle, ProgramPropertyARB.LinkStatus) == 0)
            throw new Exception($"Failed to link program: {Global.gl.GetProgramInfoLog(this.ProgramHandle)}");

        Global.gl.DeleteShader(vertShader);
        Global.gl.DeleteShader(fragShader);

        Global.Logger.LogDebug(LogCategory.Graphics, $"Compiled program {this.ProgramHandle}");
    }

    public void Dispose()
    {
        Global.gl.DeleteProgram(this.ProgramHandle);
        this.ProgramHandle = 0xAAAAAAAA;
    }

    public void Bind()
    {
        Global.gl.UseProgram(this.ProgramHandle);
    }

    public void Unbind()
    {
        Global.gl.UseProgram(0);
    }
    public int GetUniformLocation(string name)
    {
        if (this._uniformLocations.TryGetValue(name, out int pos)) return pos;

        int loc = Global.gl.GetUniformLocation(this.ProgramHandle, name);

        this._uniformLocations[name] = loc;

        return loc;
    }

    public void SetUniform(string name, Matrix4x4 mat)
    {
        Global.gl.UniformMatrix4(this.GetUniformLocation(name), 1, false, (float*)&mat);
    }
    ~Shader()
    {
        Global.DeadObjects.Enqueue(this);
        GC.SuppressFinalize(this);
    }
}