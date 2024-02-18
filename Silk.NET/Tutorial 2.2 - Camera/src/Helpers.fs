[<AutoOpen>]
module Tutorial1_4_Abstractions.Heplers

open System

let degreesToRadians (degrees:float32) =
    MathF.PI / 180f * degrees

let resultToOption = function
    | Error error ->
        printfn "Error: %s" error
        None
    | Ok value ->
        Some value

let printError (result: Result<'T, string>) : unit =
    result
    |> Result.mapError (printfn "Error: %s")
    |> ignore
