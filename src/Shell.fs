module Shell

open System
open System.IO
open System.Diagnostics
open FSharp.Control.Reactive
open System.Threading.Tasks
open System.Reactive.Concurrency

module Options =

    type ExitMethod = 
        | InputClose 
        | Close 
        | CloseMainWindow 
        | Kill 
        | SendControlSignal of timeout:int 
        | SendQuitCommand   of command:string
  
     type ShellSettings =    
         {
               Input: IObservable<string>;
               WriteNewLines: bool;
               RedirectInput: bool;
               RedirectOutput: bool;
               RedirectError: bool;
               ExitMethod: ExitMethod;
               WorkingDirectory: string option;
               ExitCodes: int[];
               ExitDelay: int;
        }

    let defaultSettings = 
        { 
            Input = Observable.empty; 
            WriteNewLines = true; 
            RedirectInput = false;
            RedirectOutput = true;
            RedirectError = true;
            ExitMethod = Kill;
            WorkingDirectory = None;
            ExitCodes = [| 0 |];
            ExitDelay = 100;
        }



[<Serializable>]
exception ProcessTerminatedException of int

let kill (proc: Process) = function
    | _ when proc.HasExited         -> ()
    | Options.InputClose            -> proc.StandardInput.Close()
    | Options.Close                 -> proc.Close()
    | Options.Kill                  -> proc.Kill()
    | Options.CloseMainWindow       -> proc.CloseMainWindow() |> ignore
    | Options.SendQuitCommand(c)    -> proc.StandardInput.Write(c)
    | _                             -> ()
    

let listen (proc: Process) (settings:Options.ShellSettings) (observer : IObserver<string>) =
    
    let exit code =
        if settings.ExitCodes |> Array.contains code then
            observer.OnCompleted()
        else
            observer.OnError(ProcessTerminatedException(code))
        
    let exithandler _ =                    
        async {
            try 
                let code = proc.ExitCode
                do! Async.Sleep(settings.ExitDelay)
                exit code
            with 
                _ -> observer.OnCompleted()
        } |> Async.Start

    let datahandler (e:DataReceivedEventArgs) = 
        if not (isNull e.Data) then
            observer.OnNext(e.Data)

    let inputhandler: string -> unit = 
        if settings.RedirectInput then
            let stdin = proc.StandardInput
            if settings.WriteNewLines then stdin.WriteLine else stdin.Write
        else
            ignore
    
    let subscriptions =
        seq {            
            if settings.RedirectOutput then 
                proc.OutputDataReceived.Subscribe datahandler    
            else
                Disposable.empty
    
            if settings.RedirectError then 
                proc.ErrorDataReceived.Subscribe datahandler    
            else
                Disposable.empty

            if settings.RedirectInput then
                settings.Input |> Observable.subscribe inputhandler
            else
                Disposable.empty
            
            if not proc.HasExited then
                proc.Exited.Subscribe exithandler
            else
                exithandler ()
                Disposable.empty
        }
    
    proc.EnableRaisingEvents <- true

    if proc.HasExited then
        exithandler ()
        Disposable.empty
    else        
        subscriptions |> Disposable.compose
    
let private run (args:ProcessStartInfo) (settings: Options.ShellSettings) (observer : IObserver<string>) =
    try
    let proc = Process.Start args
    let subscription = listen proc settings observer     

    if settings.RedirectOutput then
        proc.BeginOutputReadLine()

    if settings.RedirectError then
        proc.BeginErrorReadLine()

    Disposable.create (fun () -> 
        try
            subscription.Dispose()
            kill proc settings.ExitMethod
            proc.CancelOutputRead()
            proc.Dispose()
        with | _ -> ()
    )

    with e -> 
    observer.OnError(e)    
    Disposable.empty

let createWith (settings:Options.ShellSettings) (args:ProcessStartInfo)  =
    args.RedirectStandardInput  <- settings.RedirectInput
    args.RedirectStandardOutput <- settings.RedirectOutput
    args.RedirectStandardError  <- settings.RedirectError
    args.UseShellExecute        <- false
    args.WorkingDirectory       <- match settings.WorkingDirectory with 
                                   | Some(dir) -> dir
                                   | None -> Directory.GetCurrentDirectory()

    Observable.createWithDisposable (run args settings)

let create settings filename args =
    createWith settings (new ProcessStartInfo(filename, args))

let private combine file path =
    Path.Combine (path, file)
    
let where executable =
    let paths = Environment.GetEnvironmentVariable "PATH"
    paths.Split(';') 
    |> Seq.map (combine executable) 
    |> Seq.filter (File.Exists)
    |> Seq.tryHead
    
    