# Notes on Syntax
Any code snippets presented are to be fully supported by Oblivia.

# Modules
- You can import an entire module or parts of it
- `module(path)` gets the module object.
- `import(module)` embeds the object into the current scope.
```
# From misc, import bar as foo
import*module("misc.obl")/{ foo:bar }
```

# Control flow

- Labeled break
- Labeled continue
- Labeled defer

- Recursive return
- Recursive yield
- Labeled goto

# Operators
```
# arithmetic overload
\+(i:i4):{}
\-(i:i4):{}
\*(i:i4):{}
\/(i:i4):{}
\+\+(i:i4):{}
\-\-(i:i4):{}
\*\*(i:i4):{}
\/\/(i:i4):{}

# implicit cast to other
_impl(d:out*f4):{}
_impl(i:out*i4):{}

# implicit cast from other
_impl(i:i4): {}
_impl(f:f4): {}

# explicit cast to other
_expl(i:out*i4): {}
_expl(f:out*f4): {}

# explicit cast from other
_expl(i:i4): {}
_expl(f:f4): {}
```

# `out` parameter
- Returned at end of function
- Overrides result of body
- Declare in block to designate return value

# Var and Type
- Initializing a key with a type makes a mutable var with the type.
- Initializing a key with a value makes a mutable var with the value's type.
- To store a type itself in a var, call `val(type)`
- `val(a)` makes an immutable variable with the value's type.
- `auto` declares a variable that will be assigned later (possibly in an inner scope)
```
a:int = a:var(int)
a:foo = a:var(typeof(foo) 5)
a:val(int) = a:int
```
- `var` converts a type into a typed var.
- Functions can return `var` objects which are used to create keys.

- DEFINE op `:` sets a key that does not exist
- ASSIGN op `:=` sets a key that already exists. 
```
d:Dict(string var)
d."foo": int
```

# Match
- class/trait names match any object that implements them
- `gt(a), geq(a), lt(a), leq(a)` are objects that match numbers
- `all(a b c)` matches object that match ALL at once.
- `any(a b c)` matches object that match any of them.
- `one(a b c)` matches object that match exactly one.
- `eq(a)` match only things that exactly equal `a`

```
# Creates a match object. Can be converted to a dictionary
foo: ?{ 1:print 2:print }
```

# Pipe
1. Evaluate LHS once (generators allowed)
2. Evaluate RHS once (for repeated eval, use alias). 

- If the RHS calls `loop/ret`, then the result is the last item
- If the RHS calls `loop/yield`, then the result is all yielded items
- `_map/break`, `_map/ret`, `_map/continue`

- `combine` summons a callable object that returns the combined object.

Sequence to object:
```
[1 2 3]|?{1:{a:_} 2:{b:_} 3:{c:_} }|combine = {a:1 b:2 c:3}
```

Filter sequence
```
[1 2 3] ?| ?(a) a = 3
[1 2 3] ?|= 3
```

# Function locals
State variables that are only used by one function can be placed in a scope only known by that function. The defined function is then converted to an object.

```
fibs(10) | print

@{ memo:List.i4/ctor() }
fib(i:i4): {
  gt(memo/Length i) ?+ memo.i ?- yield * fib.i
}
fibs(i:i4): seq.i4 {
  yield *| range(i) | fib
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
```

```
w|/{ a := 10 }
w = [{a:10}, {a:10}, {a:10}, {a:10}]
```

## Binding and default
```
{ foo -> int: bar := 5 } := thing
```

# Locatable values 
Things that represent a location.
- Variable name `a`
- Variable tuple `(a b c)`
- Variable struct `{ a b c }`
- Member access `a/b`
- Function call `a/b()`
- Alias of Locator

A literal such as `7` is not a locator.

# Mutable values
Things that can be assigned to
- Setter `set { }`
- Var 
- Alias of Mutable

# Keyable values
Things for which we can implicitly generate a key
- Variable name `{a} = {a:a}`, `{'a} = {a:'a}`
- Tuple of keys `{(a b c)} = {a:a b:b c:c}`, `{('a 'b 'c)} = {a:'a b:'b c:'c}` (maybe require spread?)
- Struct of keys `{{a b c}} = {a:a b:b c:c}`, `{{'a 'b 'c}} = {a:'a b:'b c:'c}` (maybe require spread?)
- Member access `{a/b} = {b:a/b}`, `{'a/b} = {b:'a/b}`
- Alias of keyable
  - `{'a} = {a:'a}`
  - `{('a 'b 'c)} = {a:'a b:'b c:'c}`
  - `{'a/b} = {b:'a/b}`
  - `{a/'b} = {b:a/'b}`
  - `{a/(b c)} = {b:a/b c:a/c}`
  - `{a/('b 'c)} = {b:'a/b c:'a/c}`
- ~~Function call if it returns an alias~~ we automatically discard function returns unless the user specificially wants them
