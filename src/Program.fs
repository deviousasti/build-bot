open System
open FatSlack
open FatSlack.Types
open FatSlack.Dsl
open FatSlack.Dsl.Types
open FatSlack.Configuration
open System.Diagnostics
open FSharp.Control.Reactive
open System.IO



[<EntryPoint>]
let main args =  

    //Shell.defaultSettings.WorkingDirectory <- Some(Path.GetFullPath("./test"))
    Git.pull @"D:\Experiments\serenity" |> Observable.log |> ignore

    Console.ReadLine() |> ignore
    //disp.Dispose()
    0