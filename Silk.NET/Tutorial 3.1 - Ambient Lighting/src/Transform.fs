namespace Tutorial1_4_Abstractions

open System.Numerics

type Transform = {
    Position : Vector3
    Scale : float32
    Rotation : Quaternion }
    with
    static member Init = {
        Position = Vector3 (0f, 0f, 0f)
        Scale = 1f
        Rotation = Quaternion.Identity }
    member this.viewMatrix () =
        Matrix4x4.Identity *
        Matrix4x4.CreateFromQuaternion this.Rotation*
        Matrix4x4.CreateScale this.Scale *
        Matrix4x4.CreateTranslation this.Position
    