module ConfigParser

open System
open System.IO

type ConfigSection = { Name: string; Path: string; Values: Map<string,string> }

let parse config_file =
    let isSectionHeader (line:string) = line.StartsWith("[") && line.EndsWith("]")
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
    |> Seq.scan (fun (section, prev) line -> 
                    if isSectionHeader line then 
                        (line, line)                        
                    else
                        (section, line)
    ) ("", "")
    |> Seq.skip  1
    |> Seq.groupBy fst
    |> Seq.map (fun (key, list) -> 
        let header = splitSectionHeader key         
        {
            Name =  fst header;
            Path =  snd header;
            Values = 
                    list 
                    |> Seq.skip 1 
                    |> Seq.map (snd >> splitValue) 
                    |> Map.ofSeq;
        })
        
    
let tryParse config_file =
    if File.Exists config_file then
        parse config_file 
    else
        Seq.empty