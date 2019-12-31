module Build

open System
open System.IO
open FSharp.Control.Reactive
open System.Reactive

type private GitRel = 
    { path: string; relative: string list; remote: string; }

type Repository = { local: string; remote: string; root: string; name: string;  }
type Graph = Map<string, seq<Repository>>

let private combine p1 p2 = Path.Combine(p1, p2)

let gitTree source =
    //recursively build dependency tree and 
    //flatten it
    let rec build source (rel: string list) =
        let config_file = Git.Paths.config source        
        let configs = 
            Git.Paths.config source |> ConfigParser.tryParse 
    
        let findurl section_name = 
            configs 
            |> Seq.filter (fun section -> section.Name = section_name) 
            |> Seq.choose (fun section -> 
                section.Values 
                |> Map.tryFind "url" 
                |> Option.map(fun url -> { path = section.Path; relative = section.Path::rel; remote = url; })
            )        
     
        seq {
            yield! findurl "remote"
            for submod in findurl "submodule" do       
                let subpath = Git.Paths.submodule source submod.path
                yield! build subpath submod.relative
        }  

    seq {
        let repos = build (combine source ".git") [] 
        for repo in repos do 
            yield { 
                root = source;
                name = Path.GetFileName(source);
                remote  = repo.remote;
                local = repo.relative 
                        |> List.skip 1
                        |> List.rev
                        |> List.fold(combine) source
            }
    }
    
let gitGraph (workspace:string) : Graph =
    workspace.Split ';'
    |> Seq.collect(Directory.EnumerateDirectories)
    |> Seq.filter(Git.Paths.isGitDirectory)
    |> Seq.collect(gitTree)
    |> Seq.groupBy(fun repo -> Git.rootUrl repo.remote)
    |> Map.ofSeq

let matches (text:string) (graph:Graph) =
    if (String.IsNullOrEmpty text) then 
        Seq.empty
    else
        graph
        |> Map.toSeq 
        |> Seq.filter(fun (url, _) -> text.Contains(url, StringComparison.InvariantCultureIgnoreCase))
        |> Seq.collect snd

let urls (graph:Graph) = graph |> Map.toSeq |> Seq.map fst

let update (repo:Repository) = Git.pull repo.local

let getBuildTargets buildroot =
    let file = Path.Combine(buildroot, "build")
    let config = ConfigParser.tryParse file |> List.ofSeq
    match config with 
    | [] -> ["debug";"release"] |> List.map(fun t -> t, Map.empty)
    | [x] -> x.Values |> Map.toList |> List.map (fun (key, value) -> key, Map.empty)
    | many -> many |> List.map (fun section -> section.Name, section.Values)

type Pipeline = string * (string -> IObservable<string>)


let build (repo:Repository) =
    let buildroot = repo.root 
    let pipelines =
        match buildroot with 
        | Targets.Powershell x  -> [x]
        | Targets.Bash x        -> [x]
        | Targets.Command x     -> [x]
        | Targets.MSBuild x     -> [x]
        | Targets.Makefile x    -> [x]
        | Targets.FAKE x        -> [x]
        | Targets.CMake x       -> [x]
        | Targets.Ninja x       -> [x]
        | Targets.PlatformIO x  -> [x]
        | Targets.Gulp x        -> [x]
        | _ -> []

    let targets = getBuildTargets buildroot
    let settings = { Shell.Options.defaultSettings with WorkingDirectory = Some buildroot }
    
    seq {
        for pipeline in pipelines do
            for target, env in targets do
                let settings' = { settings with EnvironmentVariables = env }
                yield Pipeline(target, pipeline settings')
    }

let logfile target (repo:Repository) = 
    let name = repo.name
    let logfilename = sprintf "%s-%s-%d.log" name target (DateTime.UtcNow.ToFileTime())
    combine (Path.GetTempPath()) logfilename

type BuildStatus = { Repository: Repository; Target: string; Log: string option; Reason : string option; }
type BuildResult = Result<BuildStatus, BuildStatus>

let run (repo:Repository) =
    let toResult target log observable =
        observable
        |> Observable.materialize
        |> Observable.last
        |> Observable.map (
            fun notification -> 
            let result = { Repository = repo; Target = target; Log = log; Reason = None }
            match notification.Kind with 
            | NotificationKind.OnCompleted -> Ok(result)                                     
            | NotificationKind.OnError -> Error({ result with Reason = Some notification.Exception.Message })                                     
            | _ -> failwith "Unexpected kind"
        )

    let pull =
        update repo 
        |> Observable.log "Git"
        |> Observable.ignoreElements        
        |> Observable.retryCount 3
        |> Observable.mapTo Unchecked.defaultof<Result<BuildStatus, BuildStatus>>

    let builds =         
        build repo 
        |> Seq.map(fun (target, pipeline) ->             
            let log = logfile target repo
            Observable.using 
                (fun () -> new StreamWriter(File.OpenWrite(log)))
                (fun file -> 
                    pipeline target 
                    |> Observable.log ("Build " + target)
                    |> Observable.iter (file.WriteLine)
                )
            |> toResult target (Some log)
        )
        |> Observable.concatSeq                
    
    Observable.concat builds pull  
    