module Env

open System

let isValid = not << String.IsNullOrWhiteSpace

let getValue key = 
    match Environment.GetEnvironmentVariable(key) with 
    | s when isValid s -> Some(s)
    | _ -> None
