using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Xml.Linq;
using static Oblivia.ExMap;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Oblivia;



public record ExGet : Node {
	public Node get;
	public object Eval (IScope ctx) => new VGet { ctx = ctx, get = get };
}
public record ExPointer : Node {
	public Node expr;
	public object Eval (IScope ctx) {
		var val = expr.Eval(ctx);
		return new VPointer { value = val };
	}
}
public record ExAlias : Node {
	public Node expr;
	public object Eval (IScope ctx) => new VAlias { ctx = ctx, expr = expr };
}
public record ExRef : Node {
	public Node expr;
	public object Eval (IScope ctx) => null;
}
public class ExTemp : Node {
	public Node lhs;
	public Node rhs;
	public object Eval (IScope ctx) {
		var v = lhs.Eval(ctx);
		var sc = ctx.MakeTemp();
		sc.locals["_"] = v;
		return rhs.Eval(sc);
	}
}
public class ExInterpolate : Node {
	public List<Node> parts;
	public object Eval (IScope ctx) {
		var result = "";
		foreach(var p in parts) {
			result += p.Eval(ctx);
		}
		return result;
	}
}
public class ExFnType : Node {
	public Node lhs = ExVal.Empty;
	public Node rhs = ExVal.Empty;
	public object Eval (IScope ctx) {
		var l = lhs.Eval(ctx);
		var r = rhs.Eval(ctx);
		return new VFnInterface {
			lhs = (IType[])l,
			rhs = (IType)r
		};
	}
}
public class ExRange : Node {
	public Node lhs = ExVal.Empty;
	public Node rhs = ExVal.Empty;
	public object Eval (IScope ctx) {
		return null;
	}
}
public class ExSeqOp : Node {
	public enum EOp {
		Reduce, SlidingWindow
	};
	public EOp op;
	public Node fn;
	public object Eval (IScope ctx) {
		throw new NotImplementedException();
	}
}
public class ExSpread : Node {
	public Node value;

