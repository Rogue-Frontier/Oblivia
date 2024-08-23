// See https://aka.ms/new-console-template for more information
using System.Collections;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;


/*

range(1, grid.width) | (x:int): int {
},
 */
/*
# Design philosophy
- `A:B` -> field A has value B
- `A!:B` -> function A has output B
- `A(B): C` -> function A with args B has output C

- Noise
  - Ink: The amount of "ink" needed to write
  - Width: The number of columns needed to write
  - A statement with more width has more noise than a statement with less width.
  - Out of definition, reassignment, and conditionals, reassignment is the most verbose and demands the most attention.
- The complexity of a statement corresponds to the noise needed to write it.
  - Whitespace is the simplest operator, such that `A B` means that A comes before B

- Function calls with 0, 1, and n>1 arguments should be allowed alternative syntax
  - Calls with n>1 arguments always require enclosing delimiters
  - Calls with 0, 1 arguments are allowed single-ended operators
  - `A!`: call A with 0 args
  - `A*B`: call A with arg B (associative-right)
  - `A-B`: call A with arg B (associative-left)
  - `A(B,C)`: call A with args B, C
  - `A-B-C-D = ((A*B)*C)*D`
  - `A*B*C*D = A(B(C(D)))`

`A ?+ B ?- C`: If A then B else C

`@! B`: lambda with no args
`@(a) B`: lambda with args

`A/B`: From A get member named B
`A@B`: From array A get item at index of value B

`A | B`: Map array A by function B
`^: A`: Return A and exit scope
`A ?* B`: While A, do B

^^/result

/*
?@

*/


//fix instance methods
var code = """
{

	Point: class {
		x: int
		y: int
		new(x:int y:int): Point { ^x := ^^x, ^y := ^^y }
		debug!: {
			print-x
			print-y
		}
	}
    main(args: string): int {
		origin: Point/new(3, 5)
		print*origin/x print*origin/y
		origin/debug!
		
		fib: {
			table: List-int/new!
			calc(a:int):
				lt(a table/Count) ?+ table@a ?- {
					result:
						gr(a 1) ?+ addi(fib*subi(a 1) fib*subi(a 2)) ?-
						eq(a 1) ?+ 1 ?-
						0
					table/Add-result
					^: result
				}
			^: calc
		}
		range(0 10) | @(a:int) {
			print*fib*a
		}
		^: 0
    }
}
""";

var tokenizer = new Tokenizer(code);
var tokens = new List<Token> { };
while(tokenizer.Next() is { type: not TokenType.EOF} t) {
    tokens.Add(t);
}
var parser = new Parser(tokens);
var scope = parser.NextScope();
var global = new Scope();


T Val <T>(T t) => t;
global.locals["int"] = typeof(int);
global.locals["string"] = typeof(string);
global.locals["bool"] = typeof(bool);
global.locals["void"] = typeof(void);
global.locals["double"] = typeof(double);

global.locals["print"] = Val((object o) => Console.WriteLine(o));
global.locals["range"] = Val((int a, int b) => Enumerable.Range(a, b - a).ToArray());

global.locals["gr"] = Val((double a, double b) => a > b);
global.locals["geq"] = Val((double a, double b) => a >= b);
global.locals["lt"] = Val((double a, double b) => a < b);
global.locals["leq"] = Val((double a, double b) => a <= b);
global.locals["eq"] = Val((double a, double b) => a == b);

global.locals["addi"] = Val((int a, int b) =>	a + b);
global.locals["subi"] = Val((int a, int b) =>	a - b);
global.locals["muli"] = Val((int a, int b) =>	a * b);
global.locals["divi"] = Val((int a, int b) =>	a / b);
global.locals["modi"] = Val((int a, int b) =>	a % b);

global.locals["add"] = Val((double a, double b) => a + b);
global.locals["sub"] = Val((double a, double b) => a - b);
global.locals["mul"] = Val((double a, double b) => a * b);
global.locals["div"] = Val((double a, double b) => a / b);

