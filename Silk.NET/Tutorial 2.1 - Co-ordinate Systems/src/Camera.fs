namespace Tutorial2_1_Co_ordinate_Systems

open System.Numerics

type Camera = {
    Position: Vector3
    Target: Vector3 }

module Cameras =
    //Set up the camera's location, and where it should look.
    let init =
        { Position=Vector3 (0f, 0f, 3f)
          Target=Vector3.Zero }

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