t = {}

assert(type(t) == "table")

t = { 1 }
t = { "a" }

t = { 1, 2, 3 }
--[[--
assert(t[1] == 1)
assert(t[2] == 2)
assert(t[3] == 3)
assert(#t == 3)
--]]--


t = { "a", "b", "c" }
--[[--
assert(t[1] == "a")
assert(t[2] == "b")
assert(t[3] == "c")
assert(#t == 3)
--]]--
                   
--[[--
t = { [a] = 1, [b] = 2, [c] = 3 }
assert(t.a == 1)
assert(t.b == 2)
assert(t.c == 3)
assert(#t == 3)
--]]--

-- end of file --
