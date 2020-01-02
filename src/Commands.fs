module Commands

open System
open FatSlack.Types
open FSharp.Control
open FSharp.Control.Reactive
open System.IO
open System.Text.RegularExpressions

let private matches toMatch command _ = 
    string(command).Split(' ') |> Array.tryHead |> Option.contains toMatch

let private args msg =
    (Bot.messageText msg).Split([|' '|], StringSplitOptions.RemoveEmptyEntries) 
    |> List.ofArray 
    |> List.map(fun s -> s.Trim('<', '>', '"'))
    |> List.skip 2

let wildCardToRegex value =
   "^" + Regex.Escape(value).Replace("\\?", ".").Replace("\\*", ".*") + "$" 

let wildCardMatch input pattern =
    Regex.IsMatch(input, (wildCardToRegex pattern))

let private reply text msg = { msg with ChatMessage.Text = text }
let private postTo api msg = 
    msg 
    |> PostMessage
    |> api.Send  
    |> Async.RunSynchronously 
    |> ignore
let private invalid = reply "Invalid command"

let hiCommand workspace graph =    
    {
        Syntax = "hi"
        Description = "Shows version info and workspace"
        EventMatcher = matches "hi"
        EventHandler = 
            fun api msg ->                        
                let version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString()
                let machine = Environment.MachineName
                let user = Environment.UserName
                msg 
                |> reply (sprintf 
                            "Hello! This is Build Bot v%s\nI am hosted on %s by user %s" 
                            version
                            machine
                            user
                )
                |> postTo api
    }     
let buildCommand _workspace graph =    
    {
        Syntax = "build <name>"
        Description = "Builds a repository and posts the result"
        EventMatcher = matches "build"
        EventHandler = 
            fun api msg ->                     
                match args msg with 
                | [name] -> match !graph |> Build.find (fun repo -> repo.name = name) with 
                            | None ->       msg |> reply "Matching repository not found"
                            | Some repo ->  msg |> reply (sprintf "Found %s" repo.remote)
                | _ -> msg |> invalid
                |> postTo api
    }     

let scanCommand workspace graph = 
    {
        Syntax = "scan"
        Description = "Rebuilds the git dependency tree again"
        EventMatcher = matches "scan"
        EventHandler = 
            fun api msg ->                     
                graph := Build.gitGraph workspace 
                msg 
                |> reply (sprintf "Rebuilt graph. Tracking %d repositories." (!graph |> Map.count))
                |> postTo api
                
    }     

let rescan workspace graph = (scanCommand workspace graph).EventHandler

let addCommand workspace graph =    
    {
        Syntax = "add <url>"
        Description = "Adds a repository to be tracked"
        EventMatcher = matches "add"
        EventHandler = 
            fun api msg ->                     
                match args msg with 
                | [url] -> match !graph |> Build.find (fun repo -> repo.remote = url) with 
                            | Some repo ->  msg |> reply "Repository already exists"
                            | None ->   let clone = Git.clone url workspace
                                        clone
                                        |> Observable.subscribeWithCallbacks 
                                            ignore 
                                            (fun exn -> msg |> reply ("Failed to clone: " + exn.Message) |> postTo api) 
                                            (fun () ->  msg |> reply "Cloned repository" |> postTo api                                                        
                                                        msg |> rescan workspace graph api
                                            )
                                        |> ignore

                                        msg |> reply "Adding repository..."
                | _ -> msg |> invalid
                |> postTo api
    }    

let removeCommand workspace graph =    
    {
        Syntax = "remove <name>"
        Description = "Removes a repository from the workspace"
        EventMatcher = matches "add"
        EventHandler = 
            fun api msg ->                     
                match args msg with 
                | [name] -> match !graph |> Build.find (fun repo -> repo.name = name) with 
                            | Some repo ->  try
                                            Directory.Delete(repo.local)
                                            msg |> rescan workspace graph api |> ignore
                                            msg |> reply "Removed repository" 
                                            with ex -> 
                                            msg |> reply (sprintf "Failed to remove repository: %s" ex.Message)                                                      

                            | None ->       msg |> reply "Repository does not exit"
                | _ -> msg |> invalid
                |> postTo api
    }   
    
let listCommand _workspace graph =    
    {
        Syntax = "ls [all|pattern]"
        Description = "Lists tracked repositories matching wildcard"
        EventMatcher = matches "ls"
        EventHandler = 
            fun api msg ->       
                let argv = args msg
                let repos = 
                    !graph 
                    |> Build.flatten 
                    |> Seq.map (fun r -> 
                                         let relpath = Path.GetRelativePath(Path.Combine(r.root,  ".."), r.local)
                                         match argv with
                                         | ["all"]   -> Some(relpath)
                                         | [pattern] -> if wildCardMatch relpath pattern then Some(relpath) else None
                                         | _ -> Some(r.name)
                                ) 
                    |> Seq.choose id
                    |> Seq.distinct
                    |> Seq.mapi (sprintf "%d> %s")
                    |> (fun s -> String.Join("\n", s))

                msg |> reply repos
                |> postTo api
    }  