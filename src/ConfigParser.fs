module ConfigParser

open System
open System.IO

type ConfigSection = { Name: string; Path: string; Values: Map<string,string> }

(*
Ref: https://git-scm.com/docs/git-config#_syntax
Sample: 
    # This is the config file, and
    # a '#' or ';' character indicates
    # a comment
    #

    [section "subsection"]
	    ; Don't trust file modes
	    filemode = false
*)

let isSectionHeader (line:string) = line.StartsWith("[") && line.EndsWith("]")
let isComment (line:string) = line.StartsWith(';') || line.StartsWith('#')

let parse config_file =
    let toTuple = function 
        | [|x|]     -> (x, "")
        | [|x; y|]  -> (x, y)
        | _         -> ("", "")

    let splitSectionHeader (line:string) = 
        line.Split([|' '|], 2) |> Array.map(fun s -> s.Trim('[',']', ' ', '"')) |> toTuple
        
    let splitValue (line:string) = 
        line.Split([|'='|], 2) |> Array.map(fun s-> s.Trim()) |> toTuple


    File.ReadLines(config_file)
    |> Seq.map (fun s -> s.Trim())
    |> Seq.filter (not << isComment)
    |> Seq.scan (fun (section, prev) line -> 
                    if isSectionHeader line then 
                        (line, line)                        
                    else
                        (section, line)
    ) ("", "")
    |> Seq.skip  1
    |> Seq.groupBy fst
    |> Seq.map (fun (key, list) -> 
        let (header, path) = splitSectionHeader key         
        {
            Name =  header;
            Path =  path;
            Values = 
                    list 
                    |> Seq.map (snd >> splitValue) 
                    |> Map.ofSeq;
        })
        
    
let tryParse config_file =
    if File.Exists config_file then
        parse config_file 
    else
        Seq.empty