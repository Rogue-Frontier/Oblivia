`import(module("misc.obl"))`

# Pipe
1. Evaluate LHS once (generators allowed)
2. Evaluate RHS once (for repeated eval, use alias). 

`[1 2 3]|?{1:{a:_} 2:{b:_} 3:{c:_} }|combine = {a:1 b:2 c:3}`

```

fibs(10) | print

@{ memo:List.i4/ctor() }
fib(i:i4): {
  gt(memo/Length i) ?+ memo.i ?- yield.i
}
```

# Assignments: Tuple, structure, memberwise, pipe.
In an assignment, the lhs is a DESTINATION and the rhs is the SOURCE.

1. Evaluate the dest values and verify that they are mutable
2. Evaluate the source values. Verify that they are large enough to assign all destination values.
3. Equalize the dest, then embed the dest into the scope.

In a tuple assignment, the destination values must be LOCATABLE (they represent a destination) and MUTABLE. They are assigned from left to right by position. The RHS must be a sequence.

In a structural assignment, the destination values must qualify as LOCATABLE (they represent a destination), MUTABLE (this location can be assigned to), and KEYABLE (we know what name to retrieve). They are assigned from left to right by name. The RHS must have keys (tuple with implicit keys are allowed).

A memberspread assignment implicitly converts to a tuple/structural assignment.
```
w: {a:2 b:3}
x: w
%x = { %x }
%x := {a:5 b:6}
w = {a:5 b:6}
```

A pipe assignment implicitly converts to a tuple assignment.
```
w:[{a:5}, {a:6}, {a:7}, {a:8}]
w|/a := [10 11 12 13]
w = [{a:10}, {a:11}, {a:12}, {a:13}]

w|/{ a := 10 }
w = [{a:10}, {a:10}, {a:10}, {a:10}]
```

# Locatable values 
Thing that represent a destination.
- Variable name `a`
- Variable tuple `(a b c)`
- Variable struct `{ a b c }`
- Member access `a/b`
- Function call `a/b()`
- Alias of Locator
A literal `7` is NOT a locator.

# Mutable values
Things that can be assigned to
- Setter `set { }`
- Var 
- Alias of Mutable

# Keyable values
Things for which we can implicitly generate a key
- Variable name `a = a:a`, `'a = a:'a`
- Tuple of keys `(a b c) = a:a b:b c:c`, `('a 'b 'c) = a:'a b:'b c:'c`
- Struct of keys `{ a b c } = a:a b:b c:c`, `{ 'a 'b 'c } = a:'a b:'b c:'c`
- Member access `a/b = b: a/b`, `'a/b = b: 'a/b`
- Alias of keyable
  - `{ 'a } = { a:'a }`
  - `{ ('a 'b 'c) } = { a:'a b:'b c:'c }`
  - `{ 'a/b } = { b: 'a/b }`
  - `{ a/'b } = { b: a/'b }`
  - `{ a/(b c) } = { b:a/b c:a/c }`
  - `{ a/('b 'c) } = { b:'a/b c:'a/c }`
- ~~Function call if it returns an alias~~ we automatically discard function returns unless the user specificially wants them
