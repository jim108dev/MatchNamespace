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

    let replaceNamespaceInFile (filePath: string) (replacement: string) =
        File.ReadAllLines filePath
        |> Seq.toList
        |> replaceNamespaceInText replacement
        |> fun contents -> File.WriteAllLines(filePath, contents)

    let replaceNamespacesInDirectory (prefix: Option<string>) (root: string) =
        Directory.EnumerateFiles(root, "*.fs", SearchOption.AllDirectories)
        |> Seq.iter (fun filePath ->
            let relativePath = Path.GetRelativePath(root, filePath)
            let namespaceFromPath = getNamespaceFromPath relativePath

            let newNamespace =
                match prefix, namespaceFromPath with
                | Some p, "" -> p
                | Some p, n -> (String.concat "." [| p; n |])
                | None, n -> n

            replaceNamespaceInFile filePath newNamespace)

    [<EntryPoint>]
    let main args =
        let arguments = A.parser.Parse args
        let prefix = arguments.TryGetResult Argument.Prefix
        let maybeRoot = arguments.TryGetResult Argument.Root

        match maybeRoot with
        | Some root ->
            match Directory.Exists root with
            | false ->
                printfn $"Directory {root} not found."
                -1
            | true ->
                replaceNamespacesInDirectory prefix root
                0
        | _ ->
            A.printUsage A.parser
            -1
