namespace MatchNamespace

module Main =
    open System.IO
    open System.Text.RegularExpressions
    open Argu

    module A = Argument

    let getNamespaceFromPath (filePath: string) =
        let path = Path.GetDirectoryName filePath

        path.Split '/'
        |> Array.filter (fun path -> path <> ".")
        |> String.concat "."

    let NAMESPACE_PATTERN = "^namespace .*$"

    //http://www.fssnip.net/29/title/Regular-expression-active-pattern
    let (|Namespace|_|) input =
        let m = Regex.Match(input, NAMESPACE_PATTERN)
        if m.Success then Some() else None

    let replaceNamespaceInText (replacement: string) (contents: list<string>) =
        let newHead = $"namespace {replacement}"

        match contents with
        | x :: xs ->
            match x with
            | Namespace -> newHead :: xs
            | _ -> newHead :: x :: xs
        | [] -> newHead :: []

    let replaceNamespaceInFile (doNothing:bool) (filePath: string) (replacement: string) =
        printfn $"Replace namespace in {filePath} by {replacement}"

        if doNothing then
            ()
        else
            File.ReadAllLines filePath
            |> Seq.toList
            |> replaceNamespaceInText replacement
            |> fun contents -> File.WriteAllLines(filePath, contents)

    let replaceNamespacesInDirectory (doNothing:bool) (prefix: Option<string>) (root: string) =
        Directory.EnumerateFiles(root, "*.fs", SearchOption.AllDirectories)
        |> Seq.iter (fun filePath ->
            let relativePath = Path.GetRelativePath(root, filePath)
            let namespaceFromPath = getNamespaceFromPath relativePath

            let newNamespace =
                match prefix, namespaceFromPath with
                | Some p, "" -> p
                | Some p, n -> (String.concat "." [| p; n |])
                | None, n -> n

            replaceNamespaceInFile doNothing filePath newNamespace)

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
                printfn $"Directory {fullPath} not found."; -1
            else
                replaceNamespacesInDirectory doNothing prefix root; 0
        | _ ->
            A.printUsage A.parser
            -1
