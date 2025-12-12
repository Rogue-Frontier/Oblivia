using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Oblivia;

public class VAttribute { public string name; }
public class VMagic ();
public class VError {
	public string msg;
	public VError () { }
	public VError (string msg) {
		this.msg = msg;
	}
	public override string ToString () => $"VError: {msg}";
}
public record VIndex {
	public Action<object> Set;
	public Func<object> Get;

	public bool can_get = false;
	public bool can_set = false;
}
public record VRef {
	public Array src;
	public int[] index;
	public void Set (object value) =>
	 src.SetValue(value, index);
	public object Get () => src.GetValue(index);
}
public record VConstructor (Type t) { }
public record VInstanceFn (object src, string key) {
	public object Call (object[] data) {
		var tp = data.Select(d => (d as object).GetType()).ToArray();
		var fl = BindingFlags.Instance | BindingFlags.Public;
		return src.GetType().GetMethod(key, fl, tp).Invoke(src, data);
	}
};
public record ValStaticFn (Type src, string key) {
	public object Call (object[] data) {
		var tp = data.Select(d => (d as object).GetType()).ToArray();
		var fl = BindingFlags.Static | BindingFlags.Public;
		return src.GetMethod(key, fl, tp).Invoke(src, data);
	}
};
public record VRet (object data, int up) {
	public object Up () => up == 1 ? data : this with { up = up - 1 };
}
public record VYield (object data);
public enum VKeyword {
	CLASS, INTERFACE, ENUM,
	GET, SET, PROP,

	UP,

	FALL,

	GO_ELSE,

	DEFAULT,
	MIMIC,
	IMPLEMENT, INHERIT,
	BREAK, CONTINUE,
	RETURN, YIELD,

	EXTEND,

	ASYNC, AWAIT,
	DEFER,

	AUTO,
	ABSTRACT,

	CANCEL,

	ALIAS, UNALIAS,

	DECLARE,
	COMPLEMENT,

	REPLACE,

	REPEAT,
	VAR,
	STAGE,
	ANY, ALL,

	ANYTHING,
	NOTHING,

	TYPE,
	FN,
	MAKE,
	PUB, PRIV, MUT, INSTANCE, STATIC, MACRO, TEMPLATE,

	FIELDS_OF, METHODS_OF, MEMBERS_OF,
	ATTR,
	MARK,

	PREV_ITER, SEEK_ITER, WAIT_ITER,

	//autoret(item) = (match(item), item)
	AUTORET,

	//function is provably halting
	HALTING,
	//function that is constant-time
	PATTERN,
	//a = match(func) = func(a)
	MATCH,

	//fn_c(2)*foo(_0 _1)
	FN_C,
	//fn_t(int int)*foo(_0 _1)
	FN_T,
	//immutable
	VAL,

	MODULE,
	IMPORT,
	EMBED,

	CTX,

	//Create a label
	LABEL,
	//Go to label
	GO,
	//Go to the top of the current scope
	REDO,

	REST,

	NOP,
	//Creates a magic constant e.g. null
	MAGIC,
	SAT,

	TYPEOF,

	//Provides multiple values for tuple
	DECONSTRUCT,
	//Gets all keys from the target for deconstruct-assign
	KEYS_OF,

	XML,
	JSON,
	REGEX,
	MATH,
	FMT,
	LEAF,

	CONSTEVAL,
	CONSTEXPR,

