open System
open System.Drawing
open System.IO
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open StbImageSharp

// Textures!
// In this tutorial, you'll learn how to load and render textures.

type VertexData = {
    vao : uint32
    vbo : uint32
    ebo : uint32
}

type Model = {
    Window : IWindow
    Gl : GL
    VertexData : VertexData
    Shader : uint32
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

let onRender (model:Model) =
    // Clear the window to the color we set earlier.
    model.Gl.Clear ClearBufferMask.ColorBufferBit

    // Bind our VAO, then the program.
    model.Gl.BindVertexArray model.VertexData.vao
    model.Gl.UseProgram model.Shader

    // Much like our texture creation earlier, we must first set our active texture unit, and then bind the
    // texture to use it during draw!
    model.Gl.ActiveTexture TextureUnit.Texture0
    model.Gl.BindTexture (TextureTarget.Texture2D, model.Texture)

    // Draw our quad! We use a count of 6 here because we have 6 total vertices that makes up a quad.
    model.Gl.DrawElements (PrimitiveType.Triangles, 6u, DrawElementsType.UnsignedInt, IntPtr.Zero.ToPointer ())

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
    // Create our vertex shader, and give it our vertex shader source code.
    let vertexShader = gl.CreateShader ShaderType.VertexShader
    gl.ShaderSource (vertexShader, vertexCode)

    // Attempt to compile the shader.
    gl.CompileShader vertexShader

    // Check to make sure that the shader has successfully compiled.
    let vStatus = gl.GetShader (vertexShader, ShaderParameterName.CompileStatus)
    if enum<GLEnum> vStatus = GLEnum.False then
        raise (Exception ("Vertex shader failed to compile: " + gl.GetShaderInfoLog vertexShader))

    // Repeat this process for the fragment shader.
    let fragmentShader = gl.CreateShader ShaderType.FragmentShader
    gl.ShaderSource (fragmentShader, fragmentCode)

    gl.CompileShader fragmentShader
    
    let vStatus = gl.GetShader (fragmentShader, ShaderParameterName.CompileStatus)
    if enum<GLEnum> vStatus = GLEnum.False then
        raise (Exception ("Fragment shader failed to compile " + gl.GetShaderInfoLog fragmentShader))

    // Create our shader program, and attach the vertex & fragment shaders.
    let program = gl.CreateProgram ()

    gl.AttachShader (program, vertexShader)
    gl.AttachShader (program, fragmentShader)

    // Attempt to "link" the program together.
    gl.LinkProgram program

    // Similar to shader compilation, check to make sure that the shader program has linked properly.
    let vStatus = gl.GetProgram (program, ProgramPropertyARB.LinkStatus)
    if enum<GLEnum> vStatus = GLEnum.False then
        raise (Exception ("Program failed to link " + gl.GetShaderInfoLog fragmentShader))

    // Detach and delete our shaders. Once a program is linked, we no longer need the individual shader objects.
    gl.DetachShader (program, vertexShader)
    gl.DetachShader (program, fragmentShader)
    gl.DeleteShader vertexShader
    gl.DeleteShader fragmentShader

    program

let onLoad (window:IWindow) : Model =
    let gl = window.CreateOpenGL ()

    gl.ClearColor Color.CornflowerBlue

    // The quad vertices data.
    // You may have noticed an addition - texture coordinates!
    // Texture coordinates are a value between 0-1 (see more later about this) which tell the GPU which part
    // of the texture to use for each vertex.
    let vertices = [|
     // aPosition--------   aTexCoords
         0.5f;  0.5f; 0.0f; 1.0f; 1.0f;
         0.5f; -0.5f; 0.0f; 1.0f; 0.0f;
        -0.5f; -0.5f; 0.0f; 0.0f; 0.0f;
        -0.5f;  0.5f; 0.0f; 0.0f; 1.0f
    |]

    // The quad indices data.
    let indices = [|
        0u; 1u; 3u
        1u; 2u; 3u
    |]
    
    let vd = createVertexData gl vertices indices

    let program = createShaderProgram gl vertexCode fragmentCode    

    // Set up our vertex attributes! These tell the vertex array (VAO) how to process the vertex data we defined
    // earlier. Each vertex array contains attributes. 

    // Our stride constant. The stride must be in bytes, so we take the first attribute (a vec3), multiply it
    // by the size in bytes of a float, and then take our second attribute (a vec2), and do the same.
    let stride = uint ((3 * sizeof<float32>) + (2 * sizeof<float32>))
    
    // Enable the "aPosition" attribute in our vertex array, providing its size and stride too.
    let positionLoc = 0u
    gl.EnableVertexAttribArray positionLoc
    gl.VertexAttribPointer (positionLoc, 3, VertexAttribPointerType.Float, false, stride, IntPtr.Zero.ToPointer ())

    // Now we need to enable our texture coordinates! We've defined that as location 1 so that's what we'll use
    // here. The code is very similar to above, but you must make sure you set its offset to the **size in bytes**
    // of the attribute before.
    let pointer = IntPtr(3 * sizeof<float32>).ToPointer ()
    let textureLoc = 1u
    gl.EnableVertexAttribArray textureLoc
    gl.VertexAttribPointer (textureLoc, 2, VertexAttribPointerType.Float, false, stride, pointer)

    // Unbind everything as we don't need it.
    gl.BindVertexArray 0u
    gl.BindBuffer (BufferTargetARB.ArrayBuffer, 0u)
    gl.BindBuffer (BufferTargetARB.ElementArrayBuffer, 0u)

    // Now we create our texture!
    // First, we create the texture itself. Then, we must set an active texture unit. Each texture unit is a
    // separate bindable texture that we can use in a shader. GPUs have a maximum number of texture units they
    // can use, however the OpenGL spec states there MUST be at least 32 units available.
    // Much like buffers, we then bind the texture to a Texture2D target.
    let texture = gl.GenTexture ()
    gl.ActiveTexture TextureUnit.Texture0
    gl.BindTexture (TextureTarget.Texture2D, texture)

    // Use StbImageSharp to load an image from our PNG file.
    // This will load and decompress the result into a raw byte array that we can pass directly into OpenGL.
    let image = File.ReadAllBytes("silk.png")
    let result = ImageResult.FromMemory (image, ColorComponents.RedGreenBlueAlpha)
    
    // Upload our texture data to the GPU.
    // Let's go over each parameter used here:
    // 1. Tell OpenGL that we want to upload to the texture bound in the Texture2D target.
    // 2. We are uploading the "base" texture level, therefore this value should be 0. You don't need to
    //    worry about texture levels for now.
    // 3. We tell OpenGL that we want the GPU to store this data as RGBA formatted data on the GPU itself.
    // 4. The image's width.
    // 5. The image's height.
    // 6. This is the image's border. This valu MUST be 0. It is a leftover component from legacy OpenGL, and
    //    it serves no purpose.
    // 7. Our image data is formatted as RGBA data, therefore we must tell OpenGL we are uploading RGBA data.
    // 8. StbImageSharp returns this data as a byte[] array, therefore we must tell OpenGL we are uploading
    //    data in the unsigned byte format.
    // 9. The actual pointer to our data!
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

    // Let's set some texture parameters!
    // This tells the GPU how it should sample the texture.
    
    // Set the texture wrap mode to repeat.
    // The texture wrap mode defines what should happen when the texture coordinates go outside of the 0-1 range.
    // In this case, we set it to repeat. The texture will just repeatedly tile over and over again.
    // You'll notice we're using S and T wrapping here. This is OpenGL's version of the standard UV mapping you
    // may be more used to, where S is on the X-axis, and T is on the Y-axis.
    gl.TextureParameter (texture, TextureParameterName.TextureWrapS, (int) TextureWrapMode.Repeat)
    gl.TextureParameter (texture, TextureParameterName.TextureWrapT, (int) TextureWrapMode.Repeat)
    
    // The min and mag filters define how the texture should be sampled as it resized.
    // The min, or minification filter, is used when the texture is reduced in size.
    // The mag, or magnification filter, is used when the texture is increased in size.
    // We're using bilinear filtering here, as it produces a generally nice result.
    // You can also use nearest (point) filtering, or anisotropic filtering, which is only available on the min
    // filter.
    // You may notice that the min filter defines a "mipmap" filter as well. We'll go over mipmaps below.
    gl.TextureParameter (texture, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.LinearMipmapLinear)
    gl.TextureParameter (texture, TextureParameterName.TextureMagFilter, (int) TextureMinFilter.Linear)
    
    // Generate mipmaps for this texture.
    // Note: We MUST do this or the texture will appear as black (this is an option you can change but this is
    // out of scope for this tutorial).
    // What is a mipmap?
    // A mipmap is essentially a smaller version of the existing texture. When generating mipmaps, the texture
    // size is continuously halved, generally stopping once it reaches a size of 1x1 pixels. (Note: there are
    // exceptions to this, for example if the GPU reaches its maximum level of mipmaps, which is both a hardware
    // limitation, and a user defined value. You don't need to worry about this for now, so just assume that
    // the mips will be generated all the way down to 1x1 pixels).
    // Mipmaps are used when the texture is reduced in size, to produce a much nicer result, and to reduce moire
    // effect patterns.
    gl.GenerateMipmap TextureTarget.Texture2D
    
    // Unbind the texture as we no longer need to update it any further.
    gl.BindTexture (TextureTarget.Texture2D, 0u)

    // Get our texture uniform, and set it to 0.
    // We can easily do this by using glGetUniformLocation and giving it a name.
    // Setting it to 0 tells it that you want it to use the 0th texture unit.
    // Generally, OpenGL should automatically initialize all uniform values to their default value (which is
    // almost always 0), however you should get into the practice of initializing all uniform values to a known
    // value, before you use them in your shader.
    let location = gl.GetUniformLocation (program, "uTexture")
    gl.Uniform1 (location, 0)
    
    // Finally a bit of blending!
    // If you disable blending, you'll notice a black border around the texture.
    // The texture is partially transparent, however OpenGL doesn't know how to handle this by default.
    // By enabling blending, and giving it a blend function, you can tell OpenGL how to handle transparency.
    // In this case, it removes the black background and just leaves the texture on its own.
    // The blend function is out of scope for this tutorial, so don't worry if you don't understand it too much.
    // The program will function just fine without blending!
    gl.Enable EnableCap.Blend
    gl.BlendFunc (BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)

    {   Window = window
        Gl = gl
        VertexData = vd
        Shader = program
        Texture = texture }

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
    window.Dispose ()
    0