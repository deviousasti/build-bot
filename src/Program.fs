﻿open System
open System.IO
open FSharp.Control.Reactive
open FatSlack.Types

    
[<EntryPoint>]
let main args =  
   
    let workspace = 
        match Env.getValue "BOT_WORKSPACE" with         
        | None ->       printfn "Workspace is missing. Using current directory."        
                        printfn "Use environment variable BOT_WORKSPACE"
                        Directory.GetCurrentDirectory()
        | Some(dir) when (Directory.Exists dir) ->  dir
        | Some(notexist) ->  failwith (sprintf "Workspace directory %s does not exist" notexist)
                        

    printfn "Workspace: %s" workspace
    let graph = ref (Build.gitGraph workspace)
    
    printfn "Logging to: %s" (Path.GetTempPath())

    printfn "Tracking Repositories:"
    !graph |> Build.urls |> Seq.iteri(printfn "%d>\t%s")    

    let messages =
        match Env.getValue "BOT_SLACK_API_TOKEN" with
        | None ->   printfn "API Token is missing. Not using bot."        
                    printfn "Use environment variable BOT_SLACK_API_TOKEN"
                    Subject.empty2
        | Some(token) -> Bot.start token 
                          (
                              [
                                  Commands.hiCommand    
                                  Commands.pushCommand  
                                  Commands.buildCommand 
                                  Commands.scanCommand  
                                  Commands.addCommand   
                                  Commands.removeCommand   
                                  Commands.listCommand  
                              ] 
                              |> List.map(fun cmd -> cmd workspace graph)
                          )
                          
    let subscriptions =
        [
            messages 
            |> Observable.filter(Bot.isHello) 
            |> Observable.subscribe (printfn "Connected: %a" Bot.writeMessage)

            messages 
            |> Observable.flatmapSeq(fun evt -> !graph |> Build.matches (Bot.messageText evt) |> Seq.map(fun repo -> evt, repo)) 
            |> Observable.iter (fun (evt, repo) -> printfn "Changes in %s (%s) from %s" repo.name repo.remote evt.Channel)
            |> Observable.flatmap (fun (evt, repo) -> Build.run repo |> Observable.map(fun result -> evt, result))
            |> Observable.map (Bot.createMessage)
            |> Observable.subscribeObserver(messages)
        ]
        
    printfn "Hit <return> to exit"
    Console.ReadLine() |> ignore
    subscriptions |> List.iter Disposable.dispose
    0