	/*
	public IEnumerable<LVal> SpreadVals () {
		switch(value) {
			case ExBlock eb:

			case ExTuple et:
			case ExUpKey:
				case ExMemberKey
		}
	}
	*/
	public object Eval (IScope ctx) {
		//TODO: alias
		return Handle(value.Eval(ctx));
		object Handle (object val) {
			switch(val) {
				case VTuple t:
					return new VSpread { value = t };
				case Array a:
					return new VSpread { value = a };
				case VArgs args:
					return new VSpread { value = args };
				case VAlias va:
					//TODO: fix
					return Handle(va.Get());
				default: return val; throw new Exception("Tuple or array or record expected");
			}
		}
	}
}
public class ExInvokeBlock : Node {
	public Node type;
	public ExBlock source_block;
	public bool attribute;
	public XElement ToXML () => new("VarBlock", new XAttribute("type", type), source_block.ToXML());
	public string Source => $"{type} {source_block.Source}";
	public object MakeScope (VDictScope ctx) => source_block.MakeScope(ctx);
	public object Eval (IScope ctx) {
		var evalSrc = () => source_block.Eval(ctx);
		var t = type.Eval(ctx);
		var getArgs = () => {
			var args = evalSrc();
			switch(args) {
				case VDictScope vds:
					return new VTuple { items = vds.locals.Select(pair => (pair.Key, pair.Value)).ToArray() };
				default:
					return ExTuple.SpreadVal(args).EvalTuple(ctx);
			}
		};
		switch(t) {
			case VEmpty: throw new Exception("Type not found");
			case VAttribute va: {
					attribute = true;
					var obj = (VDictScope)evalSrc();
					foreach(var l in obj.locals.Where(l => l.Key is not ['_', '_', ..] && l.Value is VMember)) {
						((VMember)l.Value).attributes.Add(va);
					};
					return obj;
				}
			case VKeyword.CTX: {
					attribute = true;
					var inner = (IScope)source_block.Eval(ctx);
					return new VAttCtx { ctx = inner };
				}
			case VAttCtx att_ctx: {
					return source_block.Eval(att_ctx.ctx);
				}
			case VKeyword.STATIC: {
					attribute = true;
					return evalSrc();
				}
			case VKeyword.DECLARE: {
					attribute = true;
					var obj = (VDictScope)evalSrc();
					foreach(var l in obj.locals.Where(l => l.Key is not ['_', '_', ..])) {
						if(l.Value is VMember vm) {
							vm.type = vm.val;
							vm.ready = false;
							
						}
					};
					return obj;
				}
			case VKeyword.PUB: {
					attribute = true;
					var obj = (VDictScope)evalSrc();
					foreach(var l in obj.locals.Where(l => l.Key is not ['_', '_', ..])) {
						if(l.Value is VMember vm) {
							vm.pub = true;
						}
					};
					return obj;
				}
			case VKeyword.PRIV: {
					attribute = true;
					var obj = (VDictScope)evalSrc();
					foreach(var l in obj.locals.Where(l => l.Key is not ['_', '_', ..])) {
						if(l.Value is VMember vm) {
							vm.pub = false;
						}
					};
					return obj;
				}
			case VKeyword.VAL: {
					attribute = true;
					var obj = (VDictScope)evalSrc();
					foreach(var l in obj.locals.Where(l => l.Key is not ['_', '_', ..])) {
						if(l.Value is VMember vm) {
							vm.mut = false;
						}
					};
					return obj;
				}
			case VKeyword.MUT: {
					attribute = true;
					var obj = (VDictScope)evalSrc();
					foreach(var l in obj.locals.Where(l => l.Key is not ['_', '_', ..])) {
						if(l.Value is VMember vm) {
							vm.mut = true;
						}
					};
					return obj;
				}
			case VEnumRecordType vert: {
					throw new Exception();
				}
			case Type tt: {
					var v = evalSrc();
					var r = new VType(tt).Cast(v, v?.GetType());
					return r;
				}
			case VClass vc: {
					return vc.VarBlock(ctx, source_block);
				}
			case VInterface vi: {
					break;
				}
			case VExtend ex:
				switch(ex.on) {
					case Type:
					case VClass:
						return new VExtendObj { call = ex, src = source_block };
					default:
						return new VExtendObj { call = ex, src = source_block }.Init(ctx);
				}
			case VKeyword.GET: {
					return new VGet { ctx = ctx, get = source_block };
				}
			case VKeyword.SET: {
					return new VSet { ctx = ctx, set = source_block };
				}
			case VKeyword.RETURN: {
					return new VRet(evalSrc(), 1);
				}
			case VKeyword.CLASS: {
					return StDefKey.MakeClass(ctx, source_block);
				}
			case VKeyword.INTERFACE: {
					if(evalSrc() is VDictScope s) {
						return new VInterface { _static = s, source = source_block };
					}
					throw new Exception("Object expected");
				}
			case VKeyword.ENUM: {
					var keyToVal = new ConcurrentDictionary<string, dynamic>();
					var valToKey = new ConcurrentDictionary<dynamic, string>();
					var keys = new string[source_block.statements.Count];
					var vals = new object[source_block.statements.Count];

					var parent = new VDictScope();
					var i = 0;
					foreach(var st in source_block.statements) {
						void Add (string k, dynamic v) {
							keyToVal[k] = v;
							valToKey[v] = k;
							keys[i] = k;
							vals[i] = v;
						}
						switch(st) {
							case ExUpKey { up: -1, key: { } k }: {
									var v = k;
									Add(k, v);
									break;
								}
							case ExInvoke { target: ExUpKey { up: -1, key: { } k }, args: { } args }: {
									var pars = args.EvalTuple(ctx);
									var v = new VEnumRecordType { parent= parent, name = k, pars = pars };
									Add(k, v);
									break;
								}
							case StDefKey { key: { } k, value: { } val }: {
									var v = val.Eval(ctx);
									Add(k, v);
									break;
								}
						}
						i++;
					}
					keyToVal["_keyToVal"] = keyToVal;
					keyToVal["_valToKey"] = valToKey;
					keyToVal["_keys"] = keys;
					keyToVal["_vals"] = vals;

					parent.locals = keyToVal;
					parent.parent = ctx;
					parent.temp = false;
					return parent;
				}
			default:
				return ExInvoke.InvokeFunc(ctx, t, getArgs);
		}
		throw new Exception("Type expected");
	}
}
public class ExEqual : Node {
	public Node lhs;
	public Node rhs;
	public bool invert;
	public object Eval (IScope scope) {
		var l = lhs.Eval(scope);
		var r = rhs.Eval(scope);
		var b = Equals(l, r);
		if(invert) {
			b = !b;
		}
		return b;
	}
}
public class ExIs : Node {
	public Node lhs;
	public Node rhs;
	public string key;
	public object Eval (IScope ctx) {
		var r = rhs.Eval(ctx);
		var l = lhs.Eval(ctx);
		ctx.SetLocal("_lhs", l);
		ctx.SetLocal("_rhs", r);
		return Match(ctx, l, r);
	}
	public static bool Match (IScope ctx, object lhs, object rhs) {
		if(Is(lhs, rhs)) {
			switch(rhs) {
				case IBindPattern ip:
					ip.Bind(ctx, lhs);
					return true;
				case VStringPattern vsp:
					vsp.Bind(ctx);
					return true;
			}
			return true;
		}
		return false;
	}
	public static bool Is (object item, object kind) {
		switch(item, kind) {
			case (var v, Type t): {
					return (v is { } o && t.IsAssignableFrom(o.GetType()));
				}
			case (var v, VClass vc): {
					return (v is VDictScope vds && vds.HasClass(vc));
				}
			case (var v, VComplement co):
				return !Is(v, co.on);
			case (var v, VPredicate sat):
				return sat.Accept(v);
			case (var v, VCriteria vcr):
				return vcr.Accept(v);
			case (var v, VInterface vi): {
					return (v is VDictScope vds && vds.HasInterface(vi));
				}
			case (var v, VEnumRecordType vert): {
					if(v is VEnumRecord ver) {
						if(ver.recordType.name == vert.name) {
							return true;
						}
						//I guess we're lying now
						if(ver.recordType == vert) {
							return true;
						}
					}
					return false;
				}
			case (var v, null):
				return v == null;
			case (null, var t):
				return false;
			case (var v, VKeyword.ANYTHING):
				return true;
			case (var v, VKeyword.NOTHING):
				return false;
			case (var v, IBindPattern ip):
				return ip.Accept(v);
			case (var v, VStringPattern vsp):

				return vsp.Accept((string)v);
			case (var v, var k):
				return Equals(v, k);
			default:
				throw new Exception();
		}
	}
}
public class ExCriteria : Node {
	public Node item;
	public Node cond;
	public object Eval (IScope ctx) {
		return new VCriteria { type = item.Eval(ctx), cond = cond };
	}
}
public class ExBranch : Node {
	public Node condition;
	public Node positive;
	public Node negative;
	public string Source => $"{condition.Source} ?+ {positive.Source}{(negative != null ? $" ?- {negative.Source}" : $"")}";
	public object Eval (IScope ctx) {
		var cond = condition.Eval(ctx);
		switch(cond) {
			case true:
				var r = positive.Eval(ctx);
				if(r is VKeyword.GO_ELSE) {
					return negative.Eval(ctx);
				}
				return r;
			case false:
				switch(negative) {
					case null: return VEmpty.VALUE;
					default: return negative.Eval(ctx);
				}
			default:
				throw new Exception("bit expected");
		}
	}
}
public class ExLoop : Node {
	public Node condition;
	public Node positive;
	public object Eval (IScope ctx) {
		object r = VEmpty.VALUE;
		//var r = new List<dynamic>();
		Step:
		var cond = condition.Eval(ctx);
		switch(cond) {
			case true:
				r = positive.Eval(ctx);

				if(r is VKeyword.BREAK) {
					return VEmpty.VALUE;
				}
				if(r is VKeyword.CONTINUE) {

				}
				if(r is VYield vy) {

				}
				if(r is VRet vr) {
					return vr.Up();
				}
				if(r is VGo vg) {
					return vg;
				}
				goto Step;
			case false: return r;
			default: throw new Exception("Boolean expected");
		}
	}
}
public class ExAt : Node {
	public Node src;
	public List<Node> index;
	public object Eval (IScope ctx) {
		var call = src.Eval(ctx);
		switch(call) {
			case VTuple vt: {
					return (((string key, object val))vt.items.ToArray().GetValue(index.Select(i => (int)i.Eval(ctx)).ToArray())).val;
				}
			default:
				return ExInvoke.InvokeFunc(ctx, call, () => new VTuple { items = [(null, index.Select(i => i.Eval(ctx)).ToArray())] });
		}
		/*
		if(false){
		 var ind = index.Eval(ctx);
		 typeof(int).GetProperty("Item", [ind]);
		}
		*/
		throw new Exception("Sequence expected");
	}
	public object Set (IScope ctx, object val) {
		throw new Exception("");
	}
}
public class ExInvoke : Node {
	public Node target;
	public ExTuple args;
	//public string Source => $"{expr.Source}{(args.Count > 1 ? $"({string.Join(", ", args.Select(a => a.Source))})" : args.Count == 1 ? $"*{args.Single().Source}" : $"!")}";
	public static ExInvoke Fn (string symbol, Node arg) => new() { target = new ExUpKey { key = symbol }, args = ExTuple.Expr(arg) };
	public object Eval (IScope ctx) {
		var f = target.Eval(ctx);
		switch(f) {
			case VError ve:
				throw new Exception(ve.msg);
			case VKeyword.REST: {
					var arg = args.Eval(ctx);
					return new VRest { type = arg };
				}
			case VKeyword.ATTR: {
					var v = (ExUpKey)args.vals.First();
					var dest = (VMember)ctx.Get(v.key, v.up);
					return dest.attributes;
				}
			case VKeyword.CTX: {
					var arg = args.Eval(ctx);
					return new VAttCtx { ctx = (VDictScope)arg };

				}
			case VKeyword.MAGIC: return new VMagic();
			case VKeyword.GO: {
					var ar = args.Eval(ctx);
					return new VGo { target = (VLabel)ar };
				}
			case VKeyword.SET: return new VSet { ctx = ctx, set = args };
			case VKeyword.GET: return new VGet { ctx = ctx, get = args };
			case VKeyword.ANY:
				var at = args.EvalTuple(ctx);
				return new VAnyType { items = at.vals };
			case VKeyword.ALL: return new VAllType { items = args.EvalTuple(ctx).vals };
			case VKeyword.EXTEND: return new VExtend { on = args.EvalExpression(ctx), inherit = true };
			case VKeyword.COMPLEMENT: return new VComplement { on = args.EvalExpression(ctx) };
			case VKeyword.UNALIAS: {
					foreach(var (k, v) in args.items) {
						if(v is ExUpKey es) {
							if(es.Get(ctx) is VAlias va) {
								if(va.expr is ExUpKey es_) {
									return es_.key;
								}
							}
						}
					}
					throw new Exception();
				}
			case VKeyword.REPLACE:
				return InvokePars(ctx, (object lhs, object rhs) => (object item) => item == lhs ? rhs : item, args);
			case VKeyword.FMT:
				string Repl (string str) {
					foreach(Match m in Regex.Matches(str, "{(?<code>.*)}")) {

					}
					return "";

				}
				return args.EvalExpression(ctx) switch {
					string str => Repl(str),
					_ => throw new Exception()
				};
			case VKeyword.RETURN: return new VRet(args.EvalExpression(ctx), 1);
			case VKeyword.YIELD: return new VYield(args.EvalExpression(ctx));
			case VKeyword.IMPLEMENT: {
					var vds = (VDictScope)ctx;
					var arg = args.EvalTuple(ctx);
					foreach(var (k, v) in arg.items) {
						switch(v) {
							case VInterface vi:
								vi.Register(vds);
								break;
							default: throw new Exception("Interface expected");
						}
					}
					return VEmpty.VALUE;
				}
			case VKeyword.IMPORT: {
					var root = ctx;
					while(ctx.parent is { } par) root = par;
					var arg = args.EvalTuple(ctx);
					foreach(var (k, v) in arg.items) {
						switch(v) {
							case VDictScope _vds: {
									foreach(var other in _vds.locals.Keys) {
										StDefKey.Define(root, other, new VAlias { ctx = _vds, expr = new ExUpKey { key = other, up = 1 } });
									}
									break;
								}
							default: throw new Exception("Class expected");
						}
					}
					return VEmpty.VALUE;
				}
			case VKeyword.EMBED: {
					var vds = (VDictScope)ctx;
					var arg = args.EvalTuple(ctx);
					foreach(var (k, v) in arg.items) {
						switch(v) {
							case VClass vc:
								vc.Embed(vds);
								break;
							case VDictScope _vds: {
									foreach(var other in _vds.locals.Keys) {
										if(other.StartsWith("_")) {
											continue;
										}
										StDefKey.Define(ctx, other, new VAlias { ctx = _vds, expr = new ExUpKey { key = other, up = 1 } });
									}
									break;
								}
							default: throw new Exception("Class expected");
						}
					}
					return VEmpty.VALUE;
				}
			case VKeyword.INHERIT: {
					throw new Exception();
					foreach(var (k, v) in args.EvalTuple(ctx).items) {
						switch(v) {
							case VTuple vt:
								vt.Inherit(ctx);
								break;
						}
					}
					return VEmpty.VALUE;
				}
			case VKeyword.SAT:
				var a = args.Eval(ctx);
				return new VPredicate { predicate = (VFn)a };
			case VKeyword.KEYS_OF: {
					var a_ = args.Eval(ctx);
					var obj = (VDictScope)a_;
					List<string> keys = [];
					foreach(var l in obj.locals.Where(l => l.Key is not ['_', '_', ..] && l.Value is VMember)) {
						keys.Add(l.Key);
					};
					return keys;
				}
			case VKeyword.FN:
			default: return InvokePars(ctx, f, args);
		}
	}
	public static object GetReturnType (object f) {
		throw new Exception("Implement");
	}
	public static object InvokePars (IScope ctx, object lhs, ExTuple pars) =>
	 InvokeFunc(ctx, lhs, () => pars.EvalTuple(ctx));
	public static object InvokeArgs (IScope ctx, object lhs, VTuple args) =>
	 InvokeFunc(ctx, lhs, () => args);
	public static object InvokeFunc (IScope ctx, object lhs, Func<VTuple> evalArgs) {
		switch(lhs) {
			case VEmpty:
				throw new Exception("Function not found");
			case VError ve:
				throw new Exception(ve.msg);
			case VConstructor vc: {
					var v = evalArgs().items.Select(pair => pair.val).ToArray();
					var c = vc.t.GetConstructor(v.Select(arg => (arg as object).GetType()).ToArray());
					if(c == null) {
						throw new Exception("Constructor not found");
					}
					return c.Invoke(v);
				}
			case VMember vm:
				return InvokeFunc(ctx, vm.val, evalArgs);
			case VInstanceFn vim: {
					var v = evalArgs().items.Select(pair => pair.val).ToArray();
					return vim.Call(v);
				}
			case ValStaticFn vsm: {
					var v = evalArgs().items.Select(pair => pair.val).ToArray();
					return vsm.Call(v);
				}
			case Type tl: {
					var v = evalArgs().items.Single().val;
					return new VType(tl).Cast(v, v?.GetType());
				}
			case VTuple vt: {
					//var parTypes = vt.items.OfType<Type>().ToArray();
					var v = evalArgs();
					var argTypes = v.items.Select(a => (Type)a.GetType()).ToArray();
					var tt = argTypes.Length switch {
						2 => typeof(ValueTuple<,>),
						3 => typeof(ValueTuple<,,>),
						4 => typeof(ValueTuple<,,,>),
						5 => typeof(ValueTuple<,,,,>),
						6 => typeof(ValueTuple<,,,,,>),
						7 => typeof(ValueTuple<,,,,,,>),
						8 => typeof(ValueTuple<,,,,,,,>),
					};
					var aa = (object)(v.items switch {
					[{ } a, { } b] => (a, b),
					[{ } a, { } b, { } c] => (a, b, c),
					[{ } a, { } b, { } c, { } d] => (a, b, c, d),
					[{ } a, { } b, { } c, { } d, { } e] => (a, b, c, d, e),
					[{ } a, { } b, { } c, { } d, { } e, { } f] => (a, b, c, d, e, f),
					[{ } a, { } b, { } c, { } d, { } e, { } f, { } g] => (a, b, c, d, e, f, g),
					[{ } a, { } b, { } c, { } d, { } e, { } f, { } g, { } h] => (a, b, c, d, e, f, h),
					[{ } a, { } b, { } c, { } d, { } e, { } f, { } g, { } h, { } i] => (a, b, c, d, e, f, h, i),
					});
					var at = tt.MakeGenericType(argTypes);
					return new VType(tt.MakeGenericType(argTypes)).Cast(aa, at);
				}

			case VEnumRecordType vert: {
					return vert.Make(evalArgs(), ctx);
					throw new Exception();
				}
			case VFn vf: {
					return vf.CallFunc(evalArgs);
				}
			case Delegate de: {
					var args = evalArgs();
					var v = args.items.Select(pair => pair.val).ToArray();
					//•⟦⟧⟨⟩〈〉←
					var r = de.DynamicInvoke(v);
					if(de.Method.ReturnType == typeof(void))
						return VEmpty.VALUE;
					return r;
				}
			case VClass vcl: {
					break;
					var args = evalArgs();
					var scope = vcl.MakeInstance();
					foreach(var (k, v) in args.items) {
						if(k == null) {
							throw new Exception();
						}
						StAssignSymbol.AssignLocal(scope, k, () => v);
					}
					return scope;
				}
			case VInterface vi: {
					var args = evalArgs();
					var v = args.items.Single().val;
					switch(v) {
						case VDictScope vds:
							if(vds.HasInterface(vi)) {
								return v;
							} else {
								throw new Exception("Does not implement interface");
							}
						//TODO: Remove this
						case null:
							return null;
					}
					throw new Exception("Cannot implement interface");
				}
			case VIndex vi: {
					var args = evalArgs();
					if(args.Length == 0) {
						return vi.Get();
					} else {
						var val = args.vals[0];
						vi.Set(val);
						return val;
					}
				}
			case VExtendObj ext:
				throw new Exception("Cannot call ExtendObject");
			case VFnScope vfs: {
					return vfs.func.CallFunc(evalArgs);
				}
			case VDictScope s: {
					if(s.locals.TryGetValue("_call", out var f) && f is VFn vf) {
						return vf.CallFunc(evalArgs);
					}
					throw new Exception("Illegal");
				}
			case VObjScope vos: {
					return InvokeFunc(ctx, vos.o, evalArgs);
				}
			case Array a: {
					var ind = evalArgs().vals.OfType<int>().ToArray();
					return new VIndex {
						Get = () => a.GetValue(ind),
						Set = (object o) => a.SetValue(o, ind),
						can_get = true
					};
				}
			case IDictionary d: {
					var ind = evalArgs().vals.Single();
					return new VIndex {
						Get = () => d[ind],
						Set = (object o) => d[ind] = o,
						can_get = d.Contains(ind),
						can_set = true
					};
				}
			case IEnumerable e: {
					var ind = evalArgs().vals.Single();
					return ind switch {
						int i => new VIndex {
							Get = () => {
								var c = ctx;
								var arr = e.OfType<object>().ToArray();
								var ind = i;
								return arr[ind];
							},
							Set = o => throw new Exception("IEnumerable??")
						},
						_ => throw new Exception("IEnumerable?"),
					};
				}
			case VArgs a: {
					var ar = evalArgs();
					var ind = ar.vals.Single();
					if(ind is Array arr) {
						return Get(arr.GetValue(0));
					}
					return Get(ind);
					object Get (object ind) {
						return ind switch {
							string s => a[s],
							int i => a[i],
							VObjScope vos => Get(vos.o),
							_ => throw new Exception("12344"),
						};
					}
				}
		}
		throw new Exception($"Unknown function {lhs}");
	}
}
public class ExBlock : Node {
	public List<Node> statements;
	public XElement ToXML () => new("Block", statements.Select(i => i.ToXML()));
	public string Source => $"{{{string.Join(", ", statements.Select(i => i.Source))}}}";
	public bool obj => _obj ??= statements.Any(s => s is StDefFn or StDefKey or StDefMulti);
	public bool? _obj;
	public object Eval (IScope ctx) {
		if(statements.Count == 0)
			return VEmpty.VALUE;
		var f = new VDictScope(ctx, false);
		object r = VEmpty.VALUE;
		var labels = new HashSet<int>();
		for(int i = 0; i < statements.Count; i++) {
			if(statements[i] is StDefKey { value: ExUpKey { key: "label" }, key: { } k }) {
				var l = new VLabel { home = f, index = i };
				f.locals[k] = l;
				labels.Add(i);
				//statements[i] = new ExVal { value = l };
			}
		}
		for(int i = 0; i < statements.Count; i++) {
			if(labels.Contains(i)) {
				continue;
			}
			var s = statements[i];
			r = s.Eval(f);
			AutoKey(f, s, r);
			switch(r) {
				case VRet vr: return vr.Up();
				case VYield vy: throw new Exception("164");
				case VGo vg:
					if(vg.target.home == f) {
						i = vg.target.index;
						break;
					} else {
						return vg;
					}
					throw new Exception("267");
			}
			f.locals["__"] = r;
		}
		return obj ? f : r;
	}
	public object Apply (IScope f) {
		object r = VEmpty.VALUE;
		foreach(var s in statements) {
			/*
			if(s is StmtDefFunc or StmtDefKey or StmtDefTuple) {
				obj = true;
			}
			*/
			r = s.Eval(f);
			AutoKey(f, s, r);
			switch(r) {
				case VRet vr: return vr.Up();
				case VGo vg:
					throw new Exception("754");
			}
			f.SetLocal("__", r);
		}
		return f;
	}
	public VDictScope MakeScope (IScope ctx) => new(ctx, false);
	public object StagedEval (IScope ctx) => StagedApply(MakeScope(ctx));