global.locals["array"] = Val((Type type, int len) => Array.CreateInstance(type, len));



global.locals["List"] = Val((Type type) => typeof(List<>).MakeGenericType(type));
global.locals["true"] = true;
global.locals["false"] = false;
var result = (Scope)scope.Eval(global);
var r  = (result.locals["main"] as ValFunc).Call(result, [new ExprVal<string> { value= "program" }]);
return;
public class ValError {

	public string msg;
	public ValError () { }
	public ValError (string msg) {
		this.msg = msg;
	}
	public ValError FunctionNotFound (string msg) => new($"Function unknown: {msg}");
	public ValError TypeMismatch (string msg) => new($"Type mismatch: {msg}");
	public ValError VariableNotFound(string msg) => new($"variable unknown: {msg}");
	public static readonly ValError FUNCTION_NOT_FOUND = new("Function unknown");
	public static readonly ValError CONSTRUCTOR_NOT_FOUND = new("Constructor unknown");
	public static readonly ValError TYPE_MISMATCH = new("Type mismatch");
	public static readonly ValError VARIABLE_NOT_FOUND = new("Variable unknown");
	public static readonly ValError TYPE_NOT_FOUND = new("Type unknown");
	public static readonly ValError TYPE_EXPECTED = new("Type expected");
	public static readonly ValError SEQUENCE_EXPECTED = new("Sequence expected");
	public static readonly ValError FUNCTION_EXPECTED = new("Function expected");
	public static readonly ValError OBJECT_EXPECTED = new("Object expected");
	public static readonly ValError BOOLEAN_EXPECTED = new("Boolean expected");
}

public record ValConstructor (Type t) { }
public record ValMethod (object src, MethodInfo m) { }


public record ValReturn(dynamic data) {

}
public class ValEmpty {
	public static readonly ValEmpty VALUE = new();
}
public class ValDeclared {
	public Type type;
}
public record ValFunc {
	public INode expr;
	public List<StmtKeyVal> pars;
	public Scope owner;

	//TODO: Make sure instance functions point to the instance and not the class

	public dynamic Call(Scope ctx, List<INode> args) {
		var frame = new Scope(owner, false);
		var argList = new List<dynamic>();
		var argDict = new Dictionary<string, dynamic>();
		frame.locals["argList"] = argList;
		frame.locals["argDict"] = argDict;
		foreach(var p in pars) {
			var val = p.value.Eval(ctx);
			StmtKeyVal.Define(frame, p.key, val);
		}
		int ind = 0;
		foreach(var arg in args) {
			if(arg is StmtKeyVal kv) {
				ind = -1;

				var val = StmtAssign.Assign(frame, kv.key, 1, () => kv.value.Eval(ctx));
			} else if(ind > -1) {
				var p = pars[ind];
				var val = StmtAssign.Assign(frame, p.key, 1, () => arg.Eval(ctx));
				ind += 1;
			}
		}

		foreach(var p in pars) {
			var val = frame.locals[p.key];
			argList.Add(val);
			argDict[p.key] = val;
		}
		var result = expr.Eval(frame);
		return result;
	}
}
public record ValType(Type type) {
	public dynamic Cast(object data) {
		if(type == typeof(void)) {
			return ValEmpty.VALUE;
		}
		if(data.GetType() != type) {
			return ValError.TYPE_MISMATCH; 
		}
		return data;

	}
}
public record ValInterface {

}
public class ValClass {
	public Scope obj;

	public Scope src_ctx;
	public INode src;



