// See https://aka.ms/new-console-template for more information
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
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Oblivia.ExprMap;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;
//TODO: Remove '@' Indexer
namespace Oblivia {

	public class Std {
        public static ValDictScope std;
		static Std () {
			T _<T> (T t) => t;
			Type MakeGeneric (Type gen, params object[] item) => gen.MakeGenericType(item.Select(i => i switch { Type t => t, _ => typeof(object) }).ToArray());
			std = new ValDictScope {
				locals = new() {
                    ["File"] = typeof(File),

					["Pt"] = typeof((int, int)),

					["i4x2"] = _((int a, int b) => (a, b)),

					["i4_f8"] = _((int i) => (double)i),
					["f8_i4"] = _((double d) => (int)d),
					["i4_u4"] = _((int i) => (uint)i),
					["i4_ch"] = _((int i) => (char)i),
					["i4_u1"] = _((int i) => (byte)i),

					["void"] = typeof(void),
					["char"] = typeof(char),
					["bit"] = typeof(bool),

					["i8"] = typeof(long),
					["i4"] = typeof(int),
					["i2"] = typeof(short),
					["i1"] = typeof(sbyte),
					["u8"] = typeof(ulong),
					["u4"] = typeof(uint),
					["u2"] = typeof(ushort),
					["u1"] = typeof(byte),
					["f8"] = typeof(double),
					["f4"] = typeof(float),
					["str"] = typeof(string),
					["obj"] = typeof(object),
					
                    ["yes"] = true,
					["no"] = false,

                    ["parse_char"] = _((string s) => char.Parse(s)),

					["empty"] = ValEmpty.VALUE,
					["default"] = _((Type t) => t.IsValueType ? Activator.CreateInstance(t) : null),
					["addi"] = _((int a, int b) => a + b),
					["addu"] = _((uint a, uint b) => a + b),
					["subi"] = _((int a, int b) => a - b),
					["muli"] = _((int a, int b) => a * b),
					["divi"] = _((int a, int b) => a / b),
					["modi"] = _((int a, int b) => a % b),
					["xori"] = _((int a, int b) => a ^ b),
					["mini"] = _((int a, int b) => Math.Min(a, b)),
					["maxi"] = _((int a, int b) => Math.Max(a, b)),
					["ori"] = _((int a, int b) => a | b),
					["sl"] = _((int a, int b) => a << b),
					["sr"] = _((int a, int b) => a >> b),
					["addf"] = _((double a, double b) => a + b),
					["subf"] = _((double a, double b) => a - b),
					["mulf"] = _((double a, double b) => a * b),
					["divf"] = _((double a, double b) => a / b),
					["modf"] = _((double a, double b) => Math.IEEERemainder(a, b) + b / 2),
					["minf"] = _((double a, double b) => Math.Min(a, b)),
					["maxf"] = _((double a, double b) => Math.Max(a, b)),

					["not"] = _((bool b) => !b),
					["and"] = _((object[] a) => a.All(a => (bool)a)),
					["or"] = _((object[] a) => a.Any(a => (bool)a)),

					["nullor"] = _((object a, object b) => a ?? b),

					["count"] = _((IEnumerable data, object value) => data.Cast<object>().Count(d => {
						var result = d.Equals(value);
						return result;
					})),
					["gt"] = _((double a, double b) => a > b),
					["geq"] = _((double a, double b) => a >= b),
					["lt"] = _((double a, double b) => a < b),
					["leq"] = _((double a, double b) => a <= b),

					["bt"] = _((double a, double b, double c) => a > b && a < c),
					["beq"] = _((double a, double b, double c) => a >= b && a <= c),

					["eq"] = _((object a, object b) => Equals(a, b)),
					["neq"] = _((object a, object b) => !Equals(a, b)),

					["cat"] = _((object[] o) => string.Join(null, o)),
					["range"] = _((int a, int b) => Enumerable.Range(a, b - a).ToArray()),
					["newline"] = "\n",
					["obj_str"] = _((object o) => o.ToString()),

                    ["ch_arr"] = _((string s) => s.ToCharArray()),

					["Array"] = _((Type type, int dim) => type.MakeArrayType(dim)),
					["arr_get"] = _((Array a, int[] ind) => a.GetValue(ind)),
					["arr_set"] = _((Array a, int[] ind, object value) => a.SetValue(value, ind)),
					["arr_at"] = _((Array a, int[] ind) => new ValRef { src = a, index = ind }),
					["str_append"] = _((StringBuilder sb, object o) => sb.Append(o)),
                    ["append"] = _((string a, string b) => a + b),
					["append_ch"] = _((string a, char b) => a + b),
					["row_from"] = _((Type t, object[] items) => {
						var result = Array.CreateInstance(t, items.Length);
						Array.Copy(items, result, items.Length);
						return result;
					}),
					["rand_bool"] = _(() => new Random().Next(2) == 1),
					["randf"] = _(new Random().NextDouble),
					["rand_range"] = _((int a, int b) => new Random().Next(a, b)),
					["Row"] = _((object type) => (type as Type ?? typeof(object)).MakeArrayType(1)),
					["Grid"] = _((Type type) => type.MakeArrayType(2)),
					["List"] = _((object item) => MakeGeneric(typeof(List<>), item)),
					["HashSet"] = _((object item) => MakeGeneric(typeof(HashSet<>), item)),
					["Dict"] = _((Type key, Type val) => typeof(Dictionary<,>).MakeGenericType(key, val)),
					["ConcDict"] = _((Type key, Type val) => typeof(ConcurrentDictionary<,>).MakeGenericType(key, val)),
					["StrBuild"] = typeof(StringBuilder),
					["Fn"] = typeof(ValFunc),
					["PQ"] = _((object a, object b) => MakeGeneric(typeof(PriorityQueue<,>), a, b)),
					["class"] = ValKeyword.CLASS,
					["interface"] = ValKeyword.INTERFACE,
					["ext"] = ValKeyword.EXTEND,
					["enum"] = ValKeyword.ENUM,
					["get"] = ValKeyword.GET,
					["set"] = ValKeyword.SET,
					["impl"] = ValKeyword.IMPLEMENT,
					["inherit"] = ValKeyword.INHERIT,
					["cut"] = ValKeyword.BREAK,
					["skip"] = ValKeyword.CONTINUE,
					["cancel"] = ValKeyword.CANCEL,
					["ret"] = ValKeyword.RETURN,
					["var"] = ValKeyword.VAR,
					["yield"] = ValKeyword.YIELD,
					["unmask"] = ValKeyword.UNALIAS,
					["declare"] = ValKeyword.DECLARE,
					["complement"] = ValKeyword.COMPLEMENT,
					["any"] = ValKeyword.ANY,
					["all"] = ValKeyword.ALL,
					["fmt"] = ValKeyword.FMT,
					["regex"] = ValKeyword.REGEX,
					["replace"] = ValKeyword.REPLACE,
					["macro"] = ValKeyword.MACRO,
                    ["magic"] = ValKeyword.MAGIC,
                    ["label"] = ValKeyword.LABEL,
                    ["go"] = ValKeyword.GO,
				}
			};
		}
	}

    public class ValMagic();
	public class ValError {
        public string msg;
        public ValError () { }
        public ValError (string msg) {
            this.msg = msg;
        }
    }
    public record ValRef {
        public Array src;
        public int[] index;
        public void Set (object value) =>
         src.SetValue(value, index);
        public object Get () => src.GetValue(index);
    }
    public record ValConstructor (Type t) { }
    public record ValInstanceMethod (object src, string key) {
        public object Call (object[] data) {
            var tp = data.Select(d => (d as object).GetType()).ToArray();
            var fl = BindingFlags.Instance | BindingFlags.Public;
            return src.GetType().GetMethod(key, fl, tp).Invoke(src, data);
        }
    };
    public record ValStaticMethod (Type src, string key) {
        public object Call (object[] data) {
            var tp = data.Select(d => (d as object).GetType()).ToArray();
            var fl = BindingFlags.Static | BindingFlags.Public;
            return src.GetMethod(key, fl, tp).Invoke(src, data);
        }
    };
    public record ValReturn (object data, int up) {
        public object Up () => up == 1 ? data : this with { up = up - 1 };
    }
    public record ValYield(object data);
    public enum ValKeyword {
        CLASS,INTERFACE,ENUM,
        GET,SET,PROP,

        MIMIC,
        IMPLEMENT,INHERIT,
        BREAK,CONTINUE,
        RETURN,YIELD,

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
        ANY,ALL,
        TYPE,
        FMT,REGEX,
        FN,
        MAKE,
        PUB, PRIV, STATIC, MACRO, TEMPLATE,


        FIELDS_OF,METHODS_OF,MEMBERS_OF,
        ATTR,
        MARK,
        XML,JSON,

        PREV_ITER,SEEK_ITER,

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

        //Create a label
        LABEL,
        //Go to label
        GO,
        //Go to the top of the current scope
        REDO,

        NOP,
        //Creates a magic constant e.g. null
        MAGIC,
    }
    public class ValMultiPattern {
        public object[] items;
        public bool all = false;
        public bool Accept (object val) {
            if(all) {
				return items.All(i => ExprMatch.Is(val, i));
			} else {
                return items.Any(i => ExprMatch.Is(val, i));
			}
        }
    }
    public class ValEmpty {
        public static readonly ValEmpty VALUE = new();
    }
    public class ValDeclared {
        public object type;
    }
    public class ValAuto { }
    public class FnTicket {
        public int argCount;
		public object Init (IScope ctx, INode src) {
			return new ValFunc { expr = src, parent_ctx = ctx, pars = new ValTuple { } };
		}
	}
    public class ExtendTicket {
        public object on;
		public bool inherit = true;
	}
	public class ExtendObject {
        public ExtendTicket call;
        public ExprBlock src;
        public object Init (IScope ctx) => Init(ctx, call.on);
        public object Init(IScope ctx, object target) {
            //Add option for inherit
            var scope = new ValDictScope(ExprApply.MakeScope(target, ctx), false) { inherit = call.inherit };
            scope.locals["base"] = target;
            src.StagedApply(scope);
            return scope;
        }
    }
    //Any methods called will return the complement of the result
    public class Complement {
        public object on;
    }
    public record ValPattern { }
	public record ValGo {
		public ExprSymbol target;

