namespace MatchNamespace

module Argument = 

    open Argu
    open System

    type CliArguments =
        | [<Unique;AltCommandLine("-p")>] Prefix of path:string
        | [<MainCommand; ExactlyOnce; Last>] Root of path:string

        interface IArgParserTemplate with
            member s.Usage =
                match s with
                | Prefix _ -> "Specify a namespace prefix."
                | Root _ -> "Specify a the root directory."


    let errorHandler =
        ProcessExiter
            (colorizer =
                function
                | ErrorCode.HelpText -> None
                | _ -> Some ConsoleColor.Red)

    let printUsage (parser: ArgumentParser) = printfn "%s" <| parser.PrintUsage()

    let parser =
        ArgumentParser.Create<CliArguments>(programName = "MatchNamespace", errorHandler = errorHandler)
            
