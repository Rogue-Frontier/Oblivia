﻿// See https://aka.ms/new-console-template for more information
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Net.WebSockets;
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
	public ValTuple pars;
	public ValDictScope parent_ctx;
	public dynamic CallPars(ValDictScope caller_ctx, ExprTuple pars) {
		return CallFunc(caller_ctx, () => pars.EvalTuple(caller_ctx));
	}
	public dynamic CallArgs(ValDictScope caller_ctx, ValTuple args) {
		return CallFunc(caller_ctx, () => args);
	}
	public dynamic CallFunc (ValDictScope caller_ctx, Func<ValTuple> evalArgs) {
		var func_ctx = new ValDictScope(parent_ctx, false);
		var argData = new Args { };
		func_ctx.locals["_arg"] = argData;
		foreach(var (k, v) in pars.items) {
			StmtKeyVal.Init(func_ctx, k, v);
		}
		int ind = 0;
		foreach(var (k, v) in evalArgs().items) {
			if(k != null) {
				ind = -1;
				var val = StmtAssign.Assign(func_ctx, k, 1, () => v);
			} else {
				if(ind == -1) {
					throw new Exception("Cannot have positional arguments after named arguments");
				}
				var p = pars.items[ind];
				var val = StmtAssign.Assign(func_ctx, p.key, 1, () => v);
				ind += 1;
			}
		}
		ind = 0;
		foreach(var p in pars.items) {
			var val = func_ctx.locals[p.key];
			argData.list.Add(val);
			argData.dict[p.key] = val;

			StmtKeyVal.Init(func_ctx, $"_{ind}", val);
			ind += 1;
		}
		var result = expr.Eval(func_ctx);
		return result;
	}
	public dynamic CallData (ValDictScope caller_ctx, IEnumerable<object> args) {
		var func_ctx = new ValDictScope(parent_ctx, false);
		var argData = new Args { };
		func_ctx.locals["_arg"] = argData;
		foreach(var (k,v) in pars.items) {
			//var val = v.Eval(caller_ctx);
			StmtKeyVal.Init(func_ctx, k, v);
		}
		int ind = 0;
		foreach(var arg in args) {
			var p = pars.items[ind];
			var val = StmtAssign.Assign(func_ctx, p.key, 1, () => arg);
			ind += 1;
		}
		ind = 0;
		foreach(var p in pars.items) {
			var val = func_ctx.locals[p.key];
			argData.list.Add(val);
			argData.dict[p.key] = val;
			StmtKeyVal.Init(func_ctx, $"_{ind}", val);
			ind += 1;
		}
		var result = expr.Eval(func_ctx);
		return result;
	}
	public dynamic ApplyData (ValDictScope caller_ctx, ValDictScope target_ctx, IEnumerable<object> args) {
		var func_ctx = new ValDictScope(parent_ctx, false);
		var argData = new Args { };
		func_ctx.locals["_arg"] = argData;
		foreach(var p in pars.items) {
			var val = p.val.Eval(caller_ctx);
			StmtKeyVal.Init(func_ctx, p.key, val);
		}
		int ind = 0;
		foreach(var arg in args) {
			var p = pars.items[ind];
			var val = StmtAssign.Assign(func_ctx, p.key, 1, () => arg);
			ind += 1;
		}
		ind = 0;
		foreach(var p in pars.items) {
			var val = func_ctx.locals[p.key];
			argData.list.Add(val);
			argData.dict[p.key] = val;

			StmtKeyVal.Init(func_ctx, $"_{ind}", val);
			ind += 1;
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
		if(nextType == null) return next;
		if(nextType != type) {
			//return ValError.TYPE_MISMATCH;
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

	public ValDictScope MakeTemp () => new ValDictScope {
		locals = {},
		parent = this,
		temp = true
	};
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
		switch(tokenType) {

			case TokenType.PIPE: {
					inc();
					var cond = default(INode);
					var type = default(INode);
					switch(tokenType) {
						case TokenType.L_ANGLE: {

								inc();
								cond = NextExpression();
								switch(tokenType) {
									case TokenType.R_ANGLE:
										inc();
										break;
									default:
										throw new Exception("Closing expected");
								}
								break;
							}
					}
					switch(tokenType) {
						case TokenType.COLON: {
								inc();
								type = NextExpression();
								break;
							}
					}
					return NextExpression(new ExprMapFunc { src = lhs, cond = cond, type = type, map = NextExpression() });
				}
			case TokenType.L_PAREN: {
					inc();
					return NextExpression(new ExprInvoke { expr = lhs, args = NextArgTuple() });
				}
			case TokenType.SPARK: {
					return NextSpark(lhs);
				}
			case TokenType.DASH: {
					inc();
					return NextExpression(new ExprInvoke { expr = lhs, args = new ExprTuple { items = [(null, NextTerm())] } });
				}
			case TokenType.SHOUT: {
					inc();
					return NextExpression(new ExprInvoke { expr = lhs, args = new ExprTuple { items = [] } });
				}
			case TokenType.PERCENT: {
					inc();
					return NextExpression(new ExprSpread { value = lhs });
				}
			case TokenType.EQUAL: {
					inc();
					var Make = (bool invert) => NextExpression(new ExprEqual {
						lhs = lhs,
						rhs = NextTerm(),
						invert = invert
					});

					switch(tokenType) {
						case TokenType.PLUS: {
								inc();
								return Make(false);
							}
						case TokenType.DASH: {
								inc();
								return Make(true);
							}
					}
					throw new Exception();
				}
			case TokenType.SLASH: {
					inc();

					switch(tokenType) {
						case TokenType.NAME:

							var name = currToken.str;
							inc();
							return NextExpression(new ExprGet { src = lhs, key = name });
						case TokenType.SLASH:
							inc();
							return NextExpression(new ExprMapExpr { src = lhs, map = NextExpression() });
						default: {
								return NextExpression(new ExprApply { lhs = lhs, rhs = NextExpression() });
							}
					}
				}
			case TokenType.SWIRL: {
					inc();
					var index = NextTerm();
					return NextExpression(new ExprIndex { src = lhs, index = [index] });
				}
			case TokenType.L_SQUARE: {
					inc();
					List<INode> index = new List<INode> { NextExpression() };
					Check:

					switch(tokenType) {
						case TokenType.R_SQUARE: {

								inc();
								return NextExpression(new ExprIndex { src = lhs, index = index });
							}

					}
					index.Add(NextExpression());
					goto Check;
				}
			case TokenType.QUERY: {
					inc();
					switch(tokenType) {
						case TokenType.PIPE: {
								inc();
								var cond = NextExpression();
								return new ExprCond { item = lhs, cond = cond };
							}
						case TokenType.L_CURLY: {
								inc();
								var items = new List<(INode cond, INode yes)> { };
								ReadBranch:
								switch(tokenType) {
									case TokenType.R_CURLY:
										inc();
										return NextExpression(new ExprPatternMatch {
											item = lhs,
											branches = items
										});
								}


								var cond_group = new List<INode> { };
								ReadItem:
								cond_group.Add(NextExpression());
								/*
								if(cond is ExprFunc ef) {
									//Handle lambda
									//If this lambda accepts this object as argument, then treat it as a branch.
									goto ReadBranch;
								}
								*/

								switch(tokenType) {
									case TokenType.COLON:
										inc();
										var yes = NextExpression();

										foreach(var c in cond_group) {
											items.Add((c, yes));
										}
										goto ReadBranch;
									default:
										goto ReadItem;
								}
							}
						case (TokenType.L_SQUARE): {
								inc();
								INode type = null;
								switch(tokenType) {
									case TokenType.COLON:
										inc();
										type = NextExpression();
										break;
								}

								var items = new List<(INode cond, INode yes, INode no)> { };
								Read:
								var ante = NextExpression();
								switch(tokenType) {
									case TokenType.COLON: {
											inc();
											var cons = NextExpression();
											items.Add((ante, cons, null));
											break;
										}
									default:
										throw new Exception();
								}
								switch(tokenType) {
									case TokenType.R_SQUARE: {
											inc();
											return NextExpression(new ExprCondSeq {
												type = type,
												filter = lhs,
												items = items
											});
										}
									default:
										goto Read;
								}
							}
						case (TokenType.PLUS): {
								inc();
								var positive = NextExpression();
								var negative = default(INode);

								switch(tokenType) {
									case TokenType.QUERY: {
											inc();
											switch(tokenType) {
												case TokenType.DASH: {

														inc();
														negative = NextExpression();
														break;
													}
												default: {
														dec(); break;
													}
											}
										}
										break;
								}
								return NextExpression(new ExprBranch {
									condition = lhs,
									positive = positive,
									negative = negative
								});
							}
						case (TokenType.SPARK): {
								inc();
								return NextExpression(new ExprLoop { condition = lhs, positive = NextExpression() });
							}
						default:
							dec();

							break;
					}

					break;
				}
		}
		/*
		if(t == TokenType.PERCENT) {
			inc();
			if(tokenType == TokenType.L_CURLY) {
				return NextExpression(new ExprApply { lhs = lhs, rhs = NextBlock() });
			} else {
				//return NextExpression(new ExprApplyBlock { lhs = lhs, rhs = NextExpression() });
				throw new Exception("Expected block");
			}
		}
		*/

		return lhs;
	}
	INode NextTerm () {
		switch(tokenType) {
			case TokenType.L_SQUARE:
				return NextArray();
			case TokenType.QUERY:
				return NextLambda();
			case TokenType.NAME:
				return NextSymbolOrBlock();
			case TokenType.CARET:
				return NextSymbolOrReturn();
			case TokenType.STRING:
				return NextString();
			case TokenType.INTEGER:
				return NextInteger();
			case TokenType.L_CURLY:
				return NextBlock();
			case TokenType.L_PAREN:
				return NextTupleOrExpression();
		}
		throw new Exception($"Unexpected token in expression: {currToken.type}");
	}
	INode NextTupleOrExpression () {
		inc();
		var expr = NextExpression();
		switch(tokenType) {
			case TokenType.R_PAREN: {
					inc();
					return expr;
				}
			default:
				return NextTuple(expr);
		}
		
	}

	ExprTuple NextArgTuple () {
		switch(tokenType) {
			case TokenType.R_PAREN:
				return new ExprTuple { items = [] };
			default:
				return NextTuple(NextExpression());
		}
	}
	ExprTuple NextTuple (INode first) {
		var items = new List<(string key, INode val)> { };
		return AddEntry(first);
		ExprTuple AddEntry (INode lhs) {
			switch(tokenType) {
				case TokenType.COLON:
					NextPair(lhs);
					break;
				default:
					items.Add((null, lhs));
					break;
			}

			switch(tokenType) {
				case TokenType.R_PAREN:
					inc();
					return new ExprTuple { items = items.ToArray() };
				default:
					return AddEntry(NextExpression());
			}
		}
		void NextPair (INode lhs) {
			if(lhs is ExprSymbol { up: -1, key: { } key }) {
				inc();
				var val = NextExpression();
				items.Add((key, val));
			} else {
				throw new Exception("Name expected");
			}
		}
	}
	INode NextArray() {
		List<INode> items = [];
		inc();
		INode type = null;
		switch(tokenType) {
			case TokenType.COLON:
				inc();
				type = NextTerm();
				break;
		}
		Check:
		
		switch(tokenType) {
			case TokenType.COMMA:
				inc();
				goto Check;
			case TokenType.R_SQUARE:


				inc();
				return new ExprSeq { items = items, type = type };
			default:
				items.Add(NextExpression());
				goto Check;
		}
	}
	INode NextLambda () {
		inc();

		var t = tokenType;
		switch(t) {
			case TokenType.SHOUT: {

					inc();
					var result = NextExpression();
					return new ExprFunc { pars = new ExprTuple { items = [] }, result = result };
				}
			case TokenType.L_PAREN:
				inc();
				return new ExprFunc { pars = NextArgTuple().ParTuple(), result = NextExpression() };
			default:
				throw new Exception($"Unexpected token {t}");
		}
	}
	INode NextSymbolOrBlock () {
		//May be cast object, variable, or a function call / literal.
		var name = currToken.str;
		inc();
		switch(tokenType) {
			case TokenType.L_CURLY:
				return new ExprVarBlock { type = name, source_block = NextBlock() };
			default:
				return new ExprSymbol { key = name };
		}		
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
				throw new Exception("Multi-level return must be assignment");
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
				return NextExpression(new ExprApply { lhs = new ExprSelf { up = up }, rhs = NextExpression() });
			}
		}
		if(t == TokenType.NAME) {
			var s = new ExprSymbol { up = up, key = currToken.str };
			inc();
			return s;
		}
		throw new Exception($"Unexpected token in up-symbol {currToken.type}");
	}
	public ExprVal NextString () {
        var value = tokens[index].str;
        inc();
        return new ExprVal { value = value };
    }
    public ExprVal NextInteger() {
        var value = int.Parse(tokens[index].str);
        inc();
        return new ExprVal { value = value };
    }
	public ExprBlock NextBlock() {
        inc();
		var ele = new List<INode>();
        Check:
		switch(tokenType) {
			case TokenType.R_CURLY:
				inc();
				return new ExprBlock { statements = ele };
			case TokenType.COMMA:
				inc();
				goto Check;
			case TokenType.L_PAREN:
				ele.Add(NextTupleOrExpression());
				goto Check;
			case TokenType.CARET:
				ele.Add(NextReassignOrExpressionOrReturn());
				goto Check;
			case TokenType.NAME:
				ele.Add(NextStatementOrExpression());
				goto Check;
			case TokenType.L_SQUARE:
				ele.Add(NextExpression());
				goto Check;
			case TokenType.HASH:
				inc();
				NextTerm();
				goto Check;
		}
        throw new Exception($"Unexpected token in object expression: {currToken.type}");
	}
    INode NextReassignOrExpressionOrReturn () {
		var symbol = NextSymbolOrReturn();
		if(symbol is StmtReturn) return symbol;

		switch(tokenType) {
			case TokenType.COMMA:
				return symbol;
			case TokenType.COLON:
				inc();
				switch(tokenType) {
					case TokenType.EQUAL:
						inc();
						return new StmtAssign { symbol = (ExprSymbol)symbol, value = NextExpression() };
				}
				break;
			case TokenType.L_PAREN:
				inc();
				return NextExpression(new ExprInvoke { expr = symbol, args = NextArgTuple() });
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
	INode NextSpark(INode lhs) {
		inc();
		return NextExpression(new ExprInvoke {
			expr = lhs,
			args = new ExprTuple { items = [(null, new ExprSpread { value = NextExpression() })] },
		});
	}
	INode NextFuncOrExpression (string name) {
		var t = tokenType;
		switch(tokenType) {
			case TokenType.SPARK: {
					return NextSpark(new ExprSymbol { key = name });
				}
			case TokenType.SHOUT: {
					inc();
					switch(tokenType) {
						case TokenType.COLON: {
								inc();
								return new StmtDefFunc {
									key = name,
									pars = new ExprTuple { items = [] },
									value = NextExpression()
								};
							}
						default:
							return NextExpression(new ExprInvoke {
								expr = new ExprSymbol { key = name },
								args = new ExprTuple { items = [] },
							});
					}
				}
			default: {

					inc();
					var args = NextArgTuple();
					t = currToken.type;
					switch(tokenType) {
						case TokenType.COLON: {
								inc();
								var pars = args.ParTuple();
								return new StmtDefFunc { key = name, pars = pars, value = NextExpression() };
							}
						default:
							return NextExpression(new ExprInvoke { expr = new ExprSymbol { key = name }, args = args });
					}
				}

		}
	}
}
public class ExprSpread : INode {
	public INode value;
	public dynamic Eval(ValDictScope ctx) {
		if(value is ExprSeq s) {
			return new ValSpread { value = s.items.Select(i => i.Eval(ctx)).ToArray() };
		}

		var val = value.Eval(ctx);
		switch(val) {
			case ValTuple vt:
				return new ValSpread { value = vt };
			case ValEmpty:
				return ValEmpty.VALUE;
			case null: return null;
			default: return val;
		}
		throw new Exception("Tuple or array or record expected");
		return val;
	}
}
public class ValSpread {
	public dynamic value;

	public void Spread(string key, List<(string key, dynamic val)> it) {
		switch(value) {
			case ValTuple vrt:
				vrt.Spread(it);
				break;
			case ValEmpty:
				break;
			default:
				it.Add((key, value));
				break;
		}
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
		switch(t) {
			case ValEmpty: throw new Exception("Type not found");
			case Type tt: {
				var r = getResult();
				return new ValType(tt).Cast(r, r?.GetType());
			}
			case ValClass vc: {
					return vc.VarBlock(ctx, source_block);
				}
			case ValInterface vi: {
					break;
				}
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
public class ExprCond : INode {
	public INode item;
	public INode cond;
	public dynamic Eval (ValDictScope ctx) {
		var l = item.Eval(ctx);
		var inner_ctx = ctx.MakeTemp(l);
		inner_ctx.locals["_var"] = item;
		var r = cond.Eval(inner_ctx);
		switch(r) {
			case bool:
				return r;
			default:
				throw new Exception();
		}
	}
}
public class ExprBranch : INode {
	public INode condition;
	public INode positive;
	public INode negative;
	public string Source => $"{condition.Source} ?+ {positive.Source}{(negative != null ? $" ?- {negative.Source}" : $"")}";
	public dynamic Eval(ValDictScope ctx) {
		var cond = condition.Eval(ctx);
		switch(cond) {
			case true:
				return positive.Eval(ctx);
			default:
				switch(negative) {
					case null:
						return ValEmpty.VALUE;
					default:
						return negative.Eval(ctx);
				}
		}
	}
}
public class ExprLoop: INode {
	public INode condition;
	public INode positive;
	public dynamic Eval(ValDictScope ctx) {
		dynamic r = ValEmpty.VALUE;
		Step:
		var cond = condition.Eval(ctx);
		switch(cond) {
			case true:
				r = positive.Eval(ctx);
				goto Step;
			default:
				throw new Exception("Boolean expected");
		}
		return r;
	}
}
public class ExprInvoke : INode {
    public INode expr;
	public ExprTuple args;
	//public string Source => $"{expr.Source}{(args.Count > 1 ? $"({string.Join(", ", args.Select(a => a.Source))})" : args.Count == 1 ? $"*{args.Single().Source}" : $"!")}";
	public dynamic Eval(ValDictScope ctx) {
		if(expr is ExprSymbol { key: "decl", up: -1 }) {
		}
		if(expr is ExprSymbol { key:"get", up: -1 }) {
			return new ValGetter { ctx = ctx, expr = args.items.Single().value };
		}
		return InvokePars(ctx, expr.Eval(ctx), args);
	}
	public static object GetReturnType(object f) {
		throw new Exception("Implement");
	}
	public static dynamic InvokePars(ValDictScope ctx, object lhs, ExprTuple pars) =>
		InvokeFunc(ctx, lhs, () => pars.EvalTuple(ctx));
	public static dynamic InvokeArgs (ValDictScope ctx, object lhs, ValTuple args) =>
		InvokeFunc(ctx, lhs, () => args);
	public static dynamic InvokeFunc (ValDictScope ctx, object lhs, Func<ValTuple> evalArgs) {
		switch(lhs) {
			case ValEmpty: {
					throw new Exception("Function not found");
				}
			case ValError ve: {
					throw new Exception(ve.msg);
				}
			case ValConstructor vc: {
					var rhs = evalArgs().items.Select(pair => pair.val).ToArray();
					var c = vc.t.GetConstructor(rhs.Select(arg => (arg as object).GetType()).ToArray());
					if(c == null) {
						throw new Exception("Constructor not found");
					}
					return c.Invoke(rhs);
				}
			case ValInstanceMember vim: {
					var vals = evalArgs().items.Select(pair => pair.val).ToArray();
					return vim.Call(vals);
				}
			case ValStaticMember vsm: {
					var vals = evalArgs().items.Select(pair => pair.val).ToArray();
					return vsm.Call(vals);
				}
			case Type tl: {
					var d = evalArgs().items.Single().val;
					return new ValType(tl).Cast(d, d?.GetType());
				}
			case ValTuple vt: {
					//var parTypes = vt.items.OfType<Type>().ToArray();
					var argArr = evalArgs();
					var argTypes = argArr.items.Select(a => (Type)a.GetType()).ToArray();
					var tt = argTypes.Length switch {
						2 => typeof(ValueTuple<,>),
						3 => typeof(ValueTuple<,,>),
						4 => typeof(ValueTuple<,,,>),
						5 => typeof(ValueTuple<,,,,>),
						6 => typeof(ValueTuple<,,,,,>),
						7 => typeof(ValueTuple<,,,,,,>),
						8 => typeof(ValueTuple<,,,,,,,>),
					};
					var aa = (object)(argArr.items switch {
					[{ } a, { } b] => (a, b),
					[{ } a, { } b, { } c] => (a, b, c),
					[{ } a, { } b, { } c, { } d] => (a, b, c, d),
					[{ } a, { } b, { } c, { } d, { } e] => (a, b, c, d, e),
					[{ } a, { } b, { } c, { } d, { } e, { } f] => (a, b, c, d, e, f),
					[{ } a, { } b, { } c, { } d, { } e, { } f, { } g] => (a, b, c, d, e, f, g),
					[{ } a, { } b, { } c, { } d, { } e, { } f, { } g, { } h] => (a, b, c, d, e, f, h)
					});
					var at = tt.MakeGenericType(argTypes);
					return new ValType(tt.MakeGenericType(argTypes)).Cast(aa, at);
				}
			case ValFunc vf: {
					return vf.CallFunc(ctx, evalArgs);
				}
			case Delegate de: {
					var r = de.DynamicInvoke(evalArgs().items.Select(pair => pair.val).ToArray());
					if(de.Method.ReturnType == typeof(void))
						return ValEmpty.VALUE;
					return r;
				}
			case ValClass vcl: {
					//Cast
					break;
				}
			case ValDictScope s: {
				throw new Exception("Illegal");
			}
		}
		throw new Exception($"Unknown function {lhs}");
	}
}
public class ExprApply : INode {
	public INode lhs;
	public INode rhs;
	public dynamic Eval(ValDictScope ctx) {
		switch(lhs.Eval(ctx)) {
			case ValDictScope vds:
				switch(rhs) {
					case ExprBlock eb:
						return eb.Apply(new ValDictScope { locals = vds.locals, parent = ctx, temp = false });
					default:
						return rhs.Eval(new ValDictScope { locals = vds.locals, parent = ctx, temp = false });
				}
		}
		/*
		if(lhs is IScope s) {
			//return block.Apply(s);
		} else if(lhs is object o) {
			//return block.Apply(new ValObjectScope { obj = o, parent = ctx });
		}
		*/
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
			switch(r) {
				case ValReturn vr:
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
			switch(r) {
				case ValReturn vr:
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

		switch(source) {
			case ValDictScope s: {

				if(s.locals.TryGetValue(key, out var v)) {
					if(v is ValGetter vg) {
						v = vg.Eval();
					}
					return v;
				} else {
					throw new Exception($"Variable not found {key}");
				}
			}
			case ValClass vc: {
					return vc._static.locals.TryGetValue(key, out var v) ? v : throw new Exception("Variable not found");
				}
			case Type t: {

					var FLS = BindingFlags.Static | BindingFlags.Public;
					if(key == "new") {
						return new ValConstructor(t);
					}
					if(t.GetField(key, FLS) is { } f) {
						return f.GetValue(null);
					}
					return new ValStaticMember(t, key);
				}
			case Args a: {
					return a[key];
				}
			case object o: {

					var ot = o.GetType();
					if(ot.GetProperty(key, FL) is { } p) {
						return p.GetValue(o);
					}
					if(ot.GetField(key, FL) is { } f) {
						return f.GetValue(o);
					}
					return new ValInstanceMember(o, key);
				}
		}
		throw new Exception("Object expected");
	}
}
public class ExprIndex : INode {
	public INode src;
	public List<INode> index;
	public dynamic Eval(ValDictScope ctx) {
		var call = src.Eval(ctx);


		switch(call) {
			case IDictionary d: {
					var i = index.Single().Eval(ctx);
					return d[i];
				}
			case IEnumerable e: {
					var ind = index.Single().Eval(ctx);
					if(ind is int i) {
						return e.Cast<object>().ElementAt(i);
					} else {
						throw new Exception("");
					}
				}
			case Args a: {
					var ind = index.Single().Eval(ctx);
					if(ind is string s) return a[s];
					if(ind is int i) return a[i];
					throw new Exception();
				}
			case ValFunc vf: {
				//return vf.Call(ctx, new ValRealTuple { items = [(null, index.Select(i => i.Eval(ctx)).ToArray())] });

				throw new Exception("ugly");
				}
			case Delegate de:{
					return de.DynamicInvoke([index.Select(a => a.Eval(ctx)).ToArray()]);
				}
			case ValTuple vt:{
					return (((string key, dynamic val))vt.items.GetValue(index.Select(i => (int)i.Eval(ctx)).ToArray())).val;
				}
		}
		/*
		if(false){
			var ind = index.Eval(ctx);
			typeof(int).GetProperty("Item", [ind]);
		}
		*/
		throw new Exception("Sequence expected");
	}
}
public class ExprVal : INode {
    public object value;
    public XElement ToXML () => new("Value", new XAttribute("value", value));
	public string Source => $"{value}";
	public dynamic Eval(ValDictScope ctx) => value;
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
			var c = cond.Eval(ctx);
			var b = ExprInvoke.InvokeArgs(ctx, f, c switch {
				ValTuple vrt => vrt,
				_ => new ValTuple { items = [(null, c)] }
			});
			switch(b) {
				case true: {

						if(yes != null) {
							var v = yes.Eval(ctx);
							if(v is ValEmpty) {
								continue;
							}
							lis.Add(v);
						}
						continue;
					}
				case false: {
						if(no != null) {
							var v = no.Eval(ctx);
							if(v is ValEmpty) {
								continue;
							}
							lis.Add(v);
						}
						continue;
					}
				default:
					throw new Exception("Boolean expected");

			}
		}
		var arr = lis.ToArray();
		return arr;
	}
}
public class ExprPatternMatch : INode {
	public INode item;
	public List<(INode cond, INode yes)> branches;
	public dynamic Eval (ValDictScope ctx) {
		var subject = item.Eval(ctx);

		var inner_ctx = ctx.MakeTemp(subject);
		inner_ctx.locals["_default"] = subject;
		foreach(var (cond, yes) in branches) {
			var b = cond.Eval(inner_ctx);
			if(Is(b)) {
				return yes.Eval(inner_ctx);
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
public class ExprMapFunc : INode {
	public INode src;
	public INode map;
	public INode cond;
	public INode type;
	public XElement ToXML () => new("Map", src.ToXML(), map.ToXML());
	public string Source => $"{src.Source} | {map.Source}";
	public dynamic Eval (ValDictScope ctx) {
		switch(src.Eval(ctx)) {
			case ValEmpty:
				throw new Exception("Variable not found");
			case ICollection c:
				return Map(c);
			case IEnumerable e:
				return Map(e);
			case ValTuple vt:
				var keys = vt.items.Select(i => i.key);
				var vals = vt.items.Select(i => i.val);
				var m = (IEnumerable<dynamic>)Map(vals);
				var r = new ValTuple { items = keys.Zip(m).ToArray() }; ;
				return r;
			default:
				throw new Exception("Sequence expected");
		}
		dynamic Map (dynamic seq) {
			var result = new List<dynamic>();
			var f = map.Eval(ctx);

			int index = 0;
			foreach(var item in seq) {
				var inner_ctx = ctx.MakeTemp();
				inner_ctx.locals["_item"] = item;
				inner_ctx.locals["_index"] = index;
				index++;
				if(cond != null) {
					switch(cond.Eval(ctx)) {
						case true:
							goto Do;
						case false:
							goto Done;
						default:
							throw new Exception("Boolean expected");
					}
				}
				Do:
				var r = ExprInvoke.InvokeArgs(inner_ctx, f, item switch {
					ValTuple vt => vt,
					_ => new ValTuple{ items = [(null, item)] }
				});
				switch(r) {
					case ValEmpty:
						continue;
					case ValReturn vr:
						if(vr.up > 1) {
							vr = vr with { up = vr.up - 1 };
						}
						return vr;
					default:
						result.Add(r);
						continue;
				}
			}
			Done:
			return Convert(result);
		}
		IEnumerable<dynamic> Convert(List<dynamic> items) {
			var r = items.ToArray();

			switch(type) {
				case null:
					return r;
				default: {
						var arr = Array.CreateInstance(type.Eval(ctx), r.Length);
						Array.Copy(r, arr, r.Length);
						return arr;
					}
			}
		}
	}
}






public class ExprMapExpr : INode {
	public INode src;
	public INode map;

	public INode cond;
	public INode type;

	public XElement ToXML () => new("Map", src.ToXML(), map.ToXML());
	public string Source => $"{src.Source} | {map.Source}";
	public dynamic Eval (ValDictScope ctx) {
		switch(src.Eval(ctx)) {
			case ValEmpty:
				throw new Exception("Variable not found");
			case ICollection c:
				return Map(c);
			case IEnumerable e:
				return Map(e);
			default:
				throw new Exception("Sequence expected");
		}

		dynamic Map (dynamic seq) {
			var result = new List<dynamic>();
			int index = 0;
			foreach(var item in seq) {
				var inner_ctx = ctx.MakeTemp();
				inner_ctx.locals["_item"] = item;
				inner_ctx.locals["_index"] = index;
				index++;
				if(cond != null) {
					var b = cond.Eval(ctx);
					if(b == true) {
						goto Do;
					} else if(b == false) {
						break;
					} else { 
						throw new Exception("Boolean expected"); 
					}
				}
				Do:
				var r = map.Eval(inner_ctx);
				switch(r) {
					case ValEmpty:
						continue;
					default:
						result.Add(r);
						break;
				}
			}
			return Convert(result);
		}
		dynamic Convert (List<dynamic> items) {
			var r = items.ToArray();
			if(type != null) {
				var arr = Array.CreateInstance(type.Eval(ctx), r.Length);
				Array.Copy(r, arr, r.Length);
				return arr;
			}
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
		switch(val) {
			case Type t:
				val = new ValDeclared { type = t };
				break;
			case ValClass vc:
				val = new ValDeclared { type = vc };
				break;
		}

		Set(ctx, key, val);
		return ValEmpty.VALUE;
	}
	public static void Set(ValDictScope ctx, string key, dynamic val) {
		ctx.locals[key] = val;
	}
}

public class ExprFunc : INode {
	public ExprTuple pars;
	public INode result;
	//public XElement ToXML () => new("ExprFunc", [.. pars.Select(i => i.ToXML()), result.ToXML()]);
	//public string Source => $"@({string.Join(", ", pars.Select(p => p.Source))}) {result.Source}";
	public dynamic Eval (ValDictScope ctx) =>
		new ValFunc {
			expr = result,
			pars = pars.EvalTuple(ctx),
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
	public (string key, INode value)[] items;
	public ValTuple EvalTuple (ValDictScope ctx) {
		var it = new List<(string key, dynamic val)> { };
		foreach(var(key, val) in items) {
			var v = val.Eval(ctx);
			switch(v) {
				case ValSpread vs:
					vs.Spread(key, it);
					break;
				case ValEmpty:
					break;
				default:
					it.Add((key, v));
					break;
			}
		}
		return new ValTuple { items = it.ToArray() };
	}
	public dynamic Eval (ValDictScope ctx) => EvalTuple(ctx);
	public void Spread(ValDictScope ctx, List<(string key, dynamic val)> it) {
		foreach(var(key,val) in items) {
			it.Add((key, val.Eval(ctx)));
		}
	}
	public ExprTuple ParTuple () =>
		new ExprTuple {
			items = items.Select(pair => {
				if(pair.key == null) {
					if(pair.value is ExprSymbol { up: -1, key: { } key }) {
						return (key, new ExprVal { value = typeof(object) });
					}
					throw new Exception("Expected");
				} else {
					return pair;
				}
			}).ToArray()
		};
}
public class ValTuple : INode {
	public (string key, dynamic val)[] items;
	public dynamic Eval (ValDictScope ctx) => this;
	public void Spread (List<(string key, dynamic val)> it) {
		foreach(var (key, val) in items) {

			it.Add((key, val));
		}
	}

	public ExprTuple expr => new ExprTuple {
		items = items.Select(pair => (pair.key, (INode)new ExprVal { value = pair.val})).ToArray()
	};
}
public class StmtDefFunc : INode {
	public string key;
    public ExprTuple pars;
	public INode value;
	//public XElement ToXML () => new("DefineFunc", [new XAttribute("key", key), ..pars.Select(i => i.ToXML()), value.ToXML()]);
    //public string Source => $"{key}({string.Join(", ",pars.Select(p => p.Source))}): {value.Source}";
	public dynamic Eval(ValDictScope ctx) {
		Define(ctx);
		return ValEmpty.VALUE;
	}
	public void Define(ValDictScope owner) {
		owner.locals[key] = new ValFunc {
			expr = value,
			pars = pars.EvalTuple(owner),
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
		var curr = ctx.Get(symbol.key, symbol.up);
		var inner_ctx = ctx.MakeTemp(curr);
		inner_ctx.locals["_curr"] = curr;
		var r = Assign(ctx, symbol.key, symbol.up, () => value.Eval(inner_ctx));
		return r;
	}
	public static dynamic Assign (ValDictScope ctx, string key, int up, Func<object> getNext) {
		var curr = (object)ctx.Get(key, up);
		switch(curr) {
			case ValError ve: 
				throw new Exception(ve.msg);
			case ValDeclared vd: 
				return Match(vd.type);
			case ValClass vc: 
				return MatchClass(vc);
			case ValDictScope vds: 
				return ctx.Set(key, getNext(), up);
			default: 
				return MatchType(curr?.GetType());

		}

		dynamic Match(object type) {
			switch(type) {
				case Type t:
					return MatchType(t);
				case ValClass vc:
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
			if(t == null) {
				goto TODO;
			}
			var nt = next.GetType();
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