	public void AutoKey (IScope f, Node s, object r) {
		switch(s) {
			case ExAlias { expr: ExMemberKey { key: { } key } } ea:
				StDefKey.Define(f, key, r);
				break;
			case ExAlias { expr: ExUpKey { key: { } key } } ea:
				StDefKey.Define(f, key, r);
				break;
			case ExMemberKey { key: { } key } eg:
				StDefKey.Define(f, key, r);
				break;
			case ExUpKey { key: { } key } es:
				StDefKey.Define(f, key, r);
				break;
			//If block is an attribute, inherit all members
			case ExInvokeBlock { attribute: true } eib: {
					foreach(var l in (r as VDictScope).locals.Where(l => l.Key is not ['_', '_', ..])) {
						f.SetLocal(l.Key, l.Value);
					}
					break;
				}
		}
	}

	//Called during class def
	public object StagedApply (IScope ctx) {
		var def = () => { };
		var seq = new List<Node> { };
		var labels = new Dictionary<string, int>();

		var i = 0;
		foreach(var s in statements) {
			switch(s) {
				/*
			case StmtDefFunc df: {
					df.Define(f);
					break;
				}
				*/
				case StDefFn df when df.value is ExInvokeBlock { type: ExUpKey { key: "class" or "interface" or "enum" } }: {
						df.Define(ctx);
						break;
					}
				case StDefKey { value: ExInvokeBlock { type: ExUpKey { key: "class", up: -1 }, source_block: { } block } } kv: {
						var _static = StDefKey.DeclareClass(ctx, block, kv.key);
						def += () => block.StagedApply(_static);
						break;
					}
				case StDefKey { value: ExInvokeBlock { type: ExUpKey { key: "interface", up: -1 }, source_block: { } block } } kv: {
						var _static = StDefKey.DeclareInterface(ctx, block, kv.key);
						def += () => block.StagedApply(_static);
						break;
					}

				case StDefKey { value: ExInvokeBlock { type: ExUpKey { key: "enum", up: -1 }, source_block: { } block } } kv: {
						def += () => kv.Eval(ctx);
						break;
					}
				case StDefKey { key: { } key, value: ExUpKey { key: "label" } }:
					labels[key] = seq.Count;
					seq.Add(s);
					break;
				default:
					seq.Add(s);
					break;
			}

			i++;
		}
		def();
		i = 0;
		while(i < seq.Count) {
			object r = VEmpty.VALUE;
			var s = seq[i];
			switch(s) {
				case StDefKey { value: ExInvokeBlock { type: ExUpKey { key: "defer", up: -1 }, source_block: { } _block } }:
					r = _block.EvalDefer(ctx);
					break;
				default:
					r = s.Eval(ctx);
					break;
			}
			AutoKey(ctx, s, r);
			switch(r) {
				case VRet vr:
					return vr.Up();
				case VGo vg:
					if(vg.target.home == ctx) {
						i = vg.target.index;
						continue;
					} else {
						return vg;
					}
			}
			ctx.SetLocal("__", r);
			i++;
		}
		return ctx;
	}
	public object EvalDefer (IScope ctx) {
		return null;
	}
}
public class ExUpKey : Node, LVal {

