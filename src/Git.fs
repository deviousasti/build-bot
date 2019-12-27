module Git

open System
open System.IO

let where = 
    let is_unix = Environment.OSVersion.Platform = PlatformID.Unix
    let path = Shell.where (if is_unix then "git" else "git.exe")
    match path with 
    | Some(x) -> x 
    | None -> failwith "Git not found in path"

let private settings = Shell.Options.defaultSettings

let git args path = Shell.create { settings with WorkingDirectory = Some(path) } where args

let version () = git "--version" "."

let pull = git "pull"
let push = git "push"

let gitconfig dir = Path.Combine(dir, ".git", "config")
let isGitDirectory = gitconfig >> File.Exists

