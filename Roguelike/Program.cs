using LibGamer;
using ExtSadConsole;
using System.Text;
using Oblivia;
using System.Collections;

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

		["double_from"] = Val((int i) => (double)i),
		["int_from"] = Val((double d) => (int)d),
		["uint_from"] = Val((int d) => (uint)d),

		["void"] = typeof(void),
		["char"] = typeof(char),
		["bool"] = typeof(bool),
		["int"] = typeof(int),
		["uint"] = typeof(uint),
		["double"] = typeof(double),
		["string"] = typeof(string),
		["object"] = typeof(object),


		["addi"] = Val((int a, int b) => a + b),
		["subi"] = Val((int a, int b) => a - b),
		["muli"] = Val((int a, int b) => a * b),
		["divi"] = Val((int a, int b) => a / b),
		["modi"] = Val((int a, int b) => a % b),
		["xori"] = Val((int a, int b) => a ^ b),

		["addf"] = Val((double a, double b) => a + b),
		["subf"] = Val((double a, double b) => a - b),
		["mulf"] = Val((double a, double b) => a * b),
		["divf"] = Val((double a, double b) => a / b),

		["sumi"] = Val((int[] a) => a.Sum()),

		["count"] = Val((IEnumerable data, object value) => data.Cast<object>().Count(d => {
			var result = d.Equals(value);
			return result;
		})),
		["gt"] = Val((double a, double b) => a > b),
		["geq"] = Val((double a, double b) => a >= b),
		["lt"] = Val((double a, double b) => a < b),
		["leq"] = Val((double a, double b) => a <= b),
		["eq"] = Val((double a, double b) => a == b),
		["neq"] = Val((double a, double b) => a != b),

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

		["Row"] = Val((Type type) => type.MakeArrayType(1)),
		["Grid"] = Val((Type type) => type.MakeArrayType(2)),
		["List"] = Val((Type type) => typeof(List<>).MakeGenericType(type)),
		["Dictionary"] = Val((Type key, Type val) => typeof(Dictionary<,>).MakeGenericType(key, val)),
		["StringBuilder"] = typeof(StringBuilder)
	}
};
var result = (ValDictScope)block.Eval(global);



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
	}
	void IScene.Update(System.TimeSpan delta) {
		(ctx.locals["update"] as ValFunc).CallData(ctx, [delta]);
	}
	void IScene.Render(System.TimeSpan delta) {
		(ctx.locals["render"] as ValFunc).CallData(ctx, [delta]);
	}
}