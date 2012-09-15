print("Testing IronLua's CLR implementation")

system = {}

system.convert = clr.import('System.Convert')
assert(system.convert,'Failed to import System.Convert')

system.int = clr.import('System.Int32')
assert(system.int,'Failed to import System.Int32')

system.short = clr.import('System.Int16')
assert(system.short,'Failed to import System.Int16')

print("Attempting: Convert.ToInt32(12)")
num1 = system.convert.ToInt32(12)
assert(tostring(num1) == '12',"Failed to convert double to Int32")
print "    Success"

print("Attempting to create an array: Int32[10]")
array = clr.makearray(system.int, 10)
assert(array,'Failed to create array')
assert(#array == 10,'Array length was not correct')
array[0]=10