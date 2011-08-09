namespace IronLua

module Helper =
    module Array =
        open Microsoft.FSharp.Collections

        let resize<'a> (array:'a[]) size (value:'a) =
            if size > Array.length array then
                Array.append array (Array.create (size - Array.length array) value)
            elif size < Array.length array then
                Array.sub array 0 size
            else
                array
