# Locator values 
Thing that can possibly be assigned to.
- Variable name `a`
- Variable tuple `(a b c)`
- Variable struct `{ a b c } = a:a b:b c:c`
- Member access `a/b`
- Function call `a/b()`
- Alias of Locator
# Key values
Things that can implicitly provide their own keys
- Variable name `a = a:a`
- Tuple of keys `(a b c) = a:a b:b c:c`
- Member access of key `a/b = b: a/b`
- Alias of key
  - `{ 'a } = { a:'a }`
  - `{ ('a 'b 'c) } = { a:'a b:'b c:'c }`
  - `{ 'a/b } = { b: 'a/b }`
  - `{ a/(b c) } = { b:a/b c:a/c }`
  - `{ a/('b 'c) } = { b:'a/b c:'a/c }`
- Function call if it returns an alias
