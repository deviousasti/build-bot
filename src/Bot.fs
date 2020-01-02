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
open System.Linq

let messageText (msg: ChatMessage) = 
    let text = msg.Text
    if isNull text then "" else text.Trim()        

let isHello (msg: ChatMessage) = msg.Type = "hello"

let writeMessage (tw: TextWriter) (msg: ChatMessage) = 
    tw.Write(sprintf "[%s] %s" msg.Type msg.Text)

let inline createMessage (msg, result) = 
    let text (value : Build.BuildStatus) = 
        (sprintf "Build %s: *%s*" value.Repository.name value.Target)

    let attachLog count title color (value : Build.BuildStatus) = 
        try 
            let last = (Enumerable.TakeLast(File.ReadLines(value.Log.Value), count))
            let log = sprintf "```%s```" (String.Join("\n", last))
            Attachment.createAttachment (Guid.NewGuid().ToString())
            |> Attachment.withColor color
            |> Attachment.withFields [ Field.createLongField title log ]            
        with _ -> 
            Attachment.createAttachment ""            

    match result with 
    | Ok value ->   msg 
                    |> ChatMessage.withText (text value)
                    |> ChatMessage.withAttachments
                        [
                            value |> attachLog 5 "Passed" Good 
                        ]
                    
    | Error value -> msg 
                    |> ChatMessage.withText (text value)
                    |> ChatMessage.withAttachments
                        [
                            value |> attachLog 10 "Failed" Danger 
                        ]
    |> PostMessage

let isAddressed msg = (messageText msg).StartsWith("<@")

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
  