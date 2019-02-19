print(1.2e-3)
--:: 0.0012
print(0x2e-2)
--XX malformed number near '0x2e-2'
print(0x2p-2)
--:: 0.5
print(1._2e3)
--XX malformed number near '1._2e3'
print(1.2e_3)
--XX malformed number near '1.2e_3'
print(.e)
--XX unexpected symbol near '.'
print(.1c)
--XX malformed number near '.1c'
print(0.3e5b5)
--XX malformed number near '0.3e5b5'