	public bool allowDefine => up is -1 or 1;
	public int up = -1;
	public string key;

	public bool publicOnly;
	public XElement ToXML () => new("Symbol", new XAttribute("key", key), new XAttribute("level", $"{up}"));
	public string Source => $"{new string('^', up)}{key}";
	public object Eval (IScope ctx) => GetValueDeref(ctx);
	public object Get (IScope ctx) => ctx.Get(key, up);
	public object GetValueDeref (IScope ctx) {
		var r = ctx.Get(key, up);
		return Deref(r);
	}
	public object Deref (object r) =>
			r switch {
				VGet vg => Deref(vg.Get()),
				VIndex vi => Deref(vi.Get()),
				VAlias va => Deref(va.Get()),
				VMember vm => Deref(vm.val),
				_ => r,
			};
	public object Assign (IScope ctx, Func<object> getNext) {
		return StAssignSymbol.Assign(ctx, key, up, getNext);
	}
}
public class ExSelf : Node {
	public int up;
	public object Eval (IScope ctx) {
		for(int i = 1; i < up; i++) ctx = ctx.parent;
		return ctx;
	}
}
public class ExMemberNumber : Node {
	/*, LVal*/
	public Node src;
	public double num;

	public object Eval (IScope ctx) {
		return num;
	}
}
public class ExMemberKey : Node, LVal {
	public Node src;
	public string key;
	public bool publicOnly = true;
	public object Eval (IScope ctx) => Get(ctx);
	public object Get (IScope ctx) {
		var source = src.Eval(ctx);
		object f (IScope ctx) {
			var v = ctx.GetLocal(key);


