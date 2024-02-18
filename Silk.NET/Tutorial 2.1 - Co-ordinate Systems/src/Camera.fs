namespace Tutorial1_4_Abstractions

open System.Numerics

type Camera = {
    Position: Vector3
    Target: Vector3 }
    with static member init =
            { Position=Vector3 (0f, 0f, 3f)
              Target=Vector3.Zero }

module Cameras =
    let direction (camera:Camera) =
        camera.Position - camera.Target
        |> Vector3.Normalize

    let right (camera:Camera) =
        camera
        |> direction
        |> (fun dir -> Vector3.Cross (Vector3.UnitY, dir))
        |> Vector3.Normalize

    let up (camera:Camera) =
        Vector3.Cross (
            camera |> direction,
            camera |> right )

    let viewMatrix (camera:Camera) =
        Matrix4x4.CreateLookAt (
            camera.Position,
            camera.Target,
            camera |> up)