module ScriptingWithoutDelegate

open Scripting
open System
open System.Linq.Expressions
open System.Reflection

let extractWithoutDelegate<'p1,'r> name (assembly:Assembly) : Result<'p1 -> 'r,string> =
  match getMemberInfo name assembly with
  | Ok memberInfo ->
    try
      let lambdaConstructor expression parameters = Expression.Lambda<Func<'p1,'r>>(expression, parameters) 
      let lambda =
        let parameters = [| Expression.Parameter(typeof<'p1>) |]
        let expression =
          if typeof<'r> = typeof<unit> then
              Expression.Block(
                Expression.Call(
                  memberInfo,
                  parameters |> Array.map(fun param -> param :> Expression)
                ),
                Expression.Constant((), typeof<'r>)
              ) :> Expression
            else
              Expression.Convert(
                Expression.Call(
                  memberInfo,
                  parameters |> Array.map(fun param -> param :> Expression)
                ),
                typeof<'r>
              ) :> Expression
        lambdaConstructor expression parameters                
      let netFunc = lambda.Compile()
      FuncConvert.FromFunc netFunc |> Ok
    with
    | ex -> Error $"{ex.GetType().Name}: {ex.Message}"
  | Error error -> Error error