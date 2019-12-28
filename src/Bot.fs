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

type BotStream = { Receiver : IObservable<ChatMessage>; }

let start apiToken =
    let receiver = Subject.broadcast
    let api =
        init
        |> withApiToken apiToken
        |> withAlias "build-bot"
        |> withSpyCommand 
            { 
                Description = "none"; 
                EventMatcher = fun _ event -> receiver.OnNext(PostMessage(event)); false;
                EventHandler = fun _ _  -> ()
            }
        |> withHelpCommand
        |> BotApp.start        
    
    let sender = Observer.Create(fun message -> api.Send message |> ignore)    
    Subject.Create<Message>(sender, receiver)
    