print('testing scanner')

--debug = require "debug"

local function dostring (x) return assert(load(x))() end

--dostring("x \v\f = \t\r 'a\0a' \v\f\f")
--assert(x == 'a\0a' and string.len(x) == 3)

-- long strings
assert('a[=[b]=]c' == [==[a[=[b]=]c]==])

-- escape sequences
assert('\n\"\'\\' == [[

"'\]])

--assert(string.find("\a\b\f\n\r\t\v", "^%c%c%c%c%c%c%c$"))

-- assume ASCII just for tests:
assert("\09912" == 'c12')
assert("\99ab" == 'cab')
assert("\099" == '\99')
assert("\099\n" == 'c\10')
assert('\0\0\0alo' == '\0' .. '\0\0' .. 'alo')

--assert(010 .. 020 .. -030 == "1020-30")

-- hexadecimal escapes
assert("\x00\x05\x10\x1f\x3C\xfF\xe8" == "\0\5\16\31\60\255\232")

--local function lexstring (x, y, n)
--  local f = assert(load('return '..x..', debug.getinfo(1).currentline'))
--  local s, l = f()
--  assert(s == y and l == n)
--end

--lexstring("'abc\\z  \n   efg'", "abcefg", 2)
--lexstring("'abc\\z  \n\n\n'", "abc", 4)
--lexstring("'\\z  \n\t\f\v\n'",  "", 3)
--lexstring("[[\nalo\nalo\n\n]]", "alo\nalo\n\n", 5)
--lexstring("[[\nalo\ralo\n\n]]", "alo\nalo\n\n", 5)
--lexstring("[[\nalo\ralo\r\n]]", "alo\nalo\n", 4)
--lexstring("[[\ralo\n\ralo\r\n]]", "alo\nalo\n", 4)
--lexstring("[[alo]\n]alo]]", "alo]\n]alo", 2)

--[[ Lua 5.2 feature
assert("abc\z
        def\z
        ghi\z
       " == 'abcdefghi')
--]]

-- testing errors
--assert(not load"a = 'non-ending string")
--assert(not load"a = 'non-ending string\n'")
--assert(not load"a = '\\345'")
--assert(not load"a = [=x]")

assert(0X4P-2 == 1)
assert(0x2p-2 == 0.5)
assert(0x.8 == 0.5)

print('OK')
