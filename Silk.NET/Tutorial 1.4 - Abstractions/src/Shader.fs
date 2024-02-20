namespace Tutorial1_4_Abstractions

open System
open System.IO
open System.Numerics
open Silk.NET.OpenGL

type Shader = {
    Handle: uint
    Gl: GL }

module Shaders =

    let private (|IsNullOrWhiteSpace|SomeString|) (str : string) =
        if str |> String.IsNullOrWhiteSpace
        then IsNullOrWhiteSpace
        else SomeString str
    
    let private loadShader (gl:GL) (shaderType:ShaderType) (path:string) : Result<uint, string> =
        //To load a single shader we need to:
        //1) Load the shader from a file.
        //2) Create the handle.
        //3) Upload the source to opengl.
        //4) Compile the shader.
        //5) Check for errors.
        try
            File.ReadAllText path |> Ok
        with
        | :? FileNotFoundException as ex ->
            Error ex.Message
        |> Result.bind (fun src ->
            let handle = gl.CreateShader shaderType
            gl.ShaderSource (handle, src)
            gl.CompileShader handle

            match gl.GetShaderInfoLog handle with
            | IsNullOrWhiteSpace ->
                Ok handle
            | SomeString infoLog ->
                Error $"Error compiling shader of type {shaderType}, failed with error {infoLog}")

    let dispose (shader:Shader) : unit =
        //Remember to delete the program when we are done.
        shader.Gl.DeleteProgram shader.Handle

    let private getUniformLocation (shader:Shader) (name:string) : Result<int, string> =
        //If GetUniformLocation returns -1 the uniform is not found.
        match shader.Gl.GetUniformLocation (shader.Handle, name) with
        | -1 ->
            Error $"{name} uniform not found on shader."
        | location ->
            Ok location

     //Uniforms are properties that applies to the entire geometry
    let setUniformFloat (name:string) (value:float32) (shader:Shader) : Result<unit, string> =
        //Setting a uniform on a shader using a name.
        getUniformLocation shader name
        |> Result.map (fun location -> shader.Gl.Uniform1 (location, value))

    let setUniformInt (name:string) (value:int) (shader:Shader) : Result<unit, string> =
        getUniformLocation shader name
        |> Result.map (fun location -> shader.Gl.Uniform1 (location, value))

    let setUniformMat4 (name:string) (value:Matrix4x4) (shader:Shader) : Result<unit, string> =
        getUniformLocation shader name
        |> Result.map (fun location -> shader.Gl.UniformMatrix4 (location, 1u, false, &value.M11))

    let setUniformVec3 (name:string) (value:Vector3) (shader:Shader) : Result<unit, string> =
        getUniformLocation shader name
        |> Result.map (fun location -> shader.Gl.Uniform3 (location, value.X, value.Y, value.Z))

    let useProgram (shader:Shader) =
        //Using the program
        shader.Gl.UseProgram shader.Handle

    let create (gl:GL) (vertexPath:string) (fragmentPath:string) : Result<Shader, string> =
        //Load the individual shaders.
        let vertexRes = loadShader gl ShaderType.VertexShader vertexPath
        let fragmentRes = loadShader gl ShaderType.FragmentShader fragmentPath
        
        match vertexRes, fragmentRes with
        | Ok vertex, Ok fragment ->
            //Create the shader program.
            let handle = gl.CreateProgram ()
            //Attach the individual shaders.
            gl.AttachShader (handle, vertex)
            gl.AttachShader (handle, fragment)
            gl.LinkProgram handle
            
            //Detach and delete the shaders
            gl.DetachShader (handle, vertex)
            gl.DetachShader (handle, fragment)
            gl.DeleteShader vertex
            gl.DeleteShader fragment
            
            //Check for linking errors.
            match gl.GetProgram (handle, GLEnum.LinkStatus) with
            | 0 ->
                gl.DeleteProgram handle
                Error $"Program failed to link with error: {gl.GetProgramInfoLog(handle)}"
            | _ ->
                Ok { Handle=handle; Gl=gl }
        | Error err1, Error err2 -> Error $"{err1} {err2}"
        | Error error, _
        | _, Error error -> Error error
        