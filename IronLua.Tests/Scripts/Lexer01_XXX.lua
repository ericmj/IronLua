print(1._2e3)
--XX malformed number near '1._2e3'
print(1.2e_3)
--XX malformed number near '1.2e_3'
print(.e)
--:: unexpected symbol near '.'
print(.1c)
--XX malformed number near '.1c'
print(0.3e5b5)
--XX malformed number near '0.3e5b5'
