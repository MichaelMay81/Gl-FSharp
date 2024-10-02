namespace Tutorial1_4_Abstractions

open System
open Silk.NET.OpenGL

type VertexArrayObject = {
    Handle : uint
    Gl : GL }

module VertexArrayObjects =
    let dispose (vao:VertexArrayObject) =
        //Remember to dispose this object so the data GPU side is cleared.
        //We don't delete the VBO and EBO here, as you can have one VBO stored under multiple VAO's.
        vao.Gl.DeleteVertexArray vao.Handle

    let bind (vao:VertexArrayObject) : unit =
        //Binding the vertex array.
        vao.Gl.BindVertexArray vao.Handle

    let vertexAttributePointer (index:uint) (count:uint) (vertexSize:uint) (offset:uint) (vao:VertexArrayObject) =
        let dataSize = VertexBufferObject.DataSize
        let vapType = VertexBufferObject.VertexAttribPointerType
        
        //Setting up a vertex attribute pointer
        vao.Gl.VertexAttribPointer (
            index,
            int count,
            vapType,
            false,
            vertexSize * uint dataSize,
            IntPtr(int offset * dataSize).ToPointer ())
        vao.Gl.EnableVertexAttribArray index

    let create (gl:GL) (vbo:VertexBufferObject) (ebo:ElementBufferObject option) =
        //Setting out handle and binding the VBO and EBO to this VAO.
        let handle = gl.GenVertexArray ()
        handle |> gl.BindVertexArray
        vbo.BufferObject |> BufferObjects.bind
        ebo |> Option.iter (fun ebo -> ebo.BufferObject |> BufferObjects.bind)
        
        { Handle = handle
          Gl = gl }