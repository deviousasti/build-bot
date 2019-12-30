module Build

open System.IO

type private GitRel = 
    { path: string; relative: string list; remote: string; }

type Repository = { local: string; remote: string; root: string }

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
    
    let combine p1 p2 = Path.Combine(p1, p2)
    seq {
        let repos = build (combine source ".git") [] 
        for repo in repos do 
            yield { 
                root = source;
                remote  = repo.remote;
                local = repo.relative 
                        |> List.skip 1
                        |> List.rev
                        |> List.fold(combine) source
            }
    }
    
let gitGraph (workspace:string) =
    workspace.Split ';'
    |> Seq.collect(Directory.EnumerateDirectories)
    |> Seq.filter(Git.Paths.isGitDirectory)
    |> Seq.collect(gitTree)
    |> Seq.groupBy(fun repo -> repo.remote)
    |> Map.ofSeq