		public object Up () => this with { target = target with { up = target.up - 1 } };
	}
    public record ExprGetter : INode {
        public INode get;
        public object Eval (IScope ctx) => new ValGetter { ctx = ctx, get = get };
    }
    public record ValSetter {
        public INode set;
        public IScope ctx;
        public object Set (object val) => Set(set, ctx, val);
        public static object Set(INode expr, IScope ctx, object val) {
			var inner_ctx = ctx.MakeTemp();
			inner_ctx.locals["_val"] = val;
			return expr.Eval(inner_ctx);
		}
    }
    public record ValGetter {
        public INode get;
        public IScope ctx;
		public object Get () => Get(get, ctx);
		public static object Get (INode expr, IScope ctx) => expr.Eval(ctx);
	}
    public record ValProp {
        public INode get, set;
        public IScope ctx;
        public object Get () => ValGetter.Get(get, ctx);
        public object Set (object val) => ValSetter.Set(set, ctx, val);
    }
    public record ValAlias {
        public INode expr;
        public IScope ctx;
        public object Deref() => expr.Eval(ctx);
        public object Set(Func<object> getNext) {
            switch(expr) {
                case ExprSymbol es:
					return es.Assign(ctx, getNext);
				case ExprTuple et:
                    return StmtAssignTuple.Assign(ctx, et.items.Select(i => (ExprSymbol)(i.value)).ToArray(), getNext());
            }
            throw new Exception();
        }
	}
    public record ExprAlias:INode {
        public INode expr;
        public object Eval (IScope ctx) => new ValAlias { ctx = ctx, expr = expr };
    }
	public record ValLazy {
		public INode expr;
		public IScope ctx;
        public bool done = false;
        public object value;
		public object Eval () => expr.Eval(ctx);
	}
	public record Args {
        public object this[string s] => dict[s];
        public object this[int s] => list[s];
        public int Length => list.Count;
        public Dictionary<string, object> dict = new();
        public List<object> list = new();
    }
    public record ValFunc {
        public INode expr;
        public ValTuple pars;
        public IScope parent_ctx;
        public ValFunc Memo(ValFunc vf) {
            throw new Exception();
        }
        private void InitPars(IScope ctx) {
			foreach(var (k, v) in pars.items) {
				StmtDefKey.Init(ctx, k, v);
			}
		}
        public object CallPars (IScope caller_ctx, ExprTuple pars) {
            return CallFunc(caller_ctx, () => pars.EvalTuple(caller_ctx));
        }
        public object CallArgs (IScope caller_ctx, ValTuple args) {
            return CallFunc(caller_ctx, () => args);
        }
        public object CallFunc (IScope caller_ctx, Func<ValTuple> evalArgs) {
            var func_ctx = new ValFuncScope(this, parent_ctx, false);
            var argData = new Args { };
            func_ctx.locals["_arg"] = argData;
            func_ctx.locals["_func"] = this;
            InitPars(func_ctx);
            int ind = 0;
            foreach(var (k, v) in evalArgs().items) {
                if(k != null) {
                    ind = -1;
                    var val = StmtAssignSymbol.AssignLocal(func_ctx, k, () => v);
                } else {
                    if(ind == -1) {
                        throw new Exception("Cannot have positional arguments after named arguments");
                    }
                    var p = pars.items[ind];
                    var val = StmtAssignSymbol.AssignLocal(func_ctx, p.key, () => v);
                    ind += 1;
                }
            }
            ReadPars(func_ctx, argData);
            var result = expr.Eval(func_ctx);
            return result;
        }
        public object CallVoid (IScope ctx) => CallData(ctx, []);
        public object CallData (IScope caller_ctx, IEnumerable<object> args) => CallFunc(caller_ctx, () => new ValTuple {
            items = args.Select(a => ((string) null, (object) a)).ToArray()
        });
		private void ReadPars (IScope func_ctx, Args argData) {
			var ind = 0;
			foreach(var p in pars.items) {
				var val = func_ctx.GetLocal(p.key);
				argData.list.Add(val);
				argData.dict[p.key] = val;
				StmtDefKey.Init(func_ctx, $"_{ind}", val);
				ind += 1;
			}
		}
	}
    public record ValType (Type type) {
        public object Cast (object next, Type nextType) {
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
        public INode source;
        public ValDictScope _static;
        public void Register (ValDictScope target) {
            foreach(var (k, v) in _static.locals) {
                switch(k) {
                    case "_classSet" or "_interfaceSet" or "_kind":
                        continue;
                }
                if(!target.locals.ContainsKey(k)) {
                    throw new Exception($"Does not implement {k}");
                }
            }
            target.AddInterface(this);
        }
    }
    public class ValClass {
        public string name;
        public ValDictScope _static;
        public INode source_expr;
        public IScope source_ctx;
        public ValDictScope MakeInstance () => MakeInstance(source_ctx);
        public ValDictScope MakeInstance (IScope scope) {
            var r = (ValDictScope)source_expr.Eval(scope);
            r.AddClass(this);
            return r;
        }
        public object VarBlock (IScope ctx, ExprBlock block) {
            var scope = MakeInstance();
            var r = block.Apply(new ValDictScope { locals = scope.locals, parent = ctx, temp = false });
            return r;
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
        public ValDictScope MakeTemp () => new ValDictScope {
            locals = { },
            parent = this,
            temp = true
        };
        public ValDictScope MakeTemp (object _) => new ValDictScope {
            locals = { ["_"] = _ },
            parent = this,
            temp = true
        };
    }
    public class ValTupleScope : IScope {
        public IScope parent { get; set; } = null;
        public ValTuple t;
        public IScope Copy (IScope parent) => new ValTupleScope { parent = parent, t = t };
        public object GetAt (string key, int up) {
            if(up == 1) {
                if(GetLocal(key, out var v))
                    return v;
            } else {
                if(parent != null)
                    return parent.GetAt(key, up - 1);
            }
            return new ValError($"Unknown variable {key}");
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
          new ValError($"Unknown variable {key}");
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
            return new ValError($"Unknown variable {key}");
        }
        public object SetNearest (string key, object val) =>
         SetLocal(key, val) ? val :
         parent != null ? parent.SetNearest(key, val) :
         new ValError($"Unknown variable {key}");
    }
    public record ValTypeScope : IScope {
        public IScope parent { get; set; } = null;
        public Type t;
        public IScope Copy (IScope parent) => new ValTypeScope { parent = parent, t = t };
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
            return new ValError($"Unknown variable {key}");
        }
        public object GetNearest (string key) =>
          GetLocal(key, out var v) ? v :
          parent != null ? parent.GetNearest(key) :
          new ValError($"Unknown variable {key}");
		public bool GetLocal (string key, out object res) {
			if(key == "ctor") {
				res = new ValConstructor(t);
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
				res = new ValStaticMethod(t, key);
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
            return new ValError($"Unknown variable {key}");
        }
        public object SetNearest (string key, object val) =>
         SetLocal(key, val) ? val :
         parent != null ? parent.SetNearest(key, val) :
         new ValError($"Unknown variable {key}");
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
    public record ValObjectScope : IScope {
        public IScope parent { get; set; } = null;
        public object o;
        BindingFlags FL = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        public IScope Copy (IScope parent) => new ValObjectScope { parent = parent, o = o };
        public object GetLocal (string key) => GetAt(key, 1);
        public object GetAt (string key, int up) {
            if(up == 1) {
                if(GetLocal(key, out var v))
                    return v;
            } else {
                if(parent != null)
                    return parent.GetAt(key, up - 1);
            }
            return new ValError($"Unknown variable {key}");
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
                res = new ValInstanceMethod(o, key);
                return true;
            }
            res = null;
            return false;
        }
        public object GetNearest (string key) =>
          GetLocal(key, out var v) ? v :
          parent != null ? parent.GetNearest(key) :
          new ValError($"Unknown variable {key}");
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
            return new ValError($"Unknown variable {key}");
        }
        public object SetNearest (string key, object val) =>
         SetLocal(key, val) ? val :
         parent != null ? parent.SetNearest(key, val) :
         new ValError($"Unknown variable {key}");
    }

    public record ValFuncScope : IScope {
        public ValFunc func;
        public bool temp = false;
        public IScope parent { get; set; } = null;
        public ConcurrentDictionary<string, dynamic> locals = new() { };
        public ValFuncScope (ValFunc func = null, IScope parent = null, bool temp = false) {
            this.func = func;
            this.temp = temp;
            this.parent = parent;
        }
        public IScope Copy (IScope parent) => new ValFuncScope { func = func, locals = locals, parent = parent, temp = false };
        public object GetAt (string key, int up) {
            if(up == 1) {
                if(locals.TryGetValue(key, out var v))
                    return v;
                else if(!temp)
                    return new ValError($"Unknown variable {key}");
            }
            return
             parent != null ? parent.GetAt(key, temp ? up : up - 1) :
             new ValError($"Unknown variable {key}");
        }
        public object GetNearest (string key) =>
          locals.TryGetValue(key, out var v) ? v :
          parent != null ? parent.GetNearest(key) :
          new ValError($"Unknown variable {key}");
        public object SetAt (string key, object val, int up) {
            if(temp) {
                return parent.SetAt(key, val, up);
            } else if(up == 1) {
                return locals[key] = val;
            } else {
                if(parent != null) {
                    parent.SetAt(key, val, up - 1);
                }
                return new ValError($"Unknown variable {key}");
            }
        }
        public object SetNearest (string key, object val) =>
         locals.TryGetValue(key, out var v) ? locals[key] = val :
         parent != null ? parent.SetNearest(key, val) :
         new ValError($"Unknown variable {key}");
    }
    public record ValDictScope : IScope {
        public bool temp = false;

