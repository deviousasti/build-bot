open System
open System.Diagnostics
open FSharp.Control.Reactive
open System.IO
open System.Reactive.Subjects

module Env = 
    let isValid = not << String.IsNullOrWhiteSpace

    let getValue key = 
        match Environment.GetEnvironmentVariable(key) with 
        | s when isValid s -> Some(s)
        | _ -> None

    
[<EntryPoint>]
let main args =  
   
    let workspace = 
        match Env.getValue "BOT_WORKSPACE" with         
        | None ->   printfn "Workspace is missing. Using current directory."        
                    printfn "Use environment variable BOT_WORKSPACE"
                    Directory.GetCurrentDirectory()
        | Some(dir) -> dir

    printfn "Workspace: %s" workspace
    let graph = Build.gitGraph workspace
    
    printfn "Tracking Repositories:"
    graph |> Build.urls |> Seq.iteri(printfn "%d>\t%s")
    
    let messages =
        match Env.getValue "BOT_SLACK_API_TOKEN" with
        | None ->   printfn "API Token is missing. Not using bot."        
                    printfn "Use environment variable BOT_SLACK_API_TOKEN"
                    Subject.empty2
        | Some(token) -> Bot.start token

    let subscriptions =
        [
            messages 
            |> Observable.filter(Bot.isHello) 
            |> Observable.subscribe (printfn "Connected: %a" Bot.writeMessage)

            messages 
            |> Observable.flatmapSeq(fun evt -> graph |> Build.matches (Bot.messageText evt) |> Seq.map(fun repo -> evt, repo)) 
            |> Observable.iter (fun (evt, repo) -> printfn "Changes in %s (%s) from %s" repo.name repo.remote evt.Channel)
            |> Observable.flatmap (fun (evt, repo) -> Build.run repo |> Observable.map(fun result -> evt, result))
            |> Observable.map (Bot.createMessage)
            |> Observable.subscribeObserver(messages)
        ]
        
        
    Console.ReadLine() |> ignore
    subscriptions |> List.iter Disposable.dispose
    0