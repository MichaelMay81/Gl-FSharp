module Tutorial1_2_Hello_Quad

open System
open System.Drawing
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL

type VertexData = {
    vao : uint32
    vbo : uint32
    ebo : uint32
}

type Model = {
    Window : IWindow
    Gl : GL
    VertexData : VertexData
    Program : uint32 }

let vertexCode = "
#version 330 core
layout (location = 0) in vec3 aPosition;
void main()
{
    gl_Position = vec4(aPosition, 1.0);
}"
let fragmentCode = "
#version 330 core
out vec4 out_color;
void main()
{
    out_color = vec4(1.0, 0.5, 0.2, 1.0);
}"

let createVertexData (gl:GL) (vertices:float32[]) (indices:uint32[]) : VertexData =
    // Create the VAO.
    let vao = gl.GenVertexArray ()
    gl.BindVertexArray vao

    // Create the VBO.
    let vbo = gl.GenBuffer ()
    gl.BindBuffer (BufferTargetARB.ArrayBuffer, vbo)

    // Upload the vertices data to the VBO.
    gl.BufferData (
        BufferTargetARB.ArrayBuffer,
        ReadOnlySpan<float32> vertices,
        BufferUsageARB.StaticDraw)

    // Create the EBO.
    let ebo = gl.GenBuffer ()
    gl.BindBuffer (BufferTargetARB.ElementArrayBuffer, ebo)
    
    // Upload the indices data to the EBO.
    gl.BufferData (
        BufferTargetARB.ElementArrayBuffer,
        ReadOnlySpan<uint32> indices,
        BufferUsageARB.StaticDraw)

    {   vao = vao
        vbo = vbo
        ebo = ebo }

let createShaderProgram (gl:GL) (vertexCode:string) (fragmentCode:string) : uint32 =
    // Create vertex shader.
    let vertexShader = gl.CreateShader ShaderType.VertexShader
    gl.ShaderSource (vertexShader, vertexCode)

    gl.CompileShader vertexShader
    let vStatus = gl.GetShader (vertexShader, ShaderParameterName.CompileStatus)
    if enum<GLEnum> vStatus = GLEnum.False then
        raise (Exception ("Vertex shader failed to compile: " + gl.GetShaderInfoLog vertexShader))

    // Create fragment shader.
    let fragmentShader = gl.CreateShader ShaderType.FragmentShader
    gl.ShaderSource (fragmentShader, fragmentCode)
    gl.CompileShader fragmentShader
    let vStatus = gl.GetShader (fragmentShader, ShaderParameterName.CompileStatus)
    if enum<GLEnum> vStatus = GLEnum.False then
        raise (Exception ("Fragment shader failed to compile " + gl.GetShaderInfoLog fragmentShader))

    // Create program.
    let program = gl.CreateProgram ()
    gl.AttachShader (program, vertexShader)
    gl.AttachShader (program, fragmentShader)
    gl.LinkProgram program
    let vStatus = gl.GetProgram (program, ProgramPropertyARB.LinkStatus)
    if enum<GLEnum> vStatus = GLEnum.False then
        raise (Exception ("Program failed to link " + gl.GetShaderInfoLog fragmentShader))

    // Cleaning up.
    gl.DetachShader (program, vertexShader)
    gl.DetachShader (program, fragmentShader)
    gl.DeleteShader vertexShader
    gl.DeleteShader fragmentShader

    program

let onLoad (window:IWindow) : Model =
    let gl = window.CreateOpenGL ()

    gl.ClearColor Color.CornflowerBlue

    // The quad vertices data.
    let vertices = [|
        0.5f;  0.5f; 0.0f
        0.5f; -0.5f; 0.0f
        -0.5f; -0.5f; 0.0f
        -0.5f;  0.5f; 0.0f
    |]

    // The quad indices data.
    let indices = [|
        0u; 1u; 3u
        1u; 2u; 3u
    |]
    
    let vd = createVertexData gl vertices indices

    let program = createShaderProgram gl vertexCode fragmentCode    

    // Setting up the attributes.
    let positionLoc = 0u
    gl.EnableVertexAttribArray positionLoc
    gl.VertexAttribPointer (positionLoc, 3, VertexAttribPointerType.Float, false, (uint) (3 * sizeof<float32>), IntPtr.Zero.ToPointer ())

    // Cleaning up.
    gl.BindVertexArray 0u
    gl.BindBuffer (BufferTargetARB.ArrayBuffer, 0u)
    gl.BindBuffer (BufferTargetARB.ElementArrayBuffer, 0u)

    {   Window = window
        Gl = gl
        VertexData = vd
        Program = program }

let onRender (model:Model) =
    model.Gl.Clear ClearBufferMask.ColorBufferBit

    model.Gl.BindVertexArray model.VertexData.vao
    model.Gl.UseProgram model.Program
    model.Gl.DrawElements (PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer ())

[<EntryPoint>]
let main _ =
    use window = Window.Create WindowOptions.Default
    window.Size <- Vector2D<int> (800, 600)
    window.Title <- "1.2 - Drawing a Quad"
    
    window.add_Load (fun _ ->
        let model = onLoad window
        
        window.add_Render (fun _ ->
            onRender model)
        )
        
    // window.add_Update (fun deltaTime -> printfn $"window update {deltaTime}")

    window.Run ()
    0