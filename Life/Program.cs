// See https://aka.ms/new-console-template for more information
using System.Collections;
using System.Text;
using Oblivia;

Console.WriteLine("Hello, World!");

/*

range(1, grid.width) | (x:int): int {
},
 */
/*

^^/result

/*
?@
*/

var code = """
{
	Life: class {
		width:int
		height:int
		grid: Grid-bool
		adj(n:int max:int):
			modi(lt(n 0) ?+ addi(n max) ?- n max)
		GetPointer(x:int y:int):
			array_at(grid [#int adj(x width) adj(y height)])	
		GetCell(x:int y:int):
			GetPointer(x y)/Get!
		SetCell(x:int y:int b:bool):
			GetPointer(x y)/Set-b
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
		activeCount: 0
		txt: StringBuilder/new!
		update!: {
			activeCount := 0
			get: GetCell

			txt/Clear!
			range(0 height) | ?(y:int) {
				range(0 width) | ?(x:int) {
					left:	subi(x 1)
					up:		addi(y 1)
					right:	addi(x 1)
					down:	subi(y 1)
					c: count([#bool
						get(left up)    get(x up)   get(right up)
						get(left y)                 get(right y)
						get(left down)  get(x down) get(right down)
					] true)
					active: bool* {
						active: GetCell(x y)
						^:
							active ?+
								(lt(c 2) ?+
									false ?-
								gt(c 3) ?+
									false ?-
									active) ?-
							eq(c 3) ?+
								true ?-
								active
					}
					SetCell(x y active)
					activeCount := active ?+ addi(activeCount 1) ?- activeCount
					str_append(txt active ?+ "*" ?- ".")
				}
				str_append(txt newline)
			}
			print*cat*["active: " activeCount]
		}
	}
    main(args: string): int {
		life: Life/new(32 32)

		print * array_at(life/grid [#int 0 0])/Get!

		range(0 life/width) | ?(x:int)
			range(0 life/height) | ?(y:int)
				life/SetCell(x y rand_bool!)
		count:1
		prevCount:0
		run: true
		Console/Clear!
		run ?* { 
			life/update!
			prevCount := count
			count := life/activeCount
			run := neq(count prevCount)

			Console/SetCursorPosition(0 0)
			print * str * life/txt
		}
    }
}
""";
var tokenizer = new Tokenizer(code);
var tokens = new List<Token> { };
while(tokenizer.Next() is { type: not TokenType.EOF } t) {
	tokens.Add(t);
}
var parser = new Parser(tokens);
var scope = parser.NextScope();
T Val<T> (T t) => t;
var global = new ValDictScope();

global.locals = new Dictionary<string, dynamic> {
	["void"] = typeof(void),
	["bool"] = typeof(bool),
	["int"] = typeof(int),
	["double"] = typeof(double),
	["string"] = typeof(string),
	["object"] = typeof(object),

	["addi"] = Val((int a, int b) => a + b),
	["subi"] = Val((int a, int b) => a - b),
	["muli"] = Val((int a, int b) => a * b),
	["divi"] = Val((int a, int b) => a / b),
	["modi"] = Val((int a, int b) => a % b),
	["xori"] = Val((int a, int b) => a ^ b),

	["addf"] = Val((double a, double b) => a + b),
	["subf"] = Val((double a, double b) => a - b),
	["mulf"] = Val((double a, double b) => a * b),
	["divf"] = Val((double a, double b) => a / b),

	["sumi"] = Val((int[] a) => a.Sum()),

	["count"] = Val((IEnumerable data, object value) => data.Cast<object>().Count(d => {
		var result = d.Equals(value);
		return result;
	})),
	["gt"] = Val((double a, double b) => a > b),
	["geq"] = Val((double a, double b) => a >= b),
	["lt"] = Val((double a, double b) => a < b),
	["leq"] = Val((double a, double b) => a <= b),
	["eq"] = Val((double a, double b) => a == b),
	["neq"] = Val((double a, double b) => a != b),

	["true"] = true,
	["false"] = false,

	["print"] = Val((object o) => Console.WriteLine(o)),
	["clear"] = Val(() => Console.Clear()),
	["set_cursor"] = Val(Console.SetCursorPosition),

	["Console"] = typeof(Console),

	["cat"] = Val((object[] o) => string.Join(null, o)),
	["range"] = Val((int a, int b) => Enumerable.Range(a, b - a).ToArray()),
	["newline"] = "\n",

	["str"] = Val((object o) => o.ToString()),

	["Array"] = Val((Type type, int dim) => type.MakeArrayType(dim)),
	["array_get"] = Val((Array a, int[] ind) => a.GetValue(ind)),
	["array_set"] = Val((Array a, int[] ind, object value) => a.SetValue(value, ind)),
	["array_at"] = Val((Array a, int[] ind) => new ValRef { src = a, index = ind }),

	["str_append"] = Val((StringBuilder sb, object o) => sb.Append(o)),
	


	["row_from"] = Val((Type t, object[] items) => {
		var result = Array.CreateInstance(t, items.Length);
		Array.Copy(items, result, items.Length);
		return result;
	}),

	["rand_bool"] = Val(() => new Random().Next(2) == 1),

	["Row"] = Val((Type type) => type.MakeArrayType(1)),
	["Grid"] = Val((Type type) => type.MakeArrayType(2)),
	["List"] = Val((Type type) => typeof(List<>).MakeGenericType(type)),
	["Dictionary"] = Val((Type key, Type val) => typeof(Dictionary<,>).MakeGenericType(key, val)),
	["StringBuilder"] = typeof(StringBuilder)
};
var result = (ValDictScope)scope.Eval(global);
var r = (result.locals["main"] as ValFunc).Call(result, [new ExprVal<string> { value = "program" }]);
return;