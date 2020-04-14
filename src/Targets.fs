module Targets

open System
open System.IO
open FSharp.Control.Reactive

let searchOptions = 
    let opt = EnumerationOptions()
    opt.MatchCasing <- MatchCasing.CaseInsensitive
    opt.IgnoreInaccessible <- true
    opt.RecurseSubdirectories <- false
    opt

let contains path pattern = 
    Directory.EnumerateFiles(path, pattern, searchOptions) |> Seq.isEmpty |> not

let inline ifContains pattern value path = 
    if contains path pattern then Some value else None

let ifFileContains file (text: string) value path =
    let filepath = Path.Combine(path, file)
    let contents = File.ReadAllText(filepath)
    if contents.Contains(text, StringComparison.InvariantCultureIgnoreCase) then 
        Some value
    else
        None

let exec cmd (args: Printf.StringFormat<string -> string>) settings target =         
    StdioObservable.create settings cmd (sprintf args target)
    
let (|Powershell|_|) =
    ifContains "build.ps1" (
        exec "powershell" "./build.ps1 %s"
    )

let (|Bash|_|) =
    ifContains "build.sh" (
        exec "bash" "-c ./build.sh %s"
    )

let (|Batch|_|) =
    ifContains "build.bat" (
        exec "cmd" "/c build.bat %s"
    )

let (|Command|_|) =
    ifContains "build.cmd" (
        exec "cmd" "/c build.cmd %s"
    )

let (|MSBuild|_|) =
    ifContains "*.sln" (
        exec "MSBuild" "/restore -p:Configuration=%s"
    )

let (|Makefile|_|) =
    ifContains "Makefile" (
        exec "make" "%s"
    )

let (|FAKE|_|) =
    ifContains "build.fsx" (
        exec "FAKE" "build.fsx %s"
    )

let (|CMake|_|) =
    ifContains "CMakeLists.txt" (
        exec "cmake" "--build . --config %s"
    )

let (|Ninja|_|) =
    ifContains "build.ninja" (
        exec "ninja" "%s"
    )

let (|PlatformIO|_|) =
    ifContains "platformio.ini" (
        exec "platformio" "run --target %s"
    )

let (|Gulp|_|) =
    ifContains "gulpfile.js" (
        exec "gulp" "%s"
    )