        public bool inherit = false;
        public IScope parent { get; set; } = null;
        public ConcurrentDictionary<string, dynamic> locals = new() {};
        public HashSet<ValClass> ClassSet => locals.GetOrAdd("_classSet", new HashSet<ValClass>() );
		public HashSet<ValInterface> InterfaceSet => locals.GetOrAdd("_interfaceSet", new HashSet<ValInterface>());
		public void AddClass (ValClass vc) {
            locals["_class"] = vc;
            locals["_proto"] = this;
            ClassSet.Add(vc);
        }
        public bool HasClass (ValClass vc) => ClassSet.Contains(vc);
		public void AddInterface (ValInterface vi) => InterfaceSet.Add(vi);
        public bool HasInterface (ValInterface vi) => InterfaceSet.Contains(vi);
        public ValFunc? _at => locals.TryGetValue("_at", out var f) ? f : null;
        public bool _seq(out object seq) {
            if(locals.TryGetValue("_seq", out dynamic f)) {
                if(f is ValGetter vg) {
                    f = vg.Get();
                }
                seq = f;
                return true;
            } else {
                seq = null;
                return false;
            }
        }
        public ValDictScope (IScope parent = null, bool temp = false) {
            this.temp = temp;
            this.parent = parent;
        }
        /*
        public void Inherit(ValDictScope other) {
            foreach(var(k,v) in other.locals) {
                if(k is "_classSet" or "_interfaceSet") {
                    continue;
                }
                StmtDefKey.Init(this, k, new ValGetter { ctx = other, expr = new ExprSymbol { key = k, up = 1 } });
            }
        }
        */
        public IScope Copy (IScope parent) => new ValDictScope { locals = locals, parent = parent, temp = false };
        /*
        public object Get(string key, int up = -1) =>
         up == -1 ? GetNearest(key) : GetAt(key, up);
        */

