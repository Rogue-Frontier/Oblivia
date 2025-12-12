using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Oblivia;

public class StDefKey : Node {
	public string key;
	public Node value;
	public Node criteria;
	public bool first = true;
	public XElement ToXML () => new("KeyVal", new XAttribute("key", key), value.ToXML());
	public string Source => $"{key}:{value?.Source ?? "null"}";
	public object Eval (IScope ctx) {
		if(value == null) {
			var val = new VMember { name = key, type = criteria.Eval(ctx), mut = true, pub = true, ready = false, val = null };
			if(first) {
				Define(ctx, key, val);
				first = false;
			} else {
				Set(ctx, key, val);
			}
		} else {
			var val = value.Eval(ctx);
			if(val is VError ve) {
				throw new Exception(ve.msg);
			}
			var mem = new VMember { pub = true, mut = true, name = key, val = val, ready = true, type = val.GetType() };
			if(first) {
				Define(ctx, key, mem);
				first = false;
			} else {
				Set(ctx, key, mem);
			}
		}
		return VEmpty.VALUE;
	}
	public static object DefineFrom (IScope ctx, string key, IScope from) => Define(ctx, key, from.Get(key));
	public static object Define (IScope ctx, string key, object val) {
		var curr = ctx.GetLocal(key);
		if(curr is not VError) {
			throw new Exception($"Key already defined: {key} = {curr} := {val}");
		}
		if(ctx is VDictScope vds) {
			//vds.KeyList.Add(key);
		}
		Set(ctx, key, val);
		return VEmpty.VALUE;
	}
	public static void Set (IScope ctx, string key, object val) {
		ctx.SetLocal(key, val);
	}
	public static VClass MakeClass (IScope f, ExBlock block) {
		var _static = block.MakeScope(f);
		block.StagedApply(_static);
		var c = new VClass { name = "unknown", parent_ctx = f, source_expr = block, _static = _static };
		_static.locals["__kind__"] = VKeyword.CLASS;
		_static.SetClass(c);
		return c;
	}
	public static VDictScope DeclareClass (IScope f, ExBlock block, string key) {
		var _static = block.MakeScope(f);
		var c = new VClass {
			name = key,
			parent_ctx = f,
			source_expr = block,
			_static = _static
		};
		f.SetLocal(key, c);
		_static.locals["__kind__"] = VKeyword.CLASS;
		_static.SetClass(c);
		return _static;
	}
	public static VDictScope DeclareInterface (IScope f, ExBlock block, string key) {
		var _static = block.MakeScope(f);
		var vi = new VInterface { _static = _static };
		f.SetLocal(key, vi);
		_static.locals["__kind__"] = VKeyword.INTERFACE;
		_static.AddInterface(vi);
		return _static;
	}
}
public class StDefFn : Node {
	public string key;
	public ExTuple pars;
	public Node value;
	//public XElement ToXML () => new("DefineFunc", [new XAttribute("key", key), ..pars.Select(i => i.ToXML()), value.ToXML()]);
	//public string Source => $"{key}({string.Join(", ",pars.Select(p => p.Source))}): {value.Source}";
	public object Eval (IScope ctx) {
		Define(ctx);
		return VEmpty.VALUE;
	}
	public void Define (IScope owner) {
		owner.SetLocal(key, new VFn {
			expr = value,
			pars = pars.EvalTuple(owner),
			parent_ctx = owner
		});
	}
	public VFn DeclareHead (IScope owner) {
		var vf = new VFn {
			expr = value,
			pars = new VTuple { items = [] },
			parent_ctx = owner
		};
		owner.SetLocal(key, vf);
		return vf;
	}
	public void DefineHead (VFn vf) {
		vf.pars = pars.EvalTuple(vf.parent_ctx);
	}
}
public class StDefMulti : Node {
	public string[] lhs;
	public Node rhs;
	public bool deconstruct = false;
	public object Eval (IScope ctx) {
		var val = rhs.Eval(ctx);
		switch(val) {
			case VTuple vt:
				if(deconstruct) {
					foreach(var sym in lhs) {
						StDefKey.Define(ctx, sym, vt.items.Where(pair => pair.key == sym).Single());
					}
					return VEmpty.VALUE;
				} else if(lhs.Length == vt.items.Length) {
					foreach(var i in Enumerable.Range(0, lhs.Length)) {
						StDefKey.Define(ctx, lhs[i], vt.items[i].val);
					}
					return VEmpty.VALUE;
				} else {
					throw new Exception("illegal");
				}
			case IScope sc:
				if(deconstruct) {
					foreach(var sym in lhs) {
						StDefKey.DefineFrom(ctx, sym, sc);
					}
					return VEmpty.VALUE;
				} else {
					throw new Exception("unknown");
				}
		}
		throw new Exception("9465958234");
	}
}
public class StAssignSymbol : Node {
	public ExUpKey symbol;
	public Node value;
	XElement ToXML () => new("Reassign", symbol.ToXML(), value.ToXML());
	public string Source => $"{symbol.Source} := {value.Source}";
	public object Eval (IScope ctx) {
		var currMember = symbol.Get(ctx);
		var curr = symbol.Deref(currMember);
		var inner_ctx = ctx.MakeTemp(curr);
		inner_ctx.locals["_curr"] = curr;
		switch(curr) {
			case VMember {ready:true } va:
				inner_ctx.locals["_type"] = va.type;
				inner_ctx.locals["_name"] = va.name;
				break;
		}
		object GetVal () {
			var v = value.Eval(inner_ctx);
			return v;
		}
		var r = symbol.Assign(ctx, GetVal);
		return r;
	}
	public static object AssignLocal (IScope ctx, string key, Func<object> getNext) => Assign(ctx, key, 1, getNext);
	public static object AssignSymbol (IScope ctx, ExUpKey sym, Func<object> getNext) => Assign(ctx, sym.key, sym.up, getNext);
	public static object Assign (IScope ctx, string key, int up, Func<object> getNext) {
		var curr = ctx.Get(key, up);
		switch(curr) {
			case VError ve: throw new Exception(ve.msg);
			case VSet vs: return vs.Set(getNext());
			case VAlias va: return va.Set(getNext);
			case VMember vm: return AssignMember(vm);
			case VClass vc: return AssignClass(vc);
			case VDictScope vds: return ctx.Set(key, getNext(), up);
			case VKeyword.AUTO: return ctx.Set(key, getNext(), up);
			default: return AssignType(curr?.GetType());
		}
		object AssignMember (VMember m) {
			if(!m.mut) {
				throw new Exception("Variable is immutable");
			}
			var next = Validate(m.type);
			m.ready = true;
			m.val = next;
			return next;
		}
		object Assign (object type) {
			switch(type) {
				case Type t: return AssignType(t);
				case VClass vc: return AssignClass(vc);
			}
			throw new Exception("213823");
		}
		object Validate (object type) {
			switch(type) {
				case Type t: return ValidateType(t);
				case VClass vc: return ValidateClass(vc);
			}
			throw new Exception("2351");
		}
		object AssignClass (VClass cl) {
			var next = ValidateClass(cl);
			return ctx.Set(key, next, up);
		}
		object ValidateClass (VClass cl) {
			var next = getNext();
			switch(next) {
				case VDictScope vds: return next;
				case VError ve: throw new Exception(ve.msg);
				default: return next;
			}
		}
		object AssignType (Type prevType) {
			var next = ValidateType(prevType);
			return ctx.Set(key, next, up);
		}
		object ValidateType (Type prevType) {
			var next = getNext();
			if(next is VError e) {
				throw new Exception(e.msg);
			}
			if(next is VMember vm) {
				next = vm.val;
			}
			if(prevType == null) {
				return next;
			}
			switch(curr) {
				case VInterface vi:
					if(next is VDictScope vds) {
						if(vds.HasInterface(vi)) {
							return next;
						}
						throw new Exception("Does not implement interface");
					}
					throw new Exception("Value must be a scope");
			}
			var nt = next.GetType();
			if(!prevType.IsAssignableFrom(nt)) {
				throw new Exception("Type mismatch");
			}
			switch(next) {
				case VError ve: throw new Exception(ve.msg);
			}
			return next;
		}
	}
	public static bool CanAssign (object prev, object next) {
		switch(prev) {
			case VError ve: throw new Exception(ve.msg);
			case VMember vm: return Match(vm.type);
			case VClass vc: return MatchClass(vc);
			case VDictScope vds: return true;
			default: return MatchType(prev?.GetType());
		}
		bool Match (object prevType) {
			switch(prevType) {
				case Type t: return MatchType(t);
				case VClass vc: return MatchClass(vc);
			}
			throw new Exception("34997465");
		}
		bool MatchClass (VClass prevClass) {
			switch(next) {
				case VDictScope vds: return true;
				case VError ve: throw new Exception(ve.msg);
				default: return true;
			}
		}
		bool MatchType (Type prevType) {
			if(prevType == null) {
				goto Good;
			}
			switch(prev) {
				case VInterface vi:
					if(next is VDictScope vds) {
						if(vds.HasInterface(vi)) {
							goto Good;
						}
						throw new Exception("Does not implement interface");
					}
					throw new Exception("Value must be a scope");
			}
			var nt = next.GetType();
			if(!prevType.IsAssignableFrom(nt)) {
				throw new Exception("Type mismatch");
			}
			if(next is VError ve) {
				throw new Exception(ve.msg);
			}
			Good:
			return true;
		}
	}
}
public class StAssignMulti : Node {
	public bool deconstruct;
	public ExUpKey[] symbols;
	public Node value;
	public object Eval (IScope ctx) {
		return deconstruct ? AssignDestructure(ctx, symbols, value.Eval(ctx)) : AssignTuple(ctx, symbols, value.Eval(ctx));
	}
	public static object AssignDestructure (IScope ctx, ExUpKey[] symbols, object val) {
		switch(val) {
			case VArgs a:
				foreach(var s in symbols) {
					s.Assign(ctx, () => a.dict[s.key]);
				}
				return VEmpty.VALUE;
			case IScope from:
				foreach(var s in symbols) {
					s.Assign(ctx, () => from.GetLocal(s.key));
				}
				return VEmpty.VALUE;
		}
		throw new Exception("2929349");
	}
	public static object AssignTuple (IScope ctx, ExUpKey[] symbols, object val) {
		switch(val) {
			case VTuple vt:
				if(symbols.Length == vt.items.Length) {
					foreach(var i in Enumerable.Range(0, symbols.Length)) {
						var k = symbols[i];
						var v = vt.items[i].val;
						StAssignSymbol.AssignSymbol(ctx, k, () => v);
					}
					return VEmpty.VALUE;
				} else {
					throw new Exception("43853845");
				}
			case VArgs a:
				if(a.Length == symbols.Length) {
					foreach(var i in Enumerable.Range(0, symbols.Length)) {
						StAssignSymbol.AssignSymbol(ctx, symbols[i], () => a[i]);
					}
					return VEmpty.VALUE;
				} else {
					throw new Exception("93545332");
				}
			case Array a:
				if(a.Length == symbols.Length) {
					foreach(var i in Enumerable.Range(0, symbols.Length)) {
						var v = a.GetValue(i);
						StAssignSymbol.AssignSymbol(ctx, symbols[i], () => v);
					}
					return VEmpty.VALUE;
				} else {
					throw new Exception("123345");
				}
		}
		throw new Exception("123123");
	}
}
public class StAssignDynamic : Node {
	public Node[] lhs;
	public Node rhs;
	public object Eval (IScope ctx) {
		return null;
	}
}

public class StReturn : Node {
	public int up = 1;
	public Node val;
	public object Eval (IScope ctx) =>
	 new VRet(val.Eval(ctx), up);
}