print("_VERSION ", _VERSION)
assert(_VERSION == 'Lua 5.1')

-- type
assert(type(0.0) == "number")
assert(type(nil) == "nil")
assert(type(false) == 'boolean')
assert(type(true) == 'boolean')
assert(type("abc") == 'string')
function f(a) print(a) end
assert(type(f) == 'function')
assert(type({}) == 'table')
t = { 1, 2, 3 }
assert(type(t) == 'table')
assert(type(_G) == 'table')

-- tonumber
assert(tonumber("1.25") == 1.25)

-- tostring
assert(tostring(1.25) == "1.25")

-- pairs
t = { a = 1, b = 2, c = 3 }
for k,v in pairs(t) do print(k,v) end

-- ipairs
t = { 4, 5, 6 }
for i,v in ipairs(t) do print(i,v) end
