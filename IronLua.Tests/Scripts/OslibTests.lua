
a = os.time({})
b = os.time({})
assert(b > a, 'Time difference was zero between calls to os.time') 

c = os.difftime(b,a)
assert(c > 0, 'Time difference was negative between calls to os.time')

assert(os.getenv("WINDIR") == [[C:\Windows]], 'os.getenv(WINDIR) was not correct')
assert(os.getenv("__does_not_exist__") == nil, 'os.getenv(__does_not_exist__) was not nil')
