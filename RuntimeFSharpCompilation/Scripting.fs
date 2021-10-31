module Scripting

open System
open System.IO
open System.Linq.Expressions
open System.Reflection
open FSharp.Compiler.CodeAnalysis

let getMemberInfo (name:string) (assembly:Assembly) =
  let fqTypeName, memberName =
    let splitIndex = name.LastIndexOf(".")
    name.[0..splitIndex - 1], name.[splitIndex + 1..]
  let candidates = assembly.GetTypes() |> Seq.where (fun t -> t.FullName = fqTypeName) |> Seq.toList    
  match candidates with
  | [t] ->
    match t.GetMethod(memberName, BindingFlags.Static ||| BindingFlags.Public) with
    | null -> Error "Member not found"
    | memberInfo -> Ok memberInfo
  | [] -> Error "Parent type not found"
  | _ -> Error "Multiple candidate parent types found"

let compileScript name =
  let script = $"./Scripts/{name}.fsx"
  let outputAssemblyName = Path.ChangeExtension(Path.GetFileName(script),".dll")
  let checker = FSharpChecker.Create()
  let errors, exitCode, dynamicAssemblyOption =
    checker.CompileToDynamicAssembly([|
      "-o" ; outputAssemblyName
      "-a" ; script
      "--targetprofile:netcore"
    |], Some (stdout, stderr))
    |> Async.RunSynchronously
    
  match exitCode, dynamicAssemblyOption with
  | 0, Some dynamicAssembly -> Ok dynamicAssembly
  | _ ->
    errors
    |> Array.map(fun error ->
      $"{error.StartLine}: {error.Message}"
    )
    |> String.concat("\n")    
    |> Error

let extractor<'r> name assembly (lambdaConstructor:(Expression -> ParameterExpression [] -> LambdaExpression)) parameters =
  match getMemberInfo name assembly with
  | Ok memberInfo ->
    try
      let lambda =
        let expression =
          if (typeof<'r> = typeof<unit>) then
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
      let systemFunc = lambda.Compile()
      systemFunc |> Ok
    with
    | ex -> Error $"{ex.GetType().Name}: {ex.Message}"
  | Error error -> Error error

let extractFunction1<'p1,'r> name (assembly:Assembly) : Result<'p1 -> 'r,string> =
  // we can't use CreateDelegate due to some type issues - namely void and unit which
  // require some special care
  (*
  getMemberInfo name assembly
  |> Result.map(fun memberInfo ->
    let createdDelegate = Delegate.CreateDelegate(typeof<Func<'p1,'r>>, null, memberInfo)
    createdDelegate :?> Func<'p1,'r> |> FuncConvert.FromFunc
  )
  *)  
  let lambdaConstructor expression parameters = Expression.Lambda(expression, parameters)
  let parameters = [| Expression.Parameter(typeof<'p1>) |]  
  let systemFuncResult = extractor<'r> name assembly lambdaConstructor parameters
  systemFuncResult |> Result.map(fun systemFunc -> systemFunc :?> Func<'p1,'r> |> FuncConvert.FromFunc )

let extractFunction2<'p1,'p2,'r> name (assembly:Assembly) : Result<'p1 -> 'p2 -> 'r,string> =
  let lambdaConstructor expression parameters = Expression.Lambda(expression, parameters)
  let parameters = [|
    Expression.Parameter(typeof<'p1>)
    Expression.Parameter(typeof<'p2>)    
  |]  
  let systemFuncResult = extractor<'r> name assembly lambdaConstructor parameters
  systemFuncResult |> Result.map(fun systemFunc -> systemFunc :?> Func<'p1,'p2,'r> |> FuncConvert.FromFunc )
  
let extractFunction3<'p1,'p2,'p3,'r> name (assembly:Assembly) : Result<'p1 -> 'p2 -> 'p3 -> 'r,string> =
  let lambdaConstructor expression parameters = Expression.Lambda(expression, parameters)
  let parameters = [|
    Expression.Parameter(typeof<'p1>)
    Expression.Parameter(typeof<'p2>)
    Expression.Parameter(typeof<'p3>)
  |]    
  let systemFuncResult = extractor<'r> name assembly lambdaConstructor parameters
  systemFuncResult |> Result.map(fun systemFunc -> systemFunc :?> Func<'p1,'p2,'p3,'r> |> FuncConvert.FromFunc )