	public dynamic MakeInstance () {
		return src.Eval(src_ctx);
	}
}
public record Scope {
	public bool temp;
	public Scope parent = null;
	public Scope (Scope parent = null, bool temp = false) {
		this.temp = temp;
		this.parent = parent;
	}
	public Dictionary<string, dynamic> locals = [];
	public dynamic Get(string key, int up = -1) =>
		up == -1 ? GetAll(key) : GetAt(key, up);
	public dynamic GetAt(string key, int up) {
		if(up == 1) {
			if(locals.TryGetValue(key, out var v))
				return v;
			else if(!temp)
				return ValError.VARIABLE_NOT_FOUND;
		}
		return
			parent != null ? parent.GetAt(key, temp ? up : up - 1) :
			ValError.VARIABLE_NOT_FOUND;
	}
	private dynamic GetAll (string key) =>
			locals.TryGetValue(key, out var v) ? v :
			parent != null ? parent.GetAll(key) :
			ValError.VARIABLE_NOT_FOUND;
	public dynamic Set (string key, object val, int up = -1) => up == -1 ? SetLast(key, val) : SetAt(key, val, up);
	public dynamic SetAt(string key, object val, int up) {
		if(temp) {
			return parent.SetAt(key, val, up);
		}
		if(up == 1) {
			return locals[key] = val;
		} 
		if(parent != null) {
			parent.SetAt(key, val, up - 1);
		}
		return ValError.VARIABLE_NOT_FOUND;
	}
	public dynamic SetLast (string key, object val) =>
		locals.TryGetValue(key, out var v) ? locals[key] = val :
		parent != null ? parent.SetLast(key, val) :
		ValError.VARIABLE_NOT_FOUND;


}

class Parser {
    int index;
    List<Token> tokens;

    public Parser(List<Token> tokens) {
        this.tokens = tokens;
    }

	void inc () => index++;
	void dec() => index--;

    public Token currToken => tokens[index];
    public TokenType tokenType => currToken.type;
	public INode NextExpression () {
		var lhs = NextTerm();
		return NextExpression(lhs);
	}
	public INode NextExpression(INode lhs) {
		var t = tokenType;
		if(t == TokenType.PIPE) {
			inc();
			return NextExpression(new ExprMap { from = lhs, map = NextExpression() });
		}
		if(t == TokenType.L_PAREN) {
			inc();
			return NextExpression(new ExprInvoke { symbol = lhs, args = NextParList() });
		}
		if(t == TokenType.SPARK) {
			inc();
			return NextExpression(new ExprInvoke { symbol = lhs, args = [NextExpression()] });
		}
		if(t == TokenType.MINUS) {
			inc();
			return NextExpression(new ExprInvoke { symbol = lhs, args = [NextTerm()] });
		}

		if(t == TokenType.SHOUT) {
			inc();
			return NextExpression(new ExprInvoke { symbol = lhs, args = [] });
		}
		if(t == TokenType.EQUAL) {
			inc();
			return NextExpression(new ExprEqual{
				lhs = lhs,
				rhs = NextExpression()
			});
		}

		if(t == TokenType.SLASH) {
			inc();
			t = tokenType;
			if(t == TokenType.NAME) {
				var name = currToken.str;
				inc();
				return NextExpression(new ExprGet {
					src = lhs,
					key = name
				});
			}
			throw new Exception("Name expected");
		}

		if(t == TokenType.SWIRL) {
			inc();
			var index = NextExpression();
			return NextExpression(new ExprIndex {
				src = lhs,
				index = index
			});
		}
		if(t == TokenType.QUERY) {
			inc();
			t = tokenType;
			if(t == TokenType.PLUS) {
				inc();
				var positive = NextExpression();
				var negative = default(INode);
				t = tokenType;
				if(t == TokenType.QUERY) {
					inc();
					t = tokenType;
					if(t == TokenType.MINUS) {
						inc();
						negative = NextExpression();
					}
				}
				return NextExpression(new ExprBranch {
					condition = lhs,
					positive = positive,
					negative = negative
				});
			}
			if(t == TokenType.SPARK) {
				inc();
				return NextExpression(new ExprLoop { condition = lhs, positive = NextExpression() });
			}
			dec();
		}
		return lhs;
	}

