// See https://aka.ms/new-console-template for more information
using System.Linq.Expressions;
using System.Reflection.Metadata.Ecma335;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;


/*

range(1, grid.width) | (x:int): int {
},
 */
/*	Design philosophy
 *	A:B -> key A has value B
 *	Out of definition, reassignment, and conditionals, reassignment is the most verbose and demands the most attention.
*/
var code = """
{
	test(a:int): void { print(args) }
    Data: class {
        str: str* "Hello World"
		num: int* 5
		flag: bool* true
    }
	b: 2
	msg!: "alert"
	printB: print
	alert! : printB * "aaa"
	alertB!: alert!
	getStr! : "6666"
	aaaa! : "556"
    main(args: string): int {
		test* 0
        a: "hello world"

		print(range(1, 5))

		z: getStr!
		printB* z

		msg: "another alert"
		msg := "not an alert"

		alert!
		alertB!

		1 -> 5


		printB* "print B"
		printB* msg

		b: 1
        b := 10
        data: {
            a: a
            b: 9
            c: ^^^^b
			print* c
        }

        modify(u: int, v: str): int {
            ^a := u
            ^data := v
        },
        val: bool* true

		printB* val
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
global.locals["int"] = new ValType(typeof(int));
global.locals["str"] = new ValType(typeof(string));
global.locals["bool"] = new ValType(typeof(bool));
global.locals["void"] = new ValType(null);

global.locals["print"] = new Action<object>(Console.WriteLine);
global.locals["range"] = new Func<int, int, int[]>((a, b) => Enumerable.Range(a, b - a).ToArray());
global.locals["true"] = true;
global.locals["false"] = false;
var result = (Scope)scope.Eval(global);
var r  = (result.locals["main"] as ValFunc).Call(result);
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
	public static readonly ValError TYPE_MISMATCH = new("Type mismatch");
	public static readonly ValError VARIABLE_NOT_FOUND = new("Variable unknown");
	public static readonly ValError TYPE_NOT_FOUND = new("Type unknown");
	public static readonly ValError TYPE_EXPECTED = new("Type expected");
}
public class ValEmpty {
	public static readonly ValEmpty VALUE = new();
}
public class ValDeclared(Type type) {}
public class ValFunc {
	public INode expr;
	public List<StmtKeyVal> pars;
	public Scope owner;

	public dynamic Call(Scope frame) {
		var ctx = new Scope(owner, false);
		var args = new List<dynamic>();
		ctx.locals["args"] = args;
		foreach(var v in pars) {
			var val = v.value.Eval(frame);
			ctx.locals[v.key] = val;
			args.Add(val);
		}
		var result = expr.Eval(ctx);
		return result;
	}
}
public record ValType(Type type) {
	public dynamic Cast(object data) {
		if(type == null) {
			return ValEmpty.VALUE;
		}
		if(data.GetType() == type) {
			return data;
		}
		return ValError.TYPE_MISMATCH;
	}
}
public record ValInterface {

}


public class Scope {
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
				return ValEmpty.VALUE;
		}
		return
			parent != null ? parent.GetAt(key, temp ? up : up - 1) :
			ValEmpty.VALUE;
	}
	private dynamic GetAll (string key) =>
			locals.TryGetValue(key, out var v) ? v :
			parent != null ? parent.GetAll(key) :
			ValEmpty.VALUE;
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
		return ValEmpty.VALUE;
	}
	public dynamic SetLast (string key, object val) =>
		locals.TryGetValue(key, out var v) ? locals[key] = val :
		parent != null ? parent.SetLast(key, val) :
		ValEmpty.VALUE;


}

class Parser {
    int index;
    List<Token> tokens;

    public Parser(List<Token> tokens) {
        this.tokens = tokens;
    }

	void inc () => index++;
	

    public Token currToken => tokens[index];
    public TokenType currType => currToken.type;
	public INode NextExpression () {
		var term = GetTerm();
		var t = currType;
		if(t == TokenType.DASH) {

		}
		return term;

		INode GetTerm () {
			switch(currType) {
				case TokenType.NAME:
					return NextSymbolicExpression();
				case TokenType.CARET:
					return NextUpSymbol();
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
        INode NextSymbolicExpression() {
			//May be cast object, variable, or a function call / literal.
			var name = currToken.str;
			inc();

			var t = currType;
			if(t == TokenType.L_PAREN) {
				inc();
				return new ExprInvoke { symbol = new ExprSymbol { key = name }, args = NextArgList() };
			}
			if(t == TokenType.SPARKLE) {
				inc();
				return new ExprInvoke { symbol = new ExprSymbol { key = name }, args = [NextExpression()] };
			}
			if(t == TokenType.SHOUT) {
				inc();
				return new ExprInvoke { symbol = new ExprSymbol { key = name }, args = [] };
			}
			if(t == TokenType.L_CURLY) {
				return new ExprCastBlock { type = name, obj = NextScope() };
			}
			return new ExprSymbol { key = name };


			throw new Exception($"Unknown token type in expression {currToken.type}");
		}
	}
	List<INode> NextArgList () {
		List<INode> items = [];
		Check:
		switch(currToken.type) {
			case TokenType.CARET:
				items.Add(NextExpression());
				goto Check;
			case TokenType.INTEGER:
				items.Add(new ExprVal<int> { value = int.Parse(currToken.str) });
				inc();
				goto Check;
			case TokenType.STRING:
				items.Add(new ExprVal<string> { value = currToken.str });
				inc();
				goto Check;
			case TokenType.R_PAREN:
				inc();
				return items;
			case TokenType.NAME:
				items.Add(new ExprSymbol { key = currToken.str });
				inc();
				goto Check;
			case TokenType.COMMA:
				inc();
				goto Check;
		}
		throw new Exception($"Unexpected token in arg list: {currToken.type}");
	}
	ExprSymbol NextUpSymbol () {
		int up = 1;
		inc();
		Check:
		var t = currType;
		if(t is TokenType.CARET) {
			up += 1;
			inc();
			goto Check;
		} else if(t is TokenType.NAME) {
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
        var t = currType;
        if(t is TokenType.CARET) {
			ele.Add(NextReassignOrExpression());
			goto Check;
		} else if(t is TokenType.NAME) {
			ele.Add(NextDefinitionOrStatement());
			goto Check;
		} else if(t is TokenType.COMMA) {
			inc();
			goto Check;
		} else if(t is TokenType.R_CURLY) {
			inc();
			return new ExprBlock { statements = ele };
		}
        throw new Exception($"Unexpected token in object expression: {currToken.type}");
	}

    INode NextReassignOrExpression () {
		var symbol = NextUpSymbol();
        var t = currType;
        if(t == TokenType.COMMA) {
            return symbol;
        } else if(t == TokenType.COLON) {
            inc();
			t = currType;
			if(t == TokenType.EQUAL) {
                inc();
                var exp = NextExpression();
                return new StmtReassign { symbol = symbol, value = exp };
            }
            throw new Exception($"Reassign expected: {currType}");

        } else if(t == TokenType.L_PAREN) {
			inc();
			var args = NextArgList();
			return new ExprInvoke { symbol = symbol, args = args };
        }
        throw new Exception($"Unexpected token in reassign or expression {currType}");
	}
	//A:B,
	//A():B,
	//A:B {},
	//A():B {},
	//A(a:int, b:int): B{}
	public INode NextDefinitionOrStatement () {
		var name = currToken.str;
		inc();
		switch(currToken.type) {
			case TokenType.L_PAREN or TokenType.SPARKLE or TokenType.SHOUT:
				return NextInvokeOrFunc(name);
			case TokenType.COLON:
				return NextDefineOrReassign(name);
			case TokenType.QUESTION:

				return NextTernary();
		}
		throw new Exception($"Unexpected token: {currType}");
	}


	public INode NextTernary () {
		return null;
	}
	INode NextDefineOrReassign (string name) {
		inc();
		switch(currToken.type) {
			case TokenType.EQUAL:
				inc();
				return new StmtReassign {
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
	INode NextInvokeOrFunc (string name) {

		var t = currType;
		if(t == TokenType.SPARKLE) {
			inc();
			return new ExprInvoke {
				symbol = new ExprSymbol { key = name },
				args = [NextExpression()],
			};
		}

		if (t == TokenType.SHOUT) {
			inc();
			t = currType;

			if(t == TokenType.COLON) {
				inc();
				return new StmtDefFunc {
					key = name,
					par = [],
					value = NextExpression()
				};
			} else {
				return new ExprInvoke {
					symbol = new ExprSymbol { key = name },
					args = [],
				};
			}
		}



		inc();



		var par = NextParList();

		t = currToken.type;
		if(t == TokenType.COLON) {
			inc();
			return new StmtDefFunc {
				key = name,
				par = par,
				value = NextExpression()
			};
		}

		return new ExprInvoke { symbol = new ExprSymbol { key = name }, args = par };

		if(t == TokenType.COMMA || t == TokenType.R_CURLY) {
			return new ExprInvoke { symbol = new ExprSymbol { key = name }, args = par };
		}

		throw new Exception($"Unexpected token {currToken.type}");
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
			if(currToken.type != TokenType.NAME) {
				throw new Exception($"Expected symbol in parameter list: {currToken.type}");
			}
			var result = new StmtKeyVal { key = key, value = new ExprVal<string> { value = currToken.str } };
			inc();
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
		var result = obj.Eval(ctx);
		var t = ctx.Get(type);
		if(t is ValEmpty) 
			return ValError.TYPE_NOT_FOUND;
		if(t is ValType vt) return vt.Cast(t);
		if(t is ValInterface vi) {
			//return vi.Cast(t);
		}

		return ValError.TYPE_EXPECTED;
		//return result;
	}
}
public class ExprInvoke : INode {
    public ExprSymbol symbol;
    public List<INode> args;
	public string Source => $"{symbol.Source}({string.Join(", ", args.Select(a => a.Source))})";

	public dynamic Eval(Scope frame) {
		var key = frame.Get(symbol.key, symbol.up);
		if(key is ValEmpty) {
			return ValError.FUNCTION_NOT_FOUND;
		}

		if(key is ValType t) {
			return t.Cast(args.Single().Eval(frame));
		}
		if(key is ValFunc vf) {
			return vf.Call(frame);
		}
		if(key is Delegate f) {
			return f.DynamicInvoke(args.Select(a => a.Eval(frame)).ToArray());
		}

		throw new Exception("Implement function eval");
	}
}
public class ExprBlock : INode {
    public List<INode> statements;
    public XElement ToXML () => new ("Block", statements.Select(i => i.ToXML()));
	public string Source => $"{{{string.Join(", ", statements.Select(i => i.Source))}}}";
	public dynamic Eval(Scope frame) {
		var f = new Scope(frame, false);
		foreach(var s in statements) {
			s.Eval(f);
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
	INode from;
	string key;
	public dynamic Eval(Scope ctx) {
		var scope = from.Eval(ctx);
		if(scope is Scope s) {
			return s.locals.TryGetValue(key, out var v) ? v : ValError.VARIABLE_NOT_FOUND;
		}
		return ValEmpty.VALUE;
	}
}

public class ExprVal<T> : INode {
    public T value;
    public XElement ToXML () => new("Value", new XAttribute("value", value));
	public string Source => $"{value}";
	public dynamic Eval(Scope frame) {
		return value;
	}
}
public class StmtKeyVal : INode {
    public string key;
    public INode value;
    public XElement ToXML () => new("KeyVal", new XAttribute("key", key), value.ToXML());
	public string Source => $"{key}:{value?.Source ?? "null"}";
	public dynamic Eval(Scope frame) {

		var val = value.Eval(frame);
		/*
		if(val is ValType t) {
			val = new ValDeclared(t.type);
		}
		*/
		frame.locals[key] = val;
		return ValEmpty.VALUE;
	}
}
public class StmtDefFunc : INode {
	public string key;
    public List<INode> par;
	public INode value;

	public XElement ToXML () => new("DefineFunction", [new XAttribute("key", key), ..par.Select(i => i.ToXML()), value.ToXML()]);
    public string Source => $"{key}({string.Join(", ",par.Select(p => p.Source))}): {value.Source}";


	public dynamic Eval(Scope frame) {
		frame.locals[key] = new ValFunc {
			expr = value,
			pars = [],
			owner = frame
		};
		return ValEmpty.VALUE;
	}
}

public class StmtReassign : INode {
    
    
    public ExprSymbol symbol;


    public INode value;

    XElement ToXML () => new("Reassign", symbol.ToXML(), value.ToXML());

    public string Source => $"{symbol.Source} := {value.Source}";


	public dynamic Eval(Scope ctx) {

		var curr = (object)ctx.Get(symbol.key, symbol.up);
		if(curr == ValEmpty.VALUE) {
			return ValError.VARIABLE_NOT_FOUND;
		}
		var next = (object)value.Eval(ctx);
		if(curr.GetType() != next.GetType()) {
			return ValError.TYPE_MISMATCH;
		}
		return ctx.Set(symbol.key, next, symbol.up);
	}
}
public interface INode {
    XElement ToXML () => new(GetType().Name);

    String Source => "";

	dynamic Eval (Scope frame) => null;
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

        Check:
        var c = src[index];
        if(TABLE.TryGetValue(c, out var tt)) {
            index += 1;
			return new Token { type = tt, str = str(c) };
		}

		switch(c) {
            case ' ' or '\r' or '\t' or '\n':
                index++;
                goto Check;
            case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z'): {
					int dest = index + 1;
					while(dest < src.Length && src[dest] is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z')) {
						dest += 1;
					}
                    var v = src[index..dest];
                    index = dest;
					return new Token { type = TokenType.NAME, str = v };
				}
			case '"': {
                    int dest = index + 1;
                    while(dest < src.Length && src[dest] != '"') {
                        dest += 1;
                    }
                    dest += 1;

                    var v = src[index..dest];
                    index = dest;
                    return new Token { type = TokenType.STRING, str = v };
                }
            case >= '0' and <= '9': {
                    int dest = index + 1;
                    while(dest < src.Length && src[dest] is >= '0' and <= '9') {
                        dest += 1;
                    }

                    var v = src[index..dest];
                    index = dest;
                    return new Token { type = TokenType.INTEGER, str = v };
                }
        }

        throw new Exception();
    }
	Dictionary<char, TokenType> TABLE = new() {
		['='] = TokenType.EQUAL,
		[':'] = TokenType.COLON,
		['('] = TokenType.L_PAREN,
		[')'] = TokenType.R_PAREN,
		['['] = TokenType.L_SQUARE,
		[']'] = TokenType.R_SQUARE,
		['{'] = TokenType.L_CURLY,
		['}'] = TokenType.R_CURLY,
		['<'] = TokenType.L_ANGLE,
		['>'] = TokenType.R_ANGLE,
		[','] = TokenType.COMMA,
		['^'] = TokenType.CARET,
		['.'] = TokenType.DOT,

		['?'] = TokenType.QUESTION,
		['='] = TokenType.EQUAL,
		['!'] = TokenType.SHOUT,
		['*'] = TokenType.SPARKLE,

		['+'] = TokenType.PLUS,
		['-'] = TokenType.DASH, // 1 = 2 ? 2 ! 3 
		['/'] = TokenType.SLASH,
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
	DASH,
    SLASH,

    QUESTION, SHOUT, SPARKLE,

	EOF
}
class Token {
    public TokenType type;
    public string str;

    public string ToString () => $"[{type}] {str}";
}