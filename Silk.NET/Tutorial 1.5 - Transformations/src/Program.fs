open System
open System.Numerics
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Tutorial1_4_Abstractions
open Silk.NET.Input

type Model = {
    Window : IWindow
    Gl : GL

    vbo : BufferObject<float32>
    ebo : BufferObject<uint>
    vao : VertexArrayObject

    Texture : Texture option
    ShaderOpt : Shader option
    
    Transforms : Transform[]}

// The quad vertices data.
let private vertices = [|
    0.5f;  0.5f; 0.0f; 1.0f; 0.0f
    0.5f; -0.5f; 0.0f; 1.0f; 1.0f
    -0.5f; -0.5f; 0.0f; 0.0f; 1.0f
    -0.5f;  0.5f; 0.0f; 0.0f; 0.0f |]

// The quad indices data.
let private indices = [|
    0u; 1u; 3u
    1u; 2u; 3u |]

let private resultToOption = function
    | Error error ->
        printfn "Error: %s" error
        None
    | Ok value ->
        Some value

let private printError =
    Result.mapError (printfn "Error: %s")
    >> ignore

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.vbo |> BufferObjects.dispose
    model.ebo |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.ShaderOpt |> Option.iter Shaders.dispose
    model.Texture |> Option.iter Textures.dispose

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear ClearBufferMask.ColorBufferBit

    //Binding and using our VAO and shader.
    model.vao |> VertexArrayObjects.bind
    model.ShaderOpt |> Option.iter Shaders.useProgram

    model.Texture |> Option.iter Textures.bindSlot0

    //Setting a uniform.
    model.ShaderOpt |> Option.iter (fun shader ->
        shader
        |> Shaders.setUniform "uTexture0" (Shaders.Int 0)
        |> printError
        
        model.Transforms
        |> Array.iter (fun transform ->
            Shaders.setUniform "uModel" (Shaders.M4 (transform.viewMatrix ())) shader
            |> printError

            model.Gl.DrawElements (PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer ()) ))


let onLoad (window:IWindow) : Model =
    let inputContext = window.CreateInput ()
    inputContext.Keyboards
    |> Seq.iter (fun keyboard ->
        keyboard.add_KeyDown (keyDown window))

    let gl = GL.GetApi window

    let ebo = BufferObjects.createUInt gl BufferTargetARB.ElementArrayBuffer indices
    let vbo = BufferObjects.createFloat gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo ebo
    
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
        ebo = ebo
        vao = vao
        ShaderOpt = shaderOpt
        Texture = texture
        Transforms = [|
            { Transform.Init with Position = Vector3 (0.5f,0.5f,0f)}
            { Transform.Init with Rotation = Quaternion.CreateFromAxisAngle (Vector3.UnitZ, 1f)}
            { Transform.Init with Scale = 0.5f }
            { Transform.Init with
                Position = Vector3 (-0.5f, 0.5f, 0f)
                Rotation = Quaternion.CreateFromAxisAngle (Vector3.UnitZ, 1f)
                Scale = 0.5f }
        |]}

let onFramebufferResize (gl:GL) (size:Vector2D<int>) =
    gl.Viewport (0, 0, uint size.X, uint size.Y)

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "1.4 - Abstractions"
    use window = Window.Create options

    window.add_Load (fun _ ->
        let model = onLoad window
        
        window.add_Render (onRender model)
        window.add_Closing (fun _ -> onClose model)
        window.add_FramebufferResize (onFramebufferResize model.Gl))
    
    window.Run ()
    window.Dispose ()
    0