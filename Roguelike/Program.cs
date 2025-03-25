using LibGamer;
using ExtSadConsole;
using System.Text;
using Oblivia;
using System.Collections;
using SadConsole.Effects;
using SadConsole.Input;
using System.Collections.Concurrent;

var tokenizer = new Tokenizer(File.ReadAllText("Mainframe.obl"));
var parser = new Parser(tokenizer.GetAllTokens());
var block = parser.NextBlock();
T _<T> (T t) => t;
var global = new VDictScope {
	locals = new() {

		["_0x8080U"] = 0x8080U,

		["Sf"] = typeof(Sf),
		["ABGR"] = typeof(ABGR),
		["TimeSpan"] = typeof(TimeSpan),
		["Runner"] = typeof(Runner),
		["KB"] = typeof(KB),
		["Hand"] = typeof(Hand),
		["HandState"] = typeof(HandState),
		["RectOptions"] = typeof(RectOptions),
		["KC"] = typeof(KC),
		["Pt"] = typeof((int, int)),

		["pairi"] = _((int a, int b) => (a, b)),
		["i32x2"] = _((int a, int b) => (a, b)),

		["double_from"] = _((int i) => (double)i),
		["int_from"] = _((double d) => (int)d),
		["uint_from"] = _((int i) => (uint)i),
		["char_from"] = _((int i) => (char)i),
		["byte_from"] = _((int i) => (byte)i),

		["void"] = typeof(void),
		["char"] = typeof(char),
		["bool"] = typeof(bool),
		["int"] = typeof(int),
		["uint"] = typeof(uint),

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

		["double"] = typeof(double),
		["string"] = typeof(string),
		["str"] = typeof(string),
		["object"] = typeof(object),
		["obj"] = typeof(object),

		["bit"] = typeof(bool),
		["b1"] = true,
		["b0"] = false,

		["empty"] = VEmpty.VALUE,

		["default"] = _((Type t) => t.IsValueType ? Activator.CreateInstance(t) :null),

		["null"] = null,

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
		["modf"] = _((double a, double b) => Math.IEEERemainder(a, b) + b/2),
		["minf"] = _((double a, double b) => Math.Min(a, b)),
		["maxf"] = _((double a, double b) => Math.Max(a, b)),

		["sumi"] = _((int[] a) => a.Sum()),

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

		["bt"] = _((double a, double b, double c) => a > b && a < c),
		["beq"] = _((double a, double b, double c) => a >= b && a <= c),

		["leq"] = _((double a, double b) => a <= b),
		["eq"] = _((object a, object b) => Equals(a, b)),
		["neq"] = _((object a, object b) => !Equals(a,b)),

		["true"] = true,
		["false"] = false,

		["print"] = _((object o) => Console.WriteLine(o)),
		["printcat"] = _((object[] o) => Console.WriteLine(string.Join(null, o))),
		["clear"] = _(() => Console.Clear()),
		["set_cursor"] = _(Console.SetCursorPosition),

		["Console"] = typeof(Console),

		["cat"] = _((object[] o) => string.Join(null, o)),
		["range"] = _((int a, int b) => Enumerable.Range(a, b - a).ToArray()),
		["newline"] = "\n",
		["str"] = _((object o) => o.ToString()),
		["Array"] = _((Type type, int dim) => type.MakeArrayType(dim)),
		["arr_get"] = _((Array a, int[] ind) => a.GetValue(ind)),
		["arr_set"] = _((Array a, int[] ind, object value) => a.SetValue(value, ind)),
		["arr_at"] = _((Array a, int[] ind) => new ValRef { src = a, index = ind }),
		["str_append"] = _((StringBuilder sb, object o) => sb.Append(o)),
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
		["List"] = _((object type) => typeof(List<>).MakeGenericType(type as Type ?? typeof(object))),
		["List"] = _((object item) => MakeGeneric(typeof(List<>), item)),
		["HashSet"] = _((object item) => MakeGeneric(typeof(HashSet<>), item)),
		["Dictionary"] = _((Type key, Type val) => typeof(Dictionary<,>).MakeGenericType(key, val)),
		["ConcurrentDictionary"] = _((Type key, Type val) => typeof(ConcurrentDictionary<,>).MakeGenericType(key, val)),
		["StringBuilder"] = typeof(StringBuilder),
		["ValFunc"] = typeof(VFn),
		["Common"] = typeof(Main),
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
		["is"] = ValKeyword.ALL,
		["fmt"] = ValKeyword.FMT,
		["regex"] = ValKeyword.REGEX,
		["replace"] = ValKeyword.REPLACE,
		["macro"] = ValKeyword.MACRO,
	}
};
PriorityQueue<object, int> a = new();
Type MakeGeneric (Type gen, params object[] item) => gen.MakeGenericType(item.Select(i => i switch { Type t => t, _ => typeof(object) }).ToArray());
var result = (VDictScope)block.StagedEval(global);
Runner.Run("Assets/font/IBMCGA+_8x8.font", r => {
	r.Go(new Mainframe(result));
});
class Mainframe :IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public Tf FONT_8x8 = new Tf(File.ReadAllBytes($"Assets/font/IBMCGA+_8x8.png"), "IBMCGA+_8x8", 8, 8, 256 / 8, 256 / 8, 219);
	public Tf FONT_6x8 = new Tf(File.ReadAllBytes($"Assets/font/IBMCGA+_6x8.png"), "IBMCGA+_6x8", 6, 8, 192 / 6, 128 / 8, 219);

	VDictScope ctx;
	public Mainframe (VDictScope ctx) {
		this.ctx = ctx;
		ctx.locals["scene"] = this;
		
		var VF = (string s) => (VFn)ctx.locals[s];
		VF("init").CallData([]);
		update = VF("update");
		render = VF("render");
		handle_key = VF("handle_key");
		handle_mouse = VF("handle_mouse");
	}
	VFn update, render, handle_key, handle_mouse;
	void IScene.Update(System.TimeSpan delta) {
		update.CallData([delta]);
	}
	void IScene.Render(System.TimeSpan delta) {
		render.CallData([delta]);
	}
	void IScene.HandleKey(LibGamer.KB kb) {
		handle_key.CallData([kb]);
	} 
	void IScene.HandleMouse(LibGamer.HandState mouse) {
		handle_mouse.CallData([mouse]);
	}
}