// See https://aka.ms/new-console-template for more information
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace Oblivia;
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

public record ValRef {
	public Array src;
	public int[] index;
	public void Set(dynamic value) =>
		src.SetValue(value, index);
	public dynamic Get () => src.GetValue(index);
}
public record ValConstructor (Type t) { }
public record ValInstanceMember (object src, string key) {
	public dynamic Call (dynamic[] data) {
		var tp = data.Select(d => (d as object).GetType()).ToArray();
		var fl = BindingFlags.Instance | BindingFlags.Public;
		return src.GetType().GetMethod(key, fl, tp).Invoke(src, data);
	}
};
public record ValStaticMember (Type src, string key) {
	public dynamic Call (dynamic[] data) {
		var tp = data.Select(d => (d as object).GetType()).ToArray();
		var fl = BindingFlags.Static | BindingFlags.Public;
		return src.GetMethod(key, fl, tp).Invoke(src, data);
	}
};
public record ValReturn(dynamic data, int up) {

}
public class ValEmpty {
	public static readonly ValEmpty VALUE = new();
}
public class ValDeclared {
	public object type;
}

public record ValGetter {
	public INode expr;
	public ValDictScope ctx;
	public dynamic Eval () => expr.Eval(ctx);
}
public record ValMacro {

}

public record Args {
	public dynamic this[string s] => dict[s];
	public dynamic this[int s] => list[s];

	public Dictionary<string, dynamic> dict = new();
	public List<dynamic> list = new();
}
public record ValFunc {
	public INode expr;
	public List<StmtKeyVal> pars;
	public ValDictScope parent_ctx;
	public dynamic Call(ValDictScope caller_ctx, IEnumerable<INode> args) {
		var func_ctx = new ValDictScope(parent_ctx, false);
		var argData = new Args { };
		func_ctx.locals["_arg"] = argData;

		foreach(var p in pars) {
			var val = p.value.Eval(caller_ctx);
			StmtKeyVal.Init(func_ctx, p.key, val);
		}
		int ind = 0;
		foreach(var arg in args) {
			if(arg is StmtKeyVal kv) {
				ind = -1;
				var val = StmtAssign.Assign(func_ctx, kv.key, 1, () => kv.value.Eval(caller_ctx));
			} else if(ind > -1) {
				var p = pars[ind];
				var val = StmtAssign.Assign(func_ctx, p.key, 1, () => arg.Eval(caller_ctx));
				ind += 1;
			}
		}
		foreach(var p in pars) {
			var val = func_ctx.locals[p.key];
			argData.list.Add(val);
			argData.dict[p.key] = val;
		}
		var result = expr.Eval(func_ctx);
		return result;
	}
	public dynamic CallData (ValDictScope caller_ctx, IEnumerable<object> args) {
		var func_ctx = new ValDictScope(parent_ctx, false);
		var argData = new Args { };
		func_ctx.locals["_arg"] = argData;
		foreach(var p in pars) {
			var val = p.value.Eval(caller_ctx);
			StmtKeyVal.Init(func_ctx, p.key, val);
		}
		int ind = 0;
		foreach(var arg in args) {
			var p = pars[ind];
			var val = StmtAssign.Assign(func_ctx, p.key, 1, () => arg);
			ind += 1;
		}
		foreach(var p in pars) {
			var val = func_ctx.locals[p.key];
			argData.list.Add(val);
			argData.dict[p.key] = val;
		}
		var result = expr.Eval(func_ctx);
		return result;
	}
	public dynamic ApplyData (ValDictScope caller_ctx, ValDictScope target_ctx, IEnumerable<object> args) {
		var func_ctx = new ValDictScope(parent_ctx, false);
		var argData = new Args { };
		func_ctx.locals["_arg"] = argData;

		foreach(var p in pars) {
			var val = p.value.Eval(caller_ctx);
			StmtKeyVal.Init(func_ctx, p.key, val);
		}
		int ind = 0;
		foreach(var arg in args) {
			var p = pars[ind];
			var val = StmtAssign.Assign(func_ctx, p.key, 1, () => arg);
			ind += 1;
		}
		foreach(var p in pars) {
			var val = func_ctx.locals[p.key];
			argData.list.Add(val);
			argData.dict[p.key] = val;
		}
		var inner_ctx = new ValDictScope {
			locals = target_ctx.locals,
			parent = func_ctx,
			temp = false
		};
		if(expr is ExprBlock b) {
			return b.Apply(inner_ctx);
		}
		if(expr is ExprVarBlock vb) {
		}
		var result = expr.Eval(func_ctx);
		return result;
	}
}
public record ValType(Type type) {
	public dynamic Cast(object next, Type nextType) {
		if(type == typeof(void)) {
			return ValEmpty.VALUE;
		}

		if(nextType != type) {
			//return ValError.TYPE_MISMATCH;

			return Convert.ChangeType(next, type);

			throw new Exception("Type mismatch");
		}
		return next;
	}
}
public record ValInterface {

}
public class ValClass {