	INode NextTerm () {
		switch(tokenType) {
			case TokenType.L_SQUARE:
				return NextArray();
			case TokenType.SWIRL:
				return NextLambda();
			case TokenType.NAME:
				return NextSymbolicExpression();
			case TokenType.CARET:
				return NextSymbolOrReturn();
			case TokenType.STRING:
				return NextString();
			case TokenType.INTEGER:
				return NextInteger();
			case TokenType.L_CURLY:
				//Object literal
				return NextScope();
		}
		throw new Exception($"Unexpected token in expression: {currToken.type}");
	}
	INode NextArray() {
		List<INode> items = [];
		inc();
		Check:
		var t = tokenType;
		if(t != TokenType.R_SQUARE) {
			items.Add(NextExpression());
			goto Check;
		}
		inc();
		return new ExprSeq { items = items };
	}
	INode NextLambda () {
		inc();
		var t = tokenType;
		if(t == TokenType.SHOUT) {
			inc();
			var result = NextExpression();
			return new ExprFunc { pars = [], result = result };
		}
		if(t == TokenType.L_PAREN) {
			inc();
			var pars = NextParList();
			var result = NextExpression();
			return new ExprFunc { pars = pars, result = result };
		}
		throw new Exception($"Unexpected token {t}");
	}
	INode NextSymbolicExpression () {
		//May be cast object, variable, or a function call / literal.
		var name = currToken.str;
		inc();

		var t = tokenType;
		if(t == TokenType.L_CURLY) {
			return new ExprCastBlock { type = name, obj = NextScope() };
		}

		return new ExprSymbol { key = name };
	}
	INode NextSymbolOrReturn () {
		inc();
		int up = 1;
		Check:
		var t = tokenType;

		if(t == TokenType.COLON && up == 1) {
			inc();
			return new StmtReturn { val = NextExpression() };
		}
		if(t is TokenType.CARET) {
			up += 1;
			inc();
			goto Check;
		}
		if(t is TokenType.NAME) {
			var s = new ExprSymbol { up = up, key = currToken.str };
			inc();
			return s;
		}
		throw new Exception($"Unexpected token in up-symbol {currToken.type}");
	}
	public ExprVal<string> NextString () {
        var value = tokens[index].str;
        inc();
        return new ExprVal<string> { value = value };
    }
    public ExprVal<int> NextInteger() {
        var value = int.Parse(tokens[index].str);
        inc();
        return new ExprVal<int> { value = value };
    }
	public ExprBlock NextScope() {
        inc();
		var ele = new List<INode>();
        Check:
        var t = tokenType;
		if(t == TokenType.R_CURLY) {
			inc();
			return new ExprBlock { statements = ele };
		}
		if(t == TokenType.COMMA) {
			inc();
			goto Check;
		}
		if(t == TokenType.CARET) {
			ele.Add(NextReassignOrExpressionOrReturn());
			goto Check;
		} 
		if(t == TokenType.NAME) {
			ele.Add(NextStatementOrExpression());
			goto Check;
		}

        throw new Exception($"Unexpected token in object expression: {currToken.type}");
	}

