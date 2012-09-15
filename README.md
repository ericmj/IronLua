#Sierra IronLua

This is a heavily modified branch of IronLua by fgretief. By "heavily modified" I mean that I am not attempting to follow any coding styles or paradigms used by the original author, rather attempting to get this branch to a usable state as quickly and efficiently as possible.

##Index
**[Current State](#current-state)**

**[Helping Out](wiki/How-to-Help)**

**[Current Branch Goals](#current-branch-goals)**

**[Possible Goals](#possible-goals)**

**[Completed Goals](#completed-goals)**

**[CLR Interop Library](wiki/CLR-Interop-Library)**

**[IronLua](#ironlua)**

<a id="current-state"></a>
## Current State
The current state of the branch is somewhat usable, though there may well be bugs and exception generating code paths which I have not checked or fixed. Feel free to poke about and let me know if you find a problem and I'll try and get it fixed when I next have time in my schedule.

Currently it is possible to interact with a large portion of the CLR from within Lua, including accessing and writing values on both classes and structures. It is also possible to import CLR types directly from within Lua through the use of the clr namespace, from which they can then be instantiated. A number of bugs and niggles that were previously present in IronLua have also been ironed out, making it possible to do a lot more than was previously possible.


<a id="ironlua"></a>
# IronLua

IronLua is intended to be a full implementation of Lua targeting .NET. Allowing easy embedding into applications and friction-less integration with .NET are key goals.

It's built with C# on top of the Dynamic Language Runtime.

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

* 2011-08-09<br/>
  I have decided to rewrite the project in C#. It should be pretty straightforward to port.

* 2011-08-15<br/>
  Rewrite to C# is done. The rewrite was done for several reasons. The binary size is 4 times smaller, probably because of F#'s discriminated unions and closure's generated code among other things. Additionally tooling is alot better for C# and it is easier to reason about code performance because the IL generated is more easily mapped to C#.

* 2011-09-14<br/>
  IronLua can now generate expression trees for its entire AST. Currently working on function invokation, specifically mapping arguments to parameters. It's a quite a complex process involving type coercion and casting, expanding varargs, using parameter and type default values if not enough parameters and wrapping overflowing arguments into "params" and Varargs parameters.<br/>
  After that I will start working on all the TODO comments and get proper exception and error code everywhere. Then it's time to implement the entire Lua standard library, some parts will probably be left unimplemented like parts of the debug package and coroutines might not be implemented for the 0.1.0 release. Finally I will create the test harness and hopefully find some useable test code I can bring in. And that's pretty much it for the 0.1.0 release. Full .NET integration and proper error messages/stack traces is slated for 0.2.0 and possibly 0.3.0.