	public string name;
	public ValDictScope _static;

	public INode source_expr;
	public ValDictScope source_ctx;
	public dynamic VarBlock(ValDictScope ctx, ExprBlock block) {
		var scope = (ValDictScope)source_expr.Eval(source_ctx);
		scope.locals["class"] = this;
		var r = block.Apply(new ValDictScope { locals = scope.locals, parent = ctx, temp = false});
		return r;
	}
}
public interface IScope {
	public dynamic Get (string key, int up = -1) =>
		up == -1 ? GetNearest(key) : GetAt(key, up);
	public dynamic GetAt (string key, int up);
	public dynamic GetNearest (string key);
	public dynamic GetHere(string key) => GetAt(key, 1);
}
public record ValObjectScope : IScope {
	public IScope parent;
	public object obj;
	public dynamic GetAt (string key, int up) {
		return null;
	}
	public dynamic GetNearest (string key) {
		return null;
	}
}

public record ValClassScope : IScope {
	public ValClass fromClass;

	public dynamic GetAt (string key, int up) {
		return null;
	}
	public dynamic GetNearest (string key) {
		return null;
	}

}
public record ValDictScope :IScope {
	public bool temp = false;
	public ValDictScope parent = null;
	public Dictionary<string, dynamic> locals = [];
	public ValDictScope (ValDictScope parent = null, bool temp = false) {
		this.temp = temp;
		this.parent = parent;
	}


	public ValDictScope MakeTemp (dynamic _) => new ValDictScope {
		locals = {
				["_"] = _
			},
		parent = this,
		temp = true
	};
	public dynamic Get(string key, int up = -1) =>
		up == -1 ? GetNearest(key) : GetAt(key, up);
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
	public dynamic GetNearest (string key) =>
			locals.TryGetValue(key, out var v) ? v :
			parent != null ? parent.GetNearest(key) :
			ValError.VARIABLE_NOT_FOUND;
	public dynamic Set (string key, object val, int up = -1) => up == -1 ? SetNearest(key, val) : SetAt(key, val, up);
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
	public dynamic SetNearest (string key, object val) =>
		locals.TryGetValue(key, out var v) ? locals[key] = val :
		parent != null ? parent.SetNearest(key, val) :
		ValError.VARIABLE_NOT_FOUND;
}
public class Parser {
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

			var cond = default(INode);
			t = tokenType;
			if(t == TokenType.L_ANGLE) {
				inc();
				cond = NextExpression();

				t = tokenType;
				if(t == TokenType.R_ANGLE) {
					inc();

				} else {
					throw new Exception("Closing expected");
				}
			}