			if(publicOnly && v is VMember { pub: false }) {
				throw new Exception("6885");
			}
			if(v is VMember { ready: false }) {
				throw new Exception("6886");
			}
			return Deref(v);
			object Deref (object v) =>
				v switch {
					VGet vg => Deref(vg.Get()),
					VAlias va => Deref(va.Get()),
					//VMember vm => Deref(vm.val),
					_ => v
				};
		}
		switch(source) {
			case VError ve: throw new Exception(ve.msg);
			case VDictScope s: { return f(s); }
			case VClass vc: { return f(vc._static); }
			case Type t: { return f(new VTypeScope { t = t }); }
			case VArgs a: { return a[key]; }
			case object o: { return f(new VObjScope { o = o }); }
		}
		throw new Exception("Object expected");
	}
	public object Assign (IScope ctx, Func<object> getVal) {
		var source = src.Eval(ctx);
		var f = StAssignSymbol.AssignLocal;
		switch(source) {
			case VDictScope s: { return f(s, key, getVal); }
			case VClass vc: { return f(vc._static, key, getVal); }
			case Type t: { return f(new VTypeScope { t = t }, key, getVal); }
			case VArgs a: { return a[key] = getVal(); }
			case object o: { return f(new VObjScope { o = o }, key, getVal); }
		}
		throw new Exception("Object expected");
	}
}
public class ExMemberBlock : Node, LVal {
	public Node lhs;
	public ExBlock rhs;
	public bool local = false;
	public static IScope MakeScope (object o, IScope par = null) {
		switch(o) {
			case IScope sc: {
					return sc.Copy(par);
				}
			case Type t: {
					return new VTypeScope { t = t, parent = par };
				}
			default: {
					return new VObjScope { o = o, parent = par };
				}
		}
	}
	public static IScope MakeScope (object o, IScope par, bool local) => MakeScope(o, local ? null : par);
	public object Assign (IScope ctx, Func<object> getVal) {
		return null;
	}
	public object Eval (IScope ctx) =>
		rhs.Apply(MakeScope(lhs.Eval(ctx), ctx, local));
}
public class ExMemberExpr : Node, LVal {
	public Node lhs;
	public Node rhs;
	public bool local = true;
	public object Assign (IScope ctx, Func<object> getVal) {
		return null;
	}
	public object Eval (IScope ctx) {
		return rhs.Eval(ExMemberBlock.MakeScope(lhs.Eval(ctx), ctx, local));
	}
}
public class ExVal : Node {
	public static ExVal From (object v) => new() { value = v };
	public object value;
	public XElement ToXML () => new("Value", new XAttribute("value", value));
	public string Source => $"{value}";
	public object Eval (IScope ctx) => value;
	public static ExVal Empty = new() { value = VEmpty.VALUE };
}
public class ExCondSeq : Node {
	public Node type;
	public Node filter;
	public List<(Node cond, Node yes, Node no)> items;
	public object Eval (IScope ctx) {
		var f = filter.Eval(ctx);
		var lis = new List<object>();
		foreach(var (cond, yes, no) in items) {
			var c = cond.Eval(ctx);
			var b = ExInvoke.InvokeArgs(ctx, f, c switch {
				VTuple vrt => vrt,
				_ => VTuple.Single(c)
			});
			switch(b) {
				case true: {
						if(yes != null) {
							var v = yes.Eval(ctx);
							switch(v) {
								case VEmpty: continue;
								case VKeyword.CONTINUE: continue;
								case VKeyword.BREAK: goto Done;
								case VRet vr: return vr.Up();
								default:
									lis.Add(v);
									continue;
							}
						}
						continue;
					}
				case false: {
						if(no != null) {
							var v = no.Eval(ctx);
							switch(v) {
								case VEmpty: continue;
								case VKeyword.CONTINUE: continue;
								case VKeyword.BREAK: goto Done;
								case VRet vr: return vr.Up();
								default:
									lis.Add(v);
									continue;
							}
						}
						continue;
					}
				default:
					throw new Exception("Boolean expected");
			}
		}
		Done:
		return ExMap.Convert(lis, type is { } t ? () => (Type)t.Eval(ctx) : null);
		var arr = lis.ToArray();
		return arr;
	}
}
public class ExSwitchFn : Node {
	public object Eval (IScope ctx) {
		return new VFn {
			pars = new VTuple { items = [("arg", null)] },
			expr = new ExSwitch {
				fn = this,
				item = ExTuple.SpreadExpr(new ExUpKey {
					key = "_arg",
					up = 1
				})
			}
		};
	}
	//Add lambda/subswitch support
	public List<(Node cond, Node yes)> branches;
	public object Call (IScope ctx, Node item) {
		var val = item.Eval(ctx);
		if(val is VIndex vi) {
			val = vi.Get();
		}
		return Match(ctx, branches, val);
	}
	/*
$Some(data)
${foo = int:bar}
$(foo:int bar:int)
	*/
	public static object Match (IScope ctx, List<(Node cond, Node yes)> branches, object lhs) {
		//To do: Add recursive call
		var inner_ctx = ctx.MakeTemp(lhs);
		inner_ctx.locals["_"] = lhs;
		foreach(var (cond, yes) in branches) {
			//TODO: Allow lambda matches
			var b = cond.Eval(inner_ctx);
			if(Is(b) || b is VKeyword.DEFAULT) {
				var res = yes.Eval(inner_ctx);
				if(res is VKeyword.FALL) {
					continue;
				}
				return res;
			}
		}
		throw new Exception($"Failed to match token {lhs}");
		bool Is (object rhs) =>
			ExIs.Match(ctx, lhs, rhs);
	}
}