	MEASURE
}
public record VPredicate {
	public VFn predicate;
	public bool Accept (object args) => predicate.CallData([args]) is true;
}
public record VTransformPattern : IBindPattern {
	public object lhs, rhs;
	public bool Accept (object args) => ExIs.Is(((VFn)lhs).CallData([args]), rhs);
	public void Bind (IScope ctx, object o) {
		if(rhs is IBindPattern ibp) {
			ibp.Bind(ctx, o);
		}
	}
}
public record VAllType : IBindPattern {
	public object[] items;
	public bool Accept (object val) {
		return items.All(i => ExIs.Is(val, i));
	}
	public void Bind (IScope ctx, object o) {
		foreach(var i in items) {
			if(i is IBindPattern ip) {
				ip.Bind(ctx, o);
			}
		}
	}
}
public record VAnyType : IBindPattern {
	public object[] items;
	public object bound;
	public bool Accept (object val) {
		foreach(var i in items) {
			if(ExIs.Is(val, i)) {
				bound = i;
				return true;
			}
		}
		return false;
	}
	public void Bind (IScope ctx, object o) {
		if(bound is IBindPattern ip) {
			ip.Bind(ctx, o);
			return;
		}
	}
}

public record VEmpty {
	public static readonly VEmpty VALUE = new();
}
public record VUp {
	public int up;
	public VRet Ret (object data) => new VRet(data, up);
}
public record ValAuto { }
public record VExtend {
	public object on;
	public bool inherit = true;
}
public record VExtendObj {
	public VExtend call;
	public ExBlock src;
	public object Init (IScope ctx) => Init(ctx, call.on);
	public object Init (IScope ctx, object target) {
		//Add option for inherit
		var scope = new VDictScope(ExMemberBlock.MakeScope(target, ctx), false) { inherit = call.inherit };
		scope.locals["base"] = target;
		src.StagedApply(scope);
		return scope;
	}
}

public record VRest {
	public object type;
}
public record VComplement {
	public object on;
}
public record VGo {
	public VLabel target;
}
public record VRetFrom {
	public IScope home;
	public object data;
}
public record VLabel {
	public int index;
	public VDictScope home;
}


public record VPointer {
	public object type;
	public object value;
}
public record VLazy {
	public Node expr;
	public IScope ctx;
	public bool done = false;
	public object value;
	public object Eval () => expr.Eval(ctx);
}
public record VFn {
	public Node expr;
	public VTuple pars;
	public IScope parent_ctx;
	public VFn Memo (VFn vf) {
		throw new Exception();
	}
	private void InitPars (IScope ctx) {
		foreach(var (k, v) in pars.items) {
			StDefKey.Define(ctx, k, v switch {
				VClass or Type => new VMember { name = k, type = v, mut = true, pub = true, ready = false },

				VRest rest => new VMember { name = k, type = rest.type, mut = true, pub = true, ready = false},
				_ => v

			});
		}
	}
	public object CallPars (IScope caller_ctx, ExTuple pars) {
		return CallFunc(() => pars.EvalTuple(caller_ctx));
	}
	public object CallArgs (VTuple args) {
		return CallFunc(() => args);
	}
	public object CallFunc (Func<VTuple> evalArgs) {
		//Need a way to add vars that only exist in all-parameter context
		//As well as parameter-specific contexts
		/*
		@(par_ctx{
			a:10
			b:20
			c:30
		})
		foo(a b c):{}
		*/