    INode NextReassignOrExpressionOrReturn () {
		var symbol = NextSymbolOrReturn();
		if(symbol is StmtReturn) return symbol;

        var t = tokenType;
        if(t == TokenType.COMMA) {
            return symbol;
        } else if(t == TokenType.COLON) {
            inc();

			t = tokenType;
			if(t == TokenType.EQUAL) {
                inc();
                var exp = NextExpression();
                return new StmtAssign { symbol = (ExprSymbol)symbol, value = exp };
            }
            throw new Exception($"Reassign expected: {tokenType}");

        } else if(t == TokenType.L_PAREN) {
			inc();
			var args = NextParList();
			return NextExpression(new ExprInvoke { symbol = symbol, args = args });
        }
        throw new Exception($"Unexpected token in reassign or expression {tokenType}");
	}
	//A:B,
	//A():B,
	//A:B {},
	//A():B {},
	//A(a:int, b:int): B{}
	public INode NextStatementOrExpression () {
		var name = currToken.str;
		inc();
		switch(tokenType) {
			case TokenType.L_PAREN or TokenType.SPARK or TokenType.SHOUT:
				return NextFuncOrExpression(name);
			case TokenType.COLON:
				return NextDefineOrReassign(name);
			default:
				return NextExpression(new ExprSymbol { key = name });
		}
		throw new Exception($"Unexpected token: {tokenType}");
	}
	INode NextDefineOrReassign (string name) {
		inc();
		switch(currToken.type) {
			case TokenType.EQUAL:
				inc();
				return new StmtAssign {
					symbol = new ExprSymbol { key = name },
					value = NextExpression()
				};
			default:
				return new StmtKeyVal {
					key = name,
					value = NextExpression()
				};
		}
	}
	INode NextFuncOrExpression (string name) {

		var t = tokenType;
		if(t == TokenType.SPARK) {
			inc();
			return NextExpression( new ExprInvoke {
				symbol = new ExprSymbol { key = name },
				args = [NextExpression()],
			});
		}

		if (t == TokenType.SHOUT) {
			inc();
			t = tokenType;

			if(t == TokenType.COLON) {
				inc();
				return new StmtDefFunc {
					key = name,
					pars = [],
					value = NextExpression()
				};
			} else {
				return NextExpression( new ExprInvoke {
					symbol = new ExprSymbol { key = name },
					args = [],
				});
			}
		}
		inc();
		var pars = NextParList();
		t = currToken.type;
		if(t == TokenType.COLON) {
			inc();
			return new StmtDefFunc {
				key = name,
				pars = pars.Cast<StmtKeyVal>().ToList(),
				value = NextExpression()
			};
		}
		return NextExpression( new ExprInvoke { symbol = new ExprSymbol { key = name }, args = pars });
	}
	List<INode> NextParList () {
		var par = new List<INode> { };
		Check:
		switch(currToken.type) {
			case TokenType.R_PAREN:
				inc();
				return par;
			case TokenType.COMMA:
				inc();
				goto Check;
			case TokenType.CARET:
			case TokenType.INTEGER:
			case TokenType.STRING:
				par.Add(NextExpression());
				goto Check;
			case TokenType.NAME:
				par.Add(NextPairOrExpression());
				goto Check;
		}
		return par;
		INode NextPairOrExpression () {
			var key = currToken.str;
			if(tokens[index + 1].type != TokenType.COLON) {
				return NextExpression();
			}
			inc();
			var t = currToken.type;
			if(t == TokenType.COMMA || t == TokenType.R_PAREN) {
				return new ExprSymbol { key = key };
			}
			if(currToken.type != TokenType.COLON) {
				throw new Exception($"Expected colon in parameter list: {currToken.type}");
			}
			inc();
			var val = NextExpression();
			var result = new StmtKeyVal { key = key, value = val };
			return result;
		}
	}
}
public class ExprCastBlock : INode {
    public string type;
    public ExprBlock obj;
    public XElement ToXML () => new("CastBlock", new XAttribute("type", type), obj.ToXML());
    public string Source => $"{type} {obj.Source}";
	public dynamic Eval(Scope ctx) {

		var getResult = () => obj.Eval(ctx);

		var t = ctx.Get(type);


		if(t is ValEmpty)
			return ValError.TYPE_NOT_FOUND;

		if(t is Type tt) {
			return new ValType(tt).Cast(getResult());
		}
		if(type == "class") {
			if(getResult() is Scope s) {
				return new ValClass { obj = s, src = obj, src_ctx = ctx };
			}
		}
		if(t is ValClass vc) {
			var scope = (Scope) vc.src.Eval(vc.src_ctx);

			scope.parent = ctx;
			var o = obj.Apply(scope);
			return o;
		
		}
		if(t is ValInterface vi) {
			//return vi.Cast(t);
		}
		
		
		
		
		return ValError.TYPE_EXPECTED;
		//return result;
	}
}
public class ExprEqual : INode {
	public INode lhs;
	public INode rhs;

	public dynamic Eval(Scope scope) {
		return lhs.Eval(scope) == rhs.Eval(scope);
	}
}
public class ExprBranch : INode {
	public INode condition;
	public INode positive;
	public INode negative;


	public string Source => $"{condition.Source} ?+ {positive.Source}{(negative != null ? $" ?- {negative.Source}" : $"")}";