        public bool GetLocal(string key, out object v) =>
            (locals.TryGetValue(key, out v));
        public object GetAt (string key, int up) {
            if(up == 1) {
                if(GetLocal(key, out var v))
                    return v;
                else if(inherit)
                    return parent.GetAt(key, 1);
                else if(!temp)
                    return new ValError($"Unknown variable {key}");
            }
            return
             parent != null ? parent.GetAt(key, temp ? up : up - 1) :
             new ValError($"Unknown variable {key}");
        }
        public object GetNearest (string key) =>
          locals.TryGetValue(key, out var v) ? v :
          parent != null ? parent.GetNearest(key) :
          new ValError($"Unknown variable {key}");
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
                return new ValError($"Unknown variable {key}");
            }
        }
        public object SetNearest (string key, object val) =>
         locals.TryGetValue(key, out var v) ? locals[key] = val :
         parent != null ? parent.SetNearest(key, val) :
         new ValError($"Unknown variable {key}");
    }
    public class Parser {
        int index;
        List<Token> tokens;
        public Parser (List<Token> tokens) {
            this.tokens = tokens;
        }

        public static ExprBlock FromFile(string path) {
			var tokenizer = new Tokenizer(File.ReadAllText(path));
			return new Parser(tokenizer.GetAllTokens()).NextBlock();
		}
        void inc () => index++;
        void dec () => index--;
        public Token currToken => tokens[index];
        public TokenType tokenType => currToken.type;
        public INode NextStatement () {
            var lhs = NextExpression();
            switch(tokenType) {
				case TokenType.COLON:
                    inc();
                    switch(lhs) {

                        case ExprTuple et:
                            switch(tokenType) {
                                case TokenType.EQUAL: {
                                        inc();
                                        List<ExprSymbol> symbols = [];
                                        foreach(var item in et.items) {
                                            if(item.value is ExprSymbol s) {
                                                symbols.Add(s);
                                            } else {
                                                throw new Exception();
                                            }
                                        }
                                        return new StmtAssignTuple { symbols = symbols.ToArray(), value = NextExpression() };
                                    }
                                default: {
                                        List<string> symbols = [];
                                        foreach(var item in et.items) {
                                            if(item.value is ExprSymbol { key: { } key, up: -1 or 1 }) {
                                                symbols.Add(key);
                                            } else {
                                                throw new Exception();
                                            }
                                        }
                                        return new StmtDefTuple { lhs = symbols.ToArray(), rhs = NextExpression() };
                                    }
                            }


                        case ExprSymbol { up: { } up } es:
                            switch(tokenType) {
                                case TokenType.EQUAL:
                                    inc();
                                    return new StmtAssignSymbol { symbol = es, value = NextExpression() };
                                default:
                                    switch(up) {
                                        case -1 or 1:
                                            return new StmtDefKey {
                                                key = es.key,
                                                value = NextExpression()
                                            };
                                        default:
                                            throw new Exception("Can only define in current scope");
                                    }
                            }
                        case ExprSelf { up: { } up }:
                            switch(up) {
                                case > 1:
                                    switch(tokenType) {
                                        case TokenType.EQUAL:
											throw new Exception("Deprecated");
											inc();
                                            return new StmtReturn { val = NextExpression(), up = up };
                                        default:
											return new StmtReturn { val = NextExpression(), up = up };
											throw new Exception("Multi-level return must be assignment");
                                    }
                                default:
                                    return new StmtReturn { val = NextExpression(), up = up };
                            }
                        case ExprInvoke { expr: ExprSymbol { up: { } up, key: { } key }, args: { } args }:
                            switch(up) {
                                case -1 or 1:
                                    return new StmtDefFunc {
                                        key = key,
                                        pars = args.ParTuple(),
                                        value = NextExpression()
                                    };
                                default:
                                    throw new Exception("Cannot define non-local function");
                            }
                        case ExprApply { lhs:ExprSelf{up: 1 or -1 }, rhs: ExprTuple{ }et } ea:
                            return new StmtDefFunc {
                                key = "_call",
                                pars = et,
                                value = NextExpression()
                            };
                        case ExprInvoke { expr: ExprSelf { up: 1 or -1 }, args: ExprTuple et } ei:
							return new StmtDefFunc {
								key = "_call",
								pars = et,
								value = NextExpression()
							};
                        case ExprBlock eb:
                            switch(tokenType) {
                                case TokenType.EQUAL:

                                    break;
                                default:
									var rhs = NextExpression();
                                    return new StmtDefTuple {
                                        lhs = eb.statements.Select(st => st is ExprSymbol { key: { } k, up: -1 } es ? es.key : throw new Exception()).ToArray(),
                                        rhs = rhs,
                                        structural = true
                                    };
                            }
                            throw new Exception("impl destructuring");
						default:
							switch(tokenType) {
								case TokenType.EQUAL: {
										inc();
										return new StmtAssignSymbol { };
										throw new Exception("TODO");
									}
							}
							throw new Exception("Cannot define this");
                    }
                default:
                    return NextExpression(lhs);
            }
        }
        public INode NextPattern () => NextExpression();
        public INode NextExpression () {
            var lhs = NextTerm();
            return NextExpression(lhs);
        }
        public INode NextExpression (INode lhs) {
            Start:
            switch(tokenType) {

                case TokenType.SPACE:
                    inc();
                    goto Start;
                case TokenType.L_CURLY:

                    return NextExpression(new ExprVarBlock { type = lhs, source_block = NextBlock() });
                case TokenType.PIPE: {
                        inc();
                        switch(tokenType) {
                            case TokenType.SPARK:
                                inc();
                                return NextExpression(new ExprMap { src = lhs, map = new ExprInvoke { expr = new ExprSelf { up = 1 }, args = ExprTuple.SingleExpr(NextExpression())  } , expr = true});
                            case TokenType.DOT:
								inc();
								return NextExpression(new ExprMap { src = lhs, map = new ExprInvoke { expr = new ExprSelf { up = 1 }, args = ExprTuple.SingleExpr(NextTerm()) }, expr = true });

							case TokenType.SLASH:
								inc();
								return NextExpression(new ExprMap { src = lhs, map = NextExpression(), expr = true });
                            default: {
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
                                    return NextExpression(new ExprMap { src = lhs, cond = cond, type = type, map = NextTerm() });
                                }
						}
                    }
                case TokenType.L_PAREN: {
                        inc();
                        return NextExpression(new ExprInvoke { expr = lhs, args = NextArgTuple() });
                    }
                case TokenType.SPARK: {
						inc();
                        switch(tokenType) {
                            case TokenType.PIPE:
                                inc();
								return NextExpression(new ExprMap {
									src = NextExpression(),
									map = lhs,
								});
                            default:
								return NextExpression(new ExprInvoke {
									expr = lhs,
									args = ExprTuple.SpreadExpr(NextExpression()),
								});
						}
					}
                case TokenType.DOT: {
                        inc();
                        switch(tokenType) {
                            case TokenType.PIPE:
                                inc();
								return NextExpression(new ExprMap {
									src = NextTerm(),
									map = lhs,
								});
                            default:
								return NextExpression(new ExprInvoke { expr = lhs, args = ExprTuple.SpreadExpr(NextTerm()) });
						}
                    }
                case TokenType.SHOUT: {
                        inc();
                        return NextExpression(new ExprInvoke { expr = lhs, args = ExprTuple.Empty });
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
                            default:
                                var pattern = NextExpression();
                                switch(tokenType) {
                                    case TokenType.COLON:
                                        inc();
                                        var symbol = NextSymbol();
                                        return new ExprMatch { lhs = lhs, rhs = pattern, key = symbol.key };
                                    default:
                                        return new ExprMatch{ lhs = lhs, rhs = pattern, key = "_"};
                                }
                                throw new Exception();
                        }
                        throw new Exception();
                    }
                case TokenType.SLASH: {
                        inc();
                        switch(tokenType) {
                            case TokenType.SLASH:
                                inc();
                                //INDEXER
                                return NextExpression(new ExprAt { src = lhs, index = [NextTerm()] });
                            case TokenType.NAME:
                                var name = currToken.str;
                                inc();
                                return NextExpression(new ExprGet { src = lhs, key = name });
                            case TokenType.L_CURLY:
                                return NextExpression(new ExprApply { lhs = lhs, rhs = NextExpression(), local = false });
                            default:
                                return NextExpression(new ExprApply { lhs = lhs, rhs = NextExpression(), local = true });
                        }
                    }
                    /*
                case TokenType.SWIRL: {
                        inc();
                        switch(tokenType) {
                            case TokenType.PIPE:
                                inc();
                                return NextExpression(new ExprMap { src = NextExpression(), map = new ExprAt { src = lhs, index = [new ExprSelf { up = 1 }] }, expr = true });
                                throw new Exception();
                        }
                        return NextExpression(new ExprAt { src = lhs, index = [NextTerm()] });
                    }
                    */
                case TokenType.L_SQUARE: {
                        var arr = NextArray();

                        return NextExpression( new ExprAt { src = lhs, index = arr.items });
                    }
                    /*
				case (TokenType.PLUS): {
						inc();
						var positive = NextExpression();
						var negative = default(INode);
						switch(tokenType) {
							case TokenType.DASH: {

									inc();
									negative = NextExpression();
									break;
								}
						}
						return NextExpression(new ExprBranch {
							condition = lhs,
							positive = positive,
							negative = negative
						});
					}
                    */

				case TokenType.QUERY: {
                        inc();
                        switch(tokenType) {
                            case TokenType.PERCENT: {
									inc();
									return NextExpression(new ExprLoop { condition = lhs, positive = NextExpression() });
                                }
                            case TokenType.COLON: {
                                    inc();
                                    return new ExprCriteria { item = lhs, cond = NextExpression() };
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
                                    var cond = NextExpression();
                                    switch(tokenType) {
                                        case TokenType.COLON: {
                                                inc();
                                                var yes = NextExpression();
                                                items.Add((cond, yes, null));
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
                                                            dec();
                                                            break;
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
            Read:
            switch(tokenType) {
                case TokenType.L_SQUARE:
                    return NextArray();
                case TokenType.QUERY:
                    return NextLambda();
                case TokenType.NAME:
                    return NextSymbol();
                case TokenType.CARET:
                    return NextCaretSymbol();
                case TokenType.STRING:
                    return NextString();
                case TokenType.INTEGER:
                    return NextInteger();
                case TokenType.L_CURLY:
                    return NextBlock();
                case TokenType.L_PAREN:
                    return NextTupleOrExpression();
                    /*
                case TokenType.PERCENT:
                    return new ExprSpread { value= NextExpression() };
                    */
                case TokenType.QUOTE:
                    inc();
                    return new ExprAlias { expr = NextExpression() };
                case TokenType.COMMA:
                    inc();
                    goto Read;
                case TokenType.SPACE:
                    inc();
                    goto Read;
            }
            throw new Exception($"Unexpected token in expression: {currToken.type}");
        }
        INode NextTupleOrExpression () {
            inc();

            switch(tokenType) {
                case TokenType.PLUS:
                    return NextLisp(0);
				case TokenType.DASH:
					return NextLisp(1);
				case TokenType.SPARK:
					return NextLisp(2);
				case TokenType.SLASH:
					return NextLisp(3);
				case TokenType.PIPE:
					return NextLisp(4);
				case TokenType.AMPERSAND:
					return NextLisp(5);
				case TokenType.PERCENT:
					return NextLisp(6);
				case TokenType.EQUAL:
					return NextLisp(7);
			}
			INode NextLisp (int op) {
                return null;
            }

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
                    inc();
                    return ExprTuple.Empty;
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
                switch(lhs) {
                    case ExprSymbol { up: -1, key: { } key }: {
                            inc();
                            var val = NextExpression();
                            items.Add((key, val));
                            break;
                        }
                    default:
                        throw new Exception("Name expected");
                }
            }
        }
        ExprSeq NextArray () {
            List<INode> items = [];
            inc();
            INode type = null;
            switch(tokenType) {
                case TokenType.COLON:
                    inc();
                    type = NextExpression();
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
                    var item = NextExpression();
                    /*
                    switch(tokenType) {
                        case TokenType.COLON:
                            type = item;
                            inc();
                            goto Check;
                    }
                    */
                    items.Add(item);
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
                        return new ExprFunc { pars = ExprTuple.Empty, result = result };
                    }
                case TokenType.L_PAREN:
                    inc();
                    var pars = NextArgTuple().ParTuple();
					switch(tokenType) {
						case TokenType.COLON:
							inc();
							break;
					}
					var r = new ExprFunc { pars = pars, result = NextExpression() };
                    return r;
                default:
                    throw new Exception($"Unexpected token {t}");
            }
        }
        ExprSymbol NextSymbol () {
            var name = currToken.str;
            inc();
            return new ExprSymbol { key = name, up = -1 };
        }
        INode NextCaretSymbol () {
            inc();
            int up = 1;
            Check:
            switch(tokenType) {
                case TokenType.CARET: {
                        up += 1;
                        inc();
                        goto Check;
                    }
                case TokenType.NAME: {
                        var s = new ExprSymbol { up = up, key = currToken.str };
                        inc();
                        return s;
                    }
                case TokenType.CASH: {
                        //Return This
                        var s = new ExprSelf { up = up };
                        inc();
                        return s;
                    }
                case TokenType.L_PAREN:
                    return new ExprApply { lhs = new ExprSelf { up = up }, rhs = NextExpression(), local = true };
                default:
                    return new ExprSelf { up = up };
            }
        }
        public ExprVal NextString () {
            var value = tokens[index].str;
            inc();
            return new ExprVal { value = value };
        }
        public ExprVal NextInteger () {
            var value = int.Parse(tokens[index].str);
            inc();
            return new ExprVal { value = value };
        }
        public ExprBlock NextBlock () {
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
                default:
                    ele.Add(NextStatement());
                    goto Check;
            }
            throw new Exception($"Unexpected token in object expression: {currToken.type}");
        }
    }
    public class ExprSpread : INode {
        public INode value;
        public object Eval (IScope ctx) {
            //TODO: alias
            return Handle(value.Eval(ctx));
            object Handle(object val) {
				switch(val) {
					case ValTuple vt:
						return new ValSpread { value = vt };
					case Array a:
						return new ValSpread { value = a };
					case ValAlias va:
                        //TODO: fix
                        return Handle(va.Deref());
					case ValEmpty:
						return ValEmpty.VALUE;
					case null: return null;
					default: return val;
				}
				throw new Exception("Tuple or array or record expected");
			}
        }
    }
    public class ValSpread {
        public object value;
        public void SpreadTuple (string key, List<(string key, object val)> it) {
            switch(value) {
                case ValTuple vrt:
                    vrt.Spread(it);
                    break;
                case ValEmpty:
                    break;
                case Array a:
                    foreach(var item in a) {
                        it.Add((null, a));
                    }
                    break;
                default:
                    it.Add((key, value));
                    break;
            }
        }
        public void SpreadArray (List<object> items) {
            switch(value) {
                case ValTuple vrt:
                    items.AddRange(vrt.items.Select(i => i.val));
                    break;
                case Array a:
                    foreach(var _a in a) {items.Add(_a);}
                    break;
                case ValSpread vs:
                    vs.SpreadArray(items);
                    break;
                default:
                    items.Add(value);
                    break;
            }
        }
    }
    public class ExprVarBlock : INode {
        public INode type;
        public ExprBlock source_block;
        public XElement ToXML () => new("VarBlock", new XAttribute("type", type), source_block.ToXML());
        public string Source => $"{type} {source_block.Source}";
        public object MakeScope (ValDictScope ctx) => source_block.MakeScope(ctx);
        public object Eval (IScope ctx) {
            var getResult = () => (object)source_block.Eval(ctx);
            var t = type.Eval(ctx);

            var getArgs = () => {
				var args = getResult();
				switch(args) {
					case ValDictScope vds:
                        return new ValTuple { items = vds.locals.Select(pair => (pair.Key, pair.Value)).ToArray() };
					default:
                        return ExprTuple.SpreadVal(args).EvalTuple(ctx);
				}
			};

            switch(t) {
                case ValEmpty: throw new Exception("Type not found");
                case Type tt: {
                        var v = getResult();
                        var r = new ValType(tt).Cast(v, v?.GetType());
                        return r;
                        return new ValCast { type = v?.GetType(), val = r };
                    }
                case ValClass vc: {
                        return vc.VarBlock(ctx, source_block);
                    }
                case ValInterface vi: {
                        break;
                    }
                case ExtendTicket ex:
                    switch(ex.on) {
                        case Type:
						case ValPattern:
						case ValClass:
							return new ExtendObject { call = ex, src = source_block };
                        default:
							return new ExtendObject { call = ex, src = source_block }.Init(ctx);
					}
                case ValKeyword.GET: {
                        return new ValGetter { ctx = ctx, get = source_block };
                    }
                case ValKeyword.SET: {
                        return new ValSetter { ctx = ctx, set = source_block };
                    }
                case ValKeyword.RETURN: {
                        return new ValReturn(getResult(), 1);
                    }
                case ValKeyword.CLASS: {
                        return StmtDefKey.MakeClass(ctx, source_block);
                    }
                case ValKeyword.INTERFACE: {
                        if(getResult() is ValDictScope s) {
                            return new ValInterface { _static = s, source = source_block };
                        }
                        throw new Exception("Object expected");
                    }
                case ValKeyword.ENUM: {
                        var locals = new Dictionary<string, object> { };
                        var rhs = getResult();
                        return new ValDictScope { locals = [], parent = ctx, temp = false };
					}
				default:
					return ExprInvoke.InvokeFunc(ctx, t, getArgs);

			}
			throw new Exception("Type expected");
            //return result;
        }
    }
    public class ExprEqual : INode {
        public INode lhs;
        public INode rhs;

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
    public class ExprMatch : INode {
        public INode lhs;
        public INode rhs;
        public string? key;
        public object Eval(IScope ctx) {
            var l = lhs.Eval(ctx);
            var r = rhs.Eval(ctx);
			ctx.SetLocal("_lhs", l);
			ctx.SetLocal("_rhs", r);
            if(Is(l, r)) {
                if(key != null) {
                    ctx.SetLocal(key, l);
                }
                return true;
            } else {
                return false;
            }
        }
        public static bool Is(object item, object kind) {
			switch(item, kind) {
				case (var v, Type t): {
						if(v is object o && t.IsAssignableFrom(o.GetType())) {
							return true;
						} else {
							return false;
						}
					}
				case (var v, ValClass vc): {
						if(v is ValDictScope vds && vds.HasClass(vc)) {
							return true;
						} else {
							return false;
						}
					}
                case (var v, Complement co):
                    return !Is(v, co.on);
                case (var v, ValMultiPattern vmp):
                    return vmp.Accept(v);
                    throw new Exception();
                case (var v, ValCriteria vcr):
                    return vcr.Accept(v);
				case (var v, ValInterface vi): {
						if(v is ValDictScope vds && vds.HasInterface(vi)) {
							return true;
						} else {
							return false;
						}
					}
				case (var v, null):
					return v == null;
				case (null, var t):
					return false;
				case (var v, var k):
					return Equals(v, k);
				default:
					throw new Exception();
			}
		}
    }
    public class ExprCriteria : INode {
        public INode item;
        public INode cond;
        public object Eval (IScope ctx) {
            return new ValCriteria { type = item.Eval(ctx), cond = cond };
        }
    }
    public class ValCriteria {
        public object type;
        public INode cond;

        public bool Accept(object o) {
            return ExprMatch.Is(o, type) && (bool)cond.Eval(ExprApply.MakeLocalScope(o));
        }
    }
    public class ExprBranch : INode {
        public INode condition;
        public INode positive;
        public INode negative;
        public string Source => $"{condition.Source} ?+ {positive.Source}{(negative != null ? $" ?- {negative.Source}" : $"")}";
        public object Eval (IScope ctx) {
            var cond = condition.Eval(ctx);
            switch(cond) {
                case true:
                    return  positive.Eval(ctx);
                default:
                    switch(negative) {
                        case null:  return ValEmpty.VALUE;
                        default:    return negative.Eval(ctx);
                    }
            }
        }
    }
    public class ExprLoop : INode {
        public INode condition;
        public INode positive;
        public object Eval (IScope ctx) {
            object r = ValEmpty.VALUE;
            //var r = new List<dynamic>();
            Step:
            var cond = condition.Eval(ctx);
            switch(cond) {
                case true:
                    r = positive.Eval(ctx);
                    goto Step;
                case false: return r;
                default:    throw new Exception("Boolean expected");
            }
        }
    }
	public class ExprAt : INode {
		public INode src;
		public List<INode> index;
		public object Eval (IScope ctx) {
			var call = src.Eval(ctx);
			switch(call) {
				case ValTuple vt: {
						return (((string key, object val))vt.items.ToArray().GetValue(index.Select(i => (int)i.Eval(ctx)).ToArray())).val;
					}
                default:
                    return ExprInvoke.InvokeFunc(ctx, call, () => new ValTuple { items = [(null, index.Select(i => i.Eval(ctx)).ToArray())] });
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
	public class ExprInvoke : INode {
        public INode expr;
        public ExprTuple args;
        //public string Source => $"{expr.Source}{(args.Count > 1 ? $"({string.Join(", ", args.Select(a => a.Source))})" : args.Count == 1 ? $"*{args.Single().Source}" : $"!")}";
        public object Eval (IScope ctx) {
            var f = expr.Eval(ctx);
            switch(f) {

                case ValKeyword.MAGIC:      return new ValMagic();
                case ValKeyword.GO:         return new ValGo { target = args.items[0].value as ExprSymbol };
                case ValKeyword.SET:        return new ValSetter { ctx= ctx, set = args };
                case ValKeyword.GET:        return new ValGetter { ctx = ctx, get = args };
                case ValKeyword.ANY:        return new ValMultiPattern { items = args.EvalTuple(ctx).vals, all = false };
                case ValKeyword.ALL:        return new ValMultiPattern { items = args.EvalTuple(ctx).vals, all = true };
                case ValKeyword.EXTEND:     return new ExtendTicket { on = args.EvalExpression(ctx), inherit = true };
                case ValKeyword.COMPLEMENT: return new Complement { on = args.EvalExpression(ctx) };
                case ValKeyword.UNALIAS:{
                        foreach(var(k,v) in args.items) {
                            if(v is ExprSymbol es) {
                                if(es.Get(ctx) is ValAlias va) {
                                    if(va.expr is ExprSymbol es_) {
                                        return es_.key;
                                    }
                                }
                            }
                        }
                        throw new Exception();
                    }
                case ValKeyword.REPLACE:
                    return InvokePars(ctx, (object lhs, object rhs) => (object item) => item == lhs ? rhs : item, args);
                case ValKeyword.FMT:
                    string Repl(string str) {
						foreach(Match m in Regex.Matches(str, "{(?<code>.*)}")) {
                            
                        }
                        return "";

					}
                    return args.EvalExpression(ctx) switch {
                        string str => Repl(str),
                        _ => throw new Exception()
                    };
				case ValKeyword.RETURN: return new ValReturn(args.EvalExpression(ctx), 1);
                case ValKeyword.YIELD:  return new ValYield(args.EvalExpression(ctx));
                case ValKeyword.IMPLEMENT: {
                        var vds = (ValDictScope)ctx;
                        var arg = args.EvalTuple(ctx);
                        foreach(var (k, v) in arg.items) {
                            switch(v) {
                                case ValInterface vi:
									vi.Register(vds);
                                    break;
                                default:throw new Exception("Interface expected");
							}
                        }
                        return ValEmpty.VALUE;
                    }
                case ValKeyword.INHERIT: {
                        throw new Exception();
                        foreach(var(k, v) in args.EvalTuple(ctx).items) {
                            switch(v) {
                                case ValTuple vt:
                                    vt.Inherit(ctx);
                                    break;
							}
                        }
                        return ValEmpty.VALUE;
                    }
                case ValKeyword.FN:
                default:    return InvokePars(ctx, f, args);
            }
        }
        public static object GetReturnType (object f) {
            throw new Exception("Implement");
        }
        public static object InvokePars (IScope ctx, object lhs, ExprTuple pars) =>
         InvokeFunc(ctx, lhs, () => pars.EvalTuple(ctx));
        public static object InvokeArgs (IScope ctx, object lhs, ValTuple args) =>
         InvokeFunc(ctx, lhs, () => args);
        public static object InvokeFunc (IScope ctx, object lhs, Func<ValTuple> evalArgs) {
            switch(lhs) {
                case ValEmpty: {
                        throw new Exception("Function not found");
                    }
                case ValError ve: {
                        throw new Exception(ve.msg);
                    }
                case ValConstructor vc: {
                        var v = evalArgs().items.Select(pair => pair.val).ToArray();
                        var c = vc.t.GetConstructor(v.Select(arg => (arg as object).GetType()).ToArray());
                        if(c == null) {
                            throw new Exception("Constructor not found");
                        }
                        return c.Invoke(v);
                    }
                case ValInstanceMethod vim: {
                        var v = evalArgs().items.Select(pair => pair.val).ToArray();
                        return vim.Call(v);
                    }
                case ValStaticMethod vsm: {
                        var v = evalArgs().items.Select(pair => pair.val).ToArray();
                        return vsm.Call(v);
                    }
                case Type tl: {
                        var v = evalArgs().items.Single().val;
                        return new ValType(tl).Cast(v, v?.GetType());
                    }
                case ValTuple vt: {
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
                        [{ } a, { } b, { } c, { } d, { } e, { } f, { } g, { } h] => (a, b, c, d, e, f, h)
                        });
                        var at = tt.MakeGenericType(argTypes);
                        return new ValType(tt.MakeGenericType(argTypes)).Cast(aa, at);
                    }
                case ValFunc vf: {
                        return vf.CallFunc(ctx, evalArgs);
                    }
                case Delegate de: {
                        var args = evalArgs();
						var v = args.items.Select(pair => pair.val).ToArray();
                        var r = de.DynamicInvoke(v);
                        if(de.Method.ReturnType == typeof(void))
                            return ValEmpty.VALUE;
                        return r;
                    }
                case ValClass vcl: {
                        break;
                        var args = evalArgs();
                        var scope = vcl.MakeInstance();
                        foreach(var (k, v) in args.items) {
                            if(k == null) {
                                throw new Exception();
                            }
                            StmtAssignSymbol.AssignLocal(scope, k, () => v);
                        }
                        return scope;
                    }
                case ValInterface vi: {
                        var args = evalArgs();
                        var v = args.items.Single().val;
                        switch(v) {
                            case ValDictScope vds:
                                if(vds.HasInterface(vi)) {
                                    return v;
                                } else {
                                    throw new Exception("Does not implement interface");
                                }
                            //TODO: Remove this
                            case null:
								return null;
                        }
                        throw new Exception();
                    }
                case ExtendObject ext:
                    
                    throw new Exception();
                case ValFuncScope vfs: {
						return vfs.func.CallFunc(ctx, evalArgs);
					}
                case ValDictScope s: {
                        if(s.locals.TryGetValue("_call", out var f) && f is ValFunc vf) {
                            return vf.CallFunc(ctx, evalArgs);
                        }
                        throw new Exception("Illegal");
                    }
                case ValObjectScope vos: {
                        return InvokeFunc(ctx, vos.o, evalArgs);
                    }
				case IDictionary d: {
						var ind = evalArgs().vals.Single();
						return d[ind];
					}
				case IEnumerable e: {
						var ind = evalArgs().vals.Single();
						return ind switch {
							int i => e.Cast<object>().ElementAt(i),
							_ => throw new Exception(),
						};
					}
				case Args a: {
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
								ValObjectScope vos => Get(vos.o),
								_ => throw new Exception(),
							};
						}
					}
			}
            throw new Exception($"Unknown function {lhs}");
        }
    }
    public class ExprApply : INode {
        public INode lhs;
        public INode rhs;
        public bool local = false;
        public static IScope MakeLocalScope(object item) {
            IScope ctx = null;
            var local = true;
			switch(item) {
				case IScope sc: {
						return sc.Copy(local ? null : ctx);
					}
				case Type t: {
                        return new ValTypeScope { t = t, parent = local ? null : ctx };
					}
				case object o: {
						return new ValObjectScope { o = o, parent = local ? null : ctx };
					}
			}
            throw new Exception();
		}

        public static IScope MakeScope(object o, IScope par) {
			switch(o) {
				case IScope sc: {
						return sc.Copy(par);
					}
				case Type t: {
						return new ValTypeScope { t = t, parent = par };
					}
				default: {
						return new ValObjectScope { o = o, parent = par };
					}
			}
		}
        public object Eval (IScope ctx) {
            var s = lhs.Eval(ctx);


            object Do(IScope dest) {
				switch(rhs) {
					case ExprBlock eb:  return eb.Apply(dest);
					default:            return rhs.Eval(dest);
				}
			}
            switch(s) {
                case IScope sc: {
                        var dest = sc.Copy(local ? null : ctx);
                        return Do(dest);     
                    }
                case Type t: {
                        var dest = new ValTypeScope { t = t, parent = local ? null : ctx };
						return Do(dest);
					}
                case object o: {
                        var dest = new ValObjectScope { o = o, parent = local ? null : ctx };
                        return Do(dest);
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
        public object Eval (IScope ctx) =>
         new ValReturn(val.Eval(ctx), up);
    }
    public class ExprBlock : INode {
        public List<INode> statements;
        public XElement ToXML () => new("Block", statements.Select(i => i.ToXML()));
        public string Source => $"{{{string.Join(", ", statements.Select(i => i.Source))}}}";
        public bool obj => _obj ??= statements.Any(s => s is StmtDefFunc or StmtDefKey or StmtDefTuple);
        public bool? _obj;
        public object Eval (IScope ctx) {
            if(statements.Count == 0)
                return ValEmpty.VALUE;
			var f = new ValDictScope(ctx, false);
			object r = ValEmpty.VALUE;
			foreach(var s in statements) {
				r = s.Eval(f);
				switch(r) {
					case ValReturn vr:  return vr.Up();
                    case ValYield vy:   throw new Exception();
				}
			}
            return obj ? f : r;
        }
        public object Apply (IScope f) {
            object r = ValEmpty.VALUE;
            foreach(var s in statements) {
                /*
                if(s is StmtDefFunc or StmtDefKey or StmtDefTuple) {
                    obj = true;
                }
                */
                r = s.Eval(f);
                switch(r) {
                    case ValReturn vr:  return vr.Up();
                }
            }
            return f;
        }
        public ValDictScope MakeScope (IScope ctx) => new ValDictScope(ctx, false);
        public object StagedEval (IScope ctx) => StagedApply(MakeScope(ctx));
        public object StagedApply (ValDictScope f) {
            var def = () => { };
            var seq = new List<INode> { };

            var labels = new Dictionary<string, int>();
            foreach(var s in statements) {
                switch(s) {
					/*
				case StmtDefFunc df: {
						df.Define(f);
						break;
					}
					*/
					case StmtDefFunc df when df.value is ExprVarBlock { type: ExprSymbol { key: "class" or "interface" } }: {
                            df.Define(f);
                            break;
                        }
                    case StmtDefKey { value: ExprVarBlock { type: ExprSymbol { key: "class", up: -1 }, source_block: { } block } } kv: {
                            var _static = StmtDefKey.DeclareClass(f, block, kv.key);
                            def += () => block.StagedApply(_static);
                            break;
                        }
                    case StmtDefKey { value: ExprVarBlock { type: ExprSymbol { key: "interface", up: -1 }, source_block: { } block } } kv: {
                            var _static = StmtDefKey.DeclareInterface(f, block, kv.key);
                            def += () => block.StagedApply(_static);
                            break;
                        }
                    case StmtDefKey { key:{ }key, value: ExprSymbol { key: "label" } }:
                        labels[key] = seq.Count;
                        seq.Add(s);
                        break;
                    default:
                        seq.Add(s);
                        break;
                }
            }
            def();



            var i = 0;
            while(i < seq.Count) {
				object r = ValEmpty.VALUE;
				var s = seq[i];
				switch(s) {
					case StmtDefKey { value: ExprVarBlock { type: ExprSymbol { key: "defer", up: -1 }, source_block: { } _block } }:
						r = _block.EvalDefer(f);
						break;
					default:
						r = s.Eval(f);
						break;
				}
				switch(r) {
					case ValReturn vr:
						return vr.Up();
					case ValGo vg:
						if(vg.target.up == 1) {
							if(labels.TryGetValue(vg.target.key, out var ind)) {
                                i = ind;
                                continue;
							}
							throw new Exception();
						}
						if(vg.target.up == -1) {
							if(labels.TryGetValue(vg.target.key, out var ind)) {
                                i = ind;
                                continue;
							}
							return vg;
						}
						return vg.Up();
				}
				i++;
            }
			return f;
		}
        public object EvalDefer (ValDictScope ctx) {
            return null;
        }
    }
    public record ExprSymbol : INode {
        public int up = -1;
        public string key;
        public XElement ToXML () => new("Symbol", new XAttribute("key", key), new XAttribute("level", $"{up}"));
        public string Source => $"{new string('^', up)}{key}";
        public object Eval (IScope ctx) => GetValue(ctx);
        public object Get (IScope ctx) => ctx.Get(key, up);
		public object GetValue(IScope ctx) {
			var r = ctx.Get(key, up);
			return r switch {
				ValGetter vg => vg.Get(),
				ValAlias va => va.Deref(),
				_ => r,
			};
		}
        public object Assign(IScope ctx, Func<object> getNext) {
            return StmtAssignSymbol.Assign(ctx, key, up, getNext);
        }
    }
    public class ExprSelf : INode {
        public int up;
        public object Eval (IScope ctx) {
            for(int i = 1; i < up; i++) ctx = ctx.parent;
            return ctx;
        }
    }
    public class ExprGet : INode {
        public INode src;
        public string key;
        public object Eval (IScope ctx) => Get(ctx);
        public object Get(IScope ctx) {
			var source = src.Eval(ctx);
			switch(source) {
				case ValDictScope s: {
						if(s.GetAt(key, 1) is { }v) {
							return v switch {
								ValGetter vg => vg.Get(),
                                ValAlias va => va.Deref(),
								_ => v,
							};
						}
						throw new Exception($"Variable not found {key}");
					}
				case ValClass vc: { return vc._static.locals.TryGetValue(key, out var v) ? v : throw new Exception("Variable not found"); }
				case Type t: { return new ValTypeScope { t = t }.GetLocal(key); }
				case Args a: { return a[key]; }
				case object o: { return new ValObjectScope { o = o }.GetLocal(key); }
			}
			throw new Exception("Object expected");
		}
        public object Set(IScope ctx, object val) {
            throw new Exception();
        }
    }
    public class ExprVal : INode {
        public object value;
        public XElement ToXML () => new("Value", new XAttribute("value", value));
        public string Source => $"{value}";
        public object Eval (IScope ctx) => value;
    }
    public class ExprCondSeq : INode {
        public INode type;
        public INode filter;
        public List<(INode cond, INode yes, INode no)> items;
        public object Eval (IScope ctx) {
            var f = filter.Eval(ctx);
            var lis = new List<object>();
            foreach(var (cond, yes, no) in items) {
                var c = cond.Eval(ctx);
                var b = ExprInvoke.InvokeArgs(ctx, f, c switch {
                    ValTuple vrt => vrt,
                    _ => ValTuple.Single(c)
				});
                switch(b) {
                    case true: {
                            if(yes != null) {
                                var v = yes.Eval(ctx);
                                switch(v) {
                                    case ValEmpty:              continue;
									case ValKeyword.CONTINUE:   continue;
									case ValKeyword.BREAK:      goto Done;
									case ValReturn vr:          return vr.Up();
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
                                    case ValEmpty:              continue;
									case ValKeyword.CONTINUE:   continue;
									case ValKeyword.BREAK:      goto Done;
									case ValReturn vr:          return vr.Up();
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
            return ExprMap.Convert(lis, type is { }t ? () => (Type)t.Eval(ctx) : null);
			var arr = lis.ToArray();
            return arr;
        }
    }
    public class ExprPatternMatch : INode {
        public INode item;
        public List<(INode cond, INode yes)> branches;
        public object Eval (IScope ctx) {
            var val = item.Eval(ctx);
            return Match(ctx, branches, val);
		}
        public static object Match(IScope ctx, List<(INode cond, INode yes)> branches, object val) {
			//To do: Add recursive call
			var inner_ctx = ctx.MakeTemp(val);
			inner_ctx.locals["_default"] = val;
			foreach(var (cond, yes) in branches) {
                //TODO: Allow lambda matches
				var b = cond.Eval(inner_ctx);
				if(Is(b)) {
					return yes.Eval(inner_ctx);
				}
			}
			throw new Exception("Fell out of match expression");
			bool Is (object pattern) {
				if(Equals(val, pattern))
					return true;
				else
					return false;
			}
		}
    }
    public class ExprMap : INode {
        public INode src;
        public INode map;
        public INode cond;
        public INode type;
        public bool expr = false;
        public XElement ToXML () => new("Map", src.ToXML(), map.ToXML());
        public string Source => $"{src.Source} | {map.Source}";
        public object Eval (IScope ctx) {
            var lhs = src.Eval(ctx);
            Func<dynamic, object> Map = expr ? MapExpr : MapFunc;
			switch(lhs) {
                case ValEmpty: throw new Exception("Variable not found");
                case ICollection c: return Map(c);
                case IEnumerable e: return Map(e);
				case ValDictScope vds: {
						if(vds._seq(out var seq) && seq is ValFunc vf) return Map(vf.CallPars(ctx, ExprTuple.Empty));
						throw new Exception();
					}
				case ValTuple vt: {
						//TO DO: rewrite
						var keys = vt.items.Select(i => i.key);
						var vals = vt.items.Select(i => i.val);
						var m = (IEnumerable<dynamic>)Map(vals);
						var r = new ValTuple { items = keys.Zip(m).ToArray() }; ;
						return r;
					}
				default:
                    throw new Exception("Sequence expected");
            }


			object MapFunc (dynamic seq) {
				Func<Type>? t = type is { } _t ? () => (Type)_t.Eval(ctx) : null;
				var f = map.Eval(ctx);
				object tr (IScope inner_ctx, object item) =>
					ExprInvoke.InvokeArgs(inner_ctx, f, item switch {
						ValTuple vt => vt,
						_ => ValTuple.Single(item)
					});
				return ExprMap.Map(seq, ctx, cond, t, (Transform)tr);
			}
			object MapExpr (dynamic seq) {
				object tr (IScope inner_ctx, object item) => map.Eval(MakeMapCtx(inner_ctx, item));
				Func<Type>? t = type is { } _t ? () => (Type)_t.Eval(ctx) : null;
				return ExprMap.Map(seq, ctx, cond, t, (Transform)tr);
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
                  ValDictScope vds => new ValDictScope { locals = vds.locals, parent = inner_ctx, temp = false },
                  Type t => new ValTypeScope { parent = inner_ctx, t = t },
                  object o => new ValObjectScope { parent = inner_ctx, o = o },
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
		public static object Map (dynamic seq, IScope ctx, INode cond, Func<Type> t, Transform tr) {
			var result = new List<object>();
			int index = 0;
			foreach(var item in seq) {
				var inner_ctx = ExprMap.MakeCondCtx(ctx, item, index);
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
					case ValEmpty: continue;
					case ValKeyword.CONTINUE: continue;
					case ValKeyword.BREAK: goto Done;
					case ValReturn vr: return vr.Up();
					default:
						result.Add(r);
						continue;
				}
			}
			Done:
			return Convert(result, t);
		}
	}
    public class StmtDefKey : INode {
        public string key;
        public INode value;
        public XElement ToXML () => new("KeyVal", new XAttribute("key", key), value.ToXML());
        public string Source => $"{key}:{value?.Source ?? "null"}";
        public object Eval (IScope ctx) {
            var val = value.Eval(ctx);
            switch(val) {
                case ValError ve: throw new Exception(ve.msg);
                default: return Init(ctx, key, val);
            }
        }
        public static object InitFrom (IScope ctx, string key, IScope from) => Init(ctx, key, from.Get(key));
        public static object Init (IScope ctx, string key, object val) {
            var curr = ctx.GetLocal(key);
            if(curr is not ValError) {
                throw new Exception();
            }
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
        public static void Set (IScope ctx, string key, object val) {
            ctx.SetLocal(key, val);
        }
        public static ValClass MakeClass (IScope f, ExprBlock block) {
            var _static = block.MakeScope(f);
            block.StagedApply(_static);
            var c = new ValClass { name = "unknown", source_ctx = f, source_expr = block, _static = _static };
            _static.locals["_kind"] = ValKeyword.CLASS;
            _static.AddClass(c);
            return c;
        }
        public static ValDictScope DeclareClass (IScope f, ExprBlock block, string key) {
            var _static = block.MakeScope(f);
            var c = new ValClass {
                name = key,
                source_ctx = f,
                source_expr = block,
                _static = _static
            };
            f.SetLocal(key, c);
			_static.locals["_kind"] = ValKeyword.CLASS;
			_static.AddClass(c);
            return _static;
        }
        public static ValDictScope DeclareInterface (IScope f, ExprBlock block, string key) {
            var _static = block.MakeScope(f);
            var vi = new ValInterface {_static = _static};
            f.SetLocal(key, vi);
			_static.locals["_kind"] = ValKeyword.INTERFACE;
			_static.AddInterface(vi);
            return _static;
        }
    }
    public class ExprFunc : INode {
        public ExprTuple pars;
        public INode result;
        //public XElement ToXML () => new("ExprFunc", [.. pars.Select(i => i.ToXML()), result.ToXML()]);
        //public string Source => $"@({string.Join(", ", pars.Select(p => p.Source))}) {result.Source}";
        public object Eval (IScope ctx) =>
         new ValFunc {
             expr = result,
             pars = pars.EvalTuple(ctx),
             parent_ctx = ctx
         };
    }
    public class ExprSeq : INode {
        public INode type;
        public List<INode> items;
        public object Eval (IScope ctx) {
            List<object> l = [];
            foreach(var it in items) {
                var r = it.Eval(ctx);
                switch(r) {
                    case ValSpread spr:
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
                default: return res;
            }
        }
    }
    public class ExprTuple : INode {
        public (string key, INode value)[] items;
        public static ExprTuple Empty => new ExprTuple { items = [] };
		public static ExprTuple SingleExpr (INode v) => new ExprTuple { items = [(null, v)] };
		public static ExprTuple SingleVal (object v) => SingleExpr(new ExprVal { value = v });
		public static ExprTuple SpreadExpr (INode n) => SingleExpr(new ExprSpread { value = n });
		public static ExprTuple SpreadVal (object v) => SpreadExpr(new ExprVal { value = v });
        public static ExprTuple ListExpr (IEnumerable<INode> items) => new ExprTuple { items = items.Select(i => ((string)null, i)).ToArray() };


		public ValTuple EvalTuple (IScope ctx) {
            var it = new List<(string key, object val)> { };
            /*
            var t = new ValTuple { items = it };
			var s = ctx.MakeTemp();
            s.locals["_tuple"] = t;
            */
            var s = ctx;
			foreach(var (key, val) in items) {
                //TODO: inc('y) != inc*'y
                if(val is ExprAlias ea) {
                    it.Add((key, ea.Eval(ctx)));
                    continue;
                }
                Handle(val.Eval(s));
                void Handle(object v) {
					switch(v) {
						case ValSpread vs:
							vs.SpreadTuple(key, it);
							break;
						case ValEmpty:
							break;
						case ValAlias va:
							Handle(va.Deref());
							//throw new Exception();
							//it.Add((key, va));
							break;
                        case ValGetter vg:
                            throw new Exception();
						default:
							it.Add((key, v));
							break;
					}
				}
            }
            return new ValTuple { items = it.ToArray() };
            //return t;
        }
        public object Eval (IScope ctx) => EvalExpression(ctx);
        public object EvalExpression (IScope ctx) {
            var a = EvalTuple(ctx);
            switch(a.Length) {
                case 0: return ValEmpty.VALUE;
                case 1:
                    return a.items.Single().val;
                default:
                    return a;
            }
        }
        public void Spread (IScope ctx, List<(string key, object val)> it) {
            foreach(var (key, val) in items) {
                it.Add((key, val.Eval(ctx)));
            }
        }
        public ExprTuple ParTuple () =>
         new ExprTuple {
             items = items.Select(pair => {
                 if(pair.key == null) {
                     switch(pair.value) {
                         case ExprSymbol { up: -1, key: { } key }:
                             return (key, new ExprVal { value = typeof(object) });
                         case ExprSymbol { up: not -1, key: { } key }:
                             return (key, pair.value);
                     }
                     throw new Exception("Expected");
                 } else {
                     return pair;
                 }
             }).ToArray()
         };
    }
    public class ValTuple : INode {
        public int Length => items.Length;
        public (string key, object val)[] items;
        public object[] vals => items.Select(i => i.val).ToArray();
        public object Eval (IScope ctx) => this;

        public static ValTuple Single (object v) => new ValTuple { items = [(null, v)] };
        public void Spread (List<(string key, object val)> it) {
            foreach(var (key, val) in items) {
                it.Add((key, val));
            }
        }
        public ExprTuple expr => new ExprTuple {
            items = items.Select(pair => (pair.key, (INode)new ExprVal { value = pair.val })).ToArray()
        };
        public void Inherit(IScope ctx) {
            foreach(var(k, v) in items) {
                if(k == null) {
                    throw new Exception();
                }
                ctx.SetLocal(k, v);

            }
        }
    }
    public class StmtDefFunc : INode {
        public string key;
        public ExprTuple pars;
        public INode value;
        //public XElement ToXML () => new("DefineFunc", [new XAttribute("key", key), ..pars.Select(i => i.ToXML()), value.ToXML()]);
        //public string Source => $"{key}({string.Join(", ",pars.Select(p => p.Source))}): {value.Source}";
        public object Eval (IScope ctx) {
            Define(ctx);
            return ValEmpty.VALUE;
        }
        public void Define (IScope owner) {
            owner.SetLocal(key, new ValFunc {
                expr = value,
                pars = pars.EvalTuple(owner),
                parent_ctx = owner
            });
        }
        public ValFunc DeclareHead (IScope owner) {
            var vf = new ValFunc {
                expr = value,
                pars = new ValTuple { items = [] },
                parent_ctx = owner
            };
            owner.SetLocal(key, vf);
            return vf;
        }
        public void DefineHead (ValFunc vf) {
            vf.pars = pars.EvalTuple(vf.parent_ctx);
        }
    }
    public class StmtDefTuple : INode {
        public string[] lhs;
        public INode rhs;
        public bool structural = false;
        public object Eval (IScope ctx) {
            var val = rhs.Eval(ctx);
            switch(val) {
                case ValTuple vt:
                    if(structural) {
						foreach(var sym in lhs) {
							StmtDefKey.Init(ctx, sym, vt.items.Where(pair => pair.key == sym).Single());
						}
						return ValEmpty.VALUE;
					} else if(lhs.Length == vt.items.Length) {
                        foreach(var i in Enumerable.Range(0, lhs.Length)) {
                            StmtDefKey.Init(ctx, lhs[i], vt.items[i].val);
                        }
                        return ValEmpty.VALUE;
                    } else {
                        throw new Exception("illegal");
                    }
                case IScope sc:
                    if(structural) {
                        foreach(var sym in lhs) {
                            StmtDefKey.InitFrom(ctx, sym, sc);
                        }
                        return ValEmpty.VALUE;
                    } else {
						throw new Exception("unknown");
					}
            }
            throw new Exception();
        }
    }
    public class StmtAssignTuple : INode {
        public ExprSymbol[] symbols;
        public INode value;
        public object Eval (IScope ctx) {
            return Assign(ctx, symbols, value.Eval(ctx));
        }
        public static object Assign (IScope ctx, ExprSymbol[] symbols, object val) {
			switch(val) {
				case ValTuple vt:
					if(symbols.Length == vt.items.Length) {
						foreach(var i in Enumerable.Range(0, symbols.Length)) {
							StmtAssignSymbol.AssignSymbol(ctx, symbols[i], () => vt.items[i].val);
						}
						return ValEmpty.VALUE;
					} else {
						throw new Exception();
					}
				case Args a:
					if(a.Length == symbols.Length) {
						foreach(var i in Enumerable.Range(0, symbols.Length)) {
							StmtAssignSymbol.AssignSymbol(ctx, symbols[i], () => a[i]);
						}
						return ValEmpty.VALUE;
					} else {
						throw new Exception();
					}
				case Array a:
					if(a.Length == symbols.Length) {
						foreach(var i in Enumerable.Range(0, symbols.Length)) {
							var v = a.GetValue(i);
							StmtAssignSymbol.AssignSymbol(ctx, symbols[i], () => v);
						}
						return ValEmpty.VALUE;

					} else {
						throw new Exception();
					}
			}
            throw new Exception();
		}
    }
    public class StmtAssignSymbol : INode {
        public ExprSymbol symbol;
        public INode value;
        XElement ToXML () => new("Reassign", symbol.ToXML(), value.ToXML());
        public string Source => $"{symbol.Source} := {value.Source}";
        public object Eval (IScope ctx) {
            var curr = symbol.Eval(ctx);
            var inner_ctx = ctx.MakeTemp(curr);
            inner_ctx.locals["_curr"] = curr;
            switch(curr) {
                case ValDeclared vd:
					inner_ctx.locals["_type"] = vd.type;
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
        public static object AssignSymbol (IScope ctx, ExprSymbol sym, Func<object> getNext) => Assign(ctx, sym.key, sym.up, getNext);
        public static object Assign (IScope ctx, string key, int up, Func<object> getNext) {
            var curr = ctx.Get(key, up);
            switch(curr) {
				case ValError ve: throw new Exception(ve.msg);
				case ValSetter vs:      return vs.Set(getNext());
                case ValAlias va:       return va.Set(getNext);
                case ValDeclared vd:    return Assign(vd.type);
                case ValClass vc:       return AssignClass(vc);
                case ValDictScope vds:  return ctx.Set(key, getNext(), up);
                case ValKeyword.AUTO:   return ctx.Set(key, getNext(), up);
                default:                return AssignType(curr?.GetType());
            }
            object Assign (object type) {
                switch(type) {
                    case Type t:        return AssignType(t);
                    case ValClass vc:   return AssignClass(vc);
                }
                throw new Exception();
            }
            object AssignClass (ValClass cl) {
                var next = getNext();
                switch(next) {
                    case ValDictScope vds:  return ctx.Set(key, vds, up);
                    case ValError ve:       throw new Exception(ve.msg);
                    default:                return ctx.Set(key, next, up);
                }
            }
            object AssignType (Type prevType) {
                var next = getNext();
                if(prevType == null) {
                    goto Do;
                }
                switch(curr) {
                    case ValInterface vi:
                        if(next is ValDictScope vds) {
                            if(vds.HasInterface(vi)) {
                                goto Do;
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
                    case ValError ve: throw new Exception(ve.msg);
                }
                Do:
                return ctx.Set(key, next, up);
            }
        }
        public static bool CanAssign(object prev, object next) {
			switch(prev) {
				case ValError ve:
					throw new Exception(ve.msg);
				case ValDeclared vd:
					return Match(vd.type);
				case ValClass vc:
					return MatchClass(vc);
				case ValDictScope vds:
					return true;
				default:
					return MatchType(prev?.GetType());
			}
			bool Match (object prevType) {
				switch(prevType) {
					case Type t:
						return MatchType(t);
					case ValClass vc:
						return MatchClass(vc);
				}
				throw new Exception();
			}
			bool MatchClass (ValClass prevClass) {
				switch(next) {
					case ValDictScope vds:
						return true;
					case ValError ve:
						throw new Exception(ve.msg);
					default:
						return true;
				}
			}
			bool MatchType (Type prevType) {
				if(prevType == null) {
					goto Good;
				}
				switch(prev) {
					case ValInterface vi:
						if(next is ValDictScope vds) {
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
				if(next is ValError ve) {
					throw new Exception(ve.msg);
				}
				Good:
				return true;
			}
		}
    }
    public record Var {
		public bool pub;
		public bool mut;
        public bool init;
        public object type;
        public object val;
        public void Init(ValCast vc) {
            this.type = vc.type;
            this.val = vc.val;
        }
        public void Assign(ValCast vc) {
            this.val = vc.val;
        }
    }
    public record ValCast {
        public object type;
        public object val;
    }
    public interface INode {
        XElement ToXML () => new(GetType().Name);
        string Source => "";
        object Eval (IScope ctx);
    }
    public class Tokenizer {
        string src;
        int index;
        public Tokenizer (string src) {
            this.src = src;
        }
        public List<Token> GetAllTokens () {
            var tokens = new List<Token> { };
            while(Next() is { type: not TokenType.EOF } t) {
                tokens.Add(t);
            }
            return tokens;
        }

        public Token Next () {
            if(index >= src.Length) {
                return new Token { type = TokenType.EOF };
            }
            var str = (params char[] c) => string.Join("", c);
            void inc () => index += 1;
            Check:
            var c = src[index];
            switch(c) {
                case '/': {

                        if(src[index + 1] == '/') {
                            inc();
                            inc();

							while(index < src.Length && src[index] != '\n') {
								inc();
							}
							goto Check;
                        } else if(src[index + 1] == '*') {
                            inc();
                            inc();

							bool checkStop = false;
							while(index < src.Length) {
								if(src[index] == '*') {
									inc();
									checkStop = true;
								} else if(src[index] == '/' && checkStop) {
									inc();
									goto Check;
								} else {
									inc();
									checkStop = false;
								}
							}
							goto Check;
                        }
                        break;
                    }
                case ('#'): {
                        inc();
                        /*
                        if(src[index] == '<') {
                            inc();

                            bool checkStop = false;
                            while(index < src.Length) {
								if(src[index] == '>' && checkStop) {
									inc();
									goto Check;
								} else if(src[index] == '#') {
                                    inc();
                                    checkStop = true;
                                } else {
                                    inc();
									checkStop = false;
								}
                            }
                            goto Check;
                        }
                        */
                        while(index < src.Length && src[index] != '\n') {
                            inc();
                        }
                        goto Check;
                    }
                case ('~'): {
                        inc();
                        while(index < src.Length && src[index] != '~') {
                            inc();
                        }
                        inc();
                        goto Check;
                    }
                case ('"'): {
                        int dest = index + 1;
                        var v = "";
                        while(dest < src.Length) {
                            if(src[dest] == '\\') {
                                dest += 1;
								v += src[dest] switch {
                                    'r' => '\r',
                                    'n' => '\n',
                                    't' => '\t',
                                    '\\' => '\\',
                                    '"' => '"'
                                };
                                dest++;
							} else if(src[dest] == '"') {
                                break;
                            } else {
                                v += src[dest];
                                dest += 1;
                            }
                        }
                        dest += 1;
                        index = dest;
                        return new Token { type = TokenType.STRING, str = v };
                    }
                    /*
                case '\t':
                    throw new Exception("Illegal token");
                    */
                case (' ' or '\r' or '\n' or '\t'): {
                        inc();
                        goto Check;
                    }
                case (>= '0' and <= '9'): {
                        int dest;
                        for(dest = index + 1; dest < src.Length && src[dest] is >= '0' and <= '9'; dest++) {
                        }
                        var v = src[index..dest];
                        index = dest;
                        return new Token { type = TokenType.INTEGER, str = v };
                    }
                case ((>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9')): {
                        int dest = index;
                        bool escape = false;

                        string v = "";
                        Read:
                        if(dest < src.Length) {
                            var ch = src[dest];
							switch(ch) {
                                case (>= 'a' and <= 'z') or (>= 'A' and <= 'Z') or '_' or (>= '0' and <= '9'):
                                    v += ch;
                                    dest += 1;
                                    goto Read;
                                case '\\':
                                    dest += 1;
                                    escape = true;
                                    goto Read;
                                default:
                                    if(escape) {
										v += ch;
										escape = false;
                                        dest += 1;
                                        goto Read;
                                    } else {
                                        goto Done;
                                    }
							}
                        }
                        Done:
                        index = dest;
                        return new Token { type = TokenType.NAME, str = v };
                    }
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
                '\'' => TokenType.QUOTE,

                ' ' => TokenType.SPACE,
                _ => default(TokenType?)
            } is { } tt) {
                index += 1;
                return new Token { type = tt, str = str(c) };
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
        QUOTE,
        SPACE,
        SWIRL, QUERY, SHOUT, SPARK, PIPE, AMPERSAND, CASH, PERCENT, HASH,

        EOF
    }
    public class Token {
        public TokenType type;
        public string str;

        public string ToString () => $"[{type}] {str}";
    }
}