namespace MatchNamespace

module Main =
    open System.IO
    open System.Text.RegularExpressions
    open Argu

    module A = Argument

    let NAMESPACE_PATTERN = "^namespace .*$"
    let MODULE_PATTERN = "^module .*$"
    let MAX_DEPTH = 10

    let getNamespaceFromPath (filePath: string) =
        let path = Path.GetDirectoryName filePath

        path.Split '/'
        |> Array.filter (fun path -> path <> ".")
        |> String.concat "."


    //http://www.fssnip.net/29/title/Regular-expression-active-pattern
    let (|Namespace|_|) input =
        let m = Regex.Match(input, NAMESPACE_PATTERN)
        if m.Success then Some() else None

    let (|Module|_|) input =
        let m = Regex.Match(input, MODULE_PATTERN)
        if m.Success then Some() else None

    let rec replaceNamespaceModuleInText
        (maxDepth: int)
        (namespaceReplacer: string -> string)
        (moduleReplacer: string -> string)
        (contents: list<string>)
        =
        let subCall =
            if maxDepth > 0 then
                replaceNamespaceModuleInText (maxDepth - 1) namespaceReplacer moduleReplacer
            else
                id

        match contents with
        | x :: xs ->
            match x with
            | Namespace -> namespaceReplacer x :: subCall xs
            | Module -> moduleReplacer x :: xs
            | x -> x :: subCall xs
        | [] -> []

    let replaceNamespaceInFile
        (doNothing: bool)
        (filePath: string)
        (namespaceReplacement: string)
        (moduleReplacement: string)
        =

        let namespaceReplacer (line: string) =
            if doNothing then
                printfn $"Replace namespace in {filePath} by {namespaceReplacement}"
                line
            else
                $"namespace {namespaceReplacement}"

        let moduleReplacer (line: string) =
            if doNothing then
                printfn $"Replace module in {filePath} by {moduleReplacement}"
                line
            else
                $"module {moduleReplacement} ="

        let contents =
            File.ReadAllLines filePath
            |> Seq.toList
            |> replaceNamespaceModuleInText MAX_DEPTH namespaceReplacer moduleReplacer

        if doNothing then
            ()
        else
            File.WriteAllLines(filePath, contents)

    let replaceNamespacesInDirectory (doNothing: bool) (prefix: Option<string>) (root: string) =
        Directory.EnumerateFiles(root, "*.fs", SearchOption.AllDirectories)
        |> Seq.iter (fun filePath ->
            let relativePath = Path.GetRelativePath(root, filePath)
            let namespaceFromPath = getNamespaceFromPath relativePath

            let newModule =
                Path.GetFileNameWithoutExtension filePath

            let newNamespace =
                match prefix, namespaceFromPath with
                | Some p, "" -> p
                | Some p, n -> (String.concat "." [| p; n |])
                | None, n -> n

            replaceNamespaceInFile doNothing filePath newNamespace newModule)

    [<EntryPoint>]
    let main args =
        let arguments = A.parser.Parse args
        let doNothing = arguments.Contains A.Nothing
        let prefix = arguments.TryGetResult A.Prefix
        let maybeRoot = arguments.TryGetResult A.Root


        match maybeRoot with
        | Some root ->
            if not (Directory.Exists root) then
                let fullPath = Path.GetFullPath root
                printfn $"Directory {fullPath} not found."
                -1
            else
                replaceNamespacesInDirectory doNothing prefix root
                0
        | _ ->
            A.printUsage A.parser
            -1
