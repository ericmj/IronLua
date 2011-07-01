namespace IronLua

module Error =
    open System

    [<AbstractClass>]
    type Error(msg) =
        inherit Exception(msg)

    type CompileError(file, position, msg) = 
        inherit Error(msg)
        
        member x.File = file
        member x.Position = position

        member x.PrettyPrint =
            sprintf "%s(%i, %i): %s" file (fst position) (snd position) msg

    module internal Message =
        let unexpectedChar = sprintf "Unexpected '%c'"
        let unexpectedEOS = "Unexpected end of string"
        let unknownEscapeChar = sprintf "Unknown escape char '\%c'"
        let unexpectedEOF = "Unexpected end of file"
        let invalidLongStringDelimter = sprintf "Invalid long string delimter '%c'"