	public dynamic Eval(Scope ctx) {
		var cond = condition.Eval(ctx);
		if(cond == true) {
			return positive.Eval(ctx);
		}
		if(negative != null) {
			return negative.Eval(ctx);
		}
		return ValEmpty.VALUE;
	}
}
public class ExprLoop: INode {
	public INode condition;
	public INode positive;

	public dynamic Eval(Scope ctx) {
		dynamic r = ValEmpty.VALUE;
		Step:
		var cond = condition.Eval(ctx);
		if(cond == true) {
			r = positive.Eval(ctx);
			goto Step;
		} else if(cond != false) {
			return ValError.BOOLEAN_EXPECTED;
		}
		return r;
	}
}


public class ExprInvoke : INode {
    public INode symbol;
    public List<INode> args;
	public string Source => $"{symbol.Source}{(args.Count > 1 ? $"({string.Join(", ", args.Select(a => a.Source))})" : args.Count == 1 ? $"*{args.Single().Source}" : $"!")}";
	public dynamic Eval(Scope ctx) {
		var lhs = symbol.Eval(ctx);
		if(lhs is ValEmpty) {
			return ValError.FUNCTION_NOT_FOUND;
		}
		if(lhs is ValConstructor vc) {
			var rhs = args.Select(arg => arg.Eval(ctx)).ToArray();
			var c = vc.t.GetConstructor(rhs.Select(arg => (arg as object).GetType()).ToArray());
			if(c == null) {
				return ValError.CONSTRUCTOR_NOT_FOUND;
			}
			return c.Invoke(rhs);
		}


		if(lhs is ValMethod vm) {
			var vals = args.Select(arg => arg.Eval(ctx)).ToArray();
			return vm.m.Invoke(vm.src, vals);
		}
		if(lhs is Type t) {
			var d = args.Single().Eval(ctx);
			return new ValType(t).Cast(d);
		}
		if(lhs is ValFunc vf) {
			return vf.Call(ctx, args);
		}
		if(lhs is Delegate f) {
			
			var r = f.DynamicInvoke(args.Select(a => a.Eval(ctx)).ToArray());

			if(f.Method.ReturnType == typeof(void)) {
				return ValEmpty.VALUE;
			}
			return r;
		}

		throw new Exception("Implement function eval");
	}
}

public class StmtReturn : INode {
	public INode val;

	public dynamic Eval(Scope ctx) {
		return new ValReturn(val.Eval(ctx));
	}

}
public class ExprBlock : INode {
    public List<INode> statements;
    public XElement ToXML () => new ("Block", statements.Select(i => i.ToXML()));
	public string Source => $"{{{string.Join(", ", statements.Select(i => i.Source))}}}";
	public dynamic Eval(Scope ctx) {
		return Apply(new Scope(ctx, false));

	}

	public dynamic Apply(Scope f) {
		dynamic r = ValEmpty.VALUE;
		foreach(var s in statements) {
			r = s.Eval(f);
			if(r is ValReturn vr) {
				return vr.data;
			}
		}
		return f;
	}
}
public class ExprSymbol : INode {
    public int up = -1;
    public string key;
    public XElement ToXML () => new("Symbol", new XAttribute("key", key), new XAttribute("level", $"{up}"));
	public string Source => $"{new string('^', up)}{key}";

	public dynamic Eval(Scope ctx) {
		return ctx.Get(key, up);
	}
}
public class ExprGet : INode {
	public INode src;
	public string key;

