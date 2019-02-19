assert(string.len("abc") == 3)
assert(string.upper("aBc") == "ABC")
assert(string.lower("XyZ") == "xyz")
assert(string.rep("abc", 3) == "abcabcabc")

assert(string.sub("abcde", 2, 4) == "bcd")
assert(string.sub("abcde", 1, 5) == "abcde")
assert(string.sub("abcde", 0, 100) == "abcde")
--assert(string.sub("abcde", 0) == "abcde")
--assert(string.sub("abcde", 1) == "abcde")
--assert(string.sub("abcde", 2) == "bcde")
--assert(string.sub("abcde", 3) == "cde")
--assert(string.sub("abcde", -2) == "de")
--assert(string.sub("abcde", -1) == "e")
assert(string.sub("abcde", 3, 2) == "")
assert(string.sub("abcde", 3, 3) == "c")
assert(string.sub("abcde", -4, -2) == "bcd")
assert(string.sub("abcde", -3, -5) == "")
assert(string.sub("abcde", -3, -3) == "c")

--assert(string.char(97, 98, 99) == "bcd")
--assert(string.char() == "")

--[[--
function assertArray(a,e)
    assert(type(a) == 'table', 'a is not a table')
    assert(type(e) == 'table', 'e is not a table')
    assert(#a == #e, 'the two arrays are not the same size')
    for i,v in ipairs(a) do 
        assert(v == e[i]) 
    end
end
assertArray({string.byte("abcde", 2, -2)}, {98,99,100})
assertArray({string.byte("abcde", 3, 2)},  {})
--]]--


