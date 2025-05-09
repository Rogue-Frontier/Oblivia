# Oblivia: Object-Building Language Intended for Very Idiosyncratic Articulations
Oblivia (OBL) is an esolang that aims to do with objects what Lisp does with lists. Oblivia follows these ideas:
- **Terse syntax**:
  - A small set of primitive operations are given the most terse syntax.
  - Casting values is as easy as `A(B)` with type `A` and value `B`.
  - Defining variables is simply `A:B(C)` with variable name `A`, type `B`, and value `C`.
- **Scopes = Objects**: In Oblivia, scopes have the power of objects.
  - Pass a scope to a function.
  - Name a variable or a member and it automatically becomes a key of the scope.
  - A scope with no explicit return simply returns itself as an object (if it has keys) or the last expression (no keys).
- **Objects = Functions**: In Oblivia, any object can define the `(), [], {}` operators.
- **Variables of any name**: Variable names are not restricted by those already defined in the outer scope. To access variables of an outer scope, Oblivia introduces the `^` "up" function
- **Same syntax everywhere**:
  - `:` is the define operator
  - `A(B), A[B], A{C}` is a function call.
  - Patterns can be stored in variables: `Some$(val)`
- **Classes are objects too**: Classes can have a static `companion` (or not) that implements interfaces and behaves just like regular singleton objects.
- **Term and Expression scoping**: The right hand side of an operator has either term scope or expression scope. A term is a single item and an expression is a sequence of terms connected by operators. A term is one of the following.
  - A name
  - A number
  - A string
  - A tuple
  - An array
  - An object
  - A monadic function and its operands

## Inspiration

- JavaScript: Destructuring
- CSharp: Tuples
- APL: short-left, long-right precedence

## Example
The following code implements a Conway's Game of Life and updates until the count of active cells becomes unchanging

```
{
    print:Console/WriteLine
    Life:class {
        #@dbg["width:" w]
        w->i4
        #@dbg["height:" h]
        h->i4
        grid->Grid(bit)
        mod(n:i4 max:i4):(%: (<: n 0) ?+ (+:n max) ?- n max)
        # xy: (x:i4 y:i4)
        # %xy
        at(x:i4 y:i4):grid(mod.|[x:w y:h]|i4)
        get(x:i4 y:i4):at(x y)/Get()
        set(x:i4 y:i4 b:bit):at(x y)/Set(b)
        new(w:i4 h:i4): Life {
            print*cat["args: " _0 ", " _1],
            (w h) := ^^/(w h),
            grid := Grid(bit)/ctor(w h)
            debug()
        }
        debug(): print*|cat*|[["width: " w],["height: " h]]
        activeNum:0
        txt:StrBuild/ctor()
        update(): {
            activeNum := 0
            g:get
            txt/Clear()
            ta: txt/Append,
            ɩh | ?(y:i4){
                ɩw | ?(x:i4){
                    w:(-:x 1) n:(+:y 1) e:(+:x 1) s:(-:y 1)
                    c:count(g.|[w:n x:n e:n w:y e:y w:s x:s e:s] ⊤)
                    active: g(x y) ?+ not((<:c 2)∨(>:c 3)) ?- eq(c 3)
                    set(x y active)
                    active ?+ { activeNum := (+:_ 1) }
                    ta(active ?+ "*" ?- " ")
                }
                ta(newline)
            }
            print*cat["active: " activeNum]
        }
    }
    main(args:str) -> i4: {
        life:Life/new(32 32)
        print*life/grid[:i4 0 0]/Get(),
        ɩ(life/w) | ?(x:i4)ɩ(life/h) | ?(y:i4) life/set(x y rand_bool())
        num:1 prevNum:0 run:⊤ i:1
        Console/Clear()
        run ?++ {
            life/update()
            prevNum := num
            num := life/activeNum
            run := neq(num prevNum)
            print*cat["time: " i]
            Console/{
                SetCursorPosition(0 0)
                Write*life/txt/ToString()
            }
        }
        ^: 0
    }
}
```
## Syntax
Oblivia has 3 basic structures.
- Array: Contains a sequence of items and nothing more.
- Tuple: Contains a sequence of items, some with string keys.
- Block: Contains a set of variables with string keys. Supports operations like `ret`