	public dynamic Eval(Scope ctx) {

		var FL = BindingFlags.Public | BindingFlags.Instance;

		var source = src.Eval(ctx);
		if(source is Scope s) {
			return s.locals.TryGetValue(key, out var v) ? v : ValError.VARIABLE_NOT_FOUND;
		}

		if(source is ValClass vc) {
			return vc.obj.locals.TryGetValue(key, out var v) ? v : ValError.VARIABLE_NOT_FOUND;
		}
		if(source is Type t) {
			if(key == "new") {
				return new ValConstructor(t);
			} else if(t.GetMethod(key, BindingFlags.Static | BindingFlags.Public) is { }m) {
				return new ValMethod(null, m);
			}
		}
		if(source is object o) {
			var ot = o.GetType();
			if(ot.GetProperty(key, FL) is { } p) {
				return p.GetValue(source);
			}
			if(ot.GetMethod(key, FL) is { } m) {
				return new ValMethod(o, m);
			}
		}
		return ValError.OBJECT_EXPECTED;
	}
}
public class ExprIndex : INode {
	public INode src;
	public INode index;
	public dynamic Eval(Scope ctx) {
		var scope = src.Eval(ctx);
		if(scope is IEnumerable e) {
			var ind = index.Eval(ctx);
			if(ind is int i) {
				return e.Cast<object>().ElementAt(i);
			}
		}
		return ValError.SEQUENCE_EXPECTED;
	}
}

public class ExprVal<T> : INode {
    public T value;
    public XElement ToXML () => new("Value", new XAttribute("value", value));
	public string Source => $"{value}";
	public dynamic Eval(Scope ctx) {
		return value;
	}
}


public class ExprMap : INode {
	public INode from;
	public INode map;


	public XElement ToXML () => new("Map", from.ToXML(), map.ToXML());
	public string Source => $"{from.Source} | {map.Source}";
	public dynamic Eval (Scope ctx) {
		var _from = from.Eval(ctx);
		
		if(_from is ValEmpty) {
			return ValError.VARIABLE_NOT_FOUND;
		}
		if(_from is not IEnumerable e) {
			return ValError.SEQUENCE_EXPECTED;
		}

		var _map = map.Eval(ctx);
		if(_map is not ValFunc vf) {
			return ValError.FUNCTION_EXPECTED;
		}

		var result = new List<dynamic>();
		foreach(var item in e) {
			var r = vf.Call(ctx, [new ExprVal<dynamic> { value = item }]);
			if(r is ValEmpty) {
				continue;
			}
			result.Add(r);
		}

		return result.Count == 0 ? ValEmpty.VALUE : result;
	}
}
public class StmtKeyVal : INode {
    public string key;
    public INode value;
    public XElement ToXML () => new("KeyVal", new XAttribute("key", key), value.ToXML());
	public string Source => $"{key}:{value?.Source ?? "null"}";
	public dynamic Eval(Scope ctx) {
		var val = value.Eval(ctx);
		return Define(ctx, key, val);
	}
	public static dynamic Define(Scope ctx, string key, dynamic val) {
		if(val is Type t) {
			val = new ValDeclared { type = t };
		}
		ctx.locals[key] = val;
		return ValEmpty.VALUE;
	}
}

public class ExprFunc : INode {
	public List<INode> pars;
	public INode result;


	public XElement ToXML () => new("ExprFunc", [.. pars.Select(i => i.ToXML()), result.ToXML()]);
	public string Source => $"@({string.Join(", ", pars.Select(p => p.Source))}) {result.Source}";


	public dynamic Eval (Scope ctx) =>
		new ValFunc {
			expr = result,
			pars = pars.Cast<StmtKeyVal>().ToList(),
			owner = ctx
		};
}

public class ExprSeq : INode {
	public List<INode> items;

	public dynamic Eval(Scope ctx) {
		return items.Select(i => i.Eval(ctx)).ToArray();
	}
}
public class StmtDefFunc : INode {
	public string key;
    public List<StmtKeyVal> pars;
	public INode value;

	public XElement ToXML () => new("DefineFunc", [new XAttribute("key", key), ..pars.Select(i => i.ToXML()), value.ToXML()]);
    public string Source => $"{key}({string.Join(", ",pars.Select(p => p.Source))}): {value.Source}";
	public dynamic Eval(Scope ctx) {
		Define(ctx);
		return ValEmpty.VALUE;
	}
	public void Define(Scope owner) {
		owner.locals[key] = new ValFunc {
			expr = value,
			pars = pars,
			owner = owner
		};
	}
}

