# Oblivia
Oblivia is an experimental programming language that aims to do with objects what Lisp does with lists. Where Lisp looks like list initializers, Oblivia looks like object initializers. Oblivia follows these ideas:
- **Scopes are objects.** The syntax you use to create objects in JavaScript is now the syntax you use to create blocks in Oblivia. A block with no explicit return simply returns itself as an object (if it has keys) or the last expression (if there are no keys).
- **Hyper-terse syntax.** The most frequent operations (for some context) use fewer columns. There are no keywords other than functions and values. Casting values is as easy as `A(B)` with type `A` and value `B`. Defining variables is simply `A:B(C)` with variable name `A`, type `B`, and value `C`.

## Example
The following code implements a Conway's Game of Life and updates until the count of active cells becomes unchanging

```
{
    Life:class {
        width:int height:int grid:Grid.bool
        adj(n:int max:int): modi(lt(n 0) ?+ addi(n max) ?- n max)
        at(x:int y:int): array_at(grid adj.|[(x width),(y height)] |:int ?(i:int) i)    
        get(x:int y:int): at(x y)/Get!
        set(x:int y:int b:bool): at(x y)/Set.b
        new(width:int height:int): Life {
            (width height) := ^^(width height)
            grid := Grid.bool/ctor(width height)
            debug!
        }
        debug!: {
            print*cat["width: " width]
            print*cat["height: " height]
        }
        activeCount:0
        txt: StringBuilder/ctor!
        update!: {
            activeCount := 0
            g: get
            txt/Clear!
            range(0 height) | ?(y:int) {
                range(0 width) | ?(x:int) {
                    w:subi(x 1) n:addi(y 1) e:addi(x 1) s:subi(y 1)
                    c: count(g.|[
                        (w n),(x n),(e n),
                        (w y),      (e y),
                        (w s),(x s),(e s),
                    ] true)
                    active:g(x y)
                    active:= _ ?+ {
                        lt(c 2) ?+ false ?-
                        gt(c 3) ?+ false ?- _
                    } ?- {
                        eq(c 3) ?+ true ?- _
                    }
                    set(x y active)
                    activeCount := active ?+ addi(_ 1) ?- _
                    str_append(txt active ?+ "+" ?- "-")
                }
                str_append(txt newline)
            }
            print*cat["active: " activeCount]
        }
    }
    main(args:string): int* {
        life:Life/new(32 32)
        print*array_at(life/grid, [:int 0 0])/Get!
        range(0 life/width) | ?(x:int) range(0 life/height) | ?(y:int)
            life/set(x y rand_bool!)
        count:1 prevCount:0 run:true
        Console/Clear!
        run ?% { 
            life/update!
            prevCount := count
            count := life/activeCount
            run := neq(count prevCount)
            Console/SetCursorPosition(0 0)
            print*str*life/txt
        }
        ^: 0
    }
}
```
## Syntax
There are no arithmetic operators. See global function table for arithmetic functions.

### Define
- `A:B`: field A has value B. If `B` is a type, then the value is a *placeholder*
- `A!:B`: function A with no args has output B
- `A(B, C): D`: function A with args B,C has output D
### Statement
- `A := B`: reassign field A to B (same type). You can use `_` for the current value of `A`
- `^: A`: Return `A` from the current scope.
- `^^: A`: Return `A` from the parent scope.
- `^^^: A`: Return `A` from the parent's parent scope.
### Invoke
- `A! = A()`: call A with 0 args
- `A*B = A(B)`: call A with arg B (associative-right)
- `A.B = A(B)`: call A with arg B (associative-left)
- `A(B, C)`: call A with args B, C
- `A.B.C.D = ((A*B)*C)*D = ((A(B))(C))(D)`
- `A*B*C*D = A(B(C(D)))`
- `A(B)`: If `A` is a generic type, then `A(B)` is the fully parametrized version of the type. If `A` is a non-generic or fully parametrized (e.g. does not accept generic arguments) type, then calling it simply casts `B` to that type.
### Expression
- `A`: Get value of identifier `A` from the latest scope that defines it (current scope, then parent scope, then parent-parent scope)
- `^A`: Get value of symbol `A` from the current scope
- `^^A`: Get value of symbol `A` from the parent scope.
- `^^^A`: Get value of symbol `A` from parent's parent scope.
- `'A`: Alias of expression `A`. Assignments on variable `B:'A` will attempt to assign to `A`.
- `A ?+ B ?- C`: If A then B else C.
- `A | B`: Map array A by function B.
- `A |/ B`: From every item in `A` get value of symbol `B`.
- `A |* B`: From every item in `A` call with arg `B`
- `A ?% B`: While A, evaluate B.
- `A/B`: In the scope of *expression* `A` evaluate *expression* B. Cannot access outer scopes.
- `A/{B}`: In the scope of *expression* `A`, evaluate statements `B`. Can access outer scopes.
- `A/ctor`: From .NET type `A` get the unnamed constructor.
- `A*B`: Call `A` with arg *expression* `B` (automatic spread)
- `A.B`: Call `A` with arg *term* `B` (no spread)
- `A *| B`: Call `A` with every item from *expression* `B` (automatic spread)
- `A .| B`: Call `A` with every item from *term* `B` (no spread)
- `A@B`: From array `A` get item at index *term* `B`
- `A[B]`: From array `A` get item at index `B`
- `A@|B`: From array `A` get item at each index in `B`
- `[A B C]`: Make an object array
- `[:type A B C]`: Make an array of `type`
- `A { B }`: Initializer block
  - If `A` is a class, then constructs an instance of `A` and applies the statements `B` to it.
- `{ A }`: Creates an scope and applies the statements `A` to it. If the scope has no locals or returns, then the scope returns the result of the last statement (empty if no statements). Otherwise returns an object.
- `?!: A`: Creates a lambda with no arguments and output `A`
- `?(A,B): C`: Creates a lambda with arguments A,B and output `C`
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
}`: Match expression (naive): For each pair `B:C`, if `A = B`, then returns `C`. TODO: Allow lambda branches.
- `A =+ B`: Returns true if `A` equals `B`
- `A =- B`: Returns true if `A` does not equal `B`
- `A = B`: Returns true if `A` matches pattern `B`
- `A = B:C`: Returns true if `A` matches pattern `B` and assigns the value to `C`

## Design philosophy
- Whitespace is the simplest operator.
  - `A B` simply means that `A` occurs before `B` in a sequence.
  - There is *never* a statement of the form `A B` such that `A`,`B` are identifiers and `A` performs some operation on `B`, other than occurring earlier in a sequence.
- Function calls with 0/1 arguments are allowed alternate syntax to save pixels
  - Calls with n>1 arguments always require enclosing delimiters `A(B C)`
  - Calls with 0, 1 arguments are allowed single-ended operators `A!`, `A*B`, `A.B`
