module Tutorial1_3_Textures

open System
open System.Drawing
open System.IO
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open StbImageSharp

type VertexData = {
    vao : uint32
    vbo : uint32
    ebo : uint32
}

type Model = {
    Window : IWindow
    Gl : GL
    VertexData : VertexData
    Program : uint32
    Texture : uint32 }


let vertexCode = "
    #version 330 core
            
    layout (location = 0) in vec3 aPosition;

    // On top of our aPosition attribute, we now create an aTexCoords attribute for our texture coordinates.
    layout (location = 1) in vec2 aTexCoords;

    // Likewise, we also assign an out attribute to go into the fragment shader.
    out vec2 frag_texCoords;

    void main()
    {
        gl_Position = vec4(aPosition, 1.0);

        // This basic vertex shader does no additional processing of texture coordinates, so we can pass them
        // straight to the fragment shader.
        frag_texCoords = aTexCoords;
    }"
let fragmentCode = "
    #version 330 core

    // This in attribute corresponds to the out attribute we defined in the vertex shader.
    in vec2 frag_texCoords;

    out vec4 out_color;

    // Now we define a uniform value!
    // A uniform in OpenGL is a value that can be changed outside of the shader by modifying its value.
    // A sampler2D contains both a texture and information on how to sample it.
    // Sampling a texture is basically calculating the color of a pixel on a texture at any given point.
    uniform sampler2D uTexture;

    void main()
    {
        // We use GLSL's texture function to sample from the texture at the given input texture coordinates.
        out_color = texture(uTexture, frag_texCoords);
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
        0.5f;  0.5f; 0.0f; 1.0f; 1.0f
        0.5f; -0.5f; 0.0f; 1.0f; 0.0f
        -0.5f; -0.5f; 0.0f; 0.0f; 0.0f
        -0.5f;  0.5f; 0.0f; 0.0f; 1.0f
    |]

    // The quad indices data.
    let indices = [|
        0u; 1u; 3u
        1u; 2u; 3u
    |]
    
    let vd = createVertexData gl vertices indices

    let program = createShaderProgram gl vertexCode fragmentCode    

    // Setting up the attributes.

    let stride = uint ((3 * sizeof<float32>) + (2 * sizeof<float32>))
    
    let positionLoc = 0u
    gl.EnableVertexAttribArray positionLoc
    gl.VertexAttribPointer (positionLoc, 3, VertexAttribPointerType.Float, false, stride, IntPtr.Zero.ToPointer ())

    let pointer = IntPtr(3 * sizeof<float32>).ToPointer ()
    let textureLoc = 1u
    gl.EnableVertexAttribArray textureLoc
    gl.VertexAttribPointer (textureLoc, 2, VertexAttribPointerType.Float, false, stride, pointer)

    // Cleaning up.
    gl.BindVertexArray 0u
    gl.BindBuffer (BufferTargetARB.ArrayBuffer, 0u)
    gl.BindBuffer (BufferTargetARB.ElementArrayBuffer, 0u)

    let texture = gl.GenTexture ()
    gl.ActiveTexture TextureUnit.Texture0
    gl.BindTexture (TextureTarget.Texture2D, texture)

    let image = File.ReadAllBytes("../Assets/silk.png")
    let result = ImageResult.FromMemory (image, ColorComponents.RedGreenBlueAlpha)
    gl.TexImage2D (
        TextureTarget.Texture2D,
        0,
        InternalFormat.Rgba,
        uint result.Width,
        uint result.Height,
        0,
        PixelFormat.Rgba,
        PixelType.UnsignedByte,
        ReadOnlySpan<byte> result.Data)

    gl.TextureParameter (texture, TextureParameterName.TextureWrapS, (int) TextureWrapMode.Repeat)
    gl.TextureParameter (texture, TextureParameterName.TextureWrapT, (int) TextureWrapMode.Repeat)
    gl.TextureParameter (texture, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.LinearMipmapLinear)
    gl.TextureParameter (texture, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear)
    
    gl.GenerateMipmap TextureTarget.Texture2D
    gl.BindTexture (TextureTarget.Texture2D, 0u)

    let location = gl.GetUniformLocation (program, "uTexture")
    gl.Uniform1 (location, 0)
    
    gl.Enable EnableCap.Blend
    gl.BlendFunc (BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)

    {   Window = window
        Gl = gl
        VertexData = vd
        Program = program
        Texture = texture }

let onRender (model:Model) =
    model.Gl.Clear ClearBufferMask.ColorBufferBit

    model.Gl.BindVertexArray model.VertexData.vao
    model.Gl.UseProgram model.Program

    model.Gl.ActiveTexture TextureUnit.Texture0
    model.Gl.BindTexture (TextureTarget.Texture2D, model.Texture)

    model.Gl.DrawElements (PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer ())

[<EntryPoint>]
let main _ =
    let mutable options = WindowOptions.Default
    options.Size <- Vector2D<int> (800, 600)
    options.Title <- "1.3 - Textures"

    use window = Window.Create options

    window.add_Load (fun _ ->
        let model = onLoad window
        
        window.add_Render (fun _ -> onRender model )
        window.add_FramebufferResize (fun size ->
            model.Gl.Viewport (0, 0, uint size.X, uint size.Y)))
    
    window.Run ()
    0