public class StmtAssign : INode {
    
    
    public ExprSymbol symbol;


    public INode value;

    XElement ToXML () => new("Reassign", symbol.ToXML(), value.ToXML());

    public string Source => $"{symbol.Source} := {value.Source}";


	public dynamic Eval(Scope ctx) {
		return Assign(ctx, symbol.key, symbol.up, () => value.Eval(ctx));
	}
	public static dynamic Assign(Scope ctx, string key, int up, Func<object> getNext) {
		var curr = (object)ctx.Get(key, up);
		if(curr == ValError.VARIABLE_NOT_FOUND) {
			return curr;
		}

		var currType = curr.GetType();
		if(curr is ValDeclared vd) {
			currType = vd.type;
		}

		var next = getNext();

		if(currType != next.GetType()) {
			return ValError.TYPE_MISMATCH;
		}
		return ctx.Set(key, next, up);
	}
}
public interface INode {
    XElement ToXML () => new(GetType().Name);

    String Source => "";

	dynamic Eval (Scope ctx) => null;
}

class Tokenizer {
    string src;
    int index;
    public Tokenizer(string src) {
        this.src = src;
    }

    public Token Next() {
        if(index >= src.Length) {
            return new Token { type = TokenType.EOF };
        }
        var str = (params char[] c) => string.Join("", c);


		void inc () => index += 1;
        Check:
        var c = src[index];
        if(c switch {
			':' => TokenType.COLON,
			'(' => TokenType.L_PAREN,
			')' => TokenType.R_PAREN,
			'[' => TokenType.L_SQUARE,
			']' => TokenType.R_SQUARE,
			'{' => TokenType.L_CURLY,
			'}' => TokenType.R_CURLY,
			'<' => TokenType.L_ANGLE,
			'>' => TokenType.R_ANGLE,
			',' => TokenType.COMMA,
			'^' => TokenType.CARET,
			'.' => TokenType.DOT,
			'@' => TokenType.SWIRL,
			'?' => TokenType.QUERY,
			'=' => TokenType.EQUAL,
			'!' => TokenType.SHOUT,
			'*' => TokenType.SPARK,
			'|' => TokenType.PIPE,
			'$' => TokenType.CASH,
			'+' => TokenType.PLUS,
			'-' => TokenType.MINUS,
			'/' => TokenType.SLASH,
			_ => default(TokenType?)
		} is { }tt) {
            index += 1;
			return new Token { type = tt, str = str(c) };
		}
		if(c is ' ' or '\r' or '\t' or '\n') {
			inc();
			goto Check;
		}
		if(c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z')) {
			int dest = index + 1;
			while(dest < src.Length && src[dest] is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z')) {
				dest += 1;
			}
			var v = src[index..dest];
			index = dest;
			return new Token { type = TokenType.NAME, str = v };
		}
		if(c == '"') {

			int dest = index + 1;
			while(dest < src.Length && src[dest] != '"') {
				dest += 1;
			}
			dest += 1;
			var v = src[index..dest];
			index = dest;
			return new Token { type = TokenType.STRING, str = v };
		}
		if(c is >= '0' and <= '9') {
			int dest;
			for(dest = index + 1; dest < src.Length && src[dest] is >= '0' and <= '9'; dest++) {
			}
			var v = src[index..dest];
			index = dest;
			return new Token { type = TokenType.INTEGER, str = v };
		}
        throw new Exception();
    }
}

public enum TokenType {
	NAME,
    COMMA,
	COLON,
	L_PAREN,
	R_PAREN,
    L_CURLY,
    R_CURLY,
    L_SQUARE,
    R_SQUARE,
	L_ANGLE,
	R_ANGLE,
	CARET,
	DOT,
	EQUAL,
	STRING,
	INTEGER,
    PLUS,
	MINUS,
    SLASH,

    SWIRL, QUERY, SHOUT, SPARK, PIPE, CASH,

	EOF
}
class Token {
    public TokenType type;
    public string str;

    public string ToString () => $"[{type}] {str}";
}