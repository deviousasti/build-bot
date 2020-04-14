module Git

open System
open System.IO
open FSharp.Control.Reactive

module Paths =
    let config dir = Path.Combine(dir, "config")
    let submodule root dir = Path.Combine(root, "modules", dir)
    let isGitDirectory dir = Path.Combine(dir, ".git") |> Directory.Exists

let where = 
    let is_unix = Environment.OSVersion.Platform = PlatformID.Unix
    let path = StdioObservable.where (if is_unix then "git" else "git.exe")
    match path with 
    | Some(x) -> x 
    | None -> failwith "Git not found in path"

let private settings = StdioObservable.Options.defaults

let git args path = StdioObservable.create { settings with WorkingDirectory = Some(path) } where args

let version () = git "--version" "."

let pull = git "pull"
let push = git "push"
let clone url = git (sprintf "clone --recurse-submodules \"%s\"" url)
let commit message = git (sprintf "commit -m \"%s\"" message)
let add spec = git ("add " + spec)
let addAll = add "--all"

let hardreset = git "reset --hard"

let rootUrl (remote:string) = 
    let trailing = remote.LastIndexOf(".git", StringComparison.InvariantCultureIgnoreCase)
    if trailing = -1 then
        remote
    else
        remote.Substring(0, trailing)