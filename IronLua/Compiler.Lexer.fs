namespace IronLua.Compiler

open IronLua.Error

module Lexer =
    type Symbol =
        // Keywords
        | And           = 0
        | Break         = 1
        | Do            = 2
        | Else          = 3
        | Elseif        = 4
        | End           = 5
        | False         = 6
        | For           = 7
        | Function      = 8
        | If            = 9
        | In            = 10
        | Local         = 11
        | Nil           = 12
        | Not           = 13
        | Or            = 14
        | Repeat        = 15
        | Return        = 16
        | Then          = 17
        | True          = 18
        | Until         = 19
        | While         = 20

        // Punctuations
        | Plus          = 21
        | Minus         = 22
        | Star          = 23
        | Slash         = 24
        | Percent       = 25
        | Carrot        = 26
        | Hash          = 27
        | EqualEqual    = 28
        | TildeEqual    = 29
        | LessEqual     = 30
        | GreaterEqual  = 31
        | Less          = 32
        | Greater       = 33
        | Equal         = 34
        | LeftParen     = 35
        | RightParen    = 36
        | LeftBrace     = 37
        | RightBrace    = 38
        | LeftBrack     = 39
        | RightBrack    = 40
        | SemiColon     = 41
        | Colon         = 42
        | Comma         = 43
        | Dot           = 44
        | DotDot        = 45
        | DotDotDot     = 46

        // Literals
        | Number        = 47
        | String        = 48
        | Identifier    = 49

        // Markers
        | EOF           = 50

    let keywords =
        [
        "and", Symbol.And;
        "break", Symbol.Break;
        "do", Symbol.Do;
        "else", Symbol.Else;
        "elseif", Symbol.Elseif;
        "end", Symbol.End;
        "false", Symbol.False;
        "for", Symbol.For;
        "function", Symbol.Function;
        "if", Symbol.If;
        "in", Symbol.In;
        "local", Symbol.Local;
        "nil", Symbol.Nil;
        "not", Symbol.Not;
        "or", Symbol.Or;
        "repeat", Symbol.Repeat;
        "return", Symbol.Return;
        "then", Symbol.Then;
        "true", Symbol.True;
        "until", Symbol.Until;
        "while", Symbol.While;
        ] |> dict

    type Lexeme = Symbol * string * int * int

    // TODO: Friendly names of symbols
    let prettySymbol symbol =
        string symbol


    module Char =
        let isDecimal c =
            c >= '0' && c <= '9'

        let isHex c =
            (c >= '0' && c <= '9')
         || (c >= 'a' && c <= 'f')
         || (c >= 'A' && c <= 'F')

        let isFirstPunctuation c =
            match c with 
            | '+' | '-' | '*' | '/' | '%' | '^' | '#'
            | '=' | '~' | '<' | '>' | '(' | ')' | '{'
            | '}' | '[' | ']' | ';' | ':' | ',' | '.' -> true
            | _ -> false

        let isIdentifierStart c =
            (c >= 'a' && c <= 'z')
         || (c >= 'A' && c <= 'Z')
         || c = '_'

        let isIdentifier c =
            (c >= 'a' && c <= 'z')
         || (c >= 'A' && c <= 'Z')
         || (c >= '0' && c <= '9')
         || c = '_'


    module internal Input =
        type State =
            val File : string
            val Source : string
            val mutable Index : int
            val mutable Char : char
            val mutable Line : int
            val mutable Column : int

            val mutable StoredLine : int
            val mutable StoredColumn : int
            val mutable Buffer : System.Text.StringBuilder
        
            new(source) = {
                File = "<unknown>"
                Source = source
                Index = 0
                Char = '\000'
                Line = 1
                Column = 1
                StoredLine = -1
                StoredColumn = -1
                Buffer = System.Text.StringBuilder(1024)
            }

        let inline create source =
            State(source)

        let inline getAt (s:State) i =
            try s.Source.[i]
            with | :? System.IndexOutOfRangeException ->  
                raise <| CompileError(s.File, (s.Line, s.Column), Message.unexpectedEOF)

        let inline current (s:State) =
            getAt s s.Index

        let inline canContinue (s:State) =
            s.Index < s.Source.Length

        let inline advance (s:State) =
            s.Index <- s.Index + 1
            s.Column <- s.Column + 1

        let inline skip (s:State) n =
            s.Index <- s.Index + n
            s.Column <- s.Column + n
            
        let inline back (s:State) =
            s.Index <- s.Index - 1
            s.Column <- s.Column - 1

        let inline storePosition (s:State) =
            s.StoredColumn <- s.Column
            s.StoredLine <- s.Line

        let inline newline (s:State) =
            s.Line <- s.Line + 1
            s.Column <- 1

        let inline peek (s:State) =
            getAt s (s.Index+1)

        let inline canPeek (s:State) =
            s.Index+1 < s.Source.Length

        let inline bufferAppend (s:State) (c:char) =
            s.Buffer.Append(c) |> ignore

        let inline bufferAppendStr (s:State) (str:string) =
            s.Buffer.Append(str) |> ignore

        let inline bufferRemoveStart (s:State) length =
            s.Buffer.Remove(0, length) |> ignore

        let inline bufferRemoveEnd (s:State) length =
            s.Buffer.Remove(s.Buffer.Length-length, length) |> ignore

        let inline bufferClear (s:State) =
            s.Buffer.Clear() |> ignore

        let inline bufferLook (s:State) =
            s.Buffer.ToString()

        let inline output (s:State) sym : Lexeme =
            sym, null, s.StoredLine, s.StoredColumn

        let inline outputBuffer (s:State) sym : Lexeme =
            sym, s.Buffer.ToString(), s.StoredLine, s.StoredColumn

        let inline outputEOF (s:State) : Lexeme =
            Symbol.EOF, null, s.Line, s.Column


    open Input
    open Char

    let inline private faillexer (s:State) msg =
        raise <| CompileError(s.File, (s.Line, s.Column), msg)

    // Counts ='s until endChar [ or ], return -1 if unknown char found
    let rec private countEquals s endChar n =
        match current s with
        | '='                -> current s |> bufferAppend s
                                advance s
                                countEquals s endChar (n+1)
        | c when c = endChar -> n
        | c                  -> -1

    // Parses a newline
    let private nextLine s =
        // Handle windows-style newline
        if current s = '\r' && canPeek s && peek s = '\n' then
            advance s
        advance s
        newline s

    // Parses a numeric escape in a string, 
    // such as \X \XX \XXX where X is a decimal number
    let private bufferNumericEscape s =
        let rec bufferNumericEscape value n =
            if n >= 3 || (current s |> isDecimal |> not) then
                value
            else
                let newValue = 10*value + int (current s) - int '0'
                advance s
                bufferNumericEscape newValue (n+1)
        bufferNumericEscape 0 0 |> char |> bufferAppend s

    // Parses a long string literal, such as [[bla bla]]
    let private longStringLiteral s =
        storePosition s
        bufferClear s
        advance s

        let numEqualsStart = countEquals s '[' 0
        if numEqualsStart = -1 then
            faillexer s (Message.invalidLongStringDelimter (current s))

        // Skip immediately following newline
        match peek s with
        | '\r' | '\n' -> nextLine s
        | _           -> ()

        let rec longStringLiteral () =
            advance s
            current s |> bufferAppend s
            match current s with
            | ']' ->
                // Output string if matching ='s found
                advance s
                let numEqualsEnd = countEquals s ']' 0

                if numEqualsStart = numEqualsEnd then
                    // Trim long string delimters
                    numEqualsStart |> bufferRemoveStart s
                    numEqualsEnd + 1 |> bufferRemoveEnd s
                    advance s
                    outputBuffer s Symbol.String
                elif numEqualsEnd = -1 then
                    current s |> bufferAppend s
                    longStringLiteral()
                else
                    // Parse ']' again because it can be the start of another long string delimeter
                    back s
                    longStringLiteral()
            
            | _ ->
                longStringLiteral()

        longStringLiteral()

    // Parses a string literal, such as "bla bla"
    let private stringLiteral s endChar =
        storePosition s
        bufferClear s

        let rec stringLiteral () =
            advance s
            match current s with
            // Escape chars
            | '\\' ->
                advance s
                match current s with
                | 'a'  -> '\a' |> bufferAppend s
                | 'b'  -> '\b' |> bufferAppend s
                | 'f'  -> '\f' |> bufferAppend s
                | 'n'  -> '\n' |> bufferAppend s
                | 'r'  -> '\r' |> bufferAppend s
                | 't'  -> '\t' |> bufferAppend s
                | 'v'  -> '\v' |> bufferAppend s
                | '\"' -> '\"' |> bufferAppend s
                | '\'' -> '\'' |> bufferAppend s
                | '\\' -> '\\' |> bufferAppend s
                | '\r' -> '\r' |> bufferAppend s; nextLine s
                | '\n' -> '\n' |> bufferAppend s; nextLine s
                | c when isDecimal c -> bufferNumericEscape s
                // Lua manual says  ", ' and \ can be escaped outside of the above chars
                // but Luac allows any char to be escaped
                | c -> c |> bufferAppend s 
                stringLiteral()

            | '\r' | '\n' ->
                faillexer s Message.unexpectedEOS
                
            | c when c = endChar ->
                skip s 2
                outputBuffer s Symbol.String

            | c ->
                bufferAppend s c
                stringLiteral()
        
        stringLiteral()

    // Parses the exponent part of a numeric literal,
    // such as e+5 p8 e2
    let private bufferExponent s =
        current s |> bufferAppend s
        advance s

        if canContinue s && (current s = '-' || current s = '+') then
            current s |> bufferAppend s
            advance s

        let rec bufferExponent () =
            if canContinue s && current s |> isDecimal then
                current s |> bufferAppend s
                advance s
                bufferExponent()
            else ()

        bufferExponent()

    // Parses a hex literal, such as 0xFF or 0x10p4
    // Can be malformed, parser handles that
    let private numericHexLiteral s =
        storePosition s
        bufferClear s
        bufferAppendStr s "0x"
        advance s

        let rec numericHexLiteral () =
            advance s
            if not (canContinue s) then
                outputBuffer s Symbol.Number
            else
                match current s with
                | 'p' | 'P' ->
                    bufferExponent s
                    outputBuffer s Symbol.Number
                | c when isHex c ->
                    current s |> bufferAppend s
                    numericHexLiteral()
                | _ ->
                    outputBuffer s Symbol.Number

        numericHexLiteral()

    let private numericLiteral s =
        storePosition s
        bufferClear s
        current s |> bufferAppend s

        let rec numericLiteral () =
            advance s
            if not (canContinue s) then
                outputBuffer s Symbol.Number
            else
                match current s with
                | 'e' | 'E' ->
                    bufferExponent s
                    outputBuffer s Symbol.Number
                | c when isDecimal c || c = '.' ->
                    current s |> bufferAppend s
                    numericLiteral()
                | _ ->
                    outputBuffer s Symbol.Number

        numericLiteral()

    // Short comment, such as --bla bla bla
    let private shortComment s =
        let rec shortComment () =
            advance s
            match current s with
            | '\r' | '\n' -> nextLine s
            | _           -> shortComment()
        shortComment()
            

    // Long comment, such as --[[bla bla bla]]
    let private longComment s =
        advance s
        let numEqualsStart = countEquals s '[' 0

        let rec longComment () =
            advance s
            match current s with
            | ']' ->
                advance s
                let numEqualsEnd = countEquals s ']' 0

                if numEqualsStart = numEqualsEnd then
                    advance s
                elif numEqualsEnd = -1 then
                    longComment()
                else
                    // Parse ']' again because it can be the start of another long string delimeter
                    back s
                    longComment()

            | _ ->
                longComment()

        // Not a valid long string delimter handle as a short comment
        if numEqualsStart = -1 then
            shortComment s
        else
            longComment()

    // Punctuation
    let private punctuation s =
        storePosition s
        let twoPunct c s1 s2 =
            advance s
            if current s = c
                then s1
                else back s; s2

        match current s with
        | '+' -> Symbol.Plus
        | '-' -> Symbol.Minus
        | '*' -> Symbol.Star
        | '/' -> Symbol.Slash
        | '%' -> Symbol.Percent
        | '^' -> Symbol.Carrot
        | '#' -> Symbol.Hash
        | '(' -> Symbol.LeftParen
        | ')' -> Symbol.RightParen
        | '{' -> Symbol.LeftBrace
        | '}' -> Symbol.RightBrace
        | '[' -> Symbol.LeftBrack
        | ']' -> Symbol.RightBrack
        | ';' -> Symbol.SemiColon
        | ':' -> Symbol.Colon
        | ',' -> Symbol.Comma

        | '=' -> twoPunct '=' Symbol.EqualEqual Symbol.Equal
        | '<' -> twoPunct '=' Symbol.LessEqual Symbol.Less
        | '>' -> twoPunct '=' Symbol.GreaterEqual Symbol.Greater
        
        | '.' ->
            advance s
            if current s = '.' then
                advance s
                if current s = '.'
                    then Symbol.DotDotDot
                    else Symbol.DotDot
            else
                Symbol.Dot

        | '~' ->
            advance s
            match current s with
            | '=' -> Symbol.TildeEqual
            | c   -> faillexer s (Message.unexpectedChar c)

        | c  -> faillexer s (Message.unexpectedChar c)

    // Identifier or keyword
    let private identifier s =
        storePosition s
        bufferClear s

        let rec identifier () =
            if canContinue s && current s |> isIdentifier then
                current s |> bufferAppend s
                advance s
                identifier()

        identifier()

        // Keyword or identifier?
        let mutable symbol = Symbol.EOF
        if keywords.TryGetValue(bufferLook s, &symbol) then
            output s symbol
        else
            outputBuffer s Symbol.Identifier

    // Create lexer - not thread safe, use multiple instances for concurrency
    let create source =
        let s = Input.create source

        let rec lexer () : Lexeme =
            if not (canContinue s) then
                outputEOF s
            else
                match current s with
                // Whitespace
                | ' ' | '\t' ->
                    advance s
                    lexer()
                
                // Newlines
                | '\r' | '\n' ->
                    nextLine s
                    lexer()

                // String
                | '\'' | '"' ->
                    stringLiteral s (current s)
                // Long string
                | '[' when canPeek s && (peek s = '[' || peek s = '=') ->
                    longStringLiteral s

                // Comment or minus
                | '-' ->
                    storePosition s
                    advance s

                    match current s with
                    | '-' ->
                        if canPeek s && peek s = '['
                            then longComment s
                            else shortComment s
                        lexer()
                    | _   ->
                        Symbol.Minus |> output s

                // Hex numeric
                | '0' when canPeek s && (peek s = 'X' || peek s = 'x') ->
                    numericHexLiteral s
                // Numeric
                | c when isDecimal c || (c = '.' && (canPeek s && isDecimal (peek s))) ->
                    numericLiteral s

                // Identifier or keyword
                | c when isIdentifierStart c ->
                    identifier s

                // Punctuation
                | c when isFirstPunctuation c ->
                    let symbol = punctuation s
                    advance s
                    output s symbol

                | c ->
                    faillexer s (Message.unexpectedChar c)

        lexer
