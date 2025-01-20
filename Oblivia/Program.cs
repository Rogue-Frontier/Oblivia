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
using System.Reflection.Emit;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using static Oblivia.ExMap;
using static System.Formats.Asn1.AsnWriter;
using static System.Runtime.InteropServices.JavaScript.JSType;
//TODO: Remove '@' Indexer
namespace Oblivia {
	public class Std {
        public static VDictScope std;
		static Std () {
			T _<T> (T t) => t;
			Type MakeGeneric (Type gen, params object[] item) => gen.MakeGenericType(item.Select(i => i switch { Type t => t, _ => typeof(object) }).ToArray());
            std = new VDictScope {
                locals = new() {
                    ["File"] = typeof(File),
                    ["Console"] = typeof(Console),

                    ["char"] = typeof(char),

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

                    ["empty"] = VEmpty.VALUE,
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

                    ["Array"] = _((Type type, int dim) =>

                    type.MakeArrayType(dim)),
                    ["arr_get"] = _((Array a, int[] ind) => a.GetValue(ind)),
                    ["arr_set"] = _((Array a, int[] ind, object value) => a.SetValue(value, ind)),
                    ["arr_at"] = _((Array a, int[] ind) => new ValRef { src = a, index = ind }),
                    ["arr_mk"] = _((Type t, int l) => Array.CreateInstance(t, l)),

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
                    ["Row"] = _((object type) => (type is Type t ? t : typeof(object)).MakeArrayType(1)),
                    ["Grid"] = _((Type type) => type.MakeArrayType(2)),
                    ["List"] = _((object item) => MakeGeneric(typeof(List<>), item)),
                    ["HashSet"] = _((object item) => MakeGeneric(typeof(HashSet<>), item)),
                    ["Dict"] = _((Type key, Type val) => typeof(Dictionary<,>).MakeGenericType(key, val)),
                    ["ConcDict"] = _((Type key, Type val) => typeof(ConcurrentDictionary<,>).MakeGenericType(key, val)),
                    ["StrBuild"] = typeof(StringBuilder),
                    ["Fn"] = typeof(VFn),
                    ["PQ"] = _((object a, object b) => MakeGeneric(typeof(PriorityQueue<,>), a, b)),
                    ["default"] = ValKeyword.DEFAULT,
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

                    ["pub"] = ValKeyword.PUB,
                    ["priv"] = ValKeyword.PRIV,
                    ["static"] = ValKeyword.STATIC,

                    ["fmt"] = ValKeyword.FMT,
                    ["leaf"] = ValKeyword.LEAF,
                    ["xml"] = ValKeyword.XML,
                    ["json"] = ValKeyword.JSON,
                    ["math"] = ValKeyword.MATH,


                    ["ɩ"] = _((int i) => Enumerable.Range(0, i))
                }
            };
		}
	}
    public class ValMagic();
	public class VError {
        public string msg;
        public VError () { }
        public VError (string msg) {
            this.msg = msg;
        }
    }
    public record VIndex {
        public Action<object> Set;
        public Func<object> Get;
    }
    public record ValRef {
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
    public record VYield(object data);
    public enum ValKeyword {
        CLASS,INTERFACE,ENUM,
        GET,SET,PROP,

        FALL,

        DEFAULT,
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
        FN,
        MAKE,
        PUB, PRIV, STATIC, MACRO, TEMPLATE,

        FIELDS_OF,METHODS_OF,MEMBERS_OF,
        ATTR,
        MARK,

        PREV_ITER,SEEK_ITER, WAIT_ITER,

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

        EMBED,

        //Create a label
        LABEL,
        //Go to label
        GO,
        //Go to the top of the current scope
        REDO,

        NOP,
        //Creates a magic constant e.g. null
        MAGIC,

        TYPEOF,

        //Provides multiple values for tuple
        DECONSTRUCT,
        //Gets all keys from the target for deconstruct-assign
        STRUCT_KEYS,

		XML,
        JSON,
		REGEX,
		MATH,
        FMT,
        LEAF
    }
    public class VInterType {
        public object[] items;
        public bool all = false;
        public bool Accept (object val) {
            if(all) {
				return items.All(i => ExIs.Is(val, i));
			} else {
                var r = items.Any(i => ExIs.Is(val, i));
				return r;
			}
        }
    }
    public class VEmpty {
        public static readonly VEmpty VALUE = new();
    }
    public class VDeclared {
        public string name;
        public object type;
    }
    public class Variable {
        public string name;
        public bool hide;
        public IType type;
        public object val;
        public bool ready;
    }
    public class ValAuto { }
    public class FnTicket {
        public int argCount;
		public object Init (IScope ctx, INode src) {
			return new VFn { expr = src, parent_ctx = ctx, pars = new VTuple { } };
		}
	}
    public class ExtendTicket {
        public object on;
		public bool inherit = true;
	}
	public class ExtendObject {
        public ExtendTicket call;
        public ExBlock src;
        public object Init (IScope ctx) => Init(ctx, call.on);
        public object Init(IScope ctx, object target) {
            //Add option for inherit
            var scope = new VDictScope(ExMemberBlock.MakeScope(target, ctx), false) { inherit = call.inherit };
            scope.locals["base"] = target;
            src.StagedApply(scope);
            return scope;
        }
    }
    //Any methods called will return the complement of the result
    public class VComplement {
        public object on;
    }
    public record VFnPattern {
        public VFn filter;

    }
    public record ValPattern { }
	public record VGo {
		public ExUpKey target;

		public object Up () => this with { target = target with { up = target.up - 1 } };
	}
    public record ExGet : INode {
        public INode get;
        public object Eval (IScope ctx) => new VGet { ctx = ctx, get = get };
    }
    public record VSet {
        public INode set;
        public IScope ctx;
        public object Set (object val) => Set(set, ctx, val);
        public static object Set(INode expr, IScope ctx, object val) {
			var inner_ctx = ctx.MakeTemp();
			inner_ctx.locals["_val"] = val;
			return expr.Eval(inner_ctx);
		}
    }
    public record VGet {
        public INode get;
        public IScope ctx;
		public object Get () => Get(get, ctx);
		public static object Get (INode expr, IScope ctx) => expr.Eval(ctx);
	}
    public record ValProp {
        public INode get, set;
        public IScope ctx;
        public object Get () => VGet.Get(get, ctx);
        public object Set (object val) => VSet.Set(set, ctx, val);
    }
    public record VAlias {
        public INode expr;
        public IScope ctx;
        public object Deref() => expr.Eval(ctx);
        public object Set(Func<object> getNext) {
            switch(expr) {
                case ExUpKey es:
					return es.Assign(ctx, getNext);
				case ExTuple et:
                    return StAssignMulti.AssignTuple(ctx, et.items.Select(i => (ExUpKey)(i.value)).ToArray(), getNext());
            }
            throw new Exception();
        }
	}
    public record ExPointer:INode {
        public INode expr;
        public object Eval (IScope ctx) {
            var val = expr.Eval(ctx);
            return new ValPointer { value = val };
		}
    }
    public record ValPointer {

