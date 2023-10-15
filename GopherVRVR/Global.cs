using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.InteropServices;
using NotEnoughLogs;
using NotEnoughLogs.Behaviour;
using Silk.NET.Input;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.OpenGL.Extensions.ImGui;
using Silk.NET.Windowing;

namespace GopherVRVR;

public static unsafe class Global
{
    // ReSharper disable once InconsistentNaming
    public static GL gl = null!;
    public static IWindow Window = null!;
    public static ImGuiController ImGuiController = null!;
    public static IInputContext Input = null!;

    public static Logger Logger = new(new LoggerConfiguration
    {
        Behaviour = new QueueLoggingBehaviour(),
        MaxLevel = LogLevel.Trace
    });

    public static readonly ConcurrentQueue<IDisposable> DeadObjects = new();

    private static Shader _Shader = null!;

    private static Vector2 _LastMousePosition;

    private static IKeyboard _Kb = null!;

    private static VertexArrayObject _Vao = null!;
    private static Buffer<Vertex> _WorldVertexBuffer = null!;

    //Setup the camera's location, and relative up and right directions
    public static Vector3 CameraPosition = new(0.0f, 0.0f, 3.0f);
    public static Vector3 CameraFront = new(0.0f, 0.0f, -1.0f);
    public static Vector3 CameraUp = Vector3.UnitY;
    public static Vector3 CameraDirection = Vector3.Zero;
    public static float CameraYaw = -90f;
    public static float CameraPitch;
    public static float CameraZoom = 45f;

    public static void Start()
    {
        Logger.LogInfo(LogCategory.ProgramState, "Starting GopherVRVR");
        WindowOptions options = WindowOptions.Default with
        {
            Position = new Vector2D<int>(2000, 100),
            API = new GraphicsAPI(
                ContextAPI.OpenGL,
                ContextProfile.Core,
#if DEBUG
                ContextFlags.Debug,
#else
                ContextFlags.Default,
#endif
                new APIVersion(4, 6)
            ),
            PreferredDepthBufferBits = 32
        };
        Window = Silk.NET.Windowing.Window.Create(options);

        Window.Load += WindowLoad;
        Window.Render += Render;
        Window.Update += Update;
        Window.Closing += () => Logger.Dispose();

        Window.Run();
    }