		//Maybe use enum instead
		/*
		baz(@(par_ctx{low:5 high:10}) a)
		baz(low)
		 */

		
		var func_ctx = new VFnScope(this, parent_ctx, false);
		var argData = new VArgs { };
		func_ctx.locals["_arg"] = argData;
		func_ctx.locals["_func"] = this;
		InitPars(func_ctx);
		int ind = 0;
		var args = evalArgs();
		while(ind < args.items.Length) {
			var (k, v) = args.items.ElementAt(ind);
			if(k == null) {
				if(ind == -1) {
					throw new Exception("Cannot have positional arguments after named arguments");
				}
				var p = pars.items[ind];
				if(p.val is VRest) {
					List<object> items = new();
					Add:
					items.Add(v);
					ind += 1;
					if(ind < args.items.Length) {
						(k, v) = args.items.ElementAt(ind);
						if(k == null) {
							goto Add;
						}
					}
					var val = StAssignSymbol.AssignLocal(func_ctx, p.key, () => items);
				} else {
					ind += 1;
					var val = StAssignSymbol.AssignLocal(func_ctx, p.key, () => v);
				}
			} else {
				ind = -1;
				var val = StAssignSymbol.AssignLocal(func_ctx, k, () => v);
			}
		}
		ReadPars(func_ctx, argData);
		var result = expr.Eval(func_ctx);
		return result;
	}
	public object CallVoid (IScope ctx) => CallData([]);
	public object CallData (IEnumerable<object> args) => CallFunc(() => new VTuple {
		items = args.Select(a => ((string)null, (object)a)).ToArray()
	});
	private void ReadPars (IScope func_ctx, VArgs argData) {
		var ind = 0;
		foreach(var p in pars.items) {
			var val = func_ctx.GetLocal(p.key);
			argData.list.Add(val);
			argData.dict[p.key] = val;
			StDefKey.Define(func_ctx, $"_{ind}", val);
			ind += 1;
		}
	}
}
public record VType (Type type) {
	public object Cast (object next, Type nextType) {
		if(type == typeof(void)) {
			return VEmpty.VALUE;
		}
		if(nextType == null) return next;
		if(nextType != type) {
			//return ValError.TYPE_MISMATCH;
			throw new Exception("Type mismatch");
		}
		return next;
	}
}
public record VInterface {
	public Node source;
	public VDictScope _static;
	public void Register (VDictScope target) {
		foreach(var (k, v) in _static.locals) {
			switch(k) {
				case "__classSet__" or "__interfaceSet__" or "__kind__":
					continue;
			}
			if(!target.locals.ContainsKey(k)) {
				throw new Exception($"Does not implement {k}");
			}
		}
		target.AddInterface(this);
	}
}
public class VClass {
	public string name;
	public VDictScope _static;
	public Node source_expr;
	public IScope parent_ctx;
	public VDictScope MakeInstance () => MakeInstance(parent_ctx);
	public VDictScope MakeInstance (IScope scope) {
		var r = (VDictScope)source_expr.Eval(scope);
		r.SetClass(this);
		return r;
	}
	public object VarBlock (IScope ctx, ExBlock block) {
		var scope = MakeInstance();
		var r = block.Apply(new VDictScope { locals = scope.locals, parent = ctx, temp = false });
		return r;
	}
	public void Embed (VDictScope target) {
		foreach(var (k, v) in _static.locals) {
			switch(k) {
				case "__classSet__" or "__interfaceSet__" or "__kind__":
					continue;
			}
			StDefKey.Define(target, k, v);
		}
		target.ClassSet.Add(this);
	}
}

