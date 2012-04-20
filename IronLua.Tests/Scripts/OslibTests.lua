
a = os.time({})
b = os.time({})
assert(b > a) 

c = os.difftime(b,a)
assert(c > 0)

assert(os.getenv("WINDIR") == [[C:\Windows]])
assert(os.getenv("__does_not_exist__") == nil)
