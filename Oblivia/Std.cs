using System.Collections;
using System.Collections.Concurrent;
using System.Data;
using System.Text;
namespace Oblivia {
	public class Std {
        public static VDictScope std;
		static Std () {
			T _<T> (T t) => t;
			Type MakeGeneric (Type gen, params object[] item) => gen.MakeGenericType(item.Select(i => i switch { Type t => t, _ => typeof(object) }).ToArray());

            var modules = new Dictionary<string, VDictScope>();

            VDictScope module(string s) {
                if(modules.TryGetValue(s, out var d)) {
                    return d;
                }

                var src = File.ReadAllText(s);


				//throw new Exception(src);
				var tokenizer = new Lexer(src);
                var tokens = tokenizer.GetAllTokens();
                var parser = new Parser(tokens);
                var scope = parser.NextBlock();
                var result = new VDictScope { parent = std };
                result = (VDictScope)scope.StagedApply(result);
                return modules[s] = result;
            };

            std = new VDictScope {
                locals = new() {
                    ["File"] = typeof(File),
                    ["Console"] = typeof(Console),


                    ["dbg"] = _((object o) => {
                        return new VAttribute { name = "Debug" };
                    }),

					["mut"] = VKeyword.MUT,
					["val"] = VKeyword.VAL,
					["rest"] = VKeyword.REST,


					["char"] = typeof(char),

                    ["Pt"] = typeof((int, int)),

                    ["i4x2"] = _((int a, int b) => (a, b)),

                    ["i4_f8"] = _((int i) => (double)i),
                    ["f8_i4"] = _((double d) => (int)d),
                    ["i4_u4"] = _((int i) => (uint)i),
                    ["i4_ch"] = _((int i) => (char)i),
                    ["i4_u1"] = _((int i) => (byte)i),

                    ["void"] = typeof(void),
                    ["chr"] = typeof(char),
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

                    ["_else"] = VKeyword.GO_ELSE,

                    ["yes"] = true,
                    ["no"] = false,
                    ["empty"] = VEmpty.VALUE,
                    ["default"] = _((Type t) => t.IsValueType ? Activator.CreateInstance(t) : null),
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

                    ["cat"] = _((object[] o) => string.Join(null, o)),
                    ["range"] = _((int a, int b) => Enumerable.Range(a, b - a).ToArray()),
                    ["newline"] = "\n",
                    ["obj_str"] = _((object o) => o.ToString()),

                    ["ch_arr"] = _((string s) => s.ToCharArray()),

                    ["Array"] = _((Type type, int dim) =>

                    type.MakeArrayType(dim)),
                    ["arr_get"] = _((Array a, int[] ind) => a.GetValue(ind)),
                    ["arr_set"] = _((Array a, int[] ind, object value) => a.SetValue(value, ind)),
                    ["arr_at"] = _((Array a, int[] ind) => new VRef { src = a, index = ind }),
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

                    ["sat"] = VKeyword.SAT,
                    ["fall"] = VKeyword.FALL,
                    ["default"] = VKeyword.DEFAULT,
                    ["class"] = VKeyword.CLASS,
                    ["interface"] = VKeyword.INTERFACE,
                    ["ext"] = VKeyword.EXTEND,
                    ["enum"] = VKeyword.ENUM,
                    ["get"] = VKeyword.GET,
                    ["set"] = VKeyword.SET,
                    ["impl"] = VKeyword.IMPLEMENT,
                    ["inherit"] = VKeyword.INHERIT,
                    ["cut"] = VKeyword.BREAK,
                    ["skip"] = VKeyword.CONTINUE,
                    ["cancel"] = VKeyword.CANCEL,
                    ["ret"] = VKeyword.RETURN,
                    ["var"] = VKeyword.VAR,
                    ["yield"] = VKeyword.YIELD,
                    ["unmask"] = VKeyword.UNALIAS,
                    ["declare"] = VKeyword.DECLARE,
                    ["complement"] = VKeyword.COMPLEMENT,
                    ["any"] = VKeyword.ANY,
                    ["all"] = VKeyword.ALL,
                    ["fmt"] = VKeyword.FMT,
                    ["regex"] = VKeyword.REGEX,
                    ["replace"] = VKeyword.REPLACE,
                    ["macro"] = VKeyword.MACRO,
                    ["magic"] = VKeyword.MAGIC,

                    ["go"] = VKeyword.GO,

                    ["keys_of"] = VKeyword.KEYS_OF,

                    ["pub"] = VKeyword.PUB,
                    ["priv"] = VKeyword.PRIV,
                    ["instance"] = VKeyword.INSTANCE,
                    ["static"] = VKeyword.STATIC,

                    ["fmt"] = VKeyword.FMT,
                    ["leaf"] = VKeyword.LEAF,
                    ["xml"] = VKeyword.XML,
                    ["json"] = VKeyword.JSON,
                    ["math"] = VKeyword.MATH,

                    ["module"] = _(module),
					["import"] = VKeyword.IMPORT,
					["embed"] = VKeyword.EMBED,

                    ["ctx"] = VKeyword.CTX,

                    ["attr"] = VKeyword.ATTR,

					["ɩ"] = _((int i) => Enumerable.Range(0, i))
                }
            };
		}
	}
}