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

type BotStream = ISubject<Message, ChatMessage>

let start apiToken : BotStream =
    let receiver = Subject.broadcast
    let api =
        init
        |> withApiToken apiToken
        |> withAlias "build-bot"
        |> withSpyCommand 
            { 
                Description = "none"; 
                EventMatcher = fun _ event -> receiver.OnNext(event); false;
                EventHandler = fun _ _  -> ()
            }
        |> withHelpCommand
        |> BotApp.start        
    
    let sender = Observer.Create(fun message -> 
       let response = api.Send message |> Async.RunSynchronously
       ()
    )    
    Subject.Create<Message, ChatMessage>(sender, receiver)
  
let messageText (msg: ChatMessage) = 
    msg.Text

let isHello (msg: ChatMessage) = msg.Type = "hello"

let writeMessage (tw: TextWriter) (msg: ChatMessage) = 
    tw.Write(sprintf "[%s] %s" msg.Type msg.Text)

let send message (stream:BotStream) = stream.OnNext(message)

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