public interface IScope {
	public IScope parent { get; }
	public object Get (string key, int up = -1) =>
	 up == -1 ? GetNearest(key) : GetAt(key, up);
	public object GetLocal (string key) => GetAt(key, 1);
	public object GetAt (string key, int up);
	public object GetNearest (string key);
	public object Set (string key, object val, int up = -1) =>
	 up == -1 ? SetNearest(key, val) : SetAt(key, val, up);
	public object SetLocal (string key, object val) => SetAt(key, val, 1);
	public object SetAt (string key, object val, int up);
	public object SetNearest (string key, object val);
	public IScope Copy (IScope parent);
	public VDictScope MakeTemp () => new() {
		locals = { },
		parent = this,
		temp = true
	};
	public VDictScope MakeTemp (object _) => new() {
		locals = { ["_"] = _ },
		parent = this,
		temp = true
	};
}
public class VTupleScope : IScope {
	public IScope parent { get; set; } = null;
	public VTuple t;
	public IScope Copy (IScope parent) => new VTupleScope { parent = parent, t = t };
	public object GetAt (string key, int up) {
		if(up == 1) {
			if(GetLocal(key, out var v))
				return v;
		} else {
			if(parent != null)
				return parent.GetAt(key, up - 1);
		}
		return new VError($"Unknown variable {key}");
	}
	public bool GetLocal (string key, out object res) {
		foreach(var (k, v) in t.items) {
			if(k == key) {
				res = v;
				return true;
			}
		}
		res = null;
		return false;
	}
	public object GetNearest (string key) =>
	  GetLocal(key, out var v) ? v :
	  parent != null ? parent.GetNearest(key) :
	  new VError($"Unknown variable {key}");
	public bool SetLocal (string key, object val) {
		for(int i = 0; i < t.items.Length; i++) {
			var it = t.items[i];
			if(it.key == key) {
				it.val = val;
			}
		}
		return false;
	}
	public object SetAt (string key, object val, int up) {
		if(up == 1) {
			if(SetLocal(key, val))
				return val;
		} else {
			if(parent != null) {
				return parent.SetAt(key, val, up - 1);
			}
		}
		return new VError($"Unknown variable {key}");
	}
	public object SetNearest (string key, object val) =>
	 SetLocal(key, val) ? val :
	 parent != null ? parent.SetNearest(key, val) :
	 new VError($"Unknown variable {key}");
}
public record VTypeScope : IScope {
	public IScope parent { get; set; } = null;
	public Type t;
	public IScope Copy (IScope parent) => new VTypeScope { parent = parent, t = t };
	BindingFlags FLS = BindingFlags.Static | BindingFlags.Public;
	public object GetLocal (string key) => GetAt(key, 1);
	public object GetAt (string key, int up) {
		if(up == 1) {
			if(GetLocal(key, out var v))
				return v;
		} else {
			if(parent != null)
				return parent.GetAt(key, up - 1);
		}
		return new VError($"Unknown key {key}");
	}
	public object GetNearest (string key) =>
	  GetLocal(key, out var v) ? v :
	  parent != null ? parent.GetNearest(key) :
	  new VError($"Unknown key {key}");
	public bool GetLocal (string key, out object res) {
		if(key == "ctor") {
			res = new VConstructor(t);
			return true;
		}
		if(t.GetProperty(key, FLS) is { } pr) {
			res = pr.GetValue(null);
			return true;
		}
		if(t.GetField(key, FLS) is { } f) {
			res = f.GetValue(null);
			return true;
		}
		if(t.GetMethods().Any(m => m.Name == key)) {
			res = new ValStaticFn(t, key);
			return true;
		}
		res = null;
		return false;
	}
	public object SetAt (string key, object val, int up) {
		if(up == 1) {
			if(SetLocal(key, val))
				return val;
		} else {
			if(parent != null) {
				return parent.SetAt(key, val, up - 1);
			}
		}
		return new VError($"Unknown key {key}");
	}
	public object SetNearest (string key, object val) =>
	 SetLocal(key, val) ? val :
	 parent != null ? parent.SetNearest(key, val) :
	 new VError($"Unknown key {key}");
	public bool SetLocal (string key, object val) {
		if(t.GetProperty(key, FLS) is { } pr) {
			pr.SetValue(null, val);
			return true;
		}
		if(t.GetField(key, FLS) is { } f) {
			f.SetValue(null, val);
			return true;
		}
		return false;
	}
}
public record VObjScope : IScope {
	public IScope parent { get; set; } = null;
	public object o;
	BindingFlags FL = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
	public IScope Copy (IScope parent) => new VObjScope { parent = parent, o = o };
	public object GetLocal (string key) => GetAt(key, 1);
	public object GetAt (string key, int up) {
		if(up == 1) {
			if(GetLocal(key, out var v))
				return v;
		} else {
			if(parent != null)
				return parent.GetAt(key, up - 1);
		}
		return new VError($"Unknown variable {key}");
	}
	public bool GetLocal (string key, out object res) {
		var ot = o.GetType();
		if(ot.GetProperty(key, FL) is { } p) {
			res = p.GetValue(o);
			return true;
		}
		if(ot.GetField(key, FL) is { } f) {
			res = f.GetValue(o);
			return true;
		}
		if(ot.GetMethods().Any(m => m.Name == key)) {
			res = new VInstanceFn(o, key);
			return true;
		}
		res = null;
		return false;
	}
	public object GetNearest (string key) =>
	  GetLocal(key, out var v) ? v :
	  parent != null ? parent.GetNearest(key) :
	  new VError($"Unknown variable {key}");
	public bool SetLocal (string key, object val) {
		var ot = o.GetType();
		if(ot.GetProperty(key, FL) is { } p) {
			p.SetValue(o, val);
			return true;
		}
		if(ot.GetField(key, FL) is { } f) {
			f.SetValue(o, val);
			return true;
		}
		return false;
	}
	public object SetAt (string key, object val, int up) {
		if(up == 1) {
			if(SetLocal(key, val))
				return val;
		} else {
			if(parent != null) {
				return parent.SetAt(key, val, up - 1);
			}
		}
		return new VError($"Unknown variable {key}");
	}
	public object SetNearest (string key, object val) =>
	 SetLocal(key, val) ? val :
	 parent != null ? parent.SetNearest(key, val) :
	 new VError($"Unknown variable {key}");
}
public record VFnScope : IScope {
	public VFn func;
	public bool temp = false;
	public IScope parent { get; set; } = null;
	public ConcurrentDictionary<string, dynamic> locals = new() { };
	public VFnScope (VFn func = null, IScope parent = null, bool temp = false) {
		this.func = func;
		this.temp = temp;
		this.parent = parent;
	}
	public IScope Copy (IScope parent) => new VFnScope { func = func, locals = locals, parent = parent, temp = false };
	public object GetAt (string key, int up) {
		if(up == 1) {
			if(locals.TryGetValue(key, out var v))
				return v;
			else if(!temp)
				return new VError($"Unknown variable {key}");
		}
		return
		 parent != null ? parent.GetAt(key, temp ? up : up - 1) :
		 new VError($"Unknown variable {key}");
	}
	public object GetNearest (string key) =>
	  locals.TryGetValue(key, out var v) ? v :
	  parent != null ? parent.GetNearest(key) :
	  new VError($"Unknown variable {key}");
	public object SetAt (string key, object val, int up) {
		if(temp) {
			return parent.SetAt(key, val, up);
		} else if(up == 1) {
			return locals[key] = val;
		} else {
			if(parent != null) {
				parent.SetAt(key, val, up - 1);
			}
			return new VError($"Unknown variable {key}");
		}
	}
	public object SetNearest (string key, object val) =>
	 locals.TryGetValue(key, out var v) ? locals[key] = val :
	 parent != null ? parent.SetNearest(key, val) :
	 new VError($"Unknown variable {key}");
}
public class VAttCtx {
	public IScope ctx;
}
public record VDictScope : IScope {
	public bool temp = false;

