open System
open System.Numerics
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Silk.NET.Input

open Tutorial1_4_Abstractions
open Tutorial1_5_Transformations

type Model = {
    Window : IWindow
    Gl : GL
    Keyboard: IKeyboard

    vbo : BufferObject<float32>
    ebo : BufferObject<uint>
    vao : VertexArrayObject

    Texture : Texture
    Shader : Shader
    
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

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.vbo |> BufferObjects.dispose
    model.ebo |> BufferObjects.dispose
    model.vao |> VertexArrayObjects.dispose
    model.Shader |> Shaders.dispose
    model.Texture |> Textures.dispose

let onRender (model:Model) (deltaTime:float) =
    model.Gl.Clear ClearBufferMask.ColorBufferBit

    //Binding and using our VAO and shader.
    model.vao |> VertexArrayObjects.bind
    model.Shader |> Shaders.useProgram

    model.Texture |> Textures.bindSlot0

    //Setting a uniform.
    model.Shader
    |> Shaders.setUniformInt "uTexture0" 0
    |> printError
    
    model.Transforms
    |> Array.iter (fun transform ->
        model.Shader
        |> Shaders.setUniformMat4 "uModel" (transform |> Transforms.viewMatrix) 
        |> printError

        model.Gl.DrawElements (PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer ()) )


let onLoad (window:IWindow) : Model option =
    let inputContext = window.CreateInput ()
    let keyboard = inputContext.Keyboards |> Seq.head
    // inputContext.Keyboards
    // |> Seq.iter (fun keyboard ->
    //     keyboard.add_KeyDown (keyDown window))

    let gl = GL.GetApi window

    let ebo = BufferObjects.createUInt gl BufferTargetARB.ElementArrayBuffer indices
    let vbo = BufferObjects.createFloat gl BufferTargetARB.ArrayBuffer vertices   
    let vao = VertexArrayObjects.create gl vbo (Some ebo)
    
    vao |> VertexArrayObjects.vertexAttributePointer 0u 3u 5u 0u
    vao |> VertexArrayObjects.vertexAttributePointer 1u 2u 5u 3u

    let shaderOpt = 
        Shaders.create gl "src/shader.vert" "src/shader.frag"
        |> resultToOption

    let textureOpt =
        Textures.createFromFile gl "../Assets/silk.png"
        |> resultToOption

    match shaderOpt, textureOpt with
    | Some shader, Some texture ->
        Some {  Window = window
                Gl = gl
                Keyboard = keyboard
                vbo = vbo
                ebo = ebo
                vao = vao
                Shader = shader
                Texture = texture
                Transforms = [|
                    { Transforms.init with Position = Vector3 (0.5f,0.5f,0f)}
                    { Transforms.init with Rotation = Quaternion.CreateFromAxisAngle (Vector3.UnitZ, 1f)}
                    { Transforms.init with Scale = 0.5f }
                    { Transforms.init with
                        Position = Vector3 (-0.5f, 0.5f, 0f)
                        Rotation = Quaternion.CreateFromAxisAngle (Vector3.UnitZ, 1f)
                        Scale = 0.5f } |]}
    | _ ->
        vbo |> BufferObjects.dispose
        ebo |> BufferObjects.dispose
        vao |> VertexArrayObjects.dispose
        shaderOpt |> Option.iter Shaders.dispose
        textureOpt |> Option.iter Textures.dispose

        None
let onFramebufferResize (gl:GL) (size:Vector2D<int>) =
    gl.Viewport (0, 0, uint size.X, uint size.Y)

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "1.4 - Abstractions"
    use window = Window.Create options

    window.add_Load (fun _ ->
        match onLoad window with
        | Some model ->
            window.add_Render (onRender model)
            window.add_Closing (fun _ -> onClose model)
            window.add_FramebufferResize (onFramebufferResize model.Gl)
        | None ->
            window.Close () )
    
    window.Run ()
    window.Dispose ()
    0