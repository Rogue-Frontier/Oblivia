# Oblivia

Oblivia is an experimental programming language that aims to do with objects what Lisp does with lists. Where Lisp looks like list initializers, Oblivia looks like object initializers. Oblivia follows these ideas:
- **Scopes are objects.** The syntax you use to create objects in JavaScript is now the syntax you use to create blocks in Oblivia. A block with no explicit return simply returns itself as an object.
- **Terse syntax.** Common operations use fewer columns. There are no keywords other than functions and values. Defining variables is as easy as `A: B(C)` where `A` is the variable name, `B` is the type, and `C` is the initial value.

## Example
The following code implements a Conway's Game of Life and updates until the count of active cells becomes unchanging

```
{
	Life: class {
		width:int	height:int	grid: Grid-bool
		adj(n:int max:int):
			modi(lt(n 0) ?+ addi(n max) ?- n max)
		GetCell(x:int y:int): {
			x := adj(x width)
			y := adj(y height)
			^: array_get(grid [#int x y])
		}
		SetCell(x:int y:int b:bool): {
			x := adj(x width)
			y := adj(y height)
			array_set(grid [#int x y] b)
		}
		new(width:int height:int): Life {
			width := ^^width
			height := ^^height
			grid := Grid-bool/new(width height)

			debug!
		}
		debug!: {
			print*cat*["width: " width]
			print*cat*["height: " height]
		}
		update!: {
			n: 0
			range(0 width) | @(x:int) {
				range(0 height) | @(y:int) {
					left:	subi(x 1)
					up:		addi(y 1)
					right:	addi(x 1)
					down:	subi(y 1)
					c: count([#bool
						GetCell(left up)	GetCell(x up)	GetCell(right up)
						GetCell(left y)						GetCell(right y)
						GetCell(left down)	GetCell(x down)	GetCell(right down)
					] true)
					active: GetCell(x y)
					active :=
						active ?+
							(lt(c 2) ?+
								false ?-
							gt(c 3) ?+
								false ?-
								active) ?-
						eq(c 3) ?+
							true ?-
							active
					SetCell(x y active)
					n := active ?+ addi(n 1) ?- n
				}
			}
			print*cat*["active: " n]
			^: n
		}
	}
    main(args: string): int {
		life: Life/new(32 32)
		range(0 life/width) | @(x:int)
			range(0 life/height) | @(y:int)
				life/SetCell(x y randb!)
		count:1
		prevCount:0
		run: true
		run ?* { 
			prevCount := count
			count := life/update!
			run := neq(count prevCount)
		}
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
- `A := B`: reassign field A to B (same type)
- `^: A`: Set the return value to `A`
### Invoke
- `A! = A()`: call A with 0 args
- `A*B = A(B)`: call A with arg B (associative-right)
- `A-B = A(B)`: call A with arg B (associative-left)
- `A(B,C)`: call A with args B, C
- `A-B-C-D = ((A*B)*C)*D`
- `A*B*C*D = A(B(C(D)))`
- `A(B)`: If `A` is a non-generic or fully parametrized (e.g. does not accept generic arguments) type, then calling it simply casts `B` to that type.
- `A(B)`: If `A` is a generic type, then `A(B)` is the fully parametrized version of the type.
### Expression
- `A`: Get value of identifier `A` from the latest scope that defines it (current scope, then parent scope, then parent-parent scope)
- `^A`: Get value of identifier `A` from the current scope
- `^^A`: Get value of identifier `A` from the parent scope. And so on.
- `A ?+ B ?- C`: If A then B else C
- `A | B`: Map array A by function B
- `A ?* B`: While A, evaluate B
- `A/B`: From A get member named B
- `A@B`: From array A get item at index of value B
- `[A B C]`: Make an object array
- `[#type A B C]`: Make an array parametrized to the `type`
- `A { B }`: If `A` is a class, then constructs an instance of `A` and applies the statements `B` to it.
- `{ A }`: Creates an object and applies the statements `B` to it.
- `@! A`: Creates a lambda with no arguments and output `A`
- `@(A,B) C`: Creates a lambda with arguments A,B and output `C`
## Design philosophy

- We define the *noise* of an expression based on two factors:
  - Width: The number of columns needed to write
  - Ink: The amount of "ink" needed to write (e.g. `;` > `:` > `,` > `.`) 

- The complexity of a statement corresponds to the noise needed to write it.
- Whitespace is the simplest operator.
  - `A B` simply means that `A` occurs before `B` in a sequence.
  - There is *never* a statement of the form `A B` such that `A`,`B` are identifiers and `A` performs some operation on `B`, other than occurring earlier in a sequence.
- Function calls with 0/1 arguments are allowed alternate syntax to save noise
  - Calls with n>1 arguments always require enclosing delimiters `A(B C)`
  - Calls with 0, 1 arguments are allowed single-ended operators `A!`, `A*B`, `A-B`
