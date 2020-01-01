module Bot

open System
open FatSlack
open FatSlack.Types
open FatSlack.Dsl
open FatSlack.Dsl.Types
open FatSlack.Configuration
open System.Reactive.Subjects
open FSharp.Control.Reactive
open System.Reactive
open System.IO

let messageText (msg: ChatMessage) = 
    let text = msg.Text
    if isNull text then "" else text.Trim()        

let isHello (msg: ChatMessage) = msg.Type = "hello"

let writeMessage (tw: TextWriter) (msg: ChatMessage) = 
    tw.Write(sprintf "[%s] %s" msg.Type msg.Text)

let inline createMessage (msg, result) = 
    let text status (value : Build.BuildStatus) = (sprintf "Build %s: %s [%s]" status value.Repository.name value.Target)
    match result with 
    | Ok value ->   msg 
                    |> ChatMessage.withText (text "Passed" value)
                    |> ChatMessage.withAttachments(
                        [
                            
                        ]
                    )
    | Error value -> msg |> ChatMessage.withText (text "Failed" value)
    |> PostMessage

let isAddressed msg = (messageText msg).StartsWith("@")

let start apiToken commands =
    let receiver = Subject.broadcast

    let api =
        init
        |> withApiToken apiToken
        |> withAlias "build-bot"
        |> withSlackCommands commands
        |> withSpyCommand 
            { 
                Description = "Listens for change notifications"; 
                EventMatcher = fun _ evt -> not (isAddressed evt) 
                EventHandler = fun _ evt -> receiver.OnNext(evt)
            }
        |> withHelpCommand
        |> BotApp.start        
    
    let sender = Observer.Create(fun message -> 
       let response = api.Send message |> Async.RunSynchronously
       ()
    )    
    Subject.Create<Message, ChatMessage>(sender, receiver)
  