        public object type;
        public object value;
    }
    public record ExAlias:INode {
        public INode expr;
        public object Eval (IScope ctx) => new VAlias { ctx = ctx, expr = expr };
    }
	public record ValLazy {
		public INode expr;
		public IScope ctx;
        public bool done = false;
        public object value;
		public object Eval () => expr.Eval(ctx);
	}
	public record Args {
        public object this[string s] {
            get => dict[s];
            set => dict[s] = value; }
        public object this[int s] {
            get => list[s];
            set => list[s] = value;
		}
        public int Length => list.Count;
        public Dictionary<string, object> dict = new();
        public List<object> list = new();
    }
    public record VFn {
        public INode expr;
        public VTuple pars;
        public IScope parent_ctx;
        public VFn Memo(VFn vf) {
            throw new Exception();
        }
        private void InitPars(IScope ctx) {
			foreach(var (k, v) in pars.items) {
				StDefKey.Define(ctx, k, v);
			}
		}
        public object CallPars (IScope caller_ctx, ExTuple pars) {
            return CallFunc(() => pars.EvalTuple(caller_ctx));
        }
        public object CallArgs (VTuple args) {
            return CallFunc(() => args);
        }
        public object CallFunc (Func<VTuple> evalArgs) {
            var func_ctx = new VFnScope(this, parent_ctx, false);
            var argData = new Args { };
            func_ctx.locals["_arg"] = argData;
            func_ctx.locals["_func"] = this;
            InitPars(func_ctx);
            int ind = 0;
            foreach(var (k, v) in evalArgs().items) {
                if(k != null) {
                    ind = -1;
                    var val = StAssignSymbol.AssignLocal(func_ctx, k, () => v);
                } else {
                    if(ind == -1) {
                        throw new Exception("Cannot have positional arguments after named arguments");
                    }
                    var p = pars.items[ind];
                    var val = StAssignSymbol.AssignLocal(func_ctx, p.key, () => v);
                    ind += 1;
                }
            }
            ReadPars(func_ctx, argData);
            var result = expr.Eval(func_ctx);
            return result;
        }
        public object CallVoid (IScope ctx) => CallData([]);
        public object CallData (IEnumerable<object> args) => CallFunc(() => new VTuple {
            items = args.Select(a => ((string) null, (object) a)).ToArray()
        });
		private void ReadPars (IScope func_ctx, Args argData) {
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
        public INode source;
        public VDictScope _static;
        public void Register (VDictScope target) {
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
    public class VClass {
        public string name;
        public VDictScope _static;
        public INode source_expr;
        public IScope source_ctx;
        public VDictScope MakeInstance () => MakeInstance(source_ctx);
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
					case "_classSet" or "_interfaceSet" or "_kind":
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
    public class ValTupleScope : IScope {
        public IScope parent { get; set; } = null;
        public VTuple t;
        public IScope Copy (IScope parent) => new ValTupleScope { parent = parent, t = t };
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
    public record VDictScope : IScope {
        public bool temp = false;

        public bool inherit = false;
        public IScope parent { get; set; } = null;
        public ConcurrentDictionary<string, dynamic> locals = new() {};
        public HashSet<VClass> ClassSet => locals.GetOrAdd("_classSet", new HashSet<VClass>() );
		public HashSet<VInterface> InterfaceSet => locals.GetOrAdd("_interfaceSet", new HashSet<VInterface>());
		public void SetClass (VClass vc) {
            locals["_class"] = vc;
            locals["_proto"] = this;
            ClassSet.Add(vc);
        }




        public bool HasClass (VClass vc) => ClassSet.Contains(vc);
		public void AddInterface (VInterface vi) => InterfaceSet.Add(vi);
        public bool HasInterface (VInterface vi) => InterfaceSet.Contains(vi);
        public VFn? _at => locals.TryGetValue("_at", out var f) ? f : null;
        public bool _seq(out object seq) {
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
                if(k is "_classSet" or "_interfaceSet") {
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

        public bool GetLocal(string key, out object v) =>
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
    public class Parser {
        int index;
        List<Token> tokens;
        public Parser (List<Token> tokens) {
            this.tokens = tokens;
        }
        public static ExBlock FromFile (string path) {
            var tokenizer = new Tokenizer(File.ReadAllText(path));
            return new Parser(tokenizer.GetAllTokens()).NextBlock();
        }
        void inc () => index++;
        void dec () => index--;
        public Token currToken => tokens[index];
        public TokenType tokenType => currToken.type;
        public INode NextStatement () {
            switch(tokenType) {
				case TokenType.AT:
					inc();
					var att = NextTerm();
					return new ExInvokeBlock { type = att, source_block = new ExBlock { statements = [NextStatement()] } };
			}
            var lhs = NextExpr();
            switch(tokenType) {
                case TokenType.COLON:
                    inc();
                    switch(lhs) {
                        case ExVal ev:
                        case ExAlias ea:
                            //key by value
                            throw new Exception("");
                        //Local structure define / assign
                        case ExBlock eb:
                            switch(tokenType) {
                                case TokenType.EQUAL: {
                                        inc();
                                        List<ExUpKey> symbols = [];
                                        foreach(var item in eb.statements) {
                                            if(item is ExUpKey s) {
                                                symbols.Add(s);
                                            } else {
                                                throw new Exception();
                                            }
                                        }
                                        return new StAssignMulti { symbols = symbols.ToArray(), value = NextExpr(), deconstruct = true };
                                    }
                                default: {
                                        inc();
                                        List<string> symbols = [];
                                        foreach(var item in eb.statements) {
                                            if(item is ExUpKey s) {
                                                symbols.Add(s.key);
                                            } else {
                                                throw new Exception();
                                            }
                                        }
                                        return new StDefMulti { lhs = symbols.ToArray(), rhs = NextExpr(), deconstruct = true };
                                    }
                            }
                        //Local tuple define/assign
                        case ExTuple et:
                            switch(tokenType) {
                                case TokenType.EQUAL: {
                                        inc();
                                        List<ExUpKey> symbols = [];
                                        foreach(var item in et.vals) {
                                            if(item is ExUpKey s) {
                                                symbols.Add(s);
                                            } else {
                                                throw new Exception();
                                            }
                                        }
                                        return new StAssignMulti { symbols = symbols.ToArray(), value = NextExpr() };
                                    }
                                default: {
                                        List<string> symbols = [];
                                        foreach(var item in et.vals) {
                                            if(item is ExUpKey { key: { } key, up: -1 or 1 }) {
                                                symbols.Add(key);
                                            } else {
                                                throw new Exception();
                                            }
                                        }
                                        return new StDefMulti { lhs = symbols.ToArray(), rhs = NextExpr() };
                                    }
                            }
                        //Local key define/assign
                        case ExUpKey { up: { } up } es:
                            switch(tokenType) {
                                case TokenType.EQUAL:
                                    inc();
                                    return new StAssignSymbol { symbol = es, value = NextExpr() };
                                default:
                                    switch(up) {
                                        case -1 or 1:
                                            return new StDefKey {
                                                key = es.key,
                                                value = NextExpr()
                                            };
                                        default:
                                            throw new Exception("Can only define in current scope");
                                    }
                            }
						case ExMemberExpr emb:
							throw new Exception();
						case ExMemberBlock emb:
							throw new Exception();
                        case ExMap { expr: true, map: ExUpKey euk } em:
							throw new Exception();
						//Return
						case ExSelf { up: { } up }:
							return new StReturn { val = NextExpr(), up = up };
						case ExInvoke { expr: ExUpKey { up: { } up, key: { } key }, args: { } args }:
                            switch(up) {
                                case -1 or 1:
                                    return new StDefFn {
                                        key = key,
                                        pars = args.ParTuple(),
                                        value = NextExpr()
                                    };
                                default:
                                    throw new Exception("Cannot define non-local function");
                            }
                        //Self call tuple
                        case ExInvoke { expr: ExSelf { up: 1 or -1 }, args: ExTuple et }:
                            return new StDefFn {
                                key = "_call",
                                pars = et,
                                value = NextExpr()
                            };
                        default:
                            switch(tokenType) {
                                case TokenType.EQUAL: {
                                        inc();
                                        return new StAssignExpr { lhs = lhs, rhs = NextExpr() };
                                    }
                            }
                            throw new Exception("Cannot define this");
                    }
                default:
                    return CompoundExpr(lhs);
            }
        }
        public INode NextPattern () => NextExpr();
        public INode NextExpr () {
            var lhs = NextTerm();
            return CompoundExpr(lhs);
        }
		public INode CompoundExpr (INode lhs) {
            Start:
            switch(tokenType) {
                case TokenType.SPACE:
                    inc();
                    goto Start;
                case TokenType.MINUS:
                    inc();
                    switch(tokenType) {
						//fn type
						case TokenType.ANGLE_R:
                            inc();
                            switch(tokenType) {
                                case TokenType.TUPLE_R:
                                case TokenType.BLOCK_R:
                                case TokenType.ARRAY_R:
                                case TokenType.ANGLE_R:
                                    return new ExFnType { lhs = lhs };
                                default: {
										var rhs = NextExpr();
										return CompoundExpr(new ExFnType { lhs = lhs, rhs = rhs });
									}
							}
                        default: {
								var rhs = NextExpr();
								return CompoundExpr(new ExRange { lhs = lhs, rhs = rhs });
							}
                    }
                    break;
                case TokenType.BLOCK_L:
                    return CompoundExpr(new ExInvokeBlock { type = lhs, source_block = NextBlock() });
                case TokenType.PIPE: {
                        inc();
                        switch(tokenType) {
                            case TokenType.STAR:
                                inc();
                                return CompoundExpr(new ExMap { src = lhs, map = new ExInvoke { expr = new ExSelf { up = 1 }, args = ExTuple.Expr(NextExpr())  } , expr = true});
                            case TokenType.PERIOD:
								inc();
								return CompoundExpr(new ExMap { src = lhs, map = new ExInvoke { expr = new ExSelf { up = 1 }, args = ExTuple.Expr(NextTerm()) }, expr = true });
							case TokenType.SLASH:
								inc();
								return CompoundExpr(new ExMap { src = lhs, map = NextExpr(), expr = true });
                            default: {
                                    var cond = default(INode);
                                    var type = default(INode);
                                    switch(tokenType) {
                                        case TokenType.ANGLE_L: {
                                                inc();
                                                cond = NextExpr();
                                                switch(tokenType) {
                                                    case TokenType.ANGLE_R:
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
                                                type = NextExpr();
                                                break;
                                            }
                                    }
                                    return CompoundExpr(new ExMap { src = lhs, cond = cond, type = type, map = NextTerm() });
                                }
						}
                    }
                case TokenType.TUPLE_L: {
                        inc();
                        return CompoundExpr(new ExInvoke { expr = lhs, args = NextArgTuple() });
                    }
                case TokenType.STAR: {
						inc();
                        switch(tokenType) {
                            case TokenType.PIPE:
                                inc();
								return CompoundExpr(new ExMap {
									src = NextExpr(),
									map = lhs,
								});
                            default:
								return CompoundExpr(new ExInvoke {
									expr = lhs,
									args = ExTuple.SpreadExpr(NextExpr()),
								});
						}
					}
                case TokenType.PERIOD: {
                        inc();
                        switch(tokenType) {
                            case TokenType.PIPE:
                                inc();
								return CompoundExpr(new ExMap {
									src = NextTerm(),
									map = lhs,
								});
                            case TokenType.PERIOD:
								inc();
								return CompoundExpr(new ExTemp { lhs = lhs, rhs = NextExpr() });
							default:
								return CompoundExpr(new ExInvoke { expr = lhs, args = ExTuple.SpreadExpr(NextTerm()) });
						}
                    }
                case TokenType.SHOUT: {
                        inc();
                        return CompoundExpr(new ExInvoke { expr = lhs, args = ExTuple.Empty });
                    }
                case TokenType.PERCENT: {
                        inc();
                        switch(tokenType) {
                            case TokenType.PLUS:
                                //Accumulator
                                return CompoundExpr(new ExSeqOp { fn = lhs, op = ExSeqOp.EOp.Reduce });
                            case TokenType.MINUS:
                                //Sliding window
                                return CompoundExpr(new ExSeqOp { fn=lhs, op = ExSeqOp.EOp.SlidingWindow });
                            default:
								return CompoundExpr(new ExSpread { value = lhs });
						}
                    }
                case TokenType.EQUAL: {
                        inc();
                        var Eq = (bool invert) => CompoundExpr(new ExEqual {
                            lhs = lhs,
                            rhs = NextTerm(),
                            invert = invert
                        });
                        switch(tokenType) {
                            case TokenType.PLUS: {
                                    inc();
                                    return Eq(false);
                                }
                            case TokenType.MINUS: {
                                    inc();
                                    return Eq(true);
                                }
                            case TokenType.ANGLE_R:{
                                var rhs = NextExpr();
                                    return CompoundExpr(new ExFn { pars = (ExTuple)lhs, result = rhs });
								}
							case TokenType.TUPLE_L: {
                                    var tup = NextTupleOrLisp();
									throw new Exception("Impl tuple match");
								}
                            case TokenType.ARRAY_L: {
                                    var arr = NextArrayOrLisp();
									throw new Exception("Impl array match");
								}
							case TokenType.BLOCK_L: {
                                    //Structure match
                                    var block = (ExBlock)NextExpr();
                                    foreach(var ex in block.statements) {
                                        switch(ex) {
                                            case ExIs:
                                            case ExIsAssign:
                                            case ExInvoke:
                                            case ExInvokeBlock:
                                            case StDefKey:
                                                continue;
                                            default:

                                                throw new Exception("Unsupported");
                                        }
                                    }
                                    throw new Exception("Impl structure match");
                                }
                            case TokenType.COLON: {
                                    var rhs = NextTerm();
                                    return CompoundExpr(new ExIsAssign { lhs = lhs, rhs = rhs });
                                }
                            default:{
                                var pattern = NextExpr();
                                switch(tokenType) {
                                    case TokenType.COLON:
                                        inc();
                                        var symbol = NextSymbol();
                                        return CompoundExpr( new ExIs { lhs = lhs, rhs = pattern, key = symbol.key });
                                    default:
                                        return CompoundExpr( new ExIs{ lhs = lhs, rhs = pattern, key = "_"});
                                }
                                    throw new Exception();
                                }
                        }
                        throw new Exception();
                    }
                case TokenType.SLASH: {
                        inc();
                        switch(tokenType) {
                            case TokenType.PIPE:
                                inc();
                                var fn = NextTerm();
                                return CompoundExpr(new ExFn {
                                    pars = ExTuple.Empty,
                                    result = new ExInvoke {
                                        expr = fn,
                                        args = ExTuple.Expr(lhs)
                                    }
                                });
                            case TokenType.SLASH:
                                inc();
                                //INDEXER
                                return CompoundExpr(new ExAt { src = lhs, index = [NextTerm()] });
                            case TokenType.STAR:
                                inc();
								return CompoundExpr(new ExAt { src = lhs, index = [NextExpr()] });
							case TokenType.PERIOD:
                                inc();
								return CompoundExpr(new ExAt { src = lhs, index = [NextTerm()] });
							case TokenType.NAME:
                                var name = currToken.str;
                                inc();
                                return CompoundExpr(new ExMemberKey { src = lhs, key = name });
                            case TokenType.INTEGER:
                                var num = currToken.str;
                                inc();
                                return CompoundExpr(new ExMemberDigit { src = lhs, num = num });
                                throw new Exception();
                            case TokenType.BLOCK_L:
                                return CompoundExpr(new ExMemberBlock { lhs = lhs, rhs = (ExBlock)NextExpr(), local = false });
                            default:
                                return CompoundExpr(new ExMemberExpr { lhs = lhs, rhs = NextExpr(), local = true });
                        }
                    }
                case TokenType.ARRAY_L: {
                        var arr = (ExSeq)NextArrayOrLisp();
                        return CompoundExpr(new ExAt { src = lhs, index = arr.items });
                    }
                case TokenType.AT: {
                        inc();
                        var term = NextTerm();
                        return CompoundExpr(new ExCompose { items = (ExTuple)NextTupleOrLisp()});
                    }
				case TokenType.QUESTION: {
                        inc();
                        switch(tokenType) {
                            case TokenType.PIPE:
                                var rhs = NextExpr();
                                return CompoundExpr(new ExFilter { lhs = lhs, rhs = rhs });
                            case TokenType.PERCENT: {
									inc();
									return CompoundExpr(new ExLoop { condition = lhs, positive = NextExpr() });
                                }
                            case TokenType.COLON: {
                                    inc();
                                    return new ExCriteria { item = lhs, cond = NextExpr() };
                                }
                            case TokenType.BLOCK_L: {
                                    inc();
                                    var items = new List<(INode cond, INode yes)> { };
                                    ReadBranch:
                                    switch(tokenType) {
                                        case TokenType.BLOCK_R:
                                            inc();
                                            return CompoundExpr(new ExMatchPattern {
                                                item = lhs,
                                                branches = items
                                            });
                                    }
                                    var cond_group = new List<INode> { };
                                    ReadItem:
                                    cond_group.Add(NextExpr());
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
                                            var yes = NextExpr();
                                            foreach(var c in cond_group) {
                                                items.Add((c, yes));
                                            }
                                            goto ReadBranch;
                                        default:
                                            goto ReadItem;
                                    }
                                }
                            case TokenType.ARRAY_L: {
                                    inc();
                                    INode type = null;
                                    switch(tokenType) {
                                        case TokenType.COLON:
                                            inc();
                                            type = NextExpr();
                                            break;
                                    }
                                    var items = new List<(INode cond, INode yes, INode no)> { };
                                    Read:
                                    var cond = NextExpr();
                                    switch(tokenType) {
                                        case TokenType.COLON: {
                                                inc();
                                                var yes = NextExpr();
                                                items.Add((cond, yes, null));
                                                break;
                                            }
                                        default:
                                            throw new Exception();
                                    }
                                    switch(tokenType) {
                                        case TokenType.ARRAY_R: {
                                                inc();
                                                return CompoundExpr(new ExCondSeq {
                                                    type = type,
                                                    filter = lhs,
                                                    items = items
                                                });
                                            }
                                        default:
                                            goto Read;
                                    }
                                }
                            case TokenType.PLUS: {
                                    inc();
                                    switch(tokenType) {
                                        case TokenType.PLUS:
                                            inc();
                                            return CompoundExpr(new ExLoop { condition = lhs, positive = NextExpr() });
                                            /*
                                        case TokenType.MINUS:
                                            inc();
                                            return CompoundExpr(new ExBranch {
                                                condition = lhs,
                                                positive = NextExpr(),
                                                negative = NextExpr()
                                            });
                                            */
                                        default:
											var positive = NextStatement();
											var negative = default(INode);
											switch(tokenType) {
												case TokenType.QUESTION: {
														inc();
														switch(tokenType) {
															case TokenType.MINUS: {
																	inc();
																	negative = NextStatement();
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
											return CompoundExpr(new ExBranch {
												condition = lhs,
												positive = positive,
												negative = negative
											});
									}
                                }
                            default:
                                dec();
                                break;
                        }
                        break;
                    }
				case TokenType.AND: {
                        inc();
						var rhs = NextTerm();
						return new ExDyadic { lhs = lhs, rhs = rhs, fn = ExDyadic.EFn.AND };
					}
				case TokenType.OR: {
                        inc();
						var rhs = NextTerm();
						return new ExDyadic { lhs = lhs, rhs = rhs, fn = ExDyadic.EFn.OR };
					}
                case TokenType.XOR: {
                        inc();
						var rhs = NextTerm();
						return new ExDyadic { lhs = lhs, rhs = rhs, fn = ExDyadic.EFn.XOR };
					}
			}
            return lhs;
        }
        INode NextTerm () {
            Read:
            switch(tokenType) {
				/*
				case TokenType.SWIRL:
					inc();
					return new ExPointer { dest = NextTerm() };
                */

				case TokenType.IOTA:
                    inc();
                    return (ExInvoke.Fn($"{(char)TokenType.IOTA}", NextTerm()));

				case TokenType.MINUS:{
					inc();
                        switch(tokenType) {
                            //fn type
                            case TokenType.ANGLE_R: {
                                    inc();
                                    switch(tokenType) {
                                        case TokenType.TUPLE_R:
                                        case TokenType.BLOCK_R:
                                        case TokenType.ARRAY_R:
                                        case TokenType.ANGLE_R:
                                            return new ExFnType{};
                                        default:
                                            var output = NextExpr();
                                            return (new ExFnType {rhs = output});
                                    }
                                }
                            case TokenType.TUPLE_R:
                            case TokenType.BLOCK_R:
                            case TokenType.ARRAY_R:
                                return new ExRange { };
                            default:
                                var to = NextExpr();
                                return (new ExRange { rhs = to });
                        }
                    }
				case TokenType.ARRAY_L:
                    return NextArrayOrLisp();
                case TokenType.QUESTION:
                    return NextLambda();
                case TokenType.NAME:
                    return NextSymbol();
                case TokenType.CARET:
                    return NextCaretSymbol();
                case TokenType.STRING:
                    return NextString();
                case TokenType.INTEGER:
                    return NextInteger();
                case TokenType.BLOCK_L:
                    return NextBlock();
                case TokenType.TUPLE_L:
                    return NextTupleOrLisp();
                    /*
                case TokenType.PERCENT:
                    return new ExprSpread { value= NextExpression() };
                    */
                case TokenType.QUOTE:
                    inc();
                    return new ExAlias { expr = NextExpr() };
                case TokenType.COMMA:
                    inc();
                    goto Read;
                case TokenType.SPACE:
                    inc();
                    goto Read;
            }
            throw new Exception($"Unexpected token in expression: {currToken.type}");
        }



		string ReadLispOp () {

			var start = index;
			var d = new Dictionary<string, int> {
				["+"] = 1,
				["-"] = 2,
				["*"] = 3,
				["/"] = 4,
				["&"] = 5,
				["|"] = 6,
				["^"] = 7,
				["%"] = 8,
				["="] = 9,
                [">"] = 10,
                ["<"] = 11,
                [">="] = 12,
                ["<="] = 13,

                [">>"] = 14,
                ["<<"] = 15
			};
			var op = "";
			ReadOp:
			switch(tokenType) {
				case TokenType.PLUS:
				case TokenType.MINUS:
				case TokenType.STAR:
				case TokenType.SLASH:
				case TokenType.CARET:
				case TokenType.PIPE:
				case TokenType.AMP:
				case TokenType.PERCENT:
				case TokenType.EQUAL:
                case TokenType.ANGLE_L:
                case TokenType.ANGLE_R:
                case TokenType.PERIOD:
					op += currToken.str;
					inc();
					goto ReadOp;
				case TokenType.COLON:
					if(op == "") {
						return "";
					}
					inc();
					return op;
				default:
					index = start;
					return "";
			}
		}

		INode NextTupleOrLisp () {
            inc();
            var op = ReadLispOp();
            var expr = NextExpr();
            switch(tokenType) {
                case TokenType.TUPLE_R: {
                        inc();
                        return expr;
                    }
                default:
                    var t = NextTuple(expr);
                    if(op.Any()) {
                        return new ExLisp { args = [..t.vals], op = op };
                    }
					return t;
			}
		}
        ExTuple NextArgTuple () {
            switch(tokenType) {
                case TokenType.TUPLE_R:
                    inc();
                    return ExTuple.Empty;
                default:
                    return NextTuple(NextExpr());
            }
        }
        ExTuple NextTuple (INode first) {
            var items = new List<(string key, INode val)> { };
            return AddEntry(first);
            ExTuple AddEntry (INode lhs) {
                switch(tokenType) {
                    case TokenType.COLON:
                        NextPair(lhs);
                        break;
                    default:
                        items.Add((null, lhs));
                        break;
                }
                switch(tokenType) {
                    case TokenType.TUPLE_R:
                        inc();
                        return new ExTuple { items = items.ToArray() };
                    default:
                        return AddEntry(NextExpr());
                }
            }
            void NextPair (INode lhs) {
                switch(lhs) {
                    case ExUpKey { up: -1, key: { } key }: {
                            inc();
                            var val = NextExpr();
                            items.Add((key, val));
                            break;
                        }
                    default:
                        throw new Exception("Name expected");
                }
            }
        }
        INode NextArrayOrLisp () {
            List<INode> items = [];
            inc();
            INode type = null;
            switch(tokenType) {
                case TokenType.COLON:
                    inc();
                    type = NextExpr();
                    break;
            }

            Check:
            switch(tokenType) {
                case TokenType.COMMA:
                    inc();
                    goto Check;
                case TokenType.ARRAY_R:
                    inc();
                    return new ExSeq { items = items, type = type };
                default:
                    var item = NextExpr();
                    if(tokenType == TokenType.COLON) {
                        
                    } else {

                    }
                    switch(tokenType) {
                        case TokenType.COLON:
                            var l = new List<INode> { item };
                            while(tokenType == TokenType.COLON) {
                                inc();
                                l.Add(NextExpr());
                            }
                            var tuple = ExTuple.ListExpr(l);
                            items.Add(tuple);
                            break;
                        default:
							items.Add(item);
                            break;
					}
                    /*
                    switch(tokenType) {
                        case TokenType.COLON:
                            type = item;
                            inc();
                            goto Check;
                    }
                    */
                    
                    goto Check;
            }
        }
        INode NextLambda () {
            inc();
            var t = tokenType;
            switch(t) {
                case TokenType.SHOUT: {
                        inc();
                        var result = NextExpr();
                        return new ExFn { pars = ExTuple.Empty, result = result };
                    }
                case TokenType.TUPLE_L:
                    inc();
                    var pars = NextArgTuple().ParTuple();
					switch(tokenType) {
						case TokenType.COLON:
							inc();
							break;
					}
					var r = new ExFn { pars = pars, result = NextExpr() };
                    return r;
                default:
                    throw new Exception($"Unexpected token {t}");
            }
        }
        ExUpKey NextSymbol () {
            var name = currToken.str;
            inc();
            return new ExUpKey { key = name, up = -1 };
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
                        var s = new ExUpKey { up = up, key = currToken.str };
                        inc();
                        return s;
                    }
                case TokenType.CASH: {
                        //Return This
                        var s = new ExSelf { up = up };
                        inc();
                        return s;
                    }
                    /*
                case TokenType.L_PAREN:
                    return new ExMemberTuple { lhs = new ExSelf { up = up }, rhs = (ExTuple)NextExpression(), local = true };
                    */
                default:
                    return new ExSelf { up = up };
            }
        }
        public ExVal NextString () {
            var value = tokens[index].str;
            inc();
            return new ExVal { value = value };
        }
        public ExVal NextInteger () {
            var value = int.Parse(tokens[index].str);
            inc();
            return new ExVal { value = value };
        }
        public ExBlock NextBlock () {
            inc();
            var ele = new List<INode>();
            Check:
            switch(tokenType) {
                case TokenType.BLOCK_R:
                    inc();
                    return new ExBlock { statements = ele };
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
    public class ExTemp : INode {
        public INode lhs;
        public INode rhs;
		public object Eval (IScope ctx) {
            var v = lhs.Eval(ctx);
            var sc = ctx.MakeTemp();
            sc.locals["_"] = v;
            return rhs.Eval(sc);
		}
	}
	public class ExFnType : INode {
		public INode lhs = ExVal.Empty;
		public INode rhs = ExVal.Empty;
		public object Eval (IScope ctx) {
			return new VFnInterface {
				lhs = (IType[])lhs.Eval(ctx),
				rhs = (IType)rhs.Eval(ctx)
			};
		}
	}
	public class SuperVal : LVal {
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
	public class ExRange : INode {
		public INode lhs = ExVal.Empty;
		public INode rhs = ExVal.Empty;
		public object Eval (IScope ctx) {
            return null;
		}
	}
    public class VRange {
        public int? start;
        public int? end;
        public IEnumerable<int> GetInt() {
            for(var i = start ?? throw new Exception(); i < (end ?? throw new Exception()); i++) { yield return i; }
        }
    }
    public class ExSeqOp : INode {
        public enum EOp {
            Reduce, SlidingWindow
        };
        public EOp op;
        public INode fn;
		public object Eval (IScope ctx) {
			throw new NotImplementedException();
		}
	}
	public class ExSpread : INode {
        public INode value;

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
            object Handle(object val) {
				switch(val) {
					case VTuple vt:
						return new VSpread { value = vt };
					case Array a:
						return new VSpread { value = a };
					case VAlias va:
                        //TODO: fix
                        return Handle(va.Deref());
					case VEmpty:
						return VEmpty.VALUE;
					case null: return null;
					default: return val;
				}
				throw new Exception("Tuple or array or record expected");
			}
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
                    foreach(var _a in a) {items.Add(_a);}
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
    public class ExInvokeBlock : INode {
        public INode type;
        public ExBlock source_block;
        public XElement ToXML () => new("VarBlock", new XAttribute("type", type), source_block.ToXML());
        public string Source => $"{type} {source_block.Source}";
        public object MakeScope (VDictScope ctx) => source_block.MakeScope(ctx);
        public object Eval (IScope ctx) {
            var getResult = () => source_block.Eval(ctx);
            var t = type.Eval(ctx);

            var getArgs = () => {
				var args = getResult();
				switch(args) {
					case VDictScope vds:
                        return new VTuple { items = vds.locals.Select(pair => (pair.Key, pair.Value)).ToArray() };
					default:
                        return ExTuple.SpreadVal(args).EvalTuple(ctx);
				}
			};

            switch(t) {
                case VEmpty: throw new Exception("Type not found");
                case Type tt: {
                        var v = getResult();
                        var r = new VType(tt).Cast(v, v?.GetType());
                        return r;
                        return new VCast { type = v?.GetType(), val = r };
                    }
                case VClass vc: {
                        return vc.VarBlock(ctx, source_block);
                    }
                case VInterface vi: {
                        break;
                    }
                case ExtendTicket ex:
                    switch(ex.on) {
                        case Type:
						case ValPattern:
						case VClass:
							return new ExtendObject { call = ex, src = source_block };
                        default:
							return new ExtendObject { call = ex, src = source_block }.Init(ctx);
					}
                case ValKeyword.GET: {
                        return new VGet { ctx = ctx, get = source_block };
                    }
                case ValKeyword.SET: {
                        return new VSet { ctx = ctx, set = source_block };
                    }
                case ValKeyword.RETURN: {
                        return new VRet(getResult(), 1);
                    }
                case ValKeyword.CLASS: {
                        return StDefKey.MakeClass(ctx, source_block);
                    }
                case ValKeyword.INTERFACE: {
                        if(getResult() is VDictScope s) {
                            return new VInterface { _static = s, source = source_block };
                        }
                        throw new Exception("Object expected");
                    }
                case ValKeyword.ENUM: {
                        var locals = new Dictionary<string, object> { };
                        foreach(var st in source_block.statements) {
                            switch(st) {
                                case ExUpKey { up: -1, key: { } k }:
                                    locals[k] = new object();
                                    break;
                                case ExInvoke { expr: ExUpKey { up: -1, key: { } k } }:
                                    locals[k] = new object();
                                    break;
                            }
                        }
                        return new VDictScope { locals = [], parent = ctx, temp = false };
					}
				default:
					return ExInvoke.InvokeFunc(ctx, t, getArgs);

			}
			throw new Exception("Type expected");
            //return result;
        }
    }
    public class ExEqual : INode {
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
	public class ExIsAssign : INode {

        public INode lhs;
        public INode rhs;
		public object Eval (IScope ctx) {
            return null;
		}
	}
    public class StructurePattern { }
	public class ExIs : INode {
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
				case (var v, VClass vc): {
						if(v is VDictScope vds && vds.HasClass(vc)) {
							return true;
						} else {
							return false;
						}
					}
                case (var v, VComplement co):
                    return !Is(v, co.on);
                case (var v, VInterType vmp):
                    return vmp.Accept(v);
                    throw new Exception();
                case (var v, VCriteria vcr):
                    return vcr.Accept(v);
				case (var v, VInterface vi): {
						if(v is VDictScope vds && vds.HasInterface(vi)) {
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
    public class ExCriteria : INode {
        public INode item;
        public INode cond;
        public object Eval (IScope ctx) {
            return new VCriteria { type = item.Eval(ctx), cond = cond };
        }
    }
    public class VCriteria {
        public object type;
        public INode cond;
        public bool Accept(object o) {
            return ExIs.Is(o, type) && (bool)cond.Eval(ExMemberBlock.MakeScope(o));
        }
    }
    public class ExBranch : INode {
        public INode condition;
        public INode positive;
        public INode negative;
        public string Source => $"{condition.Source} ?+ {positive.Source}{(negative != null ? $" ?- {negative.Source}" : $"")}";
        public object Eval (IScope ctx) {
            var cond = condition.Eval(ctx);
            switch(cond) {
                case true:
                    return  positive.Eval(ctx);
                case false:
                    switch(negative) {
                        case null:  return VEmpty.VALUE;
                        default:    return negative.Eval(ctx);
                    }
                default:
                    throw new Exception("bit expected");
            }
        }
    }
    public class ExLoop : INode {
        public INode condition;
        public INode positive;
        public object Eval (IScope ctx) {
            object r = VEmpty.VALUE;
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
	public class ExAt : INode {
		public INode src;
		public List<INode> index;
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
	public class ExInvoke : INode {
        public INode expr;
        public ExTuple args;
        //public string Source => $"{expr.Source}{(args.Count > 1 ? $"({string.Join(", ", args.Select(a => a.Source))})" : args.Count == 1 ? $"*{args.Single().Source}" : $"!")}";

        public static ExInvoke Fn (string symbol, INode arg) => new() { expr = new ExUpKey { key = symbol }, args = ExTuple.Expr(arg) };

        public object Eval (IScope ctx) {
            var f = expr.Eval(ctx);
            switch(f) {
                case VError ve:
                    throw new Exception(ve.msg);
                case ValKeyword.MAGIC:      return new ValMagic();
                case ValKeyword.GO:         return new VGo { target = args.items[0].value as ExUpKey };
                case ValKeyword.SET:        return new VSet { ctx= ctx, set = args };
                case ValKeyword.GET:        return new VGet { ctx = ctx, get = args };
                case ValKeyword.ANY:
                    var at = args.EvalTuple(ctx);
					return new VInterType { items = at.vals, all = false };
                case ValKeyword.ALL:        return new VInterType { items = args.EvalTuple(ctx).vals, all = true };
                case ValKeyword.EXTEND:     return new ExtendTicket { on = args.EvalExpression(ctx), inherit = true };
                case ValKeyword.COMPLEMENT: return new VComplement { on = args.EvalExpression(ctx) };
                case ValKeyword.UNALIAS:{
                        foreach(var(k,v) in args.items) {
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
				case ValKeyword.RETURN: return new VRet(args.EvalExpression(ctx), 1);
                case ValKeyword.YIELD:  return new VYield(args.EvalExpression(ctx));
                case ValKeyword.IMPLEMENT: {
                        var vds = (VDictScope)ctx;
                        var arg = args.EvalTuple(ctx);
                        foreach(var (k, v) in arg.items) {
                            switch(v) {
                                case VInterface vi:
									vi.Register(vds);
                                    break;
                                default:throw new Exception("Interface expected");
							}
                        }
                        return VEmpty.VALUE;
                    }
                case ValKeyword.EMBED: {
                        var vds = (VDictScope)ctx;
                        var arg = args.EvalTuple(ctx);
                        foreach(var (k, v) in arg.items) {
                            switch(v) {
                                case VClass vc:
                                    vc.Embed(vds);
                                    break;
                                case VDictScope _vds: {
                                        foreach(var other in _vds.locals.Keys) {
                                            StDefKey.Define(ctx, other, new VAlias { ctx = _vds, expr = new ExUpKey { key = other, up = 1 } });
                                        }
                                        break;
                                    }
                                default: throw new Exception("Class expected");
                            }
                        }
                        return VEmpty.VALUE;
                    }
				case ValKeyword.INHERIT: {
                        throw new Exception();
                        foreach(var(k, v) in args.EvalTuple(ctx).items) {
                            switch(v) {
                                case VTuple vt:
                                    vt.Inherit(ctx);
                                    break;
							}
                        }
                        return VEmpty.VALUE;
                    }
                case ValKeyword.FN:
                default:    return InvokePars(ctx, f, args);
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
                case VEmpty: {
                        throw new Exception("Function not found");
                    }
                case VError ve: {
                        throw new Exception(ve.msg);
                    }
                case VConstructor vc: {
                        var v = evalArgs().items.Select(pair => pair.val).ToArray();
                        var c = vc.t.GetConstructor(v.Select(arg => (arg as object).GetType()).ToArray());
                        if(c == null) {
                            throw new Exception("Constructor not found");
                        }
                        return c.Invoke(v);
                    }
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
                        [{}a,{}b] => (a,b),
                        [{}a,{}b,{}c] => (a,b,c),
                        [{}a,{}b,{}c,{}d] => (a,b,c,d),
                        [{}a,{}b,{}c,{}d,{}e] => (a,b,c,d,e),
                        [{}a,{}b,{}c,{}d,{}e,{}f] => (a,b,c,d,e,f),
                        [{}a,{}b,{}c,{}d,{}e,{}f,{}g] => (a,b,c,d,e,f,g),
						[{}a,{}b,{}c,{}d,{}e,{}f,{}g,{}h] => (a,b,c,d,e,f,h),
						[{}a,{}b,{}c,{}d,{}e,{}f,{}g,{}h,{}i] => (a,b,c,d,e,f,h,i),
						});
                        var at = tt.MakeGenericType(argTypes);
                        return new VType(tt.MakeGenericType(argTypes)).Cast(aa, at);
                    }
                case VFn vf: {
                        return vf.CallFunc(evalArgs);
                    }
                case Delegate de: {
                        var args = evalArgs();
						var v = args.items.Select(pair => pair.val).ToArray();
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
                        throw new Exception();
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
                case ExtendObject ext:
                    throw new Exception();
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
                    case Array a:{
						var ind = ((Array)evalArgs().vals.Single()).OfType<int>().ToArray();
						return new VIndex {
                            
                            Get = () => a.GetValue(ind),
                            Set = (object o) => a.SetValue(o, ind) };
					}


				case IDictionary d: {
						var ind = evalArgs().vals.Single();
                        return new VIndex { Get = () => d[ind], Set = (object o) => d[ind] = o };
					}
				case IEnumerable e: {
						var ind = evalArgs().vals.Single();
						return ind switch {
							int i => new VIndex {
                                Get = () => e.Cast<object>().ElementAt(i),
                                Set = o => throw new Exception()
							},
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
								VObjScope vos => Get(vos.o),
								_ => throw new Exception(),
							};
						}
					}
			}
            throw new Exception($"Unknown function {lhs}");
        }
    }
    public class StReturn : INode {
        public int up = 1;
        public INode val;
        public object Eval (IScope ctx) =>
         new VRet(val.Eval(ctx), up);
    }
    public class ExBlock : INode {
        public List<INode> statements;
        public XElement ToXML () => new("Block", statements.Select(i => i.ToXML()));
        public string Source => $"{{{string.Join(", ", statements.Select(i => i.Source))}}}";
        public bool obj => _obj ??= statements.Any(s => s is StDefFn or StDefKey or StDefMulti);
        public bool? _obj;
        public object Eval (IScope ctx) {
            if(statements.Count == 0)
                return VEmpty.VALUE;
			var f = new VDictScope(ctx, false);
			object r = VEmpty.VALUE;
			foreach(var s in statements) {
				
				switch(s) {
                    case ExFnType { lhs:ExInvoke{ expr:ExUpKey{ allowDefine:true, key:{ }key } } } ft:
                        //fix type
						StDefKey.Define(ctx, key, new VDeclared { name = key, type = ft });
                        continue;
                    case ExFnType { lhs: ExUpKey { allowDefine: true, key: { } key }, rhs:{ }rhs } ft:
                        //Fix the type
						StDefKey.Define(ctx, key, new VDeclared { name = key, type = ft });
						continue;
				}

				r = s.Eval(f);

				AutoKey(f, s, r);
				switch(r) {
					case VRet vr:  return vr.Up();
                    case VYield vy:   throw new Exception();
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
                    case VRet vr:  return vr.Up();
				}
				f.SetLocal("__", r);
			}
            return f;
        }
        public VDictScope MakeScope (IScope ctx) => new(ctx, false);
        public object StagedEval (IScope ctx) => StagedApply(MakeScope(ctx));

        public void AutoKey(IScope f, INode s, object r) {
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
			}
		}

        //Called during class def
        public object StagedApply (IScope f) {
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
					case StDefFn df when df.value is ExInvokeBlock { type: ExUpKey { key: "class" or "interface" } }: {
                            df.Define(f);
                            break;
                        }
                    case StDefKey { value: ExInvokeBlock { type: ExUpKey { key: "class", up: -1 }, source_block: { } block } } kv: {
                            var _static = StDefKey.DeclareClass(f, block, kv.key);
                            def += () => block.StagedApply(_static);
                            break;
                        }
                    case StDefKey { value: ExInvokeBlock { type: ExUpKey { key: "interface", up: -1 }, source_block: { } block } } kv: {
                            var _static = StDefKey.DeclareInterface(f, block, kv.key);
                            def += () => block.StagedApply(_static);
                            break;
                        }
                    case StDefKey { key:{ }key, value: ExUpKey { key: "label" } }:
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
				object r = VEmpty.VALUE;
				var s = seq[i];
				switch(s) {
					case StDefKey { value: ExInvokeBlock { type: ExUpKey { key: "defer", up: -1 }, source_block: { } _block } }:
						r = _block.EvalDefer(f);
						break;
					default:
						r = s.Eval(f);
						break;
				}
                AutoKey(f, s, r);
				switch(r) {
					case VRet vr:
						return vr.Up();
					case VGo vg:
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
				f.SetLocal("__", r);
				i++;
            }
			return f;
		}
        public object EvalDefer (IScope ctx) {
            return null;
        }
    }
    public record ExUpKey : INode, LVal {

        public bool allowDefine => up is -1 or 1;
        public int up = -1;
        public string key;
        public XElement ToXML () => new("Symbol", new XAttribute("key", key), new XAttribute("level", $"{up}"));
        public string Source => $"{new string('^', up)}{key}";
        public object Eval (IScope ctx) => GetValue(ctx);
        public object Get (IScope ctx) => ctx.Get(key, up);
		public object GetValue(IScope ctx) {
			var r = ctx.Get(key, up);
			return r switch {
				VGet vg => vg.Get(),
                VIndex vi => vi.Get(),
				VAlias va => va.Deref(),
				_ => r,
			};
		}
        public object Assign(IScope ctx, Func<object> getNext) {
            return StAssignSymbol.Assign(ctx, key, up, getNext);
        }
    }
    public class ExSelf : INode {
        public int up;
        public object Eval (IScope ctx) {
            for(int i = 1; i < up; i++) ctx = ctx.parent;
            return ctx;
        }
    }
    public class ExMemberDigit : INode {
		/*, LVal*/
		public INode src;
        public string num;

        public object Eval (IScope ctx) {
            return 0;
        }
    }

    //Try to find extension fn, then member fn
    public class ExFindFn {

    }

	public class ExMemberKey : INode, LVal {
        public INode src;
        public string key;
        public bool publicOnly = true;
        public object Eval (IScope ctx) => Get(ctx);
        public object Get(IScope ctx) {
			var source = src.Eval(ctx);
            object f (IScope ctx) {
                var v = ctx.GetLocal(key);
                return v switch {
                    VGet vg => vg.Get(),
                    VAlias va => va.Deref(),
                    _ => v
                };
			}
			switch(source) {
                case VError ve: throw new Exception(ve.msg);
				case VDictScope s:  { return f(s);}
				case VClass vc:     { return f(vc._static); }
				case Type t:        { return f(new VTypeScope { t = t }); }
				case Args a:        { return a[key]; }
				case object o:      { return f(new VObjScope { o = o }); }
			}
			throw new Exception("Object expected");
		}
        public object Assign(IScope ctx, Func<object> getVal) {
			var source = src.Eval(ctx);
            var f = StAssignSymbol.AssignLocal;
			switch(source) {
				case VDictScope s:  { return f(s, key, getVal); }
				case VClass vc:     { return f(vc._static, key, getVal); }
				case Type t:        { return f(new VTypeScope { t = t }, key, getVal); }
				case Args a:        { return a[key] = getVal(); }
				case object o:      { return f(new VObjScope { o = o }, key, getVal); }
			}
			throw new Exception("Object expected");
		}
    }
	public class ExMemberBlock : INode, LVal {
		public INode lhs;
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
	public class ExMemberExpr : INode, LVal {
		public INode lhs;
		public INode rhs;
		public bool local = true;
		public object Assign (IScope ctx, Func<object> getVal) {
            return null;
		}
		public object Eval (IScope ctx) {
			return rhs.Eval(ExMemberBlock.MakeScope(lhs.Eval(ctx), ctx, local));
		}
	}
	public class ExVal : INode {
        public static ExVal From (object v) => new() { value = v };
        public object value;
        public XElement ToXML () => new("Value", new XAttribute("value", value));
        public string Source => $"{value}";
        public object Eval (IScope ctx) => value;
        public static ExVal Empty = new() { value = VEmpty.VALUE };
	}
    public class ExCondSeq : INode {
        public INode type;
        public INode filter;
        public List<(INode cond, INode yes, INode no)> items;
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
                                    case VEmpty:              continue;
									case ValKeyword.CONTINUE:   continue;
									case ValKeyword.BREAK:      goto Done;
									case VRet vr:          return vr.Up();
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
                                    case VEmpty:              continue;
									case ValKeyword.CONTINUE:   continue;
									case ValKeyword.BREAK:      goto Done;
									case VRet vr:          return vr.Up();
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
            return ExMap.Convert(lis, type is { }t ? () => (Type)t.Eval(ctx) : null);
			var arr = lis.ToArray();
            return arr;
        }
    }
    public class ExMatchPattern : INode {
        public INode item;
        public List<(INode cond, INode yes)> branches;
        public object Eval (IScope ctx) {
            var val = item.Eval(ctx);
            if(val is VIndex vi) {
                val = vi.Get();
            }
            return Match(ctx, branches, val);
		}
        public static object Match(IScope ctx, List<(INode cond, INode yes)> branches, object val) {
			//To do: Add recursive call
			var inner_ctx = ctx.MakeTemp(val);
			inner_ctx.locals["_default"] = val;
			foreach(var (cond, yes) in branches) {
                //TODO: Allow lambda matches
				var b = cond.Eval(inner_ctx);
				if(Is(b) || b is ValKeyword.DEFAULT) {
                    var res = yes.Eval(inner_ctx);
                    if(res is ValKeyword.FALL) {
                        continue;
                    }
                    return res;
				}
			}
			throw new Exception("Fell out of match expression");
			bool Is (object pattern) {
                switch(pattern) {
                    case VInterType mp:
                        return mp.Accept(val);
                }
				if(Equals(val, pattern))
					return true;
				else
					return false;
			}
		}
    }
    public class ExCompose : INode{
        public ExTuple items;
        public object Eval (IScope ctx) => throw new Exception();
    }
    public class ExFilter : INode {
        public INode lhs;
        public INode rhs;
		public object Eval (IScope ctx) {
            return null;

            var l = lhs.Eval(ctx);
            var r = (VFn)rhs.Eval(ctx);
			object FilterFn (dynamic seq, VFn f) {
				object tr (IScope inner_ctx, object item) =>
					ExInvoke.InvokeArgs(inner_ctx, f, item switch {
						VTuple vt => vt,
						_ => VTuple.Single(item)
					});
				return ExMap.Map(seq, ctx, null, null, (Transform)tr);
			}
		}
	}
    public class ExMap : INode, LVal {
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
                case VEmpty: throw new Exception("Variable not found");
                case ICollection c: return Map(c);
                case IEnumerable e: return Map(e);
                case VRange r: return Map(r.GetInt());
				case VDictScope vds: {
						if(vds._seq(out var seq) && seq is VFn vf) return Map(vf.CallPars(ctx, ExTuple.Empty));
						throw new Exception();
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
		public static object Map (dynamic seq, IScope ctx, INode cond, Func<Type> t, Transform tr) {

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
					case ValKeyword.CONTINUE: continue;
					case ValKeyword.BREAK: goto Done;
					case VRet vr: return vr.Up();
					default:
						result.Add(r);
						continue;
				}
			}
			Done:
			return Convert(result, t);
		}
		public object Assign (IScope ctx, Func<object> getVal) {
            return null;
		}
	}
    public class StDefKey : INode {
        public string key;
        public INode value;
        public INode type;
        public XElement ToXML () => new("KeyVal", new XAttribute("key", key), value.ToXML());
        public string Source => $"{key}:{value?.Source ?? "null"}";
        public object Eval (IScope ctx) {
            var val = value.Eval(ctx);
            switch(val) {
                case VError ve: throw new Exception(ve.msg);
                default: return Define(ctx, key, val);
            }
        }
        public static object DefineFrom (IScope ctx, string key, IScope from) => Define(ctx, key, from.Get(key));
        public static object Define (IScope ctx, string key, object val) {
            var curr = ctx.GetLocal(key);
            if(curr is not VError) {
                throw new Exception();
            }
            switch(val) {
                case Type t:
                    val = new VDeclared { type = t, name = key };
                    break;
                case VClass vc:
                    val = new VDeclared { type = vc, name = key };
                    break;
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
            var c = new VClass { name = "unknown", source_ctx = f, source_expr = block, _static = _static };
            _static.locals["_kind"] = ValKeyword.CLASS;
            _static.SetClass(c);
            return c;
        }
        public static VDictScope DeclareClass (IScope f, ExBlock block, string key) {
            var _static = block.MakeScope(f);
            var c = new VClass {
                name = key,
                source_ctx = f,
                source_expr = block,
                _static = _static
            };
            f.SetLocal(key, c);
			_static.locals["_kind"] = ValKeyword.CLASS;
			_static.SetClass(c);
            return _static;
        }
        public static VDictScope DeclareInterface (IScope f, ExBlock block, string key) {
            var _static = block.MakeScope(f);
            var vi = new VInterface {_static = _static};
            f.SetLocal(key, vi);
			_static.locals["_kind"] = ValKeyword.INTERFACE;
			_static.AddInterface(vi);
            return _static;
        }
    }
    public class ExFn : INode {
        public ExTuple pars;
        public INode result;
        //public XElement ToXML () => new("ExprFunc", [.. pars.Select(i => i.ToXML()), result.ToXML()]);
        //public string Source => $"@({string.Join(", ", pars.Select(p => p.Source))}) {result.Source}";
        public object Eval (IScope ctx) =>
         new VFn {
             expr = result,
             pars = pars.EvalTuple(ctx),
             parent_ctx = ctx
         };
    }
    public class VTypeFn : IType {
        public VFn criteria;
		public bool Accept (object src) {
			return (bool)criteria.CallData([src]);
		}
	}
    public class ExSeq : INode {
		/*, LVal */
		public INode type;
        public List<INode> items;
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
    public class ExLisp : INode {
        public INode[] args;
        public string op;
        public object Eval (IScope ctx) {
            var lhs = args.First().Eval(ctx);
            foreach(var rhs in args.Skip(1)) {
                //Get extension member or internal member
                switch(lhs) {
                    case IScope sc:
						lhs = new ExInvoke { expr = new ExMemberKey { src = ExVal.From(lhs), key = op }, args = ExTuple.Expr(rhs) }.Eval(ctx);
                        break;
					default:
                        var l = (dynamic)lhs;
						var r = (dynamic)rhs.Eval(ctx);
						lhs = op switch {
                            "+" => l + r,
                            "-" => l - r,
                            "*" => l * r,
                            "/" => l / r,
							">" => l > r,
							"<" => l < r,
							">>" => l >> r,
							"<<" => l << r,

							"=" => l == r,

                            ".." => l..r,

							"&" => l & r,
							"|" => l | r,

							"%" => l % r,
							"&&" => l && r,
							"||" => l || r,

						};
						break;
				}
            }
            return lhs;
        }
	}
    public class ExTuple : INode {
        /*, LVal*/
		public (string key, INode value)[] items;
        public IEnumerable<INode> vals => items.Select(i => i.value);
        public static ExTuple Empty => new() { items = [] };
		public static ExTuple Expr (INode v) => new() { items = [(null, v)] };
		public static ExTuple Val (object v) => Expr(new ExVal { value = v });
		public static ExTuple SpreadExpr (INode n) => Expr(new ExSpread { value = n });
		public static ExTuple SpreadVal (object v) => SpreadExpr(new ExVal { value = v });
        public static ExTuple ListExpr (IEnumerable<INode> items) => new() { items = items.Select(i => ((string)null, i)).ToArray() };
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
                void Handle(object v) {
					switch(v) {
						case VSpread vs:
							vs.SpreadTuple(key, it);
							break;
						case VEmpty:
							break;
						case VAlias va:
							Handle(va.Deref());
							//throw new Exception();
							//it.Add((key, va));
							break;
                        case VGet vg:
                            throw new Exception();
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
    public class VTuple : INode {
        public int Length => items.Length;
        public (string key, object val)[] items;
        public object[] vals => items.Select(i => i.val).ToArray();
        public object Eval (IScope ctx) => this;
        public static VTuple Single (object v) => new() { items = [(null, v)] };
        public void Spread (List<(string key, object val)> it) {
            foreach(var (key, val) in items) {
                it.Add((key, val));
            }
        }
        public ExTuple expr => new() {
            items = items.Select(pair => (pair.key, (INode)new ExVal { value = pair.val })).ToArray()
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
    public class ExMonadic {
        public INode rhs;
    }
    public class ExDyadic : INode {
        public INode lhs;
        public INode rhs;
        public EFn fn;

		public object Eval (IScope ctx) {
            switch(fn) {
                case EFn.AND: {
                        var l = (bool)lhs.Eval(ctx);
                        switch(l) {
                            case true: {
                                    var r =(bool) rhs.Eval(ctx);
                                    switch(r) {
                                        case true:  return true;
                                        case false: return false;
                                    }
                                }
                            case false: return false;
                        }
                    }
				case EFn.OR: {
						var l = (bool)lhs.Eval(ctx);
						switch(l) {
							case false: {
									var r = (bool)rhs.Eval(ctx);
									switch(r) {
										case false: return false;
										case true:  return true;
									}
								}
							case true: return true;
						}
					}

				case EFn.XOR: {
						var l = (bool)lhs.Eval(ctx);
						var r = (bool)rhs.Eval(ctx);
                        return l ^ r;
					}

				default:
                    throw new Exception();
			}
		}

		public enum EFn {
            ERR,
            AND,
            OR,
            XOR,

            };
    }
    public class StDefFn : INode {
        public string key;
        public ExTuple pars;
        public INode value;
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
    public class StDefMulti : INode {
        public string[] lhs;
        public INode rhs;
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
            throw new Exception();
        }
    }
	public class StAssignSymbol : INode {
		public ExUpKey symbol;
		public INode value;
		XElement ToXML () => new("Reassign", symbol.ToXML(), value.ToXML());
		public string Source => $"{symbol.Source} := {value.Source}";
		public object Eval (IScope ctx) {
			var curr = symbol.Eval(ctx);
			var inner_ctx = ctx.MakeTemp(curr);
			inner_ctx.locals["_curr"] = curr;
			switch(curr) {
				case VDeclared vd:
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
		public static object AssignSymbol (IScope ctx, ExUpKey sym, Func<object> getNext) => Assign(ctx, sym.key, sym.up, getNext);
		public static object Assign (IScope ctx, string key, int up, Func<object> getNext) {
			var curr = ctx.Get(key, up);
			switch(curr) {
				case VError ve: throw new Exception(ve.msg);
				case VSet vs: return vs.Set(getNext());
				case VAlias va: return va.Set(getNext);
				case VDeclared vd: return Assign(vd.type);
				case VClass vc: return AssignClass(vc);
				case VDictScope vds: return ctx.Set(key, getNext(), up);
				case ValKeyword.AUTO: return ctx.Set(key, getNext(), up);
				default: return AssignType(curr?.GetType());
			}
			object Assign (object type) {
				switch(type) {
					case Type t: return AssignType(t);
					case VClass vc: return AssignClass(vc);
				}
				throw new Exception();
			}
			object AssignClass (VClass cl) {
				var next = getNext();
				switch(next) {
					case VDictScope vds: return ctx.Set(key, vds, up);
					case VError ve: throw new Exception(ve.msg);
					default: return ctx.Set(key, next, up);
				}
			}
			object AssignType (Type prevType) {
				var next = getNext();
                if(next is VError e) {
                    throw new Exception(e.msg);
                }


				if(prevType == null) {
					goto Do;
				}
				switch(curr) {
					case VInterface vi:
						if(next is VDictScope vds) {
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
					case VError ve: throw new Exception(ve.msg);
				}
				Do:
				return ctx.Set(key, next, up);
			}
		}
		public static bool CanAssign (object prev, object next) {
			switch(prev) {
				case VError ve:
					throw new Exception(ve.msg);
				case VDeclared vd:
					return Match(vd.type);
				case VClass vc:
					return MatchClass(vc);
				case VDictScope vds:
					return true;
				default:
					return MatchType(prev?.GetType());
			}
			bool Match (object prevType) {
				switch(prevType) {
					case Type t:
						return MatchType(t);
					case VClass vc:
						return MatchClass(vc);
				}
				throw new Exception();
			}
			bool MatchClass (VClass prevClass) {
				switch(next) {
					case VDictScope vds:
						return true;
					case VError ve:
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
	public class StAssignMulti : INode {
        public bool deconstruct;
        public ExUpKey[] symbols;
        public INode value;
        public object Eval (IScope ctx) {


            return deconstruct ? AssignDestructure(ctx, symbols, value.Eval(ctx)) : AssignTuple(ctx, symbols, value.Eval(ctx));
        }
        public static object AssignDestructure(IScope ctx, ExUpKey[] symbols, object val) {
            switch(val) {
                case Args a:
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
            throw new Exception();
        }
        public static object AssignTuple (IScope ctx, ExUpKey[] symbols, object val) {
			switch(val) {
				case VTuple vt:
					if(symbols.Length == vt.items.Length) {
						foreach(var i in Enumerable.Range(0, symbols.Length)) {
							StAssignSymbol.AssignSymbol(ctx, symbols[i], () => vt.items[i].val);
						}
						return VEmpty.VALUE;
					} else {
						throw new Exception();
					}
				case Args a:
					if(a.Length == symbols.Length) {
						foreach(var i in Enumerable.Range(0, symbols.Length)) {
							StAssignSymbol.AssignSymbol(ctx, symbols[i], () => a[i]);
						}
						return VEmpty.VALUE;
					} else {
						throw new Exception();
					}
				case Array a:
					if(a.Length == symbols.Length) {
						foreach(var i in Enumerable.Range(0, symbols.Length)) {
							var v = a.GetValue(i);
							StAssignSymbol.AssignSymbol(ctx, symbols[i], () => v);
						}
						return VEmpty.VALUE;
					} else {
						throw new Exception();
					}
			}
            throw new Exception();
		}
    }
    public class StAssignDynamic:INode {
        public INode[] lhs;
        public INode rhs;
        public object Eval(IScope ctx) {
            return null;
        }
    }
    //lhs evaluates to an index or setter
    public class StAssignExpr:INode {
        public INode lhs;
        public INode rhs;
		public object Eval (IScope ctx) {
            var l = lhs.Eval(ctx);
            var r = rhs.Eval(ctx);
			switch(l) {
                case VIndex vi:
                    vi.Set(r);
                    return r;
                case VSet vs:
                    vs.Set(r);
                    return r;
            }
            throw new Exception();
		}
	}
    public record Var {
		public bool pub;
		public bool mut;
        public bool init;
        public object type;
        public object val;
        public void Init(VCast vc) {
            this.type = vc.type;
            this.val = vc.val;
        }
        public void Assign(VCast vc) {
            this.val = vc.val;
        }
    }
    public record VCast {
        public object type;
        public object val;
    }
    public interface INode {
        XElement ToXML () => new(GetType().Name);
        string Source => "";
        object Eval (IScope ctx);
    }
    public interface LVal {
        object Assign (IScope ctx, Func<object> getVal);
        //IEnumerable<LVal> Unpack ();
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

            
			if(Enum.IsDefined(typeof(TokenType), (ulong)c)) {

				var _t = (TokenType)(ulong)c;
				index += 1;
				return new Token { type = _t, str = str(c) };
			}
            throw new Exception();
        }
    }
    public enum TokenType : ulong {
        COMMA = ',',
        COLON = ':',
		BLOCK_L = '{',
		BLOCK_R = '}',
		TUPLE_L = '(',
        TUPLE_R = ')',
        ARRAY_L = '[',
        ARRAY_R = ']',
        ANGLE_L = '<',
        ANGLE_R = '>',
        CARET = '^',
        PERIOD = '.',
        EQUAL ='=',
		PLUS = '+',
		MINUS = '-',
		SLASH = '/',
		QUOTE = '\'',
		AT = '@',
        QUESTION = '?',
        SHOUT = '!',
        STAR = '*',
        PIPE = '|',
        AMP = '&',
        CASH = '$',
        PERCENT = '%',
        HASH = '#',
        REPEAT = '¨',
		FLOOR = '⌊',
		TIMES = '×',
		IOTA = 'ɩ',
        DIVIDE = '÷',

        NOT= '¬',
        INV_QUESTION = '¿',

		NOT_EQUAL = '≠',
        APPROX_EQUAL = '≈',
        EQUIV = '≡',
        NOT_EQUIV = '≢',
        INF = '∞',
        INTERSECT = '∩',
        INTEGRAL = '∫',
        SQRT = '√',
        BULLET_OPERATOR = '∙',
        SUM = '∑',
        PRODUCT = '∏',
        INCREMENT = '∆',
        GEQ = '≥',
        LEQ = '≤',

		ARROW_W = '←',
        ARROW_E = '→',
        ARROW_S = '↓',
        ARROW_N = '↑',
        CROSS_PRODUCT = '⨯',

		HOUSE = '⌂',
        DEGREE = '°',
        BULLET = '•',
        SECTION = '§',
        LOZENGE = '◊',
        CIRCLE = '○',
        INV_SHOUT = '¡',
        TRI_N = '▲',
        TRI_S = '▼',
        MINI_TRI_N = '▴',
        MINI_TRI_E = '▸',
        MINI_TRI_S = '▾',
        MINI_TRI_W = '◂',
        PTR_R = '►',
        PTR_L = '◄',
        INTERRO = '‽',
        DOUBLE_SHOUT = '‼',
		SPACE = ' ',

        IN = '∈',
        NOT_IN = '∉',

        EMPTY = '∅',

        IS_SUBSET = '⊆',
        HAS_SUBSET = '⊇',

        SUBSET = '⊂',
        SUBSET_2 = '⊃',

        DELTA = 'Δ',

		OR = '∨',
		AND = '∧',
		XOR = '⊻',

		CAP = '∩',
        CUP = '∪',

        CO_PRODUCT = '⊔',
        AAA = '⊓',

		NAME = 0xFFFFFFFFFFFFFFF0,
		STRING,
        INTEGER,
		EOF,
    }
	//≣
	//«µ»əɅʌΘ∕❮❯❰❱
	//
	//Ⱶⱻ♪♫↔↕↨∟
	public class Token {
        public TokenType type;
        public string str;

        public string ToString () => $"[{type}] {str}";
    }
}