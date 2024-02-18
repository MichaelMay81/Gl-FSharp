open System.Numerics
open System.Linq
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Tutorial1_4_Abstractions
open Silk.NET.Input
open System

type Model = {
    Window: IWindow
    Gl: GL
    Keyboard: IKeyboard
    Mouse: IMouse

    Width: int
    Height: int

    vbo: BufferObject<float32>
    vao: VertexArrayObject
    Texture: Texture option
    ShaderOpt: Shader option

    Camera: Camera
    Zoom: float32
    LastMousePosition: Vector2 option }

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

let OnMouseWheel (model:Model) (scrollWheel:ScrollWheel) : Model =
    { model with Zoom = Math.Clamp (model.Zoom - scrollWheel.Y, 1f, 45f) }

let onMouseMove (model:Model) (position:Vector2) : Model =
    let lookSensitivity = 0.1f

    match model.LastMousePosition with
    | None -> { model with LastMousePosition = Some position }
    | Some lastMousePosition ->
        let xOffset = (position.X - lastMousePosition.X) * lookSensitivity
        let yOffset = (position.Y - lastMousePosition.Y) * lookSensitivity

        let yaw = model.Camera.Yaw + xOffset
        let pitch = model.Camera.Pitch - yOffset
        
        let yaw = Math.Clamp (yaw, -100f, -80f)
        let pitch = Math.Clamp (pitch, -10f, 10f)

        { model with
            LastMousePosition = Some position
            Camera = { model.Camera with Yaw=yaw; Pitch=pitch }}

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear
        (ClearBufferMask.ColorBufferBit |||
        ClearBufferMask.DepthBufferBit)

    //Binding and using our VAO and shader.
    model.vao |> VertexArrayObjects.bind
    model.Texture |> Option.iter Textures.bindSlot0
    model.ShaderOpt |> Option.iter Shaders.useProgram

    let difference =
        model.Window.Time * 100.
        |> float32
        |> degreesToRadians

    let modelMat =
        Matrix4x4.CreateRotationY difference *
        Matrix4x4.CreateRotationX difference
    let viewMat = model.Camera |> Cameras.viewMatrix
    let projMat =
        Matrix4x4.CreatePerspectiveFieldOfView (
            model.Zoom |> degreesToRadians,
            (float32 model.Width) / (float32 model.Height),
            0.1f,
            100f )

    let setUniform name value shader =
        shader |> Option.iter (Shaders.setUniform name value >> printError)
        shader

    //Setting a uniform.
    model.ShaderOpt
    |> setUniform "uTexture0" (Shaders.Int 0)
    |> setUniform "uModel" (Shaders.M4 modelMat)
    |> setUniform "uView" (Shaders.M4 viewMat)
    |> setUniform "uProjection" (Shaders.M4 projMat)
    |> ignore
    
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        vertices |> Array.length |> uint)

let onUpdate (model:Model) (deltaTime:float) : Model =
    let moveSpeed = 2.5f * float32 deltaTime

    let ifKeyIsPressed key changePos position =
        if model.Keyboard.IsKeyPressed key then
            position + changePos
        else position

    let moveForward = moveSpeed * (model.Camera |> Cameras.front)
    let MoveRight =
        Vector3.Cross (model.Camera |> Cameras.front, model.Camera.Up)
        |> Vector3.Normalize
        |> (*) moveSpeed

    let cameraPosition =
        model.Camera.Position
        |> ifKeyIsPressed Key.W moveForward
        |> ifKeyIsPressed Key.S -moveForward
        |> ifKeyIsPressed Key.A -MoveRight
        |> ifKeyIsPressed Key.D MoveRight

    { model with Camera = {model.Camera with Position = cameraPosition} }


let onLoad (window:IWindow) : Model =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards.FirstOrDefault ()
    let mouse = inputContext.Mice.FirstOrDefault ()
    //mouse.Cursor.CursorMode <- CursorMode.Raw
    
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
        Textures.createFromFile gl "../Assests/silk.png"
        |> resultToOption

    {   Window = window
        Gl = gl
        vbo = vbo
        vao = vao
        ShaderOpt = shaderOpt
        Texture = texture
        Camera = Camera.init
        Zoom = 45f
        Width = window.Size.X
        Height = window.Size.Y
        Keyboard = keyboard
        Mouse = mouse
        LastMousePosition = None }

let onFramebufferResize (gl:GL) (size:Vector2D<int>) =
    gl.Viewport (0, 0, uint size.X, uint size.Y)

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "2.2 - Camera"
    options.PreferredDepthBufferBits <- 24
    use window = Window.Create options

    window.add_Load (fun _ ->
        let mutable model = onLoad window
        
        model.Keyboard.add_KeyDown (keyDown window)
        model.Mouse.add_MouseMove (fun _ pos -> model <- onMouseMove model pos)
        model.Mouse.add_Scroll (fun _ scrollWheel -> model <- OnMouseWheel model scrollWheel)

        window.add_Update (fun deltaTime -> model <- onUpdate model deltaTime)
        window.add_Render (onRender model)
        window.add_Closing (fun _ -> onClose model)
        window.add_FramebufferResize (onFramebufferResize model.Gl))
    
    window.Run ()
    window.Dispose ()
    0