			return NextExpression(new ExprMap { from = lhs, cond = cond, map = NextExpression() });
		}
		if(t == TokenType.L_PAREN) {
			inc();
			return NextExpression(new ExprInvoke { symbol = lhs, args = NextParList() });
		}
		if(t == TokenType.SPARK) {
			inc();
			return NextExpression(new ExprInvoke { symbol = lhs, args = [NextExpression()] });
		}
		if(t == TokenType.DASH) {
			inc();
			return NextExpression(new ExprInvoke { symbol = lhs, args = [NextTerm()] });
		}
		if(t == TokenType.SHOUT) {
			inc();
			return NextExpression(new ExprInvoke { symbol = lhs, args = [] });
		}
		if(t == TokenType.EQUAL) {
			inc();

			t = tokenType;

			bool invert;
			if(t == TokenType.PLUS) {
				inc();
				invert = false;
				goto Done;
			}
			if(t == TokenType.DASH) {
				inc();
				invert = true;
				goto Done;
			}
			throw new Exception();

			Done:
			return NextExpression(new ExprEqual {
				lhs = lhs,
				rhs = NextTerm(),
				invert = invert
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
			if(t == TokenType.L_CURLY) {
				return NextExpression(new ExprApply { lhs = lhs, rhs = NextBlock() });
			}
			throw new Exception("Name expected");
		}
		if(t == TokenType.SWIRL) {
			inc();
			var index = NextTerm();
			return NextExpression(new ExprIndex {
				src = lhs,
				index = index
			});
		}
		//Index
		if(t == TokenType.L_SQUARE) {
			inc();
			var index = NextExpression();
			t = tokenType;
			if(t == TokenType.R_SQUARE) {
				inc();
				return NextExpression(new ExprIndex {
					src = lhs,
					index = index
				});
			} else {
				//Add more index terms
			}
			throw new Exception("Expected closing delimiter");
		}
		if(t == TokenType.QUERY) {
			inc();
			t = tokenType;
			if(t == TokenType.L_CURLY) {
				inc();
				var items = new List<(INode antecedent, INode consequent)> { };

				Read:
				t = tokenType;
				if(t == TokenType.R_CURLY) {
					inc();
					return NextExpression(new ExprMatch {
						item = lhs,
						branches = items
					});
				}
				var ante = NextExpression();
				if(ante is ExprFunc ef) {
					//Handle lambda
					//If this lambda accepts this object as argument, then treat it as a branch.
					goto Read;
				}
				t = tokenType;
				if(t == TokenType.COLON) {
					inc();
					var cons = NextExpression();
					items.Add((ante, cons));
					goto Read;
				}
				throw new Exception();
			}
			//Conditional sequence
			if(t == TokenType.L_SQUARE) {
				inc();
				INode type = null;
				t = tokenType;
				if(t == TokenType.COLON) {
					inc();
					type = NextExpression();
				}
				var items = new List<(INode cond, INode yes, INode no)> { };
				Read:
				var ante = NextExpression();
				t = tokenType;
				if(t == TokenType.COLON) {
					inc();
					var cons = NextExpression();
					items.Add((ante, cons, null));
				} else {
					throw new Exception();
				}
				t = tokenType;
				if(t == TokenType.R_SQUARE) {
					inc();


					return NextExpression( new ExprCondSeq {
						type = type,
						filter = lhs,
						items = items
					});
				} else {
					goto Read;
				}
			}
			if(t == TokenType.PLUS) {
				inc();
				var positive = NextExpression();
				var negative = default(INode);
				t = tokenType;
				if(t == TokenType.QUERY) {
					inc();
					t = tokenType;
					if(t == TokenType.DASH) {
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
		if(t == TokenType.PERCENT) {
			inc();
			if(tokenType == TokenType.L_CURLY) {
				return NextExpression(new ExprApply { lhs = lhs, rhs = NextBlock() });
			} else {
				//return NextExpression(new ExprApplyBlock { lhs = lhs, rhs = NextExpression() });
				throw new Exception("Expected block");
			}
		}

		return lhs;
	}
	INode NextTerm () {

		switch(tokenType) {
			case TokenType.L_SQUARE:
				return NextArray();
			case TokenType.QUERY:
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
				return NextBlock();
			case TokenType.L_PAREN:
				return NextTupleOrExpression();

		}
		throw new Exception($"Unexpected token in expression: {currToken.type}");
	}

	INode NextTupleOrExpression () {
		inc();
		var expr = NextExpression();
		var t = tokenType;
		if(t == TokenType.R_PAREN) {
			inc();
			return expr;
		}
		return NextTuple(expr);
		INode NextTuple(INode first) {
			var items = new List<INode> { first };
			Check:
			items.Add(NextExpression());
			var t = tokenType;
			if(t == TokenType.R_PAREN) {
				inc();
				return new ExprTuple { items = items };
			}
			goto Check;
		}
		throw new Exception("Tuples not supported");
	}
	INode NextArray() {
		List<INode> items = [];
		inc();
		INode type = null;
		var t = tokenType;
		if(t == TokenType.COLON) {
			inc();
			type = NextExpression();
		}
		Check:
		t = tokenType;
		if(t == TokenType.COMMA) {
			inc();
			goto Check;
		}
		if(t != TokenType.R_SQUARE) {
			items.Add(NextExpression());
			goto Check;
		}
		inc();
		return new ExprSeq { items = items, type = type };
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
			return new ExprVarBlock { type = name, source_block = NextBlock() };
		}
		return new ExprSymbol { key = name };
	}
	INode NextSymbolOrReturn () {
		inc();
		int up = 1;
		Check:
		var t = tokenType;
		if(t == TokenType.COLON) {
			inc();
			if(up > 1) {
				t = tokenType;
				if(t == TokenType.EQUAL) {
					inc();
					return new StmtReturn { val = NextExpression(), up = up };
				}
				throw new Exception("Long return must be assignment");
			} else {
				return new StmtReturn { val = NextExpression(), up = up };
			}
		}
		if(t == TokenType.CARET) {
			up += 1;
			inc();
			goto Check;
		}
		if(t == TokenType.CASH) {
			//Return This
			var s = new ExprSelf { up = up };
			inc();
			return s;
		}
		if(t == TokenType.SLASH) {
			inc();
			t = tokenType;
			if(t == TokenType.NAME) {
				var r = new ExprGet { key = currToken.str, src = new ExprSelf { up = up } };
				inc();
				return r;
			} else {
				throw new Exception();
			}
		}

		if(t == TokenType.NAME) {
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
	public ExprBlock NextBlock() {
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
		if(t == TokenType.L_PAREN) {
			ele.Add(NextTupleOrExpression());
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
		if(t == TokenType.L_SQUARE) {
			ele.Add(NextExpression());
			goto Check;
		}
		if(t == TokenType.HASH) {
			inc();

			NextTerm();
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
			bool spread = false;
			t = tokenType;
			if(t == TokenType.SPARK) {
				inc();
			}
			var a = NextExpression();
			return NextExpression( new ExprInvoke {
				symbol = new ExprSymbol { key = name },
				args = [new ExprSpread { value = a }],
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

			case TokenType.NAME:
				par.Add(NextPairOrExpression());
				goto Check;
			default:
				par.Add(NextExpression());
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
public class ExprSpread : INode {
	public INode value;
	public dynamic Eval(ValDictScope ctx) {
		if(value is ExprSeq s) {
			return s.items.Select(i => i.Eval(ctx)).ToArray();
		}
		return value.Eval(ctx);
	}
}
public class ExprVarBlock : INode {
    public string type;
    public ExprBlock source_block;
    public XElement ToXML () => new("VarBlock", new XAttribute("type", type), source_block.ToXML());
    public string Source => $"{type} {source_block.Source}";
	public dynamic MakeScope (ValDictScope ctx) => source_block.MakeScope(ctx);
	public dynamic Eval(ValDictScope ctx) {
		var getResult = () => source_block.Eval(ctx);
		if(type == "class") {
			if(getResult() is ValDictScope s) {
				var c = new ValClass { _static = s, source_expr = source_block, source_ctx = ctx };
				s.locals["class"] = c;
				return c;
			} else {
				throw new Exception("Class expected");
			}
		}
		var t = ctx.Get(type);
		if(t is ValEmpty) {
			throw new Exception("Type not found");
		}
		if(t is Type tt) {
			var r = getResult();
			return new ValType(tt).Cast(r, r?.GetType());
		}
		if(type == "interface") {
			return null;
			if(getResult() is ValDictScope s) {
			}
		}
		if(type == "enum") {
			var locals = new Dictionary<string, dynamic> { };
			var rhs = getResult();
			return new ValDictScope { locals = [], parent = ctx, temp = false };
		}
		if(t is ValClass vc) {
			return vc.VarBlock(ctx, source_block);
		}
		if(t is ValInterface vi) {
			//return vi.Cast(t);
		}
		throw new Exception("Type expected");
		//return result;
	}
}
public class ExprEqual : INode {
	public INode lhs;
	public INode rhs;

	public bool invert;
	public dynamic Eval(ValDictScope scope) {
		var l = lhs.Eval(scope);
		var r = rhs.Eval(scope);
		var b = Equals(l, r);
		if(invert) {
			b = !b;
		}
		return b;
	}
}
public class ExprSingleMatch : INode {
	public INode lhs;
	public INode rhs;

	public dynamic Eval (ValDictScope ctx) {
		var l = lhs.Eval(ctx);

		var inner_ctx = ctx.MakeTemp(l);
		var r = rhs.Eval(inner_ctx);
		if(r is not bool) {
			throw new Exception();
		}
		return r;
	}

}
public class ExprBranch : INode {
	public INode condition;
	public INode positive;
	public INode negative;
	public string Source => $"{condition.Source} ?+ {positive.Source}{(negative != null ? $" ?- {negative.Source}" : $"")}";
	public dynamic Eval(ValDictScope ctx) {
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
	public dynamic Eval(ValDictScope ctx) {
		dynamic r = ValEmpty.VALUE;
		Step:
		var cond = condition.Eval(ctx);
		if(cond == true) {
			r = positive.Eval(ctx);
			goto Step;
		} else if(cond != false) {
			throw new Exception("Boolean expected");
		}
		return r;
	}
}
public class ExprInvoke : INode {
    public INode symbol;
    public List<INode> args;
	public string Source => $"{symbol.Source}{(args.Count > 1 ? $"({string.Join(", ", args.Select(a => a.Source))})" : args.Count == 1 ? $"*{args.Single().Source}" : $"!")}";
	public dynamic Eval(ValDictScope ctx) {
		if(symbol is ExprSymbol { key: "decl", up: -1 }) {
		}
		if(symbol is ExprSymbol { key:"get", up: -1 }) {
			return new ValGetter { ctx = ctx, expr = args.Single() };
		}
		var lhs = symbol.Eval(ctx);
		return Invoke(ctx, lhs, args);
	}
	public static object GetReturnType(object f) {
		throw new Exception("Implement");
	}
	public static dynamic Invoke(ValDictScope ctx, object lhs, IEnumerable<INode> pars) {
		if(lhs is ValEmpty) {
			throw new Exception("Function not found");
			//return ValError.FUNCTION_NOT_FOUND;
		}
		if(lhs is ValError ve) {
			throw new Exception(ve.msg);
		}
		var args = pars.Select(a => a.Eval(ctx));
		if(lhs is ValConstructor vc) {
			var rhs = args.ToArray();
			var c = vc.t.GetConstructor(rhs.Select(arg => (arg as object).GetType()).ToArray());
			if(c == null) {
				throw new Exception("Constructor not found");
				//return ValError.CONSTRUCTOR_NOT_FOUND;
			}
			return c.Invoke(rhs);
		}
		if(lhs is ValInstanceMember vim) {
			var vals = args.ToArray();
			return vim.Call(vals);
		}
		if(lhs is ValStaticMember vsm) {
			var vals = args.ToArray();
			return vsm.Call(vals);
		}
		if(lhs is Type t) {
			var d = args.Single();
			return new ValType(t).Cast(d, d?.GetType());
			if(t.IsPrimitive) {
				//return new ValType(t).Cast(d);
			} else {
				var o = t.GetConstructor([]).Invoke([]);
				var sc = new ValObjectScope { obj = o, parent = ctx };
				//args.Single().Eval(sc);
				return o;
			}
		}
		if(lhs is ValFunc vf) {
			return vf.Call(ctx, pars);
		}
		if(lhs is Delegate f) {
			var argArr = args.ToArray();
			var r = f.DynamicInvoke(argArr);
			if(f.Method.ReturnType == typeof(void))
				return ValEmpty.VALUE;
			return r;
		}
		if(lhs is ValClass vcl) {
			//Cast
		}
		if(lhs is ValDictScope s) {

			throw new Exception("Illegal");
			var result = new List<dynamic> { };
			foreach(var a in pars) {
				var r = a.Eval(s);
				if(r is ValEmpty)
					continue;
				result.Add(r);
			}
			if(result.Count > 0)
				return result;
			else return ValEmpty.VALUE;
		}
		throw new Exception();
	}
	public static dynamic InvokeData (ValDictScope ctx, object lhs, IEnumerable<dynamic> evalArgs) {
		if(lhs is ValEmpty) {
			throw new Exception("Function not found");
			//return ValError.FUNCTION_NOT_FOUND;
		}
		if(lhs is ValError ve) {
			throw new Exception(ve.msg);
		}
		if(lhs is ValConstructor vc) {
			var rhs = evalArgs.ToArray();
			var c = vc.t.GetConstructor(rhs.Select(arg => (arg as object).GetType()).ToArray());
			if(c == null) {
				throw new Exception("Constructor not found");
				//return ValError.CONSTRUCTOR_NOT_FOUND;
			}
			return c.Invoke(rhs);
		}
		if(lhs is ValInstanceMember vim) {
			var vals = evalArgs.ToArray();
			return vim.Call(vals);
		}
		if(lhs is ValStaticMember vsm) {

			var vals = evalArgs.ToArray();
			return vsm.Call(vals);
		}
		if(lhs is Type t) {
			var d = evalArgs.Single();
			return new ValType(t).Cast(d, d?.GetType());
			if(t.IsPrimitive) {
				//return new ValType(t).Cast(d);
			} else {
				var o = t.GetConstructor([]).Invoke([]);
				var sc = new ValObjectScope { obj = o, parent = ctx };
				//args.Single().Eval(sc);
				return o;
			}
		}
		if(lhs is ValFunc vf) {
			return vf.CallData(ctx, evalArgs.ToList());
		}
		if(lhs is Delegate f) {
			var argValues = evalArgs.ToArray();
			var r = f.DynamicInvoke(argValues);
			if(f.Method.ReturnType == typeof(void))
				return ValEmpty.VALUE;
			return r;
		}
		if(lhs is ValClass vcl) {
			//Cast
		}
		if(lhs is ValDictScope s) {

			throw new Exception("Illegal");
			var result = new List<dynamic> { };
			foreach(var a in evalArgs) {
				var r = a.Eval(s);
				if(r is ValEmpty)
					continue;
				result.Add(r);
			}
			if(result.Count > 0)
				return result;
			else return ValEmpty.VALUE;
		}
		throw new Exception();
	}
}
public class ExprApply : INode {
	public INode lhs;
	public ExprBlock rhs;
	public dynamic Eval(ValDictScope ctx) {
		var lhs = this.lhs.Eval(ctx);
		if(lhs is ValDictScope vds) {
			return rhs.Apply(new ValDictScope { locals = vds.locals, parent = ctx, temp = false });
		}
		if(lhs is IScope s) {
			//return block.Apply(s);
		} else if(lhs is object o) {
			//return block.Apply(new ValObjectScope { obj = o, parent = ctx });
		}
		throw new Exception();
	}
}
public class StmtReturn : INode {
	public int up = 1;
	public INode val;
	public dynamic Eval(ValDictScope ctx) =>
		new ValReturn(val.Eval(ctx), up);
}
public class ExprBlock : INode {
    public List<INode> statements;
    public XElement ToXML () => new ("Block", statements.Select(i => i.ToXML()));
	public string Source => $"{{{string.Join(", ", statements.Select(i => i.Source))}}}";
	public dynamic Eval(ValDictScope ctx) {
		return Apply(new ValDictScope(ctx, false));
	}
	public dynamic Apply(ValDictScope f) {
		dynamic r = ValEmpty.VALUE;
		foreach(var s in statements) {
			r = s.Eval(f);
			if(r is ValReturn vr) {
				if(vr.up > 1) {
					return vr with {
						up = vr.up - 1
					};
				}
				return vr.data;
			}
		}
		return f;
	}
	public dynamic MakeScope (ValDictScope ctx) => new ValDictScope(ctx, false);
	public dynamic StagedEval (ValDictScope ctx) => StagedApply(MakeScope(ctx));
	public dynamic StagedApply (ValDictScope f) {
		dynamic r = ValEmpty.VALUE;
		var stageA = () => { };
		var stageB = () => { };
		var stageD = new List<INode> { };
		foreach(var s in statements) {
			if(s is StmtKeyVal { value: ExprVarBlock { type: "class", source_block: {}block } } kv) {
				var _static = (ValDictScope)block.MakeScope(f);
				var c = new ValClass {
					name = kv.key,
					source_ctx = f,
					source_expr = block,
					_static = _static
				};
				f.locals[kv.key] = c;
				_static.locals["class"] = c;
				stageA += () => {
					block.StagedApply(_static);
				};
			} else {
				stageD.Add(s);
			}
		}
		stageA();
		foreach(var s in stageD) {
			if(s is StmtKeyVal { value: ExprVarBlock { type: "defer", source_block: { } _block } }) {
				r = _block.EvalDefer(f);
			} else {
				r = s.Eval(f);
			}
			if(r is ValReturn vr) {
				return vr.data;
			}
		}
		return f;
	}

	public dynamic EvalDefer(ValDictScope ctx) {
		return null;
	}
}
public class ExprSymbol : INode {
    public int up = -1;
    public string key;
    public XElement ToXML () => new("Symbol", new XAttribute("key", key), new XAttribute("level", $"{up}"));
	public string Source => $"{new string('^', up)}{key}";
	public dynamic Eval(ValDictScope ctx) {
		var r = ctx.Get(key, up);
		if(r is ValGetter vg) {
			r = vg.Eval();
		}
		return r;
	}
}
public class ExprSelf : INode {
	public int up;
	public dynamic Eval(ValDictScope ctx) {
		for(int i = 1; i < up; i++) {
			ctx = ctx.parent;
		}
		return ctx;
	}
}
public class ExprGet : INode {
	public INode src;
	public string key;
	public dynamic Eval(ValDictScope ctx) {
		var FL = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
		var source = src.Eval(ctx);
		if(source is ValDictScope s) {
			if(s.locals.TryGetValue(key, out var v)) {
				if(v is ValGetter vg) {
					v = vg.Eval();
				}
				return v;
			} else { throw new Exception($"Variable not found {key}"); }
		}
		if(source is ValClass vc) {
			return vc._static.locals.TryGetValue(key, out var v) ? v : throw new Exception("Variable not found");
		}
		if(source is Type t) {
			var FLS = BindingFlags.Static | BindingFlags.Public;
			if(key == "new") {
				return new ValConstructor(t);
			}
			if(t.GetField(key, FLS) is { } f) {
				return f.GetValue(null);
			}

			return new ValStaticMember(t, key);
		}
		if(source is object o) {
			var ot = o.GetType();
			if(ot.GetProperty(key, FL) is { } p) {
				return p.GetValue(o);
			}
			if(ot.GetField(key, FL) is { } f) {
				return f.GetValue(o);
			}
			return new ValInstanceMember(o, key);
			//return new ValMember(null, key);

			throw new Exception($"Unknown instance member {key}");
		}
		throw new Exception("Object expected");
	}
}
public class ExprIndex : INode {
	public INode src;
	public INode index;
	public dynamic Eval(ValDictScope ctx) {
		var scope = src.Eval(ctx);
		if(scope is IDictionary d) {
			var i = index.Eval(ctx);
			return d[i];
		}
		if(scope is IEnumerable e) {
			var ind = index.Eval(ctx);
			if(ind is int i) {
				return e.Cast<object>().ElementAt(i);
			}
		}

		{
			var ind = index.Eval(ctx);
			typeof(int).GetProperty("Item", [ind]);
		}
		throw new Exception("Sequence expected");
	}
}

public class ExprVal<T> : INode {
    public T value;
    public XElement ToXML () => new("Value", new XAttribute("value", value));
	public string Source => $"{value}";
	public dynamic Eval(ValDictScope ctx) {
		return value;
	}
}

public class ExprCondSeq : INode {
	public INode type;
	public INode filter;

	public List<(INode cond, INode yes, INode no)> items;
	public dynamic Eval(ValDictScope ctx) {
		var f = filter.Eval(ctx);
		var lis =(List<object>)(
			type == null ?
				new List<object> { } :
				(typeof(List<>).MakeGenericType(type.Eval(ctx)) as Type).GetConstructor([]).Invoke([])
				);
		foreach(var (cond, yes, no) in items) {
			var b = ExprInvoke.Invoke(ctx, f, new INode[] { cond });
			if(b == true) {
				if(yes != null) {
					var v = yes.Eval(ctx);
					if(v is ValEmpty) {
						continue;
					}
					lis.Add(v);
				}
			} else if(b == false) {
				if(no != null) {
					var v = no.Eval(ctx);
					if(v is ValEmpty) {
						continue;
					}
					lis.Add(v);
				}
			} else {
				throw new Exception("Boolean expected");
			}
		}
		var arr = lis.ToArray();
		return arr;
	}
}
public class ExprMatch : INode {
	public INode item;
	public List<(INode cond, INode yes)> branches;
	public dynamic Eval (ValDictScope ctx) {
		var subject = item.Eval(ctx);
		foreach(var (cond, yes) in branches) {
			var b = cond.Eval(ctx);
			if(Is(b)) {
				return yes.Eval(ctx);
			}
		}
		bool Is(dynamic pattern) {
			if(subject == pattern) {
				return true;
			}
			return false;
		}
		throw new Exception("Fell out of match expression");
	}
}
public class ExprMap : INode {
	public INode from;
	public INode map;
	public INode cond;
	public INode type;
	public XElement ToXML () => new("Map", from.ToXML(), map.ToXML());
	public string Source => $"{from.Source} | {map.Source}";
	public dynamic Eval (ValDictScope ctx) {
		var _from = from.Eval(ctx);
		if(_from is ValEmpty) {
			throw new Exception("Variable not found");
		}
		if(_from is ICollection c) {
			var result = new List<dynamic>();
			var f = map.Eval(ctx);
			foreach(var item in c) {
				if(cond != null) {
					var b = cond.Eval(ctx);
					if(b == true) {
						goto Do;
					}
					if(b == false) {
						break;
					}
					throw new Exception("Boolean expected");
				}

				Do:
				var r = ExprInvoke.Invoke(ctx, f, new List<INode?> { new ExprVal<dynamic> { value = item } });
				if(r is ValEmpty) {
					continue;
				}
				result.Add(r);
			}
			return Convert(result);
		} else if(_from is IEnumerable e) {

			var result = new List<dynamic>();
			var f = map.Eval(ctx);
			foreach(var item in e) {
				if(cond != null) {
					var b = cond.Eval(ctx);
					if(b == true) {
						goto Do;
					}
					if(b == false) {
						break;
					}
					throw new Exception("Boolean expected");
				}
				Do:
				var r = ExprInvoke.Invoke(ctx, f, new List<INode> { new ExprVal<dynamic> { value = item } });
				if(r is ValEmpty) {
					continue;
				}
				result.Add(r);
			}
			return Convert(result);
		} else {
			throw new Exception("Sequence expected");
		}
		
		dynamic Convert(List<dynamic> items) {
			var r = items.ToArray();
			return r;
		}
	}
}
public class StmtKeyVal : INode {
    public string key;
    public INode value;
    public XElement ToXML () => new("KeyVal", new XAttribute("key", key), value.ToXML());
	public string Source => $"{key}:{value?.Source ?? "null"}";
	public dynamic Eval(ValDictScope ctx) {
		var val = value.Eval(ctx);

		if(val is ValError ve) throw new Exception(ve.msg);

		if(value is ExprVarBlock { type: "class" }) {
			Set(ctx, key, val);
			return ValEmpty.VALUE;
		}
		return Init(ctx, key, val);
	}
	public static dynamic Init(ValDictScope ctx, string key, dynamic val) {
		if(val is Type t) {
			val = new ValDeclared { type = t };
		} else if(val is ValClass vc) {
			val = new ValDeclared { type = vc };
		}
		Set(ctx, key, val);
		return ValEmpty.VALUE;
	}
	public static void Set(ValDictScope ctx, string key, dynamic val) {
		ctx.locals[key] = val;
	}
}

public class ExprFunc : INode {
	public List<INode> pars;
	public INode result;
	public XElement ToXML () => new("ExprFunc", [.. pars.Select(i => i.ToXML()), result.ToXML()]);
	public string Source => $"@({string.Join(", ", pars.Select(p => p.Source))}) {result.Source}";
	public dynamic Eval (ValDictScope ctx) =>
		new ValFunc {
			expr = result,
			pars = pars.Select(p => p switch {
				StmtKeyVal kv => kv,
				ExprSymbol s => new StmtKeyVal { key = s.key, value = new ExprVal<Type> { value = typeof(object) } },
				_ => throw new Exception("Symbol or KeyVal expected")

			}).ToList(),
			parent_ctx = ctx
		};
}
public class ExprSeq : INode {
	public INode type;
	public List<INode> items;
	public dynamic Eval(ValDictScope ctx) {
		var src = items.Select(i => i.Eval(ctx)).ToArray();
		var _type = type?.Eval(ctx);
		if(_type is Type t) {

			var arr = Array.CreateInstance(t, src.Length);
			Array.Copy(src, arr, arr.Length);
			return arr;
		}
		return src;
	}
}
public class ExprTuple : INode {
	public List<INode> items;
	public dynamic Eval (ValDictScope ctx) {
		return null;
	}
}
public class StmtDefFunc : INode {
	public string key;
    public List<StmtKeyVal> pars;
	public INode value;
	public XElement ToXML () => new("DefineFunc", [new XAttribute("key", key), ..pars.Select(i => i.ToXML()), value.ToXML()]);
    public string Source => $"{key}({string.Join(", ",pars.Select(p => p.Source))}): {value.Source}";
	public dynamic Eval(ValDictScope ctx) {
		Define(ctx);
		return ValEmpty.VALUE;
	}
	public void Define(ValDictScope owner) {
		owner.locals[key] = new ValFunc {
			expr = value,
			pars = pars,
			parent_ctx = owner
		};
	}
}
public class StmtAssign : INode {
    public ExprSymbol symbol;
    public INode value;
    XElement ToXML () => new("Reassign", symbol.ToXML(), value.ToXML());
    public string Source => $"{symbol.Source} := {value.Source}";
	public dynamic Eval(ValDictScope ctx) {
		var inner_ctx = ctx.MakeTemp(ctx.Get(symbol.key, symbol.up));
		var r = Assign(ctx, symbol.key, symbol.up, () => value.Eval(inner_ctx));
		return r;
	}
	public static dynamic Assign (ValDictScope ctx, string key, int up, Func<object> getNext) {
		var curr = (object)ctx.Get(key, up);
		if(curr is ValError ve) {
			throw new Exception(ve.msg);
		}
		if(curr is ValDeclared vd) {
			return Match(vd.type);
		} else if(curr is ValClass vc) {
			return MatchClass(vc);
		} else if(curr is ValDictScope vds) {
			return ctx.Set(key, getNext(), up);
		} else {
			return MatchType(curr?.GetType());
		}
		dynamic Match(object type) {
			if(type is Type t) {
				return MatchType(t);
			} else if(type is ValClass vc) {
				return MatchClass(vc);
			}
			throw new Exception();
		}
		dynamic MatchClass(ValClass cl) {
			var next = getNext();
			if(next is ValClassScope vcs && vcs.fromClass == cl) {
				return ctx.Set(key, vcs, up);
			}
			if(next is ValDictScope vds) {
				return ctx.Set(key, vds, up);
			}

			if(next is ValError ve) {
				throw new Exception(ve.msg);
			}


			return ctx.Set(key, next, up);
			throw new Exception("Type mismatch");
		}
		dynamic MatchType(Type t) {
			var next = getNext();
			var nt = next.GetType();

			if(t == null) {
				goto TODO;
			}
			if(!t.IsAssignableFrom(nt)) {
				
				throw new Exception("Type mismatch");
			}



			if(next is ValError ve) {
				throw new Exception(ve.msg);
			}

			TODO:
			return ctx.Set(key, next, up);
		}
	}
}
public interface INode {
    XElement ToXML () => new(GetType().Name);

    String Source => "";

	dynamic Eval (ValDictScope ctx) => null;
}
public class Tokenizer {
    string src;
    int index;
    public Tokenizer(string src) {
        this.src = src;
    }

	public List<Token> GetAllTokens () {

		var tokens = new List<Token> { };
		while(Next() is { type: not TokenType.EOF } t) {
			tokens.Add(t);
		}
		return tokens;
	}

    public Token Next() {
        if(index >= src.Length) {
            return new Token { type = TokenType.EOF };
        }
        var str = (params char[] c) => string.Join("", c);


		void inc () => index += 1;
        Check:
        var c = src[index];


		if(c is '#') {
			inc();
			while(index < src.Length && src[index] != '\n') {
				inc();
			}
			goto Check;
		}
		if(c is '~') {
			inc();
			while(index < src.Length && src[index] != '~') {
				inc();
			}
			inc();
			goto Check;
		}


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
			'-' => TokenType.DASH,
			'/' => TokenType.SLASH,
			'%' => TokenType.PERCENT,
			'#' => TokenType.HASH,
			_ => default(TokenType?)
		} is { }tt) {
            index += 1;
			return new Token { type = tt, str = str(c) };
		}
		if(c is ' ' or '\r' or '\t' or '\n') {
			inc();
			goto Check;
		}


		if(c is >= '0' and <= '9') {
			int dest;
			for(dest = index + 1; dest < src.Length && src[dest] is >= '0' and <= '9'; dest++) {
			}
			var v = src[index..dest];
			index = dest;
			return new Token { type = TokenType.INTEGER, str = v };
		}

		bool isAlphanum (char c) => c is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9');

		if(isAlphanum(c)) {
			int dest = index + 1;
			while(dest < src.Length && isAlphanum(src[dest])) {
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
			var v = src[(index+1)..(dest-1)];
			index = dest;
			return new Token { type = TokenType.STRING, str = v };
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
	DASH,
    SLASH,

    SWIRL, QUERY, SHOUT, SPARK, PIPE, CASH, PERCENT, HASH,

	EOF
}
public class Token {
    public TokenType type;
    public string str;

    public string ToString () => $"[{type}] {str}";
}