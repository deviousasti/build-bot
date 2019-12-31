module FSharp.Control.Reactive

open System
open FSharp.Control.Reactive


module Observable = 
    /// Creates an observable sequence from a specified Subscribe method implementation.
    let createWithDisposable subscribe =
        System.Reactive.Linq.Observable.Create(Func<IObserver<'Result>,IDisposable> subscribe)
    let subcribeLog v = v |> Observable.materialize |> Observable.subscribe (printf "%A\n")
    let ignoreElements = System.Reactive.Linq.Observable.IgnoreElements

module Disposable =
    let empty = Disposable.create id
    let compose seq = seq |> Seq.toArray |> Disposables.compose

module Subject =
    let empty = { new System.Reactive.Subjects.ISubject<_> with
                      member this.OnCompleted() = ()
                      member this.OnError(error) = ()
                      member this.OnNext(value) = ()
                      member this.Subscribe(observer) = Disposable.empty
                }
    let empty2 = { new System.Reactive.Subjects.ISubject<_,_> with
        member this.OnCompleted() = ()
        member this.OnError(error) = ()
        member this.OnNext(value) = ()
        member this.Subscribe(observer) = Disposable.empty
    }