public class ExSwitch : Node {
	public Node item;
	public ExSwitchFn fn;
	public object Eval (IScope ctx) {
		return fn.Call(ctx, item);
	}
}
public class ExCompose : Node {
	public ExTuple items;
	public object Eval (IScope ctx) => throw new Exception("87");
}
public class ExFilter : Node {
	public Node lhs;
	public Node rhs;
	public object Eval (IScope ctx) {
		var l = lhs.Eval(ctx);
		var r = (VFn)rhs.Eval(ctx);
		return FilterFn(l, r);
		object FilterFn (dynamic seq, VFn f) {
			object tr (IScope inner_ctx, object item) =>
				ExInvoke.InvokeArgs(inner_ctx, f, item switch {
					VTuple vt => vt,
					_ => VTuple.Single(item)
				}) switch {
					true => item,
					false => VEmpty.VALUE,
					_ => throw new Exception("9")
				};
			return ExMap.Map(seq, ctx, null, null, (Transform)tr);
		}
	}
}
public class ExMap : Node, LVal {
	public Node src;
	public Node map;
	public Node cond;
	public Node type;
	public bool expr = false;
	public XElement ToXML () => new("Map", src.ToXML(), map.ToXML());
	public string Source => $"{src.Source} | {map.Source}";
	public object Eval (IScope ctx) {
		var lhs = src.Eval(ctx);
		Func<dynamic, object> Map = expr ? MapExpr : MapFunc;
		switch(lhs) {
			case VEmpty: throw new Exception("Variable not found");
			case ICollection c: return Map(c);
			case IEnumerable e: return Map(e);
			case VRange r: return Map(r.GetInt());
			case VDictScope vds: {
					if(vds._seq(out var seq) && seq is VFn vf) return Map(vf.CallPars(ctx, ExTuple.Empty));
					throw new Exception("777");
				}
			case VTuple vt: {
					//TO DO: rewrite
					var keys = vt.items.Select(i => i.key);
					var vals = vt.items.Select(i => i.val);
					var m = (IEnumerable<dynamic>)Map(vals);
					var r = new VTuple { items = keys.Zip(m).ToArray() }; ;
					return r;
				}
			default:
				throw new Exception("Sequence expected");
		}
		object MapFunc (dynamic seq) {
			Func<Type>? t = type is { } _t ? () => (Type)_t.Eval(ctx) : null;
			var f = map.Eval(ctx);
			if(f is Type tt) {
				return ExMap.Map(seq, ctx, cond, (Func<Type>?)(() => tt), null);
			}
			Transform tr = (IScope inner_ctx, object item) =>
				ExInvoke.InvokeArgs(inner_ctx, f, item switch {
					VTuple vt => vt,
					_ => VTuple.Single(item)
				});
			return ExMap.Map(seq, ctx, cond, t, (Transform)tr);
		}
		object MapExpr (dynamic seq) {
			object tr (IScope inner_ctx, object item) => map.Eval(MakeMapCtx(inner_ctx, item));
			Func<Type>? t = type is { } _t ? () => (Type)_t.Eval(ctx) : null;
			return ExMap.Map(seq, ctx, cond, t, (Transform)tr);
		}
	}
	public static IScope MakeCondCtx (IScope ctx, object item, int index) {
		var inner_ctx = (IScope)ctx.MakeTemp();
		inner_ctx.SetLocal("_item", item);
		inner_ctx.SetLocal("_index", index);
		return inner_ctx;
	}
	public static IScope MakeMapCtx (IScope inner_ctx, object item) {
		return
		  item switch {
			  VDictScope vds => new VDictScope { locals = vds.locals, parent = inner_ctx, temp = false },
			  Type t => new VTypeScope { parent = inner_ctx, t = t },
			  object o => new VObjScope { parent = inner_ctx, o = o },
		  };
	}
	public static object Convert (List<object> items, Func<Type>? type) {
		var r = items.ToArray();
		if(type != null) {
			var t = type();
			var arr = Array.CreateInstance(t, r.Length);
			Array.Copy(r, arr, r.Length);
			return arr;
		}
		return r;
	}
	public delegate object Transform (IScope inner_ctx, object item);
	public static object Map (dynamic seq, IScope ctx, Node cond, Func<Type> t, Transform tr) {
		var result = new List<object>();
		if(tr == null) {
			foreach(var item in seq) {
				result.Add(item);
			}
			goto Done;
		}
		int index = 0;
		foreach(var item in seq) {
			var inner_ctx = ExMap.MakeCondCtx(ctx, item, index);
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
			var r = tr(inner_ctx, item);
			switch(r) {
				case VEmpty: continue;
				case VKeyword.CONTINUE: continue;
				case VKeyword.BREAK: goto Done;
				case VRet vr: return vr.Up();
				default:
					result.Add(r);
					continue;
			}
		}
		Done:
		return Convert(result, t);
	}


	public delegate void Process (IScope inner_ctx, object item);
	public static object ForEach (dynamic seq, IScope ctx, Node cond, Func<Type> t, Process tr) {
		if(tr == null) {
			throw new Exception("134234");
		}
		int index = 0;
		foreach(var item in seq) {
			var inner_ctx = ExMap.MakeCondCtx(ctx, item, index);
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

			tr(ctx, item);
		}
		Done:
		return VEmpty.VALUE;
	}







	public object Assign (IScope ctx, Func<object> getVal) {
		return null;
	}
}
public class ExFn : Node {
	public ExTuple pars;
	public Node result;
	//public XElement ToXML () => new("ExprFunc", [.. pars.Select(i => i.ToXML()), result.ToXML()]);
	//public string Source => $"@({string.Join(", ", pars.Select(p => p.Source))}) {result.Source}";
	public object Eval (IScope ctx) =>
	 new VFn {
		 expr = result,
		 pars = pars.EvalTuple(ctx),
		 parent_ctx = ctx
	 };
}
public class ExSeq : Node {
	/*, LVal */
	public Node type;
	public List<Node> items;
	public object Eval (IScope ctx) {
		List<object> l = [];
		foreach(var it in items) {
			var r = it.Eval(ctx);
			switch(r) {
				case VSpread spr:
					spr.SpreadArray(l);
					break;
				default:
					l.Add(r);
					break;
			}
		}
		var res = l.ToArray();
		switch(type?.Eval(ctx)) {
			case Type t:
				var arr = Array.CreateInstance(t, res.Length);
				Array.Copy(res.ToArray(), arr, arr.Length);
				return arr;
			//todo: function
			case VFn f:
			default: return res;
		}
	}
}
public class ExLisp : Node {
	public Node[] args;
	public string op;
	public object Eval (IScope ctx) {
		var lhs = args.First().Eval(ctx);
		if(args.Length == 1) {
			var l = (dynamic)lhs;
			return op switch {
				"++" => l + 1,
				"--" => l - 1
			};
		}
		foreach(var rhs in args.Skip(1)) {
			//Get extension member or internal member
			switch(lhs) {
				case IScope sc:
					lhs = new ExInvoke { target = new ExMemberKey { src = ExVal.From(lhs), key = op }, args = ExTuple.Expr(rhs) }.Eval(ctx);
					break;
				default:
					var l = (dynamic)lhs;
					var r = (dynamic)rhs.Eval(ctx);
					lhs = op switch {
						"+" => l + r,
						"-" => l - r,
						"*" => l * r,
						"/" => l / r,

						"&" => l & r,
						"|" => l | r,
						"^" => l ^ r,
						"%" => l % r,
						"=" => l == r,
						"≠" => l != r,
						">" => l > r,
						"<" => l < r,

						"<=" => l <= r,
						">=" => l >= r,

						"**" => Math.Pow(l, r),
						"//" => (int)Math.Floor(l / r),

						">>" => l >> r,
						"<<" => l << r,
						".." => l..r,
						"&&" => l && r,
						"||" => l || r,
					};
					break;
			}
		}
		return lhs;
	}
}
public class ExTuple : Node {

