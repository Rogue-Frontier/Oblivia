# Tuple and structure assignments.
In an assignment, the lhs is a DESTINATION and the rhs is the SOURCE.

1. Evaluate the dest values and verify that they are mutable
2. Evaluate the source values. Verify that they are large enough to assign all destination values.

In a tuple assignment, the destination values must be LOCATABLE (they represent a destination) and MUTABLE. They are assigned from left to right by position. The RHS must be a sequence.

In a structural assignment, the destination values must qualify as LOCATABLE (they represent a destination), MUTABLE (this location can be assigned to), and KEYABLE (we know what name to retrieve). They are assigned from left to right by name. The RHS must have keys (tuple with implicit keys are allowed).

# Mutable values
- Setter `set { }`
- Variable


# Locatable values 
Thing that can possibly be assigned to.
- Variable name `a`
- Variable tuple `(a b c)`: assigned positionally
- Variable struct `{ a b c }`: assigned by ke
- Member access `a/b`
- Function call `a/b()`
- Alias of Locator
# Keyable values
Things for which we can implicitly generate a key
- Variable name `a = a:a`, `'a = a:'a`
- Tuple of keys `(a b c) = a:a b:b c:c`, `('a 'b 'c) = a:'a b:'b c:'c`
- Struct of keys `{ a b c } = a:a b:b c:c`, `{ 'a 'b 'c } = a:'a b:'b c:'c`
- Member access `a/b = b: a/b`, `'a/b = b: 'a/b`
- Alias of key
  - `{ 'a } = { a:'a }`
  - `{ ('a 'b 'c) } = { a:'a b:'b c:'c }`
  - `{ 'a/b } = { b: 'a/b }`
  - `{ a/'b } = { b: a/'b }`
  - `{ a/(b c) } = { b:a/b c:a/c }`
  - `{ a/('b 'c) } = { b:'a/b c:'a/c }`
- ~~Function call if it returns an alias~~ we automatically discard function returns unless the user specificially wants them
