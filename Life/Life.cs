// See https://aka.ms/new-console-template for more information
using System.Collections;
using System.Text;
using Oblivia;
var scope = Parser.FromFile("Assets/Life.obl");
T Val<T> (T t) => t;
var global = new VDictScope();
global.locals = new() {
	["File"] = typeof(File),
	["count"] = Val((IEnumerable data, object value) => data.Cast<object>().Count(d => {
		var result = d.Equals(value);
		return result;
	})),
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
};
var result = (VDictScope)scope.StagedApply(Std.std);
var r = (result.locals["main"] as VFn).CallPars(result, ExTuple.Val("program"));
return;