    public static void WindowLoad()
    {
        Logger.LogInfo(LogCategory.ProgramState, "Window loading...");
        gl = Window.CreateOpenGL();
        Input = Window.CreateInput();
        ImGuiController = new ImGuiController(gl, Window, Input);

        _Shader = new Shader();

        List<Vertex> geometryBuilder = new();
        
        const float floorHeight = -0.6f;
        const float worldWidth = 10;
        const float roadWidth = 2;
        const float grassWidth = (worldWidth - roadWidth);
        Vector4 grassColor = new Vector4(0, 0.2f, 0.1f, 1);
        Vector4 roadColor = new Vector4(0.21f, 0.21f, 0.21f, 1);

        { //Create grass
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth, floorHeight, -worldWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth + grassWidth, floorHeight, -worldWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth, floorHeight, -worldWidth + grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth, floorHeight, -worldWidth + grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth + grassWidth, floorHeight, -worldWidth + grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth + grassWidth, floorHeight, -worldWidth), new Vector2(0, 0), grassColor));

            geometryBuilder.Add(new Vertex(new Vector3(worldWidth, floorHeight, -worldWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth - grassWidth, floorHeight, -worldWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth, floorHeight, -worldWidth + grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth, floorHeight, -worldWidth + grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth - grassWidth, floorHeight, -worldWidth + grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth - grassWidth, floorHeight, -worldWidth), new Vector2(0, 0), grassColor));

            geometryBuilder.Add(new Vertex(new Vector3(worldWidth, floorHeight, worldWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth - grassWidth, floorHeight, worldWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth, floorHeight, worldWidth - grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth, floorHeight, worldWidth - grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth - grassWidth, floorHeight, worldWidth - grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth - grassWidth, floorHeight, worldWidth), new Vector2(0, 0), grassColor));

            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth, floorHeight, worldWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth + grassWidth, floorHeight, worldWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth, floorHeight, worldWidth - grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth, floorHeight, worldWidth - grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth + grassWidth, floorHeight, worldWidth - grassWidth), new Vector2(0, 0), grassColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth + grassWidth, floorHeight, worldWidth), new Vector2(0, 0), grassColor));
        }

        { //Create roads
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth + grassWidth, floorHeight, worldWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth - grassWidth, floorHeight, worldWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth + grassWidth, floorHeight, -worldWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth - grassWidth, floorHeight, worldWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth - grassWidth, floorHeight, -worldWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth + grassWidth, floorHeight, -worldWidth), new Vector2(0, 0), roadColor));
            
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth, floorHeight, worldWidth - grassWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth, floorHeight, worldWidth - grassWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth, floorHeight, -worldWidth + grassWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth, floorHeight, worldWidth - grassWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(-worldWidth, floorHeight, -worldWidth + grassWidth), new Vector2(0, 0), roadColor));
            geometryBuilder.Add(new Vertex(new Vector3(worldWidth, floorHeight, -worldWidth + grassWidth), new Vector2(0, 0), roadColor));
        }
        
        _WorldVertexBuffer = new Buffer<Vertex>((uint)geometryBuilder.Count, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);

        _Vao = new VertexArrayObject();
        _WorldVertexBuffer.Bind();
        _WorldVertexBuffer.SetData(geometryBuilder.ToArray());
        _WorldVertexBuffer.Unbind();
        SetupVAO(_WorldVertexBuffer, _Vao);
        
        gl.DepthFunc(DepthFunction.Less);

        _Kb = Input.Keyboards[0];
        foreach (IMouse mouse in Input.Mice)
        {
            mouse.Cursor.CursorMode = CursorMode.Raw;
            mouse.MouseMove += MouseMove;
            mouse.Scroll += MouseScroll;
        }
        
        List<Vertex> geometryBuilderItem = new();
        geometryBuilderItem.AddRange(new Vertex[]
        {
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 0.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1.0f, 0.0f), new Vector4(0, 1, 1, 1)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1.0f, 1.0f), new Vector4(1, 0, 1, 1)),
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 0.0f), new Vector4(1, 1, 0, 1)),

            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0.0f, 0.0f), new Vector4(1, 0, 0, 1)),
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0.0f, 1.0f), new Vector4(1, 0, 1, 1)),
            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0.0f, 0.0f), new Vector4(0, 0, 1, 1)),

            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(1.0f, 1.0f), new Vector4(1, 1, 0, 1)),
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector4(0, 1, 1, 1)),
            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0.0f, 0.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector4(1, 1, 1, 1)),

            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector4(1, 1, 0, 1)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector4(1, 0, 1, 1)),
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(0.0f, 0.0f), new Vector4(0, 1, 0, 1)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector4(1, 1, 1, 1)),

            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, -0.5f, -0.5f), new Vector2(1.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector4(1, 0, 0, 1)),
            new(new Vector3(0.5f, -0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(-0.5f, -0.5f, 0.5f), new Vector2(0.0f, 0.0f), new Vector4(0, 1, 1, 1)),
            new(new Vector3(-0.5f, -0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector4(1, 1, 1, 1)),

            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, 0.5f, -0.5f), new Vector2(1.0f, 1.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(0.5f, 0.5f, 0.5f), new Vector2(1.0f, 0.0f), new Vector4(1, 0, 1, 1)),
            new(new Vector3(-0.5f, 0.5f, 0.5f), new Vector2(0.0f, 0.0f), new Vector4(1, 1, 1, 1)),
            new(new Vector3(-0.5f, 0.5f, -0.5f), new Vector2(0.0f, 1.0f), new Vector4(1, 1, 1, 1))
        });
        Buffer<Vertex> itemBuffer = new((uint)geometryBuilderItem.Count, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        itemBuffer.Bind();
        itemBuffer.SetData(geometryBuilderItem.ToArray());
        itemBuffer.Unbind();
        Item item = new Item(itemBuffer, Matrix4x4.CreateTranslation(new Vector3(1, 1, 1)), new VertexArrayObject());
        SetupVAO(item.Buffer, item.Vao);
        Items.Add(item);
    }

    public static void SetupVAO(Buffer<Vertex> buf, VertexArrayObject vao)
    {
        vao.Bind();
        buf.Bind();
        gl.EnableVertexAttribArray(0);
        gl.EnableVertexAttribArray(1);
        gl.EnableVertexAttribArray(2);

        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), null);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord)));
        gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.Color)));
        vao.Unbind();
    }
    
    private static void MouseScroll(IMouse arg1, ScrollWheel arg2)
    {
        CameraZoom -= arg2.Y * 2;
        CameraZoom = Math.Clamp(CameraZoom, 10, 90);
    }
    private static void MouseMove(IMouse mouse, Vector2 position)
    {
        float lookSensitivity = 0.1f;
        if (_LastMousePosition == default) { _LastMousePosition = position; }
        else
        {
            float xOffset = (position.X - _LastMousePosition.X) * lookSensitivity;
            float yOffset = (position.Y - _LastMousePosition.Y) * lookSensitivity;
            _LastMousePosition = position;

            CameraYaw += xOffset;
            CameraPitch -= yOffset;

            //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
            CameraPitch = Math.Clamp(CameraPitch, -89.0f, 89.0f);

            CameraDirection.X = MathF.Cos(MathHelper.DegreesToRadians(CameraYaw)) * MathF.Cos(MathHelper.DegreesToRadians(CameraPitch));
            CameraDirection.Y = MathF.Sin(MathHelper.DegreesToRadians(CameraPitch));
            CameraDirection.Z = MathF.Sin(MathHelper.DegreesToRadians(CameraYaw)) * MathF.Cos(MathHelper.DegreesToRadians(CameraPitch));
            CameraFront = Vector3.Normalize(CameraDirection);
        }
    }

    public static List<Item> Items = new();
    
    public static void Render(double dt)
    {
        //Destroy any disposal objects that are queued for deletion
        while (DeadObjects.TryDequeue(out IDisposable? obj))
        {
            obj.Dispose();
        }

        //Enable depth testing
        gl.Enable(EnableCap.DepthTest);

        //Clear the screen
        gl.ClearColor(0, 14.0f / 256.0f, 38.0f / 256.0f, 1);
        gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        Matrix4x4 model = Matrix4x4.Identity;
        Matrix4x4 view = Matrix4x4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CameraZoom), (float)Window.FramebufferSize.X / Window.FramebufferSize.Y, 0.1f, 100.0f);
        
        //Bind our used objects
        _Shader.Bind();
        _Vao.Bind();
        
        _Shader.SetUniform("ProjectionMatrix", projection);
        _Shader.SetUniform("ViewMatrix", view);
        _Shader.SetUniform("ModelMatrix", model);
        _WorldVertexBuffer.Bind();
        //Draw our world vertex buffer
        gl.DrawArrays(PrimitiveType.Triangles, 0, _WorldVertexBuffer.Count);
        foreach (Item item in Items)
        {
            _Shader.SetUniform("ModelMatrix", item.ModelMatrix);
            item.Vao.Bind();
            item.Buffer.Bind();
            gl.DrawArrays(PrimitiveType.Triangles, 0, item.Buffer.Count);
        }

        ImGuiController.Render();
    }

    public static void Update(double dt)
    {
        ImGuiController.Update((float)dt);

        float moveSpeed = 2.5f * (float)dt;

        if (_Kb.IsKeyPressed(Key.W))
            //Move forwards
            CameraPosition += new Vector3(MathF.Cos(MathHelper.DegreesToRadians(CameraYaw)), 0, MathF.Sin(MathHelper.DegreesToRadians(CameraYaw))) * moveSpeed;
        if (_Kb.IsKeyPressed(Key.S))
            //Move backwards
            CameraPosition -= new Vector3(MathF.Cos(MathHelper.DegreesToRadians(CameraYaw)), 0, MathF.Sin(MathHelper.DegreesToRadians(CameraYaw))) * moveSpeed;
        if (_Kb.IsKeyPressed(Key.A))
            //Move left
            CameraPosition -= Vector3.Normalize(Vector3.Cross(CameraFront, CameraUp)) * moveSpeed;
        if (_Kb.IsKeyPressed(Key.D))
            //Move right
            CameraPosition += Vector3.Normalize(Vector3.Cross(CameraFront, CameraUp)) * moveSpeed;
    }
}