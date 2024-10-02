[<AutoOpen>]
module Tutorial1_4_Abstractions.Helpers

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
