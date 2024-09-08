using LibGamer;
using ExtSadConsole;
using System.Text;
using Oblivia;
using System.Collections;
using SadConsole.Effects;
using SadConsole.Input;
using System.Collections.Concurrent;
using Common;

var tokenizer = new Tokenizer(File.ReadAllText("Mainframe.obl"));
var parser = new Parser(tokenizer.GetAllTokens());
var block = parser.NextBlock();

T Val<T> (T t) => t;
var global = new ValDictScope {
	locals = new Dictionary<string, dynamic> {
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

		["pairi"] = Val((int a, int b) => (a, b)),

		["double_from"] = Val((int i) => (double)i),
		["int_from"] = Val((double d) => (int)d),
		["uint_from"] = Val((int i) => (uint)i),
		["char_from"] = Val((int i) => (char)i),
		["byte_from"] = Val((int i) => (byte)i),

		["void"] = typeof(void),
		["char"] = typeof(char),
		["bool"] = typeof(bool),
		["int"] = typeof(int),
		["uint"] = typeof(uint),
		["double"] = typeof(double),
		["string"] = typeof(string),
		["object"] = typeof(object),

		["bit"] = typeof(bool),
		["b1"] = true,
		["b0"] = false,

		["empty"] = ValEmpty.VALUE,

		["default"] = Val((Type t) => t.IsValueType ? Activator.CreateInstance(t) : null),

		["null"] = null,

		["addi"] = Val((int a, int b) => a + b),
		["subi"] = Val((int a, int b) => a - b),
		["muli"] = Val((int a, int b) => a * b),
		["divi"] = Val((int a, int b) => a / b),
		["modi"] = Val((int a, int b) => a % b),
		["xori"] = Val((int a, int b) => a ^ b),
		["mini"] = Val((int a, int b) => Math.Min(a, b)),
		["maxi"] = Val((int a, int b) => Math.Max(a, b)),

		["addf"] = Val((double a, double b) => a + b),
		["subf"] = Val((double a, double b) => a - b),
		["mulf"] = Val((double a, double b) => a * b),
		["divf"] = Val((double a, double b) => a / b),
		["minf"] = Val((double a, double b) => Math.Min(a, b)),
		["maxf"] = Val((double a, double b) => Math.Max(a, b)),

		["sumi"] = Val((int[] a) => a.Sum()),

		["not"] = Val((bool b) => !b),
		["and"] = Val((bool a, bool b) => a && b),
		["or"] = Val((bool a, bool b) => a || b),

		["count"] = Val((IEnumerable data, object value) => data.Cast<object>().Count(d => {
			var result = d.Equals(value);
			return result;
		})),
		["gt"] = Val((double a, double b) => a > b),
		["geq"] = Val((double a, double b) => a >= b),
		["lt"] = Val((double a, double b) => a < b),

		["bt"] = Val((double a, double b, double c) => a > b && a < c),
		["beq"] = Val((double a, double b, double c) => a >= b && a <= c),


		["leq"] = Val((double a, double b) => a <= b),
		["eq"] = Val((object a, object b) => Equals(a, b)),
		["neq"] = Val((object a, object b) => !Equals(a,b)),

		["true"] = true,
		["false"] = false,

		["print"] = Val((object o) => Console.WriteLine(o)),
		["printcat"] = Val((object[] o) => Console.WriteLine(string.Join(null, o))),
		["clear"] = Val(() => Console.Clear()),
		["set_cursor"] = Val(Console.SetCursorPosition),

		["Console"] = typeof(Console),

		["cat"] = Val((object[] o) => string.Join(null, o)),
		["range"] = Val((int a, int b) => Enumerable.Range(a, b - a).ToArray()),
		["newline"] = "\n",

		["str"] = Val((object o) => o.ToString()),

		["Array"] = Val((Type type, int dim) => type.MakeArrayType(dim)),
		["array_get"] = Val((Array a, int[] ind) => a.GetValue(ind)),
		["array_set"] = Val((Array a, int[] ind, object value) => a.SetValue(value, ind)),
		["array_at"] = Val((Array a, int[] ind) => new ValRef { src = a, index = ind }),

		["str_append"] = Val((StringBuilder sb, object o) => sb.Append(o)),
		["row_from"] = Val((Type t, object[] items) => {
			var result = Array.CreateInstance(t, items.Length);
			Array.Copy(items, result, items.Length);
			return result;
		}),
		["rand_bool"] = Val(() => new Random().Next(2) == 1),
		["randf"] = Val(new Random().NextDouble),
		["rand_range"] = Val((int a, int b) => new Random().Next(a, b)),

		["Row"] = Val((object type) => (type as Type ?? typeof(object)).MakeArrayType(1)),
		["Grid"] = Val((Type type) => type.MakeArrayType(2)),
		["List"] = Val((object type) => typeof(List<>).MakeGenericType(type as Type ?? typeof(object))),
		["List"] = Val((object item) => MakeGeneric(typeof(List<>), item)),
		["HashSet"] = Val((object item) => MakeGeneric(typeof(HashSet<>), item)),
		["Dictionary"] = Val((Type key, Type val) => typeof(Dictionary<,>).MakeGenericType(key, val)),
		["ConcurrentDictionary"] = Val((Type key, Type val) => typeof(ConcurrentDictionary<,>).MakeGenericType(key, val)),
		["StringBuilder"] = typeof(StringBuilder),
		["ValFunc"] = typeof(ValFunc),

		["Common"] = typeof(Main),
	}
};

Type MakeGeneric (Type gen, object item) => item is Type t ? gen.MakeGenericType(t) : gen.MakeGenericType(typeof(object));

var result = (ValDictScope)block.StagedEval(global);
/*
	A: class {
		a:B{}
	}
	B: class {
		b:A{}
	}
 */
Runner.Run("Assets/font/IBMCGA+_8x8.font", r => {
	r.Go(new Mainframe(result));

});
return;
class Mainframe : IScene {
	public Action<IScene> Go { get; set; }
	public Action<Sf> Draw { get; set; }
	public Action<SoundCtx> PlaySound { get; set; }
	public Tf FONT_8x8 = new Tf(File.ReadAllBytes($"Assets/font/IBMCGA+_8x8.png"), "IBMCGA+_8x8", 8, 8, 256 / 8, 256 / 8, 219);
	public Tf FONT_6x8 = new Tf(File.ReadAllBytes($"Assets/font/IBMCGA+_6x8.png"), "IBMCGA+_6x8", 6, 8, 192 / 6, 128 / 8, 219);

	ValDictScope ctx;
	public Mainframe (ValDictScope ctx) {
		this.ctx = ctx;
		ctx.locals["scene"] = this;
		(ctx.locals["init"] as ValFunc).CallData(ctx, []);
	}
	void IScene.Update(System.TimeSpan delta) {
		(ctx.locals["update"] as ValFunc).CallData(ctx, [delta]);
	}
	void IScene.Render(System.TimeSpan delta) {
		(ctx.locals["render"] as ValFunc).CallData(ctx, [delta]);
	}
	void IScene.HandleKey(LibGamer.KB kb) {
		(ctx.locals["handle_key"] as ValFunc).CallData(ctx, [kb]);
	} 
	void IScene.HandleMouse(LibGamer.HandState mouse) {
		(ctx.locals["handle_mouse"] as ValFunc).CallData(ctx, [mouse]);
	}
}