Oblivia has these basic data types:
- `bit`: Boolean, `yes` and `no`
- `i8`: 8 byte signed integer
- `f8`: 8 byte signed float
### Arithmetic
Infix arithmetic is available for common operations:
- `A + B`
- `A - B`
- `A × B`
- `A ÷ B`
- `A > B`
- `A < B`

Lisp-like arithmetic allows you to spread operands. Operators are converted to reductions e.g. `[+: a b c] = a/\+(b)/\+(c) = reduce([a b c] ?(a b) a/\+(b))`
- `[+: a b]`
- `[-: a b]`
- `[*: a b]`
- `[**: a b]`
- `[/: a b]`
- `[//: a b]`
- `[^: a b]`
- `[%: a b]`
- `[=: a b]`
- `[>: a b]`
- `[<: a b]`
- `[~: a b]`
- `[>>: a b]`
- `[<<: a b]`
- `[&: a b]`
- `[|: a b]`
- `[&&: a b]`
- `[||: a b]`
### Define
- `A:B`: field A has value B. If `B` is a type, then the value is a *placeholder*
- `A -> B`: Declare field A with type B
- `A() -> B`: Declare method A has type B`
- `A -> B: C`:
- `A() -> B: C`:
- `A!:B`: function A with no args has output B
- `A(B, C): D`: function A with args B,C has output D
- `A[B C]: D`:
- `A{B C}: D`:
### Assign
- `A := B`: reassign field A to B (same type). You can use `_` for the current value of `A`
- `^: A`: Return `A` from the current scope.
- `^^: A`: Return `A` from the parent scope.
- `^^^: A`: Return `A` from the parent's parent scope.
### Invoke
- `A! = A()`: call A with 0 args
- `A*B = A(B)`: call A with arg B (associative-right)
- `A.B = A(B)`: call A with arg B (associative-left)
- `A(B C)`: call A with args B, C
- `A.B.C.D = ((A*B)*C)*D = ((A(B))(C))(D)`
- `A*B*C*D = A(B(C(D)))`
- `A(B)`: If `A` is a generic type, then `A(B)` is the fully parametrized version of the type. If `A` is a non-generic or fully parametrized (e.g. does not accept generic arguments) type, then calling it simply casts `B` to that type.
### Expression
- `A`: Get value of identifier `A` from the latest scope that defines it (current scope, then parent scope, then parent-parent scope)
- `^A`: Get value of symbol `A` from the current scope
- `^^A`: Get value of symbol `A` from the parent scope.
- `^^^A`: Get value of symbol `A` from parent's parent scope.
- `'A`: Alias of expression `A`. Assignments on variable `B:'A` will attempt to assign to `A`.
- `[A B C]`: Make an object array
- `[A:B C:D] = [(A B), (C D)]`
- `[:type A B C]`: Make an array of `type`
- `{ A }`: Creates an scope and applies the statements `A` to it. If the scope has no locals or returns, then the scope returns the result of the last statement (empty if no statements). Otherwise returns an object.
- `A ?+ B ?- C`: If A then B else C.
- `A ?++ B ?+- C ?-- D`: While A, evaluate B. If at least one iter, eval `C`. If no iter, eval `D`
- `A(B)`
- `A[B]`: `A([B])`
- `A{B}`: `A({B})` Call `A` with the result of `{B}`
  - If `A` is a class, then constructs an instance of `A` and applies the statements `B` to it.
- `A-B`: Range from A to B
- `A->B`: Function type
- `A..B`: Eval A, assign to _, then eval B
- `A.B`: `A(B)` Call `A` with arg *term* `B` (no spread)
- `A*B`: `A(B)` Call `A` with arg *expression* `B` (spread if tuple)
- `A/B`: In the scope of *expression* `A` evaluate *expression* B. Cannot access outer scopes.
- `A/{B}`: In the scope of *expression* `A`, evaluate statements `B`. Can access outer scopes.
- `A/ctor`: From .NET type `A` get the unnamed constructor.
- `A|B`: Map array A by function B.
- `A?|B`: Filter A by B
- `?(): A`: Creates a lambda with no arguments and output `A`
- `?(A): B`: Creates a lambda with arguments `A` and output `B`
- `A.|B`: `B|A` Call `A` with every item from *term* `B` (no spread)
- `A*|B`: `B|A` Call `A` with every item from *expression* `B` (spread if tuple)
- `A/|B`: `?(C) B(A C)`
- `A/|B(C)`: `B(A C)`
- `A|.B`: `A|?(a):a(B)` From every item in `A` call with arg term `B`
- `A|*B`: `A|?(a):a(B)` From every item in `A` call with arg expression `B`
- `A|/B`: `A | ?(a) a/B` From every item in `A` get value of symbol `B`.
- `A||B`: `?(C) A | ?(a) B(a C)`
- `A||B(C)`: `A | ?(a) B(a C)`
- `A ?[
    B0:C0
    B1:C1
    B2:C2
    B3:C3
]`: Conditional sequence; for each pair `B:C`, if `A(B)` is `true` then include `C` in the result.
- `A ?{
    B0:C0
    B1:C1
    B2:C2
    B3:C3
    D0
    D1
    D3
}`: Match expression (naive): For each pair `B:C`, if `A = B`, then returns `C`. Can also accept lambda `D`
- `?{ A0:B0 }`: Matcher function
- `A =+ B`: Returns true if `A` equals `B`
- `A =- B`: Returns true if `A` does not equal `B`
- `A = B`: Returns true if `A` matches pattern `B`
- `A = B:C`: Returns true if `A` matches pattern `B` and assigns the value to `C`
- `A =: B`: if `A = typeof(B)`, then sets `A := B` and returns true
### Pattern
- `$(A:B)`
- `$[A B C]`: Array
- `$[A:B C:D]`: 
- `$[A=B C=D]`: 
- `${ A = B }`: Object member `A` of type `B`
- `${ A = B:C }`: Object member `A` of type `B`; make local `C:B(A)`
- `${ A:B }`: Object member `A` of type `B`; define `A:B`
### Constants
- `yes`: True
- `no`: False
- `⟙`: True
- `⟘`: False
- `∅`: Empty
- `empty`: Automatically removed when added to a tuple or array
### Monadic functions
- `↕A`: Returns `[0 1 2 ... A]`
- `⍋A`: Returns `[n n+1 n+2 ... m]` where `[A(n) A(n+1) A(n+2) ... A(m)] = sorted(A)`
- `⍒A`: Returns `[n n+1 n+2 ... m]` where `[A(m) ... A(n+2) A(n+1) A(n)] = sorted(A)`
- `⌈A`: `ceil(A)`
- `⌊A`: `floor(A)`
- `A⋖`: First element of `A`
- `A⋗`: Last element of `A`
- `A⌗`: Returns length of `A`
- `⚄A`: Returns a random item from `[0 1 2 ... A]`
- `⌨A`: Returns the value of the first char of `A`
- `¬A`: Returns `not(A)`
### Dyadic functions
- `A⌗B`: Returns count of `B` in `A`
- `A∀B`: Returns `A ?| B⌗ = A⌗`
- `A∃B`: Returns `A ?| B⌗ > 0`
- `A∄B`: Returns `A |? B⌗ = 0`
- `A⫷B`: Constructs class `A` with data `B`
- `A∨B`: Returns `[||:A B]`
- `A∧B`: Returns `[&&:A B]`
- `A⋃B`: Returns `any(A B)`
- `A⋂B`: Returns `all(A B)`
- `A≤B`:
- `A≥B`:
## Design philosophy
- Whitespace is the simplest operator.
  - Two adjacent identifiers `A B` simply means that `A` occurs before `B` in a sequence. Identifiers are never grouped together outside of tuples and arrays. `{ A B:C D } = { A:A, B:C, D:D }`, `{ (A B):(C D) } = { A:C, B:D }` 
  - There is *never* a statement of the form `A B` such that `A`,`B` are identifiers and `A` performs some operation on `B`, other than occurring earlier in a sequence.
- Function calls with 0/1 arguments are allowed alternate syntax to save pixels
  - Calls with n>1 arguments always require enclosing delimiters `A(B C)`
  - Calls with 0, 1 arguments are allowed single-ended operators `A!`, `A*B`, `A.B`

## Rejected features.
OBL emphasizes **generality** and **terseness**, rejecting features that are not versatile enough to justify the syntax cost.
- Partial application / *whatever-priming* (Raku): Scope-constrained to simple expressions. Cannot control argument order.
  - `?(<par>) <expr>` Lambdas solve this problem by forward declaring arguments and allowing any size scope.
- `=`-based assignment: This function is often confused between assignments and boolean comparisons.
  - OBL uses `:=`, where `:` makes it clear that this function is strictly assignment. `=` is a pattern match function.