	public bool inherit = false;
	public IScope parent { get; set; } = null;
	public ConcurrentDictionary<string, dynamic> locals = new() { };
	public HashSet<VClass> ClassSet => locals.GetOrAdd("__classSet__", new HashSet<VClass>());
	public HashSet<VInterface> InterfaceSet => locals.GetOrAdd("__interfaceSet__", new HashSet<VInterface>());
	public List<string> KeyList => locals.GetOrAdd("__keyList__", new List<string>());
	public void SetClass (VClass vc) {
		locals["_class"] = vc;
		locals["_proto"] = this;
		ClassSet.Add(vc);
	}
	public bool HasClass (VClass vc) => ClassSet.Contains(vc);
	public void AddInterface (VInterface vi) => InterfaceSet.Add(vi);
	public bool HasInterface (VInterface vi) => InterfaceSet.Contains(vi);
	public VFn? _at => locals.TryGetValue("_at", out var f) ? f : null;
	public bool _seq (out object seq) {
		if(locals.TryGetValue("_seq", out dynamic f)) {
			if(f is VGet vg) {
				f = vg.Get();
			}
			seq = f;
			return true;
		} else {
			seq = null;
			return false;
		}
	}
	public VDictScope (IScope parent = null, bool temp = false) {
		this.temp = temp;
		this.parent = parent;
	}
	/*
	public void Inherit(ValDictScope other) {
		foreach(var(k,v) in other.locals) {
			if(k is "__classSet__" or "__interfaceSet__") {
				continue;
			}
			StmtDefKey.Init(this, k, new ValGetter { ctx = other, expr = new ExprSymbol { key = k, up = 1 } });
		}
	}
	*/
	public IScope Copy (IScope parent) => new VDictScope { locals = locals, parent = parent, temp = false };
	/*
	public object Get(string key, int up = -1) =>
	 up == -1 ? GetNearest(key) : GetAt(key, up);
	*/
	public bool GetLocal (string key, out object v) =>
		(locals.TryGetValue(key, out v));
	public object GetAt (string key, int up) {
		if(up == 1) {
			if(GetLocal(key, out var v))
				return v;
			else if(inherit)
				return parent.GetAt(key, 1);
			else if(!temp)
				return new VError($"Unknown variable {key}");
		}
		return
			parent != null ? parent.GetAt(key, temp ? up : up - 1) :
			new VError($"Unknown variable {key}");
	}
	public object GetNearest (string key) =>
	  locals.TryGetValue(key, out var v) ? v :
	  parent != null ? parent.GetNearest(key) :
	  new VError($"Unknown variable {key}");
	/*
	public object Set (string key, object val, int up = -1) =>
	 up == -1 ? SetNearest(key, val) : SetAt(key, val, up);
	*/
	public object SetAt (string key, object val, int up) {
		if(temp) {
			return parent.SetAt(key, val, up);
		} else if(up == 1) {
			return locals[key] = val;
		} else {
			if(parent != null) {
				parent.SetAt(key, val, up - 1);
			}
			return new VError($"Unknown variable {key}");
		}
	}
	public object SetNearest (string key, object val) =>
	 locals.TryGetValue(key, out var v) ? locals[key] = val :
	 parent != null ? parent.SetNearest(key, val) :
	 new VError($"Unknown variable {key}");
}
public record VArgs {
	public object this[string s] {
		get => dict[s];
		set => dict[s] = value;
	}
	public object this[int s] {
		get => list[s];
		set => list[s] = value;
	}
	public int Length => list.Count;
	public Dictionary<string, object> dict = new();
	public List<object> list = new();

