namespace Tutorial1_5_Transformations

open System.Numerics

//A transform abstraction.
//For a transform we need to have a position, a scale, and a rotation,
//depending on what application you are creating, the type for these may vary.

//Here we have chosen a vec3 for position, float for scale and quaternion for rotation,
//as that is the most normal to go with.
//Another example could have been vec3, vec3, vec4, so the rotation is an axis angle instead of a quaternion

type Transform = {
    Position : Vector3
    Scale : float32
    Rotation : Quaternion }

module Transforms =
    let init = {
        Position = Vector3 (0f, 0f, 0f)
        Scale = 1f
        Rotation = Quaternion.Identity }

    let viewMatrix (transform:Transform) =
        Matrix4x4.Identity *
        Matrix4x4.CreateFromQuaternion transform.Rotation*
        Matrix4x4.CreateScale transform.Scale *
        Matrix4x4.CreateTranslation transform.Position
    