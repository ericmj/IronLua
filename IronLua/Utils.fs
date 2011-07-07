namespace IronLua

module Utils =
    type Either<'a, 'b> =
        | Left of 'a
        | Right of 'b
