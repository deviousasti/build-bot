module ConfigParser

open System
open System.IO

type ConfigSection = { Name: string; Path: string option; Values: Map<string,string> }

let parse config_file =
    let isSectionHeader (line:string) = line.StartsWith("[") && line.EndsWith("]")
    let toTuple split = 
        if Array.length split > 1 then
            (split.[0], split.[1])            
        else
            (split.[0], "")

    let splitSectionHeader (line:string) = 
        line.Trim('[',']').Split([|' '|], 2) |> toTuple
        
    let splitValue (line:string) = 
        line.Split([|'='|], 2) |> Array.map(fun s-> s.Trim()) |> toTuple


    File.ReadLines(config_file)
    |> Seq.map (fun s -> s.Trim())
    |> Seq.scan (fun (section, prev) line -> 
                    if isSectionHeader line then 
                        splitSectionHeader line                        
                    else
                        (section, line)
    ) ("", "")
    |> Seq.skip  1
    |> Seq.groupBy fst
    |> Seq.map (fun (key, list) -> 
        {
            Name = key;
            Path =  list 
                    |> Seq.map snd 
                    |> Seq.tryHead 
                    |> Option.filter (not << String.IsNullOrWhiteSpace);
            Values = 
                    list 
                    |> Seq.skip 1 
                    |> Seq.map (snd >> splitValue) 
                    |> Map.ofSeq;
        })
        
    

