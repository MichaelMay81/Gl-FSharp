open System
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open Silk.NET.Input

type VertexData = {
    vao : uint32
    vbo : uint32
    ebo : uint32 }

type Model = {
    Window : IWindow
    Gl : GL

    VertexData : VertexData
    Shader : uint32 }

//Vertex shaders are run on each vertex.
let VertexShaderSource = "
#version 330 core
layout (location = 0) in vec3 aPosition;
void main()
{
    gl_Position = vec4(aPosition, 1.0);
}"

//Fragment shaders are run on each fragment/pixel of the geometry.
let FragmentShaderSource = "
#version 330 core
out vec4 out_color;
void main()
{
    out_color = vec4(1.0, 0.5, 0.2, 1.0);
}"

//Vertex data, uploaded to the VBO.
let vertices = [|
    //X    Y      Z
     0.5f;  0.5f; 0.0f;
     0.5f; -0.5f; 0.0f;
    -0.5f; -0.5f; 0.0f;
    -0.5f;  0.5f; 0.0f |]

//Index data, uploaded to the EBO.
let indices = [|
    0u; 1u; 3u
    1u; 2u; 3u |]

let keyDown (window:IWindow) (_:IKeyboard) (key:Key) (_:int) =
    match key with
    | Key.Escape ->
        window.Close ()
    | _ -> ()

let onClose (model:Model) =
    model.VertexData.vbo |> model.Gl.DeleteBuffer
    model.VertexData.ebo |> model.Gl.DeleteBuffer
    model.VertexData.vao |> model.Gl.DeleteVertexArray
    model.Shader |> model.Gl.DeleteProgram

let onRender (model:Model) =
    //Clear the color channel.
    model.Gl.Clear ClearBufferMask.ColorBufferBit

    //Bind the geometry and shader.
    model.Gl.BindVertexArray model.VertexData.vao
    model.Gl.UseProgram model.Shader

    //Draw the geometry.
    model.Gl.DrawElements (PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer ())

let createVertexData (gl:GL) (vertices:float32[]) (indices:uint32[]) : VertexData =
    //Creating a vertex array.
    let vao = gl.GenVertexArray ()
    gl.BindVertexArray vao

    //Initializing a vertex buffer that holds the vertex data.
    let vbo = gl.GenBuffer () //Creating the buffer.
    gl.BindBuffer (BufferTargetARB.ArrayBuffer, vbo) //Binding the buffer.
    gl.BufferData (
        BufferTargetARB.ArrayBuffer,
        ReadOnlySpan<float32> vertices,
        BufferUsageARB.StaticDraw)

    //Initializing a element buffer that holds the index data.
    let ebo = gl.GenBuffer () //Creating the buffer.
    gl.BindBuffer (BufferTargetARB.ElementArrayBuffer, ebo)  //Binding the buffer.
    gl.BufferData (
        BufferTargetARB.ElementArrayBuffer,
        ReadOnlySpan<uint32> indices,
        BufferUsageARB.StaticDraw)

    {   vao = vao
        vbo = vbo
        ebo = ebo }

let createShaderProgram (gl:GL) (vertexCode:string) (fragmentCode:string) : uint32 =
    //Creating a vertex shader.
    let vertexShader = gl.CreateShader ShaderType.VertexShader
    gl.ShaderSource (vertexShader, vertexCode)
    gl.CompileShader vertexShader

    //Checking the shader for compilation errors.
    let vStatus = gl.GetShader (vertexShader, ShaderParameterName.CompileStatus)
    if enum<GLEnum> vStatus = GLEnum.False then
        raise (Exception ("Vertex shader failed to compile: " + gl.GetShaderInfoLog vertexShader))

    //Creating a fragment shader.
    let fragmentShader = gl.CreateShader ShaderType.FragmentShader
    gl.ShaderSource (fragmentShader, fragmentCode)
    gl.CompileShader fragmentShader

    //Checking the shader for compilation errors.
    let vStatus = gl.GetShader (fragmentShader, ShaderParameterName.CompileStatus)
    if enum<GLEnum> vStatus = GLEnum.False then
        raise (Exception ("Fragment shader failed to compile " + gl.GetShaderInfoLog fragmentShader))

    //Combining the shaders under one shader program.
    let shader = gl.CreateProgram ()
    gl.AttachShader (shader, vertexShader)
    gl.AttachShader (shader, fragmentShader)
    gl.LinkProgram shader

    //Checking the linking for errors.
    let vStatus = gl.GetProgram (shader, ProgramPropertyARB.LinkStatus)
    if enum<GLEnum> vStatus = GLEnum.False then
        raise (Exception ("Program failed to link " + gl.GetShaderInfoLog fragmentShader))

    //Delete the no longer useful individual shaders;
    gl.DetachShader (shader, vertexShader)
    gl.DetachShader (shader, fragmentShader)
    gl.DeleteShader vertexShader
    gl.DeleteShader fragmentShader

    shader

let onLoad (window:IWindow) : Model =
    let inputContext = window.CreateInput ()
    inputContext.Keyboards
    |> Seq.iter (fun keyboard ->
        keyboard.add_KeyDown (keyDown window))

    //Getting the opengl api for drawing to the screen.
    let gl = GL.GetApi window

    let vd = createVertexData gl vertices indices
    let shader = createShaderProgram gl VertexShaderSource FragmentShaderSource

    //Tell opengl how to give the data to the shaders.
    gl.VertexAttribPointer (0u, 3, VertexAttribPointerType.Float, false, (uint) (3 * sizeof<float32>), IntPtr.Zero.ToPointer ())
    gl.EnableVertexAttribArray 0u

    // Cleaning up.
    gl.BindVertexArray 0u
    gl.BindBuffer (BufferTargetARB.ArrayBuffer, 0u)
    gl.BindBuffer (BufferTargetARB.ElementArrayBuffer, 0u)

    {   Window = window
        Gl = gl
        VertexData = vd
        Shader = shader }

[<EntryPoint>]
let main _ =
    use window = Window.Create WindowOptions.Default
    window.Size <- Vector2D<int> (800, 600)
    window.Title <- "1.2 - Drawing a Quad"
    
    window.add_Load (fun _ ->
        let model = onLoad window
        
        window.add_Render (fun _ -> onRender model)
        window.add_Closing (fun _ -> onClose model)
        // window.add_Update (fun deltaTime -> printfn $"window update {deltaTime}")
    )

    window.Run ()

    window.Dispose ()
    0