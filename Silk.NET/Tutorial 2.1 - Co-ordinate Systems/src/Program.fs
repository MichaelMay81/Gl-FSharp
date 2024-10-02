open System.Numerics
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Silk.NET.Input

open Tutorial1_4_Abstractions
open Tutorial2_1_Co_ordinate_Systems

type Model = {
    Window: IWindow
    Gl: GL
    Keyboard: IKeyboard

    Width: int
    Height: int

    vbo: VertexBufferObject
    vao: VertexArrayObject
    Texture: Texture
    Shader: Shader

    Camera: Camera }

let private vertices = [|
    //X    Y      Z     U   V
    -0.5f; -0.5f; -0.5f;  0.0f; 0.0f;
     0.5f; -0.5f; -0.5f;  1.0f; 0.0f;
     0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
     0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
    -0.5f;  0.5f; -0.5f;  0.0f; 1.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; 0.0f;

    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f;
     0.5f; -0.5f;  0.5f;  1.0f; 0.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 1.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 1.0f;
    -0.5f;  0.5f;  0.5f;  0.0f; 1.0f;
    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f;

    -0.5f;  0.5f;  0.5f;  1.0f; 0.0f;
    -0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f;
    -0.5f;  0.5f;  0.5f;  1.0f; 0.0f;

     0.5f;  0.5f;  0.5f;  1.0f; 0.0f;
     0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
     0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
     0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
     0.5f; -0.5f;  0.5f;  0.0f; 0.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 0.0f;

    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f;
     0.5f; -0.5f; -0.5f;  1.0f; 1.0f;
     0.5f; -0.5f;  0.5f;  1.0f; 0.0f;
     0.5f; -0.5f;  0.5f;  1.0f; 0.0f;
    -0.5f; -0.5f;  0.5f;  0.0f; 0.0f;
    -0.5f; -0.5f; -0.5f;  0.0f; 1.0f;

    -0.5f;  0.5f; -0.5f;  0.0f; 1.0f;
     0.5f;  0.5f; -0.5f;  1.0f; 1.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 0.0f;
     0.5f;  0.5f;  0.5f;  1.0f; 0.0f;
    -0.5f;  0.5f;  0.5f;  0.0f; 0.0f;
    -0.5f;  0.5f; -0.5f;  0.0f; 1.0f |]

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.vbo.BufferObject |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.Shader |> Shaders.dispose
    model.Texture |> Textures.dispose

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear (ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit )

    model.vao |> VertexArrayObjects.bind
    model.Texture |> Textures.bindSlot0
    model.Shader |> Shaders.useProgram
    
    let shaderWerror func = model.Shader |> func |> printError
    shaderWerror <| Shaders.setUniformInt "uTexture0" 0

    //Use elapsed time to convert to radians to allow our cube to rotate over time
    let difference =
        model.Window.Time * 100.
        |> float32
        |> degreesToRadians

    let modelMat =
        Matrix4x4.CreateRotationY difference *
        Matrix4x4.CreateRotationX difference
    let viewMat = Matrix4x4.CreateLookAt (
            model.Camera.Position,
            model.Camera.Target,
            model.Camera |> Cameras.up)
    let projMat =
        Matrix4x4.CreatePerspectiveFieldOfView (
        45f |> degreesToRadians,
        (float32 model.Width) / (float32 model.Height),
        0.1f,
        100f)

    shaderWerror <| Shaders.setUniformMat4 "uModel" modelMat
    shaderWerror <| Shaders.setUniformMat4 "uView" viewMat
    shaderWerror <| Shaders.setUniformMat4 "uProjection" projMat

    //We're drawing with just vertices and no indices, and it takes 36 vertices to have a six-sided textured cube
    model.Gl.DrawArrays (
        PrimitiveType.Triangles,
        0,
        36u)

let onLoad (window:IWindow) : Model option =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards |> Seq.head
    
    let gl = GL.GetApi window
    gl.Enable EnableCap.DepthTest

    let vbo = BufferObjects.createVBO gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo None
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 5u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 2u 5u 3u

    let shaderOpt = 
        Shaders.create gl "shader.vert" "shader.frag"
        |> resultToOption

    let textureOpt =
        Textures.createFromFile gl "silk.png"
        |> resultToOption

    match shaderOpt, textureOpt with
    | Some shader, Some texture ->
        {   Window = window
            Gl = gl
            Keyboard = keyboard
            vbo = vbo
            vao = vao
            Shader = shader
            Texture = texture
            Camera = Cameras.init
            Width = window.Size.X
            Height = window.Size.Y } |> Some
    | _ ->
        vbo.BufferObject |> BufferObjects.dispose
        vao |> VertexArrayObjects.dispose
        shaderOpt |> Option.iter Shaders.dispose
        textureOpt |> Option.iter Textures.dispose
        None

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "2.1 - Co-ordinate Systems"
    options.PreferredDepthBufferBits <- 24
    use window = Window.Create options

    window.add_Load (fun _ ->
        match onLoad window with
        | Some model ->
            window.add_Render (onRender model)
            window.add_Closing (fun _ -> onClose model)
            model.Keyboard.add_KeyDown (keyDown window)
        | None ->
            window.Close ())
    
    window.Run ()
    window.Dispose ()
    0