	public object[] items => list.ToArray();
}

public class VSuperVal : LVal {
	public (IType type, object val)[] items;
	public void Assign (object next) {
		foreach(var i in Enumerable.Range(0, items.Length)) {
			if(items[i].type.Accept(next)) {
				items[i].val = next;
				break;
			}
		}
	}

	public object Assign (IScope ctx, Func<object> getVal) {
		Assign(getVal());
		return this;
	}
}
public interface IType {
	public bool Accept (object src);
}
public class VFnInterface : IType {
	public IType[] lhs;
	public IType rhs;
	public bool Accept (object src) {
		return (src is VFn fn && fn.pars.vals.Zip(lhs).All(p => true));
	}
}
public class VRange {
	public int? start;
	public int? end;
	public IEnumerable<int> GetInt () {
		for(var i = start ?? throw new Exception(); i < (end ?? throw new Exception()); i++) { yield return i; }
	}
}
public class VSpread {
	public object value;
	public void SpreadTuple (string key, List<(string key, object val)> it) {
		switch(value) {
			case VTuple vrt:
				vrt.Spread(it);
				break;
			case VEmpty:
				break;
			case Array a:
				foreach(var item in a) {
					it.Add((null, item));
				}
				break;
			case VArgs args: {
					foreach(var item in args.list) {
						it.Add((null, item));
					}
					break;
				}
			default:
				it.Add((key, value));
				break;
		}
	}
	public void SpreadArray (List<object> items) {
		switch(value) {
			case VTuple vrt:
				items.AddRange(vrt.items.Select(i => i.val));
				break;
			case Array a:
				foreach(var _a in a) { items.Add(_a); }
				break;
			case VSpread vs:
				vs.SpreadArray(items);
				break;
			default:
				items.Add(value);
				break;
		}
	}
}
public class VCriteria {
	public object type;
	public Node cond;
	public bool Accept (object o) {
		return ExIs.Is(o, type) && (bool)cond.Eval(ExMemberBlock.MakeScope(o));
	}
}


