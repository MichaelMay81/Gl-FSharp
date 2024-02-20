namespace Tutorial1_4_Abstractions

open System
open Silk.NET.OpenGL

type VertexArrayObject = {
    Handle : uint
    Gl : GL
    VertexBufferDataType : DataType }

module VertexArrayObjects =
    let dispose (vao:VertexArrayObject) =
        //Remember to dispose this object so the data GPU side is cleared.
        //We dont delete the VBO and EBO here, as you can have one VBO stored under multiple VAO's.
        vao.Gl.DeleteVertexArray vao.Handle

    let bind (vao:VertexArrayObject) : unit =
        //Binding the vertex array.
        vao.Gl.BindVertexArray vao.Handle

    let vertexAttributePointer (index:uint) (count:uint) (vertexSize:uint) (offset:uint) (vao:VertexArrayObject) =
        let dataSize, vapType =
            match vao.VertexBufferDataType with
            | Float -> sizeof<float32>, VertexAttribPointerType.Float
            | UInt -> sizeof<uint32>, VertexAttribPointerType.UnsignedInt

        //Setting up a vertex attribute pointer
        vao.Gl.VertexAttribPointer (
            index,
            int count,
            vapType,
            false,
            vertexSize * uint dataSize,
            IntPtr(int offset * dataSize).ToPointer ())
        vao.Gl.EnableVertexAttribArray index

    let create (gl:GL) (vbo:BufferObject<'V>) (ebo:BufferObject<uint32> option) =
        //Setting out handle and binding the VBO and EBO to this VAO.
        let handle = gl.GenVertexArray ()
        handle |> gl.BindVertexArray
        vbo |> BufferObjects.bind
        ebo |> Option.iter BufferObjects.bind

        { Handle = handle
          Gl = gl
          VertexBufferDataType = vbo.DataType }