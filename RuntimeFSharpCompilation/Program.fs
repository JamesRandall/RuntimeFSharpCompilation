// Learn more about F# at http://docs.microsoft.com/dotnet/fsharp

open System
open Model

open Scripting

[<EntryPoint>]
let main _ =
  let logError (error:string) =
    Console.ForegroundColor <- ConsoleColor.Red
    Console.WriteLine error
    
  let assemblyOption = compileScript "ExampleScript"
  match assemblyOption with
  | Ok assembly ->  
    assembly
    |> extractFunction1<ExampleRecord, unit> "ExampleScript.scriptWithUnitResult"  
    |> (function
      | Ok extractedFunction -> extractedFunction { Surname = "Smith" ; Forename = "Joe" }
      | Error error -> error |> logError
    )
      
    assembly
    |> extractFunction1<ExampleRecord, int> "ExampleScript.scriptWithIntResult"
    |> (function
      | Ok extractedFunction -> extractedFunction { Surname = "Smith" ; Forename = "Joe" } |> ignore
      | Error error -> error |> logError
    )
    
    assembly
    |> extractFunction2<int, int, int> "ExampleScript.add"
    |> (function
      | Ok extractedFunction -> printf $"Add: {extractedFunction 1 2}" 
      | Error error -> error |> logError
    )
    
  | Error error -> error |> logError  
  0 // return an integer exit code