public record VMember {
	public string name = "";
	public List<VAttribute> attributes = [];
	public bool pub = true;
	public bool mut = true;
	public bool ready = false;
	public object type = typeof(int);
	public object val = 0;
	public void Init (VCast vc) {
		type = vc.type;
		val = vc.val;
	}
	public void Assign (VCast vc) {
		val = vc.val;
	}
}
public record VCast {
	public object type;
	public object val;
}


public class VEnumRecord {
	public VEnumRecordType recordType;
	public VTuple args;
}
public class VEnumRecordType {
	public string name;
	public VTuple pars;

	public object parent;
	public VEnumRecord Make(VTuple args, IScope ctx) {
		return new VEnumRecord { recordType = this, args = args };
	}
}

public class VTypeFn : IType {
	public VFn criteria;
	public bool Accept (object src) {
		return (bool)criteria.CallData([src]);
	}
}
public class VTuple : Node {
	public int Length => items.Length;
	public (string key, object val)[] items;

	public Dictionary<string, object> dict => items.Where(pair => pair.key != null).ToDictionary(p => p.key, p => p.val);
	public object[] vals => items.Select(i => i.val).ToArray();
	public object Eval (IScope ctx) => this;
	public static VTuple Single (object v) => new() { items = [(null, v)] };
	public void Spread (List<(string key, object val)> it) {
		foreach(var (key, val) in items) {
			it.Add((key, val));
		}
	}
	public ExTuple expr => new() {
		items = items.Select(pair => (pair.key, (Node)new ExVal { value = pair.val })).ToArray()
	};
	public void Inherit (IScope ctx) {
		foreach(var (k, v) in items) {
			if(k == null) {
				throw new Exception("95485");
			}
			ctx.SetLocal(k, v);

		}
	}
}

