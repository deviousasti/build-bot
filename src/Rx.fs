module FSharp.Control.Reactive

open System
open FSharp.Control.Reactive


module Observable = 
    /// Creates an observable sequence from a specified Subscribe method implementation.
    let createWithDisposable subscribe =
        System.Reactive.Linq.Observable.Create(Func<IObserver<'Result>,IDisposable> subscribe)
    let log v = v |> Observable.materialize |> Observable.subscribe (printf "%A\n")

module Disposable =
    let empty = Disposable.create id
    let compose seq = seq |> Seq.toArray |> Disposables.compose