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
    private const float floorHeight = -0.6f;
    private const float worldWidth = 10;
    private const float roadWidth = 2;
    private const float grassWidth = worldWidth - roadWidth;
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
    private static Buffer<InstanceData> _WorldInstanceBuffer = null!;
    private static Buffer<Vertex> _WorldVertexBuffer = null!;

    //Setup the camera's location, and relative up and right directions
    public static Vector3 CameraPosition = new(0.0f, 0.0f, 3.0f);
    public static Vector3 CameraFront = new(0.0f, 0.0f, -1.0f);
    public static Vector3 CameraUp = Vector3.UnitY;
    public static Vector3 CameraDirection = Vector3.Zero;
    public static float CameraYaw = -90f;
    public static float CameraPitch;
    public static float CameraZoom = 45f;

    private static VertexArrayObject _ItemVao = null!;
    private static Buffer<Vertex> _ItemBuffer = null!;
    private static Buffer<InstanceData> _InstanceData = null!;
    private static readonly Vector4 grassColor = new(0, 0.2f, 0.1f, 1);
    private static readonly Vector4 roadColor = new(0.21f, 0.21f, 0.21f, 1);

    private static VertexArrayObject KioskVao = null!;
    private static Buffer<Vertex> KioskBuffer = null!;
    private static Buffer<InstanceData> KioskInstanceData = null!;

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
        _WorldInstanceBuffer = new Buffer<InstanceData>(1, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        _WorldInstanceBuffer.Bind();
        _WorldInstanceBuffer.SetData(new[]
        {
            new InstanceData
            {
                Matrix = Matrix4x4.Identity
            }
        });
        _WorldInstanceBuffer.Unbind();
        SetupVAO(_WorldVertexBuffer, _WorldInstanceBuffer, _Vao);

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
        _ItemBuffer = new((uint)geometryBuilderItem.Count, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        _ItemBuffer.Bind();
        _ItemBuffer.SetData(geometryBuilderItem.ToArray());
        _ItemBuffer.Unbind();
        _InstanceData = AAAAA(5, false);

        _ItemVao = new VertexArrayObject();
        SetupVAO(_ItemBuffer, _InstanceData, _ItemVao);

        Vector3 rlN_i0 = new(120, -528, 44);
        Vector3 rlN_i1 = new(-120, -528, 44);
        Vector3 rlN_i2 = new(0, -880, -28);
        Vector3 rlN_i3 = new(120, -528, -100);
        Vector3 rlN_i4 = new(-120, -528, -100);

        Vector3 rlN_i10 = new(216, -528, -244);
        Vector3 rlN_i11 = new(216, -528, 188);
        Vector3 rlN_i12 = new(-216, -528, 188);
        Vector3 rlN_i13 = new(-216, -528, -244);
        Vector3 rlN_i14 = new(-216, -240, -244);
        Vector3 rlN_i15 = new(216, -240, -244);
        Vector3 rlN_i16 = new(216, -240, 188);
        Vector3 rlN_i17 = new(-216, -240, 188);

        Vector3 rlN_i20 = new(120, -240, 96);
        Vector3 rlN_i21 = new(216, 0, 188);
        Vector3 rlN_i22 = new(-216, 0, 188);
        Vector3 rlN_i23 = new(-120, -240, 96);
        Vector3 rlN_i24 = new(120, -240, -148);
        Vector3 rlN_i25 = new(216, 0, -244);
        Vector3 rlN_i26 = new(-216, 0, -244);
        Vector3 rlN_i27 = new(-120, -240, -148);

        List<Vertex> kioskBuilder = new();

        Vector4 kioskColor = new(1, 1, 1, 1);
        { //Top
            kioskBuilder.Add(new Vertex(rlN_i1, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i2, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i0, new Vector2(0, 0), kioskColor));

            kioskBuilder.Add(new Vertex(rlN_i0, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i2, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i3, new Vector2(0, 0), kioskColor));

            kioskBuilder.Add(new Vertex(rlN_i3, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i2, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i4, new Vector2(0, 0), kioskColor));

            kioskBuilder.Add(new Vertex(rlN_i4, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i2, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i1, new Vector2(0, 0), kioskColor));
        }

        { //Middle
            //Face 1
            kioskBuilder.Add(new Vertex(rlN_i16, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i11, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i10, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i16, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i10, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i15, new Vector2(0, 0), kioskColor));
            
            //Face 2
            kioskBuilder.Add(new Vertex(rlN_i17, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i12, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i11, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i17, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i11, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i16, new Vector2(0, 0), kioskColor));
            
            //Face 3
            kioskBuilder.Add(new Vertex(rlN_i14, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i13, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i12, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i14, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i12, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i17, new Vector2(0, 0), kioskColor));
            
            //Face 4
            kioskBuilder.Add(new Vertex(rlN_i15, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i10, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i13, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i15, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i13, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i14, new Vector2(0, 0), kioskColor));
            
            //Bottom
            kioskBuilder.Add(new Vertex(rlN_i14, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i17, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i16, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i14, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i16, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i15, new Vector2(0, 0), kioskColor));
            
            //Top
            kioskBuilder.Add(new Vertex(rlN_i12, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i13, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i10, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i12, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i10, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i11, new Vector2(0, 0), kioskColor));
        }

        { //Bottom
            //Face 1
            kioskBuilder.Add(new Vertex(rlN_i21, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i20, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i24, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i21, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i24, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i25, new Vector2(0, 0), kioskColor)); 
            
            //Face 2
            kioskBuilder.Add(new Vertex(rlN_i22, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i23, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i20, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i22, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i20, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i21, new Vector2(0, 0), kioskColor)); 
            
            //Face 3
            kioskBuilder.Add(new Vertex(rlN_i26, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i27, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i23, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i26, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i23, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i22, new Vector2(0, 0), kioskColor)); 
            
            //Face 4
            kioskBuilder.Add(new Vertex(rlN_i25, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i24, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i27, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i25, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i27, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i26, new Vector2(0, 0), kioskColor)); 
            
            //Bottom
            kioskBuilder.Add(new Vertex(rlN_i26, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i22, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i21, new Vector2(0, 0), kioskColor));
            
            kioskBuilder.Add(new Vertex(rlN_i26, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i21, new Vector2(0, 0), kioskColor));
            kioskBuilder.Add(new Vertex(rlN_i25, new Vector2(0, 0), kioskColor)); 
        }

        KioskBuffer = new((uint)kioskBuilder.Count, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        KioskBuffer.Bind();
        KioskBuffer.SetData(kioskBuilder.ToArray());
        KioskBuffer.Unbind();

        KioskInstanceData = new Buffer<InstanceData>(1, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        KioskInstanceData.Bind();
        KioskInstanceData.SetData(new InstanceData[]
        {
            new()
            {
                Matrix = Matrix4x4.CreateScale(new Vector3(0.001f, -0.001f, 0.001f)) * Matrix4x4.CreateTranslation(new Vector3(0, floorHeight, 0))
            }
        });
        KioskInstanceData.Unbind();

        KioskVao = new VertexArrayObject();
        SetupVAO(KioskBuffer, KioskInstanceData, KioskVao);
    }

    public static Buffer<InstanceData> AAAAA(int numObjects, bool spiralLayout)
    {
        float further = 1.0f;
        float theta = 0.1f;
        float thetaIncrease = MathF.PI / 6;
        const float thetaLimit = MathF.Tau - 0.1f;
        float radius = 2000;

        List<InstanceData> data = new();

        for (int i = 0; i < numObjects; i++)
        {
            int blockResult = 0;

            if (blockResult == 0)
            {
                Vector3 translation = new Vector3(
                    further * radius * MathF.Cos(theta),
                    (radius - 2000.0f) / 8.0f,
                    further * radius * MathF.Sin(theta)
                ) * 0.001f;
                translation.Y += floorHeight;
                float rotation = 900 - theta * 360.0f / MathF.Tau;
                Matrix4x4 matrix = Matrix4x4.CreateScale(new Vector3(0.3f)) * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(rotation)) * Matrix4x4.CreateTranslation(translation);
                data.Add(new InstanceData
                {
                    Matrix = matrix
                });
            }

            if (spiralLayout)
                further += 0.05f;

            theta += thetaIncrease;
            if (theta > thetaLimit)
            {
                theta = 0.1f;
                if (!spiralLayout) radius += 1000;
                thetaIncrease = thetaIncrease * (radius - 1000) / radius;
            }
        }

        Buffer<InstanceData> buf = new((uint)data.Count, BufferTargetARB.ArrayBuffer, BufferUsageARB.StaticDraw);
        buf.Bind();
        buf.SetData(data.ToArray());
        buf.Unbind();

        return buf;
    }

    public static void SetupVAO(Buffer<Vertex> modelBuf, Buffer<InstanceData> instanceBuf, VertexArrayObject vao)
    {
        vao.Bind();
        modelBuf.Bind();
        gl.EnableVertexAttribArray(0);
        gl.EnableVertexAttribArray(1);
        gl.EnableVertexAttribArray(2);
        gl.EnableVertexAttribArray(3);
        gl.EnableVertexAttribArray(4);
        gl.EnableVertexAttribArray(5);
        gl.EnableVertexAttribArray(6);

        gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), null);
        gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.TexCoord)));
        gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, (uint)sizeof(Vertex), (void*)Marshal.OffsetOf<Vertex>(nameof(Vertex.Color)));

        instanceBuf.Bind();
        gl.VertexAttribPointer(3, 4, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)0);
        gl.VertexAttribPointer(4, 4, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)(sizeof(Vector4) * 1));
        gl.VertexAttribPointer(5, 4, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)(sizeof(Vector4) * 2));
        gl.VertexAttribPointer(6, 4, VertexAttribPointerType.Float, false, (uint)sizeof(InstanceData), (void*)(sizeof(Vector4) * 3));

        gl.VertexAttribDivisor(3, 1);
        gl.VertexAttribDivisor(4, 1);
        gl.VertexAttribDivisor(5, 1);
        gl.VertexAttribDivisor(6, 1);

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

        Matrix4x4 view = Matrix4x4.CreateLookAt(CameraPosition, CameraPosition + CameraFront, CameraUp);
        Matrix4x4 projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(CameraZoom), (float)Window.FramebufferSize.X / Window.FramebufferSize.Y, 0.1f, 1000.0f);

        //Bind our used objects
        _Shader.Bind();
        _Vao.Bind();

        _Shader.SetUniform("ProjectionMatrix", projection);
        _Shader.SetUniform("ViewMatrix", view);

        _WorldVertexBuffer.Bind();
        gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, _WorldVertexBuffer.Count, _WorldInstanceBuffer.Count);

        _ItemVao.Bind();
        gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, _ItemBuffer.Count, _InstanceData.Count);

        KioskVao.Bind();
        gl.DrawArraysInstanced(PrimitiveType.Triangles, 0, KioskBuffer.Count, KioskInstanceData.Count);

        ImGuiController.Render();
    }

    public static void Update(double dt)
    {
        ImGuiController.Update((float)dt);

        // float moveSpeed = 10f * (float)dt;
        float moveSpeed = 2.5f * (float)dt;

        // Console.WriteLine(1.0 / dt);

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
        // if (_Kb.IsKeyPressed(Key.Space))
        //     CameraPosition += new Vector3(0, moveSpeed, 0);
        // if (_Kb.IsKeyPressed(Key.ShiftLeft))
        //     CameraPosition -= new Vector3(0, moveSpeed, 0);
    }
}