//Problem: We need partial binding for guard patterns
//Solution: Use predicates instead?
public interface IBindPattern {
	public bool Accept (object o);
	public void Bind (IScope ctx, object o);
}
public class VStructurePattern : IBindPattern {
	public bool rest;
	public List<(string lhs, object rhs, string key)> binds;
	public bool Accept (object o) {
		if(o is VDictScope vd) {
			for(var i = 0; i < binds.Count; i++) {
				var b = binds[i];
				var lhs = b.lhs;
				var val = vd.GetAt(lhs, 1) switch {
					VMember vm => vm.val
				};
				var rhs = b.rhs ?? VKeyword.ANYTHING;
				if(!ExIs.Is(val, rhs)) {
					return false;
				}
			}
			return true;
		}
		/*
		if(o is VTuple{ dict:{}dict }) {
			for(var i = 0; i < binds.Count; i++) {
				var b = binds[i];
				var lhs = b.lhs;
				var val = dict[lhs];
				var rhs = b.rhs ?? VKeyword.ANYTHING;
				if(!ExIs.Is(val, rhs)) {
					return false;
				}
			}
			return true;
		}
		*/
		if(o is VEnumRecord { args: { dict: { }dict } } ver) {
			for(var i = 0; i < binds.Count; i++) {
				var b = binds[i];
				var lhs = b.lhs;
				var val = dict[lhs];
				var rhs = b.rhs ?? VKeyword.ANYTHING;
				if(!ExIs.Is(val, rhs)) {
					return false;
				}
			}
			return true;
		}
		return false;
	}
	public void Bind (IScope ctx, object o) {
		var vd = (VDictScope)o;
		for(var i = 0; i < binds.Count; i++) {
			var b = binds[i];
			var lhs = b.lhs;
			var val = vd.GetAt(lhs, 1);
			string key = b.key;
			if(key is { }) {
				ctx.SetLocal(key, val);
			}
			var type = b.rhs;
			if(type is IBindPattern ip) {
				ip.Bind(ctx, val);
			}
		}
	}
}
public class VArrayPattern : IBindPattern {
	public void Bind (IScope ctx, object o) {
		throw new Exception();
	}
	public bool Accept(object o) {
		throw new Exception();
	}
}
public class VTuplePattern : IBindPattern {
	public bool rest;
	public List<(string key, object type)> binds;
	public bool accepted { get; set; }
	public bool Accept (object o) {
		if(binds.Count == 1) {
			var v = binds[0].type;
			if(o is VEnumRecord {args:{Length:1 } } ver) {
				return ExIs.Is(ver.args.items[0].val, v);
			}
			var b = ExIs.Is(o, v);
			return b;
		} else {
			if(o is VTuple vt) {
				for(var i = 0; i < binds.Count; i++) {
					var b = binds[i];
					var val = vt.items[i].val;
					var type = b.type;
					if(!ExIs.Is(val, type)) {
						return false;
					}
				}
				return true;
			}
			if(o is VEnumRecord ver) {
				for(var i = 0; i < binds.Count; i++) {
					var b = binds[i];
					var val = ver.args.items[i].val;
					var type = b.type;
					if(!ExIs.Is(val, type)) {
						return false;
					}
				}
				return true;
			}
		}
		return false;
	}
	public void Bind (IScope ctx, object o) {
		if(binds.Count == 1) {
			var b = binds[0];
			var val = o;
			if(b.key is { } key) {
				ctx.SetLocal(key, val);
			}
			if(b.type is IBindPattern ip) {
				ip.Bind(ctx, val);
			}
			return;
		}
		var vt = (VTuple)o;
		for(var i = 0; i < binds.Count; i++) {
			var b = binds[i];
			var val = vt.items[i].val;
			if(b.key is { } key) {
				ctx.SetLocal(key, val);
			}
			if(b.type is IBindPattern ip) {
				ip.Bind(ctx, val);
			}
		}
	}
}
public class VWildcardPattern : IBindPattern {
	public string key;
	public bool accepted { get; set; }
	public bool Accept (object o) => true;
	public void Bind (IScope ctx, object o) => ctx.SetLocal(key, o);
}
public class VGuardPattern : IBindPattern {
	public Node cond;
	public IScope ctx;
	public bool Accept (IScope ctx, object o) => (bool)cond.Eval(ctx);
	public bool Accept (object o) => (bool)cond.Eval(ctx);
	public void Bind (IScope ctx, object o) {}
}
public record VSet {
	public Node set;
	public IScope ctx;
	public object Set (object val) => Set(set, ctx, val);
	public static object Set (Node expr, IScope ctx, object val) {
		var inner_ctx = ctx.MakeTemp();
		inner_ctx.locals["_val"] = val;
		return expr.Eval(inner_ctx);
	}
}
public record VGet {
	public Node get;
	public IScope ctx;
	public object Get () => Get(get, ctx);
	public static object Get (Node expr, IScope ctx) => expr.Eval(ctx);
}
public record VAlias {
	public Node expr;
	public IScope ctx;
	public object Get () => expr.Eval(ctx);
	public object Set (Func<object> getNext) {
		switch(expr) {
			case ExUpKey es:
				return es.Assign(ctx, getNext);
			case ExTuple et:
				return StAssignMulti.AssignTuple(ctx, et.items.Select(i => (ExUpKey)(i.value)).ToArray(), getNext());
		}
		throw new Exception();
	}
}

