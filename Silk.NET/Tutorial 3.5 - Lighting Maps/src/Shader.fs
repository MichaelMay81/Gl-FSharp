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
        shader.Gl.DeleteProgram shader.Handle

    let private getUniformLocation (shader:Shader) (name:string) : Result<int, string> =
        match shader.Gl.GetUniformLocation (shader.Handle, name) with
        | -1 ->
            Error $"{name} uniform not found on shader."
        | location ->
            Ok location

    let setUniformFloat (name:string) (value:float32) (shader:Shader) : Result<unit, string> =
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
        shader.Gl.UseProgram shader.Handle

    let create (gl:GL) (vertexPath:string) (fragmentPath:string) : Result<Shader, string> =
        let vertexRes = loadShader gl ShaderType.VertexShader vertexPath
        let fragmentRes = loadShader gl ShaderType.FragmentShader fragmentPath
        
        match vertexRes, fragmentRes with
        | Ok vertex, Ok fragment ->
            let handle = gl.CreateProgram ()
            gl.AttachShader (handle, vertex)
            gl.AttachShader (handle, fragment)
            gl.LinkProgram handle
            
            gl.DetachShader (handle, vertex)
            gl.DetachShader (handle, fragment)
            gl.DeleteShader vertex
            gl.DeleteShader fragment
            
            match gl.GetProgram (handle, GLEnum.LinkStatus) with
            | 0 ->
                Error $"Program failed to link with error: {gl.GetProgramInfoLog(handle)}"
            | _ ->
                Ok { Handle=handle; Gl=gl }
        | Error err1, Error err2 -> Error $"{err1} {err2}"
        | Error error, _
        | _, Error error -> Error error
        