	/*, LVal*/
	public (string key, Node value)[] items;
	public IEnumerable<Node> vals => items.Select(i => i.value);
	public static ExTuple Empty => new() { items = [] };
	public static ExTuple Expr (Node v) => new() { items = [(null, v)] };
	public static ExTuple Val (object v) => Expr(new ExVal { value = v });
	public static ExTuple SpreadExpr (Node n) => Expr(new ExSpread { value = n });
	public static ExTuple SpreadVal (object v) => SpreadExpr(new ExVal { value = v });
	public static ExTuple ListExpr (IEnumerable<Node> items) => new() { items = items.Select(i => ((string)null, i)).ToArray() };
	public VTuple EvalTuple (IScope ctx) {
		var it = new List<(string key, object val)> { };
		/*
		var t = new ValTuple { items = it };
		var s = ctx.MakeTemp();
		s.locals["_tuple"] = t;
		*/
		var s = ctx;
		foreach(var (key, val) in items) {
			//TODO: inc('y) != inc*'y
			if(val is ExAlias ea) {
				it.Add((key, ea.Eval(ctx)));
				continue;
			}
			Handle(val.Eval(s));
			void Handle (object v) {
				switch(v) {
					case VSpread vs:
						vs.SpreadTuple(key, it);
						break;
					case VEmpty:
						break;
					case VAlias va:
						Handle(va.Get());
						//throw new Exception();
						//it.Add((key, va));
						break;
					case VGet vg:
						throw new Exception("32778");
					default:
						it.Add((key, v));
						break;
				}
			}
		}
		return new VTuple { items = it.ToArray() };
		//return t;
	}
	public object Eval (IScope ctx) => EvalExpression(ctx);
	public object EvalExpression (IScope ctx) {
		var a = EvalTuple(ctx);
		switch(a.Length) {
			case 0: return VEmpty.VALUE;
			case 1: return a.items.Single().val;
			default: return a;
		}
	}
	public void Spread (IScope ctx, List<(string key, object val)> it) {
		foreach(var (key, val) in items) {
			it.Add((key, val.Eval(ctx)));
		}
	}
	public ExTuple ParTuple () =>
	 new() {
		 items = items.Select(pair => {
			 if(pair.key == null) {
				 switch(pair.value) {
					 case ExUpKey { up: -1, key: { } key }:
						 return (key, new ExVal { value = typeof(object) });
					 case ExUpKey { up: not -1, key: { } key }:
						 return (key, pair.value);
				 }
				 throw new Exception("Expected");
			 } else {
				 return pair;
			 }
		 }).ToArray()
	 };
}
public class ExMonadic : Node {
	public Node rhs;
	public EFn fn;
	public object Eval (IScope ctx) {
		var r = rhs.Eval(ctx);
		if(r is VMember vm) {
			r = vm.val;
		}
		switch(fn) {
			case EFn.range:
				return Enumerable.Range(0, (int)r);
			case EFn.not:
				return !(bool)r;
			case EFn.floor:
				return Math.Floor((dynamic)r);
			case EFn.ceil:
				return Math.Ceiling((dynamic)r);
			case EFn.dice:
				return new Random().Next((dynamic)r);
			case EFn.keyboard:
				return ((string)r)[0];
			case EFn.first:
				return (r as IEnumerable<object>).First();
			case EFn.last:
				return (r as IEnumerable<object>).Last();
			case EFn.index_ascend: {
					var src = r as Array;
					return Enumerable.Range(0, src.Length).OrderBy(i => ((dynamic)r)[i]);
				}
			case EFn.index_descend: {
					var src = r as Array;
					return Enumerable.Range(0, src.Length).OrderByDescending(i => ((dynamic)r)[i]);
				}
			case EFn.sat: {
					return new VPredicate { predicate = (VFn)r };
				}
			default: throw new Exception("845834");
		}
	}
	public enum EFn {
		err,
		range,
		count,
		not,
		floor,
		ceil,
		log,

		index_ascend, index_descend,
		dice,
		keyboard,

		first, last,
		sat
	}
}
public class ExDyadicSeq : Node {
	public Node lhs, rhs;
	public bool lseq, rseq;
	public ExDyadic.EFn fn;
	public object Eval (IScope ctx) {
		switch(lseq, rseq) {
			case (true, true): {
					var l = (Array)lhs.Eval(ctx);
					var r = (Array)rhs.Eval(ctx);
					var ret = Array.CreateInstance(typeof(object), l.Length);
					for(int i = 0; i < l.Length; i++) {
						ret.SetValue(Fn(l.GetValue(i), r.GetValue(i)), i);
					}
					return ret;
				}
			case (true, false): {
					var l = (Array)lhs.Eval(ctx);
					var r = rhs.Eval(ctx);
					var ret = Array.CreateInstance(typeof(object), l.Length);
					for(int i = 0; i < l.Length; i++) {
						ret.SetValue(Fn(l.GetValue(i), r), i);
					}
					return ret;
				}
			case (false, true): {
					var l = lhs.Eval(ctx);
					var r = (Array)rhs.Eval(ctx);
					var ret = Array.CreateInstance(typeof(object), r.Length);
					for(int i = 0; i < r.Length; i++) {
						ret.SetValue(Fn(l, r.GetValue(i)), i);
					}
					return ret;
				}
			default:
				throw new Exception("34546565");
		}
	}
	public object Fn (dynamic l, dynamic r) {
		switch(fn) {
			case ExDyadic.EFn.add: return l + r;
			case ExDyadic.EFn.sub: return l - r;
			case ExDyadic.EFn.mul: return l * r;
			case ExDyadic.EFn.div: return l / r;
			default: throw new Exception("3434345345");
		}
	}
}
public class ExDyadic : Node {
	public Node lhs;
	public Node rhs;
	public EFn fn;
	public object Eval (IScope ctx) {
		bool and () {
			foreach(var b in (Node[])[lhs, rhs]) {
				var r = b.Eval(ctx);
				switch(r) {
					case false: return false;
					case true: continue;
					default: throw new Exception("boolean expected");
				}
			}
			return true;
		};
		bool or () {
			foreach(var b in (Node[])[lhs, rhs]) {
				switch(b.Eval(ctx)) {
					case true: return true;
					case false: continue;
					default: throw new Exception("boolean expected");
				}
			}
			return false;
		};
		bool xor () {
			var l = (bool)lhs.Eval(ctx);
			var r = (bool)rhs.Eval(ctx);
			return l ^ r;
		};
		var _lhs = () => (dynamic)lhs.Eval(ctx);
		var _rhs = () => (dynamic)rhs.Eval(ctx);
		switch(fn) {
			case EFn.neq: return !ExIs.Is(_lhs(), _rhs());
			case EFn.and: return and();
			case EFn.or: return or();
			case EFn.xor: return xor();
			case EFn.nand: return !and();
			case EFn.nor: return !or();
			case EFn.xnor: return !xor();
			case EFn.max: return Math.Max(_lhs(), _rhs());
			case EFn.min: return Math.Min(_lhs(), _rhs());
			case EFn.for_all: {
					var l = lhs.Eval(ctx);
					var r = (VFn)rhs.Eval(ctx);
					return ForAll(l, r);
					bool ForAll (dynamic seq, VFn f) {
						var r = true;
						void tr (IScope inner_ctx, object item) {
							var _ = ExInvoke.InvokeArgs(inner_ctx, f, item switch {
								VTuple vt => vt,
								_ => VTuple.Single(item)
							}) switch {
								false => r = false,
								_ => throw new Exception("23123857")
							};
						}
						ExMap.ForEach(seq, ctx, null, null, (Process)tr);
						return r;
					}
				}
			case EFn.exists: {
					var l = _lhs();
					var r = (VFn)_rhs();
					return Exists(l, r);
				}
			case EFn.not_exists: {
					var l = _lhs();
					var r = (VFn)_rhs();
					return !Exists(l, r);
				}
			case EFn.count: {
					var l = _lhs();
					var r = _rhs();
					return Count(l, r);
					int Count (dynamic lhs, object rhs) {
						var r = 0;
						void tr (IScope inner_ctx, object item) {
							if(Equals(rhs, item)) {
								r += 1;
							}
						}
						ExMap.ForEach(lhs, ctx, null, null, (Process)tr);
						return r;
					}
				}
			case EFn.concat: return new ExSeq { items = [new ExSpread { value = lhs }, new ExSpread { value = rhs }] };
			case EFn.add: return (_lhs() + _rhs());
			case EFn.sub: return (_lhs() - _rhs());
			case EFn.mul: return (_lhs() * _rhs());
			case EFn.div: return (_lhs() / _rhs());
			case EFn.gt: return (_lhs() > _rhs());
			case EFn.lt: return (_lhs() < _rhs());
			case EFn.geq: return (_lhs() >= _rhs());
			case EFn.leq: return (_lhs() <= _rhs());
			case EFn.construct: {
					var l = (VClass)_lhs();
					var r = (VTuple)_rhs();
					var result = l.MakeInstance();
					var i = 0;
					foreach(var k in l._static.KeyList) {
						result.SetAt(k, r.vals[i], 0); i++;
					}
					return result;
				}
			case EFn.union: return new VAnyType { items = [_lhs(), _rhs()] };
			case EFn.intersect: return new VAllType { items = [_lhs(), _rhs()] };
			case EFn.transform: return new VTransformPattern { lhs = _lhs(), rhs = _rhs() };
			case EFn.assign: {
				return new StAssignSymbol { symbol = (ExUpKey)lhs, value = rhs }.Eval(ctx);
				
			}
			default: throw new Exception("92391293");
		}
		bool Exists (dynamic seq, VFn f) {
			var r = false;
			void tr (IScope inner_ctx, object item) {
				var _ = ExInvoke.InvokeArgs(inner_ctx, f, item switch {
					VTuple vt => vt,
					_ => VTuple.Single(item)
				}) switch {
					true => r = true,
					_ => throw new Exception("943848")
				};
			}
			ExMap.ForEach(seq, ctx, null, null, (Process)tr);
			return r;
		}
	}
	public enum EFn {
		err,
		neq,
		and, or, xor,
		nand, nor, xnor,
		add, sub, mul, div,
		gt, lt, geq, leq,
		max, min,
		front, back,
		for_all, exists, not_exists, concat, count,
		log, index_of,
		take, drop,
		deal, assign,
		select_element,
		insert_zero,
		construct,
		range,
		compose,

		union, intersect,

		transform
	};
}
public interface VStringPattern {
	public bool Accept (string str);
	public void Bind (IScope scope);

	public string RegexPattern { get; }
}
public class PatternString : VStringPattern {
	public string pattern;
	public bool regex;

	public string key;
	public string val;

	public string RegexPattern => $"({(key is { }s ? $"?<{key}>" : "")}{pattern})";
	public bool Accept (string str) {
		var r = false;
		if(regex) { r = Regex.IsMatch(str, pattern); } else r = str == pattern;
		if(r) {
			val = pattern;
		}
		return r;
	}
	public void Bind (IScope scope) {
		scope.SetAt(key, val, 1);
	}
}
public class AnyString : VStringPattern {
	List<VStringPattern> seq;
	public VStringPattern bound;


	public string RegexPattern => $"({string.Join("|", seq.Select(s => s.RegexPattern))})";
	public bool Accept (string str) {
		foreach(var s in seq) {
			if(s.Accept(str)) {
				bound = s;
				return true;
			}
		}
		return false;
	}
	public void Bind (IScope scope) {
		bound?.Bind(scope);
	}
}
public class AllString : VStringPattern {
	public List<VStringPattern> seq;

	public string RegexPattern => $"({string.Join("", seq.Select(s => s.RegexPattern))})";
	public bool Accept (string str) {
		foreach(var s in seq) {
			if(!s.Accept(str)) {
				return false;
			}
		}
		return true;
	}
	public void Bind (IScope scope) {
		foreach(var s in seq) {
			s.Bind(scope);
		}
	}
}
public class ExGuardPattern : Node {
	public Node cond;
	public object Eval (IScope ctx) => new VGuardPattern { cond = cond, ctx = ctx };
}
public class ExStructurePattern : Node {
	public bool rest;
	public List<(string lhs, Node rhs, string key)> binds;
	public object Eval (IScope ctx) {
		List<(string lhs, object rhs, string key)> bound = [];
		foreach(var b in binds) {
			var rhs = b.rhs is { } v ? v.Eval(ctx) : VKeyword.ANYTHING;
			bound.Add((b.lhs, rhs, b.key));
		}
		return new VStructurePattern { binds = bound, rest = rest };
	}
}
public class ExTuplePattern : Node {
	public bool rest;
	public List<(string key, Node? type)> binds;
	public object Eval (IScope ctx) {
		List<(string key, object type)> bound = [];
		foreach(var b in binds) {
			var rhs = b.type is { } v ? v.Eval(ctx) : VKeyword.ANYTHING;
			bound.Add((b.key, rhs));
		}
		return new VTuplePattern { binds = bound, rest = rest };
	}
}
public class ExWildcardPattern : Node {
	public string key;
	public object Eval (IScope ctx) => new VWildcardPattern { key = key };
}