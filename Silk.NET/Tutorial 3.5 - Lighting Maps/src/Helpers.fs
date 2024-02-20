[<AutoOpen>]
module Tutorial1_4_Abstractions.Heplers

open System

let degreesToRadians (degrees:float32) =
    MathF.PI / 180f * degrees