# IronLua

IronLua is intended to be a full implementation of Lua targeting .NET. Allowing easy embedding into applications and friction-less integration with .NET are key goals.

It's built with F# on top of the Dynamic Language Runtime.

Licensing has not been decided upon yet but it will be some form of [permissive free software license](http://en.wikipedia.org/wiki/Permissive_free_software_licence) for easy contribution and usage without any fuss.

## A work in progress

*This is very much a work in progress project and isn't near a usable state yet.*

* 2011-06-30<br/>
  Started work on lexer.

* 2011-07-05<br/>
  Lexer has all major functionallity and can lex entire Lua. Still some bugs that will be fixed while working on parser.<br/>
  Started work on parser.

* 2011-07-17<br/>
  Can parse entire Lua. Probably have lots of minor bugs that will be fixed when I pull in the test suites.<br/>
  Have begun reading up on DLR. Will probably take some time reading documentation of the DLR before I start working on the runtime and translation of the AST to DLR expressions.
