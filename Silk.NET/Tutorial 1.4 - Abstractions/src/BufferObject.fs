namespace Tutorial1_4_Abstractions

open System
open Silk.NET.OpenGL

//Our buffer object abstraction.
type BufferObject = {
    Handle : uint
    BufferType : BufferTargetARB
    Gl : GL }

type ElementBufferObject = | ElementBufferObject of BufferObject
with member this.BufferObject = match this with | ElementBufferObject bo -> bo

type VertexBufferObject = | VertexBufferObject of BufferObject
with
    member this.BufferObject = match this with | VertexBufferObject bo -> bo
    static member DataSize = sizeof<float32>
    static member VertexAttribPointerType = VertexAttribPointerType.Float

module BufferObjects =
    let dispose (bufferObject:BufferObject) : unit =
        //Remember to delete our buffer.
        bufferObject.Gl.DeleteBuffer bufferObject.Handle

    let bind (bufferObject:BufferObject) : unit =
        //Binding the buffer object, with the correct buffer type.
        bufferObject.Gl.BindBuffer (bufferObject.BufferType, bufferObject.Handle)

    let private create (gl:GL) (bufferType:BufferTargetARB) (data:'T []) : BufferObject =
        //Getting the handle, and then uploading the data to said handle.
        let handle = gl.GenBuffer ()
        gl.BindBuffer (bufferType, handle)
        gl.BufferData (
            bufferType,
            ReadOnlySpan<'T> data,
            BufferUsageARB.StaticDraw)

        //Setting the gl instance and storing our buffer type.
        { BufferType = bufferType
          Gl = gl
          Handle = handle }

    let createVBO (gl:GL) (bufferType:BufferTargetARB) (data:float32 []) : VertexBufferObject =
        create gl bufferType data
        |> VertexBufferObject

    let createEBO (gl:GL) (bufferType:BufferTargetARB) (data:uint32 []) : ElementBufferObject =
        create gl bufferType data
        |> ElementBufferObject
