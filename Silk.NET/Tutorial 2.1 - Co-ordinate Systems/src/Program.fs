open System.Numerics
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Tutorial1_4_Abstractions
open Silk.NET.Input

type Model = {
    Window: IWindow
    Gl: GL

    vbo: BufferObject<float32>
    vao: VertexArrayObject

    Texture: Texture option
    ShaderOpt: Shader option
    Camera: Camera

    Width: int
    Height: int }

// The quad vertices data.
let private vertices = [|
    -0.5f; -0.5f; -0.5f;  0.0f; 0.0f
    0.5f; -0.5f; -0.5f;  1.0f; 0.0f
    0.5f;  0.5f; -0.5f;  1.0f; 1.0f
    0.5f;  0.5f; -0.5f;  1.0f; 1.0f
    -0.5f;  0.5f; -0.5f;  0.0f; 1.0f
    -0.5f; -0.5f; -0.5f;  0.0f; 0.0f

    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f
    0.5f; -0.5f;  0.5f;  1.0f; 0.0f
    0.5f;  0.5f;  0.5f;  1.0f; 1.0f
    0.5f;  0.5f;  0.5f;  1.0f; 1.0f
    -0.5f;  0.5f;  0.5f;  0.0f; 1.0f
    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f

    -0.5f;  0.5f;  0.5f;  1.0f; 0.0f
    -0.5f;  0.5f; -0.5f;  1.0f; 1.0f
    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f
    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f
    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f
    -0.5f;  0.5f;  0.5f;  1.0f; 0.0f

    0.5f;  0.5f;  0.5f;  1.0f; 0.0f
    0.5f;  0.5f; -0.5f;  1.0f; 1.0f
    0.5f; -0.5f; -0.5f;  0.0f; 1.0f
    0.5f; -0.5f; -0.5f;  0.0f; 1.0f
    0.5f; -0.5f;  0.5f;  0.0f; 0.0f
    0.5f;  0.5f;  0.5f;  1.0f; 0.0f

    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f
    0.5f; -0.5f; -0.5f;  1.0f; 1.0f
    0.5f; -0.5f;  0.5f;  1.0f; 0.0f
    0.5f; -0.5f;  0.5f;  1.0f; 0.0f
    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f
    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f

    -0.5f;  0.5f; -0.5f;  0.0f; 1.0f
    0.5f;  0.5f; -0.5f;  1.0f; 1.0f
    0.5f;  0.5f;  0.5f;  1.0f; 0.0f
    0.5f;  0.5f;  0.5f;  1.0f; 0.0f
    -0.5f;  0.5f;  0.5f;  0.0f; 0.0f
    -0.5f;  0.5f; -0.5f;  0.0f; 1.0f |]

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.vbo |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.ShaderOpt |> Option.iter Shaders.dispose
    model.Texture |> Option.iter Textures.dispose

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear (ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit )

    let difference =
        model.Window.Time * 100.
        |> float32
        |> degreesToRadians
    let modelMat =
        Matrix4x4.CreateRotationY difference *
        Matrix4x4.CreateRotationX difference
    let viewMat =
        model.Camera
        |> Cameras.viewMatrix
    let projMat =
        Matrix4x4.CreatePerspectiveFieldOfView (
            45f |> degreesToRadians,
            (float32 model.Width) / (float32 model.Height),
            0.1f,
            100f
        )

    //Binding and using our VAO and shader.
    model.vao |> VertexArrayObjects.bind
    model.Texture |> Option.iter Textures.bindSlot0
    model.ShaderOpt |> Option.iter Shaders.useProgram

    //Setting a uniform.
    model.ShaderOpt |> Option.iter (fun shader ->
        shader
        |> Shaders.setUniform "uTexture0" (Shaders.Int 0)
        |> printError
        shader
        |> Shaders.setUniform "uModel" (Shaders.M4 modelMat)
        |> printError
        shader
        |> Shaders.setUniform "uView" (Shaders.M4 viewMat)
        |> printError
        shader
        |> Shaders.setUniform "uProjection" (Shaders.M4 projMat)
        |> printError)
    
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        vertices |> Array.length |> uint)


let onLoad (window:IWindow) : Model =
    let inputContext = window.CreateInput ()
    inputContext.Keyboards
    |> Seq.iter (fun keyboard ->
        keyboard.add_KeyDown (keyDown window))

    let gl = GL.GetApi window
    gl.Enable EnableCap.DepthTest

    let vbo = BufferObjects.createFloat gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo None
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 5u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 2u 5u 3u

    let shaderOpt = 
        Shaders.create gl "src/shader.vert" "src/shader.frag"
        |> resultToOption

    let texture =
        Textures.createFromFile gl "../Assets/silk.png"
        |> resultToOption

    {   Window = window
        Gl = gl
        vbo = vbo
        vao = vao
        ShaderOpt = shaderOpt
        Texture = texture
        Camera = Camera.init
        Width = window.Size.X
        Height = window.Size.Y }

let onFramebufferResize (gl:GL) (size:Vector2D<int>) =
    gl.Viewport (0, 0, uint size.X, uint size.Y)

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "1.4 - Abstractions"
    options.PreferredDepthBufferBits <- 24
    use window = Window.Create options

    window.add_Load (fun _ ->
        let model = onLoad window
        
        window.add_Render (onRender model)
        window.add_Closing (fun _ -> onClose model)
        window.add_FramebufferResize (onFramebufferResize model.Gl))
    
    window.Run ()
    window.Dispose ()
    0