assert(math.abs(1.2) == 1.2)
assert(math.abs(-1.2) == 1.2)

assert(math.mod(3,5) == 3)
assert(math.mod(14,5) == 4)

assert(math.floor(1.7) == 1.0)
assert(math.floor(1.2) == 1.0)
assert(math.floor(0.0) == 0.0)
assert(math.floor(-1.2) == -2.0)
assert(math.floor(-1.7) == -2.0)

assert(math.ceil(1.7) == 2.0)
assert(math.ceil(1.2) == 2.0)
assert(math.ceil(0.0) == 0.0)
assert(math.ceil(-1.2) == -1.0)
assert(math.ceil(-1.7) == -1.0)

assert(math.min(4,8) == 4)
assert(math.min(8,4) == 4)
assert(math.min(-10,10) == -10)

assert(math.max(4,8) == 8)
assert(math.max(8,4) == 8)
assert(math.max(-10,10) == 10)

assert(math.acos(-1) == math.pi)
assert(2*math.acos(-1) == math.tau)
assert(math.rad(360) == math.tau)
assert(math.rad(180) == math.pi)
assert(math.deg(math.pi) == 180)
assert(math.deg(math.tau) == 360)

assert(math.sqrt(9) == 3)
assert